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
using Sql;
using GConf;

namespace Sonance
{
	public class Library
	{
		public Database Db;
		public LibraryTransactionManager TransactionManager;
		public Hashtable Tracks;
		public Hashtable Playlists;
		
		public event EventHandler Reloaded;
		
		public Library()
		{
			Tracks = new Hashtable();
			Playlists = new Hashtable();
			ReloadDatabase();
			TransactionManager = new LibraryTransactionManager();
		}
		
		public void ReloadDatabase()
		{
			string libraryLocation = Location;

			if(!Directory.Exists(libraryLocation))
				Directory.CreateDirectory(libraryLocation);
			
			Db = new Database("Library", libraryLocation + "/Library.sdb");
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
	}

	public enum SourceType : uint {
		Library = 1,
		Playlist = 2
	}

	public abstract class Source
	{
		protected string name;
		protected SourceType type;
		
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
			
			set {
				UpdateName(name, value);
				EventHandler handler = Updated;
				if(handler != null)
					handler(this, new EventArgs());
			}
		}
		
		public SourceType Type
		{
			get {
				return type;
			}
		}
		
		public abstract void UpdateName(string oldName, string newName);
	}
	
	public class LibrarySource : Source
	{
		public LibrarySource() : base("Library", SourceType.Library)
		{
		
		}
		
		public override void UpdateName(string oldName, string newName)
		{
			
		}
	}
	
	public class PlaylistSource : Source
	{
		public PlaylistSource(string name) : base(name, SourceType.Playlist)
		{
		
		}
		
		public override void UpdateName(string oldName, string newName)
		{
			if(oldName.Equals(newName))
				return;
			
			Playlist pl = new Playlist(oldName);
			pl.Rename(newName);
			name = newName;
		}
	}

	public class Playlist 
	{
		public string name;
		public ArrayList items;
		
		public event EventHandler Saved;
		
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
		
		public static string UniqueName
		{
			get {
				string prefix = "New Playlist ";
				
				for(int i = 1; true; i++) {
					string name = prefix + i;
					if(!Playlist.Exists(name))
						return name;
				}
			}
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
				while(reader.Read()){
					list.Add(reader[0]);
				Console.WriteLine(reader[0]);}
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
			items.Add(ti);
		}
		
		public void Append(ArrayList tis)
		{
			foreach(TrackInfo ti in tis)
				items.Add(ti);
		}
		
		public void Rename(string newName)
		{
			Statement query = new Update("Playlists", "Name", newName) + 
				new Where("PlaylistID", Op.EqualTo, Playlist.GetId(name));
			try {
				Core.Library.Db.Execute(query);
				name = newName;
			} catch(Exception) {}
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
			EventHandler handler = Saved;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public string Name
		{
			get {
				return name;
			}
		}
	}
}
