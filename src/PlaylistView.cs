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
using Mono.Unix;
using Gtk;
using Gdk;
using Pango;

namespace Banshee
{
    public class LoaderAdditionArgs : EventArgs
    {
        public long Count;
    }
    
    public delegate void LoaderAdditionHandler(object o, 
        LoaderAdditionArgs args);
        
    public class PlaylistView : TreeView
    {
        private enum ColumnId : int {
            Track,
            Artist,
            Title,
            Album,
            Time,
            Rating,
            PlayCount,
            LastPlayed
        };
            
        private ArrayList columns;
        PlaylistModel model;
        
        PlaylistColumnChooserDialog columnChooser;
        Pixbuf nowPlayingPixbuf;
        Pixbuf syncNeededPixbuf;
        Pixbuf songDrmedPixbuf;
        Pixbuf ripColumnPixbuf;

        public TreeViewColumn SyncColumn;
        public TreeViewColumn RipColumn;

        public PlaylistView(PlaylistModel model)
        {        
            // set up columns
            columns = new ArrayList();
            
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Track"), "Track", 
                new TreeCellDataFunc(TrackCellTrack), new CellRendererText(),
                0, (int)ColumnId.Track));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Artist"), "Artist", 
                new TreeCellDataFunc(TrackCellArtist), new CellRendererText(),
                1, (int)ColumnId.Artist));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Title"), "Title", 
                new TreeCellDataFunc(TrackCellTitle), new CellRendererText(),
                2, (int)ColumnId.Title));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Album"), "Album", 
                new TreeCellDataFunc(TrackCellAlbum), new CellRendererText(),
                3, (int)ColumnId.Album));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Time"), "Time", 
                new TreeCellDataFunc(TrackCellTime), new CellRendererText(),
                4, (int)ColumnId.Time));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Rating"), "Rating", 
                new TreeCellDataFunc(TrackCellRating), new RatingRenderer(),
                5, (int)ColumnId.Rating));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Plays"), "Plays", 
                new TreeCellDataFunc(TrackCellPlayCount), 
                new CellRendererText(),
                6, (int)ColumnId.PlayCount));
            columns.Add(new PlaylistColumn(this, Catalog.GetString("Last Played"), "Last-Played", 
                new TreeCellDataFunc(TrackCellLastPlayed), 
                new CellRendererText(),
                7, (int)ColumnId.LastPlayed));
            
            foreach(PlaylistColumn plcol in columns) {
                InsertColumn(plcol.Column, plcol.Order);
            }

            TreeViewColumn playIndColumn = new TreeViewColumn();
            Gtk.Image playIndImg = new Gtk.Image(
                Gdk.Pixbuf.LoadFromResource("blue-speaker.png"));
            playIndImg.Show();
            playIndColumn.Expand = false;
            playIndColumn.Resizable = false;
            playIndColumn.Clickable = false;
            playIndColumn.Reorderable = false;
            playIndColumn.Widget = playIndImg;
            
            nowPlayingPixbuf = Gdk.Pixbuf.LoadFromResource("now-playing-arrow.png");
            syncNeededPixbuf = Gdk.Pixbuf.LoadFromResource("sync-needed.png");
            songDrmedPixbuf = Gdk.Pixbuf.LoadFromResource("song-drm.png");
            ripColumnPixbuf = Gdk.Pixbuf.LoadFromResource("cd-action-rip-16.png");
            
            CellRendererPixbuf indRenderer = new CellRendererPixbuf();
            playIndColumn.PackStart(indRenderer, true);
            playIndColumn.SetCellDataFunc(indRenderer, 
                new TreeCellDataFunc(TrackCellInd));
            InsertColumn(playIndColumn, 0);

            SyncColumn = new TreeViewColumn();
            Gtk.Image syncNeededImg = new Gtk.Image(syncNeededPixbuf);
            syncNeededImg.Show();
            SyncColumn.Expand = false;
            SyncColumn.Resizable = false;
            SyncColumn.Clickable = false;
            SyncColumn.Reorderable = false;
            SyncColumn.Visible = false;
            SyncColumn.Widget = syncNeededImg;
            
            CellRendererPixbuf syncRenderer = new CellRendererPixbuf();
            SyncColumn.PackStart(syncRenderer, true);
            SyncColumn.SetCellDataFunc(syncRenderer, 
                new TreeCellDataFunc(SyncCellInd));
            InsertColumn(SyncColumn, 1);
            
            RipColumn = new TreeViewColumn();
            Gtk.Image ripImage = new Gtk.Image(ripColumnPixbuf);
            ripImage.Show();
            RipColumn.Expand = false;
            RipColumn.Resizable = false;
            RipColumn.Clickable = false;
            RipColumn.Reorderable = false;
            RipColumn.Visible = false;
            RipColumn.Widget = ripImage;
            
            CellRendererToggle ripRenderer = new CellRendererToggle();
            ripRenderer.Activatable = true;
            ripRenderer.Toggled += OnRipToggled;
            RipColumn.PackStart(ripRenderer, true);
            RipColumn.SetCellDataFunc(ripRenderer, new TreeCellDataFunc(RipCellInd));
            InsertColumn(RipColumn, 1);

            ColumnDragFunction = new TreeViewColumnDropFunc(CheckColumnDrop);

            Model = this.model = model;
            model.DefaultSortFunc = new TreeIterCompareFunc(DefaultTreeIterCompareFunc);
                
            // set up tree view
            RulesHint = true;
            HeadersClickable = true;
            HeadersVisible = true;
            Selection.Mode = SelectionMode.Multiple;
            
            model.SetSortFunc((int)ColumnId.Track, 
                    new TreeIterCompareFunc(TrackTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.Artist, 
                new TreeIterCompareFunc(ArtistTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.Title, 
                new TreeIterCompareFunc(TitleTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.Album, 
                new TreeIterCompareFunc(AlbumTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.Time, 
                new TreeIterCompareFunc(TimeTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.Rating, 
                new TreeIterCompareFunc(RatingTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.PlayCount, 
                new TreeIterCompareFunc(PlayCountTreeIterCompareFunc));
            model.SetSortFunc((int)ColumnId.LastPlayed, 
                new TreeIterCompareFunc(LastPlayedTreeIterCompareFunc));
        }    

        private void OnRipToggled(object o, ToggledArgs args)
        {
            try {
                AudioCdTrackInfo ti = (AudioCdTrackInfo)model.PathTrackInfo(new TreePath(args.Path));
                CellRendererToggle renderer = (CellRendererToggle)o;
                ti.CanRip = !ti.CanRip;
                renderer.Active = ti.CanRip;
            } catch(Exception) {
            
            }   
        }

        private bool CheckColumnDrop(TreeView tree, TreeViewColumn col,
                                     TreeViewColumn prev, TreeViewColumn next)
        {
            // Don't allow moving other columns before the first column
            return prev != null;
        }

        private int StringFieldCompare(string a, string b)
        {
            if(a != null)
                a = a.ToLower();
                
            if(b != null)
                b = b.ToLower();
                
            return String.Compare(a, b);
        }
        
        private int LongFieldCompare(long a, long b)
        {
            return a < b ? -1 : (a == b ? 0 : 1);
        }
            
        public int TrackTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            return LongFieldCompare((long)model.IterTrackInfo(a).TrackNumber,
                (long)model.IterTrackInfo(b).TrackNumber);
        }
            
        public int ArtistTreeIterCompareFunc(TreeModel _model, TreeIter a, 
            TreeIter b)
        {
            return StringFieldCompare(model.IterTrackInfo(a).Artist, 
                model.IterTrackInfo(b).Artist);
        }
        
        public int TitleTreeIterCompareFunc(TreeModel _model, TreeIter a, 
            TreeIter b)
        {
            return StringFieldCompare(model.IterTrackInfo(a).Title, 
                model.IterTrackInfo(b).Title);
        }
        
        public int AlbumTreeIterCompareFunc(TreeModel _model, TreeIter a, 
            TreeIter b)
        {
            int v = StringFieldCompare(model.IterTrackInfo(a).Album, 
                model.IterTrackInfo(b).Album);

            if (v != 0)
                return v;

            return TrackTreeIterCompareFunc (_model, a, b);
        }
        
        public int TimeTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            return LongFieldCompare(model.IterTrackInfo(a).Duration,
                model.IterTrackInfo(b).Duration);
        }
        
        public int RatingTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            return LongFieldCompare((long)model.IterTrackInfo(a).Rating,
                (long)model.IterTrackInfo(b).Rating);
        }
        
        public int PlayCountTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            return LongFieldCompare((long)model.IterTrackInfo(a).NumberOfPlays,
                (long)model.IterTrackInfo(b).NumberOfPlays);
        }
        
        public int LastPlayedTreeIterCompareFunc(TreeModel _model, TreeIter a,
            TreeIter b)
        {
            return DateTime.Compare(model.IterTrackInfo(a).LastPlayed,
                model.IterTrackInfo(b).LastPlayed);
        }
        
        public int DefaultTreeIterCompareFunc(TreeModel _model, TreeIter a, 
            TreeIter b)
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
            renderer.Weight = iter.Equals(model.PlayingIter) 
                ? (int)Pango.Weight.Bold 
                : (int)Pango.Weight.Normal;
            
            renderer.Foreground = null;
            
            TrackInfo ti = model.IterTrackInfo(iter);
            if(ti.GetType() != typeof(IpodTrackInfo)) 
                return;
                
            if((ti as IpodTrackInfo).NeedSync)
                renderer.Foreground = "blue";
        }
        
        protected void TrackCellInd(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            CellRendererPixbuf renderer = (CellRendererPixbuf)cell;
            renderer.Pixbuf = iter.Equals(model.PlayingIter)
                ? nowPlayingPixbuf
                : null;
        }
        
        protected void RipCellInd(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            CellRendererToggle toggle = (CellRendererToggle)cell;
            AudioCdTrackInfo ti = model.IterTrackInfo(iter) as AudioCdTrackInfo;
            if (ti != null)
                toggle.Active = ti.CanRip;
            else
                toggle.Active = false;
        }
        
        protected void SyncCellInd(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            CellRendererPixbuf renderer = (CellRendererPixbuf)cell;
            
            if(model == null || model.Source == null) {
                renderer.Pixbuf = null;
                return;
            }
            
            if(model.Source.GetType() != typeof(IpodSource)) {
                renderer.Pixbuf = null;
                return; 
            }
            
            TrackInfo ti = model.IterTrackInfo(iter);
            
            if(ti.GetType() != typeof(IpodTrackInfo)) {
                renderer.Pixbuf = null;
                return;
            }
            
            IpodTrackInfo iti = ti as IpodTrackInfo;
            renderer.Pixbuf = iti.NeedSync
                ? syncNeededPixbuf
                : (iti.CanPlay ? null : songDrmedPixbuf); 
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
                Track.Duration < 0 ? Catalog.GetString("N/A") : 
                String.Format("{0}:{1}", Track.Duration / 60, 
                (Track.Duration % 60).ToString("00")), iter);
        }
        
        protected void TrackCellPlayCount(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            uint plays = model.IterTrackInfo(iter).NumberOfPlays;
            SetRendererAttributes((CellRendererText)cell, 
                plays > 0 ? Convert.ToString(plays) : "", 
                iter);
        }
        
        protected void TrackCellRating(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {            
            ((RatingRenderer)cell).Track = model.IterTrackInfo(iter);
        }
        
        protected void TrackCellLastPlayed(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            DateTime lastPlayed = model.IterTrackInfo(iter).LastPlayed;
            
            string disp = String.Empty;
            
            if(lastPlayed > DateTime.MinValue)
                disp = lastPlayed.ToString();
            
            SetRendererAttributes((CellRendererText)cell, 
                String.Format("{0}", disp), iter);
        }
        
        public void PlayPath(TreePath path)
        {
            model.PlayPath(path);
            QueueDraw();
            ScrollToPlaying();
        }
        
        public void UpdateView()
        {
            QueueDraw();
            ScrollToPlaying();
        }
        
        public void ThreadedQueueDraw()
        {
            Application.Invoke(delegate {
                QueueDraw();
            });
        }

        public void ScrollToPlaying()
        {
            Gdk.Rectangle cellRect = GetCellArea (model.PlayingPath, Columns[0]);

            Point point = new Point ();
            WidgetToTreeCoords (cellRect.Left, cellRect.Top, out point.X, out point.Y);
            cellRect.Location = point;

            // we only care about vertical bounds
            if (cellRect.Location.Y < VisibleRect.Location.Y ||
                cellRect.Location.Y + cellRect.Size.Height > VisibleRect.Location.Y + VisibleRect.Size.Height) {
                ScrollToCell(model.PlayingPath, null, true, 0.5f, 0.0f);
            }
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
                //    playingPath = null;
                //    QueueDraw();
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
        
        
        public bool PlaySelected()
        {
            TreeIter selIter;
            
            try {
                if(!model.GetIter(out selIter, Selection.GetSelectedRows()[0]))
                    return false;
            } catch(Exception) {
                return false;
            }
            
            model.PlayIter(selIter);
            return true;
        }
        
        public void AddSelectedToPlayList(string name)
        {
            Playlist pl = new Playlist(name);
            pl.Load();
        
            foreach(TreePath p in Selection.GetSelectedRows()) {
                TrackInfo ti = model.PathTrackInfo(p);
                if(ti == null)
                    continue;
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
        
        public TrackInfo [] SelectedTrackInfoMultiple {
            get {
                if(Selection.CountSelectedRows() == 0)
                    return null;
                
                ArrayList list = new ArrayList();
                
                foreach(TreePath path in Selection.GetSelectedRows())
                    list.Add(model.PathTrackInfo(path));
                
                return list.ToArray(typeof(TrackInfo)) as TrackInfo [];
            }
        }

        protected override void OnDragBegin(Gdk.DragContext ctx)
        {
            // This is just here to block GtkTreeView's
            // implementation, which would use an image of the row
            // being dragged as the drag icon, which we don't want
            // since we might actually be dragging multiple rows.
        }

        protected override bool OnDragMotion(Gdk.DragContext context, int x, int y, uint time)
        {
            if (!base.OnDragMotion(context, x, y, time))
                return false;

            // Force the drag highlight to be either before or after a
            // row, not on top of one.

            TreePath path;
            TreeViewDropPosition pos;
            if(GetDestRowAtPos(x, y, out path, out pos))
                SetDragDestRow(path, (TreeViewDropPosition)((int)pos & 0x1));

            return true;
        }
    }
}
