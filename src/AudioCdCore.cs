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
        private string device;
        private bool do_rip;
        
        public AudioCdTrackInfo(string device)
        {
            PreviousTrack = Gtk.TreeIter.Zero;
            canSaveToDatabase = false;
            this.device = device;
            do_rip = true;
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
        
        public int TrackIndex { 
            get { 
                return track_index;
            } 
            
            set { 
                track_index = value;
                TrackNumber = (uint)value;
                uri = new Uri("cdda://" + track_index + "#" + device); 
            } 
        }
        
        public string Device { 
            get { 
                return device; 
            } 
        }
        
        public bool CanRip {
            get {
                return do_rip;
            }
            
            set {
                do_rip = value;
            }
        }
    }
    
    public class AudioCdDisk
    {
        private string udi;
        private string device_node;
        private string drive_name;
        
        private string artist_name;
        private string album_title;
        
        private AudioCdTrackInfo [] tracks;
        
        public event EventHandler Updated;

        public AudioCdDisk(DiskInfo disk)
        {
            udi = disk.Udi;
            device_node = disk.DeviceNode;
            drive_name = disk.DriveName;
            
            LoadDiskInfo();
        }
              
        private void LoadDiskInfo()
        {
            ArrayList track_list = new ArrayList();
            SimpleDisc mb_disc = new SimpleDisc(device_node);
            //mb_disc.Client.Debug = true;
            
            foreach(SimpleTrack mb_track in mb_disc) {
                AudioCdTrackInfo track = new AudioCdTrackInfo(device_node);
                track.Duration = mb_track.Length;
                track.TrackIndex = mb_track.Index;
                track.Artist = Catalog.GetString("Unknown Artist");
                track.Album = Catalog.GetString("Unkown Album");
                track.Title = String.Format(Catalog.GetString("Track {0}"), mb_track.Index);
                
                track_list.Add(track);
            }
            
            album_title = Catalog.GetString("Audio CD");
            tracks = track_list.ToArray(typeof(AudioCdTrackInfo)) as AudioCdTrackInfo [];
            
            ThreadPool.QueueUserWorkItem(QueryMusicBrainz, mb_disc);
        }
        
        private void QueryMusicBrainz(object o)
        {
            SimpleDisc mb_disc = o as SimpleDisc;
            
            try {
                mb_disc.QueryCDMetadata();
            } catch(Exception) {
                return;
            }
            
            int min = tracks.Length < mb_disc.Tracks.Length 
                ? tracks.Length : mb_disc.Tracks.Length;
            
            if(mb_disc.AlbumName != null) {
                album_title = mb_disc.AlbumName;
            } 
            
            for(int i = 0; i < min; i++) {
                tracks[i].Duration = mb_disc[i].Length;
                tracks[i].TrackIndex = mb_disc[i].Index;
                
                if(mb_disc[i].Artist != null) {
                    tracks[i].Artist = mb_disc[i].Artist;
                }
                
                if(mb_disc.AlbumName != null) {
                    tracks[i].Album = mb_disc.AlbumName;
                }
                
                if(mb_disc[i].Title != null) {
                    tracks[i].Title = mb_disc[i].Title;
                }
                
                tracks[i].Asin = mb_disc.AmazonAsin;
            }
            
            string asin = mb_disc.AmazonAsin;
            mb_disc.Dispose();
            
            Gtk.Application.Invoke(delegate {
                if(Updated != null) {
                    Updated(this, new EventArgs());
                }
            });
            
            string path = Paths.GetCoverArtPath(asin);
            if(System.IO.File.Exists(path)) {
                return;
            }
            
            if(AmazonCoverFetcher.Fetch(asin, Paths.CoverArtDirectory)) {
                Gtk.Application.Invoke(delegate {
                    if(Updated != null) {
                        Updated(this, new EventArgs());
                    }
                });
            }
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
                using(UnixStream stream = UnixFile.Open(device_node, 
                    (Mono.Unix.OpenFlags)(UnixFileUtil.OpenFlags.O_RDONLY | 
					UnixFileUtil.OpenFlags.O_NONBLOCK))) {
                    return ioctl(stream.Handle, open
                        ? EjectOperation.Open
                        : EjectOperation.Close) == 0;
                }
            } catch {
                return false;
            }
        }
        
        public string Udi { 
            get { 
                return udi;
            }
        }
        
        public string DeviceNode { 
            get { 
                return device_node;
            }
        }
        
        public string DriveName { 
            get { 
                return drive_name;
            }
        }
        
        public string Title { 
            get { 
                return album_title;
            }
        }
        
        public int TrackCount { 
            get { 
                return tracks.Length; 
            }
        }
        
        public bool Valid { 
            get { 
                return true;
            }
        }
        
        public AudioCdTrackInfo [] Tracks { 
            get { 
                return tracks; 
            }
        }
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
        
        private CdDetectUdiCallback AddedCallback;
        private CdDetectUdiCallback RemovedCallback;
        
        public event EventHandler Updated;
        public event AudioCdCoreDiskRemovedHandler DiskRemoved;
        public event AudioCdCoreDiskAddedHandler DiskAdded;
        
        public AudioCdCore()
        {
            IntPtr ptr = cd_detect_new();
            if(ptr == IntPtr.Zero) {
                throw new ApplicationException(
                    Catalog.GetString("Could not initialize HAL for CD Detection"));
            }
            
            handle = new HandleRef(this, ptr);
                
            AddedCallback = new CdDetectUdiCallback(OnDiskAdded);
            RemovedCallback = new CdDetectUdiCallback(OnDeviceRemoved);
                
            cd_detect_set_device_added_callback(handle, AddedCallback);
            cd_detect_set_device_removed_callback(handle, RemovedCallback);    
            
            DebugLog.Add("Audio CD Core Initialized");
            
            BuildInitialList();
        }
        
        public void Dispose()
        {
            cd_detect_free(handle);
        }
    
        private AudioCdDisk CreateDisk(DiskInfo halDisk)
        {
            try {
                AudioCdDisk disk = new AudioCdDisk(halDisk);
                disk.Updated += OnAudioCdDiskUpdated;
                if(disk.Valid) {
                    disks[disk.Udi] = disk;
                }
                return disk;
            } catch(Exception e) {
                Gtk.Application.Invoke(e.Message, new EventArgs(), OnCouldNotReadCDError);
            }
            
            return null;
        }
        
        private void OnCouldNotReadCDError(object o, EventArgs args)
        {
            ErrorDialog.Run(Catalog.GetString("Could not Read Audio CD"), o as string);
        }
    
        private void OnDiskAdded(IntPtr udiPtr)
        {
            string udi = Marshal.PtrToStringAnsi(udiPtr);

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
            
            if(udi == null || disks[udi] == null) {
                 return;
            }
                 
            disks.Remove(udi);
            HandleUpdated();
            
            AudioCdCoreDiskRemovedHandler handler = DiskRemoved;
            if(handler != null) {
                AudioCdCoreDiskRemovedArgs args = new AudioCdCoreDiskRemovedArgs();
                args.Udi = udi;
                handler(this, args);
            }
        }
          
        private DiskInfo [] GetHalDisks()
        {
            IntPtr arrayPtr = cd_detect_get_disk_array(handle);
            int arraySize = 0;
            
            ArrayList disks = new ArrayList();

            if(arrayPtr == IntPtr.Zero) {
                return null;
            }
            
            while(Marshal.ReadIntPtr(arrayPtr, arraySize * IntPtr.Size) != IntPtr.Zero) {
                arraySize++;
            }
            
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
                CreateDisk(halDisk);
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
