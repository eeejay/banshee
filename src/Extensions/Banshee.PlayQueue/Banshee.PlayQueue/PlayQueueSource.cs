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

        private LibraryTrackInfo playing_track;
        private BansheeActionGroup actions;
        private bool actions_loaded = false;
        
        public PlayQueueSource () : base (Catalog.GetString ("Play Queue"), null)
        {
            BindToDatabase ();
            
            Order = 0;
            Properties.SetString ("Icon.Name", "audio-x-generic");
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
            
            actions = new BansheeActionGroup ("PlayQueueSource");
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
        }
        
        public void Dispose ()
        {
            if (ClearOnQuitSchema.Get ()) {
                OnClearPlayQueue (this, EventArgs.Empty);
            }
        }
        
        private void BindToDatabase ()
        {
            object result = ServiceManager.DbConnection.ExecuteScalar (new HyenaSqliteCommand (@"
                SELECT PlaylistID FROM CorePlaylists 
                    WHERE Special = 1 AND Name = ?
                    LIMIT 1", special_playlist_name));
            
            if (result != null) {
                DbId = Convert.ToInt32 (result);
            } else {
                ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    INSERT INTO CorePlaylists VALUES (0, ?, -1, 0, 1)
                ", special_playlist_name));
                DbId = ServiceManager.DbConnection.LastInsertRowId;
            }
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
                RemoveFirstTrack ();
            }
        }
        
        private void OnAddToPlayQueue (object o, EventArgs args)
        {
            AddSelectedTracks (ServiceManager.Get<InterfaceActionService> ().TrackActions.TrackSelector.TrackModel);
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
            
            uia_service.GlobalActions.UpdateAction ("ClearPlayQueueAction", true, Count > 0);
            uia_service.TrackActions.UpdateAction ("AddToPlayQueueAction", 
                ServiceManager.SourceManager.ActiveSource != this, true);
        }
        
        void IBasicPlaybackController.First ()
        {
            ((IBasicPlaybackController)this).Next ();
        }
        
        void IBasicPlaybackController.Next ()
        {
            RemoveFirstTrack ();
            
            if (Count <= 0) {
                playing_track = null;
                ServiceManager.PlaybackController.Source = (ITrackModelSource)ServiceManager.SourceManager.DefaultSource;
                ServiceManager.PlaybackController.Next ();
                return;
            }
            
            playing_track = (LibraryTrackInfo)TrackModel[0];
            ServiceManager.PlayerEngine.OpenPlay (playing_track);
        }
        
        void IBasicPlaybackController.Previous ()
        {
        }
        
        private void RemoveFirstTrack ()
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
