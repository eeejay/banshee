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

using Hyena.Data;
using Hyena.Data.Query;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class TrackListDatabaseModel : TrackListModel, IExportableModel, IFilterable, ISortable, ICareAboutView
    {
        private Dictionary<int, LibraryTrackInfo> tracks = new Dictionary<int, LibraryTrackInfo>();
        
        private BansheeDbConnection connection;
        private int rows;
        private int uid;
        
        private ISortableColumn sort_column;
        private string sort_query;
        
        private Dictionary<string, string> filter_field_map = new Dictionary<string, string>();
        private string select_query;
        private string filter_query;
        private string filter;
        private string artist_id_filter_query;
        private string album_id_filter_query;
        private string join_fragment, condition;
        
        private int rows_in_view;

        private static int model_count = 0;
        private static bool cache_initialized = false;

        public TrackListDatabaseModel(BansheeDbConnection connection)
        {
            uid = model_count++;

            filter_field_map.Add("artist", "CoreArtists.Name");
            filter_field_map.Add("album", "CoreAlbums.Title");
            filter_field_map.Add("title", "CoreTracks.Title");
            
            this.connection = connection;

            if (!cache_initialized) {
                cache_initialized = true;
                // Invalidate any old cache
                IDbCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CoreTracksCache";
                command.ExecuteNonQuery();
            }

            Refilter ();
        }
        
        private void GenerateFilterQueryPart()
        {
            if (String.IsNullOrEmpty(Filter)) {
                filter_query = null;
            } else {
                filter_query = String.Format(@"
                    AND (CoreTracks.Title LIKE '%{0}%' 
                        OR CoreArtists.Name LIKE '%{0}%'
                        OR CoreAlbums.Title LIKE '%{0}%')", Filter);
            }
            
            /*Console.WriteLine(Filter);
            QueryParser parser = new QueryParser(Filter);
            QueryListNode parse_tree = parser.BuildTree();
            parse_tree.Dump();
            SqlQueryGenerator sql_generator = new SqlQueryGenerator(filter_field_map, parse_tree);
            
            filter_query = sql_generator.GenerateQuery();
            
            Console.WriteLine(filter_query);*/
            
           /*filter_query = String.Format(@"
                CoreTracks.AlbumID = CoreAlbums.AlbumID 
                AND CoreTracks.ArtistID = CoreArtists.ArtistID 
                AND (CoreTracks.Title LIKE '%{0}%'
                OR CoreTracks.ArtistID IN (SELECT CoreArtists.ArtistID FROM CoreArtists WHERE CoreArtists.Name LIKE '%{0}%')
                OR CoreTracks.AlbumID IN (SELECT CoreAlbums.AlbumID FROM CoreAlbums WHERE CoreAlbums.Title LIKE '%{0}%'))
            ", Filter);*/


            if (!UseCache) {
                select_query = String.Format(@"
                    SELECT CoreTracks.*, CoreArtists.Name, CoreAlbums.Title
                        FROM CoreTracks, CoreArtists, CoreAlbums{0}
                        WHERE 
                            CoreArtists.ArtistID = CoreTracks.ArtistID
                            AND CoreAlbums.AlbumID = CoreTracks.AlbumID {1}",
                    JoinFragment, ConditionFragment
                );
            } else {
                select_query = String.Format(@"
                    SELECT CoreTracks.*, CoreArtists.Name, CoreAlbums.Title 
                        FROM CoreTracks, CoreArtists, CoreAlbums{0}
                        INNER JOIN CoreTracksCache
                            ON CoreTracks.TrackID = CoreTracksCache.ID
                        WHERE
                            CoreArtists.ArtistID = CoreTracks.ArtistID
                            AND CoreAlbums.AlbumID = CoreTracks.AlbumID
                            AND CoreTracksCache.TableID = {1}
                            {2}",
                    JoinFragment, uid, ConditionFragment
                );
            }
            select_query += " LIMIT {0}, {1}";
        }
        
        private void GenerateSortQueryPart()
        {
            if(sort_column == null) {
                sort_query = null;
                return;
            }
            
            switch(sort_column.SortKey) {
                case "artist":
                    sort_query = "lower(CoreArtists.Name)";
                    break;
                case "album":
                    sort_query = "lower(CoreAlbums.Title)";
                    break;
                case "title":
                    sort_query = "lower(CoreTracks.Title)";
                    break;
                default:
                    sort_query = null;
                    return;
            }
            
            sort_query = String.Format(" {0} {1} ", sort_query, 
                sort_column.SortType == SortType.Ascending ? " ASC" : " DESC");
        }

        public void Refilter()
        {
            lock(this) {
                GenerateFilterQueryPart();
                InvalidateManagedCache();
            }
        }
        
        public void Sort(ISortableColumn column)
        {
            lock(this) {
                if(sort_column == column && sort_column != null) {
                    sort_column.SortType = sort_column.SortType == SortType.Ascending 
                        ? SortType.Descending 
                        : SortType.Ascending;
                }
            
                sort_column = column;
            
                GenerateSortQueryPart();
                InvalidateManagedCache();
            }
        }
        
        private void InvalidateManagedCache()
        {
            tracks.Clear();
        }

        public override void Clear()
        {
            InvalidateManagedCache();
            rows = 0;
            OnCleared();
        }
        
        public override void Reload()
        {
            bool from_cache = UseCache;
            if (from_cache) {
                StringBuilder qb = new StringBuilder ();
                qb.Append (String.Format (@"
                    DELETE FROM CoreTracksCache WHERE TableID = {0};
                    INSERT INTO CoreTracksCache 
                        SELECT null, {0}, CoreTracks.TrackID 
                            FROM CoreTracks, CoreAlbums, CoreArtists{1}
                            WHERE 
                                CoreTracks.AlbumID = CoreAlbums.AlbumID 
                                AND CoreTracks.ArtistID = CoreArtists.ArtistID {2}",
                    uid, JoinFragment, ConditionFragment
                ));
                
                if (artist_id_filter_query != null) {
                    qb.Append ("AND ");
                    qb.Append (artist_id_filter_query);
                }
                        
                if (album_id_filter_query != null) {
                    qb.Append ("AND ");
                    qb.Append (album_id_filter_query);
                }
                
                if (filter_query != null)
                    qb.Append (filter_query);
                
                if (sort_query != null) {
                    qb.Append (" ORDER BY ");
                    qb.Append (sort_query);
                }
                
                string cache_build_query = qb.ToString ();
                Console.WriteLine (StringExtensions.Flatten (cache_build_query));
                
                using (new Timer ("Generating cache table")) {
                    IDbCommand command = connection.CreateCommand ();
                    command.CommandText = cache_build_query;
                    command.ExecuteNonQuery ();
                }
            }

            string count_query = from_cache ?
                String.Format ("SELECT COUNT(*) FROM CoreTracksCache WHERE TableID = {0}", uid) :
                String.Format ("SELECT COUNT(*) FROM CoreTracks{0} {1}", JoinFragment, WhereFragment);
            
            using (new Timer ("Counting tracks")) {
                Console.WriteLine("Count query: {0}", count_query);
                IDbCommand command = connection.CreateCommand ();
                command.CommandText = count_query;
                rows = Convert.ToInt32 (command.ExecuteScalar ());
            }

            Console.WriteLine ("Total rows: {0}", rows);

            OnReloaded ();
        }

        private bool UseCache {
            get {
                // Always return true, meaning a source's tracks always get put into the CoreTracksCache
                // and other pieces (album/artist models, for example) can rely on that to filter themselves.
                return true;
                //return sort_query != null || filter_query != null || 
                //       artist_id_filter_query != null || album_id_filter_query != null;
            }
        }

        public override TrackInfo GetValue(int index)
        {
            if (tracks.ContainsKey (index)) {
                return tracks[index];
            }
            
            using(new Timer("Loading Track Set")) {
            
            int fetch_count = RowsInView > 0 ? RowsInView * 5 : 100;
            
            IDbCommand command = connection.CreateCommand ();
            command.CommandText = String.Format (select_query, index, fetch_count);
            Console.WriteLine (command.CommandText);
            IDataReader reader = command.ExecuteReader ();

            int i = index;
            while(reader.Read()) {
                if(!tracks.ContainsKey(i)) {
                    LibraryTrackInfo track = new LibraryTrackInfo(reader);
                    tracks.Add(i++, track);
                }
            }
            
            }
            
            if (tracks.ContainsKey (index)) {
                return tracks[index];
            }
            
            return null;
        }
        
        public override int Rows {
            get { return rows; }
        }
        
        public string Filter {
            get { return filter; }
            set { 
                lock(this) {
                    filter = value; 
                }
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

        public string WhereFragment {
            get { return PrefixCondition ("WHERE"); }
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

        public int DbId {
            get { return uid; }
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
            return Rows;
        }
        
        IDictionary<string, object> IExportableModel.GetMetadata(int index)
        {
            return GetValue(index).GenerateExportable();
        }
    }
}
