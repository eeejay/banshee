/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  IpodCore.cs
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
using Mono.Posix;
using IPod;

namespace Banshee
{
	public class IpodCore : IDisposable
	{
		private static IpodCore instance;
		private DeviceEventListener listener;
		private Hashtable devices;
		
		public event EventHandler Updated;
	
		public IpodCore()
		{
			IPod.Initializer.UseDefaultContext = true;
			listener = new DeviceEventListener();
			listener.DeviceAdded += OnDeviceAdded;
			listener.DeviceRemoved += OnDeviceRemoved;
			
			devices = new Hashtable();
			
			foreach(Device device in Device.ListDevices()) {
				devices[device.VolumeId] = device;
			}
		}
		
		public void Dispose()
		{
			
		}
		
		private void OnDeviceAdded(object o, DeviceAddedArgs args)
		{
			if(devices[args.Udi] == null) {
				devices[args.Udi] = new Device(args.Udi);
				HandleUpdated();
			}
		}
		
		private void OnDeviceRemoved(object o, DeviceRemovedArgs args)
		{
			if(devices[args.Udi] != null) {
				devices.Remove(args.Udi);
				HandleUpdated();
			}
		}
		
		public void ListAll()
		{
			//foreach(Device device in Device.ListDevices())
			//	device.Debug();
		}
		
		private void HandleUpdated()
		{
			EventHandler handler = Updated;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public Device [] Devices
		{
			get {
				ArrayList list = new ArrayList(devices.Values);
				return list.ToArray(typeof(Device)) as Device [];
			}
		}
	}
	
	public class IpodSyncTransaction : LibraryTransaction
	{
		public override string Name {
			get {
				return "iPod Sync Transaction";
			}
		}
	
		private Device device;
		
		public IpodSyncTransaction(Device device)
		{
			this.device = device;
			showCount = false;
		}

		private string ToLower (string str)
		{
			if (str == null)
				return null;
			else
				return str.ToLower ();
		}
		
		private bool TrackCompare(LibraryTrackInfo libTrack, Song song)
		{
			return ToLower(song.Title) == ToLower(libTrack.Title) && 
				ToLower(song.Album) == ToLower(libTrack.Album) &&
				ToLower(song.Artist) == ToLower(libTrack.Artist) &&
				song.Year == libTrack.Year &&
				song.TrackNumber == libTrack.TrackNumber;
		}
		
		private bool ExistsOnIpod(Song[] songs, LibraryTrackInfo libTrack)
		{
			foreach(Song song in songs) {
				if(TrackCompare(libTrack, song))
					return true;
			}
			
			return false;
		}
		
		private bool ExistsInLibrary(Song song)
		{
			foreach(LibraryTrackInfo libTrack in Core.Library.Tracks.Values) {
				if(TrackCompare(libTrack, song))
					return true;
			}
			
			return false;
		}

		public override void Run()
		{
			statusMessage = String.Format(Catalog.GetString(
				"Preparing to sync '{0}'"), device.Name);
				
			currentCount = 0;
			totalCount = 0;
			
			bool doUpdate = false;
			
			foreach(Song song in device.SongDatabase.Songs) {
				if(ExistsInLibrary(song))
					continue;

				device.SongDatabase.RemoveSong(song);
				doUpdate = true;
			}

			Song[] ipodSongs = device.SongDatabase.Songs;
			
			foreach(LibraryTrackInfo libTrack in Core.Library.Tracks.Values) {
				if(ExistsOnIpod(ipodSongs, libTrack) || libTrack.Uri == null)
					continue;
					
				Song song = device.SongDatabase.CreateSong();
				song.Album = libTrack.Album;
				song.Artist = libTrack.Artist;
				song.Title = libTrack.Title;
				song.Genre = libTrack.Genre;
				song.Length = (int)(libTrack.Duration * 1000);
				song.TrackNumber = (int)libTrack.TrackNumber;
				song.TotalTracks = (int)libTrack.TrackCount;
				song.Filename = libTrack.Uri;
				song.Year = (int)libTrack.Year;
				
				if(song.Artist == null)
					song.Artist = String.Empty;

				if(song.Album == null)
					song.Album = String.Empty;

				if(song.Title == null)
					song.Title = String.Empty;

				if(song.Genre == null)
					song.Genre = String.Empty;

				doUpdate = true;
			}
			
			if(!doUpdate)
				return;
			
			device.SongDatabase.SaveProgressChanged += OnSaveProgressChanged;

			device.SongDatabase.Save();

			device.SongDatabase.SaveProgressChanged -= OnSaveProgressChanged;
		} 
		
		private void OnSaveProgressChanged(SongDatabase db, Song song, 
			double currentPercent, int completed, int total)
		{
			currentCount = completed;
			totalCount = total;
			statusMessage = String.Format(Catalog.GetString(
				"Copying {0} - {1}"), song.Artist, song.Title); 
		}
	}
}
