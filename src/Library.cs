/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Library.cs
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
using System.Data;
using System.IO;
using Mono.Unix;
using Sql;
using GConf;

namespace Banshee
{
	public class Library
	{
		public Database Db;
		public LibraryTransactionManager TransactionManager;
		public Hashtable Tracks;
		public Hashtable TracksFnKeyed;
		public Hashtable Playlists;
		
		public event EventHandler Reloaded;
		public event EventHandler Updated;
		
		public Library()
		{
			Tracks = new Hashtable();
			TracksFnKeyed = new Hashtable();
			Playlists = new Hashtable();
			ReloadDatabase();
			TransactionManager = new LibraryTransactionManager();
		}
		
		public void ReloadDatabase()
		{
			string libraryLocation = Location;

			if(!Directory.Exists(libraryLocation))
				Directory.CreateDirectory(libraryLocation);
			
			Db = new Database("Library", libraryLocation + "/.banshee.db");
		}
		
		public void ReloadLibrary()
		{
			SqlLoadTransaction transaction = new SqlLoadTransaction(
				new Select("Tracks"));
			
			Tracks.Clear();
			transaction.Finished += OnReloadLibraryFinished;
			TransactionManager.Register(transaction);
			
			/*string [] names = Playlist.ListAll();
			if(names == null)
				return;
				
			Playlists.Clear();
			foreach(string name in names) {
				Playlist playlist = new Playlist(name);
				playlist.Load();
				Playlists[name] = playlist;
			}*/
		}
		
		private void OnReloadLibraryFinished(object o, EventArgs args)
		{
			EventHandler handler = Reloaded;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public string Location
		{
			 get {
			 	GConf.Client gc = Core.IsInstantiated
			 		? Core.GconfClient
			 		: new GConf.Client();
			 		
				string libraryLocation;
			
				try {
					libraryLocation = (string)gc.Get(GConfKeys.LibraryLocation);
				} catch(Exception) {
			 		libraryLocation = Paths.DefaultLibraryPath;
				}
			
				gc.Set(GConfKeys.LibraryLocation, libraryLocation);
				
				return libraryLocation;		 	
			 }	
		}
		
		public void SetTrack(int id, LibraryTrackInfo track)
		{
		    lock(Tracks.SyncRoot) {
		    		Tracks[id] = track;
		    	}
		    	
		    lock(TracksFnKeyed.SyncRoot) {
		    		TracksFnKeyed[MakeFilenameKey(track.Uri)] = track;
		    	}
		    
		    EventHandler handler = Updated;
		    if(handler != null)
		      handler(this, new EventArgs());
		}
		
		public void Remove(LibraryTrackInfo track)
		{
		    lock(Tracks.SyncRoot) {
		      Tracks.Remove(track.TrackId);
		    }
		    
		    lock(TracksFnKeyed.SyncRoot) {
		      TracksFnKeyed.Remove(MakeFilenameKey(track.Uri));
		    }
		}
		
		public static string MakeFilenameKey(Uri uri)
		{
		      string path = uri.LocalPath;
		
    		      return Path.GetDirectoryName(path) + 
                     Path.DirectorySeparatorChar + 
                     Path.GetFileNameWithoutExtension(path);
		}
	}

	public enum SourceType : uint {
		Library = 1,
		Playlist = 2,
		Ipod = 3,
		AudioCd
	}

	public abstract class Source
	{
		protected string name;
		protected SourceType type;
		protected bool canEject;
		protected bool canRename;

		public event EventHandler Updated;
		
		public Source(string name, SourceType type)
		{
			this.name = name;
			this.type = type;
		}
		
		public string Name 
		{
			get {
				return name;
			}
		}
		
		public bool Rename(string newName)
		{
		    if(!UpdateName(name, newName))
				    return false;
				    
        		EventHandler handler = Updated;
        		if(handler != null)
        			handler(this, new EventArgs());
        			
        	   return true;
		}
		
		public SourceType Type
		{
			get {
				return type;
			}
		}
		
		public abstract int Count
		{
			get;
		}
		
		public bool CanEject
		{
			get {
				return canEject;
			}
		}
		
