/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SourceView.cs
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
using Mono.Unix;
using Gtk;
using Gdk;
using Pango;

using Banshee.Base;
using Banshee.Dap;

namespace Banshee
{
    public class CellEdit : Entry, CellEditable
    {
        public string path;
    
        public CellEdit() : base()
        {
        
        }
        
        protected CellEdit(System.IntPtr ptr) : base(ptr)
        {
        
        }
    }
    
    public class EditedEventArgs : EventArgs
    {
        public TreePath path;
        public string text;
    }
    
    public delegate void EditedEventHandler(object o, 
        EditedEventArgs args);

    public class SourceView : TreeView
    {
        private ListStore store;
        private Source selectedSource;
        private bool forceReselect = false;

        private Source newPlaylistSource = new PlaylistSource(Catalog.GetString("New Playlist"));
        private TreeIter newPlaylistIter = TreeIter.Zero;
        private bool newPlaylistVisible = false;
        
        public event EventHandler SourceChanged;

        private int currentTimeout = -1;

        public SourceView()
        {
            TreeViewColumn col = new TreeViewColumn();
            SourceRowRenderer renderer = new SourceRowRenderer();
            col.Title = Catalog.GetString("Source");
            col.PackStart(renderer, true);
            col.SetCellDataFunc(renderer, new TreeCellDataFunc(SourceCellDataFunc));
            AppendColumn(col);
            
            store = new ListStore(typeof(Source));
            Model = store;
            HeadersVisible = false;
            
            CursorChanged += OnCursorChanged;
            
            try {
                DapCore.DapAdded += OnDapCoreDapAdded;
                DapCore.DapRemoved += OnDapCoreDapRemoved;
                if(Core.Instance.AudioCdCore != null) {
                    Core.Instance.AudioCdCore.DiskAdded += OnAudioCdCoreDiskAdded;
                    Core.Instance.AudioCdCore.DiskRemoved += OnAudioCdCoreDiskRemoved;
                    Core.Instance.AudioCdCore.Updated += OnAudioCdCoreUpdated;
                }
                Core.Library.Updated += OnLibraryUpdated;
            } catch(NullReferenceException) {}
            
            RefreshList();
        }
                    
        protected void SourceCellDataFunc(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            SourceRowRenderer renderer = (SourceRowRenderer)cell;
            renderer.view = this;
            renderer.source = (Source)store.GetValue(iter, 0);
            if(renderer.source == null)
                return;
            renderer.Selected = renderer.source.Equals(selectedSource);
            renderer.Italicized = renderer.source.Equals(newPlaylistSource);
            renderer.Editable = renderer.source.CanRename;
        }
        
        public void UpdateRow(TreePath path, string text)
        {
            TreeIter iter;
            
            if(!store.GetIter(out iter,path))
                return;
            
            Source source = store.GetValue(iter, 0) as Source;
            source.Rename(text);
            QueueDraw();
        }
        
        private void RefreshList()
        {
            Application.Invoke(delegate {
            
            store.Clear();
            store.AppendValues(new LibrarySource());
            
            if(Core.Instance.AudioCdCore != null) {
                try {
                    foreach(AudioCdDisk disk in Core.Instance.AudioCdCore.Disks)
                        store.AppendValues(new AudioCdSource(disk));
                } catch(NullReferenceException) {}
            }
        
            // iPod Sources
            try {
                foreach(DapDevice device in DapCore.Devices)
                    store.AppendValues(new DapSource(device));
            } catch(NullReferenceException) {}
            
            // Playlist Sources
            string [] names = Playlist.ListAll();
            
            if(names == null)
                return;
                
            foreach(string name in names) {
                AddPlaylist(name);
            }
            
            });
        } 
        
        public void AddPlaylist(string name)
        {
            PlaylistSource plsrc = new PlaylistSource(name);
            plsrc.Updated += OnSourceUpdated;
            store.AppendValues(plsrc);
        }
        
        private void OnCursorChanged(object o, EventArgs args)
        {                
            if(currentTimeout < 0)
                currentTimeout = (int)GLib.Timeout.Add(200, 
                    OnCursorChangedTimeout);
        }
        
        private bool OnCursorChangedTimeout()
        {
            TreeIter iter;
            TreeModel model;
            
            currentTimeout = -1;
            
            if(!Selection.GetSelected(out model, out iter))
                return false;
            
            Source newSource = store.GetValue(iter, 0) as Source;
            if(selectedSource == newSource && !forceReselect)
                return false;
                
            forceReselect = false;
            selectedSource = newSource;
            
            QueueDraw();
            
            EventHandler handler = SourceChanged;
            if(handler != null)
                handler(this, new EventArgs());
            
            return false;
        }
        
