/***************************************************************************
 *  AudioCdRipper.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;

using Banshee.Widgets;
using Banshee.Base;

namespace Banshee.Base
{
    public delegate void AudioCdRipperProgressHandler(object o, AudioCdRipperProgressArgs args);
    public delegate void AudioCdRipperTrackFinishedHandler(object o, AudioCdRipperTrackFinishedArgs args);
        
    public class AudioCdRipperProgressArgs : EventArgs
    {
        public int SecondsEncoded;
        public int TotalSeconds;
        public AudioCdTrackInfo Track;
    }

    public class AudioCdRipperTrackFinishedArgs : EventArgs
    {
        public AudioCdTrackInfo Track;
        public int TrackNumber;
        public Uri Uri;
    }

    public class AudioCdTrackRipper : IDisposable
    {
        private delegate void CdRipProgressCallback(IntPtr ripper, int seconds, IntPtr user_info);
        
        private HandleRef handle;
        private CdRipProgressCallback progress_callback;
        private AudioCdTrackInfo current_track;
        
        public event AudioCdRipperProgressHandler Progress;
        public event AudioCdRipperTrackFinishedHandler TrackFinished;
        
        public AudioCdTrackRipper(string device, int paranoiaMode, string encoderPipeline)
        {
            IntPtr ptr = gst_cd_ripper_new(device, paranoiaMode, encoderPipeline);
            if(ptr == IntPtr.Zero) {
                throw new ApplicationException(Catalog.GetString("Could not create CD Ripper"));
            }
            
            handle = new HandleRef(this, ptr);
            
            progress_callback = new CdRipProgressCallback(OnProgress);
            gst_cd_ripper_set_progress_callback(handle, progress_callback);
        }
        
        public void Dispose()
        {
            gst_cd_ripper_free(handle);
        }
        
        public void RipTrack(AudioCdTrackInfo track, int trackNumber, Uri outputUri)
        {
            ThreadAssist.Spawn(delegate {
                RipTrack_08(track, trackNumber, outputUri);
            });
        }
        
        private void RipTrack_08(AudioCdTrackInfo track, int trackNumber, Uri outputUri)
        {
            current_track = track;
            
            bool result = gst_cd_ripper_rip_track(handle, outputUri.AbsoluteUri, trackNumber, 
                track.Artist, track.Album, track.Title, track.Genre, 
                (int)track.TrackNumber, (int)track.TrackCount, IntPtr.Zero);
                
            if(result) {
                ThreadAssist.ProxyToMain(delegate {
                    OnTrackFinished(track, trackNumber, outputUri);
                });
            }
            
            track = null;
        }
        
        private void OnTrackFinished(AudioCdTrackInfo track, int trackNumber, Uri outputUri)
        {
            AudioCdRipperTrackFinishedHandler handler = TrackFinished;
            if(handler != null) {
                AudioCdRipperTrackFinishedArgs args = new AudioCdRipperTrackFinishedArgs();
                args.Track = track;
                args.TrackNumber = trackNumber;
                args.Uri = outputUri;
                handler(this, args);
            }
        }
        
        public void Cancel()
        {
            gst_cd_ripper_cancel(handle);
        }
        
        public string Error {
            get {
                IntPtr errPtr = gst_cd_ripper_get_error(handle);
                if(errPtr == IntPtr.Zero)
                    return null;
                
                return GLib.Marshaller.Utf8PtrToString(errPtr);
            }
        }
        
        private void OnProgress(IntPtr ripper, int seconds, IntPtr user_info)
        {
            AudioCdRipperProgressHandler handler = Progress;
            if(handler == null)
                return;
                
            AudioCdRipperProgressArgs args = new AudioCdRipperProgressArgs();
            args.TotalSeconds = (int)current_track.Duration.TotalSeconds;
            args.SecondsEncoded = seconds;
            args.Track = current_track;
            
            handler(this, args); 
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr gst_cd_ripper_new(string device,
            int paranoia_mode, string encoder_pipeline);
            
        [DllImport("libbanshee")]
        private static extern void gst_cd_ripper_free(HandleRef ripper);
        
        [DllImport("libbanshee")]
        private static extern bool gst_cd_ripper_rip_track(HandleRef ripper, 
            string uri, int track_number, string md_artist, string md_album, 
            string md_title, string md_genre, int md_track_number, 
            int md_track_count, IntPtr user_info);
            
        [DllImport("libbanshee")]
        private static extern void gst_cd_ripper_set_progress_callback(
            HandleRef ripper, CdRipProgressCallback cb);
            
        [DllImport("libbanshee")]
        private static extern void gst_cd_ripper_cancel(HandleRef ripper);
        
        [DllImport("libbanshee")]
        private static extern IntPtr gst_cd_ripper_get_error(HandleRef ripper);
    }

    public class AudioCdRipper
    {
        private Queue tracks = new Queue();
        private AudioCdTrackRipper ripper;
        private string device;
        private int currentIndex = 0;
        private int overallProgress = 0;
        private string status;
        private int total_tracks;
        
        // speed calculations
        private int currentSeconds = 0;
        private int lastPollSeconds = 0;
        private uint pollDelay = 1000;
        private long totalCount;
        
        private uint timeout_id; 
        private int current = 1;
        private PipelineProfile profile;
        
        public event HaveTrackInfoHandler HaveTrackInfo;
        
        private ActiveUserEvent user_event;
        
        public AudioCdRipper()
        {
            user_event = new ActiveUserEvent("Importing CD");
            user_event.Icon = IconThemeUtils.LoadIcon("cd-action-rip-24", 22);
            user_event.CancelRequested += OnCancelRequested;
        }

        public void QueueTrack(AudioCdTrackInfo track)
        {
            if(device == null) {
                device = track.Device;
            } else if(device != track.Device) {
                throw new ApplicationException(String.Format(Catalog.GetString(
                    "The device node '{0}' differs from the device node " + 
                    "already set for previously queued tracks ({1})"),
                    track.Device, device));
            }
            
            tracks.Enqueue(track);
            totalCount += (int)track.Duration.TotalSeconds;
        }
        
        public void Start()
        {
            current = 1;
            
            user_event.Header = Catalog.GetString("Importing Audio CD");
            user_event.Message = Catalog.GetString("Initializing Drive");
        
            profile = PipelineProfile.GetConfiguredProfile("Ripping");
            
            try {
                string encodePipeline = profile.Pipeline;
        
                LogCore.Instance.PushDebug("Ripping CD and Encoding with Pipeline", encodePipeline);
            
                ripper = new AudioCdTrackRipper(device, 0, encodePipeline);
                ripper.Progress += OnRipperProgress;
                ripper.TrackFinished += OnTrackRipped;
                
                timeout_id = GLib.Timeout.Add(pollDelay, OnTimeout);
                
                total_tracks = tracks.Count;
                
                RipNextTrack();
            } catch(PipelineProfileException e) {
                LogCore.Instance.PushError("Cannot Import CD", e.Message);
            }
        }
        
        private void RipNextTrack()
        {
            if(tracks.Count <= 0) {
                return;
            }
            
            AudioCdTrackInfo track = tracks.Dequeue() as AudioCdTrackInfo;

            user_event.Header = String.Format(Catalog.GetString("Importing {0} of {1}"), current++, QueueSize);
            status = String.Format("{0} - {1}", track.Artist, track.Title);
            user_event.Message = status;

            Uri uri = PathUtil.PathToFileUri(FileNamePattern.BuildFull(track, profile.Extension));

            ripper.RipTrack(track, track.TrackIndex, uri);
        }
        
        private void OnTrackRipped(object o, AudioCdRipperTrackFinishedArgs args)
        {
            overallProgress += (int)args.Track.Duration.TotalSeconds;

            if(!user_event.IsCancelRequested) {
                TrackInfo lti;
                try {
                    lti = new LibraryTrackInfo(args.Uri, args.Track);
                } catch(ApplicationException) {
                    lti = Globals.Library.TracksFnKeyed[Library.MakeFilenameKey(args.Uri)] as TrackInfo;
                }
            
                if(lti != null) {                       
                    HaveTrackInfoHandler handler = HaveTrackInfo;
                    if(handler != null) {
                        HaveTrackInfoArgs hargs = new HaveTrackInfoArgs();
                        hargs.TrackInfo = lti;
                        handler(this, hargs);
                    }
                }
            }

            currentIndex++;
            
            if(tracks.Count > 0 && !user_event.IsCancelRequested) {
                RipNextTrack();
                return;
            }
            
            if(timeout_id > 0) {
                GLib.Source.Remove(timeout_id);
            }
                
            ripper.Dispose();
            user_event.Dispose();
        }
        
        private bool OnTimeout()
        {
            int diff = currentSeconds - lastPollSeconds;  
            lastPollSeconds = currentSeconds;
            
            if(diff <= 0) {
                user_event.Message = status;
                return true;
            }
            
            user_event.Message = status + String.Format(" ({0}x)", diff);
            return true;
        }
        
        private void OnRipperProgress(object o, AudioCdRipperProgressArgs args)
        {
            if(args.SecondsEncoded == 0) {
                return;
            }
            
            user_event.Progress = (double)(args.SecondsEncoded + overallProgress) / (double)(totalCount);
            currentSeconds = args.SecondsEncoded;
        }
        
        private void OnCancelRequested(object o, EventArgs args)
        {
            if(ripper != null) {
                ripper.Cancel();
            }
        }
        
        public int QueueSize {
            get {
                return total_tracks;
            }
        }
    }
}
