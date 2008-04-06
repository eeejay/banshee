// Artist.cs
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;

namespace MusicBrainz
{
    public enum ArtistType
    {
        Unknown,
        Group,
        Person
    }

    public sealed class ArtistReleaseType
    {
        string str;

        public ArtistReleaseType (ReleaseType type, bool various) : this ((Enum)type, various)
        {
        }

        public ArtistReleaseType (ReleaseStatus status, bool various) : this ((Enum)status, various)
        {
        }
        
        public ArtistReleaseType (ReleaseType type, ReleaseStatus status, bool various)
        {
            StringBuilder builder = new StringBuilder ();
            Format (builder, type, various);
            builder.Append ('+');
            Format (builder, status, various);
            str = builder.ToString ();
        }

        ArtistReleaseType (Enum enumeration, bool various)
        {
            StringBuilder builder = new StringBuilder ();
            Format (builder, enumeration, various);
            str = builder.ToString ();
        }
        
        void Format (StringBuilder builder, Enum enumeration, bool various)
        {
            builder.Append (various ? "va-" : "sa-");
            Utils.EnumToString (builder, enumeration.ToString ());
        }

        public override string ToString ()
        {
            return str;
        }

    }

    public sealed class Artist : MusicBrainzEntity
    {
        const string EXTENSION = "artist";
        
        #region Constructors

        Artist (string mbid) : base (mbid, null)
        {
        }

        Artist (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        Artist (string mbid, ArtistReleaseType artist_release_type)
            : this (mbid, "&inc=" + artist_release_type.ToString ())
        {
            have_all_releases = true;
            this.artist_release_type = artist_release_type;
        }

        internal Artist (XmlReader reader) : base (reader, false)
        {
        }
        
        #endregion
        
        #region Protected Overrides
        
        protected override string UrlExtension {
            get { return EXTENSION; }
        }

        protected override void CreateIncCore (StringBuilder builder)
        {
            AppendIncParameters (builder, artist_release_type.ToString ());
            base.CreateIncCore (builder);
        }

        protected override void LoadMissingDataCore ()
        {
            Artist artist = new Artist (Id, CreateInc ());
            type = artist.Type;
            base.LoadMissingDataCore (artist);
        }

        protected override bool ProcessAttributes (XmlReader reader)
        {
            switch (reader ["type"]) {
            case "Group":
                type = ArtistType.Group;
                break;
            case "Person":
                type = ArtistType.Person;
                break;
            }
            return type != ArtistType.Unknown;
        }

        protected override bool ProcessXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.ProcessXml (reader);
            if (!result) {
                result = true;
                switch (reader.Name) {
                case "release-list":
                    if (reader.ReadToDescendant ("release")) {
                        List<Release> releases = new List<Release> ();
                        do releases.Add (new Release (reader.ReadSubtree ()));
                        while (reader.ReadToNextSibling ("release"));
                        this.releases = releases.AsReadOnly ();
                    }
                    break;
                default:
                    reader.Skip (); // FIXME this is a workaround for Mono bug 334752
                    result = false;
                    break;
                }
            }
            reader.Close ();
            return result;
        }
        
        #endregion

        #region Properties
        
        public static ArtistReleaseType DefaultArtistReleaseType =
            new ArtistReleaseType (ReleaseStatus.Official, false);
        
        ArtistReleaseType artist_release_type = DefaultArtistReleaseType;
        
        public ArtistReleaseType ArtistReleaseType {
            get { return artist_release_type; }
            set {
                artist_release_type = value;
                releases = null;
                have_all_releases = false;
            }
        }

        [Queryable ("arid")]
        public override string Id {
            get { return base.Id; }
        }

        [Queryable ("artist")]
        public override string Name {
            get { return base.Name; }
        }

        ArtistType? type;
        [Queryable ("artype")]
        public ArtistType Type {
            get { return GetPropertyOrDefault (ref type, ArtistType.Unknown); }
        }

        ReadOnlyCollection<Release> releases;
        bool have_all_releases;
        public ReadOnlyCollection<Release> Releases {
            get {
                return releases ?? (have_all_releases
                    ? releases = new ReadOnlyCollection<Release> (new Release [0])
                    : new Artist (Id, artist_release_type).Releases);
            }
        }

        #endregion
        
        #region Static

        public static Artist Get (string mbid)
        {
            if (mbid == null) throw new ArgumentNullException ("mbid");
            return new Artist (mbid);
        }

        public static Query<Artist> Query (string name)
        {
            if (name == null) throw new ArgumentNullException ("name");
            return new Query<Artist> (EXTENSION, QueryLimit, CreateNameParameter (name));
        }

        public static Query<Artist> QueryLucene (string luceneQuery)
        {
            if (luceneQuery == null) throw new ArgumentNullException ("luceneQuery");
            return new Query<Artist> (EXTENSION, QueryLimit, CreateLuceneParameter (luceneQuery));
        }

        public static implicit operator string (Artist artist)
        {
            return artist.ToString ();
        }
        
        #endregion
        
    }
}