        protected override bool OnDragMotion(Gdk.DragContext context, int x, int y, uint time)
        {
            base.OnDragMotion(context, x, y, time);
            SetDragDestRow(null, TreeViewDropPosition.IntoOrAfter);
            Gdk.Drag.Status(context, 0, time);

            // TODO: Support other drag sources
            if(SelectedSource.Type != SourceType.Library)
                return true;
            
            if(!newPlaylistVisible) {
                newPlaylistIter = store.AppendValues(newPlaylistSource);
                newPlaylistVisible = true;
            }

            TreePath path;
            TreeViewDropPosition pos;
            if(!GetDestRowAtPos(x, y, out path, out pos))
                path = store.GetPath(newPlaylistIter);

            Source source = GetSource(path);
            if(source == null)
                return true;
            // TODO: Support other drag destinations
            if(source.Type != SourceType.Playlist && source.Type != SourceType.Dap)
                return true;
            
            SetDragDestRow(path, TreeViewDropPosition.IntoOrAfter);
            Gdk.Drag.Status(context, Gdk.DragAction.Copy, time);
            return true;
        }
        
        protected override void OnDragLeave(Gdk.DragContext context, uint time)
        {
            if (newPlaylistVisible) {
                store.Remove(ref newPlaylistIter);
                newPlaylistVisible = false;
            }
        }

        protected override void OnDragDataReceived(Gdk.DragContext context, int x, int y,
            Gtk.SelectionData selectionData, uint info, uint time)
        {
            TreePath destPath;
            TreeViewDropPosition pos;
            
            string rawData = Dnd.SelectionDataToString(selectionData);        
            string [] rawDataArray = Dnd.SplitSelectionData(rawData);
            if(rawData.Length <= 0) {
                Gtk.Drag.Finish(context, false, false, time);
                return;        
            }
            
            ArrayList tracks = new ArrayList();
            foreach(string trackId in rawDataArray) {
                try {
                    int tid = Convert.ToInt32(trackId);
                    tracks.Add(Core.Library.Tracks[tid]);
                } catch(Exception) {
                    continue;
                }
            }
            
            if(GetDestRowAtPos(x, y, out destPath, out pos)) {
                Source source = GetSource(destPath);
                
                if(source.Type == SourceType.Playlist) {
                    Playlist pl = new Playlist(source.Name);
                    pl.Load();
                    pl.Append(tracks);
                    pl.Save();
                } else if(source.Type == SourceType.Dap) {
                    DapSource dapSource = source as DapSource;
                    
                    foreach(LibraryTrackInfo lti in tracks) {
                        dapSource.QueueForSync(lti);    
                    }
                }
            } else {
                Playlist pl = new Playlist(Playlist.GoodUniqueName(tracks));
                pl.Append(tracks);
                pl.Save();
                pl.Saved += OnPlaylistSaved;
            }
            
            Gtk.Drag.Finish(context, true, false, time);
        }
        
        private void OnPlaylistSaved(object o, PlaylistSavedArgs args)
        {
            AddPlaylist(args.Name);
        }
        
        public void SelectLibrary()
        {
            Selection.SelectPath(new TreePath("0"));
            OnCursorChanged(this, new EventArgs());
        }
        
        public void SelectLibraryForce()
        {
            forceReselect = true;
            Selection.SelectPath(new TreePath("0"));
            OnCursorChangedTimeout();
        }

        public void SelectSource(Source source)
        {
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                TreeIter iter = TreeIter.Zero;
                if(!store.IterNthChild(out iter, i)) {
                    continue;
                }
                
                if((store.GetValue(iter, 0) as Source) == source) {
                    Selection.SelectPath(store.GetPath(iter));
                    OnCursorChanged(this, new EventArgs());
                    return;
                }
            }
        }
        
        public void HighlightPath(TreePath path)
        {
            Selection.SelectPath(path);
        }
        
        private void OnSourceUpdated(object o, EventArgs args)
        {
            ThreadedQueueDraw();
        }
        
        private void OnIpodCoreDeviceChanged(object o, EventArgs args)
        {
          //playlistModel.Source.Rename(device.Name);
                
        }
        
        private void OnDapCoreDapAdded(object o, DapEventArgs args)
        {
            int index = FindSourceLastIndex(SourceType.AudioCd);

            TreeIter iter = store.Insert(index + 1);
            store.SetValue(iter, 0, new DapSource(args.Dap));
        }
        
