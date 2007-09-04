//
// TrackListDatabaseModel.cs
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
using System.Data;
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
        
        private ISortableColumn sort_column;
        private string sort_query;
        
        private Dictionary<string, string> filter_field_map = new Dictionary<string, string>();
        private string filter_query;
        private string filter;
        private string artist_id_filter_query;
        private string album_id_filter_query;
        
        private int rows_in_view;
        
        public TrackListDatabaseModel(BansheeDbConnection connection)
        {
            filter_field_map.Add("artist", "CoreArtists.Name");
            filter_field_map.Add("album", "CoreAlbums.Title");
            filter_field_map.Add("title", "CoreTracks.Title");
            
            this.connection = connection;
        }
        
        private void GenerateFilterQueryPart()
        {
            if(String.IsNullOrEmpty(Filter)) {
                filter_query = null;
                return;
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

            filter_query = String.Format(@"
                AND (CoreTracks.Title LIKE '%{0}%' 
                    OR CoreArtists.Name LIKE '%{0}%'
                    OR CoreAlbums.Title LIKE '%{0}%')", Filter);
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
            string count_query = "SELECT COUNT(*) FROM CoreTracks";
            
            if(filter_query != null || sort_query != null || 
                artist_id_filter_query != null || album_id_filter_query != null) {
                string cache_build_query = @"
                    DELETE FROM CoreTracksCache WHERE TableID = 0;
                    INSERT INTO CoreTracksCache 
                        SELECT null, 0, CoreTracks.TrackID 
                            FROM CoreTracks, CoreAlbums, CoreArtists 
                            WHERE 
                                CoreTracks.AlbumID = CoreAlbums.AlbumID 
                                AND CoreTracks.ArtistID = CoreArtists.ArtistID ";
                
                bool from_cache = false;
                
                if(artist_id_filter_query != null) {
                    from_cache = true;
                    cache_build_query = String.Format("{0} AND {1}", cache_build_query, artist_id_filter_query);
                }
                        
                if(album_id_filter_query != null) {
                    from_cache = true;
                    cache_build_query = String.Format("{0} AND {1}", cache_build_query, album_id_filter_query);
                }
                
                if(filter_query != null) {
                    from_cache = true;
                    cache_build_query = String.Format("{0} {1}", cache_build_query, filter_query);
                }
                
                if(sort_query != null) {
                    cache_build_query = String.Format("{0} ORDER BY {1}", cache_build_query, sort_query);
                }
                
                if(from_cache) {
                    count_query = "SELECT COUNT(*) FROM CoreTracksCache";
                }
                
                Console.WriteLine(StringExtensions.Flatten(cache_build_query));
                
                using(new Timer("Generating cache table")) {
                    IDbCommand command = connection.CreateCommand();
                    command.CommandText = cache_build_query;
                    command.ExecuteNonQuery();
                }
            }
            
			using(new Timer("Counting tracks")) {
			    IDbCommand command = connection.CreateCommand();
			    command.CommandText = count_query;
			    rows = Convert.ToInt32(command.ExecuteScalar());
			}
			
			Console.WriteLine("Total rows: {0}", rows);
			
			OnReloaded();
        }

        public override TrackInfo GetValue(int index)
        {
            if(tracks.ContainsKey(index)) {
                return tracks[index];
            }
            
            using(new Timer("Loading Track Set")) {
            
            int fetch_count = RowsInView > 0 ? RowsInView * 5 : 100;
            
            IDbCommand command = connection.CreateCommand();
            if(sort_query == null && filter_query == null && 
                artist_id_filter_query == null && album_id_filter_query == null) {
			    command.CommandText = String.Format(@"
			        SELECT CoreTracks.*, CoreArtists.Name, CoreAlbums.Title
			            FROM CoreTracks, CoreArtists, CoreAlbums
                        WHERE 
                            CoreArtists.ArtistID = CoreTracks.ArtistID
                            AND CoreAlbums.AlbumID = CoreTracks.AlbumID
			            LIMIT {0}, {1}", index, fetch_count);
			} else {
                command.CommandText = String.Format(@"
			        SELECT CoreTracks.*, CoreArtists.Name, CoreAlbums.Title 
			            FROM CoreTracks, CoreArtists, CoreAlbums
                        INNER JOIN CoreTracksCache
                            ON CoreTracks.TrackID = CoreTracksCache.ID
                        WHERE
                            CoreArtists.ArtistID = CoreTracks.ArtistID
                            AND CoreAlbums.AlbumID = CoreTracks.AlbumID

			            LIMIT {0}, {1}", index, fetch_count);
			}

            Console.WriteLine(command.CommandText);
			
			IDataReader reader = command.ExecuteReader();
			
			int i = index;
			while(reader.Read()) {
			    if(!tracks.ContainsKey(i)) {
			        LibraryTrackInfo track = new LibraryTrackInfo(reader);
			        tracks.Add(i++, track);
			    }
			}
			
			}
            
            if(tracks.ContainsKey(index)) {
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
