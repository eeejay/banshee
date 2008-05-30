//
// DatabaseRebuilder.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Mono.Unix;
using IPod;

using Hyena;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Widgets;

namespace Banshee.Dap.Ipod
{
    public class DatabaseRebuilder
    {
        private class FileContainer
        {
            public string Path;
            public TagLib.File File;
        }
        
        private class FileContainerComparer : IComparer<FileContainer>
        {
            public int Compare (FileContainer a, FileContainer b)
            {
                int artist = String.Compare (a.File.Tag.FirstPerformer, b.File.Tag.FirstPerformer);
                if (artist != 0) {
                    return artist;
                }
                
                int album = String.Compare (a.File.Tag.Album, b.File.Tag.Album);
                if (album != 0) {
                    return album;
                }
                
                int at = (int)a.File.Tag.Track;
                int bt = (int)b.File.Tag.Track;
                
                if (at == bt) {
                    return 0;
                } else if (at < bt) {
                    return -1;
                }
                
                return 1;
            }
        }
    
        private IpodSource source;
        private UserJob user_job;
        private Queue<FileInfo> song_queue = new Queue<FileInfo>();
        private List<FileContainer> files = new List<FileContainer>();
        private int discovery_count;

        public event EventHandler Finished;

        public DatabaseRebuilder (IpodSource source)
        {
            this.source = source;
            
            user_job = new UserJob (Catalog.GetString ("Rebuilding Database"));
            user_job.Title = Catalog.GetString ("Rebuilding Database");
            user_job.Status = Catalog.GetString ("Scanning iPod...");
            user_job.IconNames = source._GetIconNames ();
            user_job.CanCancel = true;
            user_job.Register ();
            
            ThreadPool.QueueUserWorkItem (RebuildDatabase);
        }
        
        private void RebuildDatabase (object state)
        {
            string music_path = Paths.Combine (source.IpodDevice.ControlPath, "Music");
            
            Directory.CreateDirectory (source.IpodDevice.ControlPath);
            Directory.CreateDirectory (music_path);
        
            DirectoryInfo music_dir = new DirectoryInfo (music_path);
                
            foreach (DirectoryInfo directory in music_dir.GetDirectories ()) {
                ScanMusicDirectory (directory);
            }
            
            ProcessTrackQueue ();
        }
        
        private void ScanMusicDirectory (DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles ()) {
                song_queue.Enqueue (file);
            }
        }
        
        private void ProcessTrackQueue ()
        {
            discovery_count = song_queue.Count;
            
            user_job.Status = Catalog.GetString ("Processing Tracks...");
            
            while (song_queue.Count > 0) {
                user_job.Progress = (double)(discovery_count - song_queue.Count) / (double)discovery_count;
                
                try {
                    ProcessTrack (song_queue.Dequeue ());
                } catch {
                }
                
                if (user_job.IsCancelRequested) {
                    break;
                }
            }
            
            user_job.Progress = 0.0;
            user_job.Status = Catalog.GetString ("Ordering Tracks...");
            
            files.Sort (new FileContainerComparer ());
            
            foreach (FileContainer container in files) {
                try {
                    ProcessTrack (container);
                } catch {
                }
                
                if (user_job.IsCancelRequested) {
                    break;
                }
            }
            
            if (!user_job.IsCancelRequested) {
                SaveDatabase ();
            }
            
            user_job.Finish ();
            user_job = null;
            
            OnFinished ();
        }
        
        private void ProcessTrack (FileInfo file)
        {
            TagLib.File af = Banshee.IO.DemuxVfs.OpenFile (file.FullName);
            FileContainer container = new FileContainer ();
            container.File = af;
            container.Path = file.FullName;
            files.Add (container);
        }
        
        private void ProcessTrack (FileContainer container)
        {
            TagLib.File af = container.File;
            Track song = source.IpodDevice.TrackDatabase.CreateTrack ();

            song.FileName = container.Path;
            song.Album = af.Tag.Album;
            song.Artist = af.Tag.FirstPerformer;
            song.Title = af.Tag.Title;
            song.Genre = af.Tag.FirstGenre;
            song.TrackNumber = (int)af.Tag.Track;
            song.TotalTracks = (int)af.Tag.TrackCount;
            song.Duration = af.Properties.Duration;
            song.Year = (int)af.Tag.Year;
            song.BitRate = af.Properties.AudioBitrate / 1024;
            song.SampleRate = (ushort)af.Properties.AudioSampleRate;
            song.Type = MediaType.Audio;
            if ((af.Properties.MediaTypes & TagLib.MediaTypes.Video) != 0) {
                song.Type = MediaType.Video;
            }
            
            ResolveCoverArt (song);
        }
        
        private void ResolveCoverArt (Track track)
        {
            string aaid = CoverArtSpec.CreateArtistAlbumId (track.Artist, track.Album);
            string path = CoverArtSpec.GetPath (aaid);
            
            if (File.Exists (path)) {
                IpodTrackInfo.SetIpodCoverArt (source.IpodDevice, track, path);
            }
        }
        
        private void SaveDatabase ()
        {
            user_job.CanCancel = false;
            user_job.Status = Catalog.GetString ("Saving new database...");
            user_job.Progress = 0.0;
            
            try {
                source.IpodDevice.Name = source.Name;
                source.IpodDevice.TrackDatabase.Save ();
                try {
                    File.Delete (source.NamePath);
                } catch {
                }
            } catch (Exception e) {
                Log.Exception (e);
                Log.Error (Catalog.GetString ("Error rebuilding iPod database"), e.Message);
            }
        }
        
        protected virtual void OnFinished ()
        {
            ThreadAssist.ProxyToMain (delegate {
                EventHandler handler = Finished;
                if (handler != null) {
                    handler (this, EventArgs.Empty);
                }
            });
        }
    }
}
