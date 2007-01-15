using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;

using Mono.Unix;

namespace Banshee.SmartPlaylist
{
    public class SmartPlaylistSource : Banshee.Sources.ChildSource
    {
        private List<TrackInfo> tracks = new List<TrackInfo>();
        private ArrayList watchedPlaylists = new ArrayList();

        public string Condition;
        public string OrderBy;
        public string LimitNumber;
        public int LimitCriterion;

        private string OrderAndLimit {
            get {
                if (OrderBy == null || OrderBy == "")
                    return null;

                if (LimitCriterion == 0)
                    return String.Format ("ORDER BY {0} LIMIT {1}", OrderBy, LimitNumber);
                else
                    return String.Format ("ORDER BY {0}", OrderBy);
            }
        }

        public bool TimeDependent {
            get {
                bool condition_is = (Condition == null) ? false : Condition.IndexOf ("current_timestamp") != -1;
                bool order_is = (OrderBy == null) ? false : OrderBy.IndexOf ("Stamp") != -1;
                return condition_is || order_is;
            }
        }

        public bool PlaylistDependent {
            get { return (Condition != null && Condition.IndexOf("PlaylistID") != -1); }
        }

        public List<SmartPlaylistSource> DependedOnBy {
            get {
                List<SmartPlaylistSource> list = new List<SmartPlaylistSource>();
                foreach (Banshee.Sources.Source src in SourceManager.Sources) {
                    SmartPlaylistSource pl = src as SmartPlaylistSource;
                    if (pl != null) {
                        if (pl.DependsOn(this)) {
                            list.Add(src as SmartPlaylistSource);
                        }
                    }
                }
                return list;
            }
        }

        public bool DependsOn(PlaylistSource pl)
        {
            return (Condition != null && Condition.IndexOf(String.Format(" PlaylistID = {0}", pl.Id)) != -1);
        }

        // Recursively figure out if this playlist depends on another one
        public bool DependsOn(SmartPlaylistSource other_sp)
        {
            return DependsOn(other_sp, true);
        }

        public bool DependsOn(SmartPlaylistSource other_sp, bool recurse)
        {
            if (other_sp == this)
                return false;

            bool ret = false;
            ret |= (Condition != null && Condition.IndexOf(String.Format("SmartPlaylistID = {0}", other_sp.Id)) != -1);

            if (recurse) {
                foreach (Banshee.Sources.Source source in watchedPlaylists) {
                    if (source is SmartPlaylistSource) {
                        ret |= (source as SmartPlaylistSource).DependsOn(other_sp);
                    }
                }
            }

            return ret;
        }

        private int id;
        public int Id {
            get { return id; }
            set { id = value; }
        }

        public override int Count {
            get { return tracks.Count; }
        }
        
        public override IEnumerable<TrackInfo> Tracks {
            get { return tracks; }
        }

        private static Gdk.Pixbuf icon = Gdk.Pixbuf.LoadFromResource("source-smart-playlist.png");
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }

        public override object TracksMutex {
            get { return ((IList)tracks).SyncRoot; }
        }

        private static string unmap_label = Catalog.GetString ("Delete Smart Playlist");
        public override string UnmapLabel {
            get { return unmap_label; }
        }
        
        private static string properties_label = Catalog.GetString ("Edit Smart Playlist...");
        public override string SourcePropertiesLabel {
            get { return properties_label; }
        }
        
        private static string generic_name = Catalog.GetString ("Smart Playlist");
        public override string GenericName {
            get { return generic_name; }
        }

        // For existing smart playlists that we're loading from the database
        public SmartPlaylistSource(int id, string name, string condition, string order_by, string limit_number, int limit_criterion) : base(name, 100)
        {
            Id = id;
            Name = name;
            Condition = condition;
            OrderBy = order_by;
            LimitNumber = limit_number;
            LimitCriterion = limit_criterion;

            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;

            if (Globals.Library.IsLoaded)
                OnLibraryReloaded(Globals.Library, new EventArgs());
            else
                Globals.Library.Reloaded += OnLibraryReloaded;

            ListenToPlaylists();
        }

