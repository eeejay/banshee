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
using System.Data;

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    
    public abstract class BansheeModelProvider<T> : CacheableDatabaseModel<T>
    {
        private IDatabaseModel<T> model; // FIXME do away with this
        
        public BansheeModelProvider(BansheeDbConnection connection, IDatabaseModel<T> model)
            : base(connection, model)
        {
            this.model = model;
        }
        
        protected override sealed int DatabaseVersion {
            get { return 1; }
        }
        
        protected override sealed void CheckVersion()
        {
            using(IDataReader reader = Connection.ExecuteReader(String.Format(
                "SELECT Value FROM CoreConfiguration WHERE Key = '{0}ModelVersion'", TableName))) {
                if(reader.Read()) {
                    int version = Int32.Parse(reader.GetString(0));
                    if(version < ModelVersion) {
                        MigrateTable(version);
                        Connection.Execute(String.Format(
                            "UPDATE CoreConfiguration SET Value = '{0}' WHERE Key = '{0}ModelVersion'",
                            ModelVersion, TableName));
                    }
                } else {
                    Connection.Execute(String.Format(
                        "INSERT INTO CoreConfiguration (Key, Value) VALUES ('{0}ModelVersion', '{1}')",
                        TableName, ModelVersion));
                }
            }
            using(IDataReader reader = Connection.ExecuteReader("SELECT Value FROM CoreConfiguration WHERE Key = 'DatabaseVersion'")) {
                if(reader.Read()) {
                    int version = Int32.Parse(reader.GetString(0));
                    if(version < DatabaseVersion) {
                        MigrateDatabase(version);
                        Connection.Execute(String.Format(
                            "UPDATE CoreConfiguration SET Value = '{0}' WHERE Key = 'DatabaseVersion'",
                            DatabaseVersion));
                    }
                } else {
                    Connection.Execute(String.Format(
                        "INSERT INTO CoreConfiguration (Key, Value) VALUES ('DatabaseVersion', '{0}')",
                        DatabaseVersion));
                }
            }
        }

        
        protected override string ReloadFragment {
            get { return model.ReloadFragment; } // FIXME move this elsewhere
        }
        
        protected override sealed string CacheTableName {
            get { return "CoreCache"; }
        }
        
        protected override sealed bool Persistent {
            get { return false; }
        }
        
        protected override sealed string CacheModelsTableName {
            get { return "CoreCacheModels"; }
        }
        
        protected override sealed void MigrateDatabase(int old_version)
        {
        }
        
        protected override void MigrateTable(int old_version)
        {
        }
    }
}
