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

using Gtk;
using Cairo;
using Mono.Unix;

using Hyena.Gui.Theming;
using Hyena.Gui.Theatrics;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;

using Banshee.Gui;

namespace Banshee.Sources.Gui
{
    // Note: This is a partial class - the drag and drop code is split
    //       out into a separate file to make this class more manageable.
    //       See SourceView_DragAndDrop.cs for the DnD code.

    public partial class SourceView : TreeView
    {
        private SourceRowRenderer renderer;
        private Theme theme;
        private Cairo.Context cr;
        
        private Stage<TreeIter> notify_stage = new Stage<TreeIter> (2000);
        
        private TreeStore store;
        private TreeViewColumn focus_column;
        private TreePath highlight_path;
        private int current_timeout = -1;
        private bool editing_row = false;

        public SourceView ()
        {
            BuildColumns ();
            BuildModel ();
            ConfigureDragAndDrop ();
            RefreshList ();
            ConnectEvents ();
            
            RowSeparatorFunc = RowSeparatorHandler;
        }
        
#region Setup Methods        
        
        private void BuildColumns ()
        {
            // Hidden expander column
            TreeViewColumn col = new TreeViewColumn ();
            col.Visible = false;
            AppendColumn (col);
            ExpanderColumn = col;
        
            focus_column = new TreeViewColumn ();
            renderer = new SourceRowRenderer ();
            focus_column.PackStart (renderer, true);
            focus_column.SetCellDataFunc (renderer, new TreeCellDataFunc (SourceCellDataFunc));
            AppendColumn (focus_column);
            
            HeadersVisible = false;
        }
        
        private void BuildModel ()
        {
            store = new TreeStore (typeof (Source), typeof (int), typeof (bool));
            store.SetSortColumnId (1, SortType.Ascending);
            store.ChangeSortColumn ();
            Model = store;
        }
        
        private void ConnectEvents ()
        {
            ServiceManager.SourceManager.SourceAdded += delegate (SourceAddedArgs args) {
                AddSource (args.Source);
            };
            
            ServiceManager.SourceManager.SourceRemoved += delegate (SourceEventArgs args) {
                RemoveSource (args.Source);
            };
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate (SourceEventArgs args) {
                ResetSelection ();
            };
            
            ServiceManager.SourceManager.SourceUpdated += delegate (SourceEventArgs args) {
                Banshee.Base.ThreadAssist.ProxyToMain (delegate {
                    lock (args.Source) {
                        TreeIter iter = FindSource (args.Source);
                        if (!TreeIter.Zero.Equals (iter)) {
                            store.SetValue (iter, 1, args.Source.Order);
                            QueueDraw ();
                        }
                    }
                });
            };
            
            ServiceManager.PlaybackController.SourceChanged += delegate {
                QueueDraw ();
            };
            
            notify_stage.ActorStep += delegate (Actor<TreeIter> actor) {
                if (!store.IterIsValid (actor.Target)) {
                    return false;
                }
                
                Gdk.Rectangle rect = GetBackgroundArea (store.GetPath (actor.Target), focus_column);
                QueueDrawArea (rect.X, rect.Y, rect.Width, rect.Height);
                return true;
            };
        }
        
#endregion

#region Gtk.Widget Overrides
        
        protected override void OnRealized ()
        {
            base.OnRealized ();
            
            theme = new GtkTheme (this);
            // theme.RefreshColors ();
        }

        protected override bool OnButtonPressEvent (Gdk.EventButton press)
        {
            TreePath path;
            TreeViewColumn column;
                       
            if (press.Button == 1) {
                ResetHighlight ();
            }
            
            // If there is not a row at the click position let the base handler take care of the press
            if (!GetPathAtPos ((int)press.X, (int)press.Y, out path, out column)) {
                return base.OnButtonPressEvent (press);
            }

            Source source = GetSource (path);

            // From F-Spot's SaneTreeView class
            int expander_size = (int) StyleGetProperty ("expander-size");
            int horizontal_separator = (int) StyleGetProperty ("horizontal-separator");
            bool on_expander = (press.X <= (horizontal_separator * 2 + path.Depth * expander_size));

            if (on_expander) {
                bool ret = base.OnButtonPressEvent (press);
                // If the active source is a child of this source, and we are about to collapse it, switch
                // the active source to the parent.
                if (source == ServiceManager.SourceManager.ActiveSource.Parent && GetRowExpanded (path)) {
                    ServiceManager.SourceManager.SetActiveSource (source);
                }
                return ret;
            }

            // For Sources that can't be activated, when they're clicked just 
            // expand or collapse them and return.
            if (press.Button == 1 && !source.CanActivate) {
                if (!source.Expanded) {
                    ExpandRow (path, false);
                } else {
                    CollapseRow (path);
                }
                return false;
            }

            if (press.Button == 3) {
                HighlightPath (path);
                OnPopupMenu ();
                return true;
            }

            if (press.Button == 1) {
                if (ServiceManager.SourceManager.ActiveSource != source) {
                    ServiceManager.SourceManager.SetActiveSource (source);
                }
            }
            
            if ((press.State & Gdk.ModifierType.ControlMask) != 0) {
                if (press.Type == Gdk.EventType.TwoButtonPress && press.Button == 1) {
                    ActivateRow (path, null);
                }
                return true;
            }
            
            return base.OnButtonPressEvent (press);
        }

