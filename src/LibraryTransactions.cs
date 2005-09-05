/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  LibraryTransactions.cs
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
using System.Threading;
using System.IO;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Mono.Unix;
using Sql; 

namespace Banshee 
{
	// --- Base LibraryTransaction Class 
	
	public abstract class LibraryTransaction
	{
		public event EventHandler Finished;
		public event EventHandler Canceled;
		public Thread ExecutingThread;
		
		// statistics fields for any UI progress display
		protected long totalCount;
		protected long currentCount;
		protected string statusMessage;
		
		protected long averageDuration;
		
		protected bool cancelRequested;
		
		protected bool showStatus;
		protected bool showCount;
		
		public abstract string Name {
			get;
		}

		public abstract void Run();
		
		public LibraryTransaction()
		{
			Finished = null;
			ExecutingThread = null;
			showStatus = true;
			showCount = true;
		}
		
		public bool ThreadedRun()
		{
			try {
				ExecutingThread = new Thread(SafeRun);
				ExecutingThread.Start();
			} catch(Exception) {
				return false;
			}
			
			return true;
		}
		
		public void SafeRun()
		{
			Run();
			
			/*try {
				Run();
			} catch(Exception e) {
				DebugLog.Add(String.Format(Catalog.GetString(
					"{0} threw an unhandled exception: ending " + 
					"transaction safely: {1} ({2})"),
					GetType().ToString(),
					e.GetType().ToString(), 
					e.Message));
			}*/
			
			Finish(this);
		}
		
		public void Cancel()
		{
			EventHandler handler = Canceled;
			if(handler != null)
				handler(this, new EventArgs());
				
			cancelRequested = true;
			CancelAction();
			
			if(ExecutingThread == null)
				return;
			
			ExecutingThread.Join(new TimeSpan(0, 0, 1));
			
			if(ExecutingThread.IsAlive) {
				try {
					ExecutingThread.Abort();
				} catch(Exception) {}
				
				DebugLog.Add("Forcefully canceled LibraryTransaction");
				return;
			}
			
			DebugLog.Add("Peacefully canceled LibraryTransaction");
		}
		
		protected void UpdateAverageDuration(DateTime start)
		{
			long timeDiff = System.DateTime.Now.Ticks - start.Ticks;
			
			if(timeDiff > 0) {
				if(averageDuration == 0)
					averageDuration = timeDiff;
				else
					averageDuration = (averageDuration + timeDiff) / 2;
			}
		}
		
		protected virtual void CancelAction()
		{
		
		}
		
		public void Finish(object o)
		{
			EventHandler handler = Finished;
			if(handler != null) 
				handler(o, new EventArgs());
		}
		
		public long TotalCount {
			get {
				return totalCount;
			}
		}
		
		public long CurrentCount {
			get {
				return currentCount;
			}
		}
		
		public string StatusMessage {
			get {
				return statusMessage;
			}
		}
		
		public long AverageDuration {
			get {
				return averageDuration;
			}
		}
		
		public bool ShowStatus {
			get {
				return showStatus;
			}
		}
		
		public bool ShowCount {
			get {
				return showCount;
			}
		}
		
		public void Register()
		{
			Core.Library.TransactionManager.Register(this);
		}
	}
	
	// --- Implementing LibraryTransaction Classes
	
	public class HaveTrackInfoArgs : EventArgs
	{
		public TrackInfo TrackInfo;
	}

	public delegate void HaveTrackInfoHandler(object o, HaveTrackInfoArgs args);
	
	public class FileLoadTransaction : LibraryTransaction
	{
		private string path;
		private bool allowLibrary, preload;
		
		public event HaveTrackInfoHandler HaveTrackInfo;
		
		public override string Name {
			get {
				return Catalog.GetString("Library Track Loader");
			}
		}
		
		public FileLoadTransaction(string path) :
			this(path, true, true) {}
		
		public FileLoadTransaction(string path, bool allowLibrary) :
			this(path, allowLibrary, true) {}
		
		public FileLoadTransaction(string path, bool allowLibrary, 
			bool preload)
		{
			this.path = path;
			this.allowLibrary = allowLibrary;
			this.preload = preload;
		}
		
		public override void Run()
		{
			totalCount = 0;
			currentCount = 0;
			statusMessage = Catalog.GetString("Processing");
			
			AddMultipleFilesRaw(path);
		}
		
