/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  LogCoreViewer.cs
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
using Gtk;
using Mono.Unix;

namespace Banshee.Logging
{
    public class LogCoreViewer : Dialog
    {
        private VBox details_box;
        private TextView details_view;
        private TreeView log_tree;
        private ListStore log_store;
        private LogCore log;
        private LogEntryType filter_type = LogEntryType.None;
        
        private Gdk.Pixbuf error_pixbuf;
        private Gdk.Pixbuf warning_pixbuf;
        private Gdk.Pixbuf debug_pixbuf;
        
        public LogCoreViewer(LogCore log, Window parent) : base(Catalog.GetString("Log Viewer"), 
            parent, DialogFlags.DestroyWithParent | DialogFlags.NoSeparator)
        {
            this.log = log;
            
            Destroyed += OnDestroyed;
            
            TypeHint = Gdk.WindowTypeHint.Utility;
            WindowPosition = WindowPosition.CenterOnParent;
            
            AccelGroup accel_group = new AccelGroup();
            AddAccelGroup(accel_group);       
            Modal = false;
            
            Button button = new Button("gtk-close");
            button.CanDefault = true;
            button.UseStock = true;
            button.Show();
            DefaultResponse = ResponseType.Close;
            button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Escape, 
                0, Gtk.AccelFlags.Visible);
        
            AddActionWidget(button, ResponseType.Close);
            
            BorderWidth = 10;
            
            log_tree = new TreeView();
            
            error_pixbuf = log_tree.RenderIcon(Stock.DialogError, IconSize.SmallToolbar, "Error");
            warning_pixbuf = log_tree.RenderIcon(Stock.DialogWarning, IconSize.SmallToolbar, "Warning");
            debug_pixbuf = log_tree.RenderIcon(Stock.DialogInfo, IconSize.SmallToolbar, "Debug");
            
            log_tree.RulesHint = true;
            
            TreeViewColumn date_column = new TreeViewColumn();
            date_column.Title = Catalog.GetString("Time Stamp");
            CellRendererPixbuf pixbuf_cr = new CellRendererPixbuf();
            date_column.PackStart(pixbuf_cr, false);
            date_column.SetAttributes(pixbuf_cr, "pixbuf", 0);
            
            CellRendererText date_cr = new CellRendererText();
            date_column.PackStart(date_cr, true);
            date_column.SetCellDataFunc(date_cr, delegate (TreeViewColumn tree_column, CellRenderer cell, 
                TreeModel tree_model, TreeIter iter) {
                try {
                    DateTime time = (DateTime)log_store.GetValue(iter, 1);
                    (cell as CellRendererText).Text = time.ToString();
                } catch(Exception) {
                    (cell as CellRendererText).Text = "";
                }
            } as TreeCellDataFunc);
            date_column.SortColumnId = 1;
            
            log_tree.AppendColumn(date_column);
            log_tree.AppendColumn(Catalog.GetString("Message"), new CellRendererText(), "text", 2).SortColumnId = 2;
                                
            log_tree.Model = CreateStore();
            log_tree.CursorChanged += OnCursorChanged;
         
            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(log_tree);
            scroll.ShadowType = ShadowType.In;
            scroll.SetSizeRequest(450, 200);
            
            HBox filter_box = new HBox();
            filter_box.Spacing = 5;
            filter_box.PackStart(new Label(Catalog.GetString("Show:")), false, false, 0);
            ComboBox filter_combo = new ComboBox();
            filter_combo.Changed += OnFilterChanged;
            ListStore filter_model = new ListStore(typeof(Gdk.Pixbuf), typeof(string), typeof(LogEntryType));
            
            CellRendererPixbuf filter_pixbuf_cr = new CellRendererPixbuf();
            CellRendererText filter_text_cr = new CellRendererText();
            filter_combo.Model = filter_model;
            filter_combo.PackStart(filter_pixbuf_cr, false);
            filter_combo.PackEnd(filter_text_cr, true);
            filter_combo.SetAttributes(filter_pixbuf_cr, "pixbuf", 0);
            filter_combo.SetAttributes(filter_text_cr, "text", 1);
            
            filter_box.PackStart(filter_combo, true, true, 0);
            VBox.PackStart(filter_box, false, false, 0);
            filter_box.ShowAll();
            
            filter_model.AppendValues(null, Catalog.GetString("All Log Entries"), LogEntryType.None);
            filter_model.AppendValues(error_pixbuf, Catalog.GetString("Only Error Messages"), LogEntryType.Error);
            filter_model.AppendValues(warning_pixbuf, Catalog.GetString("Only Warning Messages"), LogEntryType.Warning);
            filter_model.AppendValues(debug_pixbuf, Catalog.GetString("Only Debug Messages"), LogEntryType.Debug);

