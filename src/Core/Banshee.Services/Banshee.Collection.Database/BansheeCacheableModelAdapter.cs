//
// BansheeCacheableModelAdapter.cs
//
// Author:
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

using Hyena.Data;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    internal sealed class CacheTable
    {
        public static int model_count = 0;
    }

    public class BansheeCacheableModelAdapter<T> : CacheableModelAdapter<T>
    {
        private BansheeDbConnection connection;
        private IDatabaseModel<T> db_model;
        private int uid;
        private string reload_command;
        private string count_command;

        private static bool cache_initialized = false;

        public BansheeCacheableModelAdapter (BansheeDbConnection connection, IDatabaseModel<T> model) : base ((ICacheableModel) model)
        {
            this.db_model = model;
            this.connection = connection;

            uid = CacheTable.model_count++;

            count_command = String.Format ("SELECT COUNT(*) FROM CoreCache WHERE ModelID = {0}", uid);
            reload_command = String.Format (@"
                DELETE FROM CoreCache WHERE ModelID = {0};
                    INSERT INTO CoreCache SELECT null, {0}, {1} ",
                uid, db_model.PrimaryKey
            );

            if (!cache_initialized) {
                cache_initialized = true;
                // Invalidate any old cache
                IDbCommand command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CoreCache";
                command.ExecuteNonQuery();
            }
        }

        public override int Reload ()
        {
            InvalidateManagedCache ();

            using (new Timer (String.Format ("Generating cache table for {0}", db_model))) {
                IDbCommand command = connection.CreateCommand ();
                command.CommandText = reload_command + db_model.ReloadFragment;
                Console.WriteLine (command.CommandText);
                command.ExecuteNonQuery ();
            }

            int rows;
            using (new Timer (String.Format ("Counting tracks for {0}", db_model))) {
                Console.WriteLine("Count query: {0}", count_command);
                IDbCommand command = connection.CreateCommand ();
                command.CommandText = count_command;
                rows = Convert.ToInt32 (command.ExecuteScalar ());
            }
            return rows;
        }

        protected override void FetchSet (int offset, int limit)
        {
            string select_query = String.Format (@"
                SELECT {0}
                    FROM {1}
                    INNER JOIN CoreCache
                        ON {2} = CoreCache.ItemID
                    WHERE
                        CoreCache.ModelID = {3} AND
                        {4}
                        LIMIT {5}, {6}",
                db_model.FetchColumns, db_model.FetchFrom, db_model.PrimaryKey,
                uid, db_model.FetchCondition, offset, limit
            );

            using(new Timer(String.Format ("Loading {0} Set", db_model))) {
                IDbCommand command = connection.CreateCommand ();
                command.CommandText = select_query;
                Console.WriteLine (command.CommandText);
                IDataReader reader = command.ExecuteReader ();

                int i = offset;
                while (reader.Read()) {
                    if (!Cache.ContainsKey(i)) {
                        T item = db_model.GetItemFromReader (reader);
                        Cache.Add (i, item);
                    }
                    i++;
                }
            }
        }

        public int CacheId {
            get { return uid; }
        }
    }
}
