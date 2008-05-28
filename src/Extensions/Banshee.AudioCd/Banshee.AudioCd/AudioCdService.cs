//
// AudioCdService.cs
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
using Mono.Unix;

using Hyena;

using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Preferences;
using Banshee.Hardware;
using Banshee.Gui;

namespace Banshee.AudioCd
{
    public class AudioCdService : IExtensionService, IDisposable
    {
        private Dictionary<string, AudioCdSource> sources;
        private Page pref_page;
        private Section pref_section;
        private uint global_interface_id;
        
        public AudioCdService ()
        {
        }
        
        public void Initialize ()
        {
            lock (this) {
                InstallPreferences ();
            
                sources = new Dictionary<string, AudioCdSource> ();
                
                foreach (ICdromDevice device in ServiceManager.HardwareManager.GetAllCdromDevices ()) {
                    MapCdromDevice (device);
                }
                
                ServiceManager.HardwareManager.DeviceAdded += OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved += OnHardwareDeviceRemoved;
                
                SetupActions ();
            }
        }
        
        public void Dispose ()
        {
            lock (this) {
                UninstallPreferences ();
            
                ServiceManager.HardwareManager.DeviceAdded -= OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved -= OnHardwareDeviceRemoved;
                
                foreach (AudioCdSource source in sources.Values) {
                    source.Dispose ();
                    ServiceManager.SourceManager.RemoveSource (source);
                }
                
                sources.Clear ();
                sources = null;
                
                DisposeActions ();
            }    
        }
        
        private void MapCdromDevice (ICdromDevice device)
        {
            lock (this) {
                foreach (IVolume volume in device) {
                    if (volume is IDiscVolume) {
                        MapDiscVolume ((IDiscVolume)volume);
                    }
                }
            }
        }
        
        private void MapDiscVolume (IDiscVolume volume)
        {
            lock (this) {
                if (!sources.ContainsKey (volume.Uuid) && volume.HasAudio) {
                    AudioCdSource source = new AudioCdSource (this, new AudioCdDiscModel (volume));
                    sources.Add (volume.Uuid, source);
                    ServiceManager.SourceManager.AddSource (source);
                    Log.DebugFormat ("Mapping audio CD ({0})", volume.Uuid);
                }
            }
        }
        
        internal void UnmapDiscVolume (string uuid)
        {
            lock (this) {
                if (sources.ContainsKey (uuid)) {
                    AudioCdSource source = sources[uuid];
                    ServiceManager.SourceManager.RemoveSource (source);
                    sources.Remove (uuid);
                    Log.DebugFormat ("Unmapping audio CD ({0})", uuid);
                }
            }
        }
        
        private void OnHardwareDeviceAdded (object o, DeviceAddedArgs args)
        {
            lock (this) {
                if (args.Device is ICdromDevice) {
                    MapCdromDevice ((ICdromDevice)args.Device);
                } else if (args.Device is IDiscVolume) {
                    MapDiscVolume ((IDiscVolume)args.Device);
                }
            }
        }
        
        private void OnHardwareDeviceRemoved (object o, DeviceRemovedArgs args)
        {
            lock (this) {
                UnmapDiscVolume (args.DeviceUuid);
            }
        }
        
#region Preferences        
        
        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            
            service.InstallWidgetAdapters += OnPreferencesServiceInstallWidgetAdapters;
            
            pref_page = new Page ("audio-cd", Catalog.GetString ("Audio CD"), 3);
            service.Add (pref_page);
            
            pref_section = pref_page.Add (new Section ("audio-cd", 
                Catalog.GetString ("Audio CD Importing"), 20));

            pref_section.Add (new VoidPreference ("import-profile",  Catalog.GetString ("_Import format")));
            pref_section.Add (new VoidPreference ("import-profile-desc"));

            pref_section.Add (new SchemaPreference<bool> (AutoRip, 
                Catalog.GetString ("_Automatically import audio CDs when inserted"),
                Catalog.GetString ("When an audio CD is inserted, automatically begin importing it " +
                    "if metadata can be found and it is not already in the library.")));

