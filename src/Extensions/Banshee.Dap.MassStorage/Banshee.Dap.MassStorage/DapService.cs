//
// DapService.cs
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

namespace Banshee.Dap.MassStorage
{
    public class DapService : IExtensionService, IDisposable
    {
        private Dictionary<string, DapSource> sources;
        
        public DapService ()
        {
        }
        
        public void Initialize ()
        {
            lock (this) {
                sources = new Dictionary<string, DapSource> ();
                
                foreach (IDiskDevice device in ServiceManager.HardwareManager.GetAllDiskDevices ()) {
                    MapDiskDevice (device);
                }
                
                ServiceManager.HardwareManager.DeviceAdded += OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved += OnHardwareDeviceRemoved;
            }
        }
        
        public void Dispose ()
        {
            lock (this) {
                ServiceManager.HardwareManager.DeviceAdded -= OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved -= OnHardwareDeviceRemoved;
                
                foreach (DapSource source in sources.Values) {
                    ServiceManager.SourceManager.RemoveSource (source);
                }
                
                sources.Clear ();
                sources = null;
            }
        }
        
        private void MapDiskDevice (IDiskDevice device)
        {
            lock (this) {
                foreach (IVolume volume in device) {
                    MapDiskVolume (volume);
                }
            }
        }
        
        private void MapDiskVolume (IVolume volume)
        {
            lock (this) {
                if (!volume.ShouldIgnore && volume.Parent.IsRemovable && !sources.ContainsKey (volume.Uuid)) {
                    Log.DebugFormat ("Mapping disk device ({0}, mount point: {1})", volume.Uuid, volume.MountPoint);
                    MassStorageSource source = new MassStorageSource (volume);
                    sources.Add (volume.Uuid, source);
                    ServiceManager.SourceManager.AddSource (source);
                }
            }
        }
        
        internal void UnmapDiskVolume (string uuid)
        {
            lock (this) {
                if (sources.ContainsKey (uuid)) {
                    Log.DebugFormat ("Unmapping DAP source ({0})", uuid);
                    DapSource source = sources[uuid];
                    ServiceManager.SourceManager.RemoveSource (source);
                    sources.Remove (uuid);
                }
            }
        }
        
        private void OnHardwareDeviceAdded (object o, DeviceAddedArgs args)
        {
            lock (this) {
                if (args.Device is IVolume) {
                    if ((args.Device as IVolume).Parent is IDiskDevice) {
                        MapDiskVolume ((IVolume)args.Device);
                    }
                }
            }
        }
        
        private void OnHardwareDeviceRemoved (object o, DeviceRemovedArgs args)
        {
            lock (this) {
                UnmapDiskVolume (args.DeviceUuid);
            }
        }
        
        string IService.ServiceName {
            get { return "DapService"; }
        }
    }
}
