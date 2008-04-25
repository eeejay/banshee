//
// IpodTrackInfo.cs
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
using Banshee.Collection.Database;

namespace Banshee.Dap.Ipod
{   
    public class IpodTrackInfo : DatabaseTrackInfo
    {
        private IPod.Track track;
        internal IPod.Track IpodTrack {
            get { return track; }
        }
        
        private int ipod_id;
        internal int IpodId {
            get { return ipod_id; }
        }
        
        public IpodTrackInfo (IPod.Track track) : base ()
        {
            this.track = track;
            LoadFromIpodTrack ();
            CanSaveToDatabase = true;
        }
        
        public IpodTrackInfo (TrackInfo track, IPod.TrackDatabase database)
        {
            if (track is IpodTrackInfo) {
                this.track = ((IpodTrackInfo)track).IpodTrack;
                LoadFromIpodTrack ();
            } else {
                Uri = track.Uri;
                AlbumTitle = track.AlbumTitle;
                ArtistName = track.ArtistName;
                TrackTitle = track.TrackTitle;
                Genre = track.Genre;
                Duration = track.Duration;
                Rating = track.Rating;
                PlayCount = track.PlayCount;
                LastPlayed = track.LastPlayed;
                DateAdded = track.DateAdded;
                TrackCount = track.TrackCount;
                TrackNumber = track.TrackNumber;
                Year = track.Year;
            }
            
            CanSaveToDatabase = true;
        }
        
        private void LoadFromIpodTrack ()
        {
            try {
                Uri = new SafeUri (track.Uri.LocalPath);
            } catch { 
                Uri = null;
            }

            ipod_id = (int)track.Id;
            Duration = track.Duration;
            PlayCount = track.PlayCount;

            AlbumTitle = String.IsNullOrEmpty (track.Album) ? null : track.Album;
            ArtistName = String.IsNullOrEmpty (track.Artist) ? null : track.Artist;
            TrackTitle = String.IsNullOrEmpty (track.Title) ? null : track.Title;
            Genre = String.IsNullOrEmpty (track.Genre) ? null : track.Genre;
            
            switch (track.Rating) {
                case IPod.TrackRating.One:   Rating = 1; break;
                case IPod.TrackRating.Two:   Rating = 2; break;
                case IPod.TrackRating.Three: Rating = 3; break;
                case IPod.TrackRating.Four:  Rating = 4; break;
                case IPod.TrackRating.Five:  Rating = 5; break;
                case IPod.TrackRating.Zero: 
                default:                     Rating = 0; break;
            }
            
            LastPlayed = track.LastPlayed;
            DateAdded = track.DateAdded;
            TrackCount = track.TotalTracks;
            TrackNumber = track.TrackNumber;
            Year = track.Year;

            if (track.IsProtected) {
                CanPlay = false;
                // FIXME: indicate the song is DRMed
            }
        }
    }
}