		public bool CanRename
		{
		    get {
		        return canRename;
		    }
		}
		
		public virtual bool UpdateName(string oldName, string newName)
		{
		  return false;
		}
		
		public virtual bool Eject()
		{
			return false;
		}
	}
	
	public class LibrarySource : Source
	{
		public LibrarySource() : base(Catalog.GetString("Library"), 
		  SourceType.Library)
		{
		      canRename = false;
		}
		
		public override int Count
		{
			get {
				return Core.Library.Tracks.Count;	
			}
		}
	}
	
	public class PlaylistSource : Source
	{
		int count = -1;
	
		public PlaylistSource(string name) : base(name, SourceType.Playlist)
		{
		    canRename = true;
		}
		
		public override bool UpdateName(string oldName, string newName)
		{
			if(oldName.Equals(newName))
				return false;
			
			Playlist pl = new Playlist(oldName);
			if(pl.Rename(newName)) {
			     name = newName;
			     return true;
			}
			
			Core.ThreadEnter();
			MessageDialogs.CannotRenamePlaylist();
			Core.ThreadLeave();
			
			return false;
		}
		
		public override int Count
		{
			get {
				if(count < 0) {
					Playlist pl = new Playlist(name);
					count = pl.Count;
				}
				
				return count;
			}
		}			
	}

	public class IpodSource : Source
	{
		private IPod.Device device;
		private ArrayList tracks = new ArrayList();
		private ArrayList removeTracks = new ArrayList();
		private bool needSync = false;
		public bool IsSyncing;
		
		public IpodSource(IPod.Device device) : base(device.Name, 
			SourceType.Ipod)
		{
			this.device = device;
			canEject = true;
			canRename = true;
			
			Refresh();
		}
		
		private void Refresh()
		{
			tracks.Clear();
			removeTracks.Clear();
			needSync = false;
			foreach(IPod.Song song in device.SongDatabase.Songs)
				tracks.Add(new IpodTrackInfo(song));
		}
		
		public void SetSourceName(string name)
		{
		  this.name = name;
		}
		
		public override bool UpdateName(string oldName, string newName)
		{
			if(oldName == null || !oldName.Equals(newName)) {
				device.Name = newName;
				name = newName;
				device.Save();
			}
			
			return true;
		}
		
		public override int Count
		{
			get {
				//return device.SongDatabase.Songs.Length;
				return tracks.Count;
			}
		}
		
		public IPod.Device Device
		{
			get {
				return device;
			}
		}
		
		public override bool Eject()
		{
			device.Eject();
			return true;
		}

		public double DiskUsageFraction
		{
			get {
				return (double)device.VolumeUsed /
					(double)device.VolumeSize;
			}
		}
		
		public bool NeedSync
		{
			get {
				if(needSync || removeTracks.Count > 0)
					return true;
				
				foreach(IpodTrackInfo iti in tracks) {
					if(iti.NeedSync)
						return true;
				}
				
				return false;
			}
		}
		
		public ArrayList Tracks
		{
			get {
				return tracks;
			}
		}
		
		private bool ExistsOnIpod(LibraryTrackInfo libTrack)
		{
			foreach(IPod.Song song in device.SongDatabase.Songs) {
				if(IpodMisc.TrackCompare(libTrack, song))
					return true;
			}
			
			return false;
		}
		
		public void QueueForSync(LibraryTrackInfo ti)
		{
			if(ti == null)
				return;
				
			foreach(IpodTrackInfo iti in tracks) {
				if(iti.LibraryTrack != null && ti.Equals(iti.LibraryTrack))
					return;
			}
			
			if(ExistsOnIpod(ti))
				return;
			
			tracks.Add(new IpodTrackInfo(device, ti));
		}
		
		public void Remove(IpodTrackInfo iti)
		{
			if(!iti.NeedSync)
				needSync = true;
			device.SongDatabase.RemoveSong(iti.Song);
			tracks.Remove(iti);
			removeTracks.Add(iti);
		}
		
