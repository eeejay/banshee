//
// HardwareManager.cs
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

using Mono.Addins;
using Hyena;

using Banshee.ServiceStack;

namespace Banshee.Hardware
{
    public sealed class HardwareManager : IService, IHardwareManager, IDisposable
    {
        private IHardwareManager manager;
        private Dictionary<string, ICustomDeviceProvider> custom_device_providers = new Dictionary<string, ICustomDeviceProvider> ();
        
        public event DeviceAddedHandler DeviceAdded;
        public event DeviceRemovedHandler DeviceRemoved;
        
        public HardwareManager ()
        {
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Banshee/Platform/HardwareManager")) {
                try {
                    if (manager != null) {
                        throw new Exception (String.Format (
                            "A HardwareManager has already been loaded ({0}). Only one can be loaded.", 
                            manager.GetType ().FullName));
                    }
                    
                    manager = (IHardwareManager)node.CreateInstance (typeof (IHardwareManager));
                } catch (Exception e) {
                    Log.Warning ("Hardware manager extension failed to load", e.Message);
                }
            }
            
            if (manager == null) {
                throw new Exception ("No HardwareManager extensions could be loaded. Hardware support will be disabled.");
            }
            
            manager.DeviceAdded += OnDeviceAdded;
            manager.DeviceRemoved += OnDeviceRemoved;
            
            AddinManager.AddExtensionNodeHandler ("/Banshee/Platform/HardwareDeviceProvider", OnExtensionChanged);
        }
        
        public void Dispose ()
        {
            lock (this) {
                if (manager != null) {
                    manager.DeviceAdded -= OnDeviceAdded;
                    manager.DeviceRemoved -= OnDeviceRemoved;
                    manager.Dispose ();
                    manager = null;
                }
                
                if (custom_device_providers != null) {
                    foreach (ICustomDeviceProvider provider in custom_device_providers.Values) {
                        provider.Dispose ();
                    }
                    
                    custom_device_providers.Clear ();
                    custom_device_providers = null;
                }
                
                AddinManager.RemoveExtensionNodeHandler ("/Banshee/Platform/HardwareDeviceProvider", OnExtensionChanged);
            }
        }
        
        private void OnExtensionChanged (object o, ExtensionNodeEventArgs args) 
        {
            lock (this) {
                TypeExtensionNode node = (TypeExtensionNode)args.ExtensionNode;
                
                if (args.Change == ExtensionChange.Add && !custom_device_providers.ContainsKey (node.Id)) {
                    custom_device_providers.Add (node.Id, (ICustomDeviceProvider)node.CreateInstance ());
                } else if (args.Change == ExtensionChange.Remove && custom_device_providers.ContainsKey (node.Id)) {
                    ICustomDeviceProvider provider = custom_device_providers[node.Id];
                    provider.Dispose ();
                    custom_device_providers.Remove (node.Id);
                }
            }
        }
        
        private void OnDeviceAdded (object o, DeviceAddedArgs args)
        {
            lock (this) {
                DeviceAddedHandler handler = DeviceAdded;
                if (handler != null) {
                    DeviceAddedArgs raise_args = args;
                    IDevice cast_device = CastToCustomDevice<IDevice> (args.Device);
                    
                    if (cast_device != args.Device) {
                        raise_args = new DeviceAddedArgs (cast_device);
                    }
                    
                    handler (this, raise_args);
                }
            }
        }
        
        private void OnDeviceRemoved (object o, DeviceRemovedArgs args)
        {
            lock (this) {
                DeviceRemovedHandler handler = DeviceRemoved;
                if (handler != null) {
                    handler (this, args);
                }
            }
        }
        
        private T CastToCustomDevice<T> (T device) where T : class, IDevice
        {
            foreach (ICustomDeviceProvider provider in custom_device_providers.Values) {
                T new_device = provider.GetCustomDevice (device);
                if (new_device != device && new_device is T) {
                    return new_device;
                }
            }
            
            return device;
        }
        
        private IEnumerable<T> CastToCustomDevice<T> (IEnumerable<T> devices) where T : class, IDevice
        {
            foreach (T device in devices) {
                yield return CastToCustomDevice<T> (device);
            }
        }

        public IEnumerable<IDevice> GetAllDevices ()
        {
            return CastToCustomDevice<IDevice> (manager.GetAllDevices ());
        }
        
        public IEnumerable<IBlockDevice> GetAllBlockDevices ()
        {
            return CastToCustomDevice<IBlockDevice> (manager.GetAllBlockDevices ());
        }
        
        public IEnumerable<ICdromDevice> GetAllCdromDevices ()
        {
            return CastToCustomDevice<ICdromDevice> (manager.GetAllCdromDevices ());
        }
        
        public IEnumerable<IDiskDevice> GetAllDiskDevices ()
        {
            return CastToCustomDevice<IDiskDevice> (manager.GetAllDiskDevices ());
        }
        
        public void Test ()
        {
            Console.WriteLine ("All Block Devices:");
            PrintBlockDeviceList (GetAllBlockDevices ());
            
            Console.WriteLine ("All CD-ROM Devices:");
            PrintBlockDeviceList (GetAllCdromDevices ());
            
            Console.WriteLine ("All Disk Devices:");
            PrintBlockDeviceList (GetAllDiskDevices ());
        }
        
        private void PrintBlockDeviceList (System.Collections.IEnumerable devices)
        {
            foreach (IBlockDevice device in devices) {
                Console.WriteLine ("{0}, {1}", device.GetType ().FullName, device);
                foreach (IVolume volume in device.Volumes) {
                    Console.WriteLine ("  {0}, {1}", volume.GetType ().FullName, volume);
                }
            }
        }
             
        string IService.ServiceName {
            get { return "HardwareManager"; }
        } 
    }
}
