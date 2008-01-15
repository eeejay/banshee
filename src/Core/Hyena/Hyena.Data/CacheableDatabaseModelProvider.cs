//
// CacheableDatabaseModelProvider.cs
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

namespace Hyena.Data
{
    public sealed class DatabaseCacheTable
    {
        public static int ModelCount;
    }
    
    public abstract class CacheableDatabaseModelProvider<T> : DatabaseModelProvider<T>
    {
        private sealed class DatabaseCacheableModelProvider : CacheableModelAdapter<T>
        {
            private CacheableDatabaseModelProvider<T> db;
            
            public DatabaseCacheableModelProvider(ICacheableModel model, CacheableDatabaseModelProvider<T> db)
                : base(model)
            {
                this.db = db;
            }
            
            protected override Dictionary<int, T> Cache {
                get { return db.Cache; }
            }
            
            public Dictionary<int, T> CacheImpl {
                get { return base.Cache; }
            }
            
            public override T GetValue(int index)
            {
                return db.GetValue(index);
            }
            
            public T GetValueImpl(int index)
            {
                return base.GetValue(index);
            }
            
            public override void Clear()
            {
                db.Clear();
            }
            
            public void ClearImpl()
            {
                base.Clear();
            }
            
            protected override void FetchSet(int offset, int limit)
            {
                db.FetchSet(offset, limit);
            }
            
            protected override void Add(int key, T value)
            {
                db.Add(key, value);
            }
            
            public void AddImpl(int key, T value)
            {
                base.Add(key, value);
            }

            public override int Reload ()
            {
                return db.Reload();
            }
            
            public void InvalidateManagedCacheImpl()
            {
                InvalidateManagedCache();
            }
        }
        
        private DatabaseCacheableModelProvider cache;
        private HyenaDbConnection connection;
        private HyenaDbCommand select_range_command;
        private HyenaDbCommand count_command;
        private string reload_sql;
        private int uid;
        private int rows;
        private bool warm;
        
        private static bool cache_initialized = false;
        
        protected abstract string ReloadFragment { get; }
        protected abstract bool Persistent { get; }
        
        protected virtual string CacheModelsTableName {
            get { return "HyenaCacheModels"; }
        }
        
        protected virtual string CacheTableName {
            get { return "HyenaCache"; }
        }
        
        public CacheableDatabaseModelProvider(HyenaDbConnection connection, ICacheableModel model)
            : base(connection)
        {
            this.connection = connection;
            cache = new DatabaseCacheableModelProvider(model, this);
            
            CheckCacheTable();
            
            count_command = new HyenaDbCommand(String.Format(
                "SELECT COUNT(*) FROM {0} WHERE ModelID = ?", CacheTableName), 1);
            
            if(Persistent) {
                FindOrCreateCacheModelId(TableName);
            } else {
                uid = DatabaseCacheTable.ModelCount++;
            }
                
            select_range_command = new HyenaDbCommand(Where.Length > 0
                ? String.Format(@"
                                SELECT {0} FROM {1}
                                    INNER JOIN {2}
                                        ON {3} = {2}.ItemID
                                    WHERE
                                        {2}.ModelID = ? AND
                                        {4}
                                    LIMIT ?, ?", Select, From, CacheTableName, PrimaryKey, Where)
                : String.Format(@"
                                SELECT {0} FROM {1}
                                    INNER JOIN {2}
                                        ON {3} = {2}.ItemID
                                    WHERE
                                        {2}.ModelID = ?
                                    LIMIT ?, ?", Select, From, CacheTableName, PrimaryKey), 3);
            
            reload_sql = String.Format(@"
                DELETE FROM {0} WHERE ModelID = {1};
                    INSERT INTO {0} SELECT null, {1}, {2} ", CacheTableName, uid, PrimaryKey);
            
            if (!cache_initialized && !Persistent) {
                // Invalidate any old cache
                connection.Execute(String.Format("DELETE FROM {0}", CacheTableName));
            }
            cache_initialized = true;
        }
        
        private void CheckCacheTable()
        {
            using(IDataReader reader = connection.ExecuteReader(GetSchemaSql(CacheTableName))) {
                if(!reader.Read()) {
                    connection.Execute(String.Format(@"
                        CREATE TABLE {0} (
                            OrderID INTEGER PRIMARY KEY,
                            ModelID INTEGER,
                            ItemID INTEGER)", CacheTableName));
                }
            }
        }
        
        protected override void PrepareSelectRangeCommand(int offset, int limit)
        {
            SelectRangeCommand.ApplyValues(uid, offset, limit);
        }
        
        protected override HyenaDbCommand SelectRangeCommand {
            get { return select_range_command; }
        }
            
        protected virtual Dictionary<int, T> Cache {
            get { return cache.CacheImpl; }
        }
        
        public virtual T GetValue(int index)
        {
            return cache.GetValueImpl(index);
        }
        
        public virtual void Clear()
        {
            cache.ClearImpl();
        }
        
        protected void InvalidateManagedCache()
        {
            cache.InvalidateManagedCacheImpl();
        }
        
        protected virtual void Add(int key, T value)
        {
            cache.AddImpl(key, value);
        }
        
        protected virtual void FetchSet(int offset, int limit)
        {
            int i = offset;
            foreach(T item in FetchRange(offset, limit)) {
                if(!Cache.ContainsKey(i)) {
                    Add(i, item);
                }
                i++;
            }
        }
        
        public virtual int Reload()
        {
            InvalidateManagedCache ();
            connection.Execute(reload_sql + ReloadFragment);
            UpdateCount();
            return rows;
        }
        
        protected void UpdateCount()
        {
            //using (new Timer (String.Format ("Counting items for {0}", db_model))) {
                rows = Convert.ToInt32(connection.ExecuteScalar(count_command.ApplyValues(uid)));
            //}
        }
        
        private void FindOrCreateCacheModelId(string id)
        {
            using(IDataReader reader = connection.ExecuteReader(GetSchemaSql(CacheModelsTableName))) {
                if(reader.Read()) {
                    uid = connection.QueryInt32(String.Format(
                        "SELECT CacheID FROM {0} WHERE ModelID = '{1}'",
                        CacheModelsTableName, id
                    ));
                    if(uid == 0) {
                        //Console.WriteLine ("Didn't find existing cache for {0}, creating", id);
                        uid = connection.Execute(new HyenaDbCommand(String.Format(
                            "INSERT INTO {0} (ModelID) VALUES (?)",
                            CacheModelsTableName),
                            id
                        ));
                    } else {
                        //Console.WriteLine ("Found existing cache for {0}: {1}", id, uid);
                        warm = true;
                        InvalidateManagedCache();
                        UpdateCount();
                    }
                }
                else {
                    connection.Execute(String.Format(
                        "CREATE TABLE {0} (CacheID INTEGER PRIMARY KEY, ModelID TEXT UNIQUE)",
                        CacheModelsTableName));
                    uid = connection.Execute(new HyenaDbCommand(String.Format(
                        "INSERT INTO {0} (ModelID) VALUES (?)",
                        CacheModelsTableName),
                        id
                    ));
                }
            }
        }
        
        public bool Warm {
            //get { return warm; }
            get { return warm; }
        }

        public int Count {
            get { return rows; }
        }
        
        public int CacheId {
            get { return uid; }
        }
        
        public static implicit operator CacheableModelAdapter<T> (CacheableDatabaseModelProvider<T> cdmp)
        {
            return cdmp.cache;
        }
    }
}