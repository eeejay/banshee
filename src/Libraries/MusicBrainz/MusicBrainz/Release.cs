/***************************************************************************
 *  Release.cs
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
    # region Enums

    public enum ReleaseType
    {
        None,
        Album,
        Single,
        EP,
        Compilation,
        Soundtrack,
        Spokenword,
        Interview,
        Audiobook,
        Live,
        Remix,
        Other
    }

    public enum ReleaseStatus
    {
        None,
        Official,
        Promotion,
        Bootleg,
        PsudoRelease
    }

    public enum ReleaseFormat
    {
        None,
        Cartridge,
        Cassette,
        CD,
        DAT,
        Digital,
        DualDisc,
        DVD,
        LaserDisc,
        MiniDisc,
        Other,
        ReelToReel,
        SACD,
        Vinyl
    }

    #endregion

    public sealed class ReleaseQueryParameters : ItemQueryParameters
    {
        string disc_id;
        public string DiscID {
            get { return disc_id; }
            set { disc_id = value; }
        }

        string date;
        public string Date {
            get { return date; }
            set { date = value; }
        }

        string asin;
        public string Asin {
            get { return asin; }
            set { asin = value; }
        }

        string language;
        public string Language {
            get { return language; }
            set { language = value; }
        }

        string script;
        public string Script {
            get { return script; }
            set { script = value; }
        }

        public override string ToString ()
        {
            StringBuilder builder = new StringBuilder ();
            if (disc_id != null) {
                builder.Append ("&discid=");
                builder.Append (disc_id);
            }
            if (date != null) {
                builder.Append ("&date=");
                Utils.PercentEncode (builder, date);
            }
            if (asin != null) {
                builder.Append ("&asin=");
                builder.Append (asin);
            }
            if (language != null) {
                builder.Append ("&lang=");
                builder.Append (language);
            }
            if (script != null) {
                builder.Append ("&script=");
                builder.Append (script);
            }
            AppendBaseToBuilder (builder);
            return builder.ToString ();
        }
    }

    public sealed class Release : MusicBrainzItem
    {
        const string EXTENSION = "release";
        
        protected override string UrlExtension {
            get { return EXTENSION; }
        }

        Release (string mbid) : base (mbid, null)
        {
        }

        Release (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal Release (XmlReader reader) : base (reader, null, false)
        {
        }
        
        static readonly string [] track_params = new string [] { "tracks", "track-level-rels", "artist" };
        
        protected override void HandleCreateInc (StringBuilder builder)
        {
            AppendIncParameters (builder, "release-events", "labels");
            if (discs == null) AppendIncParameters (builder, "discs");
            if (tracks == null) {
                AppendIncParameters (builder, track_params);
                AllRelsLoaded = false;
            }
            base.HandleCreateInc (builder);
        }

        protected override void HandleLoadMissingData ()
        {
            Release release = new Release (Id, CreateInc ());
            type = release.Type;
            status = release.Status;
            language = release.Language;
            script = release.Script;
            asin = release.Asin;
            events = release.Events;
            if (discs == null) discs = release.Discs;
            if (tracks == null) tracks = release.Tracks;
            base.HandleLoadMissingData (release);
        }

        protected override bool HandleAttributes (XmlReader reader)
        {
            // How sure am I about getting the type and status in the "Type Status" format?
            // MB really ought to specify these two things seperatly.
            string type_string = reader ["type"];
            if (type_string != null)
                foreach (string token in type_string.Split (' ')) {
                    if (type == null) {
                        type = Utils.StringToEnumOrNull<ReleaseType> (token);
                        if (type != null) continue;
                    }
                    this.status = Utils.StringToEnumOrNull<ReleaseStatus> (token);
                }
            return this.type != null || this.status != null;
        }

        protected override bool HandleXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.HandleXml (reader);
            if (!result) {
                result = true;
                switch (reader.Name) {
                case "text-representation":
                    language = reader ["language"];
                    script = reader ["script"];
                    break;
                case "asin":
                    reader.Read ();
                    if (reader.NodeType == XmlNodeType.Text)
                        asin = reader.ReadContentAsString ();
                    break;
                case "disc-list": {
                    if (reader.ReadToDescendant ("disc")) {
                        List<Disc> discs = new List<Disc> ();
                        do discs.Add (new Disc (reader.ReadSubtree ()));
                        while (reader.ReadToNextSibling ("disc"));
                        this.discs = discs.AsReadOnly ();
                    }
                    break;
                }
                case "release-event-list":
                    if (!AllDataLoaded) reader.Skip(); // FIXME this is a workaround for Mono bug 334752
                    if (reader.ReadToDescendant ("event")) {
                        List<Event> events = new List<Event> ();
                        do events.Add (new Event (reader.ReadSubtree ()));
                        while (reader.ReadToNextSibling ("event"));
                        this.events = events.AsReadOnly ();
                    }
                    break;
                case "track-list": {
                    string offset = reader ["offset"];
                    if (offset != null)
                        track_number = int.Parse (offset) + 1;
                    if (reader.ReadToDescendant ("track")) {
                        List<Track> tracks = new List<Track> ();
                        do tracks.Add (new Track (reader.ReadSubtree (), Artist, AllDataLoaded));
                        while (reader.ReadToNextSibling ("track"));
                        this.tracks = tracks.AsReadOnly ();
                    }
                    break;
                }
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

        [Queryable ("reid")]
        public override string Id {
            get { return base.Id; }
        }

        [Queryable ("release")]
        public override string Title {
            get { return base.Title; }
        }

        ReleaseType? type;
        [Queryable]
        public ReleaseType Type {
            get { return GetPropertyOrDefault (ref type, ReleaseType.None); }
        }

        ReleaseStatus? status;
        [Queryable]
        public ReleaseStatus Status {
            get { return GetPropertyOrDefault (ref status, ReleaseStatus.None); }

        }

        string language;
        public string Language {
            get { return GetPropertyOrNull (ref language); }
        }

        string script;
        [Queryable]
        public string Script {
            get { return GetPropertyOrNull (ref script); }
        }

        string asin;
        [Queryable]
        public string Asin {
            get { return GetPropertyOrNull (ref asin); }
        }

        ReadOnlyCollection<Disc> discs;
        [QueryableMember("Count", "discids")]
        public ReadOnlyCollection<Disc> Discs {
            get { return GetPropertyOrNew (ref discs); }
        }

        ReadOnlyCollection<Event> events;
        public ReadOnlyCollection<Event> Events {
            get { return GetPropertyOrNew (ref events); }
        }

        ReadOnlyCollection<Track> tracks;
        [QueryableMember ("Count", "tracks")]
        public ReadOnlyCollection<Track> Tracks {
            get { return GetPropertyOrNew (ref tracks); }
        }

        int? track_number;
        internal int TrackNumber {
            get { return track_number != null ? track_number.Value : -1; }
        }

        #endregion
        
        #region Static

        public static Release Get (string mbid)
        {
            if (mbid == null) throw new ArgumentNullException ("mbid");
            return new Release (mbid);
        }

        public static Query<Release> Query (string title)
        {
            if (title == null) throw new ArgumentNullException ("title");
            
            ReleaseQueryParameters parameters = new ReleaseQueryParameters ();
            parameters.Title = title;
            return Query (parameters);
        }

        public static Query<Release> Query (string title, string artist)
        {
            if (title == null) throw new ArgumentNullException ("title");
            if (artist == null) throw new ArgumentNullException ("artist");
            
            ReleaseQueryParameters parameters = new ReleaseQueryParameters ();
            parameters.Title = title;
            parameters.Artist = artist;
            return Query (parameters);
        }

        public static Query<Release> Query (ReleaseQueryParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException ("parameters");
            return new Query<Release> (EXTENSION, QueryLimit, parameters.ToString ());
        }

        public static Query<Release> QueryFromDevice(string device)
        {
            if (device == null) throw new ArgumentNullException ("device");
            
            ReleaseQueryParameters parameters = new ReleaseQueryParameters ();
            parameters.DiscID = LocalDisc.GetFromDevice (device).Id;
            return Query (parameters);
        }

        public static Query<Release> QueryLucene (string luceneQuery)
        {
            if (luceneQuery == null) throw new ArgumentNullException ("luceneQuery");
            return new Query<Release> (EXTENSION, QueryLimit, CreateLuceneParameter (luceneQuery));
        }

        public static implicit operator string (Release release)
        {
            return release.ToString ();
        }
        
        #endregion

    }
}