        // For new smart playlists
        public SmartPlaylistSource(string name, string condition, string order_by, string limit_number, int limit_criterion) : base(name, 100)
        {
            Name = name;
            Condition = condition;
            OrderBy = order_by;
            LimitNumber = limit_number;
            LimitCriterion = limit_criterion;

            DbCommand command = new DbCommand(@"
                INSERT INTO SmartPlaylists
                    (Name, Condition, OrderBy, LimitNumber, LimitCriterion)
                    VALUES (:name, :condition, :orderby, :limitnumber, :limitcriterion)",
                    "name", Name, 
                    "condition", Condition, 
                    "orderby", OrderBy, 
                    "limitnumber", LimitNumber, 
                    "limitcriterion", LimitCriterion
            );

            Id = Globals.Library.Db.Execute(command);

            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;

            if (Globals.Library.IsLoaded)
                OnLibraryReloaded(Globals.Library, new EventArgs());
            else
                Globals.Library.Reloaded += OnLibraryReloaded;

            ListenToPlaylists();
        }

        public void ListenToPlaylists()
        {
            // First, stop listening to any/all playlists
            foreach (Banshee.Sources.Source source in watchedPlaylists) {
                source.TrackAdded -= HandlePlaylistChanged;
                source.TrackRemoved -= HandlePlaylistChanged;
            }

            watchedPlaylists.Clear();

            if (PlaylistDependent) {
                foreach (Banshee.Sources.Source source in SourceManager.Sources) {
                    if ((source is PlaylistSource && DependsOn(source as PlaylistSource)) ||
                        (source is SmartPlaylistSource && DependsOn(source as SmartPlaylistSource, false)))
                    {
                        source.TrackAdded += HandlePlaylistChanged;
                        source.TrackRemoved += HandlePlaylistChanged;
                        watchedPlaylists.Add(source);
                    }
                }
            }
        }

        public void RefreshMembers()
        {
            Timer t = new Timer ("RefreshMembers", Name);

            //Console.WriteLine ("Refreshing smart playlist {0} with condition {1}", Source.Name, Condition);

            // Delete existing tracks
            Globals.Library.Db.Execute(new DbCommand(
                "DELETE FROM SmartPlaylistEntries WHERE SmartPlaylistID = :playlist_id",
                "playlist_id", Id
            ));

            // Add matching tracks
            Globals.Library.Db.Execute(String.Format(
                @"INSERT INTO SmartPlaylistEntries 
                    SELECT {0} as SmartPlaylistID, TrackId FROM Tracks {1} {2}",
                    Id, PrependCondition("WHERE"), OrderAndLimit
            ));

            // Load the new tracks in
            IDataReader reader = Globals.Library.Db.Query(new DbCommand(
                @"SELECT TrackID 
                    FROM SmartPlaylistEntries
                    WHERE SmartPlaylistID = :playlist_id",
                    "playlist_id", Id
            ));
            
            List<TrackInfo> tracks_to_add = new List<TrackInfo>();
            List<TrackInfo> tracks_to_remove = new List<TrackInfo>();

            Dictionary<int, TrackInfo> old_tracks = new Dictionary<int, TrackInfo>(tracks.Count);
            foreach (TrackInfo track in tracks) {
                old_tracks.Add(track.TrackId, track);
            }

            double sum = 0;
            double limit = 0;
            bool check_limit = LimitCriterion != 0 && LimitNumber != "0";
            if (check_limit) {
                limit = Double.Parse(LimitNumber); 
            }

            while(reader.Read()) {
                int id = Convert.ToInt32(reader[0]);

                TrackInfo track = null;
                try {
                    track = Globals.Library.Tracks[id] as TrackInfo;
                } catch {}

                //Console.WriteLine ("evaluating track {0} (old? {1})", track, old_tracks.Contains(track));
                if (track == null || track.TrackId <= 0) {
                    Console.WriteLine ("bad track = {0}", track);
                    continue;
                }

                if (check_limit) {
                    switch (LimitCriterion) {
                    case 1: // minutes
                        sum += track.Duration.TotalMinutes;
                        break;
                    case 2: // hours
                        sum += track.Duration.TotalHours;
                        break;
                    case 3: // MB
                        try {
                            Gnome.Vfs.FileInfo file = new Gnome.Vfs.FileInfo(track.Uri.AbsoluteUri);
                            sum += (double) (file.Size / (1024 * 1024));
                        } catch (System.IO.FileNotFoundException) {}
                        break;
                    }

                    // If we've reached the limit, break out of the add track cycle
                    if (sum > limit) {
                        break;
                    }
                }

                if (old_tracks.ContainsKey(track.TrackId)) {
                    // If we already have it, remove it from the old_tracks list so it isn't removed
                    old_tracks.Remove(track.TrackId);
                } else {
                    // Otherwise, we need to add it.
                    tracks_to_add.Add(track);
                }
            }

            // If there are old tracks we didn't examine, they should be removed
            tracks_to_remove.AddRange(old_tracks.Values);

            RemoveTracks(tracks_to_remove);
            AddTracks(tracks_to_add);

            reader.Dispose();

            t.Stop();
        }

