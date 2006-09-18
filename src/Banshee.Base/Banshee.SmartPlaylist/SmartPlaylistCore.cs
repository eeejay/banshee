using System;
using System.Data;
using System.Collections;
using Gtk;

using Mono.Unix;
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Plugins;

namespace Banshee.SmartPlaylist
{
    public class SmartPlaylistCore : Banshee.Plugins.Plugin
    {
        private ArrayList playlists = new ArrayList ();

        private Menu musicMenu;
        private MenuItem addItem;
        private MenuItem addFromSearchItem;

        private static SmartPlaylistCore instance = null;

        private uint timeout_id = 0;

        public static SmartPlaylistCore Instance {
            get { return instance; }
        }

        protected override string ConfigurationName {
            get { return "SmartPlaylists"; }
        }

        public override string DisplayName {
            get { return Catalog.GetString("Smart Playlists"); }
        }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Create playlists that automatically add and remove songs based on customizable queries."
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] {
                    "Aaron Bockover",
                    "Gabriel Burt",
                    "Dominik Meister"
                };
            }
        }

        public SmartPlaylistCore()
        {
            instance = this;
        }
 
        protected override void PluginInitialize()
        {
            Timer t = new Timer ("PluginInitialize");

            // Check that our SmartPlaylists table exists in the database, otherwise make it
            if(!Globals.Library.Db.TableExists("SmartPlaylists")) {
                Globals.Library.Db.Execute(@"
                    CREATE TABLE SmartPlaylists (
                        PlaylistID  INTEGER PRIMARY KEY,
                        Name        TEXT NOT NULL,
                        Condition   TEXT,
                        OrderBy     TEXT,
                        LimitNumber TEXT,
                        LimitCriterion INTEGER)
                ");
            } else {
                // Database Schema Updates
                try {
                    Globals.Library.Db.QuerySingle("SELECT LimitCriterion FROM SmartPlaylists LIMIT 1");
                } catch(ApplicationException) {
                    LogCore.Instance.PushDebug("Adding new database column", "LimitCriterion INTEGER");
                    Globals.Library.Db.Execute("ALTER TABLE SmartPlaylists ADD LimitCriterion INTEGER");
                    Globals.Library.Db.Execute("UPDATE SmartPlaylists SET LimitCriterion = 0");
                }
            }

            if(!Globals.Library.Db.TableExists("SmartPlaylistEntries")) {
                Globals.Library.Db.Execute(@"
                    CREATE TABLE SmartPlaylistEntries (
                        EntryID     INTEGER PRIMARY KEY,
                        PlaylistID  INTEGER NOT NULL,
                        TrackID     INTEGER NOT NULL)
                ");
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
            IDataReader reader = Globals.Library.Db.Query(String.Format(
                "SELECT PlaylistID, Name, Condition, OrderBy, LimitNumber, LimitCriterion FROM SmartPlaylists"
            ));

            while (reader.Read()) {
                SmartPlaylistSource.LoadFromReader (reader);
            }

            reader.Dispose();

            t.Stop();
        }

        protected override void InterfaceInitialize()
        {
            // Add a menu option to create a new smart playlist
            if(!Globals.UIManager.IsInitialized) {
                Globals.UIManager.Initialized += OnUIManagerInitialized;
            } else {
                OnUIManagerInitialized (null, null);
            }
        }

        private void OnUIManagerInitialized(object o, EventArgs args)
        {
            Timer t = new Timer ("OnUIManagerInitialized");

            musicMenu = (Globals.ActionManager.GetWidget ("/MainMenu/MusicMenu") as MenuItem).Submenu as Menu;
            addItem = new MenuItem (Catalog.GetString("New Smart Playlist..."));
            addItem.Activated += delegate {
                Editor ed = new Editor ();
                ed.RunDialog ();
            };

            addFromSearchItem = new MenuItem (Catalog.GetString("New Smart Playlist from Search..."));
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

        protected override void PluginDispose()
        {
            if (timeout_id != 0)
                GLib.Source.Remove (timeout_id);

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
        }

        /*public override Widget GetConfigurationWidget ()
        {
            return new ConfigPage (this);
        }*/

        private void HandleLibraryReloaded (object sender, EventArgs args)
        {
            //Console.WriteLine ("LibraryReloaded");
            // Listen for changes to any track to keep our playlists up to date
            IDataReader reader = Globals.Library.Db.Query(String.Format(
                "SELECT TrackID FROM Tracks"
            ));

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
            if (args.Source is PlaylistSource) {
                foreach (SmartPlaylistSource pl in playlists) {
                    if (pl.PlaylistDependent) {
                        pl.ListenToPlaylists();
                    }
                }
                return;
            }

            SmartPlaylistSource playlist = args.Source as SmartPlaylistSource;
            if (playlist == null)
                return;

            Timer t = new Timer ("HandleSourceAdded" + playlist.Name);

            StartTimer (playlist);
            
            playlists.Add(playlist);

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


        public void StartTimer (SmartPlaylistSource playlist)
        {
            // Check if the playlist is time-dependent, and if it is,
            // start the auto-refresh timer if needed.
            if (timeout_id == 0 && playlist.TimeDependent) {
                LogCore.Instance.PushInformation (
                        "Starting timer",
                        "Time-dependent smart playlist added, so starting auto-refresh timer.",
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
            Timer t = new Timer ("CheckTrack " + track.Title);

            foreach (SmartPlaylistSource playlist in playlists)
                playlist.Check (track);

            t.Stop();
        }
    }

    // Class used for timing different operations.  Commented out for normal operation.
    public class Timer
    {
        //DateTime time;
        //string name;

        public Timer () : this ("Timer") {}

        public Timer (string name)
        {
            //this.name = name;
            //time = DateTime.Now;

            //System.Console.WriteLine ("{0} started", name);
        }

        public void Stop ()
        {
            //System.Console.WriteLine ("{0} stopped: {1} seconds elapsed", name, (DateTime.Now - time).TotalSeconds);
        }
    }
}
