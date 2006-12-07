using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Plugins;
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
            get { return (Condition == null) ? false : Condition.IndexOf ("PlaylistID") != -1; }
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
            foreach (PlaylistSource source in watchedPlaylists) {
                source.TrackAdded -= HandlePlaylistChanged;
                source.TrackRemoved -= HandlePlaylistChanged;
            }
            watchedPlaylists.Clear();

            if (PlaylistDependent) {
                foreach (PlaylistSource source in PlaylistSource.Playlists) {
                    if (Condition.IndexOf (String.Format ("PlaylistID = {0}", source.Id)) != -1 ||
                        Condition.IndexOf (String.Format ("PlaylistID != {0}", source.Id)) != -1)
                    {
                        //Console.WriteLine ("{0} now listening to {1}", Name, source.Name);
                        source.TrackAdded += HandlePlaylistChanged;
                        source.TrackRemoved += HandlePlaylistChanged;
                        watchedPlaylists.Add (source);
                    }
                }
            }
        }

        public void RefreshMembers()
        {
            RefreshMembers(true);
        }

        public void RefreshMembers(bool notify)
        {
            Timer t = new Timer ("RefreshMembers", Name);

            //Console.WriteLine ("Refreshing smart playlist {0} with condition {1}", Source.Name, Condition);

            // Delete existing tracks
            Globals.Library.Db.Execute(new DbCommand(
                "DELETE FROM SmartPlaylistEntries WHERE PlaylistID = :playlist_id",
                "playlist_id", Id
            ));

            // Add matching tracks
            Globals.Library.Db.Execute(String.Format(
                @"INSERT INTO SmartPlaylistEntries 
                    SELECT NULL as EntryId, {0} as PlaylistID, TrackId FROM Tracks {1} {2}",
                    Id, PrependCondition("WHERE"), OrderAndLimit
            ));

            // Load the new tracks in
            IDataReader reader = Globals.Library.Db.Query(new DbCommand(
                @"SELECT TrackID 
                    FROM SmartPlaylistEntries
                    WHERE PlaylistID = :playlist_id ORDER BY TrackId",
                    "playlist_id", Id
            ));
            
            List<TrackInfo> tracks_to_add = new List<TrackInfo>();
            List<TrackInfo> tracks_to_remove = new List<TrackInfo>();
            Queue<TrackInfo> old_tracks = new Queue<TrackInfo>(tracks);

            double sum = 0;
            double limit = 0;
            bool check_limit = LimitCriterion != 0 && LimitNumber != "0";
            if (check_limit) {
                limit = Double.Parse(LimitNumber); 
            }

            int counter = 0;
            while(reader.Read()) {
                int id = Convert.ToInt32(reader[0]);

                TrackInfo track = Globals.Library.Tracks[id] as TrackInfo;
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

                if (old_tracks.Count == 0 || id < old_tracks.Peek().TrackId) {
                    // If we've examined all the old tracks or
                    // if the new track's ID is less than the old track's, add it
                    tracks_to_add.Add(track);
                } else if (id > old_tracks.Peek().TrackId) {
                    // We know the old track has been removed if this track's ID is greater than it b/c of how we've sorted things
                    tracks_to_remove.Add(old_tracks.Dequeue());

                    tracks_to_add.Add(track);
                } else {
                    // The new track is equal to the old track, so move on to the next old track
                    old_tracks.Dequeue();
                }
                counter++;
            }

            // If there are old tracks we didn't examine, they should be removed
            tracks_to_remove.AddRange (old_tracks);

            RemoveTracks(tracks_to_remove, notify);
            AddTracks(tracks_to_add, notify);

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
                WHERE PlaylistID = :playlistid",
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

        public void AddTracks(List<TrackInfo> tracks_to_add, bool notify)
        {
            lock(TracksMutex) {
                tracks.AddRange(tracks_to_add);
            }

            if (notify) {
                ThreadAssist.ProxyToMain(delegate {
                    OnTrackAdded(null, tracks_to_add);
                    OnUpdated();
                });
            }
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

        public void RemoveTracks(List<TrackInfo> tracks_to_remove, bool notify)
        {
            lock(TracksMutex) {
                foreach (TrackInfo track in tracks_to_remove) {
                    tracks.Remove(track);
                }
            }

            if (notify) {
                ThreadAssist.ProxyToMain(delegate {
                    OnTrackRemoved(null, tracks_to_remove);
                    OnUpdated();
                });
            }
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
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM SmartPlaylistEntries
                    WHERE PlaylistID = '{0}'",
                    id
            ));
            
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM SmartPlaylists
                    WHERE PlaylistID = '{0}'",
                    id
            ));
            
            LibrarySource.Instance.RemoveChildSource(this);
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

        private void OnLibraryTrackRemoved(object o, LibraryTrackRemovedArgs args)
        {
            if(args.Track != null) {
                if(tracks.Contains(args.Track)) {
                    RemoveTrack(args.Track);
                    RefreshMembers();
                }
                
                return;
            } else if(args.Tracks == null) {
                return;
            }
            
            bool removed_any = false;
            foreach(TrackInfo track in args.Tracks) {
                if(tracks.Contains(track)) {
                    RemoveTrack (track);
                    removed_any = true;
                }
            }
            
            if (removed_any) {
                RefreshMembers();
            }
        }

        private void HandlePlaylistChanged (object sender, TrackEventArgs args)
        {
            if (SmartPlaylistCore.Instance.RateLimit())
                return;

            //Console.WriteLine ("{0} sent playlist changed to {1}", (sender as PlaylistSource).Name, Name);
            if (args.Track != null) {
                DateTime start = DateTime.Now;
                Check (args.Track);
                SmartPlaylistCore.Instance.CpuTime += (DateTime.Now - start).TotalMilliseconds;
            }
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
