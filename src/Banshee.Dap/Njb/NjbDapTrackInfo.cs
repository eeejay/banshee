
/***************************************************************************
 *  NjbDapTrackInfo.cs
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
using NJB=Njb;

using Banshee.Base;
using Banshee.Dap;

namespace Banshee.Dap.Njb
{
    public sealed class NjbDapTrackInfo : DapTrackInfo
    {
        private NJB.Song song;
        
        public NjbDapTrackInfo(NJB.Song song, DapDevice dap) : base()
        {
            this.song = song;
            LoadFromNjbSong(dap);
            CanPlay = false;
        }
        
        public NjbDapTrackInfo(TrackInfo track, DapDevice dap) : base()
        {
            if(track is NjbDapTrackInfo) {
                NjbDapTrackInfo njb_track = (NjbDapTrackInfo)track;
                this.song = njb_track.Song;
                LoadFromNjbSong(dap);
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
            
            CanPlay = false;
        }
        
        private void LoadFromNjbSong(DapDevice dap)
        {
            uri = new Uri(String.Format("dap://{0}/{1}", dap.Uid, song.Id));
            album = song.Album == String.Empty ? null : song.Album;
            artist = song.Artist == String.Empty ? null : song.Artist;
            title = song.Title == String.Empty ? null : song.Title;
            genre = song.Genre == String.Empty ? null : song.Genre;
            
            track_id = song.Id;
            duration = song.Duration;
            play_count = 0;
            rating = 0;
            
            track_count = 0;
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
        
        public NJB.Song Song
        {
            get {
                return song;
            }
        }
    }
}
