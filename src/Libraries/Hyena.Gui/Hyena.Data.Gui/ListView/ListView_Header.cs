//
// ListView_Header.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Mono.Unix;
using Gtk;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : Container
    {
        internal struct CachedColumn
        {
            public static readonly CachedColumn Zero;

            public Column Column;
            public int X1;
            public int X2;
            public int Width;
            public int ResizeX1;
            public int ResizeX2;
            public int Index;
        }
        
        private static Gdk.Cursor resize_x_cursor = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
        private static Gdk.Cursor drag_cursor = new Gdk.Cursor (Gdk.CursorType.Fleur);

        private int resizing_column_index = -1;
        private int pressed_column_index = -1;
        private int pressed_column_x_start = -1;
        private int pressed_column_x_offset = -1;
        private int pressed_column_x_drag = -1;
        private bool pressed_column_is_dragging = false;
        
        private Pango.Layout column_layout;
        
        private CachedColumn [] column_cache;
        
#region Columns
        
        private void InvalidateColumnCache ()
        {
            if (column_cache == null) {
                return;
            }
        
            for (int i = 0; i < column_cache.Length; i++) {
                column_cache[i] = CachedColumn.Zero;
            }
            
            column_cache = null;
        }
        
        private void RegenerateColumnCache ()
        {
            if (!IsRealized) {
                return;
            }
            
            InvalidateColumnCache ();
            
            if (column_controller == null) {
                return;
            }
            
            int i = 0;
            column_cache = new CachedColumn[column_controller.Count];
            
            foreach (Column column in column_controller) {
                if (!column.Visible) {
                    continue;
                }
                
                column_cache[i] = new CachedColumn ();
                column_cache[i].Column = column;
                
                column_cache[i].Width = (int)Math.Round (((double)header_interaction_alloc.Width * column.Width));
                column_cache[i].X1 = i == 0 ? 0 : column_cache[i - 1].X2;
                column_cache[i].X2 = column_cache[i].X1 + column_cache[i].Width;
                column_cache[i].ResizeX1 = column_cache[i].X2;
                column_cache[i].ResizeX2 = column_cache[i].ResizeX1 + 2;
                column_cache[i].Index = i;
                
                i++;
            }
            
            Array.Resize (ref column_cache, i);
        }
        
        protected virtual void OnColumnControllerUpdated ()
        {
            RegenerateColumnCache ();
            InvalidateListView ();
        }
        
        protected virtual void OnColumnRightClicked (Column clickedColumn, int x, int y)
        {
            Menu menu = new Menu ();
            
            if (clickedColumn.Id != null) { // FIXME: Also restrict if the column vis can't be changed
                menu.Append (new ColumnHideMenuItem (clickedColumn));
                menu.Append (new SeparatorMenuItem ());
            }
            
            Column [] columns = ColumnController.ToArray ();
            Array.Sort (columns, delegate (Column a, Column b) {
                // Fully qualified type name to avoid Mono 1.2.4 bug
                return System.String.Compare (a.Title, b.Title);
            });
            
            foreach (Column column in columns) {
                if (column.Id == null) {
                    continue;
                }
                
                menu.Append (new ColumnToggleMenuItem (column));
            }
            
            menu.ShowAll ();
            menu.Popup (null, null, delegate (Menu popup, out int pos_x, out int pos_y, out bool push_in) {
                int win_x, win_y;
                GdkWindow.GetOrigin (out win_x, out win_y);
                
                pos_x = win_x + x;
                pos_y = win_y + y;
                push_in = true;
            }, 3, Gtk.Global.CurrentEventTime);
        }
        
        private void ResizeColumn (double x)
        {
            CachedColumn resizing_column = column_cache[resizing_column_index];

            double resize_delta = x - resizing_column.ResizeX2;
            double subsequent_columns = column_cache.Length - resizing_column.Index - 1;
            double even_distribution = 0.0;
            
            int min_width = 25;
            IHeaderCell header_cell = resizing_column.Column.HeaderCell as IHeaderCell;
            if (header_cell != null) {
                min_width = header_cell.MinWidth;
            }
            
            if (resizing_column.Width + resize_delta < min_width) {
                resize_delta = min_width - resizing_column.Width;
            }
                        
            for (int i = 0; i <= resizing_column_index; i++) {
                even_distribution += column_cache[i].Column.Width * resize_delta;
            }

            even_distribution /= subsequent_columns;

            resizing_column.Column.Width = (resizing_column.Width + resize_delta) / (double)list_rendering_alloc.Width;

            for (int i = resizing_column_index + 1; i < column_cache.Length; i++) {
                column_cache[i].Column.Width = (column_cache[i].Width - 
                    (column_cache[i].Column.Width * resize_delta) - 
                    even_distribution) / (double)list_rendering_alloc.Width;
            }
            
            RegenerateColumnCache ();
            InvalidateListView ();
        }
        
        private Column GetColumnForResizeHandle (int x)
        {
            if (column_cache == null) {
                return null;
            }
            
            foreach (CachedColumn column in column_cache) {
                if (x >= column.ResizeX1 - 2 && 
                    x <= column.ResizeX2 + 2) {
                    return column.Column;
                }
            }
            
            return null;
        }
        
        private Column GetColumnAt (int x)
        {
            if (column_cache == null) {
                return null;
            }
            
            foreach (CachedColumn column in column_cache) {
                if (x >= column.X1 && x <= column.X2) {
                    return column.Column;
                }
            }
            return null;
        }
        
        private CachedColumn GetCachedColumnForColumn (Column col)
        {
            foreach (CachedColumn ca_col in column_cache) {
                if (ca_col.Column == col) {
                    return ca_col;
                }
            }
            
            return CachedColumn.Zero;
        }
                
        private ColumnController column_controller;
        public ColumnController ColumnController {
            get { return column_controller; }
            set { 
                if (column_controller != null) {
                    column_controller.Updated -= OnColumnControllerUpdatedHandler;
                }
                
                column_controller = value;
                
                RegenerateColumnCache ();
                InvalidateListView ();
                
                if (column_controller != null) {
                    column_controller.Updated += OnColumnControllerUpdatedHandler;
                }
            }
        }
        
#endregion

#region Header

        private int header_height = 0;
        private int HeaderHeight {
            get {
                if (!header_visible) {
                    return 0;
                }
                
                if (header_height == 0) {
                    int w;
                    int h;
                    column_layout.SetText ("W");
                    column_layout.GetPixelSize (out w, out h);
                    header_height = h;
                    header_height += 10;
                }
                
                return header_height;
            }
        }
        
        private bool header_visible = true;
        public bool HeaderVisible {
            get { return header_visible; }
            set { 
                header_visible = value;
                MoveResize (Allocation);
            }
        }
        
#endregion

#region Gtk.MenuItem Wrappers for the column context menu

        private class ColumnToggleMenuItem : CheckMenuItem
        {
            private Column column;
            private bool ready = false;
            
            public ColumnToggleMenuItem (Column column) : base (column.Title)
            {
                this.column = column;
                Active = column.Visible; 
                ready = true;
            }
            
            protected override void OnActivated ()
            {
                base.OnActivated ();
                
                if (!ready) {
                    return;
                }
                
                column.Visible = Active;
            }
        }
        
        private class ColumnHideMenuItem : MenuItem
        {
            private Column column;
            
            public ColumnHideMenuItem (Column column) 
                : base (String.Format (Catalog.GetString ("Hide {0}"), column.Title))
            {
                this.column = column;
            }
            
            protected override void OnActivated ()
            {
                column.Visible = false;
            }
        }

#endregion

    }
}