        protected override bool OnPopupMenu ()
        {
            ServiceManager.Get<InterfaceActionService> ().SourceActions["SourceContextMenuAction"].Activate ();
            return true;
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            try {
                cr = Gdk.CairoHelper.Create (evnt.Window);
                return base.OnExposeEvent (evnt);
            } finally {
                ((IDisposable)cr.Target).Dispose ();
                ((IDisposable)cr).Dispose ();
                cr = null;
            }
        }

#endregion

#region Gtk.TreeView Overrides
        
        protected override void OnRowExpanded (TreeIter iter, TreePath path)
        {
            base.OnRowExpanded (iter, path);
            GetSource (iter).Expanded = true;
        }
        
        protected override void OnRowCollapsed (TreeIter iter, TreePath path)
        {
            base.OnRowCollapsed (iter, path);
            GetSource (iter).Expanded = false;
        }
        
        protected override void OnCursorChanged ()
        {
            if (current_timeout < 0) {
                current_timeout = (int)GLib.Timeout.Add (200, OnCursorChangedTimeout);
            }
        }
        
        private bool OnCursorChangedTimeout ()
        {
            TreeIter iter;
            TreeModel model;
            
            current_timeout = -1;
            
            if (!Selection.GetSelected (out model, out iter)) {
                return false;
            }
            
            Source new_source = store.GetValue (iter, 0) as Source;
            if (ServiceManager.SourceManager.ActiveSource == new_source) {
                return false;
            }
            
            ServiceManager.SourceManager.SetActiveSource (new_source);
            
            QueueDraw ();

            return false;
        }
        
        private bool RowSeparatorHandler (TreeModel model, TreeIter iter)
        {
            return (bool)store.GetValue (iter, 2);
        }
        
#endregion

#region Source <-> Iter Methods

        public Source GetSource (TreeIter iter)
        {
            return store.GetValue (iter, 0) as Source;
        }
        
        public Source GetSource (TreePath path)
        {
            TreeIter iter;
        
            if (store.GetIter (out iter, path)) {
                return GetSource (iter);
            }
        
            return null;
        }

        private TreeIter FindSource (Source source)
        {
            foreach (TreeIter iter in FindInModel (0, source)) {
                return iter;
            }
            
            return TreeIter.Zero;
        }
        
        private IEnumerable<TreeIter> FindInModel (int column, object match)
        {
            TreeIter iter = TreeIter.Zero;
            store.GetIterFirst (out iter);
            return FindInModel (column, match, iter);
        }
        
        private IEnumerable<TreeIter> FindInModel (int column, object match, TreeIter iter)
        {
            if (!store.IterIsValid (iter)) {
                yield break;
            }
            
            do {
                object result = store.GetValue (iter, column);
                Type result_type = result != null ? result.GetType () : null;
                if (result_type != null && ((result_type.IsValueType && result.Equals (match)) || result == match)) {
                    yield return iter;
                }
                
                if (store.IterHasChild (iter)) {
                    TreeIter citer = TreeIter.Zero;
                    store.IterChildren (out citer, iter);
                    foreach (TreeIter yiter in FindInModel (column, match, citer)) {
                        if (!yiter.Equals (TreeIter.Zero)) {
                            yield return yiter;
                        }
                    }
                }
            } while (store.IterNext (ref iter));
        }
        
        /*private void AddRowSeparator (int order)
        {
            TreeIter iter = store.InsertNode (order);
            
            store.SetValue (iter, 0, null);
            store.SetValue (iter, 1, order);
            store.SetValue (iter, 2, true);
        }
        
        private void ClearRowSeparators ()
        {
            Queue<TreeIter> to_remove = new Queue<TreeIter> ();
            foreach (TreeIter iter in FindInModel (2, true)) {
                to_remove.Enqueue (iter);
            }
            
            while (to_remove.Count > 0) {
                TreeIter iter = to_remove.Dequeue ();
                store.Remove (ref iter);
            }
        }*/
        
#endregion

#region Add/Remove Sources / SourceManager interaction

        private void AddSource (Source source)
        {
            AddSource (source, TreeIter.Zero);
        }

        private void AddSource (Source source, TreeIter parent)
        {
            // Don't add duplicates
            if (!FindSource (source).Equals (TreeIter.Zero)) {
                return;
            }
            
            // Don't add a child source before its parent
            if (parent.Equals (TreeIter.Zero) && source.Parent != null) {
                return;
            }
            
            int position = source.Order;
            
            TreeIter iter = parent.Equals (TreeIter.Zero)
                ? store.InsertNode (position) 
                : store.InsertNode (parent, position);
            
            store.SetValue (iter, 0, source);
            store.SetValue (iter, 1, source.Order);
            store.SetValue (iter, 2, false);

            lock (source.Children) {
                foreach (Source child in source.Children) {
                    AddSource (child, iter);
                }
            }
            
            source.ChildSourceAdded += OnSourceChildSourceAdded; 
            source.ChildSourceRemoved += OnSourceChildSourceRemoved;
            source.UserNotifyUpdated += OnSourceUserNotifyUpdated;
           
            if (source.Expanded || source.AutoExpand == true) {
                Expand (iter);
            } else if (source.Parent != null && source.Parent.AutoExpand == true) {
                Expand (FindSource (source.Parent));
            }
            
            UpdateView ();
        }

