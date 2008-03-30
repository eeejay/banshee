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
using System.Threading;
using Mono.Unix;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;

namespace Banshee.AudioCd
{
    public class AudioCdSource : Source, ITrackModelSource, IUnmapableSource, IDurationAggregator, IDisposable
    {
        private AudioCdService service;
        private AudioCdDiscModel disc_model;
        private SourceMessage query_message;
        
        public AudioCdSource (AudioCdService service, AudioCdDiscModel discModel) 
            : base (Catalog.GetString ("Audio CD"), discModel.Title, 200)
        {
            this.service = service;
            this.disc_model = discModel;
            
            disc_model.MetadataQueryStarted += OnMetadataQueryStarted;
            disc_model.MetadataQueryFinished += OnMetadataQueryFinished;
            disc_model.LoadModelFromDisc ();
            
            Properties.SetStringList ("Icon.Name", "media-cdrom", "gnome-dev-cdrom-audio", "source-cd-audio");
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Eject Disc"));
            Properties.SetString ("UnmapSourceActionIconName", "media-eject");
        }
        
        public TimeSpan Duration {
            get { return disc_model.Duration; }
        }
        
        public void Dispose ()
        {
            ClearMessages ();
            disc_model.MetadataQueryStarted -= OnMetadataQueryStarted;
            disc_model.MetadataQueryFinished -= OnMetadataQueryFinished;
            service = null;
            disc_model = null;
        }
        
        public AudioCdDiscModel DiscModel {
            get { return disc_model; }
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
        
            if (query_message == null) {
                return;
            }
            
            if (disc_model.MetadataQuerySuccess) {
                DestroyQueryMessage ();
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

#region Source Overrides

        public override void Rename (string newName)
        {
            base.Rename (newName);
        }

        public override bool CanSearch {
            get { return false; }
        }
        
        protected override string TypeUniqueId {
            get { return "audio-cd"; }
        }
        
        public override int Count {
            get { return disc_model.Count; }
        }
        
        public override bool CanRename {
            get { return false; }
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
            AudioCdTrackInfo playing_track = ServiceManager.PlayerEngine.CurrentTrack as AudioCdTrackInfo;
            if (playing_track != null && playing_track.Model == disc_model) {
                ServiceManager.PlayerEngine.Close ();
            }
            
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
                    });
                    
                    Log.Exception (e);
                }
            });
            
            return true;
        }

        public bool CanUnmap {
            get { return true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return false; }
        }

#endregion

    }
}
