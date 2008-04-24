//
// DapSource.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using Banshee.Hardware;
using Banshee.MediaEngine;
using Banshee.MediaProfiles;

namespace Banshee.Dap
{
    public abstract class DapSource : RemovableSource
    {
        protected IDevice device;
        protected string [] acceptable_mimetypes;

        internal IDevice Device {
            get { return device; }
        }

        protected DapSource () : base ()
        {
        }

        protected override void Initialize ()
        {
            base.Initialize ();

            if (!String.IsNullOrEmpty (device.Vendor) && !String.IsNullOrEmpty (device.Product)) {
                string device_icon_name = (device.Vendor.Trim () + "-" + device.Product.Trim ()).Replace (' ', '-').ToLower ();
                Properties.SetStringList ("Icon.Name", device_icon_name, FallbackIcon);
            } else {
                Properties.SetStringList ("Icon.Name", FallbackIcon);
            }

            Properties.Set<string> ("SourcePropertiesActionLabel", Catalog.GetString ("Device Properties"));
            Properties.Set<OpenPropertiesDelegate> ("SourceProperties.GuiHandler", delegate { new DapPropertiesDialog (this).RunDialog (); });

            GenericName = IsMediaDevice ? Catalog.GetString ("Audio Player") : Catalog.GetString ("Media Device");
            if (String.IsNullOrEmpty (Name))
                Name = device.Name;

            acceptable_mimetypes = (MediaCapabilities != null) ? MediaCapabilities.PlaybackMimeTypes : new string [] {"taglib/mp3"};
        }

        bool initialized = false;

        public bool Resolve (IDevice device)
        {
            this.device = device;
            type_unique_id = device.Uuid;
            bool ret = Initialize (device);
            initialized = true;
            return ret;
        }

        protected abstract bool Initialize (IDevice device);

        protected bool TrackNeedsTranscoding (TrackInfo track)
        {
            foreach (string mimetype in AcceptableMimeTypes) {
                if (ServiceManager.MediaProfileManager.GetExtensionForMimeType (track.MimeType) == ServiceManager.MediaProfileManager.GetExtensionForMimeType (mimetype)) {
                    return false;
                }
            }

            return true;
        }

        private ProfileConfiguration preferred_config;
        private ProfileConfiguration PreferredConfiguration {
            get {
                if (preferred_config == null) {
                    preferred_config = ServiceManager.MediaProfileManager.GetActiveProfileConfiguration (UniqueId, acceptable_mimetypes);
                }
                return preferred_config;
            }
        }

        protected override void AddTrackAndIncrementCount (DatabaseTrackInfo track)
        {
            if (TrackNeedsTranscoding (track)) {
                if (PreferredConfiguration == null) {
                    throw new Exception (Catalog.GetString ("Format not supported by device, and no converter found"));
                }

                ServiceManager.Get <TranscoderService> ().Enqueue (track, PreferredConfiguration, delegate (TrackInfo ti, SafeUri outUri) {
                    AddTrackJob.Status = String.Format ("{0} - {1}", track.ArtistName, track.TrackTitle);
                    try {
                        AddTrackToDevice (track, outUri);
                    } catch (Exception e) {
                        Log.Exception (e);
                    }
                    IncrementAddedTracks ();
                }, delegate { IncrementAddedTracks (); });
            } else {
                AddTrackToDevice (track, track.Uri);
                IncrementAddedTracks ();
            }
        }

        protected abstract void AddTrackToDevice (DatabaseTrackInfo track, SafeUri fromUri);

        protected virtual bool IsMediaDevice {
            get { return device.MediaCapabilities != null; }
        }

        protected virtual string FallbackIcon {
            get { return IsMediaDevice ? "multimedia-player" : "harddrive"; }
        }

        protected IDeviceMediaCapabilities MediaCapabilities {
            get { return device.MediaCapabilities; }
        }

        public string [] AcceptableMimeTypes {
            get { return acceptable_mimetypes; }
        }

        public override bool HasProperties {
            get { return true; }
        }

        public override void AddChildSource (Source child)
        {
            if (initialized && child is Banshee.Playlist.AbstractPlaylistSource) {
                Log.Information ("Note: playlists added to digital audio players within Banshee are not yet saved to the device.", true);
            }
            base.AddChildSource (child);
        }
    }
}
