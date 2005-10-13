/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlaylistModel.cs
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
using Gtk;
using Sql;

namespace Banshee
{
	public class PlaylistModel : ListStore, IPlaybackModel
	{
		private static int uid;
		private long totalDuration = 0;
		
		private ArrayList trackInfoQueue;
		private bool trackInfoQueueLocked = false;
		private TreeIter playingIter;
		
		private bool repeat = false;
		private bool shuffle = false;
		
		private bool canUpdate;
		private FileLoadTransaction importTransaction;
		
		public Source Source;
		
		public event EventHandler Updated;
		
		public static int NextUid
		{
			get {
				return uid++;
			}
		}
		
		public PlaylistModel() : base(typeof(TrackInfo))
		{
			trackInfoQueue = new ArrayList();
			GLib.Timeout.Add(300, new GLib.TimeoutHandler(OnIdle));
			canUpdate = true;
		}
	
		private void SyncPlayingIter()
		{
			if(Core.Instance.Player.Track == null) {
				playingIter = TreeIter.Zero;
			} else {
				for(int i = 0, n = Count(); i < n; i++) {
					TreeIter iter;
					if(!IterNthChild(out iter, i))
						continue;
						
					TrackInfo ti = IterTrackInfo(iter);
					
					if(Core.Instance.Player.Track.Uid == ti.Uid) {
						playingIter = iter;
						break;
					}
				}
			}
		}
	
		// --- Load Queue and Additions ---
	
		private bool OnIdle()
		{
			QueueSync();
			return true;
		}

		private void QueueSync()
		{
			if(trackInfoQueue.Count <= 0)
				return;
			
			trackInfoQueueLocked = true;
				
			foreach(TrackInfo ti in trackInfoQueue)
				AddTrack(ti);

			trackInfoQueue.Clear();
			trackInfoQueueLocked = false;
			SyncPlayingIter();
			
			return;
		}
			
		public void QueueAddTrack(TrackInfo ti)
		{
			while(trackInfoQueueLocked);
			trackInfoQueue.Add(ti);
		}

		private void OnLoaderHaveTrackInfo(object o, HaveTrackInfoArgs args)
		{
			if(o == importTransaction && !canUpdate)
				return;
				
			QueueAddTrack(args.TrackInfo);
		}

		public void AddTrack(TrackInfo ti)
		{
			if(ti == null)
				return;

			totalDuration += ti.Duration;

			Core.ThreadEnter();
			AppendValues(ti);
			Core.ThreadLeave();
			
			RaiseUpdated(this, new EventArgs());
		}
		
		public void AddFile(string path)
		{
			importTransaction = new FileLoadTransaction(path);
			importTransaction.HaveTrackInfo += OnLoaderHaveTrackInfo;
			importTransaction.Register();	
		}
		
		public void AddSql(object query)
		{
			SqlLoadTransaction loader = 
				new SqlLoadTransaction(query.ToString());
			loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
			Core.Library.TransactionManager.Register(loader);
		}
		
		public void LoadFromPlaylist(string name)
		{
			ClearModel();
			PlaylistLoadTransaction loader = new PlaylistLoadTransaction(name);
			loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
			Core.Library.TransactionManager.Register(loader);
		}
		
		public void LoadFromLibrary()
		{
			ClearModel();
			/*LibraryLoadTransaction loader = new LibraryLoadTransaction();
			loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
			loader.Register();*/
			
			foreach(TrackInfo ti in Core.Library.Tracks.Values)
				AddTrack(ti);
		}
		
		public void LoadFromIpodSource(IpodSource ipodSource)
		{
			ClearModel();
			
			foreach(IpodTrackInfo track in ipodSource.Tracks) {
				AddTrack(track);
			}
			
			SyncPlayingIter();
		}
		
		public void LoadFromAudioCdSource(AudioCdSource cdSource)
		{	
			ClearModel();
			
			AudioCdDisk disk = cdSource.Disk;
			
			foreach(AudioCdTrackInfo track in disk.Tracks) {
				AddTrack(track);
			}
			
			SyncPlayingIter();
		}

		// --- Helper Methods ---
		
		public TrackInfo IterTrackInfo(TreeIter iter)
		{
		    object o = GetValue(iter, 0);
		    if(o != null) {
		      return o as TrackInfo;
		    }
		    
		    return null;
		}
		
		public TrackInfo PathTrackInfo(TreePath path)
		{
			TreeIter iter;
			
			if(!GetIter(out iter, path))
				return null;
				
			return IterTrackInfo(iter);
		}
		
		// --- Playback Methods ---
		
		public void PlayPath(TreePath path)
		{
			TrackInfo ti = PathTrackInfo(path);
			if(ti == null)
				return;
				
			Core.Instance.PlayerInterface.PlayFile(ti);
			GetIter(out playingIter, path);
		}
		
		public void PlayIter(TreeIter iter)
		{
			TrackInfo ti = IterTrackInfo(iter);
			if(ti == null)
				return;
				
			if(ti.CanPlay) {
				Core.Instance.PlayerInterface.PlayFile(ti);
				playingIter = iter;
			} else {
				playingIter = iter;
				Continue();
			}
		}
		
		// --- IPlaybackModel 
		
		public void PlayPause()
		{
		
		}
		
		public void Advance()
		{
			ChangeDirection(true);
		}