		public IpodSync Sync(bool full)
		{
			IpodSync sync;
			
			if(full)
				sync = new IpodSync(device);
			else
				sync = new IpodSync(device, tracks, removeTracks);
				
			sync.SyncStarted += OnIpodSyncStarted;
			sync.SyncCompleted += OnIpodSyncCompleted;
			
			return sync;
		}
		
		private void OnIpodSyncStarted(object o, EventArgs args)
		{
			IsSyncing = true;
		}
		
		private void OnIpodSyncCompleted(object o, EventArgs args)
		{
			IsSyncing = false;
			Refresh();
	    }

		private static string BytesToString(ulong bytes)
		{
			ulong mb = bytes / (1024 * 1024);

			if (mb > 1024)
				return String.Format(Catalog.GetString("{0} GB"), mb / 1024);
			else
				return String.Format(Catalog.GetString("{0} MB"), mb);
		}

		public string DiskUsageString
		{
			get {
				// Translators: iPod disk usage. Each {N} is something like "100 MB"
				return String.Format(
					Catalog.GetString("{0} of {1}"),
					BytesToString(device.VolumeUsed),
					BytesToString(device.VolumeSize));
			}
		}

		public string DiskAvailableString
		{
			get {
				// Translators: iPod disk usage. {0} is something like "100 MB"
				return String.Format(
					Catalog.GetString("({0} Remaining)"),
					BytesToString(device.VolumeAvailable));
			}
		}
	}
	
	public class AudioCdSource : Source
	{
		private AudioCdDisk disk;
		
		public AudioCdSource(AudioCdDisk disk) : base(disk.Title, 
			SourceType.AudioCd)
		{
			this.disk = disk;
			disk.Updated += OnUpdated;
			canEject = true;
			canRename = false;
		}
		
		public override int Count
		{
			get {
				return disk.TrackCount;
			}
		}
		
		public AudioCdDisk Disk
		{
			get {
				return disk;
			}
		}
		
		public override bool Eject()
		{
			disk.Eject();
			return true;
		}
		
		private void OnUpdated(object o, EventArgs args)
		{
			Core.ThreadEnter();
			name = disk.Title;
			Core.ThreadLeave();
		}
	}

    public delegate void PlaylistSavedHandler(object o, PlaylistSavedArgs args);
    
    public class PlaylistSavedArgs : EventArgs
    {
        public string Name;
    }

	public class Playlist 
	{
		public string name;
		public ArrayList items;
		
		public event PlaylistSavedHandler Saved;
		
		public static int GetId(string name)
		{
			Statement query = new Select("Playlists", new List("PlaylistID")) +
				new Where(new Compare("Name", Op.EqualTo, name));

			try {
				object result = Core.Library.Db.QuerySingle(query);
				int id = Convert.ToInt32(result);
				return id;
			} catch(Exception) {
				return 0;
			}
		}
		
		public static bool Exists(string name)
		{
			return GetId(name) > 0;
		}
		
		private static string PostfixDuplicate(string prefix)
		{
			string name = prefix;
			for(int i = 1; true; i++) {
				if(!Playlist.Exists(name))
					return name;
					
				name = prefix + " " + i;
			}
		}
		
		public static string UniqueName
		{
			get {
				return PostfixDuplicate(Catalog.GetString("New Playlist"));
			}
		}
		
		public static string GoodUniqueName(ArrayList tracks)
		{
			ArrayList names = new ArrayList();
			Hashtable groups = new Hashtable();
			
			foreach(TrackInfo ti in tracks) {
				bool haveArtist = ti.Artist != null && !ti.Artist.Equals(String.Empty);
				bool haveAlbum = ti.Album != null && !ti.Album.Equals(String.Empty);
			
				if(haveArtist && haveAlbum)
					names.Add(ti.Artist + " - " + ti.Album);
				else if(haveArtist)
					names.Add(ti.Artist);
				else if(haveAlbum)
					names.Add(ti.Album);
				else
					names.Add("New Playlist");
			}
				
			names.Sort();
			groups[names[0]] = 1;
			
			for(int i = 1; i < names.Count; i++) {
				bool match = false;
				foreach(string key in groups.Keys) {
					if(names[i].Equals(key)) {
						groups[key] = ((int)groups[key]) + 1;
						match = true;
						break;
					}
				}
			
				if(match)
					continue;
			
				groups[names[i]] = 1;
			}
			
			string bestMatch = String.Empty;
			int maxValue = 0;
			
			foreach(int count in groups.Values) {
				if(count > maxValue) {
					maxValue = count;
					foreach(string key in groups.Keys) {
						if((int)groups[key] == maxValue) {
							bestMatch = key;
							break;
						}
					}
				}
			}
			
			if(bestMatch.Equals(String.Empty))
				return UniqueName;
				
			return PostfixDuplicate(bestMatch);
		}
		
