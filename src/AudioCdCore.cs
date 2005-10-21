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

namespace Banshee
{
    internal delegate void CdDetectUdiCallback(IntPtr udiPtr);

    [StructLayout(LayoutKind.Sequential)]
    public struct DiskInfoRaw
    {
        public IntPtr Udi;
        public IntPtr DeviceNode;
        public IntPtr DriveName;
    }

    public class DiskInfo
    {
        public string Udi;
        public string DeviceNode;
        public string DriveName;
        
        public DiskInfo(DiskInfoRaw raw)
        {
            Udi = Marshal.PtrToStringAnsi(raw.Udi);
            DeviceNode = Marshal.PtrToStringAnsi(raw.DeviceNode);
            DriveName = Marshal.PtrToStringAnsi(raw.DriveName);
        }
    }
        
    public class AudioCdTrackInfo : TrackInfo
    {
        private int track_index;
        private int minutes;
        private int seconds;
        private string device;
        
        public AudioCdTrackInfo(string device, string udi)
        {
            PreviousTrack = Gtk.TreeIter.Zero;
            canSaveToDatabase = false;
            this.device = device;
        }
        
        public override void Save()
        {
            
        }
        
        public override void IncrementPlayCount()
        {
        
        }
        
        protected override void SaveRating()
        {
        
        }
        
        public int TrackIndex 
        { 
            get { 
                return track_index;
            } 
            
            set { 
                track_index =  value; 
                uri = new Uri("cdda://" + (track_index + 1) + "#" + device); 
            } 
        }
        
        public int Minutes
        { 
            get { return minutes; } 
            set { minutes = value; }
        }
        
        public int Seconds
        { 
            get { return seconds; } 
            set { seconds = value; }
        }
        
        public string Device
        { 
            get { return device; } 
        }
    }
    
    public class AudioCdDisk
    {
        private string udi;
        private string deviceNode;
        private string driveName;
        private string diskId;
        private string title;    
    
        private int trackCount;
        private int fullTrackCount;
        private long totalSeconds;
        private string offsets;
        
        private bool valid;
        
        private AudioCdTrackInfo [] tracks;
        
        public event EventHandler Updated;

