/***************************************************************************
 *  DapCore.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mono.Unix;
using Hal;

using Banshee.Base;
using Banshee.Plugins;

namespace Banshee.Dap
{
    public delegate void DapEventHandler(object o, DapEventArgs args);
    
    public class DapEventArgs : EventArgs
    {
        private DapDevice dap;
        
        public DapEventArgs(DapDevice dap)
        {
            this.dap = dap;
        }
        
        public DapDevice Dap {
            get { return dap; }
        }
    }

    public static class DapCore 
    {
        private static Dictionary<string, DapDevice> device_table = new Dictionary<string, DapDevice>();
        private static List<string> volume_mount_wait_list = new List<string>();
        private static List<Type> supported_dap_types = new List<Type>();
        private static uint volume_mount_wait_timeout = 0;
    
        public static event DapEventHandler DapAdded;
        public static event DapEventHandler DapRemoved;
    
        static DapCore()
        {
            if(!HalCore.Initialized) {
                return;
            }
            
            PluginFactory<DapDevice> plugin_factory = new PluginFactory<DapDevice>(PluginFactoryType.Type);
            plugin_factory.AddScanDirectory(ConfigureDefines.InstallDir + "Banshee.Dap");
            plugin_factory.IncludeMask = "*Dap.dll";
            plugin_factory.LoadPlugins();
            
            foreach(Type type in plugin_factory.PluginTypes) {
                try {
                    Attribute [] dap_attrs = Attribute.GetCustomAttributes(type, typeof(DapProperties));
                    DapProperties dap_props = null;
                    if(dap_attrs != null && dap_attrs.Length >= 1) {
                        dap_props = dap_attrs[0] as DapProperties;
                    }
                        
                    if(dap_props != null && dap_props.DapType == DapType.NonGeneric) {
                        supported_dap_types.Insert(0, type);
                    } else {
                        supported_dap_types.Add(type);
                    }
                } catch {
                    continue;
                }
            }
        }
        
        public static void Initialize()
        {
            if(!HalCore.Initialized) {
                throw new ApplicationException(Catalog.GetString(
                    "Cannot initialize DapCore because HalCore is not initialized"));
            }
            
            if(supported_dap_types.Count <= 0) {
                return;
            }
            
            BuildDeviceTable();
            
            HalCore.Manager.DeviceAdded += OnHalDeviceAdded;
            HalCore.Manager.DeviceRemoved += OnHalDeviceRemoved;
        }
        
        public static void Dispose()
        {
            if(volume_mount_wait_timeout > 0) {
                GLib.Source.Remove(volume_mount_wait_timeout);
                volume_mount_wait_timeout = 0;
            }
            
            foreach(DapDevice device in Devices) {
                device.Dispose();
            }
        }
        
        private static void OnHalDeviceAdded(object o, DeviceAddedArgs args)
        {
            AddDevice(args.Device);
        }
        
        private static void OnHalDeviceRemoved(object o, DeviceRemovedArgs args)
        {
            RemoveDevice(args.Udi);
        }
        
        internal static void QueueWaitForVolumeMount(Device device)
        {
            if(volume_mount_wait_list.Contains(device.Udi) || device_table.ContainsKey(device.Udi)) {
                return;
            }
            
            if(!device.PropertyExists("volume.policy.should_mount") || 
                !device.GetPropertyBoolean("volume.policy.should_mount")) {
                return;
            }
            
            volume_mount_wait_list.Add(device.Udi);
            volume_mount_wait_timeout = GLib.Timeout.Add(500, CheckVolumeMountWaitList);
        }
        
        private static bool CheckVolumeMountWaitList()
        {
            Queue<string> remove_queue = new Queue<string>();
        
            foreach(string udi in volume_mount_wait_list) {
                try {
                    Device device = new Device(udi);
                    if(device.GetPropertyBoolean("volume.is_mounted")) {
                        AddDevice(device);
                        remove_queue.Enqueue(udi);
                    }
                } catch {
                    remove_queue.Enqueue(udi);
                }
            }
            
            while(remove_queue.Count > 0) {
                volume_mount_wait_list.Remove(remove_queue.Dequeue());
            }
            
            if(volume_mount_wait_list.Count == 0) {
                volume_mount_wait_timeout = 0;
                return false;
            }
            
            return true;
        }
        
        private static void BuildDeviceTable()
        {
            // All volume devices, should cover all storage based players
            foreach(string udi in HalCore.Manager.FindDeviceByStringMatch("info.category", "volume")) {
                Device device = new Device(udi);
                if(device.PropertyExists("volume.policy.should_mount") && 
                    device.GetPropertyBoolean("volume.policy.should_mount")) {
                    if(device.PropertyExists("volume.is_disc") && 
                        device.GetPropertyBoolean("volume.is_disc")) {
                        continue;
                    }
                    
                    AddDevice(device);
                }
            }

            // Non-storage based players
            foreach(string udi in HalCore.Manager.FindDeviceByStringMatch("info.category", "portable_audio_player")) {
                AddDevice(new Device(udi));
            }
        }
        
        private static bool AddDevice(Device device)
        {
            foreach(Type type in supported_dap_types) {
                if(AddDevice(device, type)) {
                    return true;
                }
            }
            
            return false;
        }
        
        private static bool AddDevice(Device device, Type type)
        {
            try {
                DapDevice dap = Activator.CreateInstance(type) as DapDevice;
                switch(dap.Initialize(device)) {
                    case InitializeResult.Valid:
                        AddDevice(device, dap);
                        return true;
                    default:
                        break;
                }
            } catch(Exception e) {
                Console.WriteLine(e);
            }
            
            return false;
        }
        
        private static bool AddDevice(Device device, DapDevice dap)
        {
            if(device == null || dap == null) {
                return false;
            }
            
            dap.HalDevice = device;
            dap.Ejected += OnDapEjected;
            
            if(!device_table.ContainsKey(device.Udi)) {
                device_table.Add(device.Udi, dap);
            } else {
                device_table[device.Udi] = dap;
            }
            
            if(DapAdded != null) {
                DapAdded(dap, new DapEventArgs(dap));
            }
            
            return true;
        }
        
        private static void RemoveDevice(string udi)
        {
            if(!device_table.ContainsKey(udi)) {
                return;
            }
            
            DapDevice dap = device_table[udi];
            device_table.Remove(udi);
            
            foreach(string vmwl_udi in volume_mount_wait_list) {
                if(vmwl_udi == udi) {
                    volume_mount_wait_list.Remove(vmwl_udi);
                    break;
                }
            }
            
            if(dap == null) {
                return;
            }
            
            dap.Dispose();
            
            if(DapRemoved != null) {
                DapRemoved(dap, new DapEventArgs(dap));
            }
        }
        
        private static void OnDapEjected(object o, EventArgs args)
        {
            DapDevice dap = o as DapDevice;
            if(dap == null) {
                return;
            }
            
            RemoveDevice(dap.HalDevice.Udi);
        }
        
        public static IEnumerable<DapDevice> Devices {
            get { return device_table.Values; }
        }
    }
}