        private void OnDapCoreDapRemoved(object o, DapEventArgs args)
        {
            TreeIter iter = TreeIter.Zero;
            
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                if(!store.IterNthChild(out iter, i))
                    continue;
                
                object obj = store.GetValue(iter, 0);
                
                if(!(obj is DapSource))
                    continue;
                    
                DapSource source = obj as DapSource;
                if(source.Device == args.Dap) {
                    store.Remove(ref iter);
                    return;
                }
            }
        }
        
        private int FindSourceLastIndex(SourceType type)
        {
            int lastIndex = 0;
            int index = 0;
            
            foreach(object [] obj in store) {
                if(obj[0].GetType() != typeof(Source)) {
                    index++;
                    continue;
                }
                
                if((obj[0] as Source).Type == type)
                    lastIndex = index;
            
                index++;
            }
            
            return lastIndex;
        }
        
        private void OnAudioCdCoreDiskAdded(object o, 
            AudioCdCoreDiskAddedArgs args)
        {
            int index = FindSourceLastIndex(SourceType.Library);

            TreeIter iter = store.Insert(index + 1);
            store.SetValue(iter, 0, new AudioCdSource(args.Disk));
        }

        private void OnAudioCdCoreDiskRemoved(object o, 
            AudioCdCoreDiskRemovedArgs args)
        {
            TreeIter iter = TreeIter.Zero;
            
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                if(!store.IterNthChild(out iter, i))
                    continue;
                
                object obj = store.GetValue(iter, 0);
                
                if(obj.GetType() != typeof(AudioCdSource))
                    continue;
                    
                AudioCdSource source = obj as AudioCdSource;
                if(source.Disk.Udi == args.Udi) {
                    store.Remove(ref iter);
                    return;
                }
            }
        }
        
        private void OnAudioCdCoreUpdated(object o, EventArgs args)
        {
            ThreadedQueueDraw();
        }
        
        private void OnLibraryUpdated(object o, EventArgs args)
        {
            ThreadedQueueDraw();
        }
        
        public Source GetSource(TreePath path)
        {
            TreeIter iter;
        
            if(store.GetIter(out iter, path))
                return store.GetValue(iter, 0) as Source;
            
            return null;
        }
        
        public Source SelectedSource
        {
            get {
                return selectedSource;
            }
        }
        
        public void ResetHighlight()
        {
            TreeIter iter = TreeIter.Zero;
            
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                if(!store.IterNthChild(out iter, i))
                    continue;
                
                object obj = store.GetValue(iter, 0);
                
                if(obj == SelectedSource) {
                    Selection.SelectIter(iter);
                }
            }
        }
        
        public Source HighlightedSource
        {
            get {
                TreeModel model;
                TreeIter iter;
                
                if(!Selection.GetSelected(out model, out iter))
                    return null;
                    
                return store.GetValue(iter, 0) as Source;
            }
        }
        
        public void ThreadedQueueDraw()
        {
            Application.Invoke(delegate {
                QueueDraw();
            });
        }
        
        public bool EditingRow = false;
    }

    public class SourceRowRenderer : CellRendererText
    {
        public bool Selected = false;
        public bool Italicized = false;
        public Source source;
        public SourceView view;

        public SourceRowRenderer()
        {
            Editable = true;
            //Editable = false;
        }
        
        protected SourceRowRenderer(System.IntPtr ptr) : base(ptr)
        {
        
        }
        
        private StateType RendererStateToWidgetState(CellRendererState flags)
        {
            StateType state = StateType.Normal;
            if((CellRendererState.Selected & flags).Equals(
                CellRendererState.Selected))
                state = StateType.Selected;
            return state;
        }
        
        public override void GetSize(Widget widget, ref Gdk.Rectangle cell_area,
            out int x_offset, out int y_offset, out int width, out int height)
        {        
               int text_x, text_y, text_w, text_h;
   
               base.GetSize(widget, ref cell_area, out text_x, out text_y, 
                   out text_w, out text_h);
                
            x_offset = 0;
            y_offset = 0;
            width = text_w;
            height = text_h + 5;
        }
        
        protected override void Render(Gdk.Drawable drawable, 
            Widget widget, Gdk.Rectangle background_area, 
            Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, 
            CellRendererState flags)
        {
            int titleLayoutWidth, titleLayoutHeight;
            int countLayoutWidth, countLayoutHeight;
            int maxTitleLayoutWidth;
            bool hideCounts = false;
            
            StateType state = RendererStateToWidgetState(flags);
            Pixbuf icon = null;
            
            if(source == null)
                return;
            
            switch(source.Type) {
                case SourceType.Playlist:
                    icon = IconThemeUtils.LoadIcon(22, "source-playlist");
                    break;
                case SourceType.Dap:
                    icon = (source as DapSource).Device.GetIcon(22);
                    break;
                case SourceType.AudioCd:
                    icon = IconThemeUtils.LoadIcon(22, "media-cdrom", "gnome-dev-cdrom-audio", "source-cd-audio");
                    break;
                case SourceType.Library:
                default:
                    icon = IconThemeUtils.LoadIcon(22, "source-library");
                    break;
            }
            
            Pango.Layout titleLayout = new Pango.Layout(widget.PangoContext);
            Pango.Layout countLayout = new Pango.Layout(widget.PangoContext);
            
            FontDescription fd = widget.PangoContext.FontDescription.Copy();
            fd.Weight = Selected ? Pango.Weight.Bold : Pango.Weight.Normal;
            if (Italicized) {
                fd.Style = Pango.Style.Italic;
                hideCounts = true;
            }

            titleLayout.FontDescription = fd;
            countLayout.FontDescription = fd;
            
            string titleText = source.Name;
            titleLayout.SetMarkup(GLib.Markup.EscapeText(titleText));
            countLayout.SetMarkup("<span size=\"small\">(" + source.Count + ")</span>");
            
            titleLayout.GetPixelSize(out titleLayoutWidth, out titleLayoutHeight);
            countLayout.GetPixelSize(out countLayoutWidth, out countLayoutHeight);
            
            maxTitleLayoutWidth = cell_area.Width - icon.Width - countLayoutWidth - 10;
            
            while(true) {
                titleLayout.GetPixelSize(out titleLayoutWidth, 
                    out titleLayoutHeight);
                if(titleLayoutWidth <= maxTitleLayoutWidth)
                    break;
                
                try {
                    titleText = titleText.Substring(0, titleText.Length - 1);
                    titleLayout.SetMarkup(GLib.Markup.EscapeText(titleText).Trim() + "...");
                } catch(Exception) {
                    titleLayout.SetMarkup(source.Name);
                    hideCounts = true;
                    break;
                }
            }
            
            Gdk.GC mainGC = widget.Style.TextGC(state);
                
            if(source.Type == SourceType.Dap 
                && (source as DapSource).NeedSync 
                && !state.Equals(StateType.Selected)) {
                mainGC = new Gdk.GC(drawable);
                mainGC.Copy(widget.Style.TextGC(state));
                
                Gdk.Color color = Gdk.Color.Zero;
                if(Gdk.Color.Parse("blue", ref color))
                    mainGC.RgbFgColor = color;
            } 
            
            drawable.DrawPixbuf(mainGC, icon, 0, 0, 
                cell_area.X + 0, 
                cell_area.Y + ((cell_area.Height - icon.Height) / 2),
                icon.Width, icon.Height,
                RgbDither.None, 0, 0);
        
            drawable.DrawLayout(mainGC, 
                cell_area.X + icon.Width + 6, 
                cell_area.Y + ((cell_area.Height - titleLayoutHeight) / 2) + 1, 
                titleLayout);
            
            if(hideCounts)
                return;
                
            Gdk.GC modGC = widget.Style.TextGC(state);
            if(!state.Equals(StateType.Selected)) {
                modGC = new Gdk.GC(drawable);
                modGC.Copy(widget.Style.TextGC(state));
                Gdk.Color fgcolor = widget.Style.Foreground(state);
                Gdk.Color bgcolor = widget.Style.Background(state);
                modGC.RgbFgColor = Utilities.ColorBlend(fgcolor, bgcolor);
            } 
            
            drawable.DrawLayout(modGC,
                (cell_area.X + cell_area.Width) - countLayoutWidth - 2,
                cell_area.Y + ((cell_area.Height - countLayoutHeight) / 2) + 1,
                countLayout);
        }
        
        public override CellEditable StartEditing(Gdk.Event evnt , Widget widget, 
            string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, 
            CellRendererState flags)
        {
            CellEdit text = new CellEdit();
            text.EditingDone += OnEditDone;
            text.Text = source.Name;
            text.path = path;
            text.Show();
            
            view.EditingRow = true;
            
            return text;
        }
        
        private void OnEditDone(object o, EventArgs args)
        {
            CellEdit edit = o as CellEdit;
            if(view == null)
                return;
            view.EditingRow = false;
            view.UpdateRow(new TreePath(edit.path), edit.Text);
        }
    }
}
