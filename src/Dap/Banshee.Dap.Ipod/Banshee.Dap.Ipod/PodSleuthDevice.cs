//
// PodSleuthDevice.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using IPod;
using Banshee.Hardware;

namespace Banshee.Dap.Ipod
{
    public class PodSleuthDevice : IPod.Device, IVolume
    {
        internal const string PodsleuthPrefix = "org.podsleuth.ipod.";

        private class _ProductionInfo : IPod.ProductionInfo
        {
            public _ProductionInfo (IVolume volume)
            {
                SerialNumber = volume.GetPropertyString (PodsleuthPrefix + "serial_number");
                FactoryId = volume.GetPropertyString (PodsleuthPrefix + "production.factory_id");
                Number = volume.GetPropertyInteger (PodsleuthPrefix + "production.number");
                Week = volume.GetPropertyInteger (PodsleuthPrefix + "production.week");
                Year = volume.GetPropertyInteger (PodsleuthPrefix + "production.year");
            }
        }
        
        private class _VolumeInfo : IPod.VolumeInfo
        {
            private IVolume volume;
            
            public _VolumeInfo (IVolume volume)
            {
                this.volume = volume;
                
                MountPoint = volume.GetPropertyString ("volume.mount_point");
                Label = volume.GetPropertyString ("volume.label");
                IsMountedReadOnly = volume.GetPropertyBoolean ("volume.is_mounted_read_only");
                Uuid = volume.GetPropertyString ("volume.uuid");
            }
            
            public override ulong Size {
                get { return volume.Capacity; }
            }
        
            public override ulong SpaceUsed {
                get { return volume.Capacity - (ulong)volume.Available; }
            }
        }
        
        private class _ModelInfo : IPod.ModelInfo
        {
            public _ModelInfo (IVolume volume)
            {
                AdvertisedCapacity = GetVolumeSizeString (volume);
                
                IsUnknown = true;
                if (volume.PropertyExists (PodsleuthPrefix + "is_unknown")) {
                    IsUnknown = volume.GetPropertyBoolean (PodsleuthPrefix + "is_unknown");
                }
                
                if (volume.PropertyExists (PodsleuthPrefix + "images.album_art_supported")) {
                    AlbumArtSupported = volume.GetPropertyBoolean (PodsleuthPrefix + "images.album_art_supported");
                }
                
                if (volume.PropertyExists (PodsleuthPrefix + "images.photos_supported")) {
                    PhotosSupported = volume.GetPropertyBoolean (PodsleuthPrefix + "images.photos_supported");
                }
                
                if (volume.PropertyExists (PodsleuthPrefix + "model.device_class")) {
                    DeviceClass = volume.GetPropertyString (PodsleuthPrefix + "model.device_class");
                }
                
                if (volume.PropertyExists (PodsleuthPrefix + "model.generation")) {
                    Generation = volume.GetPropertyDouble (PodsleuthPrefix + "model.generation");
                }
                
                if (volume.PropertyExists (PodsleuthPrefix + "model.shell_color")) {
                    ShellColor = volume.GetPropertyString (PodsleuthPrefix + "model.shell_color");
                }
                
                if (volume.PropertyExists ("info.icon_name")) {
                    IconName = volume.GetPropertyString ("info.icon_name");
                }
                
                if (volume.PropertyExists (PodsleuthPrefix + "capabilities")) {
                    foreach (string capability in volume.GetPropertyStringList (PodsleuthPrefix + "capabilities")) {
                        AddCapability (capability);
                    }
                }
            }
            
            private static string GetVolumeSizeString (IVolume volume)
            {
                string format = "GiB";
                double value = volume.GetPropertyUInt64 ("volume.size") / 1000.0 / 1000.0 / 1000.0;

                if(value < 1.0) {
                    format = "MiB";
                    value *= 1000.0;
                }

                return String.Format ("{0} {1}", (int)Math.Round (value), format);
            }
        }
        
        private IVolume volume;
        
        private IPod.ProductionInfo production_info;
        private IPod.VolumeInfo volume_info;
        private IPod.ModelInfo model_info;
        
        public override IPod.ProductionInfo ProductionInfo {
            get { return production_info; }
        }
        
        public override IPod.VolumeInfo VolumeInfo {
            get { return volume_info; }
        }
        
        public override IPod.ModelInfo ModelInfo {
            get { return model_info; }
        }

