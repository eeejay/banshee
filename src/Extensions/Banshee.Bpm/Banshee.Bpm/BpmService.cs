//
// BpmService.cs
//
// Authors:
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
using System.Data;

using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Library;
using Banshee.Metadata;
using Banshee.Networking;
using Banshee.Sources;
using Banshee.Preferences;

namespace Banshee.Bpm
{
    public class BpmService : IExtensionService
    {
        private BpmDetectJob job;
        private bool disposed;
        private object sync = new object ();
        
        public BpmService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            Banshee.MediaEngine.IBpmDetector detector = BpmDetectJob.GetDetector ();
            if (detector == null) {
                throw new ApplicationException ("No BPM detector available");
            } else {
                detector.Dispose ();
            }

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
        }
        
        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }
        
        private bool ServiceStartup ()
        {
            if (ServiceManager.SourceManager.MusicLibrary == null) {
                return false;
            }
            
            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;
            InstallPreferences ();

            Banshee.ServiceStack.Application.RunTimeout (4000, delegate {
                Detect ();
                return false;
            });
            
            return true;
        }
        
        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            UninstallPreferences ();
        
            disposed = true;
        }
        
        public void Detect ()
        {
            if (!Enabled) {
                return;
            }

            lock (sync) {
                if (job != null) {
                    return;
                } else {
                    job = new BpmDetectJob ();
                }
            }

            job.Finished += delegate { job = null; };
            job.RunAsync ();
        }
        
        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            Detect ();
        }
        
        string IService.ServiceName {
            get { return "BpmService"; }
        }

#region Preferences        

        private PreferenceBase enabled_pref;
        
        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            
            enabled_pref = service["general"]["misc"].Add (new SchemaPreference<bool> (EnabledSchema, 
                Catalog.GetString ("_Automatically detect BPM for all songs"),
                Catalog.GetString ("Detect BPM for all songs that don't already have a value set"),
                delegate { Enabled = EnabledSchema.Get (); }
            ));
        }
        
        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            
            service["general"]["misc"].Remove (enabled_pref);
            enabled_pref = null;
        }
        
#endregion

        public bool Enabled {
            get { return EnabledSchema.Get (); }
            set {
                EnabledSchema.Set (value);
                if (value) {
                    Detect ();
                } else {
                    if (job != null) {
                        job.Cancel ();
                    }
                }
            }
        }
        
        private static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.bpm", "auto_enabled",
            false,
            "Automatically detect BPM on imported music",
            "Automatically detect BPM on imported music"
        );
    }
}
