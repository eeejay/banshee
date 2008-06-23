//
// CachedList.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;

using Hyena.Data;
using Hyena.Collections;
using Hyena.Data.Sqlite;

using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{
    public class CachedList<T> : IEnumerable<T> where T : ICacheableItem, new ()
    {
        private static object lock_obj = new object ();
        private static int cache_id = 0;

        private static string NextCacheId ()
        {
            lock (lock_obj) {
                cache_id++;
                return String.Format ("CachedList-{0}", cache_id);
            }
        }

        private BansheeModelCache<T> cache;

        public static CachedList<DatabaseTrackInfo> CreateFromModelSelection (DatabaseTrackListModel model)
        {
            CachedList<DatabaseTrackInfo> list = new CachedList<DatabaseTrackInfo> (DatabaseTrackInfo.Provider);

            HyenaSqliteCommand add_range_command = new HyenaSqliteCommand (String.Format (@"
                INSERT INTO CoreCache (ModelID, ItemID)
                    SELECT ?, {0}", model.TrackIdsSql
            ));

            lock (model) {
                foreach (RangeCollection.Range range in model.Selection.Ranges) {
                    ServiceManager.DbConnection.Execute (add_range_command, list.CacheId, model.CacheId, range.Start, range.Count);
                }
            }

            list.cache.UpdateAggregates ();

            return list;
        }

        public CachedList (BansheeModelProvider<T> provider)
        {
            string uuid = NextCacheId ();
            cache = new BansheeModelCache <T> (ServiceManager.DbConnection, uuid, CacheableDatabaseModel.Instance, provider);
            Clear ();
        }

        public void Clear ()
        {
            ServiceManager.DbConnection.Execute ("DELETE FROM CoreCache WHERE ModelId = ?", CacheId);
        }

        public long CacheId {
            get { return cache.CacheId; }
        }

        public long Count {
            get { return cache.Count; }
        }

        public IEnumerator<T> GetEnumerator ()
        {
            for (long i = 0; i < cache.Count; i++) {
                yield return cache.GetValue (i);
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        private class CacheableDatabaseModel : ICacheableDatabaseModel
        {
            public static CacheableDatabaseModel Instance = new CacheableDatabaseModel ();
            public int FetchCount { get { return 200; } }
            public string ReloadFragment { get { return null; } }
            public string SelectAggregates { get { return null; } }
            public string JoinTable { get { return null; } }
            public string JoinFragment { get { return null; } }
            public string JoinPrimaryKey { get { return null; } }
            public string JoinColumn { get { return null; } }
            public bool CachesJoinTableEntries { get { return false; } }
            public bool CachesValues { get { return false; } }
            public Selection Selection { get { return null; } }
        }
    }
}
