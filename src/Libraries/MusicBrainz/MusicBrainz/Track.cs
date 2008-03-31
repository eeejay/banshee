/***************************************************************************
 *  Track.cs
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
    public sealed class TrackQueryParameters : ItemQueryParameters
    {
        string release;
        public string Release {
            get { return release; }
            set { release = value; }
        }

        string release_id;
        public string ReleaseId {
            get { return release_id; }
            set { release_id = value; }
        }

        uint? duration;
        public uint? Duration {
            get { return duration; }
            set { duration = value; }
        }

        int? track_number;
        public int? TrackNumber {
            get { return track_number; }
            set { track_number = value; }
        }

        string puid;
        public string Puid {
            get { return puid; }
            set { puid = value; }
        }

        public override string ToString ()
        {
            StringBuilder builder = new StringBuilder ();
            if (release != null) {
                builder.Append ("&release=");
                Utils.PercentEncode (builder, release);
            }
            if (release_id != null) {
                builder.Append ("&releaseid=");
                builder.Append (release_id);
            }
            if (duration != null) {
                builder.Append ("&duration=");
                builder.Append (duration.Value);
            }
            if (track_number != null) {
                builder.Append ("&tracknumber=");
                builder.Append (track_number.Value);
            }
            if (puid != null) {
                builder.Append ("&puid=");
                builder.Append (puid);
            }
            AppendBaseToBuilder (builder);
            return builder.ToString ();
        }
    }

    public sealed class Track : MusicBrainzItem
    {
        const string EXTENSION = "track";
        
        protected override string UrlExtension {
            get { return EXTENSION; }
        }

        Track (string mbid) : base (mbid, null)
        {
        }

        Track (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal Track (XmlReader reader) : base (reader, null, false)
        {
        }

        internal Track (XmlReader reader, Artist artist, bool all_rels_loaded) : base (reader, artist, all_rels_loaded)
        {
        }

        protected override void HandleCreateInc (StringBuilder builder)
        {
            if (releases == null) AppendIncParameters (builder, "releases");
            if (puids == null) AppendIncParameters (builder, "puids");
            base.HandleCreateInc (builder);
        }

        protected override void HandleLoadMissingData ()
        {
            Track track = new Track (Id, CreateInc ());
            duration = track.Duration;
            if (releases == null) releases = track.Releases;
            if (puids == null) puids = track.Puids;
            base.HandleLoadMissingData (track);
        }

        protected override bool HandleAttributes (XmlReader reader)
        {
            return true;
        }

        protected override bool HandleXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.HandleXml (reader);
            if (!result) {
                result = true;
                switch (reader.Name) {
                case "duration":
                    reader.Read ();
                    if (reader.NodeType == XmlNodeType.Text)
                        duration = uint.Parse (reader.ReadContentAsString ());
                    break;
                case "release-list":
                    if(reader.ReadToDescendant ("release")) {
                        List<Release> releases = new List<Release> ();
                        do releases.Add (new Release (reader.ReadSubtree ()));
                        while (reader.ReadToNextSibling ("release"));
                        this.releases = releases.AsReadOnly ();
                    }
                    break;
                case "puid-list":
                    if(reader.ReadToDescendant ("puid")) {
                        List<string> puids = new List<string> ();
                        do puids.Add (reader ["id"]);
                        while (reader.ReadToNextSibling ("puid"));
                        this.puids = puids.AsReadOnly ();
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

        [Queryable ("trid")]
        public override string Id {
            get { return base.Id; }
        }

        [Queryable ("track")]
        public override string Title {
            get { return base.Title; }
        }

        uint duration;
        [Queryable ("dur")]
        public uint Duration {
            get { return duration; }
        }

        ReadOnlyCollection<Release> releases;
        [QueryableMember ("Contains", "release")]
        public ReadOnlyCollection<Release> Releases {
            get { return GetPropertyOrNew (ref releases); }

        }

        ReadOnlyCollection<string> puids;
        public ReadOnlyCollection<string> Puids {
            get { return GetPropertyOrNew (ref puids); }
        }

        public int GetTrackNumber (Release release)
        {
            if (release == null) throw new ArgumentNullException ("release");
            
            foreach (Release r in Releases)
                if (r.Equals (release))
                    return r.TrackNumber;
            return -1;
        }

        #endregion
        
        #region Static

        public static Track Get (string mbid)
        {
            if (mbid == null) throw new ArgumentNullException ("mbid");
            return new Track (mbid);
        }

        public static Query<Track> Query (string title)
        {
            if (title == null) throw new ArgumentNullException ("title");
            
            TrackQueryParameters parameters = new TrackQueryParameters ();
            parameters.Title = title;
            return Query (parameters);
        }

        public static Query<Track> Query (string title, string release)
        {
            if (title == null) throw new ArgumentNullException ("title");
            if (release == null) throw new ArgumentNullException ("release");
            
            TrackQueryParameters parameters = new TrackQueryParameters ();
            parameters.Title = title;
            parameters.Release = release;
            return Query (parameters);
        }
        
        public static Query<Track> Query (string title, string release, string artist)
        {
            if (title == null) throw new ArgumentNullException ("title");
            if (release == null) throw new ArgumentNullException ("release");
            if (artist == null) throw new ArgumentNullException ("artist");
            
            TrackQueryParameters parameters = new TrackQueryParameters ();
            parameters.Title = title;
            parameters.Release = release;
            parameters.Artist = artist;
            return Query (parameters);
        }

        public static Query<Track> Query (TrackQueryParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException ("parameters");
            return new Query<Track> (EXTENSION, QueryLimit, parameters.ToString ());
        }

        public static Query<Track> QueryLucene (string luceneQuery)
        {
            if(luceneQuery == null) throw new ArgumentNullException ("luceneQuery"); 
            return new Query<Track> (EXTENSION, QueryLimit, CreateLuceneParameter (luceneQuery));
        }

        public static implicit operator string (Track track)
        {
            return track.ToString ();
        }
        
        #endregion

    }
}
