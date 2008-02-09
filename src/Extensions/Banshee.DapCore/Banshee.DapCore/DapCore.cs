//
// DapCore.cs
//
// Author:
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

using Banshee.Base;
using Banshee.Hal;
using Banshee.ServiceStack;

using Hal;
using Mono.Addins;

using System;
using System.Collections.Generic;

namespace Banshee.Dap
{

    public class DapCore : IExtensionService
    {
        private HalCore halCore;
        private List<Type> supported_dap_types = new List<Type>();
        private Dictionary<string, IDevice> device_table = new Dictionary<string, IDevice>();

        private HalCore HalCore {
            get { return halCore; }
        }

        void IExtensionService.Initialize () {
            // TODO: This is the same construct as in NotificationAreaService.cs's Initialize.
            
            if (ServiceManager.Contains ("HalCore")) {
                ContinueInitialize ();
            } else {
                ServiceManager.ServiceStarted += delegate (ServiceStartedArgs args) {
                   if (args.Service is Banshee.Hal.HalCore) {
                       ContinueInitialize ();
                   }
                };
            }
        }

        private void OnExtensionChanged (object s, ExtensionNodeEventArgs args) {
            TypeExtensionNode node = (TypeExtensionNode) args.ExtensionNode;
            Console.WriteLine(">>> Found Node ID: {0}", node.Id);
            Type device_type = Type.GetType (node.Id);

            if (args.Change == ExtensionChange.Add) {
                // Register device plugin
                supported_dap_types.Add (device_type);
            } else {
                // Unregister device plugin
                supported_dap_types.Remove (device_type);
            }
        }

        private void ContinueInitialize () {
            // TODO: Hack!
            AddinManager.AddExtensionNodeHandler ("/Banshee/Dap/DeviceClass", OnExtensionChanged);
            halCore = ServiceManager.Get<HalCore> ("HalCore");
            HalCore.Manager.DeviceAdded += OnHalDeviceAdded;
            HalCore.Manager.DeviceRemoved += OnHalDeviceRemoved;

            BuildDeviceTable();
        }

        private void BuildDeviceTable ()
        {
            // All volume devices, should cover all storage based players
            foreach(string udi in HalCore.Manager.FindDeviceByStringMatch ("info.category", "volume")) {
                Device device = new Device (udi);
                if(device.PropertyExists ("volume.fsusage") && 
                    device["volume.fsusage"] == "filesystem") {
                    if((device.PropertyExists ("volume.is_disc") && 
                        device.GetPropertyBoolean ("volume.is_disc")) || 
                        (device.PropertyExists ("volume.ignore") &&
                        device.GetPropertyBoolean ("volume.ignore"))) {
                        continue;
                    }
                    
                    AddDeviceThreaded (device);
                }
            }

            // Non-storage based players
            foreach(string udi in HalCore.Manager.FindDeviceByStringMatch ("info.category", "portable_audio_player")) {
                AddDeviceThreaded (new Device (udi));
            }
        }

        private void OnHalDeviceAdded(object o, DeviceAddedArgs args)
        {
            Device device = args.Device;
            if(device["info.category"] == "portable_audio_player" ||
                (device["info.category"] == "volume" &&
                device.PropertyExists("volume.fsusage") &&
                device["volume.fsusage"] == "filesystem" &&
                (!device.PropertyExists("volume.is_disc") || 
                !device.GetPropertyBoolean("volume.is_disc")))) {
                AddDeviceThreaded (device);
            }
        }

        private void AddDeviceThreaded (Device device)
        {
            ThreadAssist.Spawn (delegate {
                AddDevice (device);
            });
        }

        private void AddDevice(Device device)
        {
            lock (device_table) {
                if (device_table.ContainsKey (device.Udi)) {
                    return;
                }

                try {
                    //DapDevice dap_device = new DapDevice (device);
                    //device_table.Add (device.Udi, dap_device);
                } catch (Exception e) {
                    Console.WriteLine (e);
                }
            }
        }
        
        private void OnHalDeviceRemoved(object o, DeviceRemovedArgs args)
        {
            // TODO: RemoveDevice(args.Udi);
        }

        string IService.ServiceName {
            get { return "DapCore"; }
        }
    }
}
