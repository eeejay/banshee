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

namespace Banshee.AudioCd
{
    public class AudioCdService : IExtensionService, IDisposable
    {
        private Dictionary<string, AudioCdSource> sources;
        private Section pref_section;
        
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
            }
        }
        
        public void Dispose ()
        {
            lock (this) {
                UninstallPreferences ();
            
                ServiceManager.HardwareManager.DeviceAdded -= OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved -= OnHardwareDeviceRemoved;
                
                foreach (AudioCdSource source in sources.Values) {
                    ServiceManager.SourceManager.RemoveSource (source);
                }
                
                sources.Clear ();
                sources = null;
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
        
        private void InstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null) {
                return;
            }
            
            pref_section = service["general"].Add (new Section ("audio-cd", Catalog.GetString ("Audio CD Importing"), 20));
            pref_section.Add (new SchemaPreference<bool> (ErrorCorrection, 
                Catalog.GetString ("Use error correction when importing"),
                Catalog.GetString ("Error correction tries to work around problem areas on a disc, such " +
                    "as surface scratches, but will slow down importing substantially.")));
        }
        
        private void UninstallPreferences ()
        {
            PreferenceService service = ServiceManager.Get<PreferenceService> ();
            if (service == null || pref_section == null) {
                return;
            }
            
            service["general"].Remove (pref_section);
            pref_section = null;
        }
        
        public static readonly SchemaEntry<bool> ErrorCorrection = new SchemaEntry<bool> (
            "import", "audio_cd_error_correction", 
            false,
            "Enable error correction",
            "When importing an audio CD, enable error correction (paranoia mode)"
        );

        string IService.ServiceName {
            get { return "AudioCdService"; }
        }
    }
}
