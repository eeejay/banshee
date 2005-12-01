using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using Hal;

namespace Banshee.Base
{
    public static class HalCore 
    {
        private delegate void HalDeviceCallback(IntPtr context, IntPtr udi);
        private delegate void HalDevicePropertyModifiedCallback(IntPtr context, IntPtr udi, IntPtr key, 
            bool is_removed, bool is_added);
        
        [DllImport("libbanshee")]
        private static extern IntPtr hal_context_new(out IntPtr error_out,
            HalDeviceCallback added_cb, HalDeviceCallback removed_cb, 
            HalDevicePropertyModifiedCallback property_modified_cb);
        
        public static event DeviceAddedHandler DeviceAdded;
        public static event DeviceRemovedHandler DeviceRemoved;
        public static event DevicePropertyModifiedHandler DevicePropertyModified;
        
        private static IntPtr hal_context_raw;
        private static Context hal_context;
        
        private static void HalDeviceAdded(IntPtr contextRaw, IntPtr udiRaw)
        {
            Event.Invoke(DeviceAdded, Context, delegate {
                DeviceAddedArgs args = new DeviceAddedArgs();
                args.Device = new Device(Context, UnixMarshal.PtrToString(udiRaw));
                return args;
            });  
        }
        
        private static void HalDeviceRemoved(IntPtr contextRaw, IntPtr udiRaw)
        {
            Event.Invoke(DeviceRemoved, Context, delegate {
                DeviceRemovedArgs args = new DeviceRemovedArgs();
                args.Device = new Device(Context, UnixMarshal.PtrToString(udiRaw));
                return args;
            });  
        }
        
        private static void HalDevicePropertyModified(IntPtr contextRaw, IntPtr udiRaw, IntPtr keyRaw,
            bool isRemoved, bool isAdded)
        {
            Event.Invoke(DevicePropertyModified, Context, delegate {
                DevicePropertyModifiedArgs args = new DevicePropertyModifiedArgs();
                args.Device = new Device(Context, UnixMarshal.PtrToString(udiRaw));
                args.Key = UnixMarshal.PtrToString(keyRaw);
                args.IsRemoved = isRemoved;
                args.IsAdded = isAdded;
                return args;
            });
        }
        
        static HalCore()
        {
            try {
                IntPtr error_ptr;
                hal_context_raw = hal_context_new(out error_ptr, HalDeviceAdded, 
                    HalDeviceRemoved, HalDevicePropertyModified);
                if(hal_context_raw == IntPtr.Zero) {
                    string error = error_ptr == IntPtr.Zero 
                        ? Catalog.GetString("HAL could not be initialized")
                        : UnixMarshal.PtrToString(error_ptr);
                    throw new ApplicationException(error);
                }
                
                hal_context = new Context(hal_context_raw);
            } catch(Exception e) {
                LogCore.Instance.PushWarning(Catalog.GetString("HAL context could not be created"),
                    e.Message + "; " + Catalog.GetString("D-Bus may not be working or configured properly"), false);
            }
        }
        
        public static void Dispose()
        {
            if(hal_context != null) {
                hal_context.Dispose();
            }
        }
        
        public static Context Context {
            get {
                return hal_context;
            }
        }
        
        public static bool Initialized {
            get {
                return hal_context != null;
            }
        }
    }
}
