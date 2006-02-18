
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
using Banshee.Sources;

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
    
    public delegate void EditedEventHandler(object o, EditedEventArgs args);

    public class SourceView : TreeView
    {
        private Source newPlaylistSource = new PlaylistSource(-1);
        private TreeIter newPlaylistIter = TreeIter.Zero;
        private bool newPlaylistVisible = false;
        
        private ListStore store;
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
            RefreshList();
            
            SourceManager.SourceAdded += delegate(SourceAddedArgs args) {
                if(FindSource(args.Source).Equals(TreeIter.Zero)) {
                    TreeIter iter = store.Insert(args.Position);
                    store.SetValue(iter, 0, args.Source);
                }
            };
            
            SourceManager.SourceRemoved += delegate(SourceEventArgs args) {
                TreeIter iter = FindSource(args.Source);
                if(!iter.Equals(TreeIter.Zero)) {
                    store.Remove(ref iter);
                }
            };
            
            SourceManager.ActiveSourceChanged += delegate(SourceEventArgs args) {
                ResetHighlight();
            };
            
            SourceManager.SourceUpdated += delegate(SourceEventArgs args) {
                QueueDraw();
            };
        }

        private TreeIter FindSource(Source source) {
            for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                TreeIter iter = TreeIter.Zero;
                if(!store.IterNthChild(out iter, i)) {
                    continue;
                }
                
                if((store.GetValue(iter, 0) as Source) == source) {
                    return iter;
                }
            }

            return TreeIter.Zero;
        }
                    
        protected void SourceCellDataFunc(TreeViewColumn tree_column,
            CellRenderer cell, TreeModel tree_model, TreeIter iter)
        {
            SourceRowRenderer renderer = (SourceRowRenderer)cell;
            renderer.view = this;
            renderer.source = (Source)store.GetValue(iter, 0);
            if(renderer.source == null) {
                return;
            }
            renderer.Selected = renderer.source.Equals(SourceManager.ActiveSource);
            renderer.Italicized = renderer.source.Equals(newPlaylistSource);
            renderer.Editable = renderer.source.CanRename;
        }
        
        public void UpdateRow(TreePath path, string text)
        {
            TreeIter iter;
            
            if(!store.GetIter(out iter, path)) {
                return;
            }
            
            Source source = store.GetValue(iter, 0) as Source;
            source.Rename(text);
        }
        
        private void RefreshList()
        {
            store.Clear();
            foreach(Source source in SourceManager.Sources) {
                store.AppendValues(source);
            }
        } 
        
        private void OnCursorChanged(object o, EventArgs args)
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
            if(SourceManager.ActiveSource == new_source) {
                return false;
            }
            
            SourceManager.SetActiveSource(new_source);
            
            QueueDraw();

            return false;
        }
        
        protected override bool OnDragMotion(Gdk.DragContext context, int x, int y, uint time)
        {
            base.OnDragMotion(context, x, y, time);
            SetDragDestRow(null, TreeViewDropPosition.IntoOrAfter);
            Gdk.Drag.Status(context, 0, time);

            // TODO: Support other drag sources
            if(!(SourceManager.ActiveSource is LibrarySource)) {
                return true;
            }
            
            if(!newPlaylistVisible) {
                newPlaylistIter = store.AppendValues(newPlaylistSource);
                newPlaylistVisible = true;
            }

            TreePath path;
            TreeViewDropPosition pos;
            if(!GetDestRowAtPos(x, y, out path, out pos)) {
                path = store.GetPath(newPlaylistIter);
            }

            Source source = GetSource(path);
            if(source == null) {
                return true;
            }
            
            // TODO: Support other drag destinations
            if(!(source is PlaylistSource) && !(source is DapSource)) {
                return true;
            }
            
            SetDragDestRow(path, TreeViewDropPosition.IntoOrAfter);
            Gdk.Drag.Status(context, Gdk.DragAction.Copy, time);
            return true;
        }
        
        protected override void OnDragLeave(Gdk.DragContext context, uint time)
        {
            if(newPlaylistVisible) {
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
                    tracks.Add(Globals.Library.Tracks[tid]);
                } catch(Exception) {
                    continue;
                }
            }
            
            if(GetDestRowAtPos(x, y, out destPath, out pos)) {
                Source source = GetSource(destPath);
                if(source is PlaylistSource || source is DapSource) {
                    source.AddTrack(tracks);
                    source.Commit();
                }
            } else {
                PlaylistSource playlist = new PlaylistSource();
                playlist.AddTrack(tracks);
                playlist.Rename(PlaylistUtil.GoodUniqueName(playlist.Tracks));
                playlist.Commit();
                SourceManager.AddSource(playlist);
            }
            
            Gtk.Drag.Finish(context, true, false, time);
        }
        
        public void HighlightPath(TreePath path)
        {
            Selection.SelectPath(path);
        }
        
        public Source GetSource(TreePath path)
        {
            TreeIter iter;
        
            if(store.GetIter(out iter, path)) {
                return store.GetValue(iter, 0) as Source;
            }
            
            return null;
        }
        
        public void ResetHighlight()
        {
            TreeIter iter = TreeIter.Zero;
            
            if(!store.IterNthChild(out iter, SourceManager.ActiveSourceIndex)) {
                return;
            }
             
            Selection.SelectIter(iter);
        }
        
        public Source HighlightedSource {
            get {
                TreeModel model;
                TreeIter iter;
                
                if(!Selection.GetSelected(out model, out iter)) {
                    return null;
                }
                    
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
            
            if(source == null) {
                return;
            }
            
            bool hideCounts = source.Count <= 0;
            
            StateType state = RendererStateToWidgetState(flags);
            Pixbuf icon = null;
            
            if(source == null) {
                return;
            }

            icon = source.Icon;
            if(icon == null) {
                icon = IconThemeUtils.LoadIcon(22, "source-library");
            }
            
            Pango.Layout titleLayout = new Pango.Layout(widget.PangoContext);
            Pango.Layout countLayout = new Pango.Layout(widget.PangoContext);
            
            FontDescription fd = widget.PangoContext.FontDescription.Copy();
            fd.Weight = Selected ? Pango.Weight.Bold : Pango.Weight.Normal;
            if(Italicized) {
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
                titleLayout.GetPixelSize(out titleLayoutWidth, out titleLayoutHeight);
                if(titleLayoutWidth <= maxTitleLayoutWidth) {
                    break;
                }
                
                // FIXME: Gross
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

            drawable.DrawPixbuf(mainGC, icon, 0, 0, 
                cell_area.X + 0, 
                cell_area.Y + ((cell_area.Height - icon.Height) / 2),
                icon.Width, icon.Height,
                RgbDither.None, 0, 0);
        
            drawable.DrawLayout(mainGC, 
                cell_area.X + icon.Width + 6, 
                cell_area.Y + ((cell_area.Height - titleLayoutHeight) / 2) + 1, 
                titleLayout);
            
            if(hideCounts) {
                return;
            }
                
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
            if(view == null) {
                return;
            }
            
            view.EditingRow = false;
            view.UpdateRow(new TreePath(edit.path), edit.Text);
        }
    }
}
