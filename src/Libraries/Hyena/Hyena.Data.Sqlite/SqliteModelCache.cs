//
// SqliteModelCache.cs
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
    public class SqliteModelCache<T> : DictionaryModelCache<T>
    {
        private HyenaSqliteConnection connection;
        private ICacheableDatabaseModel model;
        private SqliteModelProvider<T> provider;
        private HyenaSqliteCommand select_range_command;
        private HyenaSqliteCommand select_single_command;
        private HyenaSqliteCommand select_first_command;
        private HyenaSqliteCommand count_command;

        private string reload_sql;
        private int uid;
        private int rows;
        // private bool warm;
        private int first_order_id;

        public delegate void AggregatesUpdatedEventHandler (IDataReader reader);
        public event AggregatesUpdatedEventHandler AggregatesUpdated;

        public SqliteModelCache (HyenaSqliteConnection connection,
                           string uuid,
                           ICacheableDatabaseModel model,
                           SqliteModelProvider<T> provider)
            : base (model)
        {
            this.connection = connection;
            this.model = model;
            this.provider = provider;
            
            CheckCacheTable ();

            if (model.SelectAggregates != null) {
                count_command = new HyenaSqliteCommand (String.Format (@"
                    SELECT COUNT(*), {0} FROM {1} WHERE {2}
                        IN (SELECT ItemID FROM {3} WHERE ModelID = ?)",
                    model.SelectAggregates, provider.TableName, provider.PrimaryKey, CacheTableName
                ));
            } else {
                count_command = new HyenaSqliteCommand (String.Format (
                    "SELECT COUNT(*) FROM {0} WHERE ModelID = ?", CacheTableName
                ));
            }

            FindOrCreateCacheModelId (String.Format ("{0}-{1}", uuid, typeof(T).Name));

            select_range_command = new HyenaSqliteCommand (
                String.Format (@"
                    SELECT {0} FROM {1}
                        INNER JOIN {2}
                            ON {3} = {2}.ItemID
                        WHERE
                            {2}.ModelID = {4} {5}
                            {6}
                        LIMIT ?, ?",
                    provider.Select, provider.From, CacheTableName,
                    provider.PrimaryKey, uid,
                    String.IsNullOrEmpty (provider.Where) ? String.Empty : "AND",
                    provider.Where
                )
            );
            
            select_single_command = new HyenaSqliteCommand (
                String.Format (@"
                    SELECT OrderID FROM {0}
                        WHERE
                            ModelID = {1} AND
                            ItemID = ?",
                    CacheTableName, uid
                )
            );
            
            select_first_command = new HyenaSqliteCommand (
                String.Format (
                    "SELECT OrderID FROM {0} WHERE ModelID = {1} LIMIT 1",
                    CacheTableName, uid
                )
            );

            reload_sql = String.Format (@"
                DELETE FROM {0} WHERE ModelID = {1};
                    INSERT INTO {0} SELECT null, {1}, {2} ",
                CacheTableName, uid, provider.PrimaryKey
            );
        }

        public bool Warm {
            //get { return warm; }
            get { return false; }
        }

        public int Count {
            get { return rows; }
        }

        public int CacheId {
            get { return uid; }
        }

        protected virtual string CacheModelsTableName {
            get { return "HyenaCacheModels"; }
        }
        
        protected virtual string CacheTableName {
            get { return "HyenaCache"; }
        }
        
        public int IndexOf (int item_id)
        {
            if (rows == 0) {
                return -1;
            }
            select_single_command.ApplyValues (item_id);
            int target_id = connection.QueryInt32 (select_single_command);
            if (target_id == 0) {
                return -1;
            }
            if (first_order_id == -1) {
                first_order_id = connection.QueryInt32 (select_first_command);
            }
            return target_id - first_order_id;
        }

        public override int Reload ()
        {
            Clear ();
            //using (new Timer (String.Format ("Generating cache table for {0}", model))) {
                connection.Execute (reload_sql + model.ReloadFragment);
            //}
            first_order_id = -1;
            UpdateAggregates ();
            return rows;
        }

        protected override void FetchSet (int offset, int limit)
        {
            //using (new Timer (String.Format ("Fetching set for {0}", model))) {
                select_range_command.ApplyValues (offset, limit);
                using (IDataReader reader = connection.ExecuteReader (select_range_command)) {
                    while (reader.Read ()) {
                        if (!ContainsKey (offset)) {
                            Add (offset, provider.Load (reader, offset));
                        }
                        offset++;
                     }
                 }
            //}
        }
        
        protected void UpdateAggregates ()
        {
            using (IDataReader reader = connection.ExecuteReader (count_command.ApplyValues (uid))) {
                rows = Convert.ToInt32 (reader[0]);

                AggregatesUpdatedEventHandler handler = AggregatesUpdated;
                if (handler != null) {
                    handler (reader);
                }
            }
        }
        
        private void FindOrCreateCacheModelId (string id)
        {
            uid = connection.QueryInt32 (String.Format (
                "SELECT CacheID FROM {0} WHERE ModelID = '{1}'",
                CacheModelsTableName, id
            ));

            if (uid == 0) {
                //Console.WriteLine ("Didn't find existing cache for {0}, creating", id);
                uid = connection.Execute (new HyenaSqliteCommand (String.Format (
                    "INSERT INTO {0} (ModelID) VALUES (?)", CacheModelsTableName
                    ), id
                ));
            } else {
                //Console.WriteLine ("Found existing cache for {0}: {1}", id, uid);
                //warm = true;
                Clear ();
                UpdateAggregates ();
            }
        }

        private void CheckCacheTable ()
        {
            if (!connection.TableExists (CacheTableName)) {
                connection.Execute (String.Format (@"
                    CREATE TABLE {0} (
                        OrderID INTEGER PRIMARY KEY,
                        ModelID INTEGER,
                        ItemID INTEGER)", CacheTableName
                ));
            }

            if (!connection.TableExists (CacheModelsTableName)) {
                connection.Execute (String.Format (
                    "CREATE TABLE {0} (CacheID INTEGER PRIMARY KEY, ModelID TEXT UNIQUE)",
                    CacheModelsTableName
                ));
            }
        }
    }
}
