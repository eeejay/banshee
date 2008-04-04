//
// AudioCdRipper.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Threading;
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.MediaProfiles;

namespace Banshee.GStreamer
{
    public class AudioCdRipper : IAudioCdRipper
    {
        private string encoder_pipeline;
    
        public event AudioCdRipperProgressHandler Progress;
        public event AudioCdRipperTrackFinishedHandler TrackFinished;
        public event AudioCdRipperErrorHandler Error;
        
        public void Begin ()
        {
            try {
                Profile profile = ServiceManager.MediaProfileManager.GetConfiguredActiveProfile ("cd-importing");
                if (profile != null) {
                    encoder_pipeline = profile.Pipeline.GetProcessById ("gstreamer");
                }
                
                if (String.IsNullOrEmpty (encoder_pipeline)) {
                    throw new ApplicationException ();
                }
                
                Hyena.Log.InformationFormat ("Ripping using encoder profile `{0}' with pipeline: {1}", 
                    profile.Name, encoder_pipeline);
            } catch (Exception e) {
                throw new ApplicationException (Catalog.GetString ("Could not find an encoder for ripping."), e);
            }
        }
        
        public void Finish ()
        {
        }
        
        public void Cancel ()
        {
            Finish ();
        }
        
        public void RipTrack (TrackInfo track, SafeUri outputUri)
        {
            ThreadPool.QueueUserWorkItem (delegate {
                DateTime start_time = DateTime.Now;    
                TimeSpan duration = TimeSpan.FromSeconds (5);
                
                while (true) {
                    TimeSpan ellapsed = DateTime.Now - start_time;
                    if (ellapsed >= duration) {
                        break;
                    }
                    
                    TimeSpan progress = TimeSpan.FromMilliseconds ((ellapsed.TotalMilliseconds 
                        / duration.TotalMilliseconds) * track.Duration.TotalMilliseconds);
                    
                    OnProgress (track, progress);
                    
                    Thread.Sleep (50);
                }
                
                OnTrackFinished (track, outputUri);
            });
            
            return;
        }
        
        protected virtual void OnProgress (TrackInfo track, TimeSpan ellapsedTime)
        {
            AudioCdRipperProgressHandler handler = Progress;
            if (handler != null) {
                handler (this, new AudioCdRipperProgressArgs (track, ellapsedTime, track.Duration));
            }
        }
        
        protected virtual void OnTrackFinished (TrackInfo track, SafeUri outputUri)
        {
            AudioCdRipperTrackFinishedHandler handler = TrackFinished;
            if (handler != null) {
                handler (this, new AudioCdRipperTrackFinishedArgs (track, outputUri));
            }
        }
        
        protected virtual void OnError (TrackInfo track, string message)
        {
            AudioCdRipperErrorHandler handler = Error;
            if (handler != null) {
                handler (this, new AudioCdRipperErrorArgs (track, message));
            }
        }
    }
}