        public AudioCdDisk(DiskInfo disk)
        {
            this.udi = disk.Udi;
            this.deviceNode = disk.DeviceNode;
            this.driveName = disk.DriveName;
            
            valid = LoadDiskInfo();
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_info_get_extended_disc_id(
            string device);
        
        private bool LoadDiskInfo()
        {
            string disc_id = GLib.Marshaller.Utf8PtrToString(
                cd_info_get_extended_disc_id(deviceNode));
              
            string [] parts = disc_id.Split(' ');
            string discid = String.Empty, offsets = String.Empty;
            ArrayList track_list = new ArrayList();
            
            fullTrackCount = 0;
            
            for(int i = 0; i < parts.Length; i++) {
                if(i == 0) {
                    discid = parts[0].Trim();
                    continue;
                }
                
                string part = parts[i].Trim();
                if(part == String.Empty) {
                    continue;
                }
                
                int offset = Convert.ToInt32(part);
                
                if(offset < 150) {
                    continue;
                }
                
                offsets += String.Format("{0} ", offset);
                
                AudioCdTrackInfo track = new AudioCdTrackInfo(deviceNode, udi);
                
                track.TrackIndex = i - 1;
                bool is_valid = true;
                
                try {
                    int offset_next = Convert.ToInt32(parts[i + 1]);
                    int diff = offset_next - offset;
                    track.Duration = diff / 75;
                } catch(Exception) {
                    is_valid = false;
                }
                
                Console.WriteLine(i + ": " + track.Duration);
                
                track.TrackNumber = (uint)i;

                totalSeconds = offset / 75;
                                
                track.Artist = Catalog.GetString("Unknown Artist");
                track.Album = Catalog.GetString("Unknown Album");
                track.Title = 
                    String.Format(Catalog.GetString("Track {0}"), i);
                    
                if(is_valid) {
                    track_list.Add(track);
                }
                
                fullTrackCount++;
            }
            
            fullTrackCount++;
            
            title = Catalog.GetString("Audio CD");
            this.offsets = offsets.Trim();
            diskId = discid;
            
            tracks = track_list.ToArray(typeof(AudioCdTrackInfo)) 
                as AudioCdTrackInfo [];
            trackCount = tracks.Length;
            
            return true;
        }
        
        public void LoadFromCddbDiscInfo(CddbSlaveClientDiscInfo disc)
        {
            string discTitle = null, artist = null;
            
            if(disc.DiscTitle != null && disc.DiscTitle != String.Empty 
                && disc.DiscTitle != "Unknown") {
                discTitle = disc.DiscTitle;
                title = discTitle;
            }
                    
            for(int i = 0; i < trackCount; i++) {
                string trackTitle = disc.Tracks[i].Name;

                if(disc.Artist != null && disc.Artist != String.Empty 
                    && disc.Artist != "Unknown")
                    artist = disc.Artist; 

                if(trackTitle == null || trackTitle == String.Empty 
                    || trackTitle == "Unknown")
                    trackTitle = null;
                
                if(artist != null && artist.ToLower() == "various" 
                    && trackTitle != null) {

                    string [] parts = 
                        System.Text.RegularExpressions.Regex.Split(trackTitle,
                             @" / ");
                    
                    if(parts != null && parts.Length >= 2) {
                        artist = parts[0].Trim();
                        trackTitle = parts[1].Trim();
                    }
                }
            
                if(artist != null)
                    tracks[i].Artist = artist;
                
                if(discTitle != null)
                    tracks[i].Album = discTitle;
                
                if(disc.Genre != null && disc.Genre != String.Empty 
                    && disc.Genre != "Unknown")
                    tracks[i].Genre = disc.Genre;
                
                if(discTitle != null)
                    tracks[i].Album = discTitle;
                    
                if(title != null)    
                    tracks[i].Title = trackTitle;
                
                if(tracks[i].Duration <= 0 && disc.Tracks[i].Length > 0)
                    tracks[i].Duration = disc.Tracks[i].Length;
            }
            
            EventHandler handler = Updated;
            if(handler != null)
                handler(this, new EventArgs());
        }

        [DllImport("libc")]
        private static extern int ioctl(int device, EjectOperation request); 

        private enum EjectOperation {
            Open = 0x5309,
            Close = 0x5319
        }

        public bool Eject()
        {
            return Eject(true);
        }

        public bool Eject(bool open)
        {
            try {
                using(UnixStream stream = UnixFile.Open(deviceNode, 
                    Mono.Unix.Native.OpenFlags.O_RDONLY | 
					Mono.Unix.Native.OpenFlags.O_NONBLOCK)) {
                    return ioctl(stream.Handle, open
                        ? EjectOperation.Open
                        : EjectOperation.Close) == 0;
                }
            } catch {
                return false;
            }
        }
        
        public string Udi        { get { return udi;        } }    
        public string DeviceNode { get { return deviceNode; } }
        public string DriveName  { get { return driveName;  } }
        public string DiskId     { get { return diskId;     } }
        public string Title      { get { return title;      } }
        public int TrackCount    { get { return trackCount; } }
        public int FullTrackCount    { get { return fullTrackCount; } }
        public bool Valid        { get { return valid;      } }
        public int    TotalSeconds { get { return (int)totalSeconds; } }
        public string Offsets { get { return offsets; } }
        public AudioCdTrackInfo [] Tracks { get { return tracks; } }
    }
    
    public delegate void AudioCdCoreDiskRemovedHandler(object o,
       AudioCdCoreDiskRemovedArgs args);
    
    public class AudioCdCoreDiskRemovedArgs : EventArgs
    {
       public string Udi;
    }
    
    public delegate void AudioCdCoreDiskAddedHandler(object o,
       AudioCdCoreDiskAddedArgs args);
    
    public class AudioCdCoreDiskAddedArgs : EventArgs
    {
       public AudioCdDisk Disk;
    }
    
    public class AudioCdCore : IDisposable
    {
        private static AudioCdCore instance;
        private Hashtable disks = new Hashtable();
        private HandleRef handle;
        
        private CddbSlaveClient cddbClient;
        
        private CdDetectUdiCallback AddedCallback;
        private CdDetectUdiCallback RemovedCallback;
        
        public event EventHandler Updated;
        public event AudioCdCoreDiskRemovedHandler DiskRemoved;
        public event AudioCdCoreDiskAddedHandler DiskAdded;
        
        public AudioCdCore()
        {
            IntPtr ptr = cd_detect_new();
            if(ptr == IntPtr.Zero)
                throw new ApplicationException(
                    Catalog.GetString("Could not initialize HAL for CD Detection"));
            
            handle = new HandleRef(this, ptr);
                
            AddedCallback = new CdDetectUdiCallback(OnDiskAdded);
            RemovedCallback = new CdDetectUdiCallback(OnDeviceRemoved);
                
            cd_detect_set_device_added_callback(handle, AddedCallback);
            cd_detect_set_device_removed_callback(handle, RemovedCallback);    
            
            cddbClient = new CddbSlaveClient();
            cddbClient.EventNotify += OnCddbSlaveClientEventNotify;

            DebugLog.Add("Audio CD Core Initialized");
            
            BuildInitialList();
        }
        
        public void Dispose()
        {
            cddbClient.Dispose();
            cd_detect_free(handle);
        }
    
