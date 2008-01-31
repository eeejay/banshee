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
            Banshee.Gui.DragDrop.DragDropTarget.TrackInfoObjects,
            Banshee.Gui.DragDrop.DragDropTarget.Source
        };
    
            
        private void ConfigureDragAndDrop ()
        {
            EnableModelDragSource (Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask,
                dnd_source_entries, DragAction.Copy | DragAction.Move);
        
            EnableModelDragDest (dnd_dest_entries, DragAction.Copy | DragAction.Move);
        }
        
        protected override void OnDragBegin (Gdk.DragContext context)
        {
            /*if(HighlightedSource.IsDragSource || HighlightedSource is IImportSource) {
                base.OnDragBegin(context);
            }*/
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
        }
        
        protected override bool OnDragMotion (Gdk.DragContext context, int x, int y, uint time)
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
        
            if(!new_playlist_visible && Gtk.Drag.GetSourceWidget(context) != this) {
                TreeIter library = FindSource(LibrarySource.Instance);
                new_playlist_iter = store.AppendNode(library);
                store.SetValue(new_playlist_iter, 0, new_playlist_source);
                store.SetValue(new_playlist_iter, 1, 999);
                new_playlist_visible = true;

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
    
        protected override void OnDragLeave (Gdk.DragContext context, uint time)
        {
            /*TreePath path;
            TreeViewDropPosition pos;
            GetDragDestRow (out path, out pos);

            if(path == null) {
                path = store.GetPath(new_playlist_iter);
            }
            
            final_drag_source = GetSource (path);
            final_drag_start_time = context.StartTime;
        
            if(new_playlist_visible) {
                store.Remove(ref new_playlist_iter);
                new_playlist_visible = false;
                UpdateView();
            }*/
        }

        protected override void OnDragDataReceived (Gdk.DragContext context, int x, int y,
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
            
            if(new_playlist_visible) {
                store.Remove(ref new_playlist_iter);
                new_playlist_visible = false;
                UpdateView();
            }
        
            Gtk.Drag.Finish(context, true, false, time);*/
        }
        
        private void TrackDropOperation (Source source, IList<TrackInfo> tracks, out PlaylistSource newPlaylist)
        {
            newPlaylist = null;
            
            /*if(source is LibrarySource && ServiceManager.SourceManager.ActiveSource is IImportable) {
                IImportable import_source = ServiceManager.SourceManager.ActiveSource as IImportable;
                import_source.Import(tracks);
            } else if(source is PlaylistSource && ServiceManager.SourceManager.ActiveSource is IImportable) {
                IImportable import_source = ServiceManager.SourceManager.ActiveSource as IImportable;
                PlaylistSource playlist = null;
                    
                if(source == new_playlist_source) {
                    playlist = new PlaylistSource();
                    LibrarySource.Instance.AddChildSource(playlist);
                    newPlaylist = playlist;
                } else {
                    playlist = source as PlaylistSource;
                }
                    
                import_source.Import(tracks, playlist);
            } else if(source == new_playlist_source) {
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
    }
}
