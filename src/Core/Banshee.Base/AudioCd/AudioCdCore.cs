/***************************************************************************
 *  AudioCdCore.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover (aaron@abock.org)
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
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using MusicBrainz;
using Hal;

using Banshee.Sources;

namespace Banshee.Base
{
    public delegate void AudioCdCoreDiskRemovedHandler(object o, AudioCdCoreDiskRemovedArgs args);
    public delegate void AudioCdCoreDiskAddedHandler(object o, AudioCdCoreDiskAddedArgs args);
    
    public class AudioCdCoreDiskRemovedArgs : EventArgs
    {
        public string Udi;
    }
    
    public class AudioCdCoreDiskAddedArgs : EventArgs
    {
        public AudioCdDisk Disk;
    }
    
    public class AudioCdCore
    {
        private class DiskInfo
        {
            public string Udi;
            public string DeviceNode;
            public string VolumeName;
            
            public DiskInfo(string udi, string deviceNode, string volumeName)
            {
                Udi = udi;
                DeviceNode = deviceNode;
                VolumeName = volumeName;
            }
        }
        
        private Dictionary<string, AudioCdDisk> disks = new Dictionary<string, AudioCdDisk>();
        
        public event EventHandler Updated;
        public event AudioCdCoreDiskRemovedHandler DiskRemoved;
        public event AudioCdCoreDiskAddedHandler DiskAdded;
        
        public AudioCdCore()
        {
            if(!HalCore.Initialized) {
                throw new ApplicationException(Catalog.GetString("HAL is not initialized"));
            }
            
            HalCore.Manager.DeviceAdded += OnDeviceAdded;
            HalCore.Manager.DeviceRemoved += OnDeviceRemoved;
            
            LogCore.Instance.PushDebug(Catalog.GetString("Audio CD Core Initialized"), "");
            
            BuildInitialList();
        }
        
        private AudioCdDisk CreateDisk(DiskInfo hal_disk)
        {
            try {
                AudioCdDisk disk = new AudioCdDisk(hal_disk.Udi, hal_disk.DeviceNode, hal_disk.VolumeName);
                disk.Updated += OnAudioCdDiskUpdated;
                if(disk.Valid && !disks.ContainsKey(disk.Udi)) {
                    disks.Add(disk.Udi, disk);
                }
                SourceManager.AddSource(new AudioCdSource(disk));
                return disk;
            } catch(Exception e) {
                Exception temp_e = e; // work around mcs #76642
                LogCore.Instance.PushError(Catalog.GetString("Could not Read Audio CD"), 
                    temp_e.Message);
            }
            
            return null;
        }
        
        private DiskInfo CreateHalDisk(Device device)
        {
            string [] volumes = HalCore.Manager.FindDeviceByStringMatch("info.parent", device.Udi);
            
            if(volumes == null || volumes.Length < 1) {
                return null;
            }
            
            Device volume = new Device(volumes[0]);
            
            if(!volume.GetPropertyBoolean("volume.disc.has_audio")) {
                return null;
            }
            
            return new DiskInfo(volume.Udi, volume["block.device"] as string, 
                volume["info.product"] as string);
        }
        
        private IList<DiskInfo> GetHalDisks()
        {
            List<DiskInfo> list = new List<DiskInfo>();
        
            foreach(string udi in HalCore.Manager.FindDeviceByStringMatch("storage.drive_type", "cdrom")) {
                try {
                    DiskInfo disk = CreateHalDisk(new Device(udi));
                    if(disk != null) {
                        list.Add(disk);
                    }
                } catch {
                }
            }
            
            return list;
        }
        
        private void BuildInitialList()
        {
            foreach(DiskInfo hal_disk in GetHalDisks()) {
                CreateDisk(hal_disk);
            }

            HandleUpdated();
        }
        
        private void OnDeviceAdded(object o, DeviceArgs args)
        {
            string udi = args.Udi;

            if(udi == null || disks.ContainsKey(udi)) {
                return;
            }

            foreach(DiskInfo hal_disk in GetHalDisks()) {
                if(hal_disk.Udi != udi) {
                    continue;
                }
                
                AudioCdDisk disk = CreateDisk(hal_disk);
                if(disk == null) {
                    continue;
                }
                
                HandleUpdated();
                
                AudioCdCoreDiskAddedHandler handler = DiskAdded;
                if(handler != null) {
                    AudioCdCoreDiskAddedArgs diskargs = new AudioCdCoreDiskAddedArgs();
                    diskargs.Disk = disk;
                    handler(this, diskargs);
                }
                
                break;
            }   
        }
        
        private void OnDeviceRemoved(object o, DeviceArgs args)
        {
            string udi = args.Udi;
            
            if(udi == null) {
                 return;
            }
                 
            if(disks.ContainsKey(udi)) {
                disks.Remove(udi);
            }
            
            HandleUpdated();
            
            AudioCdCoreDiskRemovedHandler handler = DiskRemoved;
            if(handler != null) {
                AudioCdCoreDiskRemovedArgs diskargs = new AudioCdCoreDiskRemovedArgs();
                diskargs.Udi = udi;
                handler(this, diskargs);
            }
            
            foreach(Source source in SourceManager.Sources) {
                if(source is AudioCdSource && (source as AudioCdSource).Disk.Udi == udi) {
                    SourceManager.RemoveSource(source);
                    break;
                }
            }       
        }
        
        private void OnAudioCdDiskUpdated(object o, EventArgs args)
        {
            HandleUpdated();
        }
        
        private void HandleUpdated()
        {
            EventHandler handler = Updated;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public ICollection<AudioCdDisk> Disks {
            get { return disks.Values; }
        }
    }
}
