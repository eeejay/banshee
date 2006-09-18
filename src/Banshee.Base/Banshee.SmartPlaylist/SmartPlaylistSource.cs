using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
 
using Banshee.Base;
using Banshee.Sources;
using Banshee.Plugins;

using Mono.Unix;

using Sql;

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
            get { return (Condition == null) ? false : Condition.IndexOf ("current_timestamp") != -1; }
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

        public override string UnmapLabel {
            get { return Catalog.GetString ("Delete Smart Playlist"); }
        }

        public override string GenericName {
            get { return Catalog.GetString ("Smart Playlist"); }
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

            Statement query = new Insert("SmartPlaylists", true,
                "Name", Name,
                "Condition", Condition,
                "OrderBy", OrderBy,
                "LimitNumber", LimitNumber,
                "LimitCriterion", LimitCriterion
            );

            Id = Globals.Library.Db.Execute(query);

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
            Timer t = new Timer ("RefreshMembers " + Name);

            //Console.WriteLine ("Refreshing smart playlist {0} with condition {1}", Source.Name, Condition);

            // Delete existing tracks
            Globals.Library.Db.Execute(String.Format(
                "DELETE FROM SmartPlaylistEntries WHERE PlaylistID = {0}", Id
            ));

            foreach (TrackInfo track in tracks)
                OnTrackRemoved (track);

            tracks.Clear();

            // Add matching tracks
            Globals.Library.Db.Execute(String.Format(
                @"INSERT INTO SmartPlaylistEntries 
                    SELECT NULL as EntryId, {0} as PlaylistID, TrackId FROM Tracks {1} {2}",
                    Id, PrependCondition("WHERE"), OrderAndLimit
            ));

            // Load the new tracks in
            IDataReader reader = Globals.Library.Db.Query(String.Format(
                @"SELECT TrackID 
                    FROM SmartPlaylistEntries
                    WHERE PlaylistID = '{0}'",
                    Id
            ));
            
            // If the limit is by any but songs, we need to prune the list up based on the desired 
            // attribute (time/size)
            if (LimitCriterion == 0 || LimitNumber == "0") {
                while(reader.Read()) {
                    AddTrack (Globals.Library.Tracks[Convert.ToInt32(reader[0])] as TrackInfo);
                }
            } else {
                LimitTracks (reader, false);
            }

            reader.Dispose();

            t.Stop();
        }

        private void LimitTracks (IDataReader reader, bool remove_if_limited)
        {
            Timer t = new Timer ("LimitTracks " + Name);

            bool was_limited = false;
            double sum = 0;
            double limit = Double.Parse(LimitNumber); 
            while(reader.Read()) {
                TrackInfo track = Globals.Library.Tracks[Convert.ToInt32(reader[0])] as TrackInfo;

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

                if (sum > limit) {
                    was_limited = true;

                    if (remove_if_limited) {
                        RemoveTrack (track);
                    } else {
                        break;
                    }
                } else if (!tracks.Contains (track)) {
                    AddTrack (track);
                }
            }

            // We do a commit here to clean up the tracks listed in the database..the commit deletes
            // all Entries for this playlist then inserts them based on one's that are actually in the playlist...
            // so all the tracks that were beyond the limit point are cleaned out
            if (was_limited || remove_if_limited)
                Commit();

            t.Stop();
        }

        public void Check (TrackInfo track)
        {
            if (OrderAndLimit == null) {
                // If this SmartPlaylist doesn't have an OrderAndLimit clause, then it's quite simple
                // to check this track - if it matches the Condition we make sure it's in, and vice-versa
                //Console.WriteLine ("Limitless condition");
                
                object id = Globals.Library.Db.QuerySingle(String.Format(
                    "SELECT TrackId FROM Tracks WHERE TrackId = {0} {1}",
                    track.TrackId, PrependCondition("AND")
                ));

                if (id == null || (int) id != track.TrackId) {
                    if (tracks.Contains (track)) {
                        // If it didn't match and is in the playlist, remove it
                        RemoveTrack (track);
                    }
                } else if(! tracks.Contains (track)) {
                    // If it matched and isn't already in the playlist
                    AddTrack (track);
                }
            } else {
                // If this SmartPlaylist has an OrderAndLimit clause things are more complicated as there are a limited
                // number of tracks -- so if we remove a track, we probably need to add a different one and vice-versa.
                //Console.WriteLine ("Checking track {0} ({1}) against condition & order/limit {2} {3}", track.Uri.LocalPath, track.TrackId, Condition, OrderAndLimit);

                // See if there is a track that was in the SmartPlaylist that now shouldn't be because
                // this track we are checking displaced it.
                IDataReader reader = Globals.Library.Db.Query(String.Format(
                    "SELECT TrackId FROM SmartPlaylistEntries WHERE PlaylistID = {0} " +
                    "AND TrackId NOT IN (SELECT TrackID FROM Tracks {1} {2})",
                    Id, PrependCondition("WHERE"), OrderAndLimit
                ));

                while (reader.Read())
                    RemoveTrack  (Globals.Library.Tracks[Convert.ToInt32(reader[0])] as TrackInfo);

                reader.Dispose();

                // Remove those tracks from the database
                Globals.Library.Db.Execute(String.Format(
                    "DELETE FROM SmartPlaylistEntries WHERE PlaylistID = {0} " +
                    "AND TrackId NOT IN (SELECT TrackID FROM Tracks {1} {2})",
                    Id, PrependCondition("WHERE"), OrderAndLimit
                ));

                // If we are already a member of this smart playlist
                if (tracks.Contains (track))
                    return;

                // We have removed tracks no longer in this smart playlist, now need to add
                // tracks that replace those that were removed (if any), and do limited by size/duration
                IDataReader tracks_res = Globals.Library.Db.Query(String.Format(
                    @"SELECT TrackId FROM Tracks 
                        WHERE TrackID IN (SELECT TrackID FROM Tracks {1} {2})",
                    Id, PrependCondition("WHERE"), OrderAndLimit
                ));

                LimitTracks (tracks_res, true);

                tracks_res.Dispose();
            }
        }

        public override void Commit ()
        {
            Timer t = new Timer ("Commit");

            Statement query = new Update("SmartPlaylists",
                "Name", Name,
                "Condition", Condition,
                "OrderBy", OrderBy,
                "LimitNumber", LimitNumber,
                "LimitCriterion", LimitCriterion) + 
                new Where(new Compare("PlaylistID", Op.EqualTo, Id));

            Globals.Library.Db.Execute(query);

            // Make sure the tracks are up to date
            Globals.Library.Db.Execute(String.Format(
                @"DELETE FROM SmartPlaylistEntries
                    WHERE PlaylistID = '{0}'",
                    id
            ));

            
            lock(TracksMutex) {
                foreach(TrackInfo track in Tracks) {
                    if(track == null || track.TrackId <= 0)
                        continue;
                        
                    Globals.Library.Db.Execute(String.Format(
                        @"INSERT INTO SmartPlaylistEntries 
                            VALUES (NULL, '{0}', '{1}')",
                            id, track.TrackId
                    ));
                }
            }

            t.Stop();
        }

        public override void ShowPropertiesDialog()
        {
            Editor ed = new Editor (this);
            ed.RunDialog ();
        }

        public override void Reorder(TrackInfo track, int position)
        {
            RemoveTrack(track);
            lock(TracksMutex) {
                tracks.Insert(position, track);
            }
        }

        public override void AddTrack(TrackInfo track)
        {
            //Console.WriteLine ("Adding track ... == null ? {0}", track == null);
            if(track is LibraryTrackInfo) {
                //Console.WriteLine ("its a LibraryTrackInfo! track = {0}", track);
                lock(TracksMutex) {
                    tracks.Add(track);
                }

                OnTrackAdded (track);
            }
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Remove (track);
                //  playlistModel.RemoveTrack(ref iters[i], track);
            }

            OnTrackRemoved (track);
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
                    
                    if(Count == 0) {
                        Unmap();
                    } else {
                        Commit();
                    }
                }
                
                return;
            } else if(args.Tracks == null) {
                return;
            }
            
            int removed_count = 0;
            
            foreach(TrackInfo track in args.Tracks) {
                if(tracks.Contains(track)) {
                    RemoveTrack (track);
                    removed_count++;
                }
            }
            
            if(removed_count > 0) {
                if(Count == 0) {
                    Unmap();
                } else {
                    Commit();
                }
            }
        }

        private void HandlePlaylistChanged (object sender, TrackEventArgs args)
        {
            //Console.WriteLine ("{0} sent playlist changed to {1}", (sender as PlaylistSource).Name, Name);
            if (args.Track != null)
                Check (args.Track);
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
