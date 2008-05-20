//
// MigoModelProvider.cs
//
// Authors:
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
using System.Collections.Generic;

using Hyena;
using Hyena.Data.Sqlite;

namespace Migo.Syndication
{
    // Caches all results retrieved from the database, such that any subsequent retrieval will
    // return the same instance.
    public class MigoModelProvider<T> : SqliteModelProvider<T> where T : ICacheableItem, new()
    {
        private Dictionary<long, T> full_cache = new Dictionary<long, T> ();
        
        public MigoModelProvider (HyenaSqliteConnection connection, string table_name) : base (connection, table_name)
        {
        }
        
        public override void Save (T target)
        {
            base.Save (target);
            long dbid = PrimaryKeyFor (target);
            if (!full_cache.ContainsKey (dbid)) {
                full_cache[dbid] = target;
            }
        }

        public override T Load (System.Data.IDataReader reader)
        {
            long dbid = PrimaryKeyFor (reader);
            if (full_cache.ContainsKey (dbid)) {
                return full_cache[dbid];
            } else {
                T item = base.Load (reader);
                full_cache[dbid] = item;
                return item;
            }
        }
        
        public override void Delete (long id)
        {
            full_cache.Remove (id);
            base.Delete (id);
        }
        
        public override void Delete (IEnumerable<T> items)
        {
            foreach (T item in items) {
                if (item != null)
                    full_cache.Remove (PrimaryKeyFor (item));
            }
                
            base.Delete (items);
        }
    }
}