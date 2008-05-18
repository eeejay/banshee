//
// StationSource.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Data.Sqlite;
using Lastfm;
using ConnectionState = Lastfm.ConnectionState;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Metadata;
using Banshee.MediaEngine;
using Banshee.Collection;
using Banshee.ServiceStack;
 
namespace Banshee.Lastfm.Radio
{
    public class StationSource : Source, ITrackModelSource, IUnmapableSource, IDisposable
    {
        private static string generic_name = Catalog.GetString ("Last.fm Station");
        
        private MemoryTrackListModel track_model;
        
        private LastfmSource lastfm;
        public LastfmSource LastfmSource {
            get { return lastfm; }
        }

        private string station;
        public string Station {
            get { return station; }
            protected set { station = value; }
        }

        private StationType type;
        public StationType Type {
            get { return type; }
            set {
                type = value;

                if (type.IconName != null)
                    Properties.SetString ("Icon.Name", type.IconName);
            }
        }

        private string arg;
        public string Arg {
            get { return arg; }
            set { arg = value; }
        }

        private int play_count;
        public int PlayCount {
            get { return play_count; }
            set { play_count = value; }
        }

        private int dbid;
        
        protected override string TypeUniqueId {
            get { return Convert.ToString (dbid); }
        }
        
        public System.Collections.Generic.IEnumerable<Banshee.Collection.Database.IFilterListModel> FilterModels {
            get { yield break; }
        }
        
        // For StationSources that already exist in the db
        protected StationSource (LastfmSource lastfm, int dbId, string name, string type, string arg, int playCount) : base (generic_name, name, 150)
        {
            this.lastfm = lastfm;
            dbid = dbId;
            Type = StationType.FindByName (type);
            Arg = arg;
            PlayCount = playCount;
            Station = Type.GetStationFor (arg);

            StationInitialize ();
        }

        public StationSource (LastfmSource lastfm, string name, string type, string arg) : base (generic_name, name, 150)
        {
            this.lastfm = lastfm;
            Type = StationType.FindByName (type);
            Arg = arg;
            Station = Type.GetStationFor (arg);

            Save ();

            StationInitialize ();
        }

        private void StationInitialize ()
        {
            track_model = new MemoryTrackListModel ();

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.StateChange);
            lastfm.Connection.StateChanged += HandleConnectionStateChanged;

            Properties.SetString ("GtkActionPath", "/LastfmStationSourcePopup");
            Properties.SetString ("SourcePropertiesActionLabel", Catalog.GetString ("Edit Last.fm Station"));
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Delete Last.fm Station"));

            UpdateUI (lastfm.Connection.State);
        }

        public void Dispose ()
        {
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
            lastfm.Connection.StateChanged -= HandleConnectionStateChanged;
        }

        public virtual void Save ()
        {
            if (dbid <= 0)
                Create ();
            else
                Update ();

            OnUpdated ();
        }

        private void Create ()
        {
            HyenaSqliteCommand command = new HyenaSqliteCommand (
                @"INSERT INTO LastfmStations (Creator, Name, Type, Arg, PlayCount)
                    VALUES (?, ?, ?, ?, ?)",
                lastfm.Account.UserName, Name, 
                Type.ToString (), Arg, PlayCount
            );

            dbid = ServiceManager.DbConnection.Execute (command);
        }

        private void Update ()
        {
            HyenaSqliteCommand command = new HyenaSqliteCommand (
                @"UPDATE LastfmStations
                    SET Name = ?, Type = ?, Arg = ?, PlayCount = ?
                    WHERE StationID = ?",
                Name, Type.ToString (), Arg, PlayCount, dbid
            );
            ServiceManager.DbConnection.Execute (command);

            Station = Type.GetStationFor (Arg);
            OnUpdated ();
        }
        
        //private bool shuffle;
        public override void Activate ()
        {
            base.Activate ();

            //action_service.GlobalActions ["PreviousAction"].Sensitive = false;

            // We lazy load the Last.fm connection, so if we're not already connected, do it
            if (lastfm.Connection.State == ConnectionState.Connected)
                TuneAndLoad ();
            else if (lastfm.Connection.State == ConnectionState.Disconnected)
                lastfm.Connection.Connect ();
        }

        private void TuneAndLoad ()
        {
            ThreadPool.QueueUserWorkItem (delegate {
                if (ChangeToThisStation ()) {
                    Thread.Sleep (250); // sleep for a bit to try to avoid Last.fm timeouts
                    if (TracksLeft < 2)
                        Refresh ();
                    else
                        HideStatus ();
                }
            });
        }

        public override void Deactivate ()
        {
            //Globals.ActionManager["PreviousAction"].Sensitive = true;
        }