        public void Check (TrackInfo track)
        {
            if (OrderAndLimit == null) {
                // If this SmartPlaylist doesn't have an OrderAndLimit clause, then it's quite simple
                // to check this track - if it matches the Condition we make sure it's in, and vice-versa
                
                object id = Globals.Library.Db.QuerySingle(String.Format(
                    "SELECT TrackId FROM Tracks WHERE TrackId = {0} {1}",
                    track.TrackId, PrependCondition("AND")
                ));

                if (id == null || (int) id != track.TrackId) {
                    // If it didn't match and is in the playlist, remove it
                    if (tracks.Contains (track)) {
                        RemoveTrack(track);
                    }
                } else if(! tracks.Contains (track)) {
                    // If it matched and isn't already in the playlist
                    AddTrack (track);
                }
            } else {
                RefreshMembers();
            }
        }

        public override void Commit ()
        {
            Timer t = new Timer ("Commit", Name);

            DbCommand command = new DbCommand(@"
                UPDATE SmartPlaylists
                SET 
                    Name = :name,
                    Condition = :condition,
                    OrderBy = :orderby,
                    LimitNumber = :limitnumber,
                    LimitCriterion = :limitcriterion
                WHERE SmartPlaylistID = :playlistid",
                "name", Name,
                "condition", Condition,
                "orderby", OrderBy,
                "limitnumber", LimitNumber,
                "limitcriterion", LimitCriterion,
                "playlistid", Id
            );

            Globals.Library.Db.Execute(command);

            t.Stop();
        }

        public override void ShowPropertiesDialog()
        {
            Editor ed = new Editor (this);
            ed.RunDialog ();
        }

        // TODO shouldn't try implement this until I understand what is does.
        /*public override void Reorder(TrackInfo track, int position)
        {
            lock(TracksMutex) {
                tracks.Insert(position, track);
            }
        }*/

        public void AddTracks(List<TrackInfo> tracks_to_add)
        {
            lock(TracksMutex) {
                tracks.AddRange(tracks_to_add);
            }

            OnTrackAdded(null, tracks_to_add);
            OnUpdated();
        }

        public override void AddTrack(TrackInfo track)
        {
            if(track is LibraryTrackInfo) {
                lock(TracksMutex) {
                    tracks.Add(track);
                }

                OnTrackAdded (track);
                OnUpdated();
            }
        }

        public void RemoveTracks(List<TrackInfo> tracks_to_remove)
        {
            lock(TracksMutex) {
                foreach (TrackInfo track in tracks_to_remove) {
                    tracks.Remove(track);
                }
            }

            OnTrackRemoved(null, tracks_to_remove);
            OnUpdated();
        }

