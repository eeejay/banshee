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
using Gdk;
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;
using GtkColorClass=Hyena.Gui.Theming.GtkColorClass;

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : Container
    {
        private Theme theme;
        protected Theme Theme {
            get { return theme; }
        }

        private Cairo.Context cairo_context;
        
        private Pango.Layout header_pango_layout;
        private Pango.Layout list_pango_layout;

        protected virtual void ChildClassPostRender (Gdk.EventExpose evnt, Cairo.Context cr, Gdk.Rectangle clip)
        {
        }
         
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {         
            cairo_context = Gdk.CairoHelper.Create (evnt.Window);
            
            Gdk.Rectangle damage = new Gdk.Rectangle ();
            foreach (Gdk.Rectangle rect in evnt.Region.GetRectangles ()) {
                damage = damage.Union (rect);
            }
            PaintRegion (evnt, damage);
            
            ((IDisposable)cairo_context.Target).Dispose ();
            ((IDisposable)cairo_context).Dispose ();
            
            return true;
        }
                
        private void PaintRegion (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Theme.DrawFrameBackground (cairo_context, Allocation, true);
            if (header_visible && column_controller != null) {
                PaintHeader (clip);
            }
            PaintList (evnt, clip);
            Theme.DrawFrameBorder (cairo_context, Allocation);
            cairo_context.Translate (list_rendering_alloc.X, list_rendering_alloc.Y);
            ChildClassPostRender (evnt, cairo_context, clip);
        }
        
        private void PaintHeader (Gdk.Rectangle clip)
        {
            Gdk.Rectangle rect = header_rendering_alloc;
            rect.Height += Theme.BorderWidth;
            clip.Intersect (rect);
            cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cairo_context.Clip ();
            
            header_pango_layout = PangoCairoHelper.CreateLayout (cairo_context);
            Theme.DrawHeaderBackground (cairo_context, header_rendering_alloc);
            
            Gdk.Rectangle cell_area = new Gdk.Rectangle ();
            cell_area.Y = header_rendering_alloc.Y;
            cell_area.Height = header_rendering_alloc.Height;

            for (int ci = 0; ci < column_cache.Length; ci++) {
                if (pressed_column_is_dragging && pressed_column_index == ci) {
                    continue;
                }
                
                cell_area.X = column_cache[ci].X1 + Theme.TotalBorderWidth + header_rendering_alloc.X;
                cell_area.Width = column_cache[ci].Width;
                PaintHeaderCell (cell_area, clip, ci, false);
            }
            
            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.X = pressed_column_x_drag + Allocation.X;
                cell_area.Width = column_cache[pressed_column_index].Width;
                PaintHeaderCell (cell_area, clip, pressed_column_index, true);
            }
            
            cairo_context.ResetClip ();
        }
        
        private void PaintHeaderCell (Gdk.Rectangle area, Gdk.Rectangle clip, int ci, bool dragging)
        {
            ColumnCell cell = column_cache[ci].Column.HeaderCell;
            
            if (dragging) {
                Theme.DrawColumnHighlight (cairo_context, area, 
                    CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Normal), 0.9));
                    
                Cairo.Color stroke_color = CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (
                    GtkColorClass.Base, StateType.Normal), 0.0);
                stroke_color.A = 0.3;
                
                cairo_context.Color = stroke_color;
                cairo_context.MoveTo (area.X + 0.5, area.Y + 1.0);
                cairo_context.LineTo (area.X + 0.5, area.Bottom);
                cairo_context.MoveTo (area.Right - 0.5, area.Y + 1.0);
                cairo_context.LineTo (area.Right - 0.5, area.Bottom);
                cairo_context.Stroke ();
            }
            
            ColumnHeaderCellText column_cell = cell as ColumnHeaderCellText;
            ISortable sortable = Model as ISortable;
            if (column_cell != null && sortable != null) {
                ISortableColumn sort_column = column_cache[ci].Column as ISortableColumn;
                column_cell.HasSort = sort_column != null && sortable.SortColumn == sort_column;
            }
            
            if (cell != null) {
                cairo_context.Save ();
                cairo_context.Translate (area.X, area.Y);
                cell.Render (new CellContext (cairo_context, header_pango_layout, this, GdkWindow, 
                    theme, area, clip), StateType.Normal, area.Width, area.Height);
                cairo_context.Restore ();
            }
            
            if (!dragging && ci < column_cache.Length - 1) {
                Theme.DrawHeaderSeparator (cairo_context, area, area.Right);
            }
        }

        private void PaintList (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            if (model == null) {
                return;
            }
            
            clip.Intersect (list_rendering_alloc);
            cairo_context.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cairo_context.Clip ();
            
            list_pango_layout = PangoCairoHelper.CreateLayout (cairo_context);

            int vadjustment_value = (int)vadjustment.Value;
            int first_row = vadjustment_value / RowHeight;
            int last_row = Math.Min (model.Count, first_row + RowsInView);     

            Gdk.Rectangle selected_focus_alloc = Gdk.Rectangle.Zero;
            Gdk.Rectangle single_list_alloc = new Gdk.Rectangle ();
            
            single_list_alloc.Width = list_rendering_alloc.Width;
            single_list_alloc.Height = RowHeight;
            single_list_alloc.X = list_rendering_alloc.X;
            single_list_alloc.Y = list_rendering_alloc.Y - vadjustment_value + (first_row * single_list_alloc.Height);
            
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
                        Theme.DrawRowRule (cairo_context, single_list_alloc.X, single_list_alloc.Y, 
                            single_list_alloc.Width, single_list_alloc.Height);
                    }
                    
                    if (ri == drag_reorder_row_index && Reorderable) {
                        cairo_context.Save ();
                        cairo_context.LineWidth = 1.0;
                        cairo_context.Antialias = Antialias.None;
                        cairo_context.MoveTo (single_list_alloc.Left, single_list_alloc.Top);
                        cairo_context.LineTo (single_list_alloc.Right, single_list_alloc.Top);
                        cairo_context.Color = Theme.Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal);
                        cairo_context.Stroke ();
                        cairo_context.Restore ();
                    }
                    
                    if (focused_row_index == ri && !Selection.Contains (ri) && HasFocus) {
                        CairoCorners corners = CairoCorners.All;
                        
                        if (Selection.Contains (ri - 1)) {
                            corners ^= CairoCorners.TopLeft | CairoCorners.TopRight;
                        }
                        
                        if (Selection.Contains (ri + 1)) {
                            corners ^= CairoCorners.BottomLeft | CairoCorners.BottomRight;
                        }
                        
                        Theme.DrawRowSelection (cairo_context, single_list_alloc.X, single_list_alloc.Y, 
                            single_list_alloc.Width, single_list_alloc.Height, false, true, 
                            Theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), corners);
                    }
                    
                    if (selection_height > 0) {
                        Theme.DrawRowSelection (
                            cairo_context, list_rendering_alloc.X, selection_y, list_rendering_alloc.Width, selection_height);
                        selection_height = 0;
                    }
                    
                    PaintRow (ri, clip, single_list_alloc, StateType.Normal);
                }
                
                single_list_alloc.Y += single_list_alloc.Height;
            }
            
            if (selection_height > 0) {
                Theme.DrawRowSelection (cairo_context, list_rendering_alloc.X, selection_y, 
                    list_rendering_alloc.Width, selection_height);
            }
            
            if (Selection.Count > 1 && !selected_focus_alloc.Equals (Gdk.Rectangle.Zero) && HasFocus) {
                Theme.DrawRowSelection (cairo_context, selected_focus_alloc.X, selected_focus_alloc.Y, 
                    selected_focus_alloc.Width, selected_focus_alloc.Height, false, true, 
                    Theme.Colors.GetWidgetColor (GtkColorClass.Dark, StateType.Selected));
            }
            
            foreach (int ri in selected_rows) {
                single_list_alloc.Y = list_rendering_alloc.Y + ri * single_list_alloc.Height - vadjustment_value;
                PaintRow (ri, clip, single_list_alloc, StateType.Selected);
            }
            
            cairo_context.ResetClip ();
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
                cell_area.X = column_cache[ci].X1 + area.X;
                PaintCell (item, ci, row_index, cell_area, clip, sensitive ? state : StateType.Insensitive, false);
            }
            
            if (pressed_column_is_dragging && pressed_column_index >= 0) {
                cell_area.Width = column_cache[pressed_column_index].Width;
                cell_area.X = pressed_column_x_drag + Allocation.X;
                PaintCell (item, pressed_column_index, row_index, cell_area, clip, state, true);
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
                cairo_context.Color = fill_color;
                cairo_context.Rectangle (area.X, area.Y, area.Width, area.Height);
                cairo_context.Fill ();
            }
            
            cairo_context.Save ();
            cairo_context.Translate (area.X, area.Y);
            cell.Render (new CellContext (cairo_context, list_pango_layout, this, GdkWindow, theme, area, clip), 
                dragging ? StateType.Normal : state, area.Width, area.Height);
            cairo_context.Restore ();
        }
        
        private void PaintDraggingColumn (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            if (!pressed_column_is_dragging || pressed_column_index < 0) {
                return;
            }
            
            CachedColumn column = column_cache[pressed_column_index];
            
            int x = pressed_column_x_drag + Allocation.X + 1;
            
            Cairo.Color fill_color = Theme.Colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            fill_color.A = 0.45;
            
            Cairo.Color stroke_color = CairoExtensions.ColorShade (Theme.Colors.GetWidgetColor (
                GtkColorClass.Base, StateType.Normal), 0.0);
            stroke_color.A = 0.3;
            
            cairo_context.Rectangle (x, header_rendering_alloc.Bottom, column.Width,
                list_rendering_alloc.Bottom - header_rendering_alloc.Bottom);
            cairo_context.Color = fill_color;
            cairo_context.Fill ();
            
            cairo_context.MoveTo (x - 0.5, header_rendering_alloc.Bottom + 0.5);
            cairo_context.LineTo (x - 0.5, list_rendering_alloc.Bottom + 0.5);
            cairo_context.LineTo (x + column.Width - 1.5, list_rendering_alloc.Bottom + 0.5);
            cairo_context.LineTo (x + column.Width - 1.5, header_rendering_alloc.Bottom + 0.5);
            
            cairo_context.Color = stroke_color;
            cairo_context.LineWidth = 1.0;
            cairo_context.Stroke ();
        }
        
        private void InvalidateList ()
        {
            if (IsRealized) {
                GdkWindow.InvalidateRect (list_rendering_alloc, true);
                QueueDraw ();
            }
        }
        
        private void InvalidateHeader ()
        {
            if (IsRealized) {
                GdkWindow.InvalidateRect (header_rendering_alloc, true);
                QueueDraw ();
            }
        }
        
        private bool rules_hint = false;
        public bool RulesHint {
            get { return rules_hint; }
            set { 
                rules_hint = value; 
                QueueDraw ();
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
