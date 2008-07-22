//
// AudioCdSource.cs
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
using System.Collections.Generic;
using System.Threading;
using Mono.Unix;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

using Gtk;
using Banshee.Gui;

namespace Banshee.AudioCd
{
    public class AudioCdSource : Source, ITrackModelSource, IUnmapableSource, IDurationAggregator, IDisposable
    {
        private AudioCdService service;
        private AudioCdDiscModel disc_model;
        private SourceMessage query_message;
        
        
        public AudioCdSource (AudioCdService service, AudioCdDiscModel discModel) 
            : base (Catalog.GetString ("Audio CD"), discModel.Title, 400)
        {
            this.service = service;
            this.disc_model = discModel;
            
            Properties.SetString ("TrackView.ColumnControllerXml", String.Format (@"
                <column-controller>
                  <column>
                    <renderer type=""Hyena.Data.Gui.ColumnCellCheckBox"" property=""RipEnabled""/>
                  </column>
                  <add-all-defaults />
                </column-controller>
            "));
            
            disc_model.MetadataQueryStarted += OnMetadataQueryStarted;
            disc_model.MetadataQueryFinished += OnMetadataQueryFinished;
            disc_model.EnabledCountChanged += OnEnabledCountChanged;
            disc_model.LoadModelFromDisc ();

            SetupGui ();
        }
        
        public TimeSpan Duration {
            get { return disc_model.Duration; }
        }
        
        public bool DiscIsPlaying {
            get {
                AudioCdTrackInfo playing_track = ServiceManager.PlayerEngine.CurrentTrack as AudioCdTrackInfo;
                return playing_track != null && playing_track.Model == disc_model;
            }
        }
        
        public void StopPlayingDisc ()
        {
            if (DiscIsPlaying) {
                ServiceManager.PlayerEngine.Close (true);
            }
        }
        
        public void Dispose ()
        {
            ClearMessages ();
            disc_model.MetadataQueryStarted -= OnMetadataQueryStarted;
            disc_model.MetadataQueryFinished -= OnMetadataQueryFinished;
            disc_model.EnabledCountChanged -= OnEnabledCountChanged;
            service = null;
            disc_model = null;
        }
        
        public AudioCdDiscModel DiscModel {
            get { return disc_model; }
        }
        
        private void OnEnabledCountChanged (object o, EventArgs args)
        {
            UpdateActions ();
        }
        
        private void OnMetadataQueryStarted (object o, EventArgs args)
        {
            if (query_message != null) {
                DestroyQueryMessage ();
            }
            
            query_message = new SourceMessage (this);
            query_message.FreezeNotify ();
            query_message.CanClose = false;
            query_message.IsSpinning = true;
            query_message.Text = Catalog.GetString ("Searching for CD metadata...");
            query_message.ThawNotify ();
            
            PushMessage (query_message);
        }
        
        private void OnMetadataQueryFinished (object o, EventArgs args)
        {
            if (disc_model.Title != Name) {
                Name = disc_model.Title;
                OnUpdated ();
            }
        
            if (disc_model.MetadataQuerySuccess) {
                DestroyQueryMessage ();
                if (DiscIsPlaying) {
                    ServiceManager.PlayerEngine.TrackInfoUpdated ();
                }

                if (AudioCdService.AutoRip.Get ()) {
                    BeginAutoRip ();
                }

                return;
            }

            if (query_message == null) {
                return;
            }
            
            query_message.FreezeNotify ();
            query_message.IsSpinning = false;
            query_message.SetIconName ("dialog-error");
            query_message.Text = Catalog.GetString ("Could not fetch metadata for CD.");
            query_message.CanClose = true;
            query_message.ThawNotify ();
        }
        
        private void DestroyQueryMessage ()
        {
            if (query_message != null) {
                RemoveMessage (query_message);
                query_message = null;
            }
        }

        private void BeginAutoRip ()
        {
            // Make sure the album isn't already in the Library
            TrackInfo track = disc_model[0];
            int count = ServiceManager.DbConnection.Query<int> (
                @"SELECT Count(*) FROM CoreTracks, CoreArtists, CoreAlbums WHERE 
                    CoreTracks.PrimarySourceID = ? AND
                    CoreTracks.ArtistID = CoreArtists.ArtistID AND 
                    CoreTracks.AlbumID = CoreAlbums.AlbumID AND 
                    CoreArtists.Name = ? AND CoreAlbums.Title = ? AND (CoreTracks.Disc = ? OR CoreTracks.Disc = 0)",
                    ServiceManager.SourceManager.MusicLibrary.DbId,
                    track.ArtistName, track.AlbumTitle, track.Disc
            );

            if (count > 0) {
                SetStatus (Catalog.GetString ("Automatic import off since this album is already in the Music Library."), true, false, null);
                return;
            }

            Log.DebugFormat ("Beginning auto rip of {0}", Name);
            ImportDisc ();
        }

        internal void ImportDisc ()
        {
            AudioCdRipper ripper = null;
            
            try {
                if (AudioCdRipper.Supported) {
                    ripper = new AudioCdRipper (this);
                    ripper.Finished += OnRipperFinished;
                    ripper.Start ();
                }
            } catch (Exception e) {
                if (ripper != null) {
                    ripper.Dispose ();
                }
                
                Log.Error (Catalog.GetString ("Could not import CD"), e.Message, true);
                Log.Exception (e);
            }
        }

        private void OnRipperFinished (object o, EventArgs args)
        {
            if (AudioCdService.EjectAfterRipped.Get ()) {
                Unmap ();
            }
        }

        internal void DuplicateDisc ()
        {
            try {
                AudioCdDuplicator.Duplicate (disc_model);
            } catch (Exception e) {
                Hyena.Log.Error (Catalog.GetString ("Could not duplicate audio CD"), e.Message, true);
                Hyena.Log.Exception (e);
            }
        }
        
        internal void LockAllTracks ()
        {
            StopPlayingDisc ();
        
            foreach (AudioCdTrackInfo track in disc_model) {
                track.CanPlay = false;
            }
            
            OnUpdated ();
        }
        
        internal void UnlockAllTracks ()
        {
            foreach (AudioCdTrackInfo track in disc_model) {
                track.CanPlay = true;
            }
            
            OnUpdated ();
        }
        
        internal void UnlockTrack (AudioCdTrackInfo track)
        {
            track.CanPlay = true;
            OnUpdated ();
        }

#region Source Overrides

        protected override string TypeUniqueId {
            get { return "audio-cd"; }
        }
        
        public override int Count {
            get { return disc_model.Count; }
        }
        
#endregion
        
#region ITrackModelSource Implementation

        public TrackListModel TrackModel {
            get { return disc_model; }
        }

        public AlbumListModel AlbumModel {
            get { return null; }
        }

        public ArtistListModel ArtistModel {
            get { return null; }
        }

        public void Reload ()
        {
            disc_model.Reload ();
        }

        public void RemoveSelectedTracks ()
        {
        }

        public void DeleteSelectedTracks ()
        {
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
        
        public bool HasDependencies {
            get { return false; }
        }

#endregion

#region IUnmapableSource Implementation

        public bool Unmap ()
        {
            StopPlayingDisc ();
            
            foreach (TrackInfo track in disc_model) {
                track.CanPlay = false;
            }
            
            OnUpdated ();
            
            SourceMessage eject_message = new SourceMessage (this);
            eject_message.FreezeNotify ();
            eject_message.IsSpinning = true;
            eject_message.CanClose = false;
            eject_message.Text = Catalog.GetString ("Ejecting audio CD...");
            eject_message.ThawNotify ();
            PushMessage (eject_message);
        
            ThreadPool.QueueUserWorkItem (delegate {
                try {
                    disc_model.Volume.Unmount ();
                    disc_model.Volume.Eject ();
                    
                    ThreadAssist.ProxyToMain (delegate {
                        service.UnmapDiscVolume (disc_model.Volume.Uuid);
                        Dispose ();
                    });
                } catch (Exception e) {
                    ThreadAssist.ProxyToMain (delegate {
                        ClearMessages ();
                        eject_message.IsSpinning = false;
                        eject_message.SetIconName ("dialog-error");
                        eject_message.Text = String.Format (Catalog.GetString ("Could not eject audio CD: {0}"), e.Message);
                        PushMessage (eject_message);
                        
                        foreach (TrackInfo track in disc_model) {
                            track.CanPlay = true;
                        }
                        OnUpdated ();
                    });
                    
                    Log.Exception (e);
                }
            });
            
            return true;
        }

        public bool CanUnmap {
            get { return DiscModel != null ? !DiscModel.IsDoorLocked : true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return false; }
        }

#endregion

#region GUI/ThickClient

        private bool actions_loaded = false;

        private void SetupGui ()
        {                                       
            Properties.SetStringList ("Icon.Name", "media-cdrom", "gnome-dev-cdrom-audio", "source-cd-audio");
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Eject Disc"));
            Properties.SetString ("UnmapSourceActionIconName", "media-eject");
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/AudioCdContextMenu");
            
            actions_loaded = true;
            
            UpdateActions ();
        }
        
        private void UpdateActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            Gtk.Action rip_action = uia_service.GlobalActions["RipDiscAction"];
            if (rip_action != null) {
                string title = disc_model.Title;
                int max_title_length = 20;
                title = title.Length > max_title_length 
                    ? String.Format ("{0}\u2026", title.Substring (0, max_title_length).Trim ())
                    : title;
                rip_action.Label = String.Format (Catalog.GetString ("Import \u201f{0}\u201d"), title);
                rip_action.ShortLabel = Catalog.GetString ("Import CD");
                rip_action.IconName = "media-import-audio-cd";
                rip_action.Sensitive = AudioCdRipper.Supported && disc_model.EnabledCount > 0;
            }
            
            Gtk.Action duplicate_action = uia_service.GlobalActions["DuplicateDiscAction"];
            if (duplicate_action != null) {
                duplicate_action.IconName = "media-cdrom";
                duplicate_action.Visible = AudioCdDuplicator.Supported;
            }
        }
        
        protected override void OnUpdated ()
        {
            if (actions_loaded) {
                UpdateActions ();
            }
            
            base.OnUpdated ();
        }
        
#endregion

    }
}
