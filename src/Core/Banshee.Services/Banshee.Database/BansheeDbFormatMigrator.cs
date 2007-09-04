//
// BansheeDbFormatMigrator.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Reflection;
using System.Threading;

namespace Banshee.Database
{
    public class BansheeDbFormatMigrator
    {
        
#region Migration Driver
        
        public delegate void SlowStartedHandler(string title, string message);
        
        public event SlowStartedHandler SlowStarted;
        public event EventHandler SlowPulse;
        public event EventHandler SlowFinished;
        
        // NOTE: Whenever there is a change in ANY of the database schema,
        //       this version MUST be incremented and a migration method
        //       MUST be supplied to match the new version number
        protected const int CURRENT_VERSION = 1;
        
        protected class DatabaseVersionAttribute : Attribute 
        {
            private int version;
            
            public DatabaseVersionAttribute(int version)
            {
                this.version = version;
            }
            
            public int Version {
                get { return version; }
            }
        }
        
        private IDbConnection connection;
        
        public BansheeDbFormatMigrator(IDbConnection connection)
        {
            this.connection = connection;
        }
        
        protected virtual void OnSlowStarted(string title, string message)
        {
            SlowStartedHandler handler = SlowStarted;
            if(handler != null) {
                handler(title, message);
            }
        }
        
        protected virtual void OnSlowPulse()
        {
            EventHandler handler = SlowPulse;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnSlowFinished()
        {
            EventHandler handler = SlowFinished;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        public void Migrate()
        {
            try {
                Execute("BEGIN");
                InnerMigrate();
                Execute("COMMIT");
            } catch(Exception) {
                Console.WriteLine("Rolling back transaction");
                Execute("ROLLBACK");
            }
        }
        
        private void InnerMigrate()
        {
            // HACK: Just to make things easy while I'm writing the migration code
            Execute("DROP TABLE IF EXISTS CoreConfiguration");
            Execute("DROP TABLE IF EXISTS CoreTracks");
            Execute("DROP TABLE IF EXISTS CoreArtists");
            Execute("DROP TABLE IF EXISTS CoreAlbums");
            Execute("DROP TABLE IF EXISTS CorePlaylists");
            Execute("DROP TABLE IF EXISTS CorePlaylistEntries");
            Execute("DROP TABLE IF EXISTS CoreSmartPlaylists");
            Execute("DROP TABLE IF EXISTS CoreSmartPlaylistEntries");
            Execute("DROP TABLE IF EXISTS CoreTracksCache");
            
            MethodInfo [] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            bool terminate = false;
            
            for(int i = DatabaseVersion + 1; i <= CURRENT_VERSION; i++) {
                foreach(MethodInfo method in methods) {
                    foreach(Attribute attr in method.GetCustomAttributes(false)) {
                        if(attr is DatabaseVersionAttribute && ((DatabaseVersionAttribute)attr).Version == i) {
                            if(!(bool)method.Invoke(this, null)) {
                                terminate = true;
                            }
                            break;
                        }
                    }
                }
                
                if(terminate) {
                    break;
                }
            }
        }
        
        protected bool TableExists(string tableName)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                    FROM sqlite_master
                    WHERE Type='table' AND Name=:table_name";
            
            IDbDataParameter table_param = command.CreateParameter();
            table_param.ParameterName = "table_name";
            table_param.Value = tableName;
            
            command.Parameters.Add(table_param);
            
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }
        
        protected void Execute(string query)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            command.ExecuteNonQuery();
        }
            
