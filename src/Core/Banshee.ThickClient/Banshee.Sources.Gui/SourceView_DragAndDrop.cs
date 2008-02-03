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

using Gtk;
using Gdk;

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
            Banshee.Gui.DragDrop.DragDropTarget.ModelSelection
        };
        
        private Source final_drag_source = null;
        private uint final_drag_start_time = 0;
        
        private void ConfigureDragAndDrop ()
        {
            EnableModelDragSource (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                dnd_source_entries, DragAction.Copy | DragAction.Move);
        
            EnableModelDragDest (dnd_dest_entries, DragAction.Copy | DragAction.Move);
        }
        
        protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
        {
            base.OnDragMotion (context, x, y, time);
            SetDragDestRow (null, TreeViewDropPosition.IntoOrAfter);
            Gdk.Drag.Status (context, Gdk.DragAction.Copy, time);
        
            if (!new_playlist_visible && Gtk.Drag.GetSourceWidget (context) != this) {
                TreeIter library = FindSource (ServiceManager.SourceManager.DefaultSource);
                new_playlist_iter = store.AppendNode (library);
                store.SetValue (new_playlist_iter, 0, new_playlist_source);
                store.SetValue (new_playlist_iter, 1, 999);
                new_playlist_visible = true;

                UpdateView ();
                Expand (library);
            }
        
            TreePath path;
            TreeViewDropPosition pos;
            
            if (!GetDestRowAtPos (x, y, out path, out pos)) {
                Gdk.Drag.Status (context, 0, time);
                return true;
            }
            
            Source drop_source = GetSource (path);
            Source active_source = ServiceManager.SourceManager.ActiveSource;

            if (drop_source == null || drop_source == active_source 
                || !drop_source.AcceptsInputFromSource (active_source)) {
                return false;
            }

            SetDragDestRow (path, TreeViewDropPosition.IntoOrAfter);
            return true;
        }
        
        protected override void OnDragLeave (Gdk.DragContext context, uint time)
        {
            TreePath path;
            TreeViewDropPosition pos;
            GetDragDestRow (out path, out pos);

            if (path == null) {
                path = store.GetPath (new_playlist_iter);
            }
            
            final_drag_source = GetSource (path);
            final_drag_start_time = context.StartTime;
        
            if (new_playlist_visible) {
                store.Remove (ref new_playlist_iter);
                new_playlist_visible = false;
                UpdateView ();
            }
        }

        protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y,
            Gtk.SelectionData selectionData, uint info, uint time)
        {
            if (final_drag_start_time == context.StartTime && final_drag_source != null) {
                Source drop_source = final_drag_source;
                
                if (final_drag_source == new_playlist_source) {
                    PlaylistSource playlist = new PlaylistSource ("New Playlist");
                    playlist.Save ();
                    ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);
                    drop_source = playlist;
                }
                
                drop_source.MergeSourceInput (ServiceManager.SourceManager.ActiveSource, true);
            }
            
            if (new_playlist_visible) {
                store.Remove (ref new_playlist_iter);
                new_playlist_visible = false;
                UpdateView ();
            }
        
            Gtk.Drag.Finish (context, true, false, time);
            
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
            }*/
        }
                
        /*protected override void OnDragBegin (Gdk.DragContext context)
        {
            if (HighlightedSource.IsDragSource || HighlightedSource is IImportSource) {
                base.OnDragBegin (context);
            }
        }
        
        protected override void OnDragDataGet (Gdk.DragContext context, SelectionData selectionData,
            uint info, uint time)
        {
            switch ((DragDropTargetType)info) {
                case DragDropTargetType.Source:
                    new DragDropList<Source> (HighlightedSource, selectionData, context.Targets[0]);
                    break;
                default:
                    return;
            }
            
            base.OnDragDataGet (context, selectionData, info, time);
        }*/
    }
}
