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
using System.IO;
using Hal;
using IPod;

using Banshee.Base;
using Banshee.Dap;
 
namespace Banshee.Dap.Ipod
{
    [DapProperties(DapType = DapType.NonGeneric)]
    public sealed class IpodDap : DapDevice
    {
        private IPod.Device device;
    
        public IpodDap(Hal.Device halDevice)
        {
            if(!halDevice.PropertyExists("block.device") || 
                !halDevice.GetPropertyBool("block.is_volume") ||
                halDevice.Parent["portable_audio_player.type"] != "ipod") {
                throw new CannotHandleDeviceException();
            } else if(!halDevice.GetPropertyBool("volume.is_mounted")) {
                throw new WaitForPropertyChangeException();
            }
            
            try {
                device = new IPod.Device(halDevice["block.device"]);
            } catch(Exception e) {
                throw new BrokenDeviceException(e.Message);
            }
            
            InstallProperty("Generation", device.Generation.ToString());
            InstallProperty("Model", device.Model.ToString());
            InstallProperty("Model Number", device.ModelNumber);
            InstallProperty("Serial Number", device.SerialNumber);
            InstallProperty("Firmware Version", device.FirmwareVersion);
            InstallProperty("Database Version", device.SongDatabase.Version.ToString());
            
            ReloadDatabase(false);
        }
        
        protected override void OnTrackAdded(TrackInfo track)
        {
        
        }
        
        protected override void OnTrackRemoved(TrackInfo track)
        {
        
        }
        
        private void ReloadDatabase(bool refresh)
        {
            ClearTracks(false);
            
            if(refresh) {
                device.SongDatabase.Reload();
            }
            
            foreach(Song song in device.SongDatabase.Songs) {
                IpodDapTrackInfo track = new IpodDapTrackInfo(song);
                AddTrack(track);            
            }
        }
        
        public override void Eject()
        {
            device.Eject();
        }
        
        public override void Save()
        {
            device.Save();
        }
        
        public override Gdk.Pixbuf GetIcon(int size)
        {
            string prefix = "portable-media-";
            string id = null;

            switch(device.Model) {
                case DeviceModel.Color: id = "ipod-standard-color"; break;
                case DeviceModel.ColorU2: id = "ipod-U2-color"; break;
                case DeviceModel.Regular: id = "ipod-standard-monochrome"; break;
                case DeviceModel.RegularU2: id = "ipod-U2-monochrome"; break;
                case DeviceModel.Mini: id = "ipod-mini-silver"; break;
                case DeviceModel.MiniBlue: id = "ipod-mini-blue"; break;
                case DeviceModel.MiniPink: id = "ipod-mini-pink"; break;
                case DeviceModel.MiniGreen: id = "ipod-mini-green"; break;
                case DeviceModel.MiniGold: id = "ipod-mini-gold"; break;
                case DeviceModel.Shuffle: id = "ipod-shuffle"; break;
                case DeviceModel.NanoWhite: id = "ipod-nano-white"; break;
                case DeviceModel.NanoBlack: id = "ipod-nano-black"; break;
                case DeviceModel.VideoWhite: id = "ipod-video-white"; break;
                case DeviceModel.VideoBlack: id = "ipod-video-black"; break;
                default:
                    id = "ipod-standard-monochrome";
                    break;
            }
            
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
                device.Name = value;
                InvokePropertiesChanged();
            }
        }
        
        public override ulong StorageCapacity {
            get {
                return device.VolumeSize;
            }
        }
        
        public override ulong StorageUsed {
            get {
                return device.VolumeUsed;
            }
        }
        
        public override bool IsReadOnly {
            get {
                return !device.CanWrite;
            }
        }
        
        public override bool IsPlaybackSupported {
            get {
                return true;
            }
        }
    }
}
