//
// DatabaseBrowsableListModel.cs
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

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Collection;
using Banshee.Database;

namespace Banshee.Collection.Database
{   
    public abstract class DatabaseFilterListModel<T, U> : FilterListModel<U>, ICacheableDatabaseModel
        where T : U, new () where U : ICacheableItem, new()
    {
        private readonly BansheeModelCache<T> cache;
        private readonly Banshee.Sources.DatabaseSource source;
        
        private long count;
        private string reload_fragment;
        
        private string reload_fragment_format;
        protected string ReloadFragmentFormat {
            get { return reload_fragment_format; }
            set { reload_fragment_format = value; }
        }
        
        protected readonly U select_all_item;

        public DatabaseFilterListModel (Banshee.Sources.DatabaseSource source, DatabaseTrackListModel trackModel, HyenaSqliteConnection connection, SqliteModelProvider<T> provider, U selectAllItem, string uuid)
            : base (trackModel)
        {
            this.source = source;
            select_all_item = selectAllItem;
            
            cache = new BansheeModelCache <T> (connection, uuid, this, provider);
            cache.HasSelectAllItem = true;
        }
        
        public override void Clear ()
        {
        }
        
        protected virtual void GenerateReloadFragment ()
        {
            ReloadFragment = String.Format (
                ReloadFragmentFormat,
                FilteredModel.CachesJoinTableEntries ? FilteredModel.JoinFragment : null,
                FilteredModel.CacheId,
                FilteredModel.CachesJoinTableEntries
                    ? String.Format (
                        "{0}.{1} AND CoreTracks.TrackID = {0}.{2}",
                        FilteredModel.JoinTable, FilteredModel.JoinPrimaryKey, FilteredModel.JoinColumn)
                    : "CoreTracks.TrackID",
                GetFilterFragment ()
            );
        }

        private string GetFilterFragment ()
        {
            StringBuilder qb = new StringBuilder ();
            foreach (IFilterListModel model in UpstreamFilters) {
                string filter = model.GetSqlFilter ();
                if (filter != null) {
                    qb.Append (" AND ");
                    qb.Append (filter);
                }
            }
            return qb.ToString ();
        }

        private IEnumerable<IFilterListModel> UpstreamFilters {
            get {
                foreach (IFilterListModel model in source.FilterModels) {
                    if (this == model) {
                        break;
                    } else {
                        yield return model;
                    }
                }
            }
        }
        
        public virtual bool CachesValues { get { return false; } }
        
        protected abstract string ItemToFilterValue (object o);

        // Ick, duplicated from DatabaseTrackListModel
        public override string GetSqlFilter ()
        {
            string filter = null;
            
            // If the only item is the "All" item, then we shouldn't allow any matches, so insert an always-false condition
            if (Count == 1) {
                return "0=1";
            } else {
                ModelHelper.BuildIdFilter<object> (GetSelectedObjects (), FilterColumn, null,
                    delegate (object item) {
                        if (item != select_all_item) {
                            return ItemToFilterValue (item);
                        }
                        return null;
                    },
                    delegate (string new_filter) { filter = new_filter; }
                );
            }
            
            return filter;
        }
        
        public abstract void UpdateSelectAllItem (long count);

        public override void Reload (bool notify)
        {
            GenerateReloadFragment ();

            cache.SaveSelection ();
            cache.Reload ();
            cache.UpdateAggregates ();
            cache.RestoreSelection ();

            count = cache.Count + 1;
            
            UpdateSelectAllItem (count - 1);

            if (notify)
                OnReloaded ();
        }
        
        public override U this[int index] {
            get {
                if (index == 0)
                    return select_all_item;

                return cache.GetValue (index - 1);
            }
        }

        public override int Count { 
            get { return (int) count; }
        }

        // Implement ICacheableModel
        public virtual int FetchCount {
            get { return 40; }
        }

        public virtual string SelectAggregates { get { return null; } }

        public string ReloadFragment {
            get { return reload_fragment; }
            protected set { reload_fragment = value; }
        }

        public int CacheId {
            get { return (int) cache.CacheId; }
        }
        
        public IEnumerable<object> GetSelectedObjects ()
        {
            foreach (object o in SelectedItems) {
                yield return o;
            }
        }

        public void InvalidateCache ()
        {
            cache.ClearManagedCache ();
            OnReloaded ();
        }
        
        public abstract string FilterColumn { get; }

        public virtual string JoinTable { get { return null; } }
        public virtual string JoinFragment { get { return null; } }
        public virtual string JoinPrimaryKey { get { return null; } }
        public virtual string JoinColumn { get { return null; } }
        public virtual bool CachesJoinTableEntries { get { return false; } }
    }
}