            TreeIter filter_active_iter;
            if(filter_combo.Model.GetIterFirst(out filter_active_iter)) {
                filter_combo.SetActiveIter(filter_active_iter);
            }
            
            VBox.PackStart(scroll, true, true, 0);
            VBox.Spacing = 5;
            scroll.ShowAll();    
            
            details_box = new VBox();
            details_box.Spacing = 2;
            Label details_label = new Label();
            details_label.Xalign = 0.0f;
            details_label.Markup = "<b>" + Catalog.GetString("Entry Details:") + "</b>";
            details_box.PackStart(details_label, false, false, 0);
            
            details_view = new TextView();
            details_view.Editable = false;
            details_view.CursorVisible = false;
            details_view.WrapMode = WrapMode.Word;
            ScrolledWindow details_scroll = new ScrolledWindow();
            details_scroll.Add(details_view);
            details_scroll.ShadowType = ShadowType.In;
            details_scroll.SetSizeRequest(-1, 50);
            details_box.PackStart(details_scroll, true, true, 0);
            details_box.ShowAll();
            VBox.PackStart(details_box, false, false, 0);

            log_tree.HasFocus = true;

            Icon = ThemeIcons.WindowManager;
            log.Updated += OnLogUpdated;
        }
        
        private void OnDestroyed(object o, EventArgs args)
        {
            log.Updated -= OnLogUpdated;
        }
        
        private void OnCursorChanged(object o, EventArgs args)
        {
            TreeIter iter;
            
            if(!log_tree.Selection.GetSelected(out iter)) {
                details_box.Hide();
                return;
            }
            
            object message = log_store.GetValue(iter, 2);
            object details = log_store.GetValue(iter, 3);
            
            if(message == null || details == null) {
                details_box.Hide();
                return;
            }
            
            details_view.Buffer.Text = (message as string) + ": " + (details as string);
            details_box.ShowAll();
        }
        
        private ListStore CreateStore()
        {
            log_store = new ListStore(typeof(Gdk.Pixbuf), typeof(DateTime), 
                typeof(string), typeof(string), typeof(LogEntryType));

            log_store.SetSortFunc(1, delegate(TreeModel model, TreeIter a, TreeIter b) {
                try {
                    DateTime a_stamp = (DateTime)log_store.GetValue(a, 1);
                    DateTime b_stamp = (DateTime)log_store.GetValue(b, 1);
                    return DateTime.Compare(a_stamp, b_stamp);
                } catch(Exception) {
                    return 0;
                }
            });
            
            log_store.SetSortColumnId(1, SortType.Descending);
            return log_store;
        }
        
        private void PopulateStore()
        {
            foreach(LogEntry entry in log) {
                AddEntry(entry, false);
            }
        }
        
        private void OnFilterChanged(object o, EventArgs args)
        {
            try {
                ComboBox combo = (ComboBox)o;
                ListStore store = (ListStore)combo.Model;
                TreeIter active_iter;
                if(!combo.GetActiveIter(out active_iter)) {
                    return;
                }
                
                filter_type = (LogEntryType)store.GetValue(active_iter, 2);
                log_store.Clear();
                PopulateStore();
            } catch(Exception) {
            }
        }
        
        private void OnLogUpdated(object o, LogCoreUpdatedArgs args)
        {
            AddEntry(args.Entry, true);
        }
        
        private void AddEntry(LogEntry entry, bool prepend)
        {
            if(filter_type != LogEntryType.None && entry.Type != filter_type) {
                return;
            }
            
            TreeIter iter = prepend ? log_store.Insert(0) : log_store.Append();
            Gdk.Pixbuf pixbuf = null;
            
            switch(entry.Type) {
                case LogEntryType.Error:
                    pixbuf = error_pixbuf;
                    break;
                case LogEntryType.Warning:
                    pixbuf = warning_pixbuf;
                    break;
                case LogEntryType.Debug:
                    pixbuf = debug_pixbuf;
                    break;
            }
            
            log_store.SetValue(iter, 0, pixbuf);
            log_store.SetValue(iter, 1, entry.TimeStamp);
            log_store.SetValue(iter, 2, entry.ShortMessage);
            log_store.SetValue(iter, 3, entry.Details);
            log_store.SetValue(iter, 4, entry.Type);
        }
    }
}
