// MusicBrainzItem.cs
//
// Copyright (c) 2008 Scott Peterson <lunchtimemama@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Text;
using System.Xml;

namespace MusicBrainz
{
    public abstract class ItemQueryParameters
    {
        internal ItemQueryParameters ()
        {
        }
        
        string title;
        public string Title {
            get { return title; }
            set { title = value; }
        }

        string artist;
        public string Artist {
            get { return artist; }
            set { artist = value; }
        }

        string artist_id;
        public string ArtistId {
            get { return artist_id; }
            set { artist_id = value; }
        }

        ReleaseType? release_type;
        public ReleaseType? ReleaseType {
            get { return release_type; }
            set { release_type = value; }
        }

        ReleaseStatus? release_status;
        public ReleaseStatus? ReleaseStatus {
            get { return release_status; }
            set { release_status = value; }
        }

        int? count;
        public int? TrackCount {
            get { return count; }
            set { count = value; }
        }

        protected void AppendBaseToBuilder (StringBuilder builder)
        {
            if (title != null) {
                builder.Append ("&title=");
                Utils.PercentEncode (builder, title);
            }
            if (artist != null) {
                builder.Append ("&artist=");
                Utils.PercentEncode (builder, artist);
            }
            if (artist_id != null) {
                builder.Append ("&artistid=");
                builder.Append (artist_id);
            }
            if (release_type != null) {
                builder.Append ("&releasetypes=");
                builder.Append (Utils.EnumToString (release_type.Value));
            }
            if (release_status != null) {
                builder.Append (release_type != null ? "+" : "&releasetypes=");
                builder.Append (release_status);
            }
            if (count != null) {
                builder.Append ("&count=");
                builder.Append (count.Value);
            }
        }
    }

    // The item-like product of an artist, such as a track or a release.
    public abstract class MusicBrainzItem : MusicBrainzObject
    {
        
        #region Constructors
        
        internal MusicBrainzItem (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal MusicBrainzItem (XmlReader reader, Artist artist, bool all_rels_loaded) : base (reader, all_rels_loaded)
        {
            if (this.artist == null) this.artist = artist;
        }
        
        #endregion
        
        #region Protected Overrides

        protected override void CreateIncCore (StringBuilder builder)
        {
            if (artist == null) AppendIncParameters(builder, "artist");
            base.CreateIncCore (builder);
        }

        protected void LoadMissingDataCore (MusicBrainzItem item)
        {
            title = item.Title;
            if (artist == null) artist = item.Artist;
            base.LoadMissingDataCore (item);
        }

        protected override bool ProcessXml (XmlReader reader)
        {
            bool result = true;
            switch (reader.Name) {
            case "title":
                reader.Read ();
                if (reader.NodeType == XmlNodeType.Text)
                    title = reader.ReadContentAsString ();
                break;
            case "artist":
                artist = new Artist (reader.ReadSubtree ());
                break;
            default:
                result = false;
                break;
            }
            return result;
        }
        
        #endregion

        #region Properties
        
        string title;
        public virtual string Title {
            get { return GetPropertyOrNull (ref title); }
        }

        Artist artist;
        [Queryable ("artist")]
        public virtual Artist Artist {
            get { return GetPropertyOrNull (ref artist); }
        }
        
        #endregion

        public override string ToString ()
        {
            return title;
        }
    }
}
