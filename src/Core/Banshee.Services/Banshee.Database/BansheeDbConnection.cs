//
// BansheeDbConnection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;
using System.Data;
using System.Threading;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Database
{
    public sealed class BansheeDbConnection : HyenaSqliteConnection, IInitializeService, IRequiredService
    {
        private BansheeDbFormatMigrator migrator;

        public BansheeDbConnection () : base (DatabaseFile)
        {
            // Each cache page is about 1.5K, so 32768 pages = 49152K = 48M
            int cache_size = (TableExists ("CoreTracks") && Query<long> ("SELECT COUNT(*) FROM CoreTracks") > 10000) ? 32768 : 16384;
            Execute ("PRAGMA cache_size = ?", cache_size);
            Execute ("PRAGMA synchronous = OFF");
            Execute ("PRAGMA temp_store = MEMORY");
            Execute ("PRAGMA count_changes = OFF");

            // don't want this on because it breaks searching/smart playlists.  See BGO #526371
            //Execute ("PRAGMA case_sensitive_like=ON");

            Log.DebugFormat ("Opened SQLite connection to {0}", DatabaseFile);

            migrator = new BansheeDbFormatMigrator (this);
            
            if (Banshee.Base.ApplicationContext.CommandLine.Contains ("debug-sql")) {
                Hyena.Data.Sqlite.HyenaSqliteCommand.LogAll = true;
                WarnIfCalledFromThread = Banshee.Base.ThreadAssist.MainThread;
            }
        }

        void IInitializeService.Initialize ()
        {
            lock (this) {
                migrator.Migrate ();
                migrator = null;
            }
        }

        public BansheeDbFormatMigrator Migrator {
            get { lock (this) { return migrator; } }
        }

        public static string DatabaseFile {
            get {
                if (ApplicationContext.CommandLine.Contains ("db")) {
                    return ApplicationContext.CommandLine["db"];
                }
                
                string proper_dbfile = Path.Combine (Paths.ApplicationData, "banshee.db");
                if (File.Exists (proper_dbfile)) {
                    return proper_dbfile;
                }

                string dbfile = Path.Combine (Path.Combine (Environment.GetFolderPath (
                    Environment.SpecialFolder.ApplicationData), 
                    "banshee"), 
                    "banshee.db"); 

                if (!File.Exists (dbfile)) {
                    string tdbfile = Path.Combine (Path.Combine (Path.Combine (Environment.GetFolderPath (
                        Environment.SpecialFolder.Personal),
                        ".gnome2"),
                        "banshee"),
                        "banshee.db");

                    dbfile = tdbfile;
                }
                
                if (File.Exists (dbfile)) {
                    Log.InformationFormat ("Copying your old Banshee Database to {0}", proper_dbfile);
                    File.Copy (dbfile, proper_dbfile);
                }
                
                return proper_dbfile;
            }
        }

        string IService.ServiceName {
            get { return "DbConnection"; }
        }
    }
}
