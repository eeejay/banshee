/***************************************************************************
 *  NautilusDrive.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

using Hal;

using Banshee.Base;
using Banshee.Cdrom;
using Banshee.Cdrom.Nautilus.Interop;

namespace Banshee.Cdrom.Nautilus
{
    public class NautilusDrive : IDrive
    {
        private BurnDrive drive;
        private Device hal_drive_device;
        private Device hal_disc_device;
        
        public event MediaHandler MediaAdded;
        public event MediaHandler MediaRemoved;
        
        private NautilusDrive()
        {
        }
        
        internal NautilusDrive(Device device, BurnDrive drive)
        {
            this.hal_drive_device = device;
            this.drive = drive;
            
            foreach(string disc_udi in HalCore.Manager.FindDeviceByStringMatch("info.parent", device.Udi)) {
                if(CheckMedia(new Device(disc_udi))) {
                    break;
                }
            }
            
            HalCore.Manager.DeviceAdded += OnHalDeviceAdded;
            HalCore.Manager.DeviceRemoved += OnHalDeviceRemoved;
        }
        
        private void OnHalDeviceAdded(object o, DeviceAddedArgs args)
        {
            CheckMedia(args.Device);
        }
        
        private void OnHalDeviceRemoved(object o, DeviceRemovedArgs args)
        {
            if(hal_disc_device != null && hal_disc_device.Udi == args.Udi) {
                hal_disc_device = null;
                OnMediaRemoved();
            }
        }
        
        private bool CheckMedia(Device discDevice)
        {
            if(discDevice == null) {
                return false;
            }
        
            Device parent = discDevice.Parent;
            
            if(parent == null) {
                return false;
            }
            
            if(parent.Udi == hal_drive_device.Udi &&
                discDevice.GetPropertyBoolean("volume.is_disc") &&
                discDevice.GetPropertyBoolean("volume.disc.is_blank")) {
                hal_disc_device = discDevice;
                OnMediaAdded();
                return true;
            }
            
            return false;
        }
                
        protected virtual void OnMediaAdded()
        {
            MediaHandler handler = MediaAdded;
            if(handler != null) {
                handler(this, new MediaArgs(this, true));
            }
        }
        
        protected virtual void OnMediaRemoved()
        {
            MediaHandler handler = MediaRemoved;
            if(handler != null) {
                handler(this, new MediaArgs(this, false));
            }
        }
        
        public bool HaveMedia {
            get { return hal_disc_device != null; }
        }
        
        public string Name {
            get { return drive.DisplayName; }
        }
        
        public string Device {
            get { return drive.Device; }
        }
        
        public int MaxWriteSpeed {
            get { return drive.MaxWriteSpeed; }
        }
        
        public int MaxReadSpeed {
            get { return drive.MaxReadSpeed; }
        }
        
        public int MinWriteSpeed {
            get { return 2; }
        }
        
        public long MediaCapacity {
            get { return HaveMedia ? drive.MediaCapacity : 0; }
        }
        
        internal BurnDrive Drive {
            get { return drive; }
        }
        
        internal Device HalDriveDevice {
            get { return hal_drive_device; }
        }
        
        internal Device HalDiscDevice {
            get { return hal_disc_device; }
        }
    }
}
