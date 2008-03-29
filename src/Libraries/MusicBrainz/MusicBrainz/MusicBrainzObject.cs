/***************************************************************************
 *  MusicBrainzObject.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace MusicBrainz
{
    internal delegate void XmlProcessingDelegate (XmlReader reader);

    public abstract class MusicBrainzObject
    {
        static TimeSpan min_interval = new TimeSpan (0, 0, 1); // 1 second
        static DateTime last_accessed;
        static readonly object server_mutex = new object ();

        bool all_data_loaded;
        protected bool AllDataLoaded {
            get { return all_data_loaded; }
        }

        bool all_rels_loaded;
        protected bool AllRelsLoaded {
            get { return all_rels_loaded; }
            set { all_rels_loaded = value; }
        }

        protected abstract string UrlExtension { get; }

        internal MusicBrainzObject (string mbid, string parameters)
        {
            all_data_loaded = true;
            CreateFromMbid (mbid, parameters ?? CreateInc ());
        }

        internal MusicBrainzObject (XmlReader reader, bool all_rels_loaded)
        {
            this.all_rels_loaded = all_rels_loaded;
            CreateFromXml (reader);
        }

        protected string CreateInc ()
        {
            StringBuilder builder = new StringBuilder ();
            HandleCreateInc (builder);
            return builder.ToString ();
        }
        
        static string [] rels_params = new string [] {
            "artist-rels",
            "release-rels",
            "track-rels",
            "label-rels",
            "url-rels"
        };

        protected virtual void HandleCreateInc (StringBuilder builder)
        {
            if (!all_rels_loaded)
                AppendIncParameters (builder, rels_params);
        }
        
        protected void AppendIncParameters (StringBuilder builder, string parameter)
        {
            builder.Append (builder.Length == 0 ? "&inc=" : "+");
            builder.Append (parameter);
        }
        
        protected void AppendIncParameters (StringBuilder builder, string parameter1, string parameter2)
        {
            builder.Append (builder.Length == 0 ? "&inc=" : "+");
            builder.Append (parameter1);
            builder.Append ('+');
            builder.Append (parameter2);
        }

        protected void AppendIncParameters (StringBuilder builder, string [] parameters)
        {
            foreach (string parameter in parameters)
                AppendIncParameters (builder, parameter);
        }

        void CreateFromMbid (string mbid, string parameters)
        {
            XmlProcessingClosure (
                CreateUrl (UrlExtension, mbid, parameters),
                delegate (XmlReader reader) {
                    reader.ReadToFollowing ("metadata");
                    reader.Read ();
                    CreateFromXml (reader.ReadSubtree ());
                    reader.Close ();
                }
            );
        }

        protected abstract bool HandleAttributes (XmlReader reader);
        protected abstract bool HandleXml (XmlReader reader);
        void CreateFromXml (XmlReader reader)
        {
            reader.Read ();
            id = reader ["id"];
            byte.TryParse (reader ["ext:score"], out score);
            HandleAttributes (reader);
            while (reader.Read () && reader.NodeType != XmlNodeType.EndElement) {
                if (reader.Name == "relation-list") {
                    all_rels_loaded = true;
                    switch (reader ["target-type"]) {
                    case "Artist":
                        List<Relation<Artist>> artist_rels = new List<Relation<Artist>> ();
                        CreateRelation (reader.ReadSubtree (), artist_rels);
                        this.artist_rels = artist_rels.AsReadOnly ();
                        break;
                    case "Release":
                        List<Relation<Release>> release_rels = new List<Relation<Release>> ();
                        CreateRelation (reader.ReadSubtree (), release_rels);
                        this.release_rels = release_rels.AsReadOnly ();
                        break;
                    case "Track":
                        List<Relation<Track>> track_rels = new List<Relation<Track>> ();
                        CreateRelation (reader.ReadSubtree (), track_rels);
                        this.track_rels = track_rels.AsReadOnly ();
                        break;
                    case "Label":
                        List<Relation<Label>> label_rels = new List<Relation<Label>> ();
                        CreateRelation (reader.ReadSubtree (), label_rels);
                        this.label_rels = label_rels.AsReadOnly ();
                        break;
                    case "Url":
                        if (!reader.ReadToDescendant ("relation")) break;
                        List<UrlRelation> url_rels = new List<UrlRelation> ();
                        do {
                            RelationDirection direction = RelationDirection.Forward;
                            string direction_string = reader ["direction"];
                            if (direction_string != null && direction_string == "backward")
                                direction = RelationDirection.Backward;
                            string attributes_string = reader ["attributes"];
                            string [] attributes = attributes_string == null
                                ? null : attributes_string.Split (' ');
                            url_rels.Add (new UrlRelation (
                                reader ["type"],
                                reader ["target"],
                                direction,
                                reader ["begin"],
                                reader ["end"],
                                attributes));
                        } while (reader.ReadToNextSibling ("relation"));
                        this.url_rels = url_rels.AsReadOnly ();
                        break;
                    }
                } else
                    HandleXml (reader.ReadSubtree ());
			}
            reader.Close ();
		}

        protected void LoadMissingData ()
        {
            if (!all_data_loaded) {
                HandleLoadMissingData ();
                all_data_loaded = true;
            }
        }

        protected abstract void HandleLoadMissingData ();
        protected void HandleLoadMissingData (MusicBrainzObject obj)
        {
            if (!all_rels_loaded) {
                artist_rels = obj.ArtistRelations;
                release_rels = obj.ReleaseRelations;
                track_rels = obj.TrackRelations;
                label_rels = obj.LabelRelations;
                url_rels = obj.UrlRelations;
            }
        }

        #region Properties
        
        protected T GetPropertyOrNull<T> (ref T field_reference) where T : class
        {
            if (field_reference == null) LoadMissingData ();
            return field_reference;
        }
        
        protected T GetPropertyOrDefault<T> (ref T? field_reference, T default_value) where T : struct
        {
            if (field_reference == null) LoadMissingData ();
            return field_reference ?? default_value;
        }
        
        protected ReadOnlyCollection<T> GetPropertyOrNew<T> (ref ReadOnlyCollection<T> field_reference)
        {
            return GetPropertyOrNew (ref field_reference, true);
        }
        
        protected ReadOnlyCollection<T> GetPropertyOrNew<T> (ref ReadOnlyCollection<T> field_reference, bool condition)
        {
            if (field_reference == null && condition) LoadMissingData ();
            return field_reference ?? new ReadOnlyCollection<T> (new T [0]);
        }

        string id;
        public virtual string Id {
            get { return id; }
        }

        byte score;
        public virtual byte Score {
            get { return score; }
        }

        ReadOnlyCollection<Relation<Artist>> artist_rels;
        public virtual ReadOnlyCollection<Relation<Artist>> ArtistRelations {
            get { return GetPropertyOrNew (ref artist_rels, !all_rels_loaded); }
        }

        ReadOnlyCollection<Relation<Release>> release_rels;
        public virtual ReadOnlyCollection<Relation<Release>> ReleaseRelations {
            get { return GetPropertyOrNew (ref release_rels, !all_rels_loaded); }
        }

        ReadOnlyCollection<Relation<Track>> track_rels;
        public virtual ReadOnlyCollection<Relation<Track>> TrackRelations {
            get { return GetPropertyOrNew (ref track_rels, !all_rels_loaded); }
        }

        ReadOnlyCollection<Relation<Label>> label_rels;
        public virtual ReadOnlyCollection<Relation<Label>> LabelRelations {
            get { return GetPropertyOrNew (ref label_rels, !all_rels_loaded); }
        }

        ReadOnlyCollection<UrlRelation> url_rels;
        public virtual ReadOnlyCollection<UrlRelation> UrlRelations {
            get { return GetPropertyOrNew (ref url_rels, !all_rels_loaded); }
        }

        public override bool Equals (object obj)
        {
            MusicBrainzObject mbobj = obj as MusicBrainzObject;
            return mbobj != null && mbobj.GetType ().Equals (GetType ()) && mbobj.Id == Id;
        }

        public override int GetHashCode ()
        {
            return (GetType ().Name + Id).GetHashCode ();
        }

        #endregion

        #region Static

        static void CreateRelation<T> (XmlReader reader, List<Relation<T>> relations) where T : MusicBrainzObject
        {
            while (reader.ReadToFollowing ("relation")) {
                string type = reader ["type"];
                RelationDirection direction = RelationDirection.Forward;
                string direction_string = reader ["direction"];
                if (direction_string != null && direction_string == "backward")
                    direction = RelationDirection.Backward;
                string begin = reader ["begin"];
                string end = reader ["end"];
                string attributes_string = reader ["attributes"];
                string [] attributes = attributes_string == null
                    ? null : attributes_string.Split (' ');

                reader.Read ();
                relations.Add (new Relation<T> (
                    type,
                    ConstructMusicBrainzObjectFromXml<T> (reader.ReadSubtree ()),
                    direction,
                    begin,
                    end,
                    attributes));
            }
            reader.Close ();
        }

        static string CreateUrl (string url_extension, int limit, int offset, string parameters)
        {
            StringBuilder builder = new StringBuilder ();
            if (limit != 25) {
                builder.Append ("&limit=");
                builder.Append (limit);
            }
            if (offset != 0) {
                builder.Append ("&offset=");
                builder.Append (offset);
            }
            builder.Append (parameters);
            return CreateUrl (url_extension, string.Empty, builder.ToString ());
        }

        static string CreateUrl (string url_extension, string mbid, string parameters)
        {
            StringBuilder builder = new StringBuilder (
                MusicBrainzService.ProviderUrl.Length + mbid.Length + parameters.Length + 9);
            builder.Append (MusicBrainzService.ProviderUrl);
            builder.Append (url_extension);
            builder.Append ('/');
            builder.Append (mbid);
            builder.Append ("?type=xml");
            builder.Append (parameters);
            return builder.ToString ();
        }

        static void XmlProcessingClosure (string url, XmlProcessingDelegate code)
        {
            Monitor.Enter (server_mutex);

            // Don't access the MB server twice within a second
            TimeSpan time = DateTime.Now - last_accessed;
            if (min_interval > time)
                Thread.Sleep ((min_interval - time).Milliseconds);

            HttpWebRequest request = WebRequest.Create (url) as HttpWebRequest;
            bool cache_implemented = false;
            
            try {
                request.CachePolicy = MusicBrainzService.CachePolicy;
                cache_implemented = true;
            } catch (NotImplementedException) {
            }
            
            HttpWebResponse response = null;
            
            try {
                response = request.GetResponse () as HttpWebResponse;
            } catch (WebException e) {
                response = (HttpWebResponse)e.Response;
            }
            
            if (response == null) throw new MusicBrainzNotFoundException ();

            switch (response.StatusCode) {
            case HttpStatusCode.BadRequest:
                Monitor.Exit (server_mutex);
                throw new MusicBrainzInvalidParameterException ();
            case HttpStatusCode.Unauthorized:
                Monitor.Exit (server_mutex);
                throw new MusicBrainzUnauthorizedException ();
            case HttpStatusCode.NotFound:
                Monitor.Exit (server_mutex);
                throw new MusicBrainzNotFoundException ();
            }

            bool from_cache = cache_implemented && response.IsFromCache;

            MusicBrainzService.OnXmlRequest (url, from_cache);

            if (from_cache) Monitor.Exit (server_mutex);

            // Should we read the stream into a memory stream and run the XmlReader off of that?
            code (new XmlTextReader (response.GetResponseStream ()));
            response.Close ();

            if (!from_cache) {
                last_accessed = DateTime.Now;
                Monitor.Exit (server_mutex);
            }
        }

        #endregion

        #region Query

        protected static byte QueryLimit {
            get { return 100; }
        }

        protected static string CreateLuceneParameter (string query)
        {
            return "&query=" + Utils.PercentEncode (query);
        }

        internal static List<T> Query<T> (string url_extension,
                                          byte limit, int offset,
                                          string parameters,
                                          out int? count) where T : MusicBrainzObject
        {
            int count_value = 0;
            List<T> results = new List<T> ();
            XmlProcessingClosure (
                CreateUrl (url_extension, limit, offset, parameters),
                delegate (XmlReader reader) {
                    reader.ReadToFollowing ("metadata");
                    reader.Read ();
                    int.TryParse (reader ["count"], out count_value);
                    while (reader.Read () && reader.NodeType == XmlNodeType.Element)
                        results.Add (ConstructMusicBrainzObjectFromXml<T> (reader.ReadSubtree ()));
                    reader.Close ();
                }
            );
            count = count_value == 0 ? results.Count : count_value;
            return results;
        }

        static T ConstructMusicBrainzObjectFromXml<T> (XmlReader reader) where T : MusicBrainzObject
        {
            ConstructorInfo constructor = typeof (T).GetConstructor (
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type [] { typeof (XmlReader) },
                null);
            return (T)constructor.Invoke (new object [] {reader});
        }

        #endregion

    }
}