        public override void RemoveTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Remove (track);
            }

            OnTrackRemoved (track);
            OnUpdated();
        }

        protected override bool UpdateName(string oldName, string newName)
        {
            if (oldName == newName)
                return false;

            Name = newName;
            Commit();
            return true;
        }

        public override bool Unmap()
        {
            return Delete(true);
        }

        public bool Delete(bool prompt)
        {
            List<SmartPlaylistSource> dependencies = DependedOnBy;
            if (dependencies.Count > 0) {
                if (prompt) {
                    Banshee.Widgets.HigMessageDialog dialog = new Banshee.Widgets.HigMessageDialog(
                        InterfaceElements.MainWindow,
                        Gtk.DialogFlags.Modal,
                        Gtk.MessageType.Warning,
                        Gtk.ButtonsType.Cancel,
                        Catalog.GetString("Smart Playlist has Dependencies"),
                        String.Format(Catalog.GetString(
                            "{0} is depended on by other smart playlists. Are you sure you want to delete this and all dependent smart playlists?"), Name)
                    );
                    dialog.AddButton(Gtk.Stock.Delete, Gtk.ResponseType.Ok, false);
                    
                    try {
                        if(dialog.Run() != (int)Gtk.ResponseType.Ok) {
                            return false;
                        }
                    } finally {
                        dialog.Destroy();
                    }
                }

                // Delete all dependent smart playlists (without further prompts) before continuing.
                foreach(SmartPlaylistSource pl in dependencies) {
                    pl.Delete(false);
                }
            } else if(prompt && !AbstractPlaylistSource.ConfirmUnmap(this)) {
                return false;
            }
            
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM SmartPlaylistEntries
                    WHERE SmartPlaylistID = '{0}'",
                    id
            ));
            
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM SmartPlaylists
                    WHERE SmartPlaylistID = '{0}'",
                    id
            ));
            
            LibrarySource.Instance.RemoveChildSource(this);
            SourceManager.RemoveSource(this);
            return true;
        }

        private string PrependCondition (string with)
        {
            return (Condition == null) ? " " : with + " (" + Condition + ")";
        }

        private void OnLibraryReloaded (object o, EventArgs args)
        {
            RefreshMembers();
        }

        private uint refresh_timeout = 0;
        public void QueueRefresh() {
            if(refresh_timeout == 0) {
                refresh_timeout = GLib.Timeout.Add(200, DoQueuedRefresh);
            }
        }

        private bool DoQueuedRefresh()
        {
            RefreshMembers();
            refresh_timeout = 0;
            return false;
        }

        private List<TrackInfo> remove_queue = new List<TrackInfo>();
        private uint remove_queue_timeout = 0;

        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            lock(this) {
                if (args.Track != null && tracks.Contains(args.Track))
                    remove_queue.Add(args.Track);

                if (args.Tracks != null)
                    foreach(TrackInfo track in args.Tracks)
                        if (track != null && tracks.Contains(track))
                            remove_queue.Add(track);
            
                if(remove_queue.Count > 0 && remove_queue_timeout == 0) {
                    remove_queue_timeout = GLib.Timeout.Add(500, FlushRemoveQueue);
                }
            }
        }

        private bool FlushRemoveQueue()
        {
            lock(this) {
                RemoveTracks(remove_queue);
                RefreshMembers();
                
                remove_queue_timeout = 0;
                return false;
            }
        }

        private void HandlePlaylistChanged (object sender, TrackEventArgs args)
        {
            if (SmartPlaylistCore.Instance.RateLimit())
                return;

            DateTime start = DateTime.Now;

            if (args.Tracks != null && args.Tracks.Count > 0) {
                RefreshMembers();
            } else if (args.Track != null) {
                Check (args.Track);
            }

            SmartPlaylistCore.Instance.CpuTime += (DateTime.Now - start).TotalMilliseconds;
        }

        public static void LoadFromReader (IDataReader reader)
        {
            int id = (int) reader[0];
            string name = reader[1] as string;
            string condition = reader[2] as string;
            string order_by = reader[3] as string;
            string limit_number = reader[4] as string;
            int limit_criterion = (int) reader[5];

            SmartPlaylistSource playlist = new SmartPlaylistSource(id, name, condition, 
                order_by, limit_number, limit_criterion);

            LibrarySource.Instance.AddChildSource(playlist);

            if(!SourceManager.ContainsSource (playlist) && SourceManager.ContainsSource(Banshee.Sources.LibrarySource.Instance)) {
                SourceManager.AddSource (playlist);
            }
        }
    }
}