        // Last.fm requires you to 'tune' to a station before requesting a track list/playing it
        public bool ChangeToThisStation ()
        {
            if (lastfm.Connection.Station == Station)
                return false;

            Log.Debug (String.Format ("Tuning Last.fm to {0}", Name), null);
            SetStatus (Catalog.GetString ("Tuning Last.fm to {0}."), false);
            StationError error = lastfm.Connection.ChangeStationTo (Station);

            if (error == StationError.None) {
                Log.Debug (String.Format ("Successfully tuned Last.fm to {0}", station), null);
                return true;
            } else {
                Log.Debug (String.Format ("Failed to tune Last.fm to {0}", Name), RadioConnection.ErrorMessageFor (error));
                SetStatus (String.Format (
                    // Translators: {0} is an error message sentence from RadioConnection.cs.
                    Catalog.GetString ("Failed to tune in station. {0}"), RadioConnection.ErrorMessageFor (error)), true
                );
                return false;
            }
        }

        public override void SetStatus (string message, bool error)
        {
            base.SetStatus (message, error);
            LastfmSource.SetStatus (status_message, lastfm, error, ConnectionState.Connected);
        }

        public void SetStatus (string message, bool error, ConnectionState state)
        {
            base.SetStatus (message, error);
            LastfmSource.SetStatus (status_message, lastfm, error, state);
        }

        /*public override void ShowPropertiesDialog ()
        {
            Editor ed = new Editor (this);
            ed.RunDialog ();
        }*/

        /*private bool playback_requested = false;
        public override void StartPlayback ()
        {
            if (CurrentTrack != null) {
                ServiceManager.PlayerEngine.OpenPlay (CurrentTrack);
            } else if (playback_requested == false) {
                playback_requested = true;
                Refresh ();
            }
        }*/

        private int current_track = 0;
        public TrackInfo CurrentTrack {
            get { return GetTrack (current_track); }
            set {
                int i = track_model.IndexOf (value);
                if (i != -1)
                    current_track = i;
            }
        }

        public TrackInfo NextTrack {
            get { return GetTrack (current_track + 1); }
        }

        private TrackInfo GetTrack (int track_num) {
            return (track_num > track_model.Count - 1) ? null : track_model[track_num];
        }

        private int TracksLeft {
            get {
                int left = track_model.Count - current_track - 1;
                return (left < 0) ? 0 : left;
            }
        }
        
        public bool HasDependencies {
            get { return false; }
        }

        private bool refreshing = false;
        public void Refresh ()
        {
            lock (this) {
                if (refreshing || lastfm.Connection.Station != Station) {
                    return;
                }
                refreshing = true;
            }

            if (TracksLeft == 0) {
                SetStatus (Catalog.GetString ("Getting new songs for {0}."), false);
            }

            ThreadAssist.Spawn (delegate {
                Media.Playlists.Xspf.Playlist playlist = lastfm.Connection.LoadPlaylistFor (Station);
                if (playlist != null) {
                    if (playlist.TrackCount == 0) {
                        SetStatus (Catalog.GetString ("No new songs available for {0}."), true);
                    } else {
                        List<TrackInfo> new_tracks = new List<TrackInfo> ();
                        foreach (Media.Playlists.Xspf.Track track in playlist.Tracks) {
                            TrackInfo ti = new LastfmTrackInfo (track, this, track.GetExtendedValue ("trackauth"));
                            new_tracks.Add (ti);
                            lock (track_model) {
                                track_model.Add (ti);
                            }
                        }
                        HideStatus ();

                        ThreadAssist.ProxyToMain (delegate {
                            //OnTrackAdded (null, new_tracks);
                            track_model.Reload ();
                            OnUpdated ();

                            /*if (playback_requested) {
                                StartPlayback ();
                                playback_requested = false;
                            }*/
                        });
                    }
                } else {
                    SetStatus (Catalog.GetString ("Failed to get new songs for {0}."), true);
                }

                refreshing = false;
            });
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            if (((PlayerEventStateChangeArgs)args).Current == PlayerState.Loaded && 
                track_model.Contains (ServiceManager.PlayerEngine.CurrentTrack)) {
                CurrentTrack = ServiceManager.PlayerEngine.CurrentTrack;

                lock (track_model) {
                    // Remove all but 5 played or skipped tracks
                    if (current_track > 5) {
                        for (int i = 0; i < (current_track - 5); i++) {
                            track_model.Remove (track_model[0]);
                        }
                        current_track = 5;
                    }

                    // Set all previous tracks as CanPlay = false
                    foreach (TrackInfo track in track_model) {
                        if (track == CurrentTrack)
                            break;
                        if (track.CanPlay) {
                            track.CanPlay = false;
                        }
                    }
                    OnUpdated ();
                }

                if (TracksLeft <= 2) {
                    Refresh ();
                }
            }
        }
        