        private void RemoveSource (Source source)
        {
            TreeIter iter = FindSource (source);
            if (!iter.Equals (TreeIter.Zero)) {
                store.Remove (ref iter);
            }
            
            source.ChildSourceAdded -= OnSourceChildSourceAdded;
            source.ChildSourceRemoved -= OnSourceChildSourceRemoved;
            source.UserNotifyUpdated -= OnSourceUserNotifyUpdated;

            UpdateView ();
        }
    
        private void Expand (TreeIter iter)
        {
            TreePath path = store.GetPath (iter);
            ExpandRow (path, true);
        }
        
        private void RefreshList ()
        {
            store.Clear ();
            foreach (Source source in ServiceManager.SourceManager.Sources) {
                AddSource (source);
            }
        }
        
        private void OnSourceChildSourceAdded (SourceEventArgs args)
        {
            AddSource (args.Source, FindSource (args.Source.Parent));
        }
        
        private void OnSourceChildSourceRemoved (SourceEventArgs args)
        {
            RemoveSource (args.Source);
        }

        private void OnSourceUserNotifyUpdated (object o, EventArgs args)
        {
            Banshee.Base.ThreadAssist.ProxyToMain (delegate {
                TreeIter iter = FindSource ((Source)o);
                if (iter.Equals (TreeIter.Zero)) {
                    return;
                }
                
                notify_stage.AddOrReset (iter);
            });
        }
        
#endregion

#region List/View Utility Methods

        private bool UpdateView ()
        {
            for (int i = 0, m = store.IterNChildren (); i < m; i++) {
                TreeIter iter = TreeIter.Zero;
                if (!store.IterNthChild (out iter, i)) {
                    continue;
                }
                
                if (store.IterNChildren (iter) > 0) {
                    ExpanderColumn = Columns[1];
                    return true;
                }
            }
        
            ExpanderColumn = Columns[0];
            return false;
        }
        
        internal void UpdateRow (TreePath path, string text)
        {
            TreeIter iter;
            
            if (!store.GetIter (out iter, path)) {
                return;
            }
            
            Source source = store.GetValue (iter, 0) as Source;
            source.Rename (text);
        }
        
        public void BeginRenameSource (Source source)
        {
            TreeIter iter = FindSource (source);
            if (iter.Equals (TreeIter.Zero)) {
                return;
            }
            
            renderer.Editable = true;
            SetCursor (store.GetPath (iter), focus_column, true);
            renderer.Editable = false;
        }
        
        private void SourceCellDataFunc (TreeViewColumn tree_column, CellRenderer cell,  
            TreeModel tree_model, TreeIter iter)
        {
            SourceRowRenderer renderer = (SourceRowRenderer)cell;
            renderer.view = this;
            renderer.source = (Source)store.GetValue (iter, 0);
            renderer.path = store.GetPath (iter);
            
            if (renderer.source == null) {
                return;
            }
            
            renderer.Sensitive = renderer.source.CanActivate;
        }
        
        private void ResetSelection ()
        {
            TreeIter iter = FindSource (ServiceManager.SourceManager.ActiveSource);
            
            if (!iter.Equals (TreeIter.Zero)){
                Selection.SelectIter (iter);
            }
        }
        
        public void HighlightPath (TreePath path)
        {
            highlight_path = path;
            QueueDraw ();
        }
        
        public void ResetHighlight ()
        {   
            highlight_path = null;
            QueueDraw ();
        }
        
#endregion

#region Public Properties
                
        public Source HighlightedSource {
            get {
                TreeIter iter;
                
                if (highlight_path == null || !store.GetIter (out iter, highlight_path)) {
                    return null;
                }
                    
                return store.GetValue (iter, 0) as Source;
            }
        }

        public bool EditingRow {
            get { return editing_row; }
            set { 
                editing_row = value;
                QueueDraw (); 
            }
        }
        
#endregion

#region Internal Properties
      
        internal TreePath HighlightedPath {
            get { return highlight_path; }
        }
        
        internal Cairo.Context Cr {
            get { return cr; }
        }
        
        internal Theme Theme {
            get { return theme; }
        }
        
        internal Stage<TreeIter> NotifyStage {
            get { return notify_stage; }
        }
        
        internal Source NewPlaylistSource {
            get {
                return new_playlist_source ??
                    new_playlist_source = new PlaylistSource (Catalog.GetString ("New Playlist"), ServiceManager.SourceManager.MusicLibrary.DbId);
            }
        }

#endregion        

    }
}
