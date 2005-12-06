/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  IpodDapTrackInfo.cs
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
using IPod;

using Banshee.Base;
using Banshee.Dap;

namespace Banshee.Dap.Ipod
{
    public sealed class IpodDapTrackInfo : DapTrackInfo
    {
        private Song song;
        
        public IpodDapTrackInfo(Song song)
        {
            this.song = song;
            LoadFromIpodSong();
            CanSaveToDatabase = false;
        }
        
        public IpodDapTrackInfo(TrackInfo track, SongDatabase database)
        {
            if(track is IpodDapTrackInfo) {
                IpodDapTrackInfo ipod_track = (IpodDapTrackInfo)track;
                this.song = ipod_track.Song;
                LoadFromIpodSong();
            } else {
                uri = track.Uri;
                album = track.Album;
                artist = track.Artist;
                title = track.Title;
                genre = track.Genre;
                track_id = track.TrackId;
                duration = track.Duration;
                rating = track.Rating;
                play_count = track.PlayCount;
                last_played = track.LastPlayed;
                date_added = track.DateAdded;
                track_count = track.TrackCount;
                track_number = track.TrackNumber;
                year = track.Year;
            }
            
            CanSaveToDatabase = false;
        }
        
        private void LoadFromIpodSong()
        {
            try {
                uri = song.Uri;
            } catch(Exception) { 
                uri = null;
            }

            album = song.Album == String.Empty ? null : song.Album;
            artist = song.Artist == String.Empty ? null : song.Artist;
            title = song.Title == String.Empty ? null : song.Title;
            genre = song.Genre == String.Empty ? null : song.Genre;
            
            track_id = song.Id;
            duration = song.Duration;
            play_count = (uint)song.PlayCount;

            switch(song.Rating) {
                case SongRating.One:   rating = 1; break;
                case SongRating.Two:   rating = 2; break;
                case SongRating.Three: rating = 3; break;
                case SongRating.Four:  rating = 4; break;
                case SongRating.Five:  rating = 5; break;
                case SongRating.Zero: 
                default: 
                    rating = 0; 
                    break;
            }
            
            last_played = song.LastPlayed;
            date_added = song.DateAdded;
            track_count = (uint)song.TotalTracks;
            track_number = (uint)song.TrackNumber;
            year = song.Year;
            can_play = !song.IsProtected;
        }
        
        public override void Save()
        {
            
        }
        
        public override void IncrementPlayCount()
        {
            play_count++;
            Save();
        }
        
        protected override void SaveRating()
        {
            Save();
        }
        
        public IPod.Song Song
        {
            get {
                return song;
            }
        }
    }
}