		private void AddMultipleFilesRaw(string rawData)
		{
			if(rawData == null)
				return;
			
			foreach(string uri in rawData.Split('\n')) {
				if(uri == null)
					continue;
					
				string file = StringUtil.UriToFileName(uri.Trim()).Trim();
				
				if(file.Length == 0)
					continue;
				
				if(preload) {
					statusMessage = Catalog.GetString("Preloading Files");
					
					if(File.Exists(file)) {
						totalCount++;
					} else if(Directory.Exists(file)) {
						DirectoryInfo di;
				
						try {
							di = new DirectoryInfo(file);
						} catch(Exception) {
							continue;
						}
			
						totalCount = FileCount(di);
					} 
				}
			
				AddFile(file);
				
				if(cancelRequested)
					return;
			}
		}
		
		private void AddFile(string file)
		{
			if(Directory.Exists(file) && RecurseDirectory(file))
					return;
				
			if(cancelRequested)
				return;
				
		    if(Path.GetFileName(file).StartsWith(".banshee-ipod-"))
		       return;
				
			DateTime startStamp = DateTime.Now;
			
			try {
				TrackInfo ti = new LibraryTrackInfo(file);
				
				bool copy = false;
				try {
					copy = (bool)Core.GconfClient.Get(GConfKeys.CopyOnImport);
				} catch(Exception) {}
				
				if(copy) {
					try {
						string destfile = FileNamePattern.BuildFull(ti, 
							Path.GetExtension(file).Substring(1));
						File.Copy(file, destfile, true);
						ti.Uri = destfile;
						ti.Save();
					} catch(Exception) { }
				}
			
				RaiseTrackInfo(ti);
				UpdateAverageDuration(startStamp);
			} catch(Exception e) {
				return;
			}
		}
		
		private void RaiseTrackInfo(TrackInfo ti)
		{
			statusMessage = String.Format(
				Catalog.GetString("Loading {0} - {1} ..."),
				ti.Artist, ti.Title);
			currentCount++;
			
			HaveTrackInfoHandler handler = HaveTrackInfo;
			if(handler != null) {
				HaveTrackInfoArgs args = new HaveTrackInfoArgs();
				args.TrackInfo = ti;
				handler(this, args);
			}
		}
		
		private bool RecurseDirectory(string path)
		{
			DirectoryInfo di;
			
			try {
				di = new DirectoryInfo(path);
			} catch(Exception) {
				return false;
			}
			
			if(!di.Exists)
				return false;
			
			foreach(DirectoryInfo sdi in di.GetDirectories()) {
				if(cancelRequested)
					return false;
					
				if(!sdi.Name.StartsWith(".")) 
					RecurseDirectory(path + "/" + sdi.Name);
			}
					
			foreach(FileInfo fi in di.GetFiles()) {
				if(cancelRequested)
					return false;
			
				AddFile(path + "/" + fi.Name);
			}
			
			return true;
		}
		
		private long FileCount(DirectoryInfo baseDirectory) 
	    {    
	        try { 
		        long count = baseDirectory.GetFiles().Length;

		        foreach(DirectoryInfo di in baseDirectory.GetDirectories()) {
		        	if(cancelRequested)
						return -1;
		            count += FileCount(di);
		         } 

		        return count;
		  	} catch(Exception) {
		  		return 0;
		  	}
	    }
	}
	
	public class PlaylistSaveTransaction : LibraryTransaction
	{	
		private Playlist pl;
	
		public override string Name 
		{
			get {
				return Catalog.GetString("Playlist Save");
			}
		}
		
		public PlaylistSaveTransaction(Playlist pl)
		{
			this.pl = pl;
		}
		
		public override void Run()
		{
			Statement query;
			int playlistId = Playlist.GetId(pl.name);
			
			statusMessage = Catalog.GetString("Flushing old entries");
			totalCount = pl.items.Count;
			currentCount = 0;

			if(playlistId == 0) {
				query = new Insert("Playlists", false, null, pl.name);
				Core.Library.Db.Execute(query);
				playlistId = Playlist.GetId(pl.name);
			}

			query = new Delete("PlaylistEntries") +
				new Where(new Compare("PlaylistID", Op.EqualTo, playlistId));
			Core.Library.Db.Execute(query);

			statusMessage = Catalog.GetString("Saving new entries");
			
			foreach(TrackInfo ti in pl.items) {
				if(cancelRequested)
					break;
			
				DateTime startStamp = DateTime.Now;
			
				if(ti.TrackId <= 0)
					continue;
					
				query = new Insert("PlaylistEntries", 
					false, null, playlistId, ti.TrackId);
					
				Core.Library.Db.Execute(query);
				
				UpdateAverageDuration(startStamp);
				currentCount++;
			}
		}
	}
	
