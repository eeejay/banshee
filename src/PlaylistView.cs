/***************************************************************************
 *  PlaylistView.cs
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
using System.Threading;
using System.Collections;
using System.Runtime.InteropServices;
using System.IO;
using Gtk;
using Gdk;
using Pango;

namespace Sonance
{
	public class LoaderAdditionArgs : EventArgs
	{
		public long Count;
	}
	
	public delegate void LoaderAdditionHandler(object o, 
		LoaderAdditionArgs args);
		
	public class PlaylistView : TreeView
	{
		private ArrayList columns;
		PlaylistModel model;
		
		PlaylistColumnChooserDialog columnChooser;

		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(PlaylistView));
				return gtype;
			}
		}

		public PlaylistView(PlaylistModel model)
		{		
			// set up columns
			columns = new ArrayList();
			
			columns.Add(new PlaylistColumn(this, "Track", 
				new TreeCellDataFunc(TrackCellTrack), 0));
			columns.Add(new PlaylistColumn(this, "Artist", 
				new TreeCellDataFunc(TrackCellArtist), 1));
			columns.Add(new PlaylistColumn(this, "Title", 
				new TreeCellDataFunc(TrackCellTitle), 2));
			columns.Add(new PlaylistColumn(this, "Album", 
				new TreeCellDataFunc(TrackCellAlbum), 3));
			columns.Add(new PlaylistColumn(this, "Time", 
				new TreeCellDataFunc(TrackCellTime), 4));
			columns.Add(new PlaylistColumn(this, "Rating", 
				new TreeCellDataFunc(TrackCellRating), 5));
			columns.Add(new PlaylistColumn(this, "Play Count", 
				new TreeCellDataFunc(TrackCellPlayCount), 6));

			foreach(PlaylistColumn plcol in columns) {
				InsertColumn(plcol.Column, plcol.Order);
				plcol.Column.Clicked += OnColumnClicked;
			}

			Model = this.model = model;
			model.DefaultSortFunc =
				new TreeIterCompareFunc(DefaultTreeIterCompareFunc);
				
			// set up tree view
			RulesHint = true;
			HeadersClickable = true;
			HeadersVisible = true;
			Selection.Mode = SelectionMode.Multiple;		
		}	
			
		public int DefaultTreeIterCompareFunc(TreeModel model, TreeIter a, TreeIter b)
		{
			
			
			return 0;	
		}
			
		public TrackInfo IterTrackInfo(TreeIter iter)
		{
			return Model.GetValue(iter, 0) as TrackInfo;
		}
		
		private void SaveColumns()
		{
			foreach(PlaylistColumn plcol in columns)
				plcol.Save(Columns);
		}
		
		public void ColumnChooser()
		{
			columnChooser = new PlaylistColumnChooserDialog(columns);
			columnChooser.ShowAll();
		}
		
		public void Shutdown()
		{
			SaveColumns();
		}
					
		protected void SetRendererAttributes(CellRendererText renderer, 
			string text, TreeIter iter)
		{
			renderer.Text = text;
			renderer.Weight = model.PlayingPath != null 
				&& model.GetPath(iter).Compare(model.PlayingPath) == 0 
				? (int)Pango.Weight.Bold 
				: (int)Pango.Weight.Normal;
		}
		
		protected void TrackCellTrack(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SetRendererAttributes((CellRendererText)cell, 
				Convert.ToString(model.IterTrackInfo(iter).TrackNumber), iter);
		}	
		
		protected void TrackCellArtist(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SetRendererAttributes((CellRendererText)cell, 
				model.IterTrackInfo(iter).Artist, iter);
		}
		
		protected void TrackCellTitle(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SetRendererAttributes((CellRendererText)cell, 
				model.IterTrackInfo(iter).Title, iter);
		}
		
		protected void TrackCellAlbum(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SetRendererAttributes((CellRendererText)cell, 
				model.IterTrackInfo(iter).Album, iter);
		}
		
		protected void TrackCellTime(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			TrackInfo Track = model.IterTrackInfo(iter);
			SetRendererAttributes((CellRendererText)cell, 
				Track.Duration < 0 ? "N/A" : 
				String.Format("{0}:{1}", Track.Duration / 60, 
				(Track.Duration % 60).ToString("00")), iter);
		}
		
		protected void TrackCellPlayCount(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SetRendererAttributes((CellRendererText)cell, 
				String.Format("{0}", model.IterTrackInfo(iter).NumberOfPlays), 
				iter);
		}
		
		protected void TrackCellRating(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SetRendererAttributes((CellRendererText)cell, 
				String.Format("{0}", model.IterTrackInfo(iter).Rating), 
				iter);
		}
		
		private void OnColumnClicked(object o, EventArgs args)
		{
			TreeViewColumn column = o as TreeViewColumn;
			
			foreach(PlaylistColumn plcol in columns) {
				if(plcol.Column != column)
					plcol.Column.SortIndicator = false;
			}
			
			column.SortIndicator = true;
			column.SortOrder = column.SortOrder == SortType.Ascending ? SortType.Descending : SortType.Ascending;
			model.SetSortColumnId(column.SortColumnId, column.SortOrder);
		}
		
		public void PlayPath(TreePath path)
		{
			model.PlayPath(path);
			QueueDraw();
			ScrollToCell(model.PlayingPath, null, true, 0.5f, 0.0f);
		}
		
		public void UpdateView()
		{
			QueueDraw();
			ScrollToCell(model.PlayingPath, null, true, 0.5f, 0.5f);
		}
		
		//QueueDraw();
		//ScrollToCell(playingPath, null, true, 0.5f, 0.0f);

		/*
		private void PlayRandomIter()
		{
			if(playedUids.Count == Count()) {
				playedUids.Clear();
				if(!repeat) {
					return;
				}
			}
				
			while(true) {
				TreeIter randIter;
				int randIndex = Core.Instance.Random.Next(0, Count());
				if(!store.IterNthChild(out randIter, randIndex))
					continue;
				
				TrackInfo rti = IterTrackInfo(randIter);
				
				if(playedUids.IndexOf(rti.Uid) == -1) {
					PlayIter(randIter);
					return;
				}
			}
		}
		
		private TrackInfo PlayingTrackInfo()
		{
			if(playingPath != null) {
				TreeIter playingIter;
				if(store.GetIter(out playingIter, playingPath))
					return IterTrackInfo(playingIter);
			}
			
			return null;
		}	
		
		private TreePath TrackInfoPath(TrackInfo ti)
		{
			TreeIter iter;
			
			for(int i = 0, n = Count(); i < n; i++) {
				store.IterNthChild(out iter, i);
				
				if(IterTrackInfo(iter).Uid == ti.Uid) 
					return store.GetPath(iter);
			}
			
			return null;
		}
			
*/
		/*private int FindIndex(TreePath a)
		{
			TreeIter iter;
			TreePath b;
			int i, n;
	
			for(i = 0, n = Count(); i < n; i++) {
				store.IterNthChild(out iter, i);
				b = store.GetPath(iter);
				if(a.Compare(b) == 0) 
					return i;
			}
	
			return -1;
		}
		
		public void Advance()
		{
			ChangeDirection(true);	
		}

		public void Regress()
		{
			ChangeDirection(false);	
		}

		public void ContinuePlay()
		{
			Advance();
		}
		
		private void ChangeDirection(bool forward)
		{
			TreePath path = playingPath;
			TreeIter nextIter;
			bool success = true;
			int count, index;

			if(shuffle) {
				PlayRandomIter();
				return;
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
				if(store.IterNthChild(out nextIter, 0))
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
			
			if(!store.GetIter(out nextIter, path))
				return;
			
			PlayIter(nextIter);
		}
		
		private void ResetPlayingPath(TrackInfo cti)
		{
			TreeIter iter;
			TrackInfo ti;
			int i, n;
	
			for(i = 0, n = Count(); i < n; i++) {
				store.IterNthChild(out iter, i);
				ti = IterTrackInfo(iter);
				if(ti.Uid == cti.Uid) {
					playingPath = store.GetPath(iter);
					QueueDraw();
					break;
				}
			}
		}
		
		public int Count()
		{
			return store.IterNChildren();
		}
		
		public void RemoveSelected()
		{
			TreePath [] paths = Selection.GetSelectedRows();
			TreeIter [] iters = new TreeIter[paths.Length];
			
			for(int i = 0; i < iters.Length; i++) {
				if(!store.GetIter(out iters[i], paths[i] as TreePath))
					return;
			}
		
			for(int i = 0; i < iters.Length; i++) {
				TreeIter playingIter, iter = (TreeIter)iters[i];
				TrackInfo ti = null;
				
				if(playingPath != null && 
					store.GetIter(out playingIter, playingPath)) {
					if(playingPath.Compare(store.GetPath(iter)) == 0) 
						playingPath = null;
					else
						ti = IterTrackInfo(playingIter);
				}
				
				try {
					totalDuration -= IterTrackInfo(iter).Duration;
				} catch(Exception) {
				}
				
				store.Remove(ref iter);
				
				if(Updated != null)
					Updated(this, new EventArgs());
					
				if(ti != null)
					ResetPlayingPath(ti);
			}
		}
		*/
		
		
		public void PlaySelected()
		{
			TreeIter selIter;
			
			try {
				if(!model.GetIter(out selIter, Selection.GetSelectedRows()[0]))
					return;
			} catch(Exception) {
				return;
			}
			
			model.PlayIter(selIter);
		}
		
		public void AddSelectedToPlayList(string name)
		{
			Playlist pl = new Playlist(name);
		
			foreach(TreePath p in Selection.GetSelectedRows()) {
				TreeIter iter;
				if(!model.GetIter(out iter, p))
					continue;
					
				TrackInfo ti = IterTrackInfo(iter);
				pl.Append(ti);
			}
			
			pl.Save();
		}
		
		public TrackInfo SelectedTrackInfo {
			get {
				TreePath [] paths = Selection.GetSelectedRows();
				TreeIter selIter;
				
				if(paths.Length <= 0)
					return null;
				
				if(!model.GetIter(out selIter, paths[0]))
					return null;
					
				return model.IterTrackInfo(selIter);
			}
		}
	}
}
