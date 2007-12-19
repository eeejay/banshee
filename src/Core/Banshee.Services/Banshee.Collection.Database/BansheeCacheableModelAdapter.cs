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

        private static BansheeDbCommand count_command = new BansheeDbCommand ("SELECT COUNT(*) FROM CoreCache WHERE ModelID = ?", 1);
        private BansheeDbCommand select_command;

        private static bool cache_initialized = false;

        public BansheeCacheableModelAdapter (BansheeDbConnection connection, IDatabaseModel<T> model) 
            : base ((ICacheableModel) model)
        {
            this.db_model = model;
            this.connection = connection;

            uid = CacheTable.model_count++;

            reload_command = String.Format (@"
                DELETE FROM CoreCache WHERE ModelID = {0};
                    INSERT INTO CoreCache SELECT null, {0}, {1} ",
                uid, db_model.PrimaryKey
            );

            select_command = new BansheeDbCommand (
                String.Format (@"
                    SELECT {0} FROM {1}
                        INNER JOIN CoreCache
                            ON {2} = CoreCache.ItemID
                        WHERE
                            CoreCache.ModelID = ? AND
                            {3}
                            LIMIT ?, ?",
                    db_model.FetchColumns, db_model.FetchFrom, db_model.PrimaryKey, db_model.FetchCondition
                ), 3
            );

            if (!cache_initialized) {
                cache_initialized = true;
                // Invalidate any old cache
                connection.Execute ("DELETE FROM CoreCache");
            }
        }

        public override int Reload ()
        {
            InvalidateManagedCache ();

            using (new Timer (String.Format ("Generating cache table for {0}", db_model))) {
                connection.Execute (reload_command + db_model.ReloadFragment);
            }

            int rows;
            using (new Timer (String.Format ("Counting items for {0}", db_model))) {
                //Console.WriteLine("Count query: {0}", count_command);
                rows = Convert.ToInt32 (connection.ExecuteScalar (count_command.ApplyValues (uid)));
            }
            return rows;
        }

        protected override void FetchSet (int offset, int limit)
        {
            //using (new Timer (String.Format ("Fetching set for {0}", db_model))) {
                select_command.ApplyValues (uid, offset, limit);
                using (IDataReader reader = connection.ExecuteReader (select_command)) {
                    int i = offset;
                    T item;
                    while (reader.Read ()) {
                        if (!Cache.ContainsKey (i)) {
                            item = db_model.GetItemFromReader (reader, i);
                            Cache.Add (i, item);
                        }
                        i++;
                     }
                 }
            //}
        }

        public int CacheId {
            get { return uid; }
        }
    }
}
