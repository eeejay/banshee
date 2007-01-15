using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Gtk;

using Mono.Unix;
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;

namespace Banshee.SmartPlaylist
{
    public class SmartPlaylistCore
    {
        private readonly double RATE_LIMIT_INTERVAL_MS = 1000.0;
        private readonly double RATE_LIMIT_EVENTS_MAX = 5;
        private readonly double RATE_LIMIT_CPU_MAX = 0.10;
        private static int RATE_LIMIT_REFRESH = 5;

        private ArrayList playlists = new ArrayList();

        private Menu musicMenu;
        private MenuItem addItem;
        private MenuItem addFromSearchItem;

        private DateTime last_check = DateTime.MinValue;
        private uint event_counter = 0;
        private bool rate_limited = false;
        private uint seconds_ratelimited = 0;
        private uint ratelimit_timeout_id = 0;
        private DateTime start;
        private uint timeout_id = 0;

        private static SmartPlaylistCore instance = null;
        public static SmartPlaylistCore Instance {
            get { 
                if(instance == null) {
                    instance = new SmartPlaylistCore();
                }
                
                return instance; 
            }
        }

        private double cpu_ms_since_last_check = 0;
        public double CpuTime {
            get { return cpu_ms_since_last_check; }
            set { cpu_ms_since_last_check = value; }
        }

        private SmartPlaylistCore()
        {
        }
        
        public void Initialize()
        {
            Gnome.Vfs.Vfs.Initialize();
            
            Timer t = new Timer("PluginInitialize");

            // Check that our SmartPlaylists table exists in the database, otherwise make it
            if(!Globals.Library.Db.TableExists("SmartPlaylists")) {
                CreateTable("SmartPlaylists");
            } else {
                // Database Schema Updates
                try {
                    Globals.Library.Db.QuerySingle("SELECT LimitCriterion FROM SmartPlaylists LIMIT 1");
                } catch {
                    LogCore.Instance.PushDebug("Adding new database column", "LimitCriterion INTEGER");
                    Globals.Library.Db.Execute("ALTER TABLE SmartPlaylists ADD LimitCriterion INTEGER");
                    Globals.Library.Db.Execute("UPDATE SmartPlaylists SET LimitCriterion = 0");
                }

                RenameColumn("SmartPlaylists", "PlaylistID", "SmartPlaylistID", "Name, Condition, OrderBy, LimitNumber, LimitCriterion");
            }

            if(!Globals.Library.Db.TableExists("SmartPlaylistEntries")) {
                CreateTable("SmartPlaylistEntries");
            } else {
                RenameColumn("SmartPlaylistEntries", "PlaylistID", "SmartPlaylistID", "TrackID");
            }

            // Listen for added/removed sources and added/changed songs
            SourceManager.SourceAdded += HandleSourceAdded;
            SourceManager.SourceRemoved += HandleSourceRemoved;

            if(Globals.Library.IsLoaded) {
                HandleLibraryReloaded (null, null);
            } else {
                Globals.Library.Reloaded += HandleLibraryReloaded;
            }
            Globals.Library.TrackAdded += HandleTrackAdded;
            Globals.Library.TrackRemoved += HandleTrackRemoved;

            // Load existing smart playlists
            IDataReader reader = Globals.Library.Db.Query(
                "SELECT SmartPlaylistID, Name, Condition, OrderBy, LimitNumber, LimitCriterion FROM SmartPlaylists"
            );

            while (reader.Read()) {
                try {
                    SmartPlaylistSource.LoadFromReader (reader);
                } catch (Exception e) {
                    LogCore.Instance.PushError (
                        "Invalid Smart Playlist",
                        e.ToString(),
                        false
                    );
                }
            }

            reader.Dispose();

            t.Stop();
            
            // Add a menu option to create a new smart playlist
            if(!Globals.UIManager.IsInitialized) {
                Globals.UIManager.Initialized += OnUIManagerInitialized;
            } else {
                OnUIManagerInitialized (null, null);
            }
        }

