//
// SourceView_DragAndDrop.cs
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

using Banshee.Sources;
using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Playlist;

using Banshee.Gui.DragDrop;

namespace Banshee.Sources.Gui
{
    public partial class SourceView
    {
        private static TargetEntry [] dnd_source_entries = new TargetEntry [] {
            Banshee.Gui.DragDrop.DragDropTarget.Source
        };
            
        private static TargetEntry [] dnd_dest_entries = new TargetEntry [] {
            Banshee.Gui.DragDrop.DragDropTarget.Source,
            Hyena.Data.Gui.ListViewDragDropTarget.ModelSelection
        };
        
        private Source new_playlist_source = null;
        private TreeIter new_playlist_iter = TreeIter.Zero;
        private bool new_playlist_visible = false;
        
        private Source final_drag_source = null;
        private uint final_drag_start_time = 0;
        
        private void ConfigureDragAndDrop ()
        {
            EnableModelDragSource (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                dnd_source_entries, DragAction.Copy | DragAction.Move);
        
            EnableModelDragDest (dnd_dest_entries, DragAction.Copy | DragAction.Copy);
        }
        
        protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
        {
            TreePath path;
            TreeViewDropPosition pos;
            Source active_source = ServiceManager.SourceManager.ActiveSource;
            
            if (active_source.SupportedMergeTypes == SourceMergeType.None) {
                Gdk.Drag.Status (context, 0, time);
                return false;
            } else if (!GetDestRowAtPos (x, y, out path, out pos)) {
                Gdk.Drag.Status (context, 0, time);
                return false;
            }
            
            Source drop_source = store.GetSource (path);
            Source parent_source = drop_source as LibrarySource ?? drop_source.Parent as LibrarySource;

            // Scroll if within 20 pixels of the top or bottom
            if (y < 20)
                Vadjustment.Value -= 30;
            else if ((Allocation.Height - y) < 20)
                Vadjustment.Value += 30;

            if (parent_source != null && parent_source.AcceptsInputFromSource (active_source)) {
                ShowNewPlaylistUnder (parent_source, active_source);
            } else if (drop_source != NewPlaylistSource) {
                HideNewPlaylistRow ();
            }

            if (!drop_source.AcceptsInputFromSource (active_source)) {
                Gdk.Drag.Status (context, 0, time);
                return true;
            }

            SetDragDestRow (path, TreeViewDropPosition.IntoOrAfter);

            bool move = (active_source is LibrarySource) && (drop_source is LibrarySource);
            Gdk.Drag.Status (context, move ? Gdk.DragAction.Move : Gdk.DragAction.Copy, time);
            
            return true;
        }

        Source new_playlist_parent = null;
        bool parent_was_expanded;
        private void ShowNewPlaylistUnder (Source parent, Source active)
        {
            if (new_playlist_visible) {
                if (parent == new_playlist_parent)
                    return;
                else
                    HideNewPlaylistRow ();
            }

            NewPlaylistSource.SetParentSource (parent);
            if (!NewPlaylistSource.AcceptsInputFromSource (active)) {
                NewPlaylistSource.SetParentSource (new_playlist_parent);
                return;
            }

            TreeIter parent_iter = store.FindSource (parent);
            new_playlist_iter = store.AppendNode (parent_iter);
            
            store.SetValue (new_playlist_iter, 0, NewPlaylistSource);
            store.SetValue (new_playlist_iter, 1, 999);
            new_playlist_visible = true;

            UpdateView ();

            TreePath parent_path = store.GetPath (parent_iter);
            parent_was_expanded = GetRowExpanded (parent_path);
            Expand (parent_iter);

            new_playlist_parent = parent;
        }

        private void HideNewPlaylistRow ()
        {
            if (!new_playlist_visible) {
                return;
            }
            
            if (!parent_was_expanded) {
                TreeIter iter = store.FindSource (new_playlist_parent);
                TreePath path = store.GetPath (iter);
                CollapseRow (path);
            }

            store.Remove (ref new_playlist_iter);
            new_playlist_visible = false;
            
            UpdateView ();
        }
        
        protected override void OnDragLeave (Gdk.DragContext context, uint time)
        {
            TreePath path;
            TreeViewDropPosition pos;
            GetDragDestRow (out path, out pos);

            if (path == null && !TreeIter.Zero.Equals (new_playlist_iter)) {
                path = store.GetPath (new_playlist_iter);
            }
            
            if (path != null) {
                final_drag_source = store.GetSource (path);
            }

            final_drag_start_time = context.StartTime;
            HideNewPlaylistRow ();
            SetDragDestRow (null, TreeViewDropPosition.Before);
        }
        
        protected override void OnDragBegin (Gdk.DragContext context)
        {
            if (ServiceManager.SourceManager.ActiveSource.SupportedMergeTypes != SourceMergeType.None) {
                base.OnDragBegin (context);
            }
        }

        protected override void OnDragEnd (Gdk.DragContext context)
        {
            base.OnDragEnd (context);
            SetDragDestRow (null, TreeViewDropPosition.Before);
        }

        protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y,
            Gtk.SelectionData selectionData, uint info, uint time)
        {
            try {
                if (final_drag_start_time != context.StartTime || final_drag_source == null) {
                    Gtk.Drag.Finish (context, false, false, time);
                    return;
                }
                
                Source drop_source = final_drag_source;    
                
                if (final_drag_source == NewPlaylistSource) {
                    PlaylistSource playlist = new PlaylistSource (Catalog.GetString ("New Playlist"), 
                        (new_playlist_parent as PrimarySource).DbId);
                    playlist.Save ();
                    playlist.PrimarySource.AddChildSource (playlist);
                    drop_source = playlist;
                }
                
                if (Gtk.Drag.GetSourceWidget (context) == this) {
                    DragDropList<Source> sources = selectionData;
                    if (sources.Count > 0) {
                        drop_source.MergeSourceInput (sources[0], SourceMergeType.Source);
                    }
                } else {
                    drop_source.MergeSourceInput (ServiceManager.SourceManager.ActiveSource, 
                        SourceMergeType.ModelSelection);
                }
                
                Gtk.Drag.Finish (context, true, false, time);
            } finally {
                HideNewPlaylistRow ();
            }
        }
        
        protected override void OnDragDataGet (Gdk.DragContext context, SelectionData selectionData,
            uint info, uint time)
        {
            switch ((DragDropTargetType)info) {
                case DragDropTargetType.Source:
                    new DragDropList<Source> (ServiceManager.SourceManager.ActiveSource, 
                        selectionData, context.Targets[0]);
                    break;
                default:
                    return;
            }
            
            base.OnDragDataGet (context, selectionData, info, time);
        }
    }
}
