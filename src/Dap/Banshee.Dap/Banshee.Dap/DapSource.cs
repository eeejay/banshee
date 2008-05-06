//
// DapSource.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using Hyena.Data.Sqlite;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;
using Banshee.MediaEngine;
using Banshee.MediaProfiles;

namespace Banshee.Dap
{
    public abstract class DapSource : RemovableSource, IDisposable
    {
        private IDevice device;
        internal IDevice Device {
            get { return device; }
        }
        
        private string addin_id;
        internal string AddinId {
            get { return addin_id; }
            set { addin_id = value; }
        }
        
        protected DapSource ()
        {
        }

        public virtual void DeviceInitialize (IDevice device)
        {
            this.device = device;
            type_unique_id = device.Uuid;
        }

        public override void Dispose ()
        {
            PurgeBuiltinSmartPlaylists ();
            PurgeTracks ();
        }
        
        private void PurgeBuiltinSmartPlaylists ()
        {
            ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                BEGIN TRANSACTION;
                    DELETE FROM CoreSmartPlaylistEntries WHERE SmartPlaylistID IN
                        (SELECT SmartPlaylistID FROM CoreSmartPlaylists WHERE PrimarySourceID = ?);
                    DELETE FROM CoreSmartPlaylists WHERE PrimarySourceID = ?;   
                COMMIT TRANSACTION",
                DbId, DbId
            ));
        }

#region Source

        protected override void Initialize ()
        {
            PurgeBuiltinSmartPlaylists ();
            
            base.Initialize ();
            
            Expanded = true;
            Properties.SetStringList ("Icon.Name", GetIconNames ());
            Properties.Set<string> ("SourcePropertiesActionLabel", Catalog.GetString ("Device Properties"));
            Properties.Set<OpenPropertiesDelegate> ("SourceProperties.GuiHandler", delegate { 
                new DapPropertiesDialog (this).RunDialog (); 
            });

            if (String.IsNullOrEmpty (GenericName)) {
                GenericName = Catalog.GetString ("Media Player");
            }
            
            if (String.IsNullOrEmpty (Name)) {
                Name = device.Name;
            }
            
            acceptable_mimetypes = MediaCapabilities != null 
                ? MediaCapabilities.PlaybackMimeTypes 
                : new string [] { "taglib/mp3" };
        }
        
        public override void AddChildSource (Source child)
        {
            if (child is Banshee.Playlist.AbstractPlaylistSource && !(child is MediaGroupSource)) {
                Log.Information ("Note: playlists added to digital audio players within Banshee are not yet saved to the device.", true);
            }
            
            base.AddChildSource (child);
        }
        
        public override bool HasProperties {
            get { return true; }
        }
        
        public override bool CanActivate {
            get { return false; }
        }

        public override void SetStatus (string message, bool can_close, bool is_spinning, string icon_name)
        {
            base.SetStatus (message, can_close, is_spinning, icon_name);
            foreach (Source child in Children) {
                child.SetStatus (message, can_close, is_spinning, icon_name);
            }
        }

        public override void HideStatus ()
        {
            base.HideStatus ();
            foreach (Source child in Children) {
                child.HideStatus ();
            }
        }

#endregion
        
