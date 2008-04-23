//
// ITranscoder.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using Banshee.MediaProfiles;
using Banshee.Collection;

namespace Banshee.MediaEngine
{
    public delegate void TranscoderProgressHandler (object o, TranscoderProgressArgs args);
    public delegate void TranscoderTrackFinishedHandler (object o, TranscoderTrackFinishedArgs args);
    public delegate void TranscoderErrorHandler (object o, TranscoderErrorArgs args);
 
    public interface ITranscoder
    {
        event TranscoderProgressHandler Progress;
        event TranscoderTrackFinishedHandler TrackFinished;
        event TranscoderErrorHandler Error;
        
        void TranscodeTrack (TrackInfo track, SafeUri outputUri, ProfileConfiguration config);
        void Finish ();
        void Cancel ();
    }
                             
    public sealed class TranscoderProgressArgs : EventArgs
    {
        public TranscoderProgressArgs (TrackInfo track, double fraction, TimeSpan totalTime)
        {
            this.track = track;
            this.fraction = fraction;
            this.total_time = totalTime;
        }
        
        private double fraction;
        public double Fraction {
            get { return fraction; }
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

    public sealed class TranscoderTrackFinishedArgs : EventArgs
    {
        public TranscoderTrackFinishedArgs (TrackInfo track, SafeUri uri)
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
    
    public sealed class TranscoderErrorArgs : EventArgs
    {
        public TranscoderErrorArgs (TrackInfo track, string message)
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
