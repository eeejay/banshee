//
// ArtistInfo.cs
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
using Mono.Unix;
using Hyena;

namespace Banshee.Collection
{
    public class ArtistInfo : CacheableItem
    {
        public static readonly string UnknownArtistName = Catalog.GetString ("Unknown Artist");

        private string name;
        private string name_sort;
        private string musicbrainz_id;

        public ArtistInfo ()
        {
        }

        public ArtistInfo(string name, string name_sort)
        {
            this.name = name;
            this.name_sort = name_sort;
        }

        public virtual string MusicBrainzId {
            get { return musicbrainz_id; }
            set { musicbrainz_id = value; }
        }

        public virtual string Name {
            get { return name; }
            set { name = value; }
        }

        public virtual string NameSort {
            get { return name_sort; }
            set { name_sort = String.IsNullOrEmpty (value) ? null : value; }
        }

        public string DisplayName {
            get { return StringUtil.MaybeFallback (Name, UnknownArtistName); }
        }
    }
}
