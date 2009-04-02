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
                AlbumArtist = track.AlbumArtist;
                AlbumTitle = track.AlbumTitle;
                ArtistName = track.ArtistName;
                BitRate = track.BitRate;
                Bpm = track.Bpm;
                Comment = track.Comment;
                Composer = track.Composer;
                Conductor = track.Conductor;
                Copyright = track.Copyright;
                DateAdded = track.DateAdded;
                DiscCount = track.DiscCount;
                DiscNumber = track.DiscNumber;
                Duration = track.Duration;
                FileSize = track.FileSize;
                Genre = track.Genre;
                Grouping = track.Grouping;
                IsCompilation = track.IsCompilation ;
                LastPlayed = track.LastPlayed;
                LastSkipped = track.LastSkipped;
                PlayCount = track.PlayCount;
                Rating = track.Rating;
                ReleaseDate = track.ReleaseDate;
                SkipCount = track.SkipCount;
                TrackCount = track.TrackCount;
                TrackNumber = track.TrackNumber;
                TrackTitle = track.TrackTitle;
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

            ExternalId = track.Id;
            ipod_id = (int)track.Id;
            
            AlbumArtist = track.AlbumArtist;
            AlbumTitle = String.IsNullOrEmpty (track.Album) ? null : track.Album;
            ArtistName = String.IsNullOrEmpty (track.Artist) ? null : track.Artist;
            BitRate = track.BitRate;
            Bpm = (int)track.BPM;
            Comment = track.Comment;
            Composer = track.Composer;
            DateAdded = track.DateAdded;
            DiscCount = track.TotalDiscs;
            DiscNumber = track.DiscNumber;
            Duration = track.Duration;
            FileSize = track.Size;
            Genre = String.IsNullOrEmpty (track.Genre) ? null : track.Genre;
            Grouping = track.Grouping;
            IsCompilation = track.IsCompilation;
            LastPlayed = track.LastPlayed;
            PlayCount = track.PlayCount;
            TrackCount = track.TotalTracks;
            TrackNumber = track.TrackNumber;
            TrackTitle = String.IsNullOrEmpty (track.Title) ? null : track.Title;
            //ReleaseDate = track.DateReleased;
            Year = track.Year;
            
            switch (track.Rating) {
                case IPod.TrackRating.One:   rating = 1; break;
                case IPod.TrackRating.Two:   rating = 2; break;
                case IPod.TrackRating.Three: rating = 3; break;
                case IPod.TrackRating.Four:  rating = 4; break;
                case IPod.TrackRating.Five:  rating = 5; break;
                case IPod.TrackRating.Zero: 
                default:                     rating = 0; break;
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
            track = track ?? device.TrackDatabase.CreateTrack ();
            ExternalId = track.Id;

            try {
                track.Uri = new Uri (Uri.AbsoluteUri);
            } catch (Exception e) {
                Log.Exception ("Failed to create System.Uri for iPod track", e);
                device.TrackDatabase.RemoveTrack (track);
            }
            
            track.AlbumArtist = AlbumArtist;
            track.BitRate = BitRate;
            track.BPM = (short)Bpm;
            track.Comment = Comment;
            track.Composer = Composer;
            track.DateAdded = DateAdded;
            track.TotalDiscs = DiscCount;
            track.DiscNumber = DiscNumber;
            track.Duration = Duration;
            track.Size = (int)FileSize;
            track.Grouping = Grouping;
            track.IsCompilation = IsCompilation;
            track.LastPlayed = LastPlayed;
            track.PlayCount = PlayCount;
            track.TotalTracks = TrackCount;
            track.TrackNumber = TrackNumber;
            track.Year = Year;
            //track.DateReleased = ReleaseDate;
            
            track.Album = AlbumTitle;
            track.Artist = ArtistName;
            track.Title = TrackTitle;
            track.Genre = Genre;
            
            switch (Rating) {
                case 1: track.Rating = IPod.TrackRating.Zero; break;
                case 2: track.Rating = IPod.TrackRating.Two; break;
                case 3: track.Rating = IPod.TrackRating.Three; break;
                case 4: track.Rating = IPod.TrackRating.Four; break;
                case 5: track.Rating = IPod.TrackRating.Five; break;
                default: track.Rating = IPod.TrackRating.Zero; break;
            }

            if (HasAttribute (TrackMediaAttributes.Podcast)) {
                //track.Description = ..
                //track.Category = ..
                //track.RememberPosition = true;
                //track.NotPlayedMark = track.PlayCount == 0;
            }

            if (HasAttribute (TrackMediaAttributes.VideoStream)) {
                if (HasAttribute (TrackMediaAttributes.Podcast)) {
                    track.Type = IPod.MediaType.VideoPodcast;
                } else if (HasAttribute (TrackMediaAttributes.Music)) {
                    track.Type = IPod.MediaType.MusicVideo;
                } else if (HasAttribute (TrackMediaAttributes.Movie)) {
                    track.Type = IPod.MediaType.Movie;
                } else if (HasAttribute (TrackMediaAttributes.TvShow)) {
                    track.Type = IPod.MediaType.TVShow;
                } else {
                    track.Type = IPod.MediaType.Video;
                }
            } else {
                if (HasAttribute (TrackMediaAttributes.Podcast)) {
                    track.Type = IPod.MediaType.Podcast;
                } else if (HasAttribute (TrackMediaAttributes.AudioBook)) {
                    track.Type = IPod.MediaType.Audiobook;
                } else if (HasAttribute (TrackMediaAttributes.Music)) {
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
        public static void SetIpodCoverArt (IPod.Device device, IPod.Track track, string path)
        {
            try {
                Gdk.Pixbuf pixbuf = null;
                foreach (IPod.ArtworkFormat format in device.LookupArtworkFormats (IPod.ArtworkUsage.Cover)) {
                    if (!track.HasCoverArt (format)) {
                        // Lazily load the pixbuf
                        if (pixbuf == null) {
                            pixbuf = new Gdk.Pixbuf (path);
                        }
                        
                        track.SetCoverArt (format, IPod.ArtworkHelpers.ToBytes (format, pixbuf));
                    }
                }
                
                if (pixbuf != null) {
                    pixbuf.Dispose ();
                }
            } catch (Exception e) {
                Log.Exception (String.Format ("Failed to set cover art on iPod from {0}", path), e);
            }
        }
    }
}
