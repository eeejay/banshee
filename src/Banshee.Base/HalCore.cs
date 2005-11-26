using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using Hal;

namespace Banshee.Base
{
    public static class HalCore 
    {
        private delegate void HalDeviceCallback(IntPtr context, IntPtr udi);
        
        [DllImport("libbanshee")]
        private static extern IntPtr hal_context_new(out IntPtr error_out,
            HalDeviceCallback added_cb, HalDeviceCallback removed_cb);
        
        public static event DeviceAddedHandler DeviceAdded;
        public static event DeviceRemovedHandler DeviceRemoved;
        
        private static IntPtr hal_context_raw;
        private static Context hal_context;
        
        private static void HalDeviceAdded(IntPtr contextRaw, IntPtr udiRaw)
        {
            DeviceAddedHandler handler = DeviceAdded;
            
            if(handler != null) {
                DeviceAddedArgs args = new DeviceAddedArgs();
                args.Device = new Device(Context, UnixMarshal.PtrToString(udiRaw));
                handler(Context, args);
            }
        }
        
        private static void HalDeviceRemoved(IntPtr contextRaw, IntPtr udiRaw)
        {
            DeviceRemovedHandler handler = DeviceRemoved;
            
            if(handler != null) {
                DeviceRemovedArgs args = new DeviceRemovedArgs();
                args.Device = new Device(Context, UnixMarshal.PtrToString(udiRaw));
                handler(Context, args);
            }    
        }
        
        static HalCore()
        {
            IntPtr error_ptr;
            hal_context_raw = hal_context_new(out error_ptr, HalDeviceAdded, HalDeviceRemoved);
            hal_context = new Context(hal_context_raw);
        }
        
        public static void Dispose()
        {
            hal_context.Dispose();    
        }
        
        public static Context Context {
            get {
                return hal_context;
            }
        }
    }
}
