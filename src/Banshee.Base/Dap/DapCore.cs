using System;
using System.Collections;
using System.Reflection;
using Hal;

using Banshee.Base;

namespace Banshee.Dap
{
    public static class DapCore
    {
        private static Hashtable device_table = new Hashtable();
        private static ArrayList supported_dap_types = new ArrayList();
    
        static DapCore()
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            foreach(Type type in asm.GetTypes()) {
                if(!type.IsSubclassOf(typeof(Dap))) {
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
        
        public static void Initialize()
        {
            if(supported_dap_types.Count <= 0) {
                return;
            }
            
            BuildDeviceTable();
            
            HalCore.DeviceAdded += delegate (object o, DeviceAddedArgs args) {
                AddDevice(args.Device);
            };
            
            HalCore.DeviceRemoved += delegate (object o, DeviceRemovedArgs args) {
                RemoveDevice(args.Device);
            };
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
                try {
                    AddDevice(device, Activator.CreateInstance(type, new object [] { device }) as Dap);
                } catch(Exception) {
                }
            }    
        }
        
        private static void AddDevice(Device device, Dap dap)
        {
            if(device == null || dap == null) {
                return;
            }
            
            device_table[device.Udi] = dap;
        }
        
        private static void RemoveDevice(Device device)
        {
            device_table.Remove(device.Udi);
        }
    }
}