        protected int DatabaseVersion {
            get {
                if(!TableExists("CoreConfiguration")) {
                    return 0;
                }
                
                IDbCommand command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Value 
                        FROM CoreConfiguration
                        WHERE Key = 'DatabaseVersion'
                ";
                
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }
        
#endregion
        
#region Migration Step Implementations
        
        // NOTE: Return true if the step should allow the driver to continue
        //       Return false if the step should terminate driver
        
        [DatabaseVersion(1)]
        private bool Migrate_1()
        {   
            if(TableExists("Tracks")) {
                InitializeFreshDatabase();
                
                using(new Timer("Database Migration")) {
                    OnSlowStarted("Upgrading your Banshee Database", 
                        "This operation may take a few minutes, but the wait will be well worth it!");
                
                    Thread thread = new Thread(MigrateFromLegacyBanshee);
                    thread.Start();
                
                    while(thread.IsAlive) {
                        OnSlowPulse();
                        Thread.Sleep(100);
                    }
                
                    OnSlowFinished();
                }
                
                return false;
            } else {
                InitializeFreshDatabase();
                return false;
            }
        }   
        
        private void InitializeFreshDatabase()
        {
            Execute("DROP TABLE IF EXISTS CoreConfiguration");
            Execute("DROP TABLE IF EXISTS CoreTracks");
            Execute("DROP TABLE IF EXISTS CoreArtists");
            Execute("DROP TABLE IF EXISTS CoreAlbums");
            Execute("DROP TABLE IF EXISTS CorePlaylists");
            Execute("DROP TABLE IF EXISTS CorePlaylistEntries");
            Execute("DROP TABLE IF EXISTS CoreSmartPlaylists");
            Execute("DROP TABLE IF EXISTS CoreSmartPlaylistEntries");
            Execute("DROP TABLE IF EXISTS CoreTracksCache");
            
            Execute(@"
                CREATE TABLE CoreConfiguration (
                    EntryID             INTEGER PRIMARY KEY,
                    Key                 TEXT,
                    Value               TEXT
                )
            ");
            
            Execute(String.Format(@"
                INSERT INTO CoreConfiguration 
                    VALUES (null, 'DatabaseVersion', '{0}')
            ", CURRENT_VERSION));
            
            Execute(@"
                CREATE TABLE CoreTracks (
                    TrackID             INTEGER PRIMARY KEY,
                    ArtistID            INTEGER,
                    AlbumID             INTEGER,
                    TagSetID            INTEGER,
                    
                    MusicBrainzID       TEXT,

                    RelativeUri         TEXT,
                    MimeType            TEXT,
                    
                    Title               TEXT,
                    TrackNumber         INTEGER,
                    TrackCount          INTEGER,
                    Duration            INTEGER,
                    Year                INTEGER,

                    Rating              INTEGER,
                    PlayCount           INTEGER,
                    LastPlayedStamp     INTEGER,
                    DateAddedStamp      INTEGER
                )
            ");
            
            Execute(@"
                CREATE TABLE CoreAlbums (
                    AlbumID             INTEGER PRIMARY KEY,
                    ArtistID            INTEGER,
                    TagSetID            INTEGER,
                    
                    MusicBrainzID       TEXT,

                    Title               TEXT,

                    ReleaseDate         INTEGER,
                    Duration            INTEGER,
                    Year                INTEGER,
                    
                    Rating              INTEGER
                )
            ");
            
            Execute(@"
                CREATE TABLE CoreArtists (
                    ArtistID            INTEGER PRIMARY KEY,
                    TagSetID            INTEGER,
                    MusicBrainzID       TEXT,
                    Name                TEXT,
                    Rating              INTEGER
                )
            ");
            
            Execute(@"
                CREATE TABLE CorePlaylists (
                    PlaylistID          INTEGER PRIMARY KEY,
                    Name                TEXT,
                    SortColumn          INTEGER NOT NULL DEFAULT -1,
                    SortType            INTEGER NOT NULL DEFAULT 0
                )
            ");
            
            Execute(@"
                CREATE TABLE CorePlaylistEntries (
                    EntryID             INTEGER PRIMARY KEY,
                    PlaylistID          INTEGER NOT NULL,
                    TrackID             INTEGER NOT NULL,
                    ViewOrder           INTEGER NOT NULL DEFAULT 0
                )
            ");
            
            Execute(@"
                CREATE TABLE CoreSmartPlaylists (
                    SmartPlaylistID     INTEGER PRIMARY KEY,
                    Name                TEXT NOT NULL,
                    Condition           TEXT,
                    OrderBy             TEXT,
                    LimitNumber         TEXT,
                    LimitCriterion      INTEGER
                )
            ");
                
            Execute(@"
                CREATE TABLE CoreSmartPlaylistEntries (
                    SmartPlaylistID     INTEGER NOT NULL,
                    TrackID             INTEGER NOT NULL
                )
            ");
            
            Execute(@"
                CREATE TABLE CoreTracksCache (
                    OrderID             INTEGER PRIMARY KEY,
                    TableID             INTEGER,
                    ID                  INTEGER
                )
            ");

            Execute("CREATE INDEX CoreTracksArtistIndex ON CoreTracks(ArtistID)");
            Execute("CREATE INDEX CoreTracksAlbumIndex  ON CoreTracks(AlbumID)");
            Execute("CREATE INDEX CoreArtistsIndex      ON CoreArtists(Name)");
            Execute("CREATE INDEX CoreAlbumsIndex       ON CoreAlbums(Title)");
        }
        
        private void MigrateFromLegacyBanshee()
        {
            Execute(@"
                INSERT INTO CoreArtists 
                    SELECT DISTINCT null, 0, null, Artist, 0 
                        FROM Tracks 
                        ORDER BY Artist
            ");
            
            Execute(@"
                INSERT INTO CoreAlbums
                    SELECT DISTINCT null,
                        (SELECT ArtistID 
                            FROM CoreArtists 
                            WHERE Name = Tracks.Artist
                            LIMIT 1),
                        0, null, AlbumTitle, ReleaseDate, 0, 0, 0
                        FROM Tracks
                        ORDER BY AlbumTitle
            ");
            
            Execute(@"
                INSERT INTO CoreTracks
                    SELECT 
                        null, 
                        (SELECT ArtistID 
                            FROM CoreArtists 
                            WHERE Name = Artist),
                        (SELECT a.AlbumID 
                            FROM CoreAlbums a, CoreArtists b
                            WHERE a.Title = AlbumTitle 
                                AND a.ArtistID = b.ArtistID
                                AND b.Name = Artist),
                        0,
                        null,
                        Uri,
                        MimeType,
                        Title,
                        TrackNumber,
                        TrackCount,
                        Duration,
                        Year,
                        Rating,
                        NumberOfPlays,
                        DateAddedStamp,
                        LastPlayedStamp
                        FROM Tracks
            ");
        }
        
#endregion
    }
}
