/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  RipTransaction.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using Mono.Unix;

namespace Banshee
{
    public delegate void CdRipProgressCallback(IntPtr ripper, int seconds,
        IntPtr user_info);
        
    public delegate void AudioCdRipperProgressHandler(object o, 
        AudioCdRipperProgressArgs args);
        
    public class AudioCdRipperProgressArgs : EventArgs
    {
        public int SecondsEncoded;
        public int TotalSeconds;
        public AudioCdTrackInfo Track;
    }

    public class AudioCdRipper : IDisposable
    {
        private HandleRef handle;
        private CdRipProgressCallback progressCallback;
        private AudioCdTrackInfo currentTrack;
        
        public event AudioCdRipperProgressHandler Progress;

        public AudioCdRipper(string device, int paranoia_mode, 
            string encoder_pipeline)
        {
            IntPtr ptr = cd_rip_new(device, paranoia_mode, encoder_pipeline);
            if(ptr == IntPtr.Zero)
                throw new ApplicationException(Catalog.GetString(
                    "Could not create CD Ripper"));
            
            handle = new HandleRef(this, ptr);
            
            progressCallback = new CdRipProgressCallback(OnProgress);
            cd_rip_set_progress_callback(handle, progressCallback);
        }
        
        public void Dispose()
        {
            cd_rip_free(handle);
        }
        
        public bool RipTrack(AudioCdTrackInfo track, int track_number, 
            string outputUri)
        {
            currentTrack = track;
            bool result = cd_rip_rip_track(handle, outputUri, track_number, 
                track.Artist, track.Album, track.Title, track.Genre, 
                (int)track.TrackNumber, (int)track.TrackCount, IntPtr.Zero);
            track = null;
            return result;
        }
        
        public void Cancel()
        {
            cd_rip_cancel(handle);
        }
        
        public string Error
        {
            get {
                IntPtr errPtr = cd_rip_get_error(handle);
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
            args.TotalSeconds = (int)currentTrack.Duration;
            args.SecondsEncoded = seconds;
            args.Track = currentTrack;
            
            handler(this, args); 
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_rip_new(string device,
            int paranoia_mode, string encoder_pipeline);
            
        [DllImport("libbanshee")]
        private static extern void cd_rip_free(HandleRef ripper);
        
        [DllImport("libbanshee")]
        private static extern bool cd_rip_rip_track(HandleRef ripper, 
            string uri, int track_number, string md_artist, string md_album, 
            string md_title, string md_genre, int md_track_number, 
            int md_track_count, IntPtr user_info);
            
        [DllImport("libbanshee")]
        private static extern void cd_rip_set_progress_callback(
            HandleRef ripper, CdRipProgressCallback cb);
            
        [DllImport("libbanshee")]
        private static extern void cd_rip_cancel(HandleRef ripper);
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_rip_get_error(HandleRef ripper);
    }

    public class RipTransaction : LibraryTransaction
    {
        private ArrayList tracks = new ArrayList();
        private AudioCdRipper ripper;
        private string device;
        private int currentIndex = 0;

        public override string Name {
            get {
                return "CD Ripping Transaction";
            }
        }
        
        public RipTransaction()
        {
            showCount = false;
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
            
            tracks.Add(track);
            totalCount += track.Duration;
        }
        
        public override void Run()
        {
            int current = 1;
            statusMessage = Catalog.GetString("Initializing CD Drive");
        
            ripper = new AudioCdRipper(device, 0, 
                "xingenc bitrate=192");
            ripper.Progress += OnRipperProgress;
            
            foreach(AudioCdTrackInfo track in tracks) {
                if(cancelRequested)
                    break;
         
                statusMessage = String.Format(Catalog.GetString(
                    "Ripping {0} of {1} : {2} - {3}"), current++, QueueSize,
                    track.Artist, track.Title);
                    
                string filename = "file://" + 
                    FileNamePattern.BuildFull(track, "mp3");
                    
                ripper.RipTrack(track, track.TrackIndex + 1, filename);
                    
              /*  if(!ripper.RipTrack(track, track.TrackIndex + 1, filename)) {
                    Console.WriteLine("** Error: " + ripper.Error);
                    break;
                }*/
                
                FileLoadTransaction flt = new FileLoadTransaction(filename);
                flt.Register();
                
                currentIndex++;
            }
                   
            ripper.Dispose();
        }
        
        private void OnRipperProgress(object o, AudioCdRipperProgressArgs args)
        {
            int count = 0;
            
            for(int i = 0; i < currentIndex; i++)
                count += (int)(tracks[i] as AudioCdTrackInfo).Duration;
            
            currentCount = args.SecondsEncoded + count;
           
            if(cancelRequested && ripper != null)
                ripper.Cancel();
        }
        
        public int QueueSize
        {
            get {
                return tracks.Count;
            }
        }
    }
}
