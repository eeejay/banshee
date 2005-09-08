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
using System.IO;
using Mono.Posix;
using IPod;

namespace Banshee
{
    public delegate void IpodDeviceAddedHandler(object o, IpodDeviceArgs args);
    public delegate void IpodDeviceRemovedHandler(object o, IpodDeviceArgs args); 

    public class IpodDeviceArgs
    {
        public Device Device;
    }

	public class IpodCore : IDisposable
	{
		private static IpodCore instance;
		private DeviceEventListener listener;
		private Hashtable devices;
		
		private static string [] validSongExtensions = 
		  {"mp3", "aac", "mp4", "m4a", "m4p"};
		
		public event IpodDeviceAddedHandler DeviceAdded;
		public event IpodDeviceRemovedHandler DeviceRemoved;
	
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
				IpodDeviceAddedHandler handler = DeviceAdded;
				if(handler != null) {
				    IpodDeviceArgs dargs = new IpodDeviceArgs();
				    dargs.Device = devices[args.Udi] as Device;
				    handler(this, dargs);
			    }
			}
		}
		
		private void OnDeviceRemoved(object o, DeviceRemovedArgs args)
		{
			if(devices[args.Udi] != null) {
			    Device device = devices[args.Udi] as Device; 
				devices.Remove(args.Udi);
				IpodDeviceRemovedHandler handler = DeviceRemoved;
				if(handler != null) {
				    IpodDeviceArgs dargs = new IpodDeviceArgs();
				    dargs.Device = device;
				    handler(this, dargs);
			    }
			}
		}
		
		public void ListAll()
		{
			//foreach(Device device in Device.ListDevices())
			//	device.Debug();
		}
		
		public Device [] Devices
		{
			get {
				ArrayList list = new ArrayList(devices.Values);
				return list.ToArray(typeof(Device)) as Device [];
			}
		}
		
        public static bool ValidSongFormat(string filename)
        {
            string ext = Path.GetExtension(filename).ToLower().Trim();

            foreach(string vext in validSongExtensions) {
                if(ext == "." + vext) {
                    return true;
                }
            }
            
            return false;
        }
        
        public static string GetIpodSong(string filename)
        {
            if(ValidSongFormat(filename)) 
                return filename;
                
            string path = Library.MakeFilenameKey(filename);
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileNameWithoutExtension(filename);
            
            foreach(string vext in validSongExtensions) {
                string newfile = dir + Path.DirectorySeparatorChar +  
                    ".banshee-ipod-" + file + "." + vext;
                
                if(File.Exists(newfile))
                    return newfile;
            }
            
            foreach(string vext in validSongExtensions) {
                string newfile = path + "." + vext;
                
                if(File.Exists(newfile))
                    return newfile;
            }   
                 
            return null;
        }
        
        public static string ConvertSongName(string filename, string newext)
        {
            string path = Library.MakeFilenameKey(filename);
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileNameWithoutExtension(filename);
            
            return dir + Path.DirectorySeparatorChar + 
                ".banshee-ipod-" + file + "." + newext;
        }
	}
	
	public class IpodSyncTransaction : LibraryTransaction
	{
		public override string Name {
			get {
				return "iPod Sync Transaction";
			}
		}
		
		private PipelineProfile profile;
		private FileEncoder encoder;
		private Device device;
		private ArrayList updateTracks = null;
		private ArrayList removeTracks = null;
		
		public event EventHandler SyncStarted;
		public event EventHandler SyncCompleted;
		
		public IpodSyncTransaction(Device device)
		{
			this.device = device;
			showCount = false;
		}
		
		public IpodSyncTransaction(Device device, ArrayList tracks, 
		  ArrayList removeTracks) : this(device)
		{
			updateTracks = tracks;
			this.removeTracks = removeTracks;
		}
		
		private bool ExistsOnIpod(Song[] songs, TrackInfo libTrack)
		{
			if(libTrack.GetType() == typeof(IpodTrackInfo)) {
				if((libTrack as IpodTrackInfo).NeedSync) {
					return false;
				}
			}
		
			foreach(Song song in songs) {
				if(IpodMisc.TrackCompare(libTrack, song))
					return true;
			}
			
			return false;
		}
		
		private bool ExistsInLibrary(Song song)
		{
			foreach(LibraryTrackInfo libTrack in Core.Library.Tracks.Values) {
				if(IpodMisc.TrackCompare(libTrack, song))
					return true;
			}
			
			return false;
		}
		
		protected override void CancelAction()
		{
		    encoder.Cancel();
		}

		private void EmitSyncStarted()
		{
			EventHandler handler = SyncStarted;
			if(handler != null)
				handler(this, new EventArgs());
		}

		private void EmitSyncCompleted()
		{
			EventHandler handler = SyncCompleted;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public override void Run()
		{
			EmitSyncStarted();
			try {
				WrapRun();
			} catch(Exception e) {
				Core.Log.Push(LogEntryType.UserError, 
					Catalog.GetString("Error Syncing iPod"),
					e.Message);
			}
			EmitSyncCompleted();
		}
		
		public void WrapRun()
		{
			//statusMessage = String.Format(Catalog.GetString(
			//	"Preparing to sync '{0}'"), device.Name);
				
			currentCount = 0;
			totalCount = 0;
			
			bool doUpdate = false;
			
			profile = PipelineProfile.GetConfiguredProfile(
                "Ipod", "mp3,aac,mp4,m4a,m4p");
            encoder = new GstFileEncoder();
            encoder.Progress += OnEncodeProgress;
			
			if(updateTracks == null) {
				foreach(Song song in device.SongDatabase.Songs) {
					if(ExistsInLibrary(song))
						continue;

					device.SongDatabase.RemoveSong(song);
					doUpdate = true;
				}
			} else if(removeTracks != null) {
			    foreach(IpodTrackInfo libTrack in removeTracks) {
			         device.SongDatabase.RemoveSong(libTrack.Song);
			         doUpdate = true;
			    }
			}
			
			Song[] ipodSongs = device.SongDatabase.Songs;
			
			ICollection collection;
			if(updateTracks == null)
				collection = Core.Library.Tracks.Values;
			else
				collection = updateTracks;
			
			foreach(TrackInfo libTrack in collection) {
			     if(cancelRequested)
			         break;
			         
			    if(ExistsOnIpod(ipodSongs, libTrack) || libTrack.Uri == null)
			    		continue;
			         
			    Song song = null;
			         
			    if(libTrack.GetType() == typeof(LibraryTrackInfo)) {
				    song = IpodMisc.TrackInfoToSong(device, 
				    	libTrack as LibraryTrackInfo);
				} else if(libTrack.GetType() == typeof(IpodTrackInfo)) {
					song = (libTrack as IpodTrackInfo).Song;
				} else {
					continue;
				}
				
				if(song == null)
					continue;
				
			    string filename = IpodCore.GetIpodSong(libTrack.Uri);
			    if(filename == null) {
			         if(profile == null) {
			             continue;
			         }
			       
			         filename = IpodCore.ConvertSongName(libTrack.Uri,
			             profile.Extension);
			         
				  //  statusMessage = String.Format(
				//	Catalog.GetString("Encoding for iPod Usage: {0} - {1}"),
				//	song.Artist, song.Title);
					
					
			        try {
			        		encoder.Encode(libTrack.Uri, filename, profile);
			        	} catch(Exception e) {
			        		Core.Log.Push(LogEntryType.Warning,
			        			Catalog.GetString("Could not encode file for iPod"),
			        			String.Format("{0} -> {1} failed",
			        				libTrack.Uri, filename));
			        		filename = null;
			        }
			    }
			    
			    if(filename == null)
			         continue;

				try {
					song.Filename = StringUtil.UriToFileName(filename);
				} catch(Exception e) {
					Core.Log.Push(LogEntryType.Warning,
		        			Catalog.GetString("Could not transfer file to iPod"),
		        			e.Message);
					continue;
				}

				doUpdate = true;
			}
			
			encoder.Dispose();
			
			if(!doUpdate || cancelRequested) {
			    device.SongDatabase.Reload();
				return;
		    }
			
			//device.SongDatabase.SaveProgressChanged += OnSaveProgressChanged;

			try {
			    //statusMessage = Catalog.GetString("Synchronizing iPod...");
			    //currentCount = 0;
			    //totalCount = 0;
				device.SongDatabase.Save();
			} catch(Exception e) {
				Core.Log.Push(LogEntryType.UserError, 
					Catalog.GetString("Could not sync iPod"),
					e.Message);
			}
			

			//device.SongDatabase.SaveProgressChanged -= OnSaveProgressChanged;
		} 
		
		/*private void OnSaveProgressChanged(SongDatabase db, Song song, 
			double currentPercent, int completed, int total)
		{
			
			currentCount = completed;
			totalCount = total;
			statusMessage = String.Format(Catalog.GetString(
				"Copying {0} - {1}"), song.Artist, song.Title);
		}*/
		
		private void OnEncodeProgress(object o, FileEncoderProgressArgs args)
		{
		    totalCount = 100;
		    currentCount = (int)(100.0 * args.Progress);
		}
	}
	
	public class IpodMisc
	{
		public static Song TrackInfoToSong(Device device, 
			LibraryTrackInfo libTrack)
		{
			Song song = device.SongDatabase.CreateSong();
			
			if(libTrack.Album != null)
				song.Album = libTrack.Album;
			
			if(libTrack.Artist != null)
				song.Artist = libTrack.Artist;
			
			if(libTrack.Title != null)
				song.Title = libTrack.Title;
			
			if(libTrack.Genre != null)
				song.Genre = libTrack.Genre;
			
			song.Length = (int)(libTrack.Duration * 1000);
			song.TrackNumber = (int)libTrack.TrackNumber;
			song.TotalTracks = (int)libTrack.TrackCount;
			song.Year = (int)libTrack.Year;
			//song.LastPlayed = libTrack.LastPlayed;
			song.LastPlayed = DateTime.MinValue;
			
			switch(libTrack.Rating) {
			    case 1: song.Rating = SongRating.Zero; break;
			    case 2: song.Rating = SongRating.Two; break;
			    case 3: song.Rating = SongRating.Three; break;
			    case 4: song.Rating = SongRating.Four; break;
			    case 5: song.Rating = SongRating.Five; break;
			    default: song.Rating = SongRating.Zero; break;
			}
			
			if(song.Artist == null)
				song.Artist = String.Empty;

			if(song.Album == null)
				song.Album = String.Empty;

			if(song.Title == null)
				song.Title = String.Empty;

			if(song.Genre == null)
				song.Genre = String.Empty;
				
			return song;
		}
		
		private static string ToLower (string str)
		{
			if (str == null)
				return null;
			else
				return str.ToLower ();
		}
		
		public static bool TrackCompare(TrackInfo libTrack, Song song)
		{
			return ToLower(song.Title) == ToLower(libTrack.Title) && 
				ToLower(song.Album) == ToLower(libTrack.Album) &&
				ToLower(song.Artist) == ToLower(libTrack.Artist) &&
				song.Year == libTrack.Year &&
				song.TrackNumber == libTrack.TrackNumber;
		}
		
	}
}
