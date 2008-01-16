//
// CacheableDatabaseModel.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
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
using System.Collections.Generic;
using System.Data;

namespace Hyena.Data.Sqlite
{
    public sealed class DatabaseCacheTable
    {
        public static int ModelCount;
    }
    
    public abstract class CacheableDatabaseModel<T> : DatabaseModel<T>, ICacheableDatabaseModel<T>
    {
        private DatabaseModelCache cache;
        private HyenaSqliteConnection connection;
        private HyenaSqliteCommand select_range_command;
        private HyenaSqliteCommand count_command;
        private string reload_sql;
        private int uid;
        private int rows;
        private bool warm;
        
        private static bool cache_initialized = false;
        
        protected abstract string ReloadFragment { get; }
        protected abstract bool Persistent { get; }
        
        
        public CacheableDatabaseModel (HyenaSqliteConnection connection, ICacheableModel model)
            : base (connection)
        {
            this.connection = connection;
            cache = new DatabaseCacheableModel (model, this);
            
            CheckCacheTable ();
            
            count_command = new HyenaSqliteCommand (String.Format (
                "SELECT COUNT(*) FROM {0} WHERE ModelID = ?", CacheTableName), 1);
            
            if (Persistent) {
                FindOrCreateCacheModelId (TableName);
            } else {
                uid = DatabaseCacheTable.ModelCount++;
            }
                
            select_range_command = new HyenaSqliteCommand (Where.Length > 0
                ? String.Format (@"
                                SELECT {0} FROM {1}
                                    INNER JOIN {2}
                                        ON {3} = {2}.ItemID
                                    WHERE
                                        {2}.ModelID = ? AND
                                        {4}
                                    LIMIT ?, ?", Select, From, CacheTableName, PrimaryKey, Where)
                : String.Format (@"
                                SELECT {0} FROM {1}
                                    INNER JOIN {2}
                                        ON {3} = {2}.ItemID
                                    WHERE
                                        {2}.ModelID = ?
                                    LIMIT ?, ?", Select, From, CacheTableName, PrimaryKey), 3);
            
            reload_sql = String.Format (@"
                DELETE FROM {0} WHERE ModelID = {1};
                    INSERT INTO {0} SELECT null, {1}, {2} ", CacheTableName, uid, PrimaryKey);
            
            if (!cache_initialized && !Persistent) {
                // Invalidate any old cache
                connection.Execute (String.Format ("DELETE FROM {0}", CacheTableName));
            }
            cache_initialized = true;
        }
        
        private void CheckCacheTable ()
        {
            using (IDataReader reader = connection.ExecuteReader (GetSchemaSql (CacheTableName))) {
                if (!reader.Read ()) {
                    connection.Execute (String.Format (@"
                        CREATE TABLE {0} (
                            OrderID INTEGER PRIMARY KEY,
                            ModelID INTEGER,
                            ItemID INTEGER)", CacheTableName));
                }
            }
        }
        
        protected override void PrepareSelectRangeCommand (int offset, int limit)
        {
            SelectRangeCommand.ApplyValues (uid, offset, limit);
        }
        
        protected override HyenaSqliteCommand SelectRangeCommand {
            get { return select_range_command; }
        }
            
        protected virtual Dictionary<int, T> Cache {
            get { return cache.CacheImpl; }
        }
        
        public virtual T GetValue (int index)
        {
            return cache.GetValueImpl (index);
        }
        
        protected void InvalidateManagedCache ()
        {
            cache.InvalidateManagedCacheImpl ();
        }
        
        protected virtual void FetchSet (int offset, int limit)
        {
            int i = offset;
            foreach (T item in FetchRange (offset, limit)) {
                if (!Cache.ContainsKey (i)) {
                    Add (i, item);
                }
                i++;
            }
        }
        
    }
}
