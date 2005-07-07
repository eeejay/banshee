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

namespace Sonance
{
	public class PlaylistModel : ListStore, IPlaybackModel
	{
		private static int uid;
		private long totalDuration = 0;
		
		private ArrayList playHistory;
		private int historyPosition = 0;
		
		private ArrayList randomQueue;
		private int randomIndex;
		private TreeIter lastNonRandomIter;
		
		private ArrayList trackInfoQueue;
		private bool trackInfoQueueLocked = false;
		private TreeIter playingIter;
		
		private bool repeat = false;
		private bool shuffle = false;
		
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
			randomQueue = new ArrayList();
			playHistory = new ArrayList();
			GLib.Timeout.Add(300, new GLib.TimeoutHandler(OnIdle));
		}
	
	    // -- Egg.TreeMultiDragSource Implementations
		
		public bool DragDataDelete(GLib.List path_list)
		{
			Console.WriteLine("DragDataDelete");
			return true;
		}
		
        public bool RowDraggable(GLib.List path_list)
		{
			Console.WriteLine("RowDraggable");
			return true;
		}
        
		public bool DragDataGet(GLib.List path_list, 
			Gtk.SelectionData selection_data)
		{
			Console.WriteLine("DragDataGet");
			return true;
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
			
			return;
		}
			
		public void QueueAddTrack(TrackInfo ti)
		{
			while(trackInfoQueueLocked);
			trackInfoQueue.Add(ti);
		}

		private void OnLoaderHaveTrackInfo(object o, HaveTrackInfoArgs args)
		{
			QueueAddTrack(args.TrackInfo);
		}

		public void AddTrack(TrackInfo ti)
		{
			if(ti == null)
				return;

			totalDuration += ti.Duration;

			Core.ThreadEnter();
			TreeIter iter = AppendValues(ti);
			Core.ThreadLeave();
			
			// random insertion into the random queue
			randomQueue.Insert(Core.Instance.Random.Next(randomQueue.Count), iter);
			
			RaiseUpdated(this, new EventArgs());
		}
		
		public void AddFile(string path)
		{
			FileLoadTransaction loader = new FileLoadTransaction(path);
			loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
			Core.Library.TransactionManager.Register(loader);	
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
			LibraryLoadTransaction loader = new LibraryLoadTransaction();
			loader.HaveTrackInfo += OnLoaderHaveTrackInfo;
			Core.Library.TransactionManager.Register(loader);
		}
		
		// --- Helper Methods ---
		
		public TrackInfo IterTrackInfo(TreeIter iter)
		{
			return GetValue(iter, 0) as TrackInfo;
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
				
			Core.Instance.PlayerInterface.PlayFile(ti);
			playingIter = iter;
			
			if(historyPosition >= playHistory.Count) {
				playHistory.Add(iter);
				historyPosition = playHistory.Count;
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
			TreePath path = GetPath(playingIter);
			TreeIter nextIter;
			bool success = true;
			int count, index;
			
			if(shuffle) {
				if(ChangeDirectionRandom(forward))
					return;
				else if(!lastNonRandomIter.Equals(TreeIter.Zero)) {
					PlayIter(lastNonRandomIter);
					return;
				}
			}
			
			if(path == null)
				return;
			
			count = Count();
			index = FindIndex(path);
			
			if(count <= 0 || index < 0 || index >= count)
				return;

			if(forward && index < count - 1) 
				path.Next();
			else if(forward && repeat) {
				if(IterNthChild(out nextIter, 0))
					PlayIter(nextIter);
				//else {
				//	playingPath = null;
				//	QueueDraw();
				//}
									
				return;
			} else if(forward) {
				//playingPath = null; 
				//QueueDraw();
				return;
			}
			
			if(!forward && index > 0)
				success = path.Prev();
			else if(!forward)
				return;
			
			if(path == null || !success)
				return;
			
			if(!GetIter(out nextIter, path))
				return;
			
			PlayIter(nextIter);
		}	
		
		private bool ChangeDirectionRandom(bool forward)
		{
			int count = randomQueue.Count;
			int nextIndex = 0;
			
			if(count <= 0 || randomIndex >= count)
				return false;
				
			if(forward && randomIndex < count - 1) 
				nextIndex = randomIndex + 1;
			else if(forward && repeat) {
				nextIndex = 0;
			} else if(forward) {
				return false;
			}
			
			if(!forward && randomIndex > 0)
				nextIndex = randomIndex - 1;
			else if(!forward)
				return false;
			
			try {
				TreeIter iter = (TreeIter)randomQueue[nextIndex];
				randomIndex = nextIndex;
				PlayIter(iter);
			} catch(Exception) {
				return false;
			}
			
			return true;
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
			randomQueue.Remove(iter);
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
				return playingIter.Equals(TreeIter.Zero) 
					? null : GetPath(playingIter);
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
				lastNonRandomIter = playingIter;
			}
			
			get {
				return shuffle;
			}
		}
	}
}
