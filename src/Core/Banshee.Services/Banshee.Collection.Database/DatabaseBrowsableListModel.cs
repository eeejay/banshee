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
using Hyena.Data.Sqlite;

using Banshee.Collection;
using Banshee.Database;

namespace Banshee.Collection.Database
{
    public interface IFilterListModel : Hyena.Data.IListModel
    {
        string FilterColumn { get; }
        string ItemToFilterValue (object item);
        void RaiseReloaded ();
        void Reload (bool notify);

        IEnumerable<object> GetSelectedObjects ();
    }
    
    public abstract class DatabaseBrowsableListModel<T, U> : BrowsableListModel<U>, IFilterListModel, ICacheableDatabaseModel
        where T : ICacheableItem, U, new()
    {
        private readonly BansheeModelCache<T> cache;
        private readonly DatabaseTrackListModel browsing_model;
        
        private long count;
        private string reload_fragment;
        
        private string reload_fragment_format;
        protected string ReloadFragmentFormat {
            get { return reload_fragment_format; }
            set { reload_fragment_format = value; }
        }
        
        protected readonly U select_all_item;

        public DatabaseBrowsableListModel (DatabaseTrackListModel trackModel, BansheeDbConnection connection, SqliteModelProvider<T> provider, U selectAllItem, string uuid)
            : base ()
        {
            browsing_model = trackModel;
            select_all_item = selectAllItem;
            
            cache = new BansheeModelCache <T> (connection, uuid, this, provider);
            cache.HasSelectAllItem = true;

            Selection.Changed += HandleSelectionChanged;
        }
        
#region IFilterModel<T> Implementation

        public abstract string FilterColumn { get; }
        public abstract string ItemToFilterValue (object item);

#endregion

        public IEnumerable<object> GetSelectedObjects ()
        {
            foreach (object o in SelectedItems) {
                yield return o;
            }
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            browsing_model.Reload (this);
        }

        public override void Reload ()
        {
            Reload (false);
        }
        
        protected virtual void GenerateReloadFragment ()
        {
            ReloadFragment = String.Format (
                ReloadFragmentFormat,
                browsing_model.CachesJoinTableEntries ? browsing_model.JoinFragment : null,
                browsing_model.CacheId,
                browsing_model.CachesJoinTableEntries
                    ? String.Format ("{0}.{1} AND CoreTracks.TrackID = {0}.{2}", browsing_model.JoinTable, browsing_model.JoinPrimaryKey, browsing_model.JoinColumn)
                    : "CoreTracks.TrackID"
            );
        }
        
        public abstract void UpdateSelectAllItem (long count);

        public void Reload (bool notify)
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
            get { return 20; }
        }

        public virtual string SelectAggregates { get { return null; } }

        public string ReloadFragment {
            get { return reload_fragment; }
            protected set { reload_fragment = value; }
        }

        public int CacheId {
            get { return (int) cache.CacheId; }
        }

        public void InvalidateCache ()
        {
            cache.ClearManagedCache ();
            OnReloaded ();
        }

        public virtual string JoinTable { get { return null; } }
        public virtual string JoinFragment { get { return null; } }
        public virtual string JoinPrimaryKey { get { return null; } }
        public virtual string JoinColumn { get { return null; } }
        public virtual bool CachesJoinTableEntries { get { return false; } }
    }
}