        private void OnDiskAdded(IntPtr udiPtr)
        {
            string udi = Marshal.PtrToStringAnsi(udiPtr);

            if(udi == null || disks[udi] != null)
                return;

            DiskInfo [] halDisks = GetHalDisks();

            if(halDisks == null || halDisks.Length == 0)
                return;

            foreach(DiskInfo halDisk in halDisks) {
                if(halDisk.Udi != udi)
                    continue;
                
                AudioCdDisk disk = new AudioCdDisk(halDisk);
                disk.Updated += OnAudioCdDiskUpdated;

                if(disk.Valid)
                    disks[disk.Udi] = disk;
                
                QueryCddb(disk);
                HandleUpdated();
                
                AudioCdCoreDiskAddedHandler handler = DiskAdded;
                if(handler != null) {
                    AudioCdCoreDiskAddedArgs args = new AudioCdCoreDiskAddedArgs();
                    args.Disk = disk;
                    handler(this, args);
                }
                
                break;
            }
        }
        
        private void OnDeviceRemoved(IntPtr udiPtr)
        {
            string udi = Marshal.PtrToStringAnsi(udiPtr);
            
            if(udi == null || disks[udi] == null)
                 return;
                 
            disks.Remove(udi);
            HandleUpdated();
            
           AudioCdCoreDiskRemovedHandler handler = DiskRemoved;
           if(handler != null) {
               AudioCdCoreDiskRemovedArgs args = new AudioCdCoreDiskRemovedArgs();
               args.Udi = udi;
                
               handler(this, args);
           }
        }
           
        private void QueryCddb(AudioCdDisk disk)
        {   
            cddbClient.Query(disk.DiskId, disk.FullTrackCount, disk.Offsets,
                disk.TotalSeconds, ConfigureDefines.PACKAGE, 
                ConfigureDefines.VERSION);
        }
        
        private void OnCddbSlaveClientEventNotify(object o, 
            CddbSlaveClientEventNotifyArgs args)
        {
            if(args.DiscInfo == null)
                return;
                
            CddbSlaveClientDiscInfo cddbDisc = args.DiscInfo;

            foreach(AudioCdDisk disk in Disks) {
                if(disk.DiskId.ToLower() == cddbDisc.DiscId.ToLower() && 
                    disk.TrackCount == cddbDisc.TrackCount) {
                    disk.LoadFromCddbDiscInfo(cddbDisc);
                }
            }
        }
        
        private DiskInfo [] GetHalDisks()
        {
            IntPtr arrayPtr = cd_detect_get_disk_array(handle);
            int arraySize = 0;
            
            ArrayList disks = new ArrayList();

            if(arrayPtr == IntPtr.Zero)
                return null;
            
            while(Marshal.ReadIntPtr(arrayPtr, arraySize * IntPtr.Size)
                != IntPtr.Zero)
                arraySize++;
            
            for(int i = 0; i < arraySize; i++) {
                IntPtr rawPtr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
                DiskInfoRaw diskRaw = (DiskInfoRaw)Marshal.PtrToStructure(
                    rawPtr, typeof(DiskInfoRaw));
                
                disks.Add(new DiskInfo(diskRaw));
            }
            
            cd_detect_disk_array_free(arrayPtr);
            
            return disks.ToArray(typeof(DiskInfo)) as DiskInfo [];
        }
        
        private void BuildInitialList()
        {
            DiskInfo [] halDisks = GetHalDisks();

            if(halDisks == null || halDisks.Length == 0)
                return;

            foreach(DiskInfo halDisk in halDisks) {
                AudioCdDisk disk = new AudioCdDisk(halDisk);
                disk.Updated += OnAudioCdDiskUpdated;
                
                if(disk.Valid) 
                    disks[disk.Udi] = disk;
                
                QueryCddb(disk);
            }

            HandleUpdated();
        }
        
        private void OnAudioCdDiskUpdated(object o, EventArgs args)
        {
            HandleUpdated();
        }
        
        private void HandleUpdated()
        {
            EventHandler handler = Updated;
            if(handler != null)
                handler(this, new EventArgs());
        }
        
        public AudioCdDisk [] Disks
        {
            get {
                ArrayList list = new ArrayList(disks.Values);
                return list.ToArray(typeof(AudioCdDisk)) as AudioCdDisk [];
            }
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_detect_new();
        
        [DllImport("libbanshee")]
        private static extern void cd_detect_free(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern IntPtr cd_detect_get_disk_array(HandleRef handle);
        
        [DllImport("libbanshee")]
        private static extern void cd_detect_disk_array_free(IntPtr list);
        
        [DllImport("libbanshee")]
        private static extern bool cd_detect_set_device_added_callback(
            HandleRef handle, CdDetectUdiCallback cb);
        
        [DllImport("libbanshee")]
        private static extern bool cd_detect_set_device_removed_callback(
            HandleRef handle, CdDetectUdiCallback cb);
    }
}
