//
// AlbumInfo.cs
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
using System.Text.RegularExpressions;
using Mono.Unix;

using Hyena;
using Banshee.Base;

namespace Banshee.Collection
{
    public class AlbumInfo : CacheableItem
    {
        public static readonly string UnknownAlbumTitle = Catalog.GetString ("Unknown Album");
        
        private string title;
        private string title_sort;
        private string artist_name;
        private string artist_name_sort;
        private bool is_compilation;
        private string artwork_id;
        private DateTime release_date = DateTime.MinValue;
        private string musicbrainz_id;
        
        public AlbumInfo ()
        {
        }
        
        public AlbumInfo (string title)
        {
            this.title = title;
        }
        
        public virtual string ArtistName {
            get { return artist_name; }
            set { artist_name = value; }
        }
        
        public virtual string ArtistNameSort {
            get { return artist_name_sort; }
            set { artist_name_sort = String.IsNullOrEmpty (value) ? null : value; }
        }
        
        public virtual string Title {
            get { return title; }
            set { title = value; }
        }
        
        public virtual string TitleSort {
            get { return title_sort; }
            set { title_sort = String.IsNullOrEmpty (value) ? null : value; }
        }
        
        public virtual bool IsCompilation {
            get { return is_compilation; }
            set { is_compilation = value; }
        }
        
        public virtual string MusicBrainzId {
            get { return musicbrainz_id; }
            set { musicbrainz_id = value; }
        }
        
        public virtual DateTime ReleaseDate {
            get { return release_date; }
            set { release_date = value; }
        }
        
        public virtual string ArtworkId {
            get { 
                if (artwork_id == null) {
                    artwork_id = CoverArtSpec.CreateArtistAlbumId (ArtistName, Title);
                }
                
                return artwork_id;
            }
        }
        
        public string DisplayArtistName {
            get { return StringUtil.MaybeFallback (ArtistName, ArtistInfo.UnknownArtistName); }
        }
        
        public string DisplayTitle {
            get { return StringUtil.MaybeFallback (Title, UnknownAlbumTitle); }
        }
    }
}
