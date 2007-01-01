/***************************************************************************
 *  DaapTrackInfo.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using System.IO;
using Mono.Unix;

using DAAP;
using Banshee.Base;

namespace Banshee.Plugins.Daap
{
    public sealed class DaapTrackInfo : TrackInfo
    {
        private DAAP.Track song;
        private DAAP.Database database;
        
        internal DaapTrackInfo(DAAP.Track song, DAAP.Database database) : this(song, database, true)
        {
        }
        
        internal DaapTrackInfo(DAAP.Track song, DAAP.Database database, bool sync)
        {
            this.song = song;
            this.database = database;
            CanSaveToDatabase = false;
            
            if(!sync) {
                return;
            }
            
            song.Updated += delegate(object o, EventArgs args) {
                LoadFromDaapTrack();
            };
            
            LoadFromDaapTrack();
        }
        
        private void LoadFromDaapTrack()
        {
            uri = new SafeUri(String.Format("{0}{1}/{2}", DaapCore.ProxyServer.HttpBaseAddress, 
                database.GetHashCode(), song.Id));
            
            album = song.Album == String.Empty ? null : song.Album;
            artist = song.Artist == String.Empty ? null : song.Artist;
            title = song.Title == String.Empty ? null : song.Title;
            genre = song.Genre == String.Empty ? null : song.Genre;
            
            track_id = song.Id;
            duration = song.Duration;
            
            date_added = song.DateAdded;
            track_count = (uint)song.TrackCount;
            track_number = (uint)song.TrackNumber;
            year = song.Year;
        }

        public Stream GetStream (out long length)
        {
            return database.StreamTrack (song, out length);
        }
        
        public override int GetHashCode()
        {
            return song.GetHashCode();
        }
        
        public override bool Equals(object o)
        {
            if(!(o is DaapTrackInfo)) {
                return false;
            }
            
            return (o as DaapTrackInfo).Track.Equals(song);
        }
        
        public override void IncrementPlayCount()
        {
            play_count++;
        }

        public DAAP.Track Track {
            get {
                return song;
            }
        }
    }
}
