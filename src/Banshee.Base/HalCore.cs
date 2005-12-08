/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  HalCore.cs
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
        
        private static void HalDevicePropertyModified(IntPtr contextRaw, IntPtr udiRaw, IntPtr keyRaw,
            bool isRemoved, bool isAdded)
        {
            DevicePropertyModifiedHandler handler = DevicePropertyModified;
            if(handler != null) {
                DevicePropertyModifiedArgs args = new DevicePropertyModifiedArgs();
                args.Device = new Device(Context, UnixMarshal.PtrToString(udiRaw));
                args.Key = UnixMarshal.PtrToString(keyRaw);
                args.IsRemoved = isRemoved;
                args.IsAdded = isAdded;
                handler(Context, args);
            }
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