        private void CreateTable(string table)
        {
            switch (table) {
            case "SmartPlaylists":
                Globals.Library.Db.Execute(@"
                    CREATE TABLE SmartPlaylists (
                        SmartPlaylistID     INTEGER PRIMARY KEY,
                        Name                TEXT NOT NULL,
                        Condition           TEXT,
                        OrderBy             TEXT,
                        LimitNumber         TEXT,
                        LimitCriterion      INTEGER)
                ");
                break;

            case "SmartPlaylistEntries":
                Globals.Library.Db.Execute(@"
                    CREATE TABLE SmartPlaylistEntries (
                        SmartPlaylistID     INTEGER NOT NULL,
                        TrackID             INTEGER NOT NULL)
                ");
                break;
            }
        }

        private void OnUIManagerInitialized(object o, EventArgs args)
        {
            Timer t = new Timer ("OnUIManagerInitialized");

            musicMenu = (Globals.ActionManager.GetWidget ("/MainMenu/MusicMenu") as MenuItem).Submenu as Menu;
            addItem = new MenuItem (Catalog.GetString("New _Smart Playlist..."));
            addItem.Activated += delegate {
                Editor ed = new Editor ();
                ed.RunDialog ();
            };

            addFromSearchItem = new MenuItem (Catalog.GetString("New Smart Playlist _from Search..."));
            addFromSearchItem.Activated += delegate {
                Editor ed = new Editor ();
                ed.SetQueryFromSearch ();
                ed.RunDialog ();
            };

            // Insert it right after the New Playlist item
            musicMenu.Insert (addFromSearchItem, 2);
            musicMenu.Insert (addItem, 2);
            addFromSearchItem.Show ();
            addItem.Show ();

            t.Stop();
        }

        public void Dispose()
        {
            if (timeout_id != 0)
                GLib.Source.Remove (timeout_id);

            if (ratelimit_timeout_id != 0)
                GLib.Source.Remove (ratelimit_timeout_id);

            if (musicMenu != null) {
                musicMenu.Remove(addItem);
                musicMenu.Remove(addFromSearchItem);
            }

            SourceManager.SourceAdded -= HandleSourceAdded;
            SourceManager.SourceRemoved -= HandleSourceRemoved;

            foreach (SmartPlaylistSource playlist in playlists)
                LibrarySource.Instance.RemoveChildSource(playlist);

            playlists.Clear();

            instance = null;

            Timer.PrintRunningTotals ();
        }

        private void HandleLibraryReloaded (object sender, EventArgs args)
        {
            //Console.WriteLine ("LibraryReloaded");
            // Listen for changes to any track to keep our playlists up to date
            IDataReader reader = Globals.Library.Db.Query(
                "SELECT TrackID FROM Tracks"
            );

            while (reader.Read()) {
                LibraryTrackInfo track = Globals.Library.GetTrack (Convert.ToInt32(reader[0]));
                if (track != null)
                    track.Changed += HandleTrackChanged;
            }

            reader.Dispose();

            Globals.Library.Reloaded -= HandleLibraryReloaded;
        }

        private void HandleSourceAdded (SourceEventArgs args)
        {
            //Console.WriteLine ("source added: {0}", args.Source.Name);
            if (args.Source is PlaylistSource || args.Source is SmartPlaylistSource) {
                foreach (SmartPlaylistSource pl in playlists) {
                    if (pl.PlaylistDependent) {
                        pl.ListenToPlaylists();
                    }
                }
                
                if (args.Source is PlaylistSource)
                    return;
            }

            SmartPlaylistSource playlist = args.Source as SmartPlaylistSource;
            if (playlist == null)
                return;

            /*LogCore.Instance.PushInformation (
                    "Smart Playlist added",
                    "Smart playlist added to sources",
                    false
            );*/

            Timer t = new Timer ("HandleSourceAdded", playlist.Name);

            StartTimer (playlist);
            
            playlists.Add(playlist);
            SortPlaylists();

            t.Stop();
        }

        private void HandleSourceRemoved (SourceEventArgs args)
        {
            SmartPlaylistSource playlist = args.Source as SmartPlaylistSource;
            if (playlist == null)
                return;

            playlists.Remove (playlist);

            StopTimer();
        }

        private void HandleTrackAdded (object sender, LibraryTrackAddedArgs args)
        {
            args.Track.Changed += HandleTrackChanged;

            CheckTrack (args.Track);
        }

        private void HandleTrackChanged (object sender, EventArgs args)
        {
            TrackInfo track = sender as TrackInfo;

            if (track != null)
                CheckTrack (track);
        }

        private void HandleTrackRemoved (object sender, LibraryTrackRemovedArgs args)
        {
            foreach (TrackInfo track in args.Tracks)
                if (track != null)
                    track.Changed -= HandleTrackChanged;
        }

        public bool RateLimit ()
        {
            event_counter++;

            if (rate_limited)
                return true;

            bool retval = false;

            // Every Nth event make sure that the last N events didn't all occur in the last RATE_LIMIT_INTERVAL_MS
            if (event_counter == RATE_LIMIT_EVENTS_MAX) {
                double delta = (DateTime.Now - last_check).TotalMilliseconds;
                //Console.WriteLine ("{2} events in last {0} ms, CpuTime = {1}", delta, CpuTime, RATE_LIMIT_EVENTS_MAX);
                if (delta < RATE_LIMIT_INTERVAL_MS || (CpuTime > RATE_LIMIT_CPU_MAX*delta)) {
                    //Console.WriteLine ("rate limited");
                    rate_limited = true;
                    seconds_ratelimited = 0;
                    ratelimit_timeout_id = GLib.Timeout.Add((uint)RATE_LIMIT_INTERVAL_MS, OnRateLimitTimer);
                    retval = true;
                }

                event_counter = 0;
                last_check = DateTime.Now;
                CpuTime = 0;
            }

            return retval;
        }

        private bool OnRateLimitTimer ()
        {
            rate_limited = (event_counter >= RATE_LIMIT_EVENTS_MAX);

            //Console.WriteLine ("{0} events in last second", event_counter);
            // Refresh all the smart playlists every five seconds or when we are no longer rate limited.
            if (!rate_limited || seconds_ratelimited++ == RATE_LIMIT_REFRESH) {
                seconds_ratelimited = 0;

                start = DateTime.Now;
                foreach (SmartPlaylistSource pl in playlists) {
                    pl.RefreshMembers ();
                }

                // In the case the above refresh was very slow, double the time between refreshes
                // while rate-limited.
                if ((DateTime.Now - start).TotalSeconds > .25 * RATE_LIMIT_REFRESH) {
                    RATE_LIMIT_REFRESH *= 2;
                }
            }

            if (!rate_limited) {
                //Console.WriteLine ("NOT rate limited");
                last_check = DateTime.Now;
                ratelimit_timeout_id = 0;
            }

            CpuTime = 0;
            event_counter = 0;
            return rate_limited;
        }

        public void StartTimer (SmartPlaylistSource playlist)
        {
            // Check if the playlist is time-dependent, and if it is,
            // start the auto-refresh timer.
            if (timeout_id == 0 && playlist.TimeDependent) {
                LogCore.Instance.PushInformation (
                    "Starting Smart Playlist Auto-Refresh",
                    "Time-dependent smart playlist added, so starting one-minute auto-refresh timer.",
                    false
                );
                timeout_id = GLib.Timeout.Add(1000*60, OnTimerBeep);
            }
        }

        public void StopTimer ()
        {
            // If the timer is going and there are no more time-dependent playlists,
            // stop the timer.
            if (timeout_id != 0) {
                foreach (SmartPlaylistSource p in playlists) {
                    if (p.TimeDependent) {
                        return;
                    }
                }

                // No more time-dependent playlists, so remove the timer
                LogCore.Instance.PushInformation (
                    "Stopping timer",
                    "There are no time-dependent smart playlists, so stopping auto-refresh timer.",
                    false
                );

                GLib.Source.Remove (timeout_id);
                timeout_id = 0;
            }
        }

        private bool OnTimerBeep ()
        {
            if (RateLimit())
                return true;

            Timer t = new Timer ("OnTimerBeep");

            foreach (SmartPlaylistSource p in playlists) {
                if (p.TimeDependent) {
                    p.RefreshMembers();
                }
            }

            t.Stop ();

            // Keep the timer going
            return true;
        }

        private void CheckTrack (TrackInfo track)
        {
            if (RateLimit())
                return;

            Timer t = new Timer ("CheckTrack");
            start = DateTime.Now;

            foreach (SmartPlaylistSource playlist in playlists)
                playlist.Check (track);

            CpuTime += (DateTime.Now - start).TotalMilliseconds;

            t.Stop();
        }

        public void SortPlaylists () {
            playlists.Sort(new DependencyComparer());
        }

        public void RenameColumn(string table, string old_name, string new_name, string other_columns)
        {
            try {
                Globals.Library.Db.QuerySingle(String.Format("SELECT {0} FROM {1} LIMIT 1", new_name, table));
            } catch {
                LogCore.Instance.PushDebug(String.Format("Renaming column {0} in {1}", old_name, table), "");
                Globals.Library.Db.Execute(String.Format("ALTER TABLE {0} RENAME TO {0}_tmp", table));
                CreateTable(table);
                Globals.Library.Db.Execute(String.Format("INSERT INTO {0} SELECT {1} as {2}, {3} FROM {0}_tmp", table, old_name, new_name, other_columns));
                Globals.Library.Db.Execute(String.Format("DROP TABLE {0}_tmp", table));
            }
        }
    }


    // Class used for timing different operations.  Commented out for normal operation.
    public class Timer
    {
        //DateTime time;
        //string name;
        //string details;

        //static Dictionary<string, double> running_totals = new Dictionary<string, double>();
        //static Dictionary<string, int> running_counts = new Dictionary<string, int>();

        public Timer () : this ("Timer") {}

        public Timer (string name) : this (name, null) {}

        public Timer (string name, string details)
        {
            /*this.name = name;
            this.details = (details == null) ? "" : " (" + details + ")";

            if (!running_totals.ContainsKey(name)) {
                running_totals.Add(name, 0);
                running_counts.Add(name, 0);
            }

            time = DateTime.Now;*/

            //System.Console.WriteLine ("{0} started", name);
        }

        public void Stop ()
        {
            /*double elapsed = (DateTime.Now - time).TotalSeconds;
            System.Console.WriteLine ("{0}{1} stopped: {2} seconds elapsed", name, details, elapsed);
            running_totals[name] += elapsed;
            //running_totals[name+details] += elapsed;
            running_counts[name]++;
            //running_counts[name+details]++;*/
        }

        public static void PrintRunningTotals ()
        {
            /*Console.WriteLine("Running totals:");
            foreach (string k in running_totals.Keys) {
                //if (running_totals[k] > .1) {
                    Console.WriteLine("{0}, {1}, {2}", k, running_counts[k], running_totals[k]);
                //}
            }*/
        }
    }

    public class DependencyComparer : IComparer {
        public int Compare(object ao, object bo)
        {
            SmartPlaylistSource a = ao as SmartPlaylistSource;
            SmartPlaylistSource b = bo as SmartPlaylistSource;

            if (b.DependsOn(a)) {
                return -1;
            } else if (a.DependsOn(b)) {
                return 1;
            } else {
                return 0;
            }
        }
    }
}
