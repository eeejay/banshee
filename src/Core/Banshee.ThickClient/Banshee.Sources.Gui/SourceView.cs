//
// SourceView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;
using Gdk;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Gui.DragDrop;
using Banshee.Playlist;
using Banshee.Collection;

namespace Banshee.Sources.Gui
{
    public class SourceView : TreeView
    {
        //private Source newPlaylistSource = new PlaylistSource(-1);
        private TreeIter newPlaylistIter = TreeIter.Zero;
        private bool newPlaylistVisible = false;
        
        private TreeStore store;
        private TreeViewColumn focus_column;
        private SourceRowRenderer renderer;
        private TreePath highlight_path;
        private int currentTimeout = -1;
        
        private static TargetEntry [] dnd_source_entries = new TargetEntry [] {
            Banshee.Gui.DragDrop.DragDropTarget.Source
        };
            
        private static TargetEntry [] dnd_dest_entries = new TargetEntry [] {
            Banshee.Gui.DragDrop.DragDropTarget.TrackInfoObjects,
            Banshee.Gui.DragDrop.DragDropTarget.Source
        };
    
        public event EventHandler SourceDoubleClicked;
    
        public SourceView()
        {
            // Hidden expander column
            TreeViewColumn col = new TreeViewColumn();
            col.Visible = false;
            AppendColumn(col);
            ExpanderColumn = col;
        
            focus_column = new TreeViewColumn();
            renderer = new SourceRowRenderer();
            focus_column.Title = Catalog.GetString("Source");
            focus_column.PackStart(renderer, true);
            focus_column.SetCellDataFunc(renderer, new TreeCellDataFunc(SourceCellDataFunc));
            AppendColumn(focus_column);
            
            store = new TreeStore(typeof(Source), typeof(int));
            store.SetSortColumnId (1, SortType.Ascending);
            store.ChangeSortColumn ();
            Model = store;
            HeadersVisible = false;
            
            EnableModelDragSource(Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                dnd_source_entries, DragAction.Copy | DragAction.Move);
        
            EnableModelDragDest(dnd_dest_entries, DragAction.Copy | DragAction.Move);
            
            RefreshList();

            ServiceManager.SourceManager.SourceAdded += delegate(SourceAddedArgs args) {
                AddSource(args.Source);
            };
            
            ServiceManager.SourceManager.SourceRemoved += delegate(SourceEventArgs args) {
                RemoveSource(args.Source);
            };
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate(SourceEventArgs args) {
                ResetSelection();
            };
            
            ServiceManager.SourceManager.SourceUpdated += delegate(SourceEventArgs args) {
                TreeIter iter = FindSource (args.Source);
                store.SetValue (iter, 1, args.Source.Order);
                QueueDraw();
            };
            
            ServiceManager.PlaybackController.SourceChanged += delegate {
                QueueDraw();
            };
        }

        private TreeIter FindSource(Source source)
        {
            TreeIter iter = TreeIter.Zero;
            store.GetIterFirst(out iter);
            return FindSource(source, iter);
        }
        
        private TreeIter FindSource(Source source, TreeIter iter)
        {
            if(!store.IterIsValid(iter)) {
                return TreeIter.Zero;
            }
            
            do {
                if((store.GetValue(iter, 0) as Source) == source) {
                    return iter;
                }
                
                if(store.IterHasChild(iter)) {
                    TreeIter citer = TreeIter.Zero;
                    store.IterChildren(out citer, iter);
                    TreeIter result = FindSource(source, citer);
                    if(!result.Equals(TreeIter.Zero)) {
                        return result;
                    }
                }
            } while(store.IterNext(ref iter));
            
            return TreeIter.Zero;
        }
        
        private void AddSource(Source source)
        {
            AddSource(source, TreeIter.Zero);
        }

