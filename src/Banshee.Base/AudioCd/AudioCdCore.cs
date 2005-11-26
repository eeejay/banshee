/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AudioCdCore.cs
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
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;
using MusicBrainz;
using Hal;

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
        
        private Hashtable disks = new Hashtable();
        
        public event EventHandler Updated;
        public event AudioCdCoreDiskRemovedHandler DiskRemoved;
        public event AudioCdCoreDiskAddedHandler DiskAdded;
        
        public AudioCdCore()
        {
            HalCore.DeviceAdded += OnDeviceAdded;
            HalCore.DeviceRemoved += OnDeviceRemoved;
            
            LogCore.Instance.PushDebug("Audio CD Core Initialized", "");
            
            BuildInitialList();
        }
        
        private AudioCdDisk CreateDisk(DiskInfo halDisk)
        {
            try {
                Console.WriteLine("SDKFJDSKF");
                AudioCdDisk disk = new AudioCdDisk(halDisk.Udi, halDisk.DeviceNode, halDisk.VolumeName);
                disk.Updated += OnAudioCdDiskUpdated;
                if(disk.Valid) {
                    disks[disk.Udi] = disk;
                }
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
            Device [] volumes = Device.FindByStringMatch(HalCore.Context, "info.parent", device.Udi);
            
            if(volumes == null || volumes.Length < 1) {
                return null;
            }
            
            Device volume = volumes[0];
            
            if(!volume.GetPropertyBool("volume.disc.has_audio") || 
                !volume.PropertyExists("block.device") || 
                !volume.PropertyExists("info.product")) {
                return null;
            }
            
            return new DiskInfo(volume.Udi, volume["block.device"], volume["info.product"]);
        }
        
        private DiskInfo [] GetHalDisks()
        {
            ArrayList list = new ArrayList();
        
            foreach(Device device in Device.FindByStringMatch(HalCore.Context, "storage.drive_type", "cdrom")) {
                DiskInfo disk = CreateHalDisk(device);
                if(disk != null) {
                    list.Add(disk);
                    Console.WriteLine(disk.Udi);
                    Console.WriteLine(disk.DeviceNode);
                    Console.WriteLine(disk.VolumeName);
                }
            }
            
            return list.ToArray(typeof(DiskInfo)) as DiskInfo [];
        }
        
        private void BuildInitialList()
        {
            DiskInfo [] halDisks = GetHalDisks();

            if(halDisks == null || halDisks.Length == 0) {
                return;
            }

            foreach(DiskInfo halDisk in halDisks) {
                CreateDisk(halDisk);
            }

            HandleUpdated();
        }
        
        private void OnDeviceAdded(object o, DeviceAddedArgs args)
        {
            string udi = args.Device.Udi;

            if(udi == null || disks[udi] != null) {
                return;
            }

            DiskInfo [] halDisks = GetHalDisks();

            if(halDisks == null || halDisks.Length == 0) {
                return;
            }

            foreach(DiskInfo halDisk in halDisks) {
                if(halDisk.Udi != udi) {
                    continue;
                }
                
                AudioCdDisk disk = CreateDisk(halDisk);
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
        
        private void OnDeviceRemoved(object o, DeviceRemovedArgs args)
        {
            string udi = args.Device.Udi;
            
            if(udi == null || disks[udi] == null) {
                 return;
            }
                 
            disks.Remove(udi);
            HandleUpdated();
            
            AudioCdCoreDiskRemovedHandler handler = DiskRemoved;
            if(handler != null) {
                AudioCdCoreDiskRemovedArgs diskargs = new AudioCdCoreDiskRemovedArgs();
                diskargs.Udi = udi;
                handler(this, diskargs);
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
        
        public AudioCdDisk [] Disks
        {
            get {
                ArrayList list = new ArrayList(disks.Values);
                return list.ToArray(typeof(AudioCdDisk)) as AudioCdDisk [];
            }
        }
    }
}
