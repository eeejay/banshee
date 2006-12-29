/***************************************************************************
 *  BurnerSessionPreparer.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Cdrom;
using Banshee.AudioProfiles;

namespace Banshee.Burner
{
    internal class BurnerSessionPreparer
    {
        private BurnerSession session;
        
        private List<string> warnings = new List<string>();
        
        private List<BurnerSessionTrack> tracks;
        private TimeSpan total_duration = TimeSpan.Zero;
        private long total_file_size = 0;
    
        private Queue<TrackInfo> encode_queue = new Queue<TrackInfo>();
        private Queue<SafeUri> burn_queue = new Queue<SafeUri>();
        private Profile profile;
        private BatchTranscoder transcoder;
        private bool canceled = false;
    
        public BurnerSessionPreparer(BurnerSession session)
        {
            this.session = session;
            tracks = new List<BurnerSessionTrack>(session.Tracks);
        }
        
        public bool Record()
        {
            if(!CheckFiles()) {
                return false;
            }
            
            if(!CheckDiscSpace()) {
                return false;
            }
            
            if(!CheckTemporarySpace()) {
                return false;
            }
            
            return PrepareDisc();
        }
        
        private bool CheckFiles()
        {
            warnings.Clear();
            
            foreach(BurnerSessionTrack track in tracks) {
                try {
                    Gnome.Vfs.FileInfo file = new Gnome.Vfs.FileInfo(track.Track.Uri.AbsoluteUri);
                    total_file_size += file.Size;
                    total_duration += track.Track.Duration;
                    encode_queue.Enqueue(track.Track);
                } catch {
                    if(track.Track.Uri.Scheme == "file") {
                        warnings.Add(track.Track.Uri.LocalPath);
                    } else {
                        warnings.Add(track.Track.Uri.AbsoluteUri);
                    }
                    
                    continue;
                }
            }
            
            return HandleWarnings(Catalog.GetString("Some songs could not be found."));
        }
        
        private bool CheckDiscSpace()
        {
            if(session.Recorder == null) {
                LogCore.Instance.PushWarning(
                    Catalog.GetString("Problem creating CD"),
                    Catalog.GetString("No CD writers were found on your system."));
                return false;
            }
            
            if(session.Recorder.MediaCapacity <= 0) {
                LogCore.Instance.PushWarning(
                    Catalog.GetString("Insert Blank CD"),
                    Catalog.GetString("Please insert a blank CD disk for the write process."));
                return false;
            }
            
            switch(session.DiscFormat) {
                case "audio":
                    return HaveRequiredAudioSpace();
                default: /* should probably find a good way to estimate MP3 size here */
                    return HaveRequiredDataSpace();
            }
        }
        
        private bool HaveRequiredAudioSpace() 
        {
            long available = (long)(((session.Recorder.MediaCapacity / 1024 / 1024) - 1) * 48 / 7);
            long remaining = (long)(available - total_duration.TotalSeconds);

            if(remaining < 0) {
                int minutes = (int)(-remaining / 60);
                string msg = String.Format(
                    Catalog.GetString("The inserted media is not large enough to hold your selected music.") + " " +
                    Catalog.GetPluralString(
                        "{0} more minute is needed on the media.",
                        "{0} more minutes are needed on the media.",
                        minutes), minutes);
            
                LogCore.Instance.PushWarning(Catalog.GetString("Not Enough Space on Disc"), msg);
                return false;
            }

            return true;
        }
        
        private bool HaveRequiredDataSpace()
        {
            long remaining = session.Recorder.MediaCapacity - total_file_size;
            
            if(remaining < 0) {
                int mbytes = (int)(-remaining / 1024 / 1024);
                string msg = String.Format(
                    Catalog.GetString("The inserted media is not large enough to hold your selected music.") + " " +
                    Catalog.GetPluralString(
                        "{0} more megabyte is needed on the media.",
                        "{0} more megabytes are needed on the media.",
                        mbytes), mbytes);
                        
                LogCore.Instance.PushWarning(Catalog.GetString("Not Enough Space on Disc"), msg);
                return false;
            }

            return true;
        }
                
        private bool CheckTemporarySpace()
        {
            long estimated_encoded_bytes = BurnerUtilities.DiscTimeToSize(total_duration);
            long free_space = PathUtil.GetDirectoryAvailableSpace(Paths.TempDir);
            
            if(free_space >= 0 && estimated_encoded_bytes >= free_space) {
                LogCore.Instance.PushError(Catalog.GetString("Insufficient Disk Space"),
                    String.Format(Catalog.GetString("Creating this CD requires at least {0} MiB of free disk space."),
                        Math.Ceiling((double)estimated_encoded_bytes / 1000000)));
                return false;
            }
            
            return true;
        }
        
        private bool HandleWarnings(string message)
        {
            if(warnings.Count == 0) {
                return true;
            }
        
            Banshee.Gui.ErrorListDialog dialog = new Banshee.Gui.ErrorListDialog();

            dialog.Header = Catalog.GetString("Problem creating CD");
            dialog.Message = Catalog.GetString(message);
            dialog.AddStockButton("gtk-cancel", ResponseType.Cancel);
            dialog.AddButton(Catalog.GetString("Continue Anyway"), ResponseType.Ok);
            dialog.IconName = "gtk-dialog-warning";
            dialog.Dialog.SetSizeRequest(400, -1);

            foreach(string warning in warnings) {
                dialog.AppendString(warning);
            }
            
            ResponseType response = dialog.Run();
            dialog.Destroy();

            if(response != ResponseType.Ok) {
                CleanTemp();
                return false;
            }
            
            return true;
        }
        
        private void CleanTemp()
        {
            foreach(string file in Directory.GetFiles(Paths.TempDir)) {
                try {
                    File.Delete(file);
                } catch {
                }
            }
        }
        
        private bool PrepareDisc()
        {
            switch(session.DiscFormat) {
                case "audio":
                    profile = Globals.AudioProfileManager.GetProfileForMimeType("audio/x-wav");
                    break;
                case "mp3":
                    profile = Globals.AudioProfileManager.GetProfileForMimeType("audio/mp3");
                    break;
                default:
                    return CreateImage();
            }
            
            if(profile == null) {
                 LogCore.Instance.PushError(
                     Catalog.GetString("Could Not Write CD"),
                     Catalog.GetString("No suitable encoder could be found to convert selected songs."));
                 return false;
            }
        
            transcoder = new BatchTranscoder(profile);
            transcoder.FileFinished += OnFileEncodeComplete;
            transcoder.BatchFinished += OnFileEncodeTransactionFinished;
            transcoder.Canceled += OnFileEncodeTransactionCanceled;
           
            while(encode_queue.Count > 0) {
                TrackInfo track = encode_queue.Dequeue();
                string output_file = null;
                
                if(Path.GetExtension(track.Uri.LocalPath) == "." + profile.OutputFileExtension) {
                    output_file = track.Uri.LocalPath;
                } else {
                    output_file = Paths.TempDir + "/"  +
                        Path.GetFileNameWithoutExtension(track.Uri.LocalPath) + "." + 
                        profile.OutputFileExtension;
                }
                
                transcoder.AddTrack(track, new SafeUri(output_file));
            }
            
            transcoder.Start();
            return true;
        }
        
        private void OnFileEncodeComplete(object o, FileCompleteArgs args)
        {
            burn_queue.Enqueue(args.EncodedFileUri);
        }
        
        private void OnFileEncodeTransactionFinished(object o, EventArgs args)
        {
            if(!canceled) {
                if(session.DiscFormat == "mp3") {
                    CreateImage();
                } else {
                    DoBurn();
                }
            }
        }
        
        private void OnFileEncodeTransactionCanceled(object o, EventArgs args)
        {
            canceled = true;
        }
        
        private bool CreateImage()
        {
            LogCore.Instance.PushError("Incomplete", "MP3/Data CD support has not yet been ported to Banshee's new CD Burning architecture. Mmmmmaahh... the joy of playing with HEAD.");
            return false;
        }
        
        private void DoBurn()
        {
            BurnerSessionRecorder session_recorder = new BurnerSessionRecorder(session);
            while(burn_queue.Count > 0) {
                SafeUri uri = burn_queue.Dequeue();
                session_recorder.AddTrack(new RecorderTrack(uri.LocalPath, RecorderTrackType.Audio));
            }
            session_recorder.Record();
        }
    }
}
