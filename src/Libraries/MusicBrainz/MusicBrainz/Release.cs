#region License

// Release.cs
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

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;

namespace MusicBrainz
{
    public sealed class Release : MusicBrainzItem
    {
        
        #region Private
        
        const string EXTENSION = "release";
        ReleaseType? type;
        ReleaseStatus? status;
        string language;
        string script;
        string asin;
        ReadOnlyCollection<Disc> discs;
        ReadOnlyCollection<Event> events;
        ReadOnlyCollection<Track> tracks;
        int? track_number;
        
        #endregion
        
        #region Constructors

        Release (string mbid) : base (mbid, null)
        {
        }

        Release (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal Release (XmlReader reader) : base (reader, null, false)
        {
        }
        
        #endregion
        
        #region Protected Overrides
        
        protected override string UrlExtension {
            get { return EXTENSION; }
        }
        
        static readonly string [] track_params = new string [] { "tracks", "track-level-rels", "artist" };
        
        protected override void CreateIncCore (StringBuilder builder)
        {
            AppendIncParameters (builder, "release-events", "labels");
            if (discs == null) AppendIncParameters (builder, "discs");
            if (tracks == null) {
                AppendIncParameters (builder, track_params);
                AllRelsLoaded = false;
            }
            base.CreateIncCore (builder);
        }

        protected override void LoadMissingDataCore ()
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
            base.LoadMissingDataCore (release);
        }

        protected override bool ProcessAttributes (XmlReader reader)
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

        protected override bool ProcessXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.ProcessXml (reader);
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
        
        #endregion

        #region Properties

        [Queryable ("reid")]
        public override string Id {
            get { return base.Id; }
        }

        [Queryable ("release")]
        public override string Title {
            get { return base.Title; }
        }

        [Queryable]
        public ReleaseType Type {
            get { return GetPropertyOrDefault (ref type, ReleaseType.None); }
        }

        [Queryable]
        public ReleaseStatus Status {
            get { return GetPropertyOrDefault (ref status, ReleaseStatus.None); }
        }

        public string Language {
            get { return GetPropertyOrNull (ref language); }
        }

        [Queryable]
        public string Script {
            get { return GetPropertyOrNull (ref script); }
        }

        [Queryable]
        public string Asin {
            get { return GetPropertyOrNull (ref asin); }
        }

        [QueryableMember("Count", "discids")]
        public ReadOnlyCollection<Disc> Discs {
            get { return GetPropertyOrNew (ref discs); }
        }

        public ReadOnlyCollection<Event> Events {
            get { return GetPropertyOrNew (ref events); }
        }

        [QueryableMember ("Count", "tracks")]
        public ReadOnlyCollection<Track> Tracks {
            get { return GetPropertyOrNew (ref tracks); }
        }

        internal int TrackNumber {
            get { return track_number ?? -1; }
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
        
        public static Query<Release> Query (Disc disc)
        {
            if (disc == null) throw new ArgumentNullException ("disc");
            
            ReleaseQueryParameters parameters = new ReleaseQueryParameters ();
            parameters.DiscId = disc.Id;
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
            parameters.DiscId = LocalDisc.GetFromDevice (device).Id;
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
    
    #region Ancillary Types
    
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

    public sealed class ReleaseQueryParameters : ItemQueryParameters
    {
        string disc_id;
        public string DiscId {
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
    
    #endregion
    
}
