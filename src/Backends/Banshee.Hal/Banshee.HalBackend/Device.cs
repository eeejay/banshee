//
// Device.cs
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

using Banshee.Hardware;

namespace Banshee.HalBackend
{
    public class Device : IDevice
    {
        private Hal.Device device;
        internal Hal.Device HalDevice {
            get { return device; }
        }
        
        private Hal.Manager manager;
        protected Hal.Manager HalManager {
            get { return manager; }
        }

        public Device (Hal.Manager manager, Hal.Device device)
        {
            this.manager = manager;
            this.device = device;
        }
        
        private string uuid;
        public string Uuid {
            get { return uuid ?? uuid = device.Udi; /*String.IsNullOrEmpty (HalDevice["usb.serial"]) ? device.Udi : HalDevice["usb.serial"];*/ }
        }

        private string serial;
        public virtual string Serial {
            get { return serial ?? serial = HalDevice["usb.serial"]; }
        }

        private string name;
        public virtual string Name {
            get {
                if (name == null) {
                    Stack<Hal.Device> usb_devices = CollectUsbDeviceStack (device);
                    while (usb_devices.Count > 0 && name == null) {
                        name = usb_devices.Pop() ["info.product"];
                    }
                    name = name ?? device["volume.label"];
                }

                return name;
            }
        }

        public virtual string Product {
            get { return device["info.product"]; }
        }

        public virtual string Vendor {
            get { return device["info.vendor"]; }
        }

        protected IDeviceMediaCapabilities media_capabilities;
        public IDeviceMediaCapabilities MediaCapabilities {
            get {
                if (media_capabilities == null && device.PropertyExists ("portable_audio_player.output_formats")) {
                    media_capabilities = new DeviceMediaCapabilities (device);
                }
                return media_capabilities;
            }
        }
        
        public bool PropertyExists (string key)
        {
            return device.PropertyExists (key);
        }
        
        public string GetPropertyString (string key)
        {
            return device.GetPropertyString (key);
        }
        
        public double GetPropertyDouble (string key)
        {
            return device.GetPropertyDouble (key);
        }
        
        public bool GetPropertyBoolean (string key)
        {
            return device.GetPropertyBoolean (key);
        }
        
        public int GetPropertyInteger (string key)
        {
            return device.GetPropertyInteger (key);
        }
        
        public ulong GetPropertyUInt64 (string key)
        {
            return device.GetPropertyUInt64 (key);
        }
        
        public string [] GetPropertyStringList (string key)
        {
            return device.GetPropertyStringList (key);
        }
        
        private static Stack<Hal.Device> CollectUsbDeviceStack (Hal.Device device)
        {
            Stack<Hal.Device> device_stack = new Stack<Hal.Device> ();
            int usb_vendor_id = -1;
            int usb_product_id = -1;

            Hal.Device tmp_device = device;

            while (tmp_device != null) {
                // Skip the SCSI parents of the volume if they are in the tree
                if ((tmp_device.PropertyExists("info.bus") && tmp_device["info.bus"] == "scsi") ||
                    (tmp_device.PropertyExists("info.category") && tmp_device["info.category"] == "scsi_host")) {
                    device_stack.Push (tmp_device);
                    tmp_device = tmp_device.Parent;
                    continue;
                }

                bool have_usb_ids = false;
                int _usb_vendor_id = -1;
                int _usb_product_id = -1;

                // Figure out the IDs if they exist
                if (tmp_device.PropertyExists ("usb.vendor_id") && 
                    tmp_device.PropertyExists ("usb.product_id")) {
                    _usb_vendor_id = tmp_device.GetPropertyInteger ("usb.vendor_id");
                    _usb_product_id = tmp_device.GetPropertyInteger ("usb.product_id");
                    have_usb_ids = true;
                } else if (tmp_device.PropertyExists("usb_device.vendor_id") && 
                    tmp_device.PropertyExists("usb_device.product_id")) {
                    _usb_vendor_id = tmp_device.GetPropertyInteger("usb_device.vendor_id");
                    _usb_product_id = tmp_device.GetPropertyInteger("usb_device.product_id");
                    have_usb_ids = true;
                }

                if (have_usb_ids) {
                    if (usb_vendor_id == -1 && usb_product_id == -1) {
                        // We found the first raw USB device, remember it
                        usb_vendor_id = _usb_vendor_id;
                        usb_product_id = _usb_product_id;
                    } else if (usb_vendor_id != _usb_vendor_id || usb_product_id != _usb_product_id) {
                        // We are no longer looking at the device we care about (could now be at a hub or something)
                        break;
                    }
                } else if (usb_vendor_id != -1 || usb_product_id != -1) {
                    // We are no longer even looking at USB devices
                    break;
                }

                device_stack.Push (tmp_device);
                tmp_device = tmp_device.Parent;
            }
            
            return device_stack;
        }
    }
}
