//
// DapSync.cs
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
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Query;

using Banshee.Configuration;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.SmartPlaylist;
using Banshee.Query;
using Banshee.Preferences;

namespace Banshee.Dap
{
    public sealed class DapSync
    {
        private DapSource dap;
        private string conf_ns;
        private List<DapLibrarySync> library_syncs = new List<DapLibrarySync> ();
        private SchemaEntry<bool> manually_manage, auto_sync;
        private SmartPlaylistSource to_remove;
        private Section dap_prefs_section;
        private List<Section> pref_sections = new List<Section> ();

        public event Action<DapSync> Updated;

        internal string ConfigurationNamespace {
            get { return conf_ns; }
        }
        
        #region Public Properites
        
        public DapSource Dap {
            get { return dap; }
        }

        public IEnumerable<DapLibrarySync> LibrarySyncs {
            get { return library_syncs; }
        }
        
        public bool Enabled {
            get { return manually_manage.Get (); }
        }
        
        public bool AutoSync {
            get { return auto_sync.Get (); }
        }

        public IEnumerable<Section> PreferenceSections {
            get { return pref_sections; }
        }
        
        #endregion
        
        public DapSync (DapSource dapSource)
        {
            dap = dapSource;
            BuildPreferences ();
            BuildSyncLists ();
        }

        private void BuildPreferences ()
        {
            conf_ns = String.Format ("{0}.{1}", dap.ConfigurationId, "sync");
            
            manually_manage = dap.CreateSchema<bool> (conf_ns, "enabled", true,
                Catalog.GetString ("Manually manage this device"),
                Catalog.GetString ("Manually managing your device means you can drag and drop items onto the device, and manually remove them.")
            );
            
            auto_sync = dap.CreateSchema<bool> (conf_ns, "auto_sync", false,
                Catalog.GetString ("Automaticlly sync the device when plugged in or the libraries change"),
                Catalog.GetString ("Begin synchronizing the device as soon as the device is plugged in or the libraries change.")
            );

            dap_prefs_section = new Section ("dap", Catalog.GetString ("Sync Preferences"), 0);
            dap_prefs_section.Add (manually_manage);
            dap_prefs_section.Add (auto_sync);
            pref_sections.Add (dap_prefs_section);
        }

        private void BuildSyncLists ()
        {
            int i = 0;
            foreach (Source source in ServiceManager.SourceManager.Sources) {
                if (source is LibrarySource) {
                    DapLibrarySync library_sync = new DapLibrarySync (this, source as LibrarySource);
                    library_syncs.Add (library_sync);
                    pref_sections.Add (library_sync.PrefsSection);
                    library_sync.PrefsSection.Order = ++i;
                }
            }

            bool first = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            foreach (DapLibrarySync sync in library_syncs) {
                if (first) {
                    first = false;
                } else {
                    sb.Append (",");
                }
                sb.Append (sync.SmartPlaylistId);
            }

            // Any items on the device that aren't in the sync lists need to be removed
            to_remove = new SmartPlaylistSource ("to_remove", dap.DbId);
            to_remove.IsTemporary = true;
            to_remove.Save ();
            to_remove.DatabaseTrackModel.AddCondition (String.Format (
                @"MetadataHash NOT IN (SELECT MetadataHash FROM CoreTracks, CoreSmartPlaylistEntries 
                    WHERE CoreSmartPlaylistEntries.SmartPlaylistID IN ({0}) AND
                        CoreTracks.TrackID = CoreSmartPlaylistEntries.TrackID)",
                sb.ToString ()
            ));
        }
        
        public int ItemCount {
            get { return 0; }
        }
        
        public long FileSize {
            get { return 0; }
        }
        
        public TimeSpan Duration {
            get { return TimeSpan.Zero; }
        }
        
        public void CalculateSync ()
        {
            foreach (DapLibrarySync library_sync in library_syncs) {
                library_sync.CalculateSync ();
            }
            to_remove.RefreshAndReload ();
            Log.Information (ToString ());

            Action<DapSync> handler = Updated;
            if (handler != null) {
                handler (this);
            }
        }

        public override string ToString ()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder ();
            foreach (DapLibrarySync library_sync in library_syncs) {
                sb.Append (library_sync.ToString ());
                sb.Append ("\n");
            }
            sb.Append (String.Format ("And {0} items to remove", to_remove.Count));
            return sb.ToString ();
        }

        
        public void Sync ()
        {
            // TODO: remove all items in to_remove
            foreach (DapLibrarySync library_sync in library_syncs) {
                library_sync.Sync ();
            }
        }
    }
}
