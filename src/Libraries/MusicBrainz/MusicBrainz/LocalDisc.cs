// LocalDisc.cs
//
// Copyright (c) 2008 Scott Peterson <lunchtimemama@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Security.Cryptography;
using System.Text;

namespace MusicBrainz
{
    public abstract class LocalDisc : Disc
    {
        byte first_track;
        byte last_track;
        int [] track_durations;
        int [] track_offsets = new int [100];
        
        internal LocalDisc()
        {
        }
        
        protected void Init ()
        {
            track_durations = new int [last_track];
            for (int i = 1; i <= last_track; i++) {
                track_durations [i - 1] = i < last_track
                    ? track_offsets [i + 1] - track_offsets [i]
                    : track_offsets [0] - track_offsets [i];
                track_durations [i - 1] /= 75; // 75 frames in a second
            }
            GenerateId ();
        }
        
        void GenerateId ()
        {
            StringBuilder input_builder = new StringBuilder (804);
            
            input_builder.Append (string.Format ("{0:X2}", FirstTrack));
            input_builder.Append (string.Format ("{0:X2}", LastTrack));
            
            for (int i = 0; i < track_offsets.Length; i++)
                input_builder.Append (string.Format ("{0:X8}", track_offsets [i]));

            // MB uses a slightly modified RFC822 for reasons of URL happiness.
            string base64 = Convert.ToBase64String (SHA1.Create ()
                .ComputeHash (Encoding.ASCII.GetBytes (input_builder.ToString ())));
            StringBuilder hash_builder = new StringBuilder (base64.Length);
            
            foreach (char c in base64)
                if      (c == '+')  hash_builder.Append ('.');
                else if (c == '/')  hash_builder.Append ('_');
                else if (c == '=')  hash_builder.Append ('-');
                else                hash_builder.Append (c);
            
            Id = hash_builder.ToString ();
        }
        
        protected byte FirstTrack {
            get { return first_track; }
            set { first_track = value; }
        }

        protected byte LastTrack {
            get { return last_track; }
            set { last_track = value; }
        }

        protected int [] TrackOffsets {
            get { return track_offsets; }
        }
        
        public int [] TrackDurations {
            get { return track_durations; }
        }
        
        string submission_url;
        public string SubmissionUrl {
            get {
                if (submission_url == null) {
                    StringBuilder builder = new StringBuilder ();
                    builder.Append ("http://mm.musicbrainz.org/bare/cdlookup.html");
                    builder.Append ("?id=");
                    builder.Append (Id);
                    builder.Append ("&tracks=");
                    builder.Append (last_track);
                    builder.Append ("&toc=");
                    builder.Append (first_track);
                    builder.Append ('+');
                    builder.Append (last_track);
                    builder.Append ('+');
                    builder.Append (track_offsets [0]);
                    for (int i = first_track; i <= last_track; i++) {
                        builder.Append ('+');
                        builder.Append (track_offsets [i]);
                    }
                    submission_url = builder.ToString ();
                }
                return submission_url;
            }
        }
        
        public static LocalDisc GetFromDevice (string device)
        {
            if (device == null) throw new ArgumentNullException ("device");
            return Environment.OSVersion.Platform != PlatformID.Unix
                ? (LocalDisc)new DiscWin32 (device) : new DiscLinux (device);
        }
    }
}
