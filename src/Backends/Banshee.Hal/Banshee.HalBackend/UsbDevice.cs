//
// UsbDevice.cs
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

using Banshee.Hardware;

namespace Banshee.HalBackend
{
    public class UsbDevice : Device, IUsbDevice
    {
        public static UsbDevice Resolve (Hal.Manager manager, Hal.Device device)
        {
            if (device["info.subsystem"] == "usb_device" && 
                device.PropertyExists ("usb_device.product_id") && 
                device.PropertyExists ("usb_device.vendor_id")) {
                return new UsbDevice (manager, device);
            }
            
            return null;
        }
        
        private UsbDevice (Hal.Manager manager, Hal.Device device) : base (manager, device)
        {
        }
        
        public int VendorId {
            get { return HalDevice.GetPropertyInteger ("usb_device.vendor_id"); }
        }
        
        public int ProductId {
            get { return HalDevice.GetPropertyInteger ("usb_device.product_id"); }
        }
        
        public override string Serial {
            get { return HalDevice.PropertyExists ("usb_device.serial") 
                ? HalDevice["usb_device.serial"] : null; }
        }
        
        public double Speed {
            get { return HalDevice.PropertyExists ("usb_device.speed") 
                ? HalDevice.GetPropertyDouble ("usb_device.speed") : 0.0; }
        }
        
        public double Version {
            get { return HalDevice.PropertyExists ("usb_device.version") 
                ? HalDevice.GetPropertyDouble ("usb_device.version") : 0.0; }
        }
    }
}
