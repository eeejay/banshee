/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  IpodDap.cs
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
using Hal;
using IPod;
using Banshee.Dap;
 
namespace Banshee.Dap.Ipod
{
    [DapProperties(DapType = DapType.NonGeneric)]
    public class IpodDap : Dap
    {
        private Hal.Device hal_device;
        private IPod.Device ipod_device;
    
        public IpodDap(Hal.Device device)
        {
            if(!device.PropertyExists("block.device") || !device.GetPropertyBool("block.is_volume") || 
                device.Parent["portable_audio_player.type"] != "ipod") {
                throw new CannotHandleDeviceException();
            }
            
            hal_device = device;
            
            try {
                ipod_device = new IPod.Device(hal_device["block.device"]);
            } catch(Exception) {
                throw new CannotHandleDeviceException();
            }
            
            InstallProperty("Generation", ipod_device.Generation.ToString());
            InstallProperty("Model", ipod_device.Model.ToString());
            InstallProperty("Model Number", ipod_device.ModelNumber);
            InstallProperty("Serial Number", ipod_device.SerialNumber);
            InstallProperty("Firmware Version", ipod_device.FirmwareVersion);
            InstallProperty("Database Version", ipod_device.SongDatabase.Version.ToString());
        }
        
        public override string Name {
            get {
                return ipod_device.Name;
            }
            
            set {
                ipod_device.Name = value;
            }
        }
        
        public override ulong StorageCapacity {
            get {
                return ipod_device.VolumeSize;
            }
        }
        
        public override ulong StorageUsed {
            get {
                return ipod_device.VolumeUsed;
            }
        }
        
        public override bool IsReadOnly {
            get {
                return !ipod_device.CanWrite;
            }
        }
        
        public override bool IsPlaybackSupported {
            get {
                return true;
            }
        }
    }
}
