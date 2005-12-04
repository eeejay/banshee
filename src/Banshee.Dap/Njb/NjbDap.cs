/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  NjbDap.cs
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
using Hal;
using NJB=Njb;
using Banshee.Dap;
using Banshee.Base;

namespace Banshee.Dap.Njb
{
    [DapProperties(DapType = DapType.NonGeneric)]
    public class NjbDap : DapDevice
    {
        private static NJB.Discoverer discoverer = new NJB.Discoverer();
        private NJB.Device device;
        private uint ping_timer_id;
        
        public NjbDap(Hal.Device halDevice)
        {
            if(halDevice["portable_audio_player.type"] != "njb") {
                throw new CannotHandleDeviceException();
            }
            
            short usb_bus_number = (short)halDevice.GetPropertyInt("usb.bus_number");
            short usb_device_number = (short)halDevice.GetPropertyInt("usb.linux.device_number");

            foreach(NJB.Device tmp_device in discoverer) {
                try {
                    tmp_device.Open();
                } catch(Exception) {
                    continue;
                }
                
                if(tmp_device.UsbDeviceId == usb_device_number && tmp_device.UsbBusPath == usb_bus_number) {
                    device = tmp_device;
                    break;
                }
                
                tmp_device.Dispose();
            }

            if(device == null) {
                throw new CannotHandleDeviceException();
            }

            device.Capture();
            
            // ping the NJB device every 10 seconds
            ping_timer_id = GLib.Timeout.Add(10000, delegate {
                if(device == null) {
                    return false;
                }
                
                device.Ping();
                return true;
            });
        }
        
        public override void Dispose()
        {
            GLib.Source.Remove(ping_timer_id);
            device.Release();
            device.Dispose();
            device = null;
        }
        
        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "portable-media-";
            string id = "dell-pocket-dj";
            
            string path = ConfigureDefines.ICON_THEME_DIR 
                + String.Format("{0}x{0}", size)
                + Path.DirectorySeparatorChar
                + "extras" 
                + Path.DirectorySeparatorChar
                + "devices" + 
                + Path.DirectorySeparatorChar
                + prefix + id + ".png";
                
            try {
                return new Gdk.Pixbuf(path);
            } catch(Exception) {
                return base.GetIcon(size);
            }
        }
        
        public override string Name {
            get {
                return device.Name;
            }
            
            set {
            }
        }
        
        public override ulong StorageCapacity {
            get {
                return device.DiskTotal;
            }
        }
        
        public override ulong StorageUsed {
            get {
                return device.DiskTotal - device.DiskFree;
            }
        }
        
        public override bool IsReadOnly {
            get {
                return false;
            }
        }
        
        public override bool IsPlaybackSupported {
            get {
                return true;
            }
        }
    }
}
