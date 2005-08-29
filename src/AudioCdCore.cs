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
using Mono.Unix;

namespace Banshee
{
	internal delegate void CdDetectUdiCallback(IntPtr udiPtr);

	[StructLayout(LayoutKind.Sequential)]
	internal struct DiskInfoRaw
	{
		public IntPtr Udi;
		public IntPtr DeviceNode;
		public IntPtr DriveName;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct CdDiskInfoRaw
	{
		public IntPtr device_node;
		
		public long n_tracks;
		public long total_sectors;
		public long total_time;
		
		public IntPtr tracks;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct CdTrackInfoRaw
	{
		int number;
		
		int duration;
		int minutes;
		int seconds;
		
		long start_sector;
		long end_sector;
		long sectors;
		long start_time;
		long end_time;
	}
	
	public class AudioDisk
	{
		private string udi;
		private string deviceNode;
		private string driveName;
		
		private int trackCount;

		[DllImport("libbanshee")]
		private static extern IntPtr cd_disk_info_new(string device_node);
		
		[DllImport("libbanshee")]
		private static extern void cd_disk_info_free(IntPtr diskPtr);

		public AudioDisk(string udi, string deviceNode, string driveName)
		{
			this.udi = udi;
			this.deviceNode = deviceNode;
			this.driveName = driveName;
			
			LoadDiskInfo();
		}
		
		private void LoadDiskInfo()
		{
			IntPtr diskPtr = cd_disk_info_new(deviceNode);
			CdDiskInfoRaw diskRaw = (CdDiskInfoRaw)Marshal.PtrToStructure(
				diskPtr, typeof(CdDiskInfoRaw));
				
			trackCount = (int)diskRaw.n_tracks;
			
			cd_disk_info_free(diskPtr);
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
					OpenFlags.O_RDONLY | OpenFlags.O_NONBLOCK)) {
					return ioctl(stream.Handle, open
						? EjectOperation.Open
						: EjectOperation.Close) == 0;
				}
			} catch {
				return false;
			}
		}
		
		public string Udi
		{
			get {
				return udi;
			}
		}
		
		public string DeviceNode
		{
			get {
				return deviceNode;
			}
		}
		
		public string DriveName
		{
			get {
				return driveName;
			}
		}
		
		public int TrackCount
		{
			get {
				return trackCount;
			}
		}
	}
	
	public class AudioCdCore : IDisposable
	{
		private static AudioCdCore instance;
		private Hashtable disks;
		private HandleRef handle;
		
		private CdDetectUdiCallback AddedCallback;
		private CdDetectUdiCallback RemovedCallback;
		
		public event EventHandler Updated;
		
		public AudioCdCore()
		{
			IntPtr ptr = cd_detect_new();
			if(ptr == IntPtr.Zero)
				throw new ApplicationException(
					"Could not initialize HAL for CD Detection");
			
			handle = new HandleRef(this, ptr);
				
			AddedCallback = new CdDetectUdiCallback(OnDiskAdded);
			RemovedCallback = new CdDetectUdiCallback(OnDeviceRemoved);
				
			cd_detect_set_device_added_callback(handle, AddedCallback);
			cd_detect_set_device_removed_callback(handle, RemovedCallback);	
						
			DebugLog.Add("Audio CD Core Initialized");
			
			BuildList();
		}
		
		public void Dispose()
		{
			cd_detect_free(handle);
		}
	
		private void OnDiskAdded(IntPtr udiPtr)
		{
			BuildList();
		}
		
		private void OnDeviceRemoved(IntPtr udiPtr)
		{
			BuildList();
		}
	
		private void BuildList()
		{
			IntPtr arrayPtr = cd_detect_get_disk_array(handle);
			int arraySize = 0;
			
			disks = new Hashtable();
			
			if(arrayPtr == IntPtr.Zero)
				return;
			
			while(Marshal.ReadIntPtr(arrayPtr, arraySize * IntPtr.Size)
				!= IntPtr.Zero)
				arraySize++;
			
			for(int i = 0; i < arraySize; i++) {
				IntPtr rawPtr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
				DiskInfoRaw diskRaw = (DiskInfoRaw)Marshal.PtrToStructure(
					rawPtr, typeof(DiskInfoRaw));
				
				AudioDisk disk = new AudioDisk(
					Marshal.PtrToStringAnsi(diskRaw.Udi),
					Marshal.PtrToStringAnsi(diskRaw.DeviceNode),
					Marshal.PtrToStringAnsi(diskRaw.DriveName)
				);
				
				disks[disk.Udi] = disk;
			}
			
			cd_detect_disk_array_free(arrayPtr);
			
			HandleUpdated();
		}
		
		private void HandleUpdated()
		{
			EventHandler handler = Updated;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public AudioDisk [] Disks
		{
			get {
				ArrayList list = new ArrayList(disks.Values);
				return list.ToArray(typeof(AudioDisk)) as AudioDisk [];
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