        private void AddSource(Source source, TreeIter parent)
        {
            // Don't add duplicates
            if(!FindSource(source).Equals(TreeIter.Zero))
                return;

            // Don't add a child source before its parent
            if(parent.Equals(TreeIter.Zero) && source.Parent != null)
                return;

            int position = source.Order;
            
            TreeIter iter = parent.Equals(TreeIter.Zero)
                ? store.InsertNode(position) 
                : store.InsertNode(parent, position);
            
            store.SetValue(iter, 0, source);
            store.SetValue(iter, 1, source.Order);

            lock(source.Children) {
                foreach(Source s in source.Children) {
                    AddSource(s, iter);
                }
            }

            source.ChildSourceAdded += delegate(SourceEventArgs e) {
                AddSource(e.Source, iter);
            };

            source.ChildSourceRemoved += delegate(SourceEventArgs e) {
                RemoveSource(e.Source);
            };
           
            if(source.Expanded || (source.AutoExpand != null && source.AutoExpand.Value)) {
                Expand(iter);
            }
            
            if (source.Parent != null ) {
                if (source.Parent.AutoExpand) {
                    Expand (FindSource (source.Parent));
                }
            }
            
            UpdateView ();
        }

        private void RemoveSource(Source source)
        {
            TreeIter iter = FindSource(source);
            if(!iter.Equals(TreeIter.Zero)) {
                store.Remove(ref iter);
            }

            UpdateView();
        }
    
        private void Expand(TreeIter iter)
        {
            TreePath path = store.GetPath(iter);
            ExpandRow(path, true);
        }
    
        private void RefreshList()
        {
            store.Clear();
            foreach(Source source in ServiceManager.SourceManager.Sources) {
                AddSource(source);
            }
        }

        private bool UpdateView()
        {
            for(int i = 0, m = store.IterNChildren(); i < m; i++) {
                TreeIter iter = TreeIter.Zero;
                if(!store.IterNthChild(out iter, i)) {
                    continue;
                }
                
                if(store.IterNChildren(iter) > 0) {
                    ExpanderColumn = Columns[1];
                    return true;
                }
            }
        
            ExpanderColumn = Columns[0];
            return false;
        }
        
        protected override void OnRowExpanded(TreeIter iter, TreePath path)
        {
            base.OnRowExpanded(iter, path);
            GetSource(iter).Expanded = true;
        }
        
        protected override void OnRowCollapsed(TreeIter iter, TreePath path)
        {
            base.OnRowCollapsed(iter, path);
            GetSource(iter).Expanded = false;
        }
        
        protected void SourceCellDataFunc(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            SourceRowRenderer renderer = (SourceRowRenderer)cell;
            renderer.view = this;
            renderer.source = (Source)store.GetValue(iter, 0);
            renderer.path = store.GetPath (iter);
            
            if(renderer.source == null) {
                return;
            }
            
            renderer.Selected = renderer.source.Equals(ServiceManager.SourceManager.ActiveSource);
            //renderer.Italicized = renderer.source.Equals(newPlaylistSource);
            renderer.Sensitive = renderer.source.CanActivate;
        }
        
        internal void UpdateRow(TreePath path, string text)
        {
            TreeIter iter;
            
            if(!store.GetIter(out iter, path)) {
                return;
            }
            
            Source source = store.GetValue(iter, 0) as Source;
            source.Rename(text);
        }
        
