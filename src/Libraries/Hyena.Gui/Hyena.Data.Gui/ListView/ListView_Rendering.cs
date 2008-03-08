//
// ListView_Rendering.cs
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
using System.Collections.Generic;

using Gtk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;
using GtkColorClass=Hyena.Gui.Theming.GtkColorClass;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : Container
    {
        private const int InnerBorderWidth = 4;
        private const int FooterHeight = InnerBorderWidth;
        
        private Theme theme;
        protected Theme Theme {
            get { return theme; }
        }

        private Cairo.Context list_cr;
        private Cairo.Context header_cr;
        private Cairo.Context footer_cr;
        private Cairo.Context left_border_cr;
        private Cairo.Context right_border_cr;
        
        private Pango.Layout header_pango_layout;
        private Pango.Layout list_pango_layout;

        public new void QueueDraw ()
        {
            base.QueueDraw ();
            
            InvalidateHeaderWindow ();
            InvalidateListWindow ();
            InvalidateFooterWindow ();
        }

        protected virtual void ChildClassPostRender (Gdk.EventExpose evnt, Cairo.Context cr, Gdk.Rectangle clip)
        {
        }
         
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {            
            foreach (Gdk.Rectangle rect in evnt.Region.GetRectangles ()) {
                PaintRegion (evnt, rect);
            }
            
            return true;
        }
                
        private void PaintRegion (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Cairo.Context cr = CairoHelper.CreateCairoDrawable (evnt.Window);
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();

            if (evnt.Window == header_window) {
                header_cr = cr;
                if (header_pango_layout == null) {
                    header_pango_layout = Pango.CairoHelper.CreateLayout (header_cr);
                }
                PaintHeader (evnt.Area);
            } else if (evnt.Window == footer_window) {
                footer_cr = cr;
                PaintFooter (evnt, clip);
            } else if (evnt.Window == left_border_window) {
                left_border_cr = cr;
                PaintLeftBorder(evnt, clip);
            } else if (evnt.Window == right_border_window) {
                right_border_cr = cr;
                PaintRightBorder(evnt, clip);
            } else if (evnt.Window == list_window) {
                list_cr = cr;
                if (list_pango_layout == null) {
                    list_pango_layout = Pango.CairoHelper.CreateLayout (list_cr);
                }
                PaintList (evnt, clip);
            }

            ChildClassPostRender (evnt, cr, clip);
            
            ((IDisposable)cr.Target).Dispose ();
            ((IDisposable)cr).Dispose ();
        }
        
        private void PaintHeader (Gdk.Rectangle clip)
        {
            Theme.DrawHeaderBackground (header_cr, header_alloc, 2, header_visible);
            
            if (column_controller == null || !header_visible) {
                return;
            }
                
            Gdk.Rectangle cell_area = new Gdk.Rectangle ();
            cell_area.Y = column_text_y;
            cell_area.Height = HeaderHeight - column_text_y;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }
                
                cell_area.X = column_cache[ci].X1 + left_border_alloc.Width;
                cell_area.Width = column_cache[ci].Width - COLUMN_PADDING;
                PaintHeaderCell (cell_area, clip, ci, false);
            }
            
            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.X = pressed_column_x_drag + left_border_alloc.Width;
                cell_area.Width = column_cache[pressed_column_index].Width - COLUMN_PADDING;
                PaintHeaderCell (cell_area, clip, pressed_column_index, true);
            }
        }
        
        private void PaintHeaderCell (Gdk.Rectangle area, Gdk.Rectangle clip, int ci, bool dragging)
        {
            ColumnCell cell = column_cache[ci].Column.HeaderCell;
            
            if (dragging) {
                if (ci < column_cache.Length - 1) {
                    Theme.DrawHeaderSeparator (header_cr, header_alloc, 
                        column_cache[ci].ResizeX1 - 1 + left_border_alloc.Width, 2);
                }
            
                Theme.DrawColumnHighlight (header_cr, area, 3, 
                    CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Normal), 0.9));
                    
                Cairo.Color stroke_color = CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (
                    GtkColorClass.Base, StateType.Normal), 0.0);
                stroke_color.A = 0.3;
                
                header_cr.Color = stroke_color;
                header_cr.MoveTo (area.X - 1.0, area.Y + 1.0);
                header_cr.LineTo (area.X - 1.0, area.Y + area.Height);
                header_cr.MoveTo (area.X + area.Width, area.Y + 1.0);
                header_cr.LineTo (area.X + area.Width, area.Y + area.Height);
                header_cr.Stroke ();
            }
            
            if (cell is ColumnHeaderCellText && Model is ISortable) {
                bool has_sort = ((ISortable)Model).SortColumn == column_cache[ci].Column as ISortableColumn 
                    && column_cache[ci].Column is ISortableColumn;
                ((ColumnHeaderCellText)cell).HasSort = has_sort;
                if (has_sort) {
                    Theme.DrawColumnHighlight (header_cr, area, 3);
                }
            }
            
            if (cell != null) {
                header_cr.Save ();
                header_cr.Translate (area.X, area.Y);
                cell.Render (new CellContext (header_cr, header_pango_layout, this, header_window, 
                    theme, area), StateType.Normal, area.Width, area.Height);
                header_cr.Restore ();
            }
            
            if (!dragging && ci < column_cache.Length - 1) {
                Theme.DrawHeaderSeparator (header_cr, header_alloc, 
                    column_cache[ci].ResizeX1 - 1 + left_border_alloc.Width, 2);
            }
        }

        private void PaintList (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            if (model == null) {
                return;
            }

            int vadjustment_value = (int)vadjustment.Value;
            int first_row = vadjustment_value / RowHeight;
            int last_row = Math.Min (model.Count, first_row + RowsInView);     

            Gdk.Rectangle selected_focus_alloc = Gdk.Rectangle.Zero;
            Gdk.Rectangle single_list_alloc = new Gdk.Rectangle ();
            
            single_list_alloc.Width = list_alloc.Width;
            single_list_alloc.Height = RowHeight;
            single_list_alloc.X = list_alloc.X;
            single_list_alloc.Y = list_alloc.Y - vadjustment_value + (first_row * single_list_alloc.Height);
            
            int selection_height = 0;
            int selection_y = 0;
            List<int> selected_rows = new List<int> ();

            for (int ri = first_row; ri < last_row; ri++) {
                if (Selection.Contains (ri)) {
                    if (selection_height == 0) {
                        selection_y = single_list_alloc.Y;
                    }
                    
                    selection_height += single_list_alloc.Height;
                    selected_rows.Add (ri);
                    
                    if (focused_row_index == ri) {
                        selected_focus_alloc = single_list_alloc;
                    }
                } else {
                    if (rules_hint && ri % 2 != 0) {
                        Theme.DrawRowRule (list_cr, single_list_alloc.X, single_list_alloc.Y, 
                            single_list_alloc.Width, single_list_alloc.Height);
                    }
                    
                    if (ri == drag_reorder_row_index && Reorderable) {
                        list_cr.Save ();
                        list_cr.LineWidth = 1.0;
                        list_cr.Antialias = Antialias.None;
                        list_cr.MoveTo (single_list_alloc.Left, single_list_alloc.Top);
                        list_cr.LineTo (single_list_alloc.Right, single_list_alloc.Top);
                        list_cr.Color = Theme.Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal);
                        list_cr.Stroke ();
                        list_cr.Restore ();
                    }
                    
                    if (focused_row_index == ri && !Selection.Contains (ri) && HasFocus) {
                        CairoCorners corners = CairoCorners.All;
                        
                        if (Selection.Contains (ri - 1)) {
                            corners ^= CairoCorners.TopLeft | CairoCorners.TopRight;
                        }
                        
                        if (Selection.Contains (ri + 1)) {
                            corners ^= CairoCorners.BottomLeft | CairoCorners.BottomRight;
                        }
                        
                        Theme.DrawRowSelection (list_cr, single_list_alloc.X, single_list_alloc.Y, 
                            single_list_alloc.Width, single_list_alloc.Height, false, true, 
                            Theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), corners);
                    }
                    
                    if (selection_height > 0) {
                        Theme.DrawRowSelection (
                            list_cr, list_alloc.X, list_alloc.Y + selection_y, list_alloc.Width, selection_height);
                        selection_height = 0;
                    }
                    
                    PaintRow (ri, clip, single_list_alloc, StateType.Normal);
                }
                
                single_list_alloc.Y += single_list_alloc.Height;
            }
            
            if (selection_height > 0) {
                Theme.DrawRowSelection (list_cr, list_alloc.X, list_alloc.Y + selection_y, 
                    list_alloc.Width, selection_height);
            }
            
            if (Selection.Count > 1 && !selected_focus_alloc.Equals (Gdk.Rectangle.Zero) && HasFocus) {
                Theme.DrawRowSelection (list_cr, selected_focus_alloc.X, selected_focus_alloc.Y, 
                    selected_focus_alloc.Width, selected_focus_alloc.Height, false, true, 
                    Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Selected));
            }
            
            foreach (int ri in selected_rows) {
                single_list_alloc.Y = ri * single_list_alloc.Height - vadjustment_value;
                PaintRow (ri, clip, single_list_alloc, StateType.Selected);
            }
            
            PaintDraggingColumn (evnt, clip);
        }

        private void PaintRow (int row_index, Gdk.Rectangle clip, Gdk.Rectangle area, StateType state)
        {
            if (column_cache == null) {
                return;
            }
            
            object item = model[row_index];
            bool sensitive = IsRowSensitive (item);
            
            Gdk.Rectangle cell_area = new Gdk.Rectangle ();
            cell_area.Height = RowHeight;
            cell_area.Y = area.Y;
            
            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }
                
                cell_area.Width = column_cache[ci].Width;
                cell_area.X = column_cache[ci].X1;
                PaintCell (item, ci, row_index, cell_area, cell_area, sensitive ? state : StateType.Insensitive, false);
            }
            
            if (pressed_column_is_dragging && pressed_column_index >= 0) {   
                cell_area.Width = column_cache[pressed_column_index].Width;
                cell_area.X = pressed_column_x_drag;
                PaintCell (item, pressed_column_index, row_index, cell_area, cell_area, state, true);
            }
        }
        
        private void PaintCell (object item, int column_index, int row_index, Gdk.Rectangle area, 
            Gdk.Rectangle clip, StateType state, bool dragging)
        {
            ColumnCell cell = column_cache[column_index].Column.GetCell (0);
            cell.BindListItem (item);
            
            if (dragging) {
                Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
                fill_color.A = 0.5;
                list_cr.Color = fill_color;
                list_cr.Rectangle (area.X, area.Y, area.Width, area.Height);
                list_cr.Fill ();
            }
            
            list_cr.Save ();
            list_cr.Translate (clip.X, clip.Y);
            cell.Render (new CellContext (list_cr, list_pango_layout, this, list_window, theme, area), 
                dragging ? StateType.Normal : state, area.Width, area.Height);
            list_cr.Restore ();
        }
        
        private void PaintDraggingColumn (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            if (!pressed_column_is_dragging || pressed_column_index < 0) {
                return;
            }
            
            CachedColumn column = column_cache[pressed_column_index];
            
            int x = pressed_column_x_drag;
            
            Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            fill_color.A = 0.45;
            
            Cairo.Color stroke_color = CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (
                GtkColorClass.Base, StateType.Normal), 0.0);
            stroke_color.A = 0.3;
            
            list_cr.Rectangle (x, list_alloc.Y, column.Width, list_alloc.Height);
            list_cr.Color = fill_color;
            list_cr.Fill ();
            
            list_cr.MoveTo (x, list_alloc.Y);
            list_cr.LineTo (x, list_alloc.Y + list_alloc.Height - 1.0);
            list_cr.LineTo (x + column.Width, list_alloc.Y + list_alloc.Height - 1.0);
            list_cr.LineTo (x + column.Width, list_alloc.Y);
            
            list_cr.Color = stroke_color;
            list_cr.Antialias = Cairo.Antialias.None;
            list_cr.LineWidth = 1.0;
            list_cr.Stroke ();
        }
        
        private void PaintLeftBorder (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Theme.DrawLeftBorder (left_border_cr, left_border_alloc);
        }
        
        private void PaintRightBorder (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Theme.DrawRightBorder (right_border_cr, right_border_alloc);
        }
        
        private void PaintFooter (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Theme.DrawFooter (footer_cr, footer_alloc);
        }
        
        protected void InvalidateListWindow ()
        {
            if (list_window != null) {
                list_window.InvalidateRect (list_alloc, false);
            }
        }
        
        protected void InvalidateHeaderWindow()
        {
            if (header_window != null) {
                header_window.InvalidateRect (header_alloc, false);
            }
        }
        
        protected void InvalidateFooterWindow ()
        {
            if (footer_window != null) {
                footer_window.InvalidateRect (footer_alloc, false);
            }
        }
        
        private bool rules_hint = false;
        public bool RulesHint {
            get { return rules_hint; }
            set { 
                rules_hint = value; 
                InvalidateListWindow();
            }
        }
        
        private int row_height = 0;
        protected int RowHeight {
            get {
                if (row_height == 0) {
                    int w_width;
                    Pango.Layout layout = new Pango.Layout (PangoContext);
                    layout.SetText ("W");
                    layout.GetPixelSize (out w_width, out row_height);
                    row_height += 8;
                }
                
                return row_height;
            }
            
            set { row_height = value; }
        }
    }
}
