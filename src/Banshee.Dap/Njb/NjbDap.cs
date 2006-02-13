
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
    [SupportedCodec(CodecType.Mp3)]
    [SupportedCodec(CodecType.Wma)]
    public sealed class NjbDap : DapDevice
    {
        private static NJB.Discoverer discoverer;
        private NJB.Device device;
        private NJB.DeviceId device_id;
        private uint ping_timer_id;
        
        static NjbDap()
        {
            try {
                discoverer = new NJB.Discoverer();
            } catch(Exception e) {
                Console.WriteLine(e);
                Console.WriteLine("Could not initialize NJB Discoverer: " + e.Message);
            }
        }
        
        public override InitializeResult Initialize(Hal.Device halDevice)
        {
            if(discoverer == null || 
                !halDevice.PropertyExists("usb.bus_number") ||
                !halDevice.PropertyExists("usb.linux.device_number") ||
                !halDevice.PropertyExists("usb.vendor_id") ||
                !halDevice.PropertyExists("usb.product_id")) {
                return InitializeResult.Invalid;
            }
            
            short usb_bus_number = (short)halDevice.GetPropertyInt("usb.bus_number");
            short usb_device_number = (short)halDevice.GetPropertyInt("usb.linux.device_number");
            
            device_id = NJB.DeviceId.GetDeviceId(
                (short)halDevice.GetPropertyInt("usb.vendor_id"),
                (short)halDevice.GetPropertyInt("usb.product_id"));
            
            if(device_id == null) {
                return InitializeResult.Invalid;
            }
            
            discoverer.Rescan();
            
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
                return InitializeResult.Invalid;
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
              
            base.Initialize(halDevice);
            
            InstallProperty("Model", device.Name);
            InstallProperty("Vendor", halDevice["usb.vendor"]);
            InstallProperty("Firmware Revision", device.FirmwareRevision.ToString());
            InstallProperty("Hardware Revision", device.HardwareRevision.ToString());
            InstallProperty("Serial Number", halDevice.PropertyExists("usb.serial") 
                ? halDevice["usb.serial"] : device.SdmiIdString);
            
            ReloadDatabase();
            
            CanCancelSave = false;
            return InitializeResult.Valid;
        }
        
        public override void Dispose()
        {
            GLib.Source.Remove(ping_timer_id);
            device.Release();
            device.Dispose();
            device = null;
            base.Dispose();
        }
        
        private void ReloadDatabase()
        {
            ClearTracks(false);

            foreach(NJB.Song song in device.GetSongs()) {
                NjbDapTrackInfo track = new NjbDapTrackInfo(song, this);
                AddTrack(track);            
            }
        }
        
        public override void Synchronize()
        {
        }
        
        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "multimedia-player-";
            string id = "dell-pocket-dj";
            Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(prefix + id, size);
            return icon == null? base.GetIcon(size) : icon;
        }
        
        public override string Name {
            get {
                return device_id.DisplayName;
            }
        }
        
        public override void SetOwner(string owner)
        {
            device.Owner = owner;
        }
        
        public override string Owner {
            get {
                return device.Owner;
            }
        }
        
        public override string GenericName {
            get {
                return device_id.DisplayName;
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