        public void BeginRenameSource(Source source)
        {
            TreeIter iter = FindSource(source);
            if(iter.Equals(TreeIter.Zero)) {
                return;
            }
            renderer.Editable = true;
            SetCursor(store.GetPath(iter), focus_column, true);
            renderer.Editable = false;
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            TreePath path;
            
            if (evnt.Button == 1) {
                ResetHighlight ();
            }
            
            if(!GetPathAtPos((int)evnt.X, (int)evnt.Y, out path)) {
                return true;
            }

            Source source = GetSource(path);
            if(evnt.Button == 1) {
                if(!source.CanActivate) {
                    if(!source.Expanded) {
                        ExpandRow(path, false);
                    } else {
                        CollapseRow(path);
                    }
                    return false;
                }
                
                if(ServiceManager.SourceManager.ActiveSource != source) {
                    ServiceManager.SourceManager.SetActiveSource(source);
                }
                
                if(evnt.Type == EventType.TwoButtonPress) {
                    OnSourceDoubleClicked();
                }
                
            } else if(evnt.Button == 3) {
                HighlightPath(path);
                OnPopupMenu ();
                return true;
            }
            
            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnPopupMenu ()
        {
            ServiceManager.Get<InterfaceActionService> ().SourceActions["SourceContextMenuAction"].Activate ();
            return true;
        }

        protected override void OnCursorChanged()
        {
            if(currentTimeout < 0) {
                currentTimeout = (int)GLib.Timeout.Add(200, OnCursorChangedTimeout);
            }
        }
        
        private bool OnCursorChangedTimeout()
        {
            TreeIter iter;
            TreeModel model;
            
            currentTimeout = -1;
            
            if(!Selection.GetSelected(out model, out iter)) {
                return false;
            }
            
            Source new_source = store.GetValue(iter, 0) as Source;
            if(ServiceManager.SourceManager.ActiveSource == new_source) {
                return false;
            }
            
            ServiceManager.SourceManager.SetActiveSource(new_source);
            
            QueueDraw();

            return false;
        }
        
        protected virtual void OnSourceDoubleClicked()
        {
            EventHandler handler = SourceDoubleClicked;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }

        protected override void OnDragBegin(Gdk.DragContext context)
        {
            /*if(HighlightedSource.IsDragSource || HighlightedSource is IImportSource) {
                base.OnDragBegin(context);
            }*/
        }
        
        protected override void OnDragDataGet(Gdk.DragContext context, SelectionData selectionData,
            uint info, uint time)
        {
            switch((DragDropTargetType)info) {
                case DragDropTargetType.Source:
                    new DragDropList<Source>(HighlightedSource, selectionData, context.Targets[0]);
                    break;
                default:
                    return;
            }
            
            base.OnDragDataGet(context, selectionData, info, time);
        }
        
        protected override bool OnDragMotion(Gdk.DragContext context, int x, int y, uint time)
        {
            /*if(Gtk.Drag.GetSourceWidget(context) == this 
                && !HighlightedSource.IsDragSource && !(HighlightedSource is IImportSource)) {
                return false;
            }
        
            base.OnDragMotion(context, x, y, time);
            SetDragDestRow(null, TreeViewDropPosition.IntoOrAfter);
            Gdk.Drag.Status(context, Gdk.DragAction.Copy, time);

            // FIXME: We need to handle this nicer
            if(Gtk.Drag.GetSourceWidget(context) != this && 
                !(ServiceManager.SourceManager.ActiveSource is LibrarySource ||
                ServiceManager.SourceManager.ActiveSource is PlaylistSource ||
                ServiceManager.SourceManager.ActiveSource is Banshee.SmartPlaylist.SmartPlaylistSource ||
                ServiceManager.SourceManager.ActiveSource is IImportable)) {
                return false;
            }
        
            if(!newPlaylistVisible && Gtk.Drag.GetSourceWidget(context) != this) {
                TreeIter library = FindSource(LibrarySource.Instance);
                newPlaylistIter = store.AppendNode(library);
                store.SetValue(newPlaylistIter, 0, newPlaylistSource);
                store.SetValue(newPlaylistIter, 1, 999);
                newPlaylistVisible = true;

                UpdateView();
                Expand(library);
            }
        
            TreePath path;
            TreeViewDropPosition pos;
            
            if(GetDestRowAtPos(x, y, out path, out pos)) {
                Source source = GetSource(path);
                
                if(source == ServiceManager.SourceManager.ActiveSource) {
                    return false;
                }
                
                SetDragDestRow(path, TreeViewDropPosition.IntoOrAfter);
                
                if((source is LibrarySource && (ServiceManager.SourceManager.ActiveSource is IImportable 
                    || ServiceManager.SourceManager.ActiveSource is IImportSource)) ||
                    (source is PlaylistSource) || (source is DapSource) || source.AcceptsInput) {
                    return true;
                }

                Gdk.Drag.Status(context, 0, time);
                return true;
            }*/
            
            return true;
        }
        
        private Source final_drag_source = null;
        private uint final_drag_start_time = 0;
    
        protected override void OnDragLeave(Gdk.DragContext context, uint time)
        {
            /*TreePath path;
            TreeViewDropPosition pos;
            GetDragDestRow (out path, out pos);

            if(path == null) {
                path = store.GetPath(newPlaylistIter);
            }
            
            final_drag_source = GetSource (path);
            final_drag_start_time = context.StartTime;
        
            if(newPlaylistVisible) {
                store.Remove(ref newPlaylistIter);
                newPlaylistVisible = false;
                UpdateView();
            }*/
        }

        protected override void OnDragDataReceived(Gdk.DragContext context, int x, int y,
            Gtk.SelectionData selectionData, uint info, uint time)
        {
            /*if(Gtk.Drag.GetSourceWidget(context) == this) {
                DragDropList<Source> sources = selectionData;
                if(sources.Count <= 0) { 
                    return;
                }
                
                Source source = sources[0];
                
                if(source is IImportSource && final_drag_source is LibrarySource) {
                    (source as IImportSource).Import();
                    Gtk.Drag.Finish(context, true, false, time);
                } else if(final_drag_source != null && source.IsDragSource && 
                    final_drag_source.AcceptsSourceDrop) {
                    final_drag_source.SourceDrop(source);
                    Gtk.Drag.Finish(context, true, false, time);
                } else {
                    Gtk.Drag.Finish(context, false, false, time);
                }
                
                return;
            }
            
            if(final_drag_start_time == context.StartTime) {
                PlaylistSource playlist_remove_on_failure = null;
                try {
                    DragDropList<TrackInfo> dnd_transfer = selectionData;
                    TrackDropOperation(final_drag_source, dnd_transfer, out playlist_remove_on_failure);
                } catch(Exception e) {
                    if(playlist_remove_on_failure != null) {
                        playlist_remove_on_failure.Unmap();
                        playlist_remove_on_failure = null;
                    }
                    
                    LogCore.Instance.PushError(Catalog.GetString("Could not import tracks"), e.Message);
                }
            }
            
            if(newPlaylistVisible) {
                store.Remove(ref newPlaylistIter);
                newPlaylistVisible = false;
                UpdateView();
            }
        
            Gtk.Drag.Finish(context, true, false, time);*/
        }
        
        private void TrackDropOperation(Source source, IList<TrackInfo> tracks, 
            out PlaylistSource newPlaylist)
        {
            newPlaylist = null;
            
            /*if(source is LibrarySource && ServiceManager.SourceManager.ActiveSource is IImportable) {
                IImportable import_source = ServiceManager.SourceManager.ActiveSource as IImportable;
                import_source.Import(tracks);
            } else if(source is PlaylistSource && ServiceManager.SourceManager.ActiveSource is IImportable) {
                IImportable import_source = ServiceManager.SourceManager.ActiveSource as IImportable;
                PlaylistSource playlist = null;
                    
                if(source == newPlaylistSource) {
                    playlist = new PlaylistSource();
                    LibrarySource.Instance.AddChildSource(playlist);
                    newPlaylist = playlist;
                } else {
                    playlist = source as PlaylistSource;
                }
                    
                import_source.Import(tracks, playlist);
            } else if(source == newPlaylistSource) {
                PlaylistSource playlist = new PlaylistSource();
                playlist.AddTrack(tracks);
                playlist.Rename(PlaylistUtil.GoodUniqueName(playlist.Tracks));
                playlist.Commit();
                LibrarySource.Instance.AddChildSource(playlist);
                UpdateView();
            } else if(source is PlaylistSource || source is DapSource || source.AcceptsInput) {
                source.AddTrack(tracks);
                source.Commit();
            }*/
        }
        
        public Source GetSource(TreeIter iter)
        {
            return store.GetValue(iter, 0) as Source;
        }
        
        public Source GetSource(TreePath path)
        {
            TreeIter iter;
        
            if(store.GetIter(out iter, path)) {
                return GetSource(iter);
            }
        
            return null;
        }
        
        private void ResetSelection()
        {
            TreeIter iter = FindSource (ServiceManager.SourceManager.ActiveSource);
            
            if(!iter.Equals(TreeIter.Zero)){
                Selection.SelectIter(iter);
            }
        }
        
        public void HighlightPath(TreePath path)
        {
            //Selection.SelectPath(path);
            highlight_path = path;
            QueueDraw ();
        }
        
        public void ResetHighlight()
        {   
            highlight_path = null;
            QueueDraw ();
        }
        
        public Source HighlightedSource {
            get {
                TreeModel model;
                TreeIter iter;
                
                if (highlight_path == null || !store.GetIter (out iter, highlight_path)) {
                    return null;
                }
                    
                return store.GetValue(iter, 0) as Source;
            }
        }
        
        internal TreePath HighlightedPath {
            get { return highlight_path; }
        }

        private bool editing_row = false;
        public bool EditingRow {
            get { return editing_row; }
            set { editing_row = value; }
        }
    }
}