        internal PodSleuthDevice (IVolume volume) 
        {
            this.volume = volume;

            volume_info = new _VolumeInfo (volume);
            production_info = new _ProductionInfo (volume);
            model_info = new _ModelInfo (volume);
            
            if (volume.PropertyExists (PodsleuthPrefix + "control_path")) {
                string relative_control = volume.GetPropertyString (PodsleuthPrefix + "control_path");
                if (relative_control[0] == Path.DirectorySeparatorChar) {
                    relative_control = relative_control.Substring (1);
                }
                
                ControlPath = Path.Combine(VolumeInfo.MountPoint, relative_control);
            }
            
            ArtworkFormats = new ReadOnlyCollection<ArtworkFormat> (LoadArtworkFormats ());

            if (volume.PropertyExists (PodsleuthPrefix + "firmware_version")) {
                FirmwareVersion = volume.GetPropertyString (PodsleuthPrefix + "firmware_version");
            }

            if (volume.PropertyExists (PodsleuthPrefix + "firewire_id")) {
                FirewireId = volume.GetPropertyString (PodsleuthPrefix + "firewire_id");
            }
            
            RescanDisk ();
        }
        
        public override void Eject ()
        {
            volume.Eject ();
        }
        
        public override void RescanDisk () 
        {
        }

        private List<ArtworkFormat> LoadArtworkFormats () 
        {
            List<ArtworkFormat> formats = new List<ArtworkFormat> ();

            if (!ModelInfo.AlbumArtSupported) {
                return formats;
            }
            
            string [] formatList = volume.GetPropertyStringList (PodsleuthPrefix + "images.formats");

            foreach (string formatStr in formatList) {
                short correlationId, width, height, rotation;
                ArtworkUsage usage;
                int size;
                PixelFormat pformat;

                correlationId = width = height = rotation = size = 0;
                usage = ArtworkUsage.Unknown;
                pformat = PixelFormat.Unknown;

                string[] pairs = formatStr.Split(',');
                
                foreach (string pair in pairs) {
                    string[] splitPair = pair.Split('=');
                    if (splitPair.Length != 2) {
                        continue;
                    }
                    
                    string value = splitPair[1];
                    switch (splitPair[0]) {
                        case "corr_id": correlationId = Int16.Parse (value); break;
                        case "width": width = Int16.Parse (value); break;
                        case "height": height = Int16.Parse (value); break;
                        case "rotation": rotation = Int16.Parse (value); break;
                        case "pixel_format":
                            switch (value) {
                                case "iyuv": pformat = PixelFormat.IYUV;  break;
                                case "rgb565": pformat = PixelFormat.Rgb565;  break;
                                case "rgb565be": pformat = PixelFormat.Rgb565BE; break;
                                case "unknown": pformat = PixelFormat.Unknown; break;
                            }
                            break;
                        case "image_type":
                            switch (value) {
                                case "photo": usage = ArtworkUsage.Photo; break;
                                case "album": usage = ArtworkUsage.Cover; break;
                                /* we don't support this right now
                                   case "chapter": usage = ArtworkUsage.Chapter; break;
                                */
                            }
                            break;
                    }
                }

                if (pformat != PixelFormat.Unknown && usage != ArtworkUsage.Unknown) {
                    formats.Add (new ArtworkFormat (usage, width, height, correlationId, size, pformat, rotation));
                }
            }

            return formats;
        }

#region IVolume Wrapper

        string IDevice.Name {
            get { return volume.Name; }
        }

        public void Unmount ()
        {
            volume.Unmount ();
        }
        
        public string Uuid {
            get { return volume.Uuid; }
        }
        public string Product {
            get { return volume.Product; }
        }

        public string Vendor {
            get { return volume.Vendor; }
        }

        public IDeviceMediaCapabilities MediaCapabilities {
            get { return volume.MediaCapabilities; }
        }

        public string DeviceNode {
            get { return volume.DeviceNode; }
        }

        public string MountPoint {
            get { return volume.MountPoint; }
        }

        public bool IsReadOnly {
            get { return volume.IsReadOnly; }
        }

        public ulong Capacity {
            get { return volume.Capacity; }
        }

        public long Available {
            get { return volume.Available; }
        }

        public IBlockDevice Parent {
            get { return volume.Parent; }
        }

        public bool ShouldIgnore {
            get { return volume.ShouldIgnore; }
        }

        public string FileSystem {
            get { return volume.FileSystem; }
        }

        public bool CanEject {
            get { return volume.CanEject; }
        }

        public bool CanUnmount {
            get { return volume.CanEject; }
        }

        public bool PropertyExists (string key)
        {
            return volume.PropertyExists (key);
        }

        public string GetPropertyString (string key)
        {
            return volume.GetPropertyString (key);
        }

        public double GetPropertyDouble (string key)
        {
            return volume.GetPropertyDouble (key);
        }

        public bool GetPropertyBoolean (string key)
        {
            return volume.GetPropertyBoolean (key);
        }

        public int GetPropertyInteger (string key)
        {
            return volume.GetPropertyInteger (key);
        }

        public ulong GetPropertyUInt64 (string key)
        {
            return volume.GetPropertyUInt64 (key);
        }

        public string[] GetPropertyStringList (string key)
        {
            return volume.GetPropertyStringList (key);
        }
        
#endregion

    }
}
