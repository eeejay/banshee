/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DapCore.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Reflection;
using Mono.Unix;
using Hal;

using Banshee.Base;

namespace Banshee.Dap
{
    public delegate void DapEventHandler(object o, DapEventArgs args);
    
    public class DapEventArgs : EventArgs
    {
        public DapDevice Dap;
        
        public DapEventArgs(DapDevice dap)
        {
            Dap = dap;
        }
    }

    public static class DapCore 
    {
        private static Hashtable device_table = new Hashtable();
        private static Hashtable device_waiting_table = new Hashtable();
        private static ArrayList supported_dap_types = new ArrayList();
    
        public static event DapEventHandler DapAdded;
        public static event DapEventHandler DapRemoved;
    
        static DapCore()
        {
            if(!HalCore.Initialized) {
                return;
            }
            
            DirectoryInfo di = null;
            FileInfo [] files = null;
            string dap_dir = ConfigureDefines.InstallDir + "Banshee.Dap" + Path.DirectorySeparatorChar;
            
            try {
                di = new DirectoryInfo(dap_dir);
                files = di.GetFiles();
                
                if(files == null || files.Length == 0) {
                    return;
                }
            } catch(Exception) {
                return;
            }

            foreach(FileInfo file in files) {
                Assembly asm;
                try {
                    asm = Assembly.LoadFrom(file.FullName);
                } catch(Exception) {
                    continue;
                }
                
                foreach(Type type in asm.GetTypes()) {
                    if(!type.IsSubclassOf(typeof(DapDevice))) {
                        continue;
                    }
                    
                    try {
                        Attribute [] dap_attrs = Attribute.GetCustomAttributes(type, typeof(DapProperties));
                        DapProperties dap_props = null;
                        if(dap_attrs != null && dap_attrs.Length >= 1) {
                            dap_props = dap_attrs[0] as DapProperties;
                        }
                        
                        if(dap_props.DapType == DapType.NonGeneric) {
                            supported_dap_types.Insert(0, type);
                        } else {
                            supported_dap_types.Add(type);
                        }
                    } catch(Exception) {
                        continue;
                    }
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
            
            HalCore.DeviceAdded += delegate(object o, DeviceAddedArgs args) {
                AddDevice(args.Device);
            };
            
            HalCore.DeviceRemoved += delegate(object o, DeviceRemovedArgs args) {
                RemoveDevice(args.Device);
            };
            
            HalCore.DevicePropertyModified += delegate(object o, DevicePropertyModifiedArgs args) {
                if(device_waiting_table[args.Device.Udi] != null) {
                    AddDevice(args.Device, device_waiting_table[args.Device.Udi] as Type);
                }
            };
        }
        
        public static void Dispose()
        {
            foreach(DapDevice device in Devices) {
                device.Dispose();
            }
        }
        
        private static void BuildDeviceTable()
        {
            foreach(Device device in Device.GetAll(HalCore.Context)) {
                AddDevice(device);
            }
        }
        
        private static void AddDevice(Device device)
        {
            foreach(Type type in supported_dap_types) {
                AddDevice(device, type);
            }    
        }
        
        private static void AddDevice(Device device, Type type)
        {
            try {
                AddDevice(device, Activator.CreateInstance(type, new object [] { device }) as DapDevice);
                device_waiting_table.Remove(device.Udi);
            } catch(TargetInvocationException te) {
                try {
                    throw te.InnerException;
                } catch(BrokenDeviceException e) {
                    LogCore.Instance.PushWarning(Catalog.GetString("Could not process connected DAP"), 
                        e.Message, false);
                } catch(CannotHandleDeviceException) {
                } catch(WaitForPropertyChangeException) {
                    device.WatchProperties = true;
                    device_waiting_table[device.Udi] = type;
                } catch(WaitForTimeoutException e) {
                    GLib.Timeout.Add(e.WaitSpan, delegate {
                        AddDevice(device);
                        return false;
                    });
                } catch(Exception e) {
                }
            }
        }
        
        private static void AddDevice(Device device, DapDevice dap)
        {
            if(device == null || dap == null) {
                return;
            }
            
            dap.HalDevice = device;
            dap.Ejected += OnDapEjected;
            device_table[device.Udi] = dap;
            
            if(DapAdded != null) {
                DapAdded(dap, new DapEventArgs(dap));
            }
        }
        
        private static void RemoveDevice(Device device)
        {
            DapDevice dap = device_table[device.Udi] as DapDevice;
            
            device_table.Remove(device.Udi);
            device_waiting_table.Remove(device.Udi);
            
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
            
            RemoveDevice(dap.HalDevice);
        }
        
        public static DapDevice [] Devices {
            get {
                ArrayList devices = new ArrayList();
                foreach(DapDevice dap in device_table.Values) {
                    devices.Add(dap);
                }
                return devices.ToArray(typeof(DapDevice)) as DapDevice [];
            }
        }
    }
}