		public void Regress()
		{
			ChangeDirection(false);	
		}

		public void Continue()
		{
			Advance();
		}
		
		private void ChangeDirection(bool forward)
		{
			// TODO: Implement random playback without repeating a track 
			// until all Tracks have been played first (see Legacy Sonance)
			
			TreePath currentPath;
			TreeIter currentIter, nextIter = TreeIter.Zero;
			TrackInfo currentTrack = null, nextTrack;
			
			try {
				currentPath = GetPath(playingIter);
			} catch(NullReferenceException) {
				currentPath = null;
			}
			
			if(currentPath == null) {
				if(GetIterFirst(out nextIter))
					PlayIter(nextIter);
				return;
			}
		
			int count = Count();
			int index = FindIndex(currentPath);
			bool lastTrack = index == count - 1;
		
			if(count <= 0 || index >= count || index < 0)
				return;
				
			currentTrack = PathTrackInfo(currentPath);
			currentIter = playingIter;
		
			if(forward) {
				if(lastTrack && repeat) {
					if(!IterNthChild(out nextIter, 0))
						return;
				} else if(shuffle) {
					int randIndex = Core.Instance.Random.Next(0, Count());
                	if(!IterNthChild(out nextIter, randIndex))
                    	return;
				} else {				
					currentPath.Next();				
					if(!GetIter(out nextIter, currentPath))
						return;
				}
				
				nextTrack = IterTrackInfo(nextIter);
				nextTrack.PreviousTrack = currentIter;
			} else {
				if(currentTrack.PreviousTrack.Equals(TreeIter.Zero)) {
					if(index > 0 && currentPath.Prev()) {
						if(!GetIter(out nextIter, currentPath)) {
							return;
						}
					} else {
						return;
					}
				} else {
					nextIter = currentTrack.PreviousTrack;
				}
			}
			
			if(!nextIter.Equals(TreeIter.Zero))
				PlayIter(nextIter);
		}

		public int Count()
		{
			return IterNChildren();
		}
		
		private int FindIndex(TreePath a)
		{
			TreeIter iter;
			TreePath b;
			int i, n;
	
			for(i = 0, n = Count(); i < n; i++) {
				IterNthChild(out iter, i);
				b = GetPath(iter);
				if(a.Compare(b) == 0) 
					return i;
			}
	
			return -1;
		}
		
		public void ClearModel()
		{
			Core.Library.TransactionManager.Cancel(typeof(FileLoadTransaction));
			trackInfoQueue.Clear();
		
			totalDuration = 0;
			playingIter = TreeIter.Zero;
			Clear();
				
			if(Updated != null && 
				Core.Instance.MainThread.Equals(Thread.CurrentThread))
				Updated(this, new EventArgs());
		}
		
		/*public void RemoveTrack(TreePath path)
		{
			TrackInfo ti = PathTrackInfo(path);
			TreeIter iter;
			
			if(!GetIter(out iter, path))
				return;
				
			if(Source.Type == SourceType.Playlist 
				|| Source.Type == SourceType.Library) {
				Statement query = new Delete("PlaylistEntries")
					+ new Where("TrackID", Op.EqualTo, ti.TrackId);
				try {
					Core.Library.Db.Execute(query);
				} catch(Exception) {}
				
				
				if(Source.Type == SourceType.Library) {
					Statement query2 = new Delete("Tracks")
						+ new Where("TrackID", Op.EqualTo, ti.TrackId);
					try {
						Core.Library.Db.Execute(query2);
					} catch(Exception) {}
				}
			}
			
			RemoveTrack(ref iter, ti);
		}
		
		private void RemoveTrack(ref TreeIter iter, TrackInfo ti)
		{
			randomQueue.Remove(iter);
			Remove(ref iter);
		}
		*/
		
		public void RemoveTrack(ref TreeIter iter)
		{
			TrackInfo ti = IterTrackInfo(iter);
			totalDuration -= ti.Duration;
			Remove(ref iter);
			RaiseUpdated(this, new EventArgs());
		}
		
		// --- Event Raise Handlers ---

		private void RaiseUpdated(object o, EventArgs args)
		{
			EventHandler handler = Updated;
			if(handler != null)
				handler(o, args);
		}
		
		public long TotalDuration 
		{
			get {
				return totalDuration;
			}
		}
		
		public TreePath PlayingPath
		{
			get {
				try {
					return playingIter.Equals(TreeIter.Zero) 
						? null : GetPath(playingIter);
				} catch(NullReferenceException) {
					return null;
				}
			}
		}
		
		public TreeIter PlayingIter
		{
			get {
				return playingIter;
			}
		}
		
		public bool Repeat {
			set {
				repeat = value;
			}
			
			get {
				return repeat;
			}
		}
		
		public bool Shuffle {
			set {
				shuffle = value;
			}
			
			get {
				return shuffle;
			}
		}
		
		public bool ImportCanUpdate {
			set {
				canUpdate = value;
			} 
			
			get {
				return canUpdate;
			}
		}
		
		public TrackInfo FirstTrack {
		  get {
		      TreeIter iter = TreeIter.Zero;
		      if(GetIterFirst(out iter) && !iter.Equals(TreeIter.Zero))
		          return IterTrackInfo(iter);
		      return null;
		  }
	   }
	}
}
