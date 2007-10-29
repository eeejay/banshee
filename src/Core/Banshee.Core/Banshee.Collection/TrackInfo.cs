//
// TrackInfo.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;
using Mono.Unix;

using Hyena.Data;
using Banshee.Base;

namespace Banshee.Collection
{
    public class TrackInfo : ITrackInfo
    {
        private SafeUri uri;

        private string artist_name;
        private string album_title;
        private string track_title;
        private string genre;

        private int track_number;
        private int track_count;
        private int year;
        private int rating;

        private TimeSpan duration;
        private DateTime date_added;
        
        private TrackAttributes attributes;

        public TrackInfo()
        {
        }

        public override string ToString()
        {
            return String.Format("{0} - {1} (on {2}) <{3}> [{4}]", ArtistName, TrackTitle, 
                AlbumTitle, Duration, Uri.AbsoluteUri);
        }

        public virtual SafeUri Uri {
            get { return uri; }
            set { uri = value; }
        }

        [ListItemSetup(FieldIndex=1)]
        public virtual string ArtistName {
            get { return artist_name; }
            set { artist_name = value; }
        }

        [ListItemSetup(FieldIndex=2)]
        public virtual string AlbumTitle {
            get { return album_title; }
            set { album_title = value; }
        }

        [ListItemSetup(FieldIndex=3)]
        public virtual string TrackTitle {
            get { return track_title; }
            set { track_title = value; }
        }
        
        public string DisplayArtistName { 
            get { 
                return String.IsNullOrEmpty(ArtistName)
                    ? Catalog.GetString("Unknown Artist") 
                    : ArtistName; 
            } 
        }

        public string DisplayAlbumTitle { 
            get { 
                return String.IsNullOrEmpty(AlbumTitle) 
                    ? Catalog.GetString("Unknown Album") 
                    : AlbumTitle; 
            } 
        }

        public string DisplayTrackTitle { 
            get { 
                return String.IsNullOrEmpty(TrackTitle) 
                    ? Catalog.GetString("Unknown Title") 
                    : TrackTitle; 
            } 
        }        

        public virtual string Genre {
            get { return genre; }
            set { genre = value; }
        }

        [ListItemSetup(FieldIndex=0)]
        public virtual int TrackNumber {
            get { return track_number; }
            set { track_number = value; }
        }

        public virtual int TrackCount {
            get { return track_count; }
            set { track_count = value; }
        }

        public virtual int Year {
            get { return year; }
            set { year = value; }
        }

        public virtual int Rating {
            get { return rating; }
            set { rating = value; }
        }

        [ListItemSetup(FieldIndex=4)]
        public virtual TimeSpan Duration {
            get { return duration; }
            set { duration = value; }
        }
        
        public virtual DateTime DateAdded {
            get { return date_added; }
            set { date_added = value; }
        }
        
        public virtual TrackAttributes Attributes {
            get { return attributes; }
            protected set { attributes = value; }
        }
        
        // Generates a{sv} of self according to http://wiki.xmms2.xmms.se/index.php/Media_Player_Interfaces#.22Metadata.22
        public IDictionary<string, object> GenerateExportable()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            
            // Properties specified by the XMMS2 player spec
            dict.Add("URI", Uri == null ? String.Empty : Uri.AbsoluteUri);
            dict.Add("length", Duration.TotalSeconds);
            dict.Add("name", TrackTitle);
            dict.Add("artist", ArtistName);
            dict.Add("album", AlbumTitle);
            
            // Our own
            dict.Add("track-number", TrackNumber);
            dict.Add("track-count", TrackCount);
            dict.Add("year", year);
            dict.Add("rating", rating);
            
            return dict;
        }
    }
}
