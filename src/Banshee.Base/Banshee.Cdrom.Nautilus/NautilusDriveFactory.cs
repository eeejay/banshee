/***************************************************************************
 *  NautilusDriveFactory.cs
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
using System.Collections;
using System.Collections.Generic;

using Hal;

using Banshee.Base;
using Banshee.Cdrom;
using Banshee.Cdrom.Nautilus.Interop;

namespace Banshee.Cdrom.Nautilus
{
    public class NautilusDriveFactory : IDriveFactory
    {
        private Dictionary<string, IDrive> drive_table = new Dictionary<string, IDrive>(); 
        
        public event DriveHandler DriveAdded;
        public event DriveHandler DriveRemoved;
        public event MediaHandler MediaAdded;
        public event MediaHandler MediaRemoved;
        
        public NautilusDriveFactory()
        {
            foreach(string udi in HalCore.Manager.FindDeviceByStringMatch("storage.drive_type", "cdrom")) {
                AddDrive(new Device(udi));
            }
        
            HalCore.Manager.DeviceAdded += OnHalDeviceAdded;
            HalCore.Manager.DeviceRemoved += OnHalDeviceRemoved;
        }
        
        private void OnHalDeviceAdded(object o, DeviceArgs args)
        {
            if(args.Device["storage.drive_type"] == "cdrom") {
                NautilusDrive drive = AddDrive(args.Device);
                if(drive != null) {
                    OnDriveAdded(drive);
                }
            }
        }
        
        private void OnHalDeviceRemoved(object o, DeviceArgs args)
        {
            if(drive_table.ContainsKey(args.Udi)) {
                IDrive drive = drive_table[args.Udi];
                drive.MediaAdded -= OnMediaAdded;
                drive.MediaRemoved -= OnMediaRemoved;
                drive_table.Remove(args.Udi);
                OnDriveRemoved(drive);
            }
        }
        
        private NautilusDrive AddDrive(Device device)
        {
            if(drive_table.ContainsKey(device.Udi)) {
                return drive_table[device.Udi] as NautilusDrive;
            }
            
            BurnDrive nautilus_drive = FindDriveByDeviceNode(device["block.device"]);
            if(nautilus_drive == null) {
                return null;
            }
            
            NautilusDrive drive = device.GetPropertyBoolean("storage.cdrom.cdr") ?
                new NautilusRecorder(device, nautilus_drive) :
                new NautilusDrive(device, nautilus_drive);
            
            drive.MediaAdded += OnMediaAdded;
            drive.MediaRemoved += OnMediaRemoved;
            drive_table.Add(device.Udi, drive);
            return drive;
        }
        
        private BurnDrive FindDriveByDeviceNode(string deviceNode)
        {
            return new BurnDrive(deviceNode);
        }
        
        protected virtual void OnDriveAdded(IDrive drive)
        {
            DriveHandler handler = DriveAdded;
            if(handler != null) {
                handler(this, new DriveArgs(drive));
            }
        }
        
        protected virtual void OnDriveRemoved(IDrive drive)
        {
            DriveHandler handler = DriveRemoved;
            if(handler != null) {
                handler(this, new DriveArgs(drive));
            }
        }
        
        protected virtual void OnMediaAdded(object o, MediaArgs args)
        {
            MediaHandler handler = MediaAdded;
            if(handler != null) {
                handler(o, args);
            }
        }
        
        protected virtual void OnMediaRemoved(object o, MediaArgs args)
        {
            MediaHandler handler = MediaRemoved;
            if(handler != null) {
                handler(o, args);
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return drive_table.Values.GetEnumerator();
        }
        
        public IEnumerator<IDrive> GetEnumerator()
        {
            return drive_table.Values.GetEnumerator();
        }
        
        public int DriveCount {
            get { return drive_table.Count; }
        }
        
        public int RecorderCount {
            get {
                int count = 0;
                
                foreach(IDrive drive in drive_table.Values) {
                    if(drive is IRecorder) {
                        count++;
                    }
                }
                
                return count;
            }
        }
    }
}