		public static void Delete(string name)
		{
			int id = GetId(name);
			
			if(id <= 0)
				return;
				
			Statement query1 = new Delete("Playlists") 
				+ new Where("PlaylistID", Op.EqualTo, id);
			
			Statement query2 = new Delete("PlaylistEntries")
				+ new Where("PlaylistID", Op.EqualTo, id);
				
			try {
				Core.Library.Db.Execute(query1);
				Core.Library.Db.Execute(query2);
			} catch(Exception) {}
		}
		
		public static string [] ListAll()
		{
			Statement query = new Select("Playlists", new List("Name")) 
				+ new OrderBy("Name", OrderDirection.Asc);
			ArrayList list = new ArrayList();
			
			try {
				IDataReader reader = Core.Library.Db.Query(query);
				while(reader.Read())
					list.Add(reader[0]);
				
				return (string [])list.ToArray(typeof(string));
			} catch(Exception) {
				return null;
			}
		}
		
		public Playlist(string name)
		{
			this.name = name;
			items = new ArrayList();
		}
		
		public void Load()
		{
			/*int id = Playlist.GetId(name);
			if(id <= 0)
				return;
				
			Statement query = new Statement(
				"SELECT t.* " + 
				"FROM PlaylistEntries p, Tracks t " + 
				"WHERE t.TrackID = p.TrackID AND p.PlaylistID = " + id);
				
			SqlLoadTransaction loader = 
				new SqlLoadTransaction(query.ToString());*/
				
			PlaylistLoadTransaction loader = new PlaylistLoadTransaction(name);
			loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
			Core.Library.TransactionManager.Register(loader);
		}
		
		private void OnLoaderHaveTrackInfo(object o, HaveTrackInfoArgs args)
		{
			items.Add(args.TrackInfo);
		}
		
		public void Append(TrackInfo ti)
		{
			if(ti.CanSaveToDatabase)
				items.Add(ti);
		}
		
		public void Append(ArrayList tis)
		{
			foreach(TrackInfo ti in tis)
				Append(ti);
		}
		
		public bool Rename(string newName)
		{
			if(GetId(newName) != 0)
			   return false;
			
			Statement query = new Update("Playlists", "Name", newName) + 
				new Where("PlaylistID", Op.EqualTo, Playlist.GetId(name));
			try {
				Core.Library.Db.Execute(query);
				name = newName;
				return true;
			} catch(Exception) {
			     return false;
			}
		}
		
		public void Save()
		{
			PlaylistSaveTransaction pst = new PlaylistSaveTransaction(this);
			pst.Finished += OnPlaylistSaveTransactionFinished;
			Core.Library.TransactionManager.Register(pst);	
		}
		
		private void OnPlaylistSaveTransactionFinished(object o, 
			EventArgs args)
		{
			PlaylistSavedHandler handler = Saved;
			if(handler != null) {
			    PlaylistSavedArgs sargs = new PlaylistSavedArgs();
			    sargs.Name = name;
				handler(this, sargs);
		    }
		}
		
		public int Count
		{
			get {
				Statement query = new Select("PlaylistEntries", 
					new List("COUNT(*)")) +
					new Where("PlaylistID", Op.EqualTo, Playlist.GetId(name));
				
				object result = Core.Library.Db.QuerySingle(query);
				return Convert.ToInt32(result);
			}
		}
		
		public string Name
		{
			get {
				return name;
			}
		}
	}
}
