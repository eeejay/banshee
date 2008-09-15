//
// DatabaseTrackListModel.cs
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
    public class DatabaseTrackListModel : TrackListModel, IExportableModel, 
        ICacheableDatabaseModel, IFilterable, ISortable, ICareAboutView
    {
        private readonly BansheeDbConnection connection;
        private IDatabaseTrackModelProvider provider;
        protected IDatabaseTrackModelCache cache;
        private Banshee.Sources.DatabaseSource source;
        
        private long count;

        private long filtered_count;
        private TimeSpan filtered_duration;
        private long filtered_filesize, filesize;
        
        private ISortableColumn sort_column;
        private string sort_query;
        private bool forced_sort_query;
        
        private string reload_fragment;
        private string join_table, join_fragment, join_primary_key, join_column, condition;

        private string query_fragment;
        private string user_query;

        private int rows_in_view;
        
        public DatabaseTrackListModel (BansheeDbConnection connection, IDatabaseTrackModelProvider provider, Banshee.Sources.DatabaseSource source)
        {
            this.connection = connection;
            this.provider = provider;
            this.source = source;
        }
        
        protected HyenaSqliteConnection Connection {
            get { return connection; }
        }

        private bool initialized = false;
        public void Initialize (IDatabaseTrackModelCache cache)
        {
            if (initialized)
                return;

            initialized = true;
            this.cache = cache;
            cache.AggregatesUpdated += HandleCacheAggregatesUpdated;
            GenerateSortQueryPart ();
        }
        
        private bool have_new_user_query = true;
        private void GenerateUserQueryFragment ()
        {
            if (!have_new_user_query)
                return;

            if (String.IsNullOrEmpty (UserQuery)) {
                query_fragment = null;
                query_tree = null;
            } else {
                query_tree = UserQueryParser.Parse (UserQuery, BansheeQuery.FieldSet);
                query_fragment = (query_tree == null) ? null : query_tree.ToSql (BansheeQuery.FieldSet);

                if (query_fragment != null && query_fragment.Length == 0) {
                    query_fragment = null;
                    query_tree = null;
                }
            }

            have_new_user_query = false;
        }

        private QueryNode query_tree;
        public QueryNode Query {
            get { return query_tree; }
        }

        protected string SortQuery {
            get { return sort_query; }
            set { sort_query = value; }
        }

        protected virtual void GenerateSortQueryPart ()
        {
            SortQuery = (SortColumn == null || SortColumn.SortType == SortType.None)
                ? (SortColumn != null && source is Banshee.Playlist.PlaylistSource)
                    ? "CorePlaylistEntries.ViewOrder ASC, CorePlaylistEntries.EntryID ASC" 
                    : BansheeQuery.GetSort ("Artist", true)
                : BansheeQuery.GetSort (SortColumn.SortKey, SortColumn.SortType == SortType.Ascending);
        }

        public void Sort (ISortableColumn column)
        {
            lock (this) {
                if (forced_sort_query) {
                    return;
                }
                
                if (sort_column == column && sort_column != null) {
                    switch (sort_column.SortType) {
                        case SortType.Ascending:    sort_column.SortType = SortType.Descending; break;
                        case SortType.Descending:   sort_column.SortType = SortType.None; break;
                        case SortType.None:         sort_column.SortType = SortType.Ascending; break;
                    }
                }
                
                // If we're switching from a different column, or we aren't in a playlist, make sure sort type isn't None
                if (sort_column != null && (sort_column != column || !(source is Banshee.Playlist.PlaylistSource)) && sort_column.SortType == SortType.None) {
                    sort_column.SortType = SortType.Ascending;
                }
                
                sort_column = column;
            
                GenerateSortQueryPart ();
                cache.Clear ();
            }
        }

        private void HandleCacheAggregatesUpdated (IDataReader reader)
        {
            filtered_duration = TimeSpan.FromMilliseconds (reader.IsDBNull (1) ? 0 : Convert.ToInt64 (reader[1]));
            filtered_filesize = reader.IsDBNull (2) ? 0 : Convert.ToInt64 (reader[2]);
        }
        
        public override void Clear ()
        {
            cache.Clear ();
            count = 0;
            filesize = 0;
            filtered_count = 0;
            OnCleared ();
        }

        public void InvalidateCache (bool notify)
        {
            if (cache == null) {
                Log.ErrorFormat ("Called invalidate cache for {0}'s track model, but cache is null", source);
            } else {
                cache.Clear ();
                if (notify) {
                    OnReloaded ();
                }
            }
        }

        private string unfiltered_query;
        public string UnfilteredQuery {
            get {
                return unfiltered_query ?? unfiltered_query = String.Format (
                    "FROM {0}{1} WHERE {2} {3}",
                    provider.From, JoinFragment,
                    String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where,
                    ConditionFragment
                );
            }
        }

        public virtual void UpdateUnfilteredAggregates ()
        {
            HyenaSqliteCommand count_command = new HyenaSqliteCommand (String.Format (
                "SELECT COUNT(*), SUM(CoreTracks.FileSize) {0}", UnfilteredQuery
            ));
            
            using (HyenaDataReader reader = new HyenaDataReader (connection.Query (count_command))) {
                count = reader.Get<long> (0);
                filesize = reader.Get<long> (1);
            }
        }

        public override void Reload ()
        {
            Reload (null);
        }

        public void Reload (IListModel reloadTrigger)
        {
            if (cache == null) {
                Log.WarningFormat ("Called Reload on {0} for source {1} but cache is null;  Did you forget to call AfterInitialized () in your DatabaseSource ctor?",
                    this, source == null ? "null source!" : source.Name);
                return;
            }

            lock (this) {
                GenerateUserQueryFragment ();

                UpdateUnfilteredAggregates ();
                cache.SaveSelection ();

                List<IFilterListModel> reload_models = new List<IFilterListModel> ();
                bool found = (reloadTrigger == null);
                foreach (IFilterListModel filter in source.CurrentFilters) {
                    if (found) {
                        reload_models.Add (filter);
                    } else if (filter == reloadTrigger) {
                        found = true;
                    }
                }

                if (reload_models.Count == 0) {
                    ReloadWithFilters (true);
                } else {
                    ReloadWithoutFilters ();

                    foreach (IFilterListModel model in reload_models) {
                        model.Reload (false);
                    }
                    
                    bool have_filters = false;
                    foreach (IFilterListModel filter in source.CurrentFilters) {
                        have_filters |= !filter.Selection.AllSelected;
                    }
                    
                    // Unless both artist/album selections are "all" (eg unfiltered), reload
                    // the track model again with the artist/album filters now in place.
                    if (have_filters) {
                        ReloadWithFilters (true);
                    }
                }

                cache.UpdateAggregates ();
                cache.RestoreSelection ();

                filtered_count = cache.Count;

                OnReloaded ();

                // Trigger these after the track list, b/c visually it's more important for it to update first
                foreach (IFilterListModel model in reload_models) {
                    model.RaiseReloaded ();
                }
            }
        }

        private void ReloadWithoutFilters ()
        {
            ReloadWithFilters (false);
        }

        private void ReloadWithFilters (bool with_filters)
        {
            StringBuilder qb = new StringBuilder ();
            qb.Append (UnfilteredQuery);
            
            if (with_filters) {
                foreach (IFilterListModel filter in source.CurrentFilters) {
                    string filter_sql = filter.GetSqlFilter ();
                    if (filter_sql != null) {
                        qb.Append (" AND ");
                        qb.Append (filter_sql);
                    }
                }
            }
            
            if (query_fragment != null) {
                qb.Append (" AND ");
                qb.Append (query_fragment);
            }
            
            if (sort_query != null) {
                qb.Append (" ORDER BY ");
                qb.Append (sort_query);
            }
            
            reload_fragment = qb.ToString ();
            cache.Reload ();
        }

        public override int IndexOf (TrackInfo track)
        {
            lock (this) {
                return (int) cache.IndexOf (track as DatabaseTrackInfo);
            }
        }

        public int IndexOfFirst (TrackInfo track)
        {
            lock (this) {
                return IndexOf (cache.GetSingle ("AND MetadataHash = ? ORDER BY OrderID", track.MetadataHash));
            }
        }

        private DateTime random_began_at = DateTime.MinValue;
        private DateTime last_random = DateTime.MinValue;
        private static string random_fragment = "AND (LastPlayedStamp < ? OR LastPlayedStamp IS NULL) AND (LastSkippedStamp < ? OR LastSkippedStamp IS NULL) ORDER BY RANDOM()";
        public override TrackInfo GetRandom (DateTime notPlayedSince, bool repeat)
        {
            lock (this) {
                if (Count == 0)
                    return null;

                if (random_began_at < notPlayedSince)
                    random_began_at = last_random = notPlayedSince;

                TrackInfo track = cache.GetSingle (random_fragment, random_began_at, random_began_at);

                if (track == null && repeat) {
                    random_began_at = last_random;
                    track = cache.GetSingle (random_fragment, random_began_at, random_began_at);
                }

                last_random = DateTime.Now;
                return track;
            }
        }

        public override TrackInfo this[int index] {
            get {
                lock (this) {
                    return cache.GetValue (index);
                }
            }
        }

        public override int Count {
            get { return (int) filtered_count; }
        }

        public TimeSpan Duration {
            get { return filtered_duration; }
        }

        public long FileSize {
            get { return filtered_filesize; }
        }
        
        public long UnfilteredFileSize {
            get { return filesize; }
        }
        
        public int UnfilteredCount {
            get { return (int) count; }
            set { count = value; }
        }

        public string UserQuery {
            get { return user_query; }
            set { 
                lock (this) {
                    user_query = value; 
                    have_new_user_query = true;
                }
            }
        }
        
        public string ForcedSortQuery {
            get { return forced_sort_query ? sort_query : null; }
            set { 
                forced_sort_query = value != null;
                sort_query = value;
                if (cache != null) {
                    cache.Clear ();
                }
            }
        }

        public string JoinTable {
            get { return join_table; }
            set {
                join_table = value;
                join_fragment = String.Format (", {0}", join_table);
            }
        }

        public string JoinFragment {
            get { return join_fragment; }
        }

        public string JoinPrimaryKey {
            get { return join_primary_key; }
            set { join_primary_key = value; }
        }

        public string JoinColumn {
            get { return join_column; }
            set { join_column = value; }
        }
        
        public void AddCondition (string part)
        {
            if (!String.IsNullOrEmpty (part)) {
                condition = condition == null ? part : String.Format ("{0} AND {1}", condition, part);
            }
        }
        
        public string Condition {
            get { return condition; }
        }

        public string ConditionFragment {
            get { return PrefixCondition ("AND"); }
        }

        private string PrefixCondition (string prefix)
        {
            string condition = Condition;
            return String.IsNullOrEmpty (condition)
                ? String.Empty
                : String.Format (" {0} {1} ", prefix, condition);
        }

        public int CacheId {
            get { return (int) cache.CacheId; }
        }

        public ISortableColumn SortColumn { 
            get { return sort_column; }
        }
                
        public virtual int RowsInView {
            protected get { return rows_in_view; }
            set { rows_in_view = value; }
        }

        int IExportableModel.GetLength () 
        {
            return Count;
        }
        
        IDictionary<string, object> IExportableModel.GetMetadata (int index)
        {
            return this[index].GenerateExportable ();
        }

        private string track_ids_sql;
        public string TrackIdsSql {
            get {
                if (track_ids_sql == null) {
                    if (!CachesJoinTableEntries) {
                        track_ids_sql = "ItemID FROM CoreCache WHERE ModelID = ? LIMIT ?, ?";
                    } else {
                        track_ids_sql = String.Format (
                            "{0} FROM {1} WHERE {2} IN (SELECT ItemID FROM CoreCache WHERE ModelID = ? LIMIT ?, ?)",
                            JoinColumn, JoinTable, JoinPrimaryKey
                        );
                    }
                }
                return track_ids_sql;
            }
        }

        private bool caches_join_table_entries = false;
        public bool CachesJoinTableEntries {
            get { return caches_join_table_entries; }
            set { caches_join_table_entries = value; }
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
        
        public bool CachesValues { get { return false; } }
    }
}
