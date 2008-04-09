//
// PlayQueueSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Mono.Unix;
using Gtk;

using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;
using Banshee.Library;
using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.PlaybackController;
using Banshee.MediaEngine;
using Banshee.Configuration;
using Banshee.Gui;

namespace Banshee.PlayQueue
{
    public class PlayQueueSource : PlaylistSource, IBasicPlaybackController, IDisposable
    {
        private static string special_playlist_name = typeof (PlayQueueSource).ToString ();

        private DatabaseTrackInfo playing_track;
        private bool actions_loaded = false;
        
        public PlayQueueSource () : base (Catalog.GetString ("Play Queue"), null, 0)
        {
            BindToDatabase ();
            
            Order = 10;
            Properties.SetString ("Icon.Name", "source-playlist");
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Play Queue"));
            
            ((TrackListDatabaseModel)TrackModel).ForcedSortQuery = "CorePlaylistEntries.EntryID ASC";
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
            ServiceManager.PlaybackController.Transition += OnCanonicalPlaybackControllerTransition;

            ServiceManager.SourceManager.AddSource (this);
            
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            uia_service.TrackActions.Add (new ActionEntry [] {
                new ActionEntry ("AddToPlayQueueAction", Stock.Add,
                    Catalog.GetString ("Add to Play Queue"), "q",
                    Catalog.GetString ("Append selected songs to the play queue"),
                    OnAddToPlayQueue)
            });
            
            uia_service.GlobalActions.AddImportant (
                new ActionEntry ("ClearPlayQueueAction", Stock.Clear,
                    Catalog.GetString ("Clear"), null,
                    Catalog.GetString ("Remove all tracks from the play queue"),
                    OnClearPlayQueue)
            );
            
            uia_service.GlobalActions.Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ClearPlayQueueOnQuitAction", null,
                    Catalog.GetString ("Clear on Quit"), null, 
                    Catalog.GetString ("Clear the play queue when quitting"), 
                    OnClearPlayQueueOnQuit, ClearOnQuitSchema.Get ())
            });
            
            uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/PlayQueueContextMenu");
            
            actions_loaded = true;
            
            UpdateActions ();
            ServiceManager.SourceManager.ActiveSourceChanged += delegate { UpdateActions (); };

            // TODO listen to all primary sources, and handle transient primary sources
            ServiceManager.SourceManager.MusicLibrary.TracksChanged += HandleTracksChanged;
            ServiceManager.SourceManager.MusicLibrary.TracksDeleted += HandleTracksDeleted;
            ServiceManager.SourceManager.VideoLibrary.TracksChanged += HandleTracksChanged;
            ServiceManager.SourceManager.VideoLibrary.TracksDeleted += HandleTracksDeleted;

            SetAsPlayingSource ();
        }
        
        private void SetAsPlayingSource ()
        {
            if (Count > 0 && ServiceManager.PlayerEngine.CurrentState != PlayerEngineState.Playing) {
                ServiceManager.PlaybackController.Source = this;
            }
        }

        public void Dispose ()
        {
            if (ClearOnQuitSchema.Get ()) {
                OnClearPlayQueue (this, EventArgs.Empty);
            }
        }
        
        private void BindToDatabase ()
        {
            int result = ServiceManager.DbConnection.Query<int> (
                "SELECT PlaylistID FROM CorePlaylists WHERE Special = 1 AND Name = ? LIMIT 1",
                special_playlist_name
            );
            
            if (result != 0) {
                DbId = result;
            } else {
                DbId = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    INSERT INTO CorePlaylists (PlaylistID, Name, SortColumn, SortType, Special) VALUES (NULL, ?, -1, 0, 1)
                ", special_playlist_name));
            }
        }

        protected override void OnTracksAdded ()
        {
            base.OnTracksAdded ();
            SetAsPlayingSource ();
        }
        
        protected override void OnUpdated ()
        {
            if (actions_loaded) {
                UpdateActions ();
            }
            
            base.OnUpdated ();
        }
        
        private void OnCanonicalPlaybackControllerTransition (object o, EventArgs args)
        {
            if (Count > 0) {
                ServiceManager.PlaybackController.Source = this;
            }
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        { 
            if (args.Event == PlayerEngineEvent.EndOfStream) {
                RemovePlayingTrack ();
            } else if (args.Event == PlayerEngineEvent.StartOfStream) {
                playing_track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo; 
            }
        }
        
        private void OnAddToPlayQueue (object o, EventArgs args)
        {
            AddSelectedTracks (ServiceManager.SourceManager.ActiveSource);
            OnUserNotifyUpdated ();
        }
        
        private void OnClearPlayQueue (object o, EventArgs args)
        {
            RemoveTrackRange ((TrackListDatabaseModel)TrackModel, new Hyena.Collections.RangeCollection.Range (0, Count));
            Reload ();
        }
        
        private void OnClearPlayQueueOnQuit (object o, EventArgs args)
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            ToggleAction action = (ToggleAction)uia_service.GlobalActions["ClearPlayQueueOnQuitAction"];
            ClearOnQuitSchema.Set (action.Active);
        }
        
        private void UpdateActions ()
        {   
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            Source source = ServiceManager.SourceManager.ActiveSource;
            bool in_library = (source != null && source.Parent is LibrarySource) || source is LibrarySource;
            
            uia_service.GlobalActions.UpdateAction ("ClearPlayQueueAction", true, Count > 0);
            uia_service.TrackActions.UpdateAction ("AddToPlayQueueAction", in_library, true);
        }
        
        void IBasicPlaybackController.First ()
        {
            ((IBasicPlaybackController)this).Next (false);
        }
        
        void IBasicPlaybackController.Next (bool restart)
        {
            RemovePlayingTrack ();
            
            if (Count <= 0) {
                playing_track = null;
                ServiceManager.PlaybackController.Source = (ITrackModelSource)ServiceManager.SourceManager.DefaultSource;
                ServiceManager.PlaybackController.Next ();
                return;
            }
            
            ServiceManager.PlayerEngine.OpenPlay ((DatabaseTrackInfo)TrackModel[0]);
        }
        
        void IBasicPlaybackController.Previous ()
        {
        }
        
        private void RemovePlayingTrack ()
        {
            if (playing_track != null) {
                RemoveTrack (playing_track);
                playing_track = null;
            }
        }
        
        public override bool CanRename {
            get { return false; }
        }
        
        public override bool ShowBrowser {
            get { return false; }
        }
        
        public override bool ConfirmRemoveTracks {
            get { return false; }
        }
        
        public override bool CanUnmap {
            get { return false; }
        }
        
        public static readonly SchemaEntry<bool> ClearOnQuitSchema = new SchemaEntry<bool> (
            "plugins.play_queue", "clear_on_quit",
            false,
            "Clear on Quit",
            "Clear the play queue when quitting"
        );
    }
}
