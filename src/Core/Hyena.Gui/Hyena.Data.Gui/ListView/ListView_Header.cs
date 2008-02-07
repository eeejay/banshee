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
        
        private const int COLUMN_PADDING = 1;
        private static Gdk.Cursor resize_x_cursor = new Gdk.Cursor (Gdk.CursorType.SbHDoubleArrow);
        
        private int column_text_y;
        private int column_text_height;
        private int resizing_column_index = -1;
        private Pango.Layout column_layout;
        
        private CachedColumn [] column_cache;
        
#region Columns
        
        private void InvalidateColumnCache ()
        {
            if (column_cache == null) {
                return;
            }
        
            for (int i = 0; i < column_cache.Length; i++) {
                column_cache[i].Column.VisibilityChanged -= OnColumnVisibilityChanged;
                column_cache[i] = CachedColumn.Zero;
            }
            
            column_cache = null;
        }
        
        private void RegenerateColumnCache ()
        {
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
                column.VisibilityChanged += OnColumnVisibilityChanged;
                
                column_cache[i].Width = (int)Math.Round (((double)list_alloc.Width * column.Width));
                column_cache[i].X1 = i == 0 ? 0 : column_cache[i - 1].X2;
                column_cache[i].X2 = column_cache[i].X1 + column_cache[i].Width;
                column_cache[i].ResizeX1 = column_cache[i].X1 + column_cache[i].Width - COLUMN_PADDING;
                column_cache[i].ResizeX2 = column_cache[i].ResizeX1 + 2;
                column_cache[i].Index = i;
                
                i++;
            }
            
            Array.Resize (ref column_cache, i);
        }
        
        private void OnColumnVisibilityChanged (object o, EventArgs args)
        {
            RegenerateColumnCache ();
            QueueDraw ();
        }
        
        protected virtual void OnColumnControllerUpdated ()
        {
            RegenerateColumnCache ();
            QueueDraw ();
        }
        
        private void ResizeColumn (double x)
        {
            CachedColumn resizing_column = column_cache[resizing_column_index];

            double resize_delta = x - resizing_column.ResizeX2;
            double subsequent_columns = column_cache.Length - resizing_column.Index - 1;
            double even_distribution = 0.0;
            
            int min_width = 25;
            if (resizing_column.Column.HeaderCell is IHeaderCell) {
                min_width = ((IHeaderCell)resizing_column.Column.HeaderCell).MinWidth;
            }
            
            if (resizing_column.Width + resize_delta < min_width) {
                resize_delta = min_width - resizing_column.Width;
            }
                        
            for (int i = 0; i <= resizing_column_index; i++) {
                even_distribution += column_cache[i].Column.Width * resize_delta;
            }

            even_distribution /= subsequent_columns;

            resizing_column.Column.Width = (resizing_column.Width + resize_delta) / (double)list_alloc.Width;

            for (int i = resizing_column_index + 1; i < column_cache.Length; i++) {
                column_cache[i].Column.Width = (column_cache[i].Width - 
                    (column_cache[i].Column.Width * resize_delta) - 
                    even_distribution) / (double)list_alloc.Width;
            }
            
            RegenerateColumnCache ();
            InvalidateHeaderWindow ();
            InvalidateListWindow ();
        }
        
        private Column GetColumnForResizeHandle (int x)
        {
            if (column_cache == null) {
                return null;
            }
            
            foreach (CachedColumn column in column_cache) {
                if (x >= column.ResizeX1 - 2 + left_border_alloc.Width && 
                    x <= column.ResizeX2 + 2 + left_border_alloc.Width ) {
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
                if (x >= column.X1 + left_border_alloc.Width && x <= column.X2 + left_border_alloc.Width) {
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
                QueueDraw ();
                
                if (column_controller != null) {
                    column_controller.Updated += OnColumnControllerUpdatedHandler;
                }
            }
        }
        
#endregion

#region Header
 
        private void ShowHideHeader ()
        {
            if (header_window == null) {
                return;
            }
            
            if (header_visible) {
                header_window.Show ();
            } else {
                header_window.Hide ();
            }
            
            MoveResizeWindows (Allocation);
        }

        private int header_height = 0;
        private int HeaderHeight {
            get {
                if (!header_visible) {
                    return InnerBorderWidth;
                }
                
                if (header_height == 0) {
                    int w_width;
                    column_layout.SetText ("W");
                    column_layout.GetPixelSize (out w_width, out column_text_height);
                    header_height = COLUMN_PADDING * 2 + column_text_height;
                    column_text_y = (header_height / 2) - (column_text_height / 2) - 2;
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
                ShowHideHeader ();
            }
        }
        
#endregion

    }
}
