//
// DapService.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Mono.Addins;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;

namespace Banshee.Dap
{
    public class DapService : IExtensionService, IDisposable
    {
        private Dictionary<string, DapSource> sources;
        private List<TypeExtensionNode> supported_dap_types = new List<TypeExtensionNode>();
        
        public DapService ()
        {
        }
        
        public void Initialize ()
        {
            sources = new Dictionary<string, DapSource> ();
            AddinManager.AddExtensionNodeHandler ("/Banshee/Dap/DeviceClass", OnExtensionChanged);
            ServiceManager.HardwareManager.DeviceAdded += OnHardwareDeviceAdded;
            ServiceManager.HardwareManager.DeviceRemoved += OnHardwareDeviceRemoved;
            ServiceManager.SourceManager.SourceRemoved += OnSourceRemoved;
        }

        private void OnExtensionChanged (object o, ExtensionNodeEventArgs args) {
            TypeExtensionNode node = (TypeExtensionNode) args.ExtensionNode;
            if (args.Change == ExtensionChange.Add) {
                Log.DebugFormat ("Dap support extension loaded: {0}", node.Addin.Id);
                supported_dap_types.Add (node);

                // See if any existing devices are handled by this new DAP support
                foreach (IDevice device in ServiceManager.HardwareManager.GetAllDevices ()) {
                    MapDevice (device);
                }
            } else {
                // TODO remove/dispose all loaded DAPs of this type?
                supported_dap_types.Remove (node);
            }
        }

        public void Dispose ()
        {
            lock (this) {
                ServiceManager.HardwareManager.DeviceAdded -= OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved -= OnHardwareDeviceRemoved;
                ServiceManager.SourceManager.SourceRemoved -= OnSourceRemoved;
                
                ThreadPool.QueueUserWorkItem (delegate {
                    List<DapSource> dap_sources = new List<DapSource> (sources.Values);
                    foreach (DapSource source in dap_sources) {
                        UnmapDevice (source.Device.Uuid);
                    }
                    
                    sources.Clear ();
                    sources = null;
                });
            }
        }
        
        private void MapDevice (IDevice device)
        {
            lock (this) {
                if (sources.ContainsKey (device.Uuid))
                    return;

                if (device is ICdromDevice || device is IDiscVolume)
                    return;

                if (device is IVolume && (device as IVolume).ShouldIgnore)
                    return;

                if (device.MediaCapabilities == null && !(device is IBlockDevice) && !(device is IVolume))
                    return;

                DapSource source = FindDeviceType (device);
                if (source != null) {
                    Log.DebugFormat ("Found DAP support for device {0}", source.Name);
                    sources.Add (device.Uuid, source);
                    ServiceManager.SourceManager.AddSource (source);
                } else {
                    //Log.DebugFormat ("Did not find DAP support for device {0}", device.Uuid);
                }
            }
        }
        
        internal void UnmapDevice (string uuid)
        {
            lock (this) {
                if (sources.ContainsKey (uuid)) {
                    Log.DebugFormat ("Unmapping DAP source ({0})", uuid);

                    DapSource source = sources[uuid];
                    source.Dispose ();
                    sources.Remove (uuid);
                    ServiceManager.SourceManager.RemoveSource (source);
                }
            }
        }

        private void OnSourceRemoved (SourceEventArgs args)
        {
            DapSource dap_source = args.Source as DapSource;
            if (dap_source != null) {
                lock (this) {
                    UnmapDevice (dap_source.Device.Uuid);
                }
            }
        }
        
        private void OnHardwareDeviceAdded (object o, DeviceAddedArgs args)
        {
            lock (this) {
                MapDevice (args.Device);
            }
        }
        
        private void OnHardwareDeviceRemoved (object o, DeviceRemovedArgs args)
        {
            lock (this) {
                UnmapDevice (args.DeviceUuid);
            }
        }

        private DapSource FindDeviceType (IDevice device)
        {
            foreach (TypeExtensionNode node in supported_dap_types) {
                try {
                    DapSource src = (DapSource) node.CreateInstance ();
                    if (src.Resolve (device)) {
                        return src;
                    }
                } catch (Exception e) {
                    Log.Exception (e);
                }
            }
            return null;
        }
        
        string IService.ServiceName {
            get { return "DapService"; }
        }
    }
}
