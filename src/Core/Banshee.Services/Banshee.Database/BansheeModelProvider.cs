//
// BansheeModelProvider.cs
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

using Hyena.Data.Sqlite;

namespace Banshee.Database
{
    public interface IDatabaseItem
    {
        int DbIndex { set; }
    }
    
    public class BansheeModelProvider<T> : ModelProvider<T> where T : IDatabaseItem, new ()
    {
        private BansheeDbConnection connection;

        public BansheeModelProvider (BansheeDbConnection connection)
            : base (connection)
        {
            this.connection = connection;
            Init ();
        }
        
        protected override sealed int DatabaseVersion {
            get { return 1; }
        }
        
        protected override sealed void CheckVersion ()
        {
            using (IDataReader reader = connection.ExecuteReader (String.Format (
                "SELECT Value FROM CoreConfiguration WHERE Key = '{0}ModelVersion'", TableName))) {
                if (reader.Read ()) {
                    int version = Int32.Parse (reader.GetString (0));
                    if (version < ModelVersion) {
                        MigrateTable (version);
                        connection.Execute (String.Format (
                            "UPDATE CoreConfiguration SET Value = '{0}' WHERE Key = '{0}ModelVersion'",
                            ModelVersion, TableName));
                    }
                } else {
                    connection.Execute (String.Format (
                        "INSERT INTO CoreConfiguration (Key, Value) VALUES ('{0}ModelVersion', '{1}')",
                        TableName, ModelVersion));
                }
            }
            using (IDataReader reader = connection.ExecuteReader ("SELECT Value FROM CoreConfiguration WHERE Key = 'DatabaseVersion'")) {
                if (reader.Read ()) {
                    int version = Int32.Parse (reader.GetString (0));
                    if (version < DatabaseVersion) {
                        MigrateDatabase (version);
                        connection.Execute (String.Format (
                            "UPDATE CoreConfiguration SET Value = '{0}' WHERE Key = 'DatabaseVersion'",
                            DatabaseVersion));
                    }
                } else {
                    connection.Execute (String.Format (
                        "INSERT INTO CoreConfiguration (Key, Value) VALUES ('DatabaseVersion', '{0}')",
                        DatabaseVersion));
                }
            }
        }

        protected override sealed void MigrateDatabase (int old_version)
        {
        }
        
        protected override void MigrateTable (int old_version)
        {
        }
        
        protected override T MakeNewObject (int index)
        {
            T item = new T ();
            item.DbIndex = index;
            return item;
        }
    }
}
