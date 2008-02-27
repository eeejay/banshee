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
using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Query;

using Banshee.Base;
using Banshee.Query;
using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class TrackListDatabaseModel : TrackListModel, IExportableModel, 
        ICacheableDatabaseModel, IFilterable, ISortable, ICareAboutView
    {
        private readonly BansheeDbConnection connection;
        private readonly BansheeModelProvider<DatabaseTrackInfo> provider;
        private readonly BansheeModelCache<DatabaseTrackInfo> cache;
        private int count;
        private TimeSpan duration;
        private long filesize;

        private int filtered_count;
        private TimeSpan filtered_duration;
        private long filtered_filesize;
        
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
            provider = DatabaseTrackInfo.Provider;
            cache = new BansheeModelCache <DatabaseTrackInfo> (connection, uuid, this, provider);
            cache.AggregatesUpdated += HandleCacheAggregatesUpdated;
            Refilter ();
        }
        
        private void GenerateFilterQueryPart()
        {
            if (String.IsNullOrEmpty(Filter)) {
                filter_query = null;
            } else {
                Hyena.Query.UserQueryParser qp = new UserQueryParser (Filter);
                QueryNode n = qp.BuildTree (BansheeQuery.FieldSet);

                filter_query = n.ToSql (BansheeQuery.FieldSet);

                /*
                Console.WriteLine ("query: {0}", Filter);
                Console.WriteLine ("Xml for Query: {0}", n.ToXml (BansheeQuery.FieldSet, true));
                Console.WriteLine ("Sql for Query: {0}", filter_query);
                Hyena.Query.QueryParser qp2 = new XmlQueryParser (n.ToXml (BansheeQuery.FieldSet));
                QueryNode n2 = qp2.BuildTree (BansheeQuery.FieldSet);
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

        private void GenerateSortQueryPart()
        {
            sort_query = (sort_column == null) ?
                null :
                BansheeQuery.GetSort (sort_column.SortKey, sort_column.SortType == SortType.Ascending);
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

        private void HandleCacheAggregatesUpdated (IDataReader reader)
        {
            filtered_duration = TimeSpan.FromMilliseconds (reader.IsDBNull (1) ? 0 : Convert.ToInt64 (reader[1]));
            filtered_filesize = reader.IsDBNull (2) ? 0 : Convert.ToInt64 (reader[2]);
        }
        
        public override void Clear()
        {
            cache.Clear ();
            count = 0;
            filtered_count = 0;
            OnCleared();
        }
        
        private bool first_reload = true;
        public override void Reload()
        {
            string unfiltered_query = String.Format (
                "FROM {0}{1} WHERE {2} {3}",
                provider.From, JoinFragment, provider.Where, ConditionFragment
            );

            using (IDataReader reader = connection.Query (String.Format (
                "SELECT COUNT(*), {0} {1}", SelectAggregates, unfiltered_query)))
            {
                if (reader.Read ()) {
                    count = Convert.ToInt32 (reader[0]);
                    duration = TimeSpan.FromMilliseconds (reader.IsDBNull (1) ? 0 : Convert.ToInt64 (reader[1]));
                    filesize = reader.IsDBNull (2) ? 0 : Convert.ToInt64 (reader[2]);
                }
            }

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

            filtered_count = cache.Count;
            first_reload = false;

            OnReloaded ();
        }

        public override int IndexOf (TrackInfo track)
        {
            DatabaseTrackInfo library_track = track as DatabaseTrackInfo;
            return library_track == null ? -1 : cache.IndexOf ((int)library_track.DbId);
        }

        public override TrackInfo this[int index] {
            get { return cache.GetValue (index); }
        }

        public override int Count {
            get { return filtered_count; }
        }

        public TimeSpan Duration {
            get { return duration; }
        }

        public long FileSize {
            get { return filesize; }
        }
        
        public int UnfilteredCount {
            get { return count; }
        }

        public TimeSpan FilteredDuration {
            get { return filtered_duration; }
        }

        public long FilteredFileSize {
            get { return filtered_filesize; }
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

        public string SelectAggregates {
            get { return "SUM(CoreTracks.Duration), SUM(CoreTracks.FileSize)"; }
        }

        // Implement IDatabaseModel
        public string ReloadFragment {
            get { return reload_fragment; }
        }
    }
}