        private void HandleConnectionStateChanged (object sender, ConnectionStateChangedArgs args)
        {
            UpdateUI (args.State);
        }

        private void UpdateUI (ConnectionState state)
        {
            if (state == ConnectionState.Connected) {
                HideStatus ();
                if (this == ServiceManager.SourceManager.ActiveSource) {
                    TuneAndLoad ();
                }
            } else {
                track_model.Clear ();
                SetStatus (RadioConnection.MessageFor (state), state != ConnectionState.Connecting, state);
                OnUpdated ();
            }
        }

        public override string GetStatusText ()
        {
            return String.Format (
                Catalog.GetPluralString ("{0} song played", "{0} songs played", PlayCount), PlayCount
            );
        }

        public override bool CanRename {
            get { return true; }
        }


#region ITrackModelSource Implementation

        public TrackListModel TrackModel {
            get { return track_model; }
        }

        public AlbumListModel AlbumModel {
            get { return null; }
        }

        public ArtistListModel ArtistModel {
            get { return null; }
        }

        public void Reload ()
        {
            track_model.Reload ();
        }

        public void RemoveSelectedTracks ()
        {
        }

        public void DeleteSelectedTracks ()
        {
            throw new Exception ("Should not call DeleteSelectedTracks on StationSource");
        }

        public bool CanAddTracks {
            get { return false; }
        }

        public bool CanRemoveTracks {
            get { return false; }
        }

        public bool CanDeleteTracks {
            get { return false; }
        }

        public bool ConfirmRemoveTracks {
            get { return false; }
        }
        
        public bool ShowBrowser {
            get { return false; }
        }

#endregion

#region IUnmapableSource Implementation

        public bool Unmap ()
        {
            ServiceManager.DbConnection.Execute (String.Format (
                @"DELETE FROM LastfmStations
                    WHERE StationID = '{0}'",
                    dbid
            ));
            Parent.RemoveChildSource (this);
            ServiceManager.SourceManager.RemoveSource (this);
            return true;
        }

        public bool CanUnmap {
            get { return true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return true; }
        }

#endregion

        public override void Rename (string newName)
        {
            base.Rename (newName);
            Save ();
        }
        
        public override bool HasProperties {
            get { return true; }
        }
        
        public static List<StationSource> LoadAll (LastfmSource lastfm, string creator)
        {
            List<StationSource> stations = new List<StationSource> ();

            HyenaSqliteCommand command = new HyenaSqliteCommand (
                "SELECT StationID, Name, Type, Arg, PlayCount FROM LastfmStations WHERE Creator = ?",
                creator
            );

            using (IDataReader reader = ServiceManager.DbConnection.Query (command)) {
                while (reader.Read ()) {
                    try {
                        stations.Add (new StationSource (lastfm,
                            Convert.ToInt32 (reader[0]),
                            reader[1] as string,
                            reader[2] as string,
                            reader[3] as string,
                            Convert.ToInt32 (reader[4])
                        ));
                    } catch (Exception e) {
                        Log.Warning ("Error Loading Last.fm Station", e.ToString (), false);
                    }
                }
            }

            // Create some default stations if the user has none
            if (stations.Count == 0) {
                stations.Add (new StationSource (lastfm, Catalog.GetString ("Recommended"), "Recommended", creator));
                stations.Add (new StationSource (lastfm, Catalog.GetString ("Personal"), "Personal", creator));
                stations.Add (new StationSource (lastfm, Catalog.GetString ("Loved"), "Loved", creator));
                stations.Add (new StationSource (lastfm, Catalog.GetString ("Banshee Group"), "Group", "Banshee"));
                stations.Add (new StationSource (lastfm, Catalog.GetString ("Neighbors"), "Neighbor", creator));
            }

            return stations;
        }

        static StationSource ()
        {
            if (!ServiceManager.DbConnection.TableExists ("LastfmStations")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE LastfmStations (
                        StationID           INTEGER PRIMARY KEY,
                        Creator             STRING NOT NULL,
                        Name                STRING NOT NULL,
                        Type                STRING NOT NULL,
                        Arg                 STRING NOT NULL,
                        PlayCount           INTEGER NOT NULL
                    )
                ");
            } else {
                try {
                    ServiceManager.DbConnection.Query<int> ("SELECT PlayCount FROM LastfmStations LIMIT 1");
                } catch {
                    Log.Debug ("Adding new database column", "Table: LastfmStations, Column: PlayCount INTEGER");
                    ServiceManager.DbConnection.Execute ("ALTER TABLE LastfmStations ADD PlayCount INTEGER");
                    ServiceManager.DbConnection.Execute ("UPDATE LastfmStations SET PlayCount = 0");
                }
            }
        }
    }
}
