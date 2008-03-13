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
using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;
using Timer=Hyena.Timer;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection.Database;
using Banshee.Streaming;

namespace Banshee.Database
{
    public class BansheeDbFormatMigrator
    {
        
#region Migration Driver
        
        public delegate void SlowStartedHandler(string title, string message);
        
        public event SlowStartedHandler SlowStarted;
        public event EventHandler SlowPulse;
        public event EventHandler SlowFinished;

        public event EventHandler Started;
        public event EventHandler Finished;
        
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
        
        private HyenaSqliteConnection connection;
        
        public BansheeDbFormatMigrator (HyenaSqliteConnection connection)
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

        protected virtual void OnStarted ()
        {
            EventHandler handler = Started;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected virtual void OnFinished ()
        {
            EventHandler handler = Finished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public void Migrate()
        {
            try {
                Execute("BEGIN");
                InnerMigrate();
                Execute("COMMIT");
            } catch(Exception e) {
                Console.WriteLine("Rolling back transaction");
                Console.WriteLine(e);
                Execute("ROLLBACK");
            }

            OnFinished ();
        }
        
        private void InnerMigrate()
        {   
            MethodInfo [] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            bool terminate = false;
            bool ran_migration_step = false;
            
            for(int i = DatabaseVersion + 1; i <= CURRENT_VERSION; i++) {
                foreach(MethodInfo method in methods) {
                    foreach(Attribute attr in method.GetCustomAttributes(false)) {
                        if(attr is DatabaseVersionAttribute && ((DatabaseVersionAttribute)attr).Version == i) {
                            if (!ran_migration_step) {
                                ran_migration_step = true;
                                OnStarted ();
                            }

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
            return connection.TableExists (tableName);
        }
        
        protected void Execute(string query)
        {
            connection.Execute (query);
        }
            
        protected int DatabaseVersion {
            get {
                if(!TableExists("CoreConfiguration")) {
                    return 0;
                }
                
                string select_query = @"
                    SELECT Value 
                        FROM CoreConfiguration
                        WHERE Key = 'DatabaseVersion'
                ";
                
                return Convert.ToInt32 (connection.Query<int> (select_query));
            }
        }
        
#endregion
        
#region Migration Step Implementations
#pragma warning disable 0169
        // NOTE: Return true if the step should allow the driver to continue
        //       Return false if the step should terminate driver
        
        [DatabaseVersion (1)]
        private bool Migrate_1 ()
        {   
            if (TableExists("Tracks")) {
                InitializeFreshDatabase ();
                
                uint timer_id = Log.DebugTimerStart ("Database Schema Migration");

                OnSlowStarted (Catalog.GetString ("Upgrading your Banshee Database"), 
                    Catalog.GetString ("Please wait while your old Banshee database is migrated to the new format."));
            
                Thread thread = new Thread (MigrateFromLegacyBanshee);
                thread.Start ();
            
                while (thread.IsAlive) {
                    OnSlowPulse ();
                    Thread.Sleep (100);
                }

                Log.DebugTimerPrint (timer_id);
            
                OnSlowFinished ();
                
                return false;
            } else {
                InitializeFreshDatabase ();
                return false;
            }
        }   
        
#pragma warning restore 0169
        
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
            Execute("DROP TABLE IF EXISTS CoreRemovedTracks");
            Execute("DROP TABLE IF EXISTS CoreTracksCache");
            Execute("DROP TABLE IF EXISTS CoreCache");
            
            Execute(@"
                CREATE TABLE CoreConfiguration (
                    EntryID             INTEGER PRIMARY KEY,
                    Key                 TEXT,
                    Value               TEXT
                )
            ");
            
            Execute (String.Format (
                "INSERT INTO CoreConfiguration VALUES (null, 'DatabaseVersion', '{0}')",
                CURRENT_VERSION
            ));
            
            
            Execute(@"
                CREATE TABLE CorePrimarySources (
                    PrimarySourceID     INTEGER PRIMARY KEY,
                    StringID            TEXT UNIQUE
                )
            ");
            Execute ("INSERT INTO CorePrimarySources (StringID) VALUES ('Library')");

            // TODO add these:
            // Others to consider:
            // AlbumArtist (TPE2) (in CoreAlbums?)
            Execute(@"
                CREATE TABLE CoreTracks (
                    PrimarySourceID     INTEGER NOT NULL,
                    TrackID             INTEGER PRIMARY KEY,
                    ArtistID            INTEGER,
                    AlbumID             INTEGER,
                    TagSetID            INTEGER,
                    
                    MusicBrainzID       TEXT,

                    Uri                 TEXT,
                    UriType             INTEGER,
                    MimeType            TEXT,
                    FileSize            INTEGER,
                    
                    Title               TEXT,
                    TrackNumber         INTEGER,
                    TrackCount          INTEGER,
                    Disc                INTEGER,
                    Duration            INTEGER,
                    Year                INTEGER,
                    Genre               TEXT,
                    Composer            TEXT,
                    Copyright           TEXT,
                    LicenseUri          TEXT,

                    Comment             TEXT,
                    Rating              INTEGER,
                    PlayCount           INTEGER,
                    SkipCount           INTEGER,
                    LastPlayedStamp     INTEGER,
                    DateAddedStamp      INTEGER,
                    DateUpdatedStamp    INTEGER
                )
            ");
            Execute("CREATE INDEX CoreTracksPrimarySourceIndex ON CoreTracks(PrimarySourceID)");
            Execute("CREATE INDEX CoreTracksAggregatesIndex ON CoreTracks(FileSize, Duration)");
            Execute("CREATE INDEX CoreTracksArtistIndex ON CoreTracks(ArtistID)");
            Execute("CREATE INDEX CoreTracksAlbumIndex  ON CoreTracks(AlbumID)");
            Execute("CREATE INDEX CoreTracksRatingIndex ON CoreTracks(Rating)");
            Execute("CREATE INDEX CoreTracksLastPlayedStampIndex ON CoreTracks(LastPlayedStamp)");
            Execute("CREATE INDEX CoreTracksDateAddedStampIndex ON CoreTracks(DateAddedStamp)");
            Execute("CREATE INDEX CoreTracksPlayCountIndex ON CoreTracks(PlayCount)");
            Execute("CREATE INDEX CoreTracksDiscIndex ON CoreTracks(Disc)");
            Execute("CREATE INDEX CoreTracksTrackNumberIndex ON CoreTracks(TrackNumber)");
            Execute("CREATE INDEX CoreTracksTitleeIndex ON CoreTracks(Title)");
            
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
            Execute("CREATE INDEX CoreAlbumsIndex       ON CoreAlbums(Title)");
            Execute("CREATE INDEX CoreAlbumsArtistID    ON CoreAlbums(ArtistID)");

            Execute(@"
                CREATE TABLE CoreArtists (
                    ArtistID            INTEGER PRIMARY KEY,
                    TagSetID            INTEGER,
                    MusicBrainzID       TEXT,
                    Name                TEXT,
                    Rating              INTEGER
                )
            ");
            Execute("CREATE INDEX CoreArtistsIndex      ON CoreArtists(Name)");
            
            Execute(@"
                CREATE TABLE CorePlaylists (
                    PlaylistID          INTEGER PRIMARY KEY,
                    Name                TEXT,
                    SortColumn          INTEGER NOT NULL DEFAULT -1,
                    SortType            INTEGER NOT NULL DEFAULT 0,
                    Special             INTEGER NOT NULL DEFAULT 0
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
            Execute("CREATE INDEX CorePlaylistEntriesIndex ON CorePlaylistEntries(PlaylistID)");
            Execute("CREATE INDEX CorePlaylistTrackIDIndex ON CorePlaylistEntries(TrackID)");
            
            Execute(@"
                CREATE TABLE CoreSmartPlaylists (
                    SmartPlaylistID     INTEGER PRIMARY KEY,
                    Name                TEXT NOT NULL,
                    Condition           TEXT,
                    OrderBy             TEXT,
                    LimitNumber         TEXT,
                    LimitCriterion      TEXT
                )
            ");
                
            Execute(@"
                CREATE TABLE CoreSmartPlaylistEntries (
                    EntryID             INTEGER PRIMARY KEY,
                    SmartPlaylistID     INTEGER NOT NULL,
                    TrackID             INTEGER NOT NULL
                )
            ");
            Execute("CREATE INDEX CoreSmartPlaylistEntriesPlaylistIndex ON CoreSmartPlaylistEntries(SmartPlaylistID)");
            Execute("CREATE INDEX CoreSmartPlaylistEntriesTrackIndex ON CoreSmartPlaylistEntries(TrackID)");

            Execute(@"
                CREATE TABLE CoreRemovedTracks (
                    TrackID             INTEGER NOT NULL,
                    Uri                 TEXT,
                    DateRemovedStamp    INTEGER
                )
            ");

            Execute(@"
                CREATE TABLE CoreCacheModels (
                    CacheID             INTEGER PRIMARY KEY,
                    ModelID             TEXT
                )
            ");
            
            Execute(@"
                CREATE TABLE CoreCache (
                    OrderID             INTEGER PRIMARY KEY,
                    ModelID             INTEGER,
                    ItemID              INTEGER
                )
            ");
            // This index slows down queries were we shove data into the CoreCache.
            // Since we do that frequently, not using it.
            //Execute("CREATE INDEX CoreCacheModelId      ON CoreCache(ModelID)");
        }
        
        private void MigrateFromLegacyBanshee()
        {
            Thread.Sleep (3000);
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
                        1,
                        TrackID, 
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
                        0,
                        MimeType,
                        0,
                        Title,
                        TrackNumber,
                        TrackCount,
                        0,
                        Duration * 1000,
                        Year,
                        Genre,
                        NULL, NULL, NULL, NULL,
                        Rating,
                        NumberOfPlays,
                        0,
                        LastPlayedStamp,
                        DateAddedStamp,
                        DateAddedStamp
                        FROM Tracks
            ");

            Execute ("update coretracks set lastplayedstamp = NULL where lastplayedstamp = -62135575200");

            Execute(@"
                INSERT INTO CorePlaylists (PlaylistID, Name, SortColumn, SortType)
                    SELECT * FROM Playlists
            ");

            Execute(@"
                INSERT INTO CorePlaylistEntries
                    SELECT * FROM PlaylistEntries
            ");

            Execute(@"
                INSERT INTO CoreSmartPlaylists (SmartPlaylistID, Name, Condition, OrderBy, LimitNumber, LimitCriterion)
                    SELECT * FROM SmartPlaylists
            ");

            ServiceManager.ServiceStarted += OnServiceStarted;
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is UserJobManager) {
                ServiceManager.ServiceStarted -= OnServiceStarted;
                if (ServiceManager.SourceManager.Library != null) {
                    RefreshMetadataDelayed ();
                } else {
                    ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
                }
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (args.Source is Banshee.Library.LibrarySource) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
                RefreshMetadataDelayed ();
            }
        }

        private void RefreshMetadataDelayed ()
        {
            Application.RunTimeout (3000, RefreshMetadata);
        }

        private bool RefreshMetadata ()
        {
            ThreadPool.QueueUserWorkItem (RefreshMetadataThread);
            return false;
        }

        private void RefreshMetadataThread (object state)
        {
            int total = ServiceManager.DbConnection.Query<int> ("SELECT count(*) FROM CoreTracks WHERE PrimarySourceID = 1");

            if (total <= 0) {
                return;
            }

            UserJob job = new UserJob (Catalog.GetString ("Refreshing Metadata"));
            job.Status = Catalog.GetString ("Scanning...");
            job.IconNames = new string [] { "system-search", "gtk-find" };
            job.Register ();

            HyenaSqliteCommand select_command = new HyenaSqliteCommand (
                String.Format (
                    "SELECT {0} FROM {1} WHERE {2} AND CoreTracks.PrimarySourceID = 1",
                    DatabaseTrackInfo.Provider.Select,
                    DatabaseTrackInfo.Provider.From,
                    DatabaseTrackInfo.Provider.Where
                )
            );

            int count = 0;
            using (System.Data.IDataReader reader = ServiceManager.DbConnection.Query (select_command)) {
                while (reader.Read ()) {
                    DatabaseTrackInfo track = null;
                    try {
                        track = DatabaseTrackInfo.Provider.Load (reader, 0);
                        TagLib.File file = StreamTagger.ProcessUri (track.Uri);
                        StreamTagger.TrackInfoMerge (track, file, true);
                        track.Save (false);

                        job.Status = String.Format ("{0} - {1}", track.DisplayArtistName, track.DisplayTrackTitle);
                    } catch (Exception e) {
                        Log.Warning (String.Format ("Failed to update metadata for {0}", track),
                            e.GetType ().ToString (), false);
                    }

                    job.Progress = (double)++count / (double)total;
                }
            }

            job.Finish ();
            ServiceManager.SourceManager.Library.NotifyTracksChanged ();
        }
        
#endregion

    }
}