	public class LibraryLoadTransaction : LibraryTransaction
	{
		public event HaveTrackInfoHandler HaveTrackInfo;
	
		public override string Name 
		{
			get {
				return Catalog.GetString("Library Load");
			}
		}
		
		public LibraryLoadTransaction()
		{
			showStatus = false;
		}
		
		public override void Run()
		{
			statusMessage = Catalog.GetString("Preloading Library");
			totalCount = Core.Library.Tracks.Count;
			currentCount = 0;
			
			lock(Core.Library.Tracks.SyncRoot) {
				foreach(TrackInfo track in Core.Library.Tracks.Values)
					RaiseTrackInfo(track);
			}
		}
		
		private void RaiseTrackInfo(TrackInfo ti)
		{
			statusMessage = String.Format(
				Catalog.GetString("Loading {0} - {1} ..."),
				ti.Artist, ti.Title);
			currentCount++;
			
			HaveTrackInfoHandler handler = HaveTrackInfo;
			if(handler != null) {
				HaveTrackInfoArgs args = new HaveTrackInfoArgs();
				args.TrackInfo = ti;
				handler(this, args);
			}
		}
	}
	
	abstract public class TrackRemoveTransaction : LibraryTransaction
	{
		public ArrayList RemoveQueue;
		
		public TrackRemoveTransaction()
		{
			RemoveQueue = new ArrayList();
			showStatus = true;
		}
	}
	
	public class LibraryTrackRemoveTransaction : TrackRemoveTransaction
	{
		public override string Name
		{
			get {
				return Catalog.GetString("Library Track Remove");
			}
		}
		
		public override void Run()
		{
			statusMessage = Catalog.GetString("Removing Tracks");
			totalCount = RemoveQueue.Count;
			currentCount = 0;
			
			Statement query = new Delete("Tracks") + new Where();
			
			for(int i = 0; i < totalCount; i++) {
				TrackInfo ti = RemoveQueue[i] as TrackInfo;
				query += new Compare("TrackID", Op.EqualTo, ti.TrackId);
				if(i < totalCount - 1)
					query += new Or();
				
				statusMessage = String.Format(
					Catalog.GetString("Removing {0} - {1}"),
					ti.Artist, ti.Title);
				currentCount++;
				Core.Library.Tracks.Remove(ti.TrackId);
			}
			
			statusMessage = Catalog.GetString("Purging Library of Removed Tracks...");
			currentCount = 0;
			totalCount = 0;
			Core.Library.Db.Execute(query);
		}
	}
	
	public class PlaylistTrackRemoveTransaction : TrackRemoveTransaction
	{
		private int id;
	
		public override string Name
		{
			get {
				return Catalog.GetString("Playlist Track Remove");
			}
		}
		
		public PlaylistTrackRemoveTransaction(int id)
		{
			this.id = id;
		}
		
		public override void Run()
		{
			statusMessage = Catalog.GetString("Removing Tracks");
			totalCount = RemoveQueue.Count;
			currentCount = 0;
			
			Statement query = new Delete("PlaylistEntries") + 
				new Where("PlaylistID", Op.EqualTo, id) + new And();
			Statement subquery = Statement.Empty;
			
			for(int i = 0; i < totalCount; i++) {
				TrackInfo ti = RemoveQueue[i] as TrackInfo;
				subquery += new Compare("TrackID", Op.EqualTo, ti.TrackId);
				if(i < totalCount - 1)
					subquery += new Or();
				
				statusMessage = String.Format(
					Catalog.GetString("Removing {0} - {1}"),
					ti.Artist, ti.Title);
				currentCount++;
			}
			
			query += new ParenGroup(subquery);

			statusMessage = Catalog.GetString("Purging Playlist of Removed Tracks...");
			currentCount = 0;
			totalCount = 0;
			Core.Library.Db.Execute(query);
		}
	}
	
	public class SqlLoadTransaction : LibraryTransaction
	{
		private string sql;
		
