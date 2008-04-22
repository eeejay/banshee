//
// IAudioCdRipper.cs
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

using Banshee.Base;
using Banshee.Collection;

namespace Banshee.MediaEngine
{
    public delegate void AudioCdRipperProgressHandler (object o, AudioCdRipperProgressArgs args);
    public delegate void AudioCdRipperTrackFinishedHandler (object o, AudioCdRipperTrackFinishedArgs args);
    public delegate void AudioCdRipperErrorHandler (object o, AudioCdRipperErrorArgs args);
 
    public interface IAudioCdRipper
    {
        event AudioCdRipperProgressHandler Progress;
        event AudioCdRipperTrackFinishedHandler TrackFinished;
        event AudioCdRipperErrorHandler Error;
        
        void Begin (string device, bool enableErrorCorrection);
        void Finish ();
        void Cancel ();
        
        void RipTrack (int trackIndex, TrackInfo track, SafeUri outputUri, out bool taggingSupported);
    }
                             
    public sealed class AudioCdRipperProgressArgs : EventArgs
    {
        public AudioCdRipperProgressArgs (TrackInfo track, TimeSpan encodedTime, TimeSpan totalTime)
        {
            this.track = track;
            this.encoded_time = encodedTime;
            this.total_time = totalTime;
        }
        
        private TimeSpan encoded_time;
        public TimeSpan EncodedTime {
            get { return encoded_time; }
        }

        private TimeSpan total_time;
        public TimeSpan TotalTime {
            get { return total_time; }
        }

        private TrackInfo track;
        public TrackInfo Track {
            get { return track; }
        }
    }

    public sealed class AudioCdRipperTrackFinishedArgs : EventArgs
    {
        public AudioCdRipperTrackFinishedArgs (TrackInfo track, SafeUri uri)
        {
            this.track = track;
            this.uri = uri;
        }

        private TrackInfo track;
        public TrackInfo Track {
            get { return track; }
        }
        
        private SafeUri uri;
        public SafeUri Uri {
            get { return uri; }
        }
    }
    
    public sealed class AudioCdRipperErrorArgs : EventArgs
    {
        public AudioCdRipperErrorArgs (TrackInfo track, string message)
        {
            this.track = track;
            this.message = message;
        }
        
        private TrackInfo track;
        public TrackInfo Track {
            get { return track; }
        }
        
        private string message;
        public string Message {
            get { return message; }
        }
    }
}
