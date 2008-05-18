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
using Banshee.Streaming;
using Banshee.Collection;
using Banshee.Collection.Database;

using Hyena;

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
        
        public IpodTrackInfo (TrackInfo track)
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
                MediaAttributes = track.MediaAttributes;
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
            LastPlayed = track.LastPlayed;
            DateAdded = track.DateAdded;
            TrackCount = track.TotalTracks;
            TrackNumber = track.TrackNumber;
            Year = track.Year;
            
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
            
            if (track.IsProtected) {
                PlaybackError = StreamPlaybackError.Drm;
            }
            
            MediaAttributes = TrackMediaAttributes.AudioStream;
            
            switch (track.Type) {
                case IPod.MediaType.Audio:
                    MediaAttributes |= TrackMediaAttributes.Music;
                    break;
                case IPod.MediaType.AudioVideo:
                case IPod.MediaType.Video:
                    MediaAttributes |= TrackMediaAttributes.VideoStream;
                    break;
                case IPod.MediaType.MusicVideo:
                    MediaAttributes |= TrackMediaAttributes.Music | TrackMediaAttributes.VideoStream;
                    break;
                case IPod.MediaType.Movie:
                    MediaAttributes |= TrackMediaAttributes.VideoStream | TrackMediaAttributes.Movie;
                    break;
                case IPod.MediaType.TVShow:
                    MediaAttributes |= TrackMediaAttributes.VideoStream | TrackMediaAttributes.TvShow;
                    break;
                case IPod.MediaType.VideoPodcast:
                    MediaAttributes |= TrackMediaAttributes.VideoStream | TrackMediaAttributes.Podcast;
                    break;
                case IPod.MediaType.Podcast:
                    MediaAttributes |= TrackMediaAttributes.Podcast;
                    // FIXME: persist URL on the track (track.PodcastUrl)
                    break;
                case IPod.MediaType.Audiobook:
                    MediaAttributes |= TrackMediaAttributes.AudioBook;
                    break;
            }
        }
        
        public void CommitToIpod (IPod.Device device)
        {
            IPod.Track track = device.TrackDatabase.CreateTrack ();

            try {
                track.Uri = new Uri (Uri.AbsoluteUri);
            } catch (Exception e) {
                Log.Exception ("Failed to create System.Uri for iPod track", e);
                device.TrackDatabase.RemoveTrack (track);
            }
            
            track.Duration = Duration;
            track.PlayCount = PlayCount;
            track.LastPlayed = LastPlayed;
            track.DateAdded = DateAdded;
            track.TotalTracks = TrackCount;
            track.TrackNumber = TrackNumber;
            track.Year = Year;
            
            if (!String.IsNullOrEmpty (AlbumTitle)) {
                track.Album = AlbumTitle;
            }
            
            if (!String.IsNullOrEmpty (ArtistName)) {
                track.Artist = ArtistName;
            }
            
            if (!String.IsNullOrEmpty (TrackTitle)) {
                track.Title = TrackTitle;
            }
            
            if (!String.IsNullOrEmpty (Genre)) {
                track.Genre = Genre;
            }
            
            switch (Rating) {
                case 1: track.Rating = IPod.TrackRating.Zero; break;
                case 2: track.Rating = IPod.TrackRating.Two; break;
                case 3: track.Rating = IPod.TrackRating.Three; break;
                case 4: track.Rating = IPod.TrackRating.Four; break;
                case 5: track.Rating = IPod.TrackRating.Five; break;
                default: track.Rating = IPod.TrackRating.Zero; break;
            }
            
            if ((MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                if ((MediaAttributes & TrackMediaAttributes.Music) != 0) {
                    track.Type = IPod.MediaType.MusicVideo;
                } else if ((MediaAttributes & TrackMediaAttributes.Podcast) != 0) {
                    track.Type = IPod.MediaType.VideoPodcast;
                } else if ((MediaAttributes & TrackMediaAttributes.Movie) != 0) {
                    track.Type = IPod.MediaType.Movie;
                } else if ((MediaAttributes & TrackMediaAttributes.TvShow) != 0) {
                    track.Type = IPod.MediaType.TVShow;
                } else {
                    track.Type = IPod.MediaType.Video;
                }
            } else {
                if ((MediaAttributes & TrackMediaAttributes.Podcast) != 0) {
                    track.Type = IPod.MediaType.Podcast;
                } else if ((MediaAttributes & TrackMediaAttributes.AudioBook) != 0) {
                    track.Type = IPod.MediaType.Audiobook;
                } else if ((MediaAttributes & TrackMediaAttributes.Music) != 0) {
                    track.Type = IPod.MediaType.Audio;
                } else {
                    track.Type = IPod.MediaType.Audio;
                }
            }
            
            if (CoverArtSpec.CoverExists (ArtworkId)) {
                SetIpodCoverArt (device, track, CoverArtSpec.GetPath (ArtworkId));
            }
        }
        
        // FIXME: No reason for this to use GdkPixbuf - the file is on disk already in 
        // the artwork cache as a JPEG, so just shove the bytes from disk into the track
        
        private void SetIpodCoverArt (IPod.Device device, IPod.Track track, string path)
        {
            try {
                Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (path);
                if (pixbuf != null) {
                    SetIpodCoverArt (device, track, IPod.ArtworkUsage.Cover, pixbuf);
                    pixbuf.Dispose ();
                }
            } catch (Exception e) {
                Log.Exception (String.Format ("Failed to set cover art on iPod from {0}", path), e);
            }
        }

        private void SetIpodCoverArt (IPod.Device device, IPod.Track track, IPod.ArtworkUsage usage, Gdk.Pixbuf pixbuf)
        {
            foreach (IPod.ArtworkFormat format in device.LookupArtworkFormats (usage)) {
                if (!track.HasCoverArt (format)) {
                    track.SetCoverArt (format, IPod.ArtworkHelpers.ToBytes (format, pixbuf));
                }
            }
        }
    }
}
