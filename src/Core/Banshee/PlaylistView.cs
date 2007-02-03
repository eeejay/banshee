/***************************************************************************
 *  PlaylistView.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Collections.Generic;

using Gtk;

using Banshee.Base;
using Banshee.Dap;
using Banshee.Sources;
using Banshee.TrackView.Columns;

namespace Banshee
{
    public class LoaderAdditionArgs : EventArgs
    {
        public long Count;
    }
    
    public delegate void LoaderAdditionHandler(object o, LoaderAdditionArgs args);
        
    public class PlaylistView : TreeView, IDisposable
    {
        private List<TrackViewColumn> columns;
        private PlaylistModel model;
       
        private Gdk.Pixbuf ripping_pixbuf;
        private Gdk.Pixbuf now_playing_pixbuf;
        private Gdk.Pixbuf drm_pixbuf;
        private Gdk.Pixbuf resource_not_found_pixbuf;
        private Gdk.Pixbuf unknown_error_pixbuf;

        public TreeViewColumn RipColumn;

        protected PlaylistView(IntPtr ptr) : base(ptr)
        {
        }

        public PlaylistView(PlaylistModel model)
        {        
            Model = this.model = model;
            
            // Load pixbufs
            drm_pixbuf = IconThemeUtils.LoadIcon(16, "emblem-readonly", "emblem-important", Stock.DialogError);
            resource_not_found_pixbuf = IconThemeUtils.LoadIcon(16, "emblem-unreadable", Stock.DialogError);
            unknown_error_pixbuf = IconThemeUtils.LoadIcon(16, "dialog-error", Stock.DialogError);
            ripping_pixbuf = Gdk.Pixbuf.LoadFromResource("cd-action-rip-16.png");
            now_playing_pixbuf = IconThemeUtils.LoadIcon(16, "media-playback-start", 
                Stock.MediaPlay, "now-playing-arrow");
                
            // Load configurable columns
            columns = new List<TrackViewColumn>();            
            columns.Add(new TrackNumberColumn());
            columns.Add(new ArtistColumn());
            columns.Add(new TitleColumn());
            columns.Add(new AlbumColumn());
            columns.Add(new GenreColumn());
            columns.Add(new YearColumn());
            columns.Add(new DurationColumn());
            columns.Add(new LastPlayedColumn());
            columns.Add(new PlayCountColumn());
            columns.Add(new RatingColumn());
            columns.Add(new UriColumn());
            columns.Sort();
            
            foreach(TrackViewColumn column in columns) {
                column.Model = model;
                AppendColumn(column);
                column.CreatePopupableHeader();
            }

            // Create static columns
            TreeViewColumn status_column = new TreeViewColumn();
            CellRendererPixbuf status_renderer = new CellRendererPixbuf();
            status_column.Expand = false;
            status_column.Resizable = false;
            status_column.Clickable = false;
            status_column.Reorderable = false;
            status_column.Widget = new Image(IconThemeUtils.LoadIcon(16, "audio-volume-high", "blue-speaker"));
            status_column.Widget.Show();
            status_column.PackStart(status_renderer, true);
            status_column.SetCellDataFunc(status_renderer, new TreeCellDataFunc(StatusColumnDataHandler));
            InsertColumn(status_column, 0);
            
            CellRendererToggle rip_renderer = new CellRendererToggle();
            rip_renderer.Activatable = true;
            rip_renderer.Toggled += OnRipToggled;
            
            RipColumn = new TreeViewColumn();
            RipColumn.Expand = false;
            RipColumn.Resizable = false;
            RipColumn.Clickable = false;
            RipColumn.Reorderable = false;
            RipColumn.Visible = false;
            RipColumn.Widget = new Gtk.Image(ripping_pixbuf);
            RipColumn.Widget.Show();
            RipColumn.PackStart(rip_renderer, true);
            RipColumn.SetCellDataFunc(rip_renderer, new TreeCellDataFunc(RipColumnDataHandler));
            InsertColumn(RipColumn, 1);
            
            TreeViewColumn void_hack_column = new TreeViewColumn();
            void_hack_column.Expand = false;
            void_hack_column.Resizable = false;
            void_hack_column.Clickable = false;
            void_hack_column.Reorderable = false;
            void_hack_column.FixedWidth = 1;
            AppendColumn(void_hack_column);
            
            // set up tree view
            RulesHint = true;
            HeadersClickable = true;
            HeadersVisible = true;
            Selection.Mode = SelectionMode.Multiple;
            
            ColumnDragFunction = new TreeViewColumnDropFunc(CheckColumnDrop);
            model.DefaultSortFunc = new TreeIterCompareFunc(TrackViewColumn.DefaultTreeIterCompareFunc);
        }    

        private void OnRipToggled(object o, ToggledArgs args)
        {
            try {
                AudioCdTrackInfo ti = (AudioCdTrackInfo)model.PathTrackInfo(new TreePath(args.Path));
                CellRendererToggle renderer = (CellRendererToggle)o;
                ti.CanRip = !ti.CanRip;
                renderer.Active = ti.CanRip;
            } catch {
            }   
        }
        
        private bool CheckColumnDrop(TreeView tree, TreeViewColumn col, TreeViewColumn prev, TreeViewColumn next)
        {
            return prev != null && next != null;
        }
            
        public TrackInfo IterTrackInfo(TreeIter iter)
        {
            return Model.GetValue(iter, 0) as TrackInfo;
        }

        public void ColumnChooser()
        {
            TrackViewColumnWindow chooser = new TrackViewColumnWindow(columns);
            chooser.Show();
        }

        public void SaveColumns()
        {
            foreach(TrackViewColumn column in columns) {
                column.Save(Columns);
            }
        }
        
        private void SetTrackPixbuf(CellRendererPixbuf renderer, TrackInfo track, bool nowPlaying)
        {
            if(nowPlaying) {
                renderer.Pixbuf = now_playing_pixbuf;
                return;
            } else if(track is AudioCdTrackInfo) {
                renderer.Pixbuf = null;
                return;
            }
            
            switch(track.PlaybackError) {
                case TrackPlaybackError.ResourceNotFound:
                    renderer.Pixbuf = resource_not_found_pixbuf;
                    break;
                case TrackPlaybackError.Drm:
                    renderer.Pixbuf = drm_pixbuf;
                    break;
                case TrackPlaybackError.Unknown:
                case TrackPlaybackError.CodecNotFound:
                    renderer.Pixbuf = unknown_error_pixbuf;
                    break;
                default:
                    renderer.Pixbuf = null;
                    break;
            }
        }
        
        protected void StatusColumnDataHandler(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            TrackInfo ti = tree_model.GetValue(iter, 0) as TrackInfo;
            CellRendererPixbuf renderer = (CellRendererPixbuf)cell;
            
            if(PlayerEngineCore.CurrentTrack == null) {
                model.PlayingIter = TreeIter.Zero;
                SetTrackPixbuf(renderer, ti, false);
                return;
            }
        
            if(ti != null) {
                bool same_track = false;
                
                if(PlayerEngineCore.CurrentTrack != null) {
                    //same_track = PlayerEngineCore.CurrentTrack.Equals(ti);
                    same_track = PlayerEngineCore.CurrentTrack == ti;
                }
                
                SetTrackPixbuf(renderer, ti, same_track);
            } else {
                renderer.Pixbuf = null;
            }
        }
        
        protected void RipColumnDataHandler(TreeViewColumn tree_column, CellRenderer cell, 
            TreeModel tree_model, TreeIter iter)
        {
            CellRendererToggle toggle = (CellRendererToggle)cell;
            AudioCdTrackInfo ti = model.IterTrackInfo(iter) as AudioCdTrackInfo;
 
            if(ti != null) {
                toggle.Sensitive = ti.CanPlay && !ti.IsRipped;
                toggle.Activatable = toggle.Sensitive;
                toggle.Active = ti.CanRip && !ti.IsRipped;
            } else {
                toggle.Active = false;
            }
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
            if(!IsRealized) {
                return;
            }
            
            Gdk.Rectangle cellRect = GetCellArea(model.PlayingPath, Columns[0]);

            Gdk.Point point = new Gdk.Point();
            WidgetToTreeCoords(cellRect.Left, cellRect.Top, out point.X, out point.Y);
            cellRect.Location = point;

            // we only care about vertical bounds
            if(cellRect.Location.Y < VisibleRect.Location.Y ||
                cellRect.Location.Y + cellRect.Size.Height > VisibleRect.Location.Y + VisibleRect.Size.Height) {
                ScrollToCell(model.PlayingPath, null, true, 0.5f, 0.0f);
            }
        }

        public void SelectPlaying()
        {
            Selection.UnselectAll();
            
            if(model.PlayingPath != null) {
                Selection.SelectPath(model.PlayingPath);
            }
        }
        
        public bool PlaySelected()
        {
            TreeIter selIter;
            
            try {
                if(!model.GetIter(out selIter, Selection.GetSelectedRows()[0])) {
                    return false;
                }
            } catch {
                return false;
            }
            
            model.PlayIter(selIter);
            return true;
        }
       
        public TrackInfo SelectedTrackInfo {
            get {
                TreePath [] paths = Selection.GetSelectedRows();
                TreeIter selIter;
                
                if(paths.Length <= 0) {
                    return null;
                }
                
                if(!model.GetIter(out selIter, paths[0])) {
                    return null;
                }
                
                return model.IterTrackInfo(selIter);
            }
        }
        
        public IList<TrackInfo> SelectedTrackInfoMultiple {
            get {
                if(Selection.CountSelectedRows() == 0) {
                    return null;
                }
                
                List<TrackInfo> list = new List<TrackInfo>();
                
                foreach(TreePath path in Selection.GetSelectedRows()) {
                    list.Add(model.PathTrackInfo(path));
                }
                
                return list;
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
            if(!base.OnDragMotion(context, x, y, time)) {
                return false;
            }
            
            // Force the drag highlight to be either before or after a
            // row, not on top of one.

            TreePath path;
            TreeViewDropPosition pos;
            
            if(GetDestRowAtPos(x, y, out path, out pos)) {
                SetDragDestRow(path, (TreeViewDropPosition)((int)pos & 0x1));
            }
            
            return true;
        }
    }
}
