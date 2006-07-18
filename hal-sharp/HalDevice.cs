/***************************************************************************
 *  HalDevice.cs
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
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Hal
{
    public class Device
    {
        public enum FindBy : uint {
            Capability,
            StringMatch,
            MatchAll
        };
    
        private string udi;
        private Context ctx;
        
        public Device(Context ctx, string udi)
        {
            if(ctx == null) {
                throw new ApplicationException("Cannot create HAL device; context is null");
            }

            this.ctx = ctx;
            this.udi = udi;
        }
        
        public void Print()
        {
            Unmanaged.libhal_device_print(ctx.Raw, udi, IntPtr.Zero);
        }

        public bool PropertyExists(string key)
        {
            return Unmanaged.libhal_device_property_exists(ctx.Raw, udi, key, IntPtr.Zero);
        }

        public string [] GetPropertyStringList(string key)
        {
            IntPtr ptr;
            string [] properties;

            ptr = Unmanaged.libhal_device_get_property_strlist(ctx.Raw, udi, key, IntPtr.Zero);
            properties = UnixMarshal.PtrToStringArray(ptr);
            Unmanaged.libhal_free_string_array(ptr);

            return properties;
        }
        
        public bool PropertyStringListAppend(string key, string val)
        {
            return Unmanaged.libhal_device_property_strlist_append(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public bool PropertyStringListPrepend(string key, string val)
        {
            return Unmanaged.libhal_device_property_strlist_append(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public bool PropertyStringListRemove(string key, string val)
        {
            return Unmanaged.libhal_device_property_strlist_remove(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public bool PropertyStringListRemoveIndex(string key, uint index)
        {
            return Unmanaged.libhal_device_property_strlist_remove_index(ctx.Raw, udi, key, index, IntPtr.Zero);
        }
        
        public string GetPropertyString(string key)
        {
            IntPtr ptr = Unmanaged.libhal_device_get_property_string(ctx.Raw, udi, key, IntPtr.Zero);
            string str = UnixMarshal.PtrToString(ptr);
            Unmanaged.libhal_free_string(ptr);
            return str;
        }
        
        public bool SetPropertyString(string key, string val)
        {
            return Unmanaged.libhal_device_set_property_string(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public int GetPropertyInt(string key)
        {
            return Unmanaged.libhal_device_get_property_int(ctx.Raw, udi, key, IntPtr.Zero);
        }

        public bool SetPropertyInt(string key, int val)
        {
            return Unmanaged.libhal_device_set_property_int(ctx.Raw,  udi, key, val, IntPtr.Zero);
        }
        
        public UInt64 GetPropertyUint64(string key)
        {
            return Unmanaged.libhal_device_get_property_uint64(ctx.Raw, udi, key, IntPtr.Zero);
        }
        
        public bool SetPropertyString(string key, UInt64 val)
        {
            return Unmanaged.libhal_device_set_property_uint64(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public double GetPropertyDouble(string key)
        {
            return Unmanaged.libhal_device_get_property_double(ctx.Raw, udi, key, IntPtr.Zero);
        }
        
        public bool SetPropertyDouble(string key, double val)
        {
            return Unmanaged.libhal_device_set_property_double(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public bool GetPropertyBool(string key)
        {
            return Unmanaged.libhal_device_get_property_bool(ctx.Raw, udi, key, IntPtr.Zero);
        }
        
        public bool SetPropertyBool(string key, bool val)
        {
            return Unmanaged.libhal_device_set_property_bool(ctx.Raw, udi, key, val, IntPtr.Zero);
        }
        
        public bool AddCapability(string capability)
        {
            return Unmanaged.libhal_device_add_capability(ctx.Raw, udi, capability, IntPtr.Zero);
        }
        
        public bool QueryCapability(string capability)
        {
            return Unmanaged.libhal_device_query_capability(ctx.Raw, udi, capability, IntPtr.Zero);
        }
        
        public void Lock(string reason)
        {
            string reason_why_locked;
            if(!Unmanaged.libhal_device_lock(ctx.Raw, udi, reason, out reason_why_locked, IntPtr.Zero)) {
                throw new HalException("Could not lock device: " + reason_why_locked);
            }
        }
        
        public bool Unlock()
        {
            return Unmanaged.libhal_device_unlock(ctx.Raw, udi, IntPtr.Zero);
        }
        
        public bool EmitCondition(string conditionName, string conditionDetails)
        {
            return Unmanaged.libhal_device_emit_condition(ctx.Raw, udi, conditionName, conditionDetails, IntPtr.Zero);
        }
        
        public bool Rescan()
        {
            return Unmanaged.libhal_device_rescan(ctx.Raw, udi, IntPtr.Zero);
        }
        
        public bool Reprobe()
        {
            return Unmanaged.libhal_device_reprobe(ctx.Raw, udi, IntPtr.Zero);
        }
        
        // Property Watching... Probably won't live here; will have to see
        // when the callback wrapping is implemented
        
        public bool WatchProperties
        {
            set {
                DBusError error = new DBusError();
                
                bool result = value 
                    ? Unmanaged.libhal_device_add_property_watch(ctx.Raw, udi, error.Raw) 
                    : Unmanaged.libhal_device_remove_property_watch(ctx.Raw, udi, error.Raw);

                error.ThrowExceptionIfSet("Could not update watch on property");

                if(!result) {
                    throw new HalException("Could not " + (value ? "add" : "remove") + " property watch");
                }
            }
        }

        public string this [string key]
        {
            get {
                return GetPropertyString(key);
            }
            
            set {
                if(!SetPropertyString(key, value)) {
                    throw new HalException("Could not set property '" + key + "'");
                }
            }
        }
        
        public Device Parent {
            get {
                if(!PropertyExists("info.parent")) {
                    return null;
                }
                
                string parent_udi = this["info.parent"];
                if(parent_udi != null && parent_udi != String.Empty) {
                    return new Device(ctx, parent_udi);
                }
                
                return null;
            }
        }
        
        public Context Context {
            get {
                return ctx;
            }
        }
        
        public override string ToString()
        {
            return udi;
        }
        
        public string Udi {
            get {
                return udi;
            }
        }
        
        public bool Exists {
            get {
                return Unmanaged.libhal_device_exists(ctx.Raw, 
                    udi, IntPtr.Zero);
            }
        }
        
        // static members
        
        public static bool DeviceExists(Context ctx, string udi)
        {
            return ctx == null ? false : Unmanaged.libhal_device_exists(ctx.Raw, udi, IntPtr.Zero);
        }
        
        public static string [] FindUdis(Context ctx, FindBy findMethod, string key, string query)
        {
            IntPtr ptr;
            string [] deviceUdis;
            int device_count;

            if(ctx == null) {
                return new string[0];
            }

            switch(findMethod) {
                case FindBy.StringMatch:
                    ptr = Unmanaged.libhal_manager_find_device_string_match(
                        ctx.Raw, key, query, out device_count, IntPtr.Zero);
                    break;
                case FindBy.Capability:
                    ptr = Unmanaged.libhal_find_device_by_capability(ctx.Raw,
                        query, out device_count, IntPtr.Zero);
                    break;
                case FindBy.MatchAll:
                default:
                    ptr = Unmanaged.libhal_get_all_devices(ctx.Raw,
                        out device_count, IntPtr.Zero);
                    break;
            }
            
            deviceUdis = UnixMarshal.PtrToStringArray(device_count, ptr);
            Unmanaged.libhal_free_string_array(ptr);
            
            return deviceUdis;
        }
        
        public static Device [] UdisToDevices(Context ctx, string [] udis)
        {
            if(ctx == null) {
                return new Device[0];
            }
        
            Device [] devices = new Device[udis.Length];
            
            for(int i = 0; i < udis.Length; i++) {
                devices[i] = new Device(ctx, udis[i]);
            }
            
            return devices;
        }
        
        public static string [] GetAllUdis(Context ctx)
        {
            return FindUdis(ctx, FindBy.MatchAll, null, null);
        }
        
        public static Device [] GetAll(Context ctx)
        {
            return UdisToDevices(ctx, GetAllUdis(ctx));
        }

        public static string [] FindUdiByStringMatch(Context ctx, string key, 
            string val)
        {
            return FindUdis(ctx, FindBy.StringMatch, key, val);
        }
        
        public static Device [] FindByStringMatch(Context ctx, string key, 
            string val)
        {
            return UdisToDevices(ctx, FindUdiByStringMatch(ctx, key, val));
        }
    
        public static string [] FindUdiByCapability(Context ctx, string cap)
        {
            return FindUdis(ctx, FindBy.Capability, null, cap);
        }
        
        public static Device [] FindByCapability(Context ctx, string cap)
        {
            return UdisToDevices(ctx, FindUdiByCapability(ctx, cap));
        }
    }
}
