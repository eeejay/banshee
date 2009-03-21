//
// LastfmTrackInfo.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Net;
using System.Web;

using Gdk;

using Banshee.Base;
using Banshee.Collection;
using Media.Playlists.Xspf;

namespace Banshee.Lastfm.Radio
{
    public class LastfmTrackInfo : TrackInfo
    {
        private StationSource station;
        private Track track;
        private bool loved, hated;
        private string trackauth;

        public Track XspfTrack {
            get { return track; }
            set { track = value; }
        }

        public bool Loved {
            get { return loved; }
        }

        public bool Hated {
            get { return hated; }
        }
        
        public string TrackAuth {
            get { return trackauth; }
        }

        public LastfmTrackInfo (Track track, StationSource station, string trackauth)
        {
            this.station = station;
            this.trackauth = trackauth;
            Uri = new SafeUri (track.Locations[0]);
            ArtistName = track.Creator;
            TrackTitle = track.Title;
            AlbumTitle = track.Album;
            Duration = track.Duration;
            TrackNumber = (int) track.TrackNumber;
            XspfTrack = track;

            MediaAttributes = TrackMediaAttributes.AudioStream | TrackMediaAttributes.Music;

            CanSaveToDatabase = false;
        }

        public override void OnPlaybackFinished (double percentCompleted)
        {
            base.OnPlaybackFinished (percentCompleted);

            station.PlayCount++;
            station.Save ();
        }

        public void Love ()
        {
            loved = true; hated = false;
            ThreadAssist.Spawn (delegate {
                try {
                    station.LastfmSource.Connection.Love (ArtistName, TrackTitle);
                } catch (System.Net.WebException e) {
                    Hyena.Log.Warning ("Got Exception Trying to Love Song", e.ToString (), false);
                }
            });
        }

        public void Ban ()
        {
            loved = false; hated = true;
            ThreadAssist.Spawn (delegate {
                try {
                    station.LastfmSource.Connection.Ban (ArtistName, TrackTitle);
                } catch (System.Net.WebException e) {
                    Hyena.Log.Warning ("Got Exception Trying to Ban Song", e.ToString (), false);
                }
            });
        }
    }
}