            pref_section.Add (new SchemaPreference<bool> (EjectAfterRipped, 
                Catalog.GetString ("_Eject when done importing"),
                Catalog.GetString ("When an audio CD has been imported, automatically eject it.")));
            
            pref_section.Add (new SchemaPreference<bool> (ErrorCorrection, 
                Catalog.GetString ("Use error correction when importing"),
                Catalog.GetString ("Error correction tries to work around problem areas on a disc, such " +
                    "as surface scratches, but will slow down importing substantially.")));
        }
        
        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null || pref_page == null) {
                return;
            }
            
            service.InstallWidgetAdapters -= OnPreferencesServiceInstallWidgetAdapters;
            
            service.Remove (pref_page);
            pref_page = null;
            pref_section = null;
        }
        
        private void OnPreferencesServiceInstallWidgetAdapters (object o, EventArgs args)
        {
            if (pref_section == null) {
                return;
            }
            
            Gtk.HBox description_box = new Gtk.HBox ();
            Banshee.MediaProfiles.Gui.ProfileComboBoxConfigurable chooser 
                = new Banshee.MediaProfiles.Gui.ProfileComboBoxConfigurable (ServiceManager.MediaProfileManager, 
                    "cd-importing", description_box);
            
            pref_section["import-profile"].DisplayWidget = chooser;
            pref_section["import-profile"].MnemonicWidget = chooser.Combo;
            pref_section["import-profile-desc"].DisplayWidget = description_box;
        }
        
        public static readonly SchemaEntry<bool> ErrorCorrection = new SchemaEntry<bool> (
            "import", "audio_cd_error_correction", 
            false,
            "Enable error correction",
            "When importing an audio CD, enable error correction (paranoia mode)"
        );

        public static readonly SchemaEntry<bool> AutoRip = new SchemaEntry<bool> (
            "import", "auto_rip_cds", 
            false,
            "Enable audio CD auto ripping",
            "When an audio CD is inserted, automatically begin ripping it."
        );

        public static readonly SchemaEntry<bool> EjectAfterRipped = new SchemaEntry<bool> (
            "import", "eject_after_ripped", 
            false,
            "Eject audio CD after ripped",
            "After an audio CD has been ripped, automatically eject it."
        );

#endregion

#region UI Actions

        private void SetupActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            uia_service.GlobalActions.AddImportant (new Gtk.ActionEntry [] {
                new Gtk.ActionEntry ("RipDiscAction", null,
                    Catalog.GetString ("Import CD"), null,
                    Catalog.GetString ("Import this audio CD to the library"),
                    OnImportDisc)
            });
            
            uia_service.GlobalActions.AddImportant (
                new Gtk.ActionEntry ("DuplicateDiscAction", null,
                    Catalog.GetString ("Duplicate CD"), null,
                    Catalog.GetString ("Duplicate this audio CD"),
                    OnDuplicateDisc)
            );
            
            global_interface_id = uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");
        }
        
        private void DisposeActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            uia_service.GlobalActions.Remove ("RipDiscAction");
            uia_service.GlobalActions.Remove ("DuplicateDiscAction");
            uia_service.UIManager.RemoveUi (global_interface_id);
        }
        
        private void OnImportDisc (object o, EventArgs args)
        {
            ImportOrDuplicateDisc (true);
        }
        
        private void OnDuplicateDisc (object o, EventArgs args)
        {
            ImportOrDuplicateDisc (false);
        }
        
        private void ImportOrDuplicateDisc (bool import)
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }
            
            AudioCdSource source = uia_service.SourceActions.ActionSource as AudioCdSource;
            if (source != null) {
                if (import) {
                    source.ImportDisc ();
                } else {
                    source.DuplicateDisc ();
                }
            }
        }

#endregion

        string IService.ServiceName {
            get { return "AudioCdService"; }
        }
    }
}
