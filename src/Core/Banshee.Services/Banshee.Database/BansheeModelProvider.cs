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

using Banshee.Configuration;

namespace Banshee.Database
{
    public class BansheeModelProvider<T> : SqliteModelProvider<T> where T : new ()
    {
        public BansheeModelProvider (BansheeDbConnection connection, string table_name)
            : base (connection, table_name)
        {
        }
        
        protected override sealed void CheckVersion ()
        {
            CheckVersion (TableName, "ModelVersion", ModelVersion, MigrateTable);
            CheckVersion ("Database", "Version", DatabaseVersion, MigrateDatabase);
        }
        
        private delegate void MigrateDel (int version);
        
        private static void CheckVersion (string namespce, string key, int new_version, MigrateDel func)
        {
            int old_version = DatabaseConfigurationClient.Client.Get <int> (
                namespce, key, -1);
            if (old_version != -1 && old_version < new_version) {
                func (old_version);
            }
            if (old_version != new_version) {
                DatabaseConfigurationClient.Client.Set <int> (
                    namespce, key, new_version);
            }
        }
    }
}
