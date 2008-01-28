//
// TrackListDatabaseModel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Data;
using System.Text;
using System.Collections.Generic;

using Mono.Unix;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Data.Query;

using Banshee.Base;
using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class TrackListDatabaseModel : TrackListModel, IExportableModel, 
        ICacheableDatabaseModel, IFilterable, ISortable, ICareAboutView
    {
        private BansheeDbConnection connection;
        private BansheeModelProvider<LibraryTrackInfo> provider;
        private BansheeModelCache<LibraryTrackInfo> cache;
        private int count;
        private int unfiltered_count;
        
        private ISortableColumn sort_column;
        private string sort_query;
        private bool forced_sort_query;
        
        private string reload_fragment;
        private string join_fragment, condition;

        private string filter_query;
        private string filter;
        private string artist_id_filter_query;
        private string album_id_filter_query;
        
        private int rows_in_view;
        
        
        public TrackListDatabaseModel (BansheeDbConnection connection, string uuid)
        {
            this.connection = connection;
            provider = LibraryTrackInfo.Provider;
            cache = new BansheeModelCache <LibraryTrackInfo> (connection, uuid, this, provider);
            Refilter ();
        }
        
        private void GenerateFilterQueryPart()
        {
            if (String.IsNullOrEmpty(Filter)) {
                filter_query = null;
            } else {
                Hyena.Data.Query.UserQueryParser qp = new UserQueryParser (Filter);
                QueryNode n = qp.BuildTree (field_set);

                filter_query = n.ToSql (field_set);

                /*
                Console.WriteLine ("query: {0}", Filter);
                Console.WriteLine ("Xml for Query: {0}", n.ToXml (field_set, true));
                Console.WriteLine ("Sql for Query: {0}", filter_query);
                Hyena.Data.Query.QueryParser qp2 = new XmlQueryParser (n.ToXml (field_set));
                QueryNode n2 = qp2.BuildTree (field_set);
                if (n2 != null) {
                    Console.WriteLine ("User query for Xml: {0}", n2.ToUserQuery ());
                } else
                    Console.WriteLine ("n2 is null");
                */

                if (filter_query.Length == 0)
                    filter_query = null;
                //else {
                    //artist_id_filter_query = null;
                    //album_id_filter_query = null;
                //}
            }
        }

        private string AscDesc ()
        {
            return sort_column.SortType == SortType.Ascending ? " ASC" : " DESC";
        }

        private void GenerateSortQueryPart()
        {
            if(sort_column == null) {
                sort_query = null;
                return;
            }
            
            sort_query = GetSort (sort_column.SortKey, AscDesc ());
        }

        private const string default_sort = "lower(CoreArtists.Name) ASC, lower(CoreAlbums.Title) ASC, CoreTracks.TrackNumber ASC, CoreTracks.Uri ASC";
        public static string GetSort (string key, string ascDesc)
        {
            string sort_query = null;
            switch(key) {
                case "Track":
                    sort_query = String.Format (@"
                        lower(CoreArtists.Name) ASC, 
                        lower(CoreAlbums.Title) ASC, 
                        CoreTracks.TrackNumber {0}", ascDesc); 
                    break;

                case "Artist":
                    sort_query = String.Format (@"
                        lower(CoreArtists.Name) {0}, 
                        lower(CoreAlbums.Title) ASC, 
                        CoreTracks.TrackNumber ASC,
                        CoreTracks.Uri ASC", ascDesc); 
                    break;

                case "Album":
                    sort_query = String.Format (@"
                        lower(CoreAlbums.Title) {0},
                        lower(CoreArtists.Name) ASC,
                        CoreTracks.TrackNumber ASC,
                        CoreTracks.Uri ASC", ascDesc); 
                    break;

                case "Title":
                    sort_query = String.Format (@"
                        lower(CoreTracks.Title) {0},
                        lower(CoreArtists.Name) ASC, 
                        lower(CoreAlbums.Title) ASC", ascDesc); 
                    break;

                case "Random":
                    sort_query = "RANDOM ()";
                    break;

                case "Year":
                case "Duration":
                case "Rating":
                case "PlayCount":
                case "SkipCount":
                case "LastPlayedStamp":
                case "DateAddedStamp":
                case "Uri":
                    sort_query = String.Format (
                        "CoreTracks.{0} {1}, {2}",
                        key, ascDesc, default_sort
                    );
                    break;
            }
            return sort_query;
        }

        public void Refilter()
        {
            lock(this) {
                GenerateFilterQueryPart();
                cache.Clear ();
            }
        }
        
        public void Sort(ISortableColumn column)
        {
            lock(this) {
                if (forced_sort_query) {
                    return;
                }
                
                if(sort_column == column && sort_column != null) {
                    sort_column.SortType = sort_column.SortType == SortType.Ascending 
                        ? SortType.Descending 
                        : SortType.Ascending;
                }
            
                sort_column = column;
            
                GenerateSortQueryPart();
                cache.Clear ();
            }
        }
        
        public override void Clear()
        {
            cache.Clear ();
            unfiltered_count = 0;
            count = 0;
            OnCleared();
        }
        
        private bool first_reload = true;
        public override void Reload()
        {
            string unfiltered_query = String.Format (
                "FROM {0}{1} WHERE {2} {3}",
                provider.From, JoinFragment, provider.Where, ConditionFragment
            );

            unfiltered_count = connection.QueryInt32 (String.Format (
                "SELECT COUNT(*) {0}", unfiltered_query
            ));

            StringBuilder qb = new StringBuilder ();
                
            qb.Append (unfiltered_query);
            
            if (artist_id_filter_query != null) {
                qb.Append ("AND ");
                qb.Append (artist_id_filter_query);
            }
                    
            if (album_id_filter_query != null) {
                qb.Append ("AND ");
                qb.Append (album_id_filter_query);
            }
            
            if (filter_query != null) {
                qb.Append ("AND ");
                qb.Append (filter_query);
            }
            
            if (sort_query != null) {
                qb.Append (" ORDER BY ");
                qb.Append (sort_query);
            }
                
            reload_fragment = qb.ToString ();

            if (!first_reload || !cache.Warm) {
                cache.Reload ();
            }

            count = cache.Count;
            first_reload = false;

            OnReloaded ();
        }

        public override int IndexOf (TrackInfo track)
        {
            LibraryTrackInfo library_track = track as LibraryTrackInfo;
            return library_track == null ? -1 : cache.IndexOf ((int)library_track.DbId);
        }

        public override TrackInfo this[int index] {
            get { return cache.GetValue (index); }
        }
        
        public override int Count {
            get { return count; }
        }
        
        public int UnfilteredCount {
            get { return unfiltered_count; }
        }
        
        public string Filter {
            get { return filter; }
            set { 
                lock(this) {
                    filter = value; 
                }
            }
        }
        
        public string ForcedSortQuery {
            get { return forced_sort_query ? sort_query : null; }
            set { 
                forced_sort_query = value != null;
                sort_query = value;
                cache.Clear ();
            }
        }

        public string JoinFragment {
            get { return join_fragment; }
            set {
                join_fragment = value;
                Refilter();
            }
        }

        public string Condition {
            get { return condition; }
            set {
                condition = value;
                Refilter();
            }
        }

        public string ConditionFragment {
            get { return PrefixCondition ("AND"); }
        }

        private string PrefixCondition (string prefix)
        {
            string condition = Condition;
            if (condition == null || condition == String.Empty)
                return String.Empty;
            else
                return String.Format (" {0} {1} ", prefix, condition);
        }
        
        public override IEnumerable<ArtistInfo> ArtistInfoFilter {
            set { 
                ModelHelper.BuildIdFilter<ArtistInfo>(value, "CoreTracks.ArtistID", artist_id_filter_query,
                    delegate(ArtistInfo artist) {
                        if(!(artist is LibraryArtistInfo)) {
                            return null;
                        }
                        
                        return ((LibraryArtistInfo)artist).DbId.ToString();
                    },
                
                    delegate(string new_filter) {
                        artist_id_filter_query = new_filter;
                        Refilter();
                        Reload();
                    }
                );
            }
        }
        
        public override IEnumerable<AlbumInfo> AlbumInfoFilter {
            set { 
                ModelHelper.BuildIdFilter<AlbumInfo>(value, "CoreTracks.AlbumID", album_id_filter_query,
                    delegate(AlbumInfo album) {
                        if(!(album is LibraryAlbumInfo)) {
                            return null;
                        }
                        
                        return ((LibraryAlbumInfo)album).DbId.ToString();
                    },
                
                    delegate(string new_filter) {
                        album_id_filter_query = new_filter;
                        Refilter();
                        Reload();
                    }
                );
            }
        }

        public int CacheId {
            get { return cache.CacheId; }
        }

        public ISortableColumn SortColumn { 
            get { return sort_column; }
        }
                
        public virtual int RowsInView {
            protected get { return rows_in_view; }
            set { rows_in_view = value; }
        }

        int IExportableModel.GetLength() 
        {
            return Count;
        }
        
        IDictionary<string, object> IExportableModel.GetMetadata(int index)
        {
            return this[index].GenerateExportable();
        }

        // Implement ICacheableModel
        public int FetchCount {
            get { return RowsInView > 0 ? RowsInView * 5 : 100; }
        }

        // Implement IDatabaseModel
        public string ReloadFragment {
            get { return reload_fragment; }
        }

        public static QueryFieldSet FieldSet {
            get { return field_set; }
        }
        
        public override QueryField ArtistField {
            get { return field_set["artist"]; }
        }

        public override QueryField AlbumField {
            get { return field_set["album"]; }
        }

        protected static QueryFieldSet field_set = new QueryFieldSet (
            new QueryField (
                "artist", Catalog.GetString ("Artist"), "CoreArtists.Name", true,
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("by"), Catalog.GetString ("artist"), Catalog.GetString ("artists"),
                "by", "artist", "artists"
            ),
            new QueryField (
                "album", Catalog.GetString ("Album"), "CoreAlbums.Title", true,
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("on"), Catalog.GetString ("album"), Catalog.GetString ("from"),
                "on", "album", "from", "albumtitle"
            ),
            new QueryField (
                "title", Catalog.GetString ("Track Title"), "CoreTracks.Title", true,
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("title"), Catalog.GetString ("titled"), Catalog.GetString ("name"), Catalog.GetString ("named"),
                "title", "titled", "name", "named"
            ),
            new QueryField (
                "year", Catalog.GetString ("Year"), "CoreTracks.Year", typeof(IntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("year"), Catalog.GetString ("released"), Catalog.GetString ("yr"),
                "year", "released", "yr"
            ),
            new QueryField (
                "rating", Catalog.GetString ("Rating"), "CoreTracks.Rating", typeof(IntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("rating"), Catalog.GetString ("stars"),
                "rating", "stars"
            ),
            new QueryField (
                "playcount", Catalog.GetString ("Play Count"), "CoreTracks.PlayCount", typeof(IntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("plays"), Catalog.GetString ("playcount"), Catalog.GetString ("listens"),
                "plays", "playcount", "numberofplays", "listens"
            ),
            new QueryField (
                "skipcount", Catalog.GetString ("Skip Count"), "CoreTracks.SkipCount", typeof(IntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("skips"), Catalog.GetString ("skipcount"),
                "skips", "skipcount"
            ),
            new QueryField (
                "filesize", Catalog.GetString ("File Size"), "CoreTracks.FileSize", typeof(FileSizeQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("size"), Catalog.GetString ("filesize"),
                "size", "filesize"
            ),
            new QueryField (
                "uri", Catalog.GetString ("File Path"), "CoreTracks.Uri",
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("uri"), Catalog.GetString ("path"), Catalog.GetString ("file"), Catalog.GetString ("location"),
                "uri", "path", "file", "location"
            ),
            new QueryField (
                "duration", Catalog.GetString ("Duration"), "CoreTracks.Duration", typeof(IntegerQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("duration"), Catalog.GetString ("length"), Catalog.GetString ("time"),
                "duration", "length", "time"
            ),
            new QueryField (
                "mimetype", Catalog.GetString ("Mime Type"), "CoreTracks.MimeType {0} OR CoreTracks.Uri {0}",
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("type"), Catalog.GetString ("mimetype"), Catalog.GetString ("format"), Catalog.GetString ("ext"),
                "type", "mimetype", "format", "ext", "mime"
            ),
            new QueryField (
                "lastplayed", Catalog.GetString ("Last Played Date"), "CoreTracks.LastPlayedStamp", typeof(DateQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("lastplayed"), Catalog.GetString ("played"), Catalog.GetString ("playedon"),
                "lastplayed", "played", "playedon"
            ),
            new QueryField (
                "added", Catalog.GetString ("Imported Date"), "CoreTracks.DateAddedStamp", typeof(DateQueryValue),
                // Translators: These are unique search fields.  Please, no spaces. Blank ok.
                Catalog.GetString ("added"), Catalog.GetString ("imported"), Catalog.GetString ("addedon"), Catalog.GetString ("dateadded"), Catalog.GetString ("importedon"),
                "added", "imported", "addedon", "dateadded", "importedon"
            ),
            new QueryField (
                "playlistid", Catalog.GetString ("Playlist"),
                "CoreTracks.TrackID {2} IN (SELECT TrackID FROM CorePlaylistEntries WHERE PlaylistID = {1})", typeof(IntegerQueryValue),
                "playlistid", "playlist"
            ),
            new QueryField (
                "smartplaylistid", Catalog.GetString ("Smart Playlist"),
                "CoreTracks.TrackID {2} IN (SELECT TrackID FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID = {1})", typeof(IntegerQueryValue),
                "smartplaylistid", "smartplaylist"
            )
        );
    }
}