		public event HaveTrackInfoHandler HaveTrackInfo;
		
		public override string Name {
			get {
				return Catalog.GetString("Library Track Loader");
			}
		}
		
		public SqlLoadTransaction(string sql)
		{
			this.sql = sql;
		}
		
		public SqlLoadTransaction(Statement sql)
		{
			this.sql = sql.ToString();
		}
		
		public override void Run()
		{
			totalCount = 0;
			currentCount = 0;
			statusMessage = Catalog.GetString("Processing");
			AddSql();
		}
		
		private void RaiseTrackInfo(TrackInfo ti)
		{
			statusMessage = String.Format(
				Catalog.GetString("Loading {0} - {1} ..."),
				ti.Artist, ti.Title);
			currentCount++;
			
			HaveTrackInfoHandler handler = HaveTrackInfo;
			if(handler != null) {
				HaveTrackInfoArgs args = new HaveTrackInfoArgs();
				args.TrackInfo = ti;
				handler(this, args);
			}
		}
		
		private void AddSql()
		{
			totalCount = SqlCount();
			IDataReader reader = Core.Library.Db.Query(sql);
			while(reader.Read() && !cancelRequested) {
				DateTime startStamp = DateTime.Now;
				RaiseTrackInfo(new LibraryTrackInfo(reader));
				UpdateAverageDuration(startStamp);
			}
		}
		
		private long SqlCount()
		{
			string countQuery = Regex.Replace(sql,
				"SELECT (.*) FROM",
				"SELECT COUNT($1) FROM"); 
				
			long count;
			
			try {
				count = Convert.ToInt64(Core.Library.Db.QuerySingle(countQuery));
			} catch(Exception) {
				count = 0;
			}

			return count;
		}
	}
	
	public class TrackInfoSaveTransaction : LibraryTransaction
	{
		private ArrayList list;
		
		public override string Name {
			get {
				return "Track Save Transaction";
			}
		}
		
		public TrackInfoSaveTransaction(ArrayList list)
		{
			this.list = list;	
			showStatus = false;
		}
		
		public override void Run()
		{
			foreach(TrackInfo track in list) {
				if(cancelRequested)
					break;
					
				track.Save();
			}
		}
	}
	
	public class PlaylistLoadTransaction : LibraryTransaction
	{
		private string name;
		private int id;
		
		public event HaveTrackInfoHandler HaveTrackInfo;
		
		public override string Name {
			get {
				return Catalog.GetString("Playlist Track Loader");
			}
		}
		
		public PlaylistLoadTransaction(string name)
		{
			this.name = name;
		}
		
		public PlaylistLoadTransaction(Playlist pl)
		{
			this.name = pl.Name;
		}
		
		public override void Run()
		{
			totalCount = 0;
			currentCount = 0;
			statusMessage = Catalog.GetString("Processing");
			
			id = Playlist.GetId(name);
			if(id <= 0)
				return;
			
			totalCount = SqlCount();
			Statement query = new Select("PlaylistEntries", new List("TrackID")) 
				+ new Where("PlaylistId", Op.EqualTo, id);
			IDataReader reader = Core.Library.Db.Query(query);
			while(reader.Read() && !cancelRequested) {
				DateTime startStamp = DateTime.Now;
				int tid = Convert.ToInt32(reader[0]);
				TrackInfo ti = Core.Library.Tracks[tid] as TrackInfo;
				RaiseTrackInfo(ti);
				UpdateAverageDuration(startStamp);
			}
		}
		
		private void RaiseTrackInfo(TrackInfo ti)
		{
			statusMessage = String.Format(
				Catalog.GetString("Loading {0} - {1} ..."),
				ti.Artist, ti.Title);
			currentCount++;
			
			HaveTrackInfoHandler handler = HaveTrackInfo;
			if(handler != null) {
				HaveTrackInfoArgs args = new HaveTrackInfoArgs();
				args.TrackInfo = ti;
				handler(this, args);
			}
		}
		
		private long SqlCount()
		{
			Statement countQuery = new Select("PlaylistEntries", 
				new List("COUNT(*)")) 
				+ new Where("PlaylistID", Op.EqualTo, id);
				
			long count;
			
			try {
				count = Convert.ToInt64(Core.Library.Db.QuerySingle(countQuery));
			} catch(Exception) {
				count = 0;
			}

			return count;
		}	
	}
}
