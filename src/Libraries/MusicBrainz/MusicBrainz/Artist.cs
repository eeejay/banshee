/***************************************************************************
 *  Artist.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

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

        ArtistReleaseType (Enum enumeration, bool various)
        {
            str = various
                ? "va-" + Utils.EnumToString (enumeration)
                : "sa-" + Utils.EnumToString (enumeration);
        }

        public override string ToString ()
        {
            return str;
        }

    }

    public sealed class Artist : MusicBrainzEntity
    {
        const string EXTENSION = "artist";
        
        protected override string UrlExtension {
            get { return EXTENSION; }
        }

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

        protected override void HandleCreateInc (StringBuilder builder)
        {
            AppendIncParameters (builder, artist_release_type.ToString ());
            base.HandleCreateInc (builder);
        }

        protected override void HandleLoadMissingData ()
        {
            Artist artist = new Artist (Id, CreateInc ());
            type = artist.Type;
            base.HandleLoadMissingData (artist);
        }

        protected override bool HandleAttributes (XmlReader reader)
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

        protected override bool HandleXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.HandleXml (reader);
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

        #region Properties

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