#region Track Management/Syncing   

        internal void LoadDeviceContents ()
        {
            ThreadPool.QueueUserWorkItem (ThreadedLoadDeviceContents);
        }
        
        private void ThreadedLoadDeviceContents (object state)
        {
            PurgeTracks ();
            SetStatus (String.Format (Catalog.GetString ("Loading {0}"), Name), false);
            LoadFromDevice ();
            OnTracksAdded ();
            HideStatus ();
            
            ThreadAssist.ProxyToMain (delegate {
                DatabaseSource src;
                AddChildSource (src = new MusicGroupSource (this));
                src.Reload ();
                AddChildSource (src = new VideoGroupSource (this));
                src.Reload ();
            });
        }

        protected virtual void LoadFromDevice ()
        {
        }
        
        protected abstract void AddTrackToDevice (DatabaseTrackInfo track, SafeUri fromUri);  

        protected bool TrackNeedsTranscoding (TrackInfo track)
        {
            foreach (string mimetype in AcceptableMimeTypes) {
                if (ServiceManager.MediaProfileManager.GetExtensionForMimeType (track.MimeType) == 
                    ServiceManager.MediaProfileManager.GetExtensionForMimeType (mimetype)) {
                    return false;
                }
            }

            return true;
        }

        protected override void AddTrackAndIncrementCount (DatabaseTrackInfo track)
        {
            if (!TrackNeedsTranscoding (track)) {
                AddTrackToDevice (track, track.Uri);
                IncrementAddedTracks ();
                return;
            }

            // If it's a video and needs transcoding, we don't support that yet
            // TODO have preferred profiles for Audio and Video separately
            if (PreferredConfiguration == null || (track.MediaAttributes & TrackMediaAttributes.VideoStream) != 0) {
                string format = System.IO.Path.GetExtension (track.Uri.LocalPath);
                format = String.IsNullOrEmpty (format) ? Catalog.GetString ("Unknown") : format.Substring (1);
                throw new ApplicationException (String.Format (Catalog.GetString (
                    "The {0} format is not supported by the device, and no converter was found to convert it."), format));
            }

            TranscoderService transcoder = ServiceManager.Get<TranscoderService> ();
            if (transcoder == null) {
                throw new ApplicationException (Catalog.GetString (
                    "File format conversion is not supported for this device."));
            }
            
            transcoder.Enqueue (track, PreferredConfiguration, OnTrackTranscoded, OnTrackTranscodeCancelled);
        }
        
        private void OnTrackTranscoded (TrackInfo track, SafeUri outputUri)
        {
            AddTrackJob.Status = String.Format ("{0} - {1}", track.ArtistName, track.TrackTitle);
            
            try {
                AddTrackToDevice ((DatabaseTrackInfo)track, outputUri);
            } catch (Exception e) {
                Log.Exception (e);
            }
            
            IncrementAddedTracks ();
        }
        
        private void OnTrackTranscodeCancelled ()
        {
            IncrementAddedTracks (); 
        }
        
#endregion

#region Device Properties

        protected virtual string [] GetIconNames ()
        {
            string vendor = device.Vendor;
            string product = device.Product;
            
            vendor = vendor != null ? vendor.Trim () : null;
            product = product != null ? product.Trim () : null;

            if (!String.IsNullOrEmpty (vendor) && !String.IsNullOrEmpty (product)) {
                return new string [] { 
                    String.Format ("{0}-{1}", vendor, product).Replace (' ', '-').ToLower (), 
                    FallbackIcon
                };
            } else {
                return new string [] { FallbackIcon };
            }
        }
        
        private string FallbackIcon {
            get { return "multimedia-player"; }
        }

        protected virtual bool HasMediaCapabilities {
            get { return MediaCapabilities != null; }
        }

        protected virtual IDeviceMediaCapabilities MediaCapabilities {
            get { return device.MediaCapabilities; }
        }
        
        private ProfileConfiguration preferred_config;
        private ProfileConfiguration PreferredConfiguration {
            get {
                if (preferred_config != null) {
                    return preferred_config;
                }
            
                MediaProfileManager manager = ServiceManager.MediaProfileManager;
                if (manager == null) {
                    return null;
                }
        
                preferred_config = manager.GetActiveProfileConfiguration (UniqueId, acceptable_mimetypes);
                return preferred_config;
            }
        }

        private string [] acceptable_mimetypes;
        public string [] AcceptableMimeTypes {
            get { return acceptable_mimetypes; }
            protected set { acceptable_mimetypes = value; }
        }
        
#endregion
        
    }
}
