//
// ListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Hyena.Data;

namespace Hyena.Data.Gui
{
    public delegate void RowActivatedHandler<T> (object o, RowActivatedArgs<T> args);    
    
    public class RowActivatedArgs<T> : EventArgs
    {
        private int row;
        private T row_value;
        
        public RowActivatedArgs (int row, T rowValue)
        {
            this.row = row;
            this.row_value = rowValue;
        }

        public int Row {
            get { return row; }
        }

        public T RowValue {
            get { return row_value; }
        }
    }
    
    [Binding(Gdk.Key.A, Gdk.ModifierType.ControlMask, "SelectAll")]
    [Binding(Gdk.Key.A, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask, "UnselectAll")]
    public class ListView<T> : Container
    {
        private static Gdk.Cursor resize_x_cursor = new Gdk.Cursor(Gdk.CursorType.SbHDoubleArrow);
    
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

        public event RowActivatedHandler<T> RowActivated;
    
        private const int COLUMN_PADDING = 1;
        private const int BorderWidth = 10;
        private const int FooterHeight = 10;
    
        private ListViewGraphics graphics;
        
        private Gdk.Window list_window;
        private Gdk.Window header_window;
        private Gdk.Window footer_window;
        private Gdk.Window left_border_window;
        private Gdk.Window right_border_window;
        
        private Cairo.Context list_cr;
        private Cairo.Context header_cr;
        private Cairo.Context footer_cr;
        private Cairo.Context left_border_cr;
        private Cairo.Context right_border_cr;
        
        private Gdk.Rectangle list_alloc;
        private Gdk.Rectangle header_alloc;
        private Gdk.Rectangle footer_alloc;
        private Gdk.Rectangle left_border_alloc;
        private Gdk.Rectangle right_border_alloc;
        
        private bool header_visible = true;
        
        private Adjustment vadjustment;
        
        private int list_y_offset;
        
        private int column_text_y;
        private int column_text_height;
        private Pango.Layout column_layout;
        private int resizing_column_index = -1;
        
        private IListModel<T> model;
        private ColumnController column_controller;
        private CachedColumn [] column_cache;
        
        private int focused_row_index = -1;
        private bool rules_hint = false;
        
        private Selection selection = new Selection();
        
        public Selection Selection {
            get { return selection; }
        }
        
        private int row_height = 0;
        protected int RowHeight {
            get {
                if(row_height == 0) {
                    int w_width;
                    Pango.Layout layout = new Pango.Layout(PangoContext);
                    layout.SetText("W");
                    layout.GetPixelSize(out w_width, out row_height);
                    row_height += 8;
                }
                
                return row_height;
            }
            
            set { row_height = value; }
        }
        
        private int header_height = 0;
        private int HeaderHeight {
            get {
                if(!header_visible) {
                    return BorderWidth;
                }
                
                if(header_height == 0) {
                    int w_width;
                    column_layout.SetText("W");
                    column_layout.GetPixelSize(out w_width, out column_text_height);
                    header_height = COLUMN_PADDING * 2 + column_text_height;
                    column_text_y = (header_height / 2) - (column_text_height / 2) - 2;
                    header_height += 10;
                }
                
                return header_height;
            }
        }
        
        public bool RulesHint {
            get { return rules_hint; }
            set { 
                rules_hint = value; 
                InvalidateListWindow();
            }
        }
        
        public bool HeaderVisible {
            get { return header_visible; }
            set { 
                header_visible = value; 
                ShowHideHeader();
            }
        }
        
        private int RowsInView {
            get { return list_alloc.Height / RowHeight + 3; }
        }
        
        public ColumnController ColumnController {
            get { return column_controller; }
            set { 
                column_controller = value;
                RegenerateColumnCache();
                QueueDraw();
            }
        }
        
        public virtual IListModel<T> Model {
            get { return model; }
            set {
                if(model != value && model != null) {
                    model.Cleared -= OnModelCleared;
                    model.Reloaded -= OnModelReloaded;
                }
                
                model = value;
                
                if(model != null) {
                    model.Cleared += OnModelCleared;
                    model.Reloaded += OnModelReloaded;
                }
                
                RefreshViewForModel();
                
                Selection.Owner = this;
            }
        }
        
        public ListView()
        {
            column_layout = new Pango.Layout(PangoContext);
            CanFocus = true;
        }      
        
#region Widget Window Management

        protected override void OnRealized()
        {
            WidgetFlags |= WidgetFlags.Realized;
            
            Gdk.WindowAttr attributes = new Gdk.WindowAttr();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Visual = Visual;
            attributes.Wclass = Gdk.WindowClass.InputOutput;
            attributes.Colormap = Colormap;
            attributes.EventMask = (int)Gdk.EventMask.VisibilityNotifyMask;
            
            Gdk.WindowAttributesType attributes_mask = 
                Gdk.WindowAttributesType.X | 
                Gdk.WindowAttributesType.Y | 
                Gdk.WindowAttributesType.Visual | 
                Gdk.WindowAttributesType.Colormap;
                
            GdkWindow = new Gdk.Window(Parent.GdkWindow, attributes, attributes_mask);
            GdkWindow.UserData = Handle;
            
            // left border window
            attributes.X = 0;
            attributes.Y = HeaderHeight;
            attributes.Width = BorderWidth;
            attributes.Height = Allocation.Height - HeaderHeight - FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask);
            
            left_border_window = new Gdk.Window(GdkWindow, attributes, attributes_mask);
            left_border_window.UserData = Handle;
             
            // right border window
            attributes.X = Allocation.Width - 2 * BorderWidth;
            attributes.Y = HeaderHeight;
            attributes.Width = BorderWidth;
            attributes.Height = Allocation.Height - HeaderHeight - FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask);
            
            right_border_window = new Gdk.Window(GdkWindow, attributes, attributes_mask);
            right_border_window.UserData = Handle;
            
            // header window
            attributes.X = 0;
            attributes.Y = 0;
            attributes.Width = Allocation.Width;
            attributes.Height = HeaderHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.ExposureMask |
                Gdk.EventMask.ScrollMask |
                Gdk.EventMask.ButtonPressMask |
                Gdk.EventMask.ButtonReleaseMask |
                Gdk.EventMask.KeyPressMask |
                Gdk.EventMask.KeyReleaseMask |
                Gdk.EventMask.PointerMotionMask |
                Events);
                
            header_window = new Gdk.Window(GdkWindow, attributes, attributes_mask);
            header_window.UserData = Handle;
            
            // footer window
            attributes.X = 0;
            attributes.Y = Allocation.Height - FooterHeight - HeaderHeight;
            attributes.Width = Allocation.Width;
            attributes.Height = FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.ExposureMask |
                Events);
                
            footer_window = new Gdk.Window(GdkWindow, attributes, attributes_mask);
            footer_window.UserData = Handle;
            
            // list window
            attributes.X = BorderWidth;
            attributes.Y = HeaderHeight;
            attributes.Width = Allocation.Width - 2 * BorderWidth;
            attributes.Height = Allocation.Height - HeaderHeight - FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.ExposureMask |
                Gdk.EventMask.ScrollMask |
                Gdk.EventMask.PointerMotionMask |
                Gdk.EventMask.EnterNotifyMask |
                Gdk.EventMask.LeaveNotifyMask |
                Gdk.EventMask.ButtonPressMask |
                Gdk.EventMask.ButtonReleaseMask |
                Events);
                
            list_window = new Gdk.Window(GdkWindow, attributes, attributes_mask);
            list_window.UserData = Handle;
            
            // style and move the windows
            Style = Style.Attach(GdkWindow);
            GdkWindow.SetBackPixmap(null, false);
            
            Style.SetBackground(GdkWindow, StateType.Normal);
            Style.SetBackground(header_window, StateType.Normal);
            Style.SetBackground(footer_window, StateType.Normal);
            
            left_border_window.Background = Style.Base(State);
            right_border_window.Background = Style.Base(State);
            list_window.Background = Style.Base(State);
            
            MoveResizeWindows(Allocation);
            
            // graphics context for drawing theme parts
            graphics = new ListViewGraphics(this);
            graphics.RefreshColors();
        }
        
        protected override void OnUnrealized()
        {
            left_border_window.UserData = IntPtr.Zero;
            left_border_window.Destroy();
            left_border_window = null;
            
            right_border_window.UserData = IntPtr.Zero;
            right_border_window.Destroy();
            right_border_window = null;
            
            header_window.UserData = IntPtr.Zero;
            header_window.Destroy();
            header_window = null;
            
            footer_window.UserData = IntPtr.Zero;
            footer_window.Destroy();
            footer_window = null;
            
            list_window.UserData = IntPtr.Zero;
            list_window.Destroy();
            list_window = null;
            
            base.OnUnrealized();
        }
        
        protected override void OnMapped()
        {
            left_border_window.Show();
            right_border_window.Show();
            list_window.Show();
            footer_window.Show();
            header_window.Show();
            GdkWindow.Show();
        }
        
        private void MoveResizeWindows(Gdk.Rectangle allocation)
        {
            header_alloc.Width = allocation.Width;
            header_alloc.Height = HeaderHeight;
            header_window.MoveResize(0, 0, header_alloc.Width, header_alloc.Height);
            
            footer_alloc.Width = allocation.Width;
            footer_alloc.Height = FooterHeight;
            footer_window.MoveResize(0, allocation.Height - footer_alloc.Height, footer_alloc.Width, footer_alloc.Height);
            
            left_border_alloc.Width = BorderWidth;
            left_border_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height; 
            left_border_window.MoveResize(0, header_alloc.Height, left_border_alloc.Width, left_border_alloc.Height);
            
            right_border_alloc.Width = BorderWidth;
            right_border_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height;
            right_border_window.MoveResize(allocation.Width - right_border_alloc.Width, header_alloc.Height, 
                right_border_alloc.Width, right_border_alloc.Height);
            
            list_alloc.Width = allocation.Width - 2 * left_border_alloc.Width;
            list_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height; 
            list_window.MoveResize(left_border_alloc.Width, header_alloc.Height, list_alloc.Width, list_alloc.Height);
        }
        
        protected override void OnSizeRequested(ref Requisition requisition)
        {
            requisition.Width = 0;
            requisition.Height = HeaderHeight;
        }
        
        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            bool resized_width = Allocation.Width != allocation.Width;
            
            base.OnSizeAllocated(allocation);
            
            if(IsRealized) {
                GdkWindow.MoveResize(allocation);
                MoveResizeWindows(allocation);
            }
           
            if(vadjustment != null) {
                vadjustment.PageSize = allocation.Height;
                vadjustment.PageIncrement = list_alloc.Height;
            }
            
            if(resized_width) {
                InvalidateHeaderWindow();
                InvalidateFooterWindow();
            }
            
            if(Model is ICareAboutView) {
                ((ICareAboutView)Model).RowsInView = RowsInView;
            }
            
            InvalidateListWindow();
            RegenerateColumnCache();
        }
        
#endregion
        
#region Widget Interaction
        
        protected override bool OnFocusInEvent(Gdk.EventFocus evnt)
        {
            return base.OnFocusInEvent(evnt);
        }
        
        protected override bool OnFocusOutEvent(Gdk.EventFocus evnt)
        {
            return base.OnFocusOutEvent(evnt);
        }
                
        protected override void OnSetScrollAdjustments(Adjustment hadj, Adjustment vadj)
        {
            if(vadj == null || hadj == null) {
                return;
            }
            
            vadj.ValueChanged += OnAdjustmentChanged;
            hadj.ValueChanged += OnAdjustmentChanged;
            
            UpdateAdjustments(hadj, vadj);
        }
        
        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            switch(evnt.Key) {
                case Gdk.Key.Up:
                    vadjustment.Value -= vadjustment.StepIncrement;
                    /*if((evnt.State & Gdk.ModifierType.ShiftMask) != 0) {
                        focus
                    }*/
                    
                    if(focused_row_index > 0) {
                        focused_row_index--;
                        InvalidateListWindow();
                    }
                    
                    break;
                case Gdk.Key.Down:
                    vadjustment.Value += vadjustment.StepIncrement;
                    
                    if(focused_row_index < Model.Rows - 1) {
                        focused_row_index++;
                        InvalidateListWindow();
                    }
                    
                    break;
                case Gdk.Key.Page_Up:
                    vadjustment.Value -= vadjustment.PageIncrement;
                    break;
                case Gdk.Key.Page_Down:
                    vadjustment.Value += vadjustment.PageIncrement;
                    break;
                case Gdk.Key.Return:
                case Gdk.Key.KP_Enter:
                case Gdk.Key.space:
                    if (focused_row_index != -1) {
                        Selection.Clear ();
                        Selection.Select (focused_row_index);
                        OnRowActivated ();
                        InvalidateListWindow();
                    }
                    break;
            }
            
            return base.OnKeyPressEvent(evnt);
        }
        
        private int last_click_row_index = -1;
        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            HasFocus = true;
            
            if (evnt.Window == header_window) {
                Column column = GetColumnForResizeHandle ((int) evnt.X);
                if (column != null) {
                    resizing_column_index = GetCachedColumnForColumn (column).Index;
                }
            } else if (evnt.Window == list_window && model != null) {
                GrabFocus ();
                    
                int row_index = GetRowAtY ((int) evnt.Y);
                object item = model.GetValue (row_index);
                if (item == null) {
                    return true;
                }
                
                if (evnt.Type == Gdk.EventType.TwoButtonPress && row_index == last_click_row_index) {
                    OnRowActivated ();
                    last_click_row_index = -1;
                } else {
                    last_click_row_index = row_index;
                    if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                        Selection.ToggleSelect(row_index);
                        FocusRow(row_index);
                    } else if ((evnt.State & Gdk.ModifierType.ShiftMask) != 0) {
                        Selection.Clear();
                        Selection.SelectRange(Math.Min(focused_row_index, row_index), 
                            Math.Max(focused_row_index, row_index));
                    } else {
                        Selection.Clear();
                        Selection.Select(row_index);
                        FocusRow(row_index);
                    }
                }
                
                InvalidateListWindow();
            }
            
            return true;
        }
        
        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
            if(evnt.Window == header_window) {
                if(resizing_column_index >= 0) {
                    resizing_column_index = -1;
                    header_window.Cursor = null;
                    return true;
                }
            
                Column column = GetColumnAt((int)evnt.X);
                if(column != null && Model is ISortable && column is ISortableColumn) {
                    ((ISortable)Model).Sort((ISortableColumn)column);
                    Model.Reload();
                    InvalidateHeaderWindow();
                }
            }
            
            return true;
        }
        
        protected override bool OnMotionNotifyEvent(Gdk.EventMotion evnt)
        {
            if(evnt.Window == header_window) {
                header_window.Cursor = resizing_column_index >= 0 || GetColumnForResizeHandle((int)evnt.X) != null ?
                    resize_x_cursor : null;
                  
                if(resizing_column_index >= 0) {
                    ResizeColumn(evnt.X);
                }
            }
            
            return true;
        }

        private void UpdateAdjustments(Adjustment hadj, Adjustment vadj)
        {
            if(vadj != null) {
                vadjustment = vadj;
            }
            
            if(vadjustment != null && model != null) {
                vadjustment.Upper = RowHeight * model.Rows + HeaderHeight;
                vadjustment.StepIncrement = RowHeight;
            }
            
            vadjustment.Change();
        }
        
        private void OnAdjustmentChanged(object o, EventArgs args)
        {
            InvalidateListWindow();
        }
        
#endregion        
        
#region Drawing
         
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {            
            foreach(Gdk.Rectangle rect in evnt.Region.GetRectangles()) {
                PaintRegion(evnt, rect);
            }
            
            return true;
        }
                
        private void PaintRegion(Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Cairo.Context cr = CairoHelper.CreateCairoDrawable(evnt.Window);
            cr.Rectangle(clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip();
            
            if(evnt.Window == header_window) {
                header_cr = cr;
                PaintHeader(evnt.Area);
            } else if(evnt.Window == footer_window) {
                footer_cr = cr;
                PaintFooter(evnt, clip);
            } else if(evnt.Window == left_border_window) {
                left_border_cr = cr;
                PaintLeftBorder(evnt, clip);
            } else if(evnt.Window == right_border_window) {
                right_border_cr = cr;
                PaintRightBorder(evnt, clip);
            } else if(evnt.Window == list_window) {
                list_cr = cr;
                PaintList(evnt, clip);
            }
            
            ((IDisposable)cr).Dispose();
        }
        
        private void PaintHeader(Gdk.Rectangle clip)
        {
            graphics.DrawHeaderBackground(header_cr, header_alloc, 2, header_visible);
            
            if(column_controller == null || !header_visible) {
                return;
            }
                
            for(int ci = 0; ci < column_cache.Length; ci++) {            
                Gdk.Rectangle cell_area = new Gdk.Rectangle();
                cell_area.X = column_cache[ci].X1 + left_border_alloc.Width;
                cell_area.Y = column_text_y;
                cell_area.Width = column_cache[ci].Width - COLUMN_PADDING;
                cell_area.Height = HeaderHeight - column_text_y;
                
                ColumnCell cell = column_cache[ci].Column.HeaderCell;
                
                if(cell is ColumnHeaderCellText && Model is ISortable) {
                    bool has_sort = ((ISortable)Model).SortColumn == column_cache[ci].Column as ISortableColumn 
                        && column_cache[ci].Column is ISortableColumn;
                    ((ColumnHeaderCellText)cell).HasSort = has_sort;
                    if(has_sort) {
                        graphics.DrawColumnHighlight(header_cr, cell_area, 3);
                    }
                }
                
                cell.Render(header_window, header_cr, this, cell_area, cell_area, StateType.Normal);
                
                if(ci < column_cache.Length - 1) {
                    graphics.DrawHeaderSeparator(header_cr, header_alloc, 
                        column_cache[ci].ResizeX1 - 1 + left_border_alloc.Width, 2);
                }
            }
        }
        
        private void PaintLeftBorder(Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            graphics.DrawLeftBorder(left_border_cr, left_border_alloc);
        }
        
        private void PaintRightBorder(Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            graphics.DrawRightBorder(right_border_cr, right_border_alloc);
        }
        
        private void PaintFooter(Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            graphics.DrawFooter(footer_cr, footer_alloc);
        }
        
        private class SelectionRectangle
        {
            public SelectionRectangle(int y, int height)
            {
                Y = y;
                Height = height;
            }
            
            public int Y;
            public int Height;
        }
        
        private void PaintList(Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            if(model == null) {
                return;
            }
            
            int rows = RowsInView;
            int first_row = (int)vadjustment.Value / RowHeight;
            int last_row = Math.Min(model.Rows, first_row + rows);
            
            // Compute a stack of Contiguous Selection Rectangles
            Stack<SelectionRectangle> cg_s_rects = new Stack<SelectionRectangle>();
            
            for(int ri = first_row; ri < last_row; ri++) {
                if(ri < 0 || !selection.Contains(ri)) {
                    continue;
                }
                
                if(selection.Contains(ri - 1) && cg_s_rects.Count > 0) {
                    cg_s_rects.Peek().Height += RowHeight; 
                } else {
                    cg_s_rects.Push(new SelectionRectangle(list_alloc.Y + 
                        (ri * RowHeight - (int)vadjustment.Value), RowHeight));
                }
            }
            
            foreach(SelectionRectangle selection_rect in cg_s_rects) {
                graphics.DrawRowSelection(list_cr, list_alloc.X, selection_rect.Y, list_alloc.Width, selection_rect.Height);
            }        

            for(int ri = first_row; ri < last_row; ri++) {
                Gdk.Rectangle single_list_alloc = new Gdk.Rectangle();
                single_list_alloc.Width = list_alloc.Width;
                single_list_alloc.Height = RowHeight;
                single_list_alloc.X = list_alloc.X;
                single_list_alloc.Y = list_alloc.Y + (ri * single_list_alloc.Height - (int)vadjustment.Value);
            
                StateType row_state = StateType.Normal;
                if(selection.Contains(ri)) {
                    row_state = StateType.Selected;
                }
                
                PaintRowBackground(ri, clip, single_list_alloc, row_state);
                PaintRowFocus(ri, clip, single_list_alloc, row_state);
                PaintRow(ri, clip, single_list_alloc, row_state);
            }
        }
        
        private void PaintRowFocus(int row_index, Gdk.Rectangle clip, Gdk.Rectangle area, StateType state)
        {
            if(row_index == focused_row_index && state != StateType.Selected) {
                Style.PaintFocus(Style, list_window, State, clip, this, "row", area.X, area.Y, area.Width, area.Height);
            }
        }
        
        private void PaintRowBackground(int row_index, Gdk.Rectangle clip, Gdk.Rectangle area, StateType state)
        {
            if(row_index % 2 != 0 && rules_hint) {
                Style.PaintFlatBox(Style, list_window, StateType.Normal, ShadowType.None, clip, this, "row",
                    area.X, area.Y, area.Width, area.Height);
            }
        }
        
        private void PaintRow(int row_index, Gdk.Rectangle clip, Gdk.Rectangle area, StateType state)
        {
            if(column_cache == null) {
                return;
            }
            
            object item = model.GetValue(row_index);
            
            for(int ci = 0; ci < column_cache.Length; ci++) {
                Gdk.Rectangle cell_area = new Gdk.Rectangle();
                cell_area.Width = column_cache[ci].Width;
                cell_area.Height = RowHeight;
                cell_area.X = column_cache[ci].X1;
                cell_area.Y = area.Y;
                    
                PaintCell(item, ci, row_index, cell_area, cell_area, state);
            }
        }
        
        private void PaintCell(object item, int column_index, int row_index, Gdk.Rectangle area, 
            Gdk.Rectangle clip, StateType state)
        {
            ColumnCell cell = column_cache[column_index].Column.GetCell(0);
            cell.BindListItem(item);
            cell.Render(list_window, list_cr, this, area, clip, state);
        }
        
        private void InvalidateListWindow()
        {
            if(list_window != null) {
                list_window.InvalidateRect(list_alloc, false);
            }
        }
        
        private void InvalidateHeaderWindow()
        {
            if(header_window != null) {
                header_window.InvalidateRect(header_alloc, false);
            }
        }
        
        private void InvalidateFooterWindow()
        {
            if(footer_window != null) {
                footer_window.InvalidateRect(footer_alloc, false);
            }
        }
        
#endregion
        
#region Various Utilities

        private void OnRowActivated ()
        {
            if (focused_row_index != -1) {
                RowActivatedHandler<T> handler = RowActivated;
                if (handler != null) {
                    handler (this, new RowActivatedArgs<T> (focused_row_index, model.GetValue (focused_row_index)));
                }
            }
        }
        
        private int GetRowAtY(int y)
        {
            int page_offset = (int)vadjustment.Value % RowHeight;
            int first_row = (int)vadjustment.Value / RowHeight;
            int row_offset = (y + page_offset) / RowHeight;
            
            return first_row + row_offset;
        }
          
        private void FocusRow(int index)
        {
            focused_row_index = index;
        }
        
        private void ShowHideHeader()
        {
            if(header_window == null) {
                return;
            }
            
            if(header_visible) {
                header_window.Show();
            } else {
                header_window.Hide();
            }
            
            MoveResizeWindows(Allocation);
        }
        
#endregion

#region Model Interaction

        private void RefreshViewForModel()
        {
            UpdateAdjustments(null, null);
            vadjustment.Value = 0;
            
            if(Parent is ScrolledWindow) {
                Parent.QueueDraw();
            }
        }

        private void OnModelCleared(object o, EventArgs args)
        {
            RefreshViewForModel();
        }
        
        private void OnModelReloaded(object o, EventArgs args)
        {
            RefreshViewForModel();
        }

#endregion
        
#region Keyboard Shortcut Handlers
        
        private void SelectAll()
        {
            Selection.SelectRange(0, model.Rows, true);
            InvalidateListWindow();
        }
        
        private void UnselectAll()
        {
            Selection.Clear();
            InvalidateListWindow();
        }
        
#endregion
          
#region Column Utilities

        private void InvalidateColumnCache()
        {
            if(column_cache == null) {
                return;
            }
        
            for(int i = 0; i < column_cache.Length; i++) {
                column_cache[i].Column.VisibilityChanged -= OnColumnVisibilityChanged;
                column_cache[i] = CachedColumn.Zero;
            }
            
            column_cache = null;
        }
        
        private void RegenerateColumnCache()
        {
            InvalidateColumnCache();
            
            if(column_controller == null) {
                return;
            }
            
            int i = 0;
            column_cache = new CachedColumn[column_controller.Count];
            
            foreach(Column column in column_controller) {
                if(!column.Visible) {
                    continue;
                }
                
                column_cache[i] = new CachedColumn();
                column_cache[i].Column = column;
                column.VisibilityChanged += OnColumnVisibilityChanged;
                
                column_cache[i].Width = (int)Math.Round(((double)list_alloc.Width * column.Width));
                column_cache[i].X1 = i == 0 ? 0 : column_cache[i - 1].X2;
                column_cache[i].X2 = column_cache[i].X1 + column_cache[i].Width;
                column_cache[i].ResizeX1 = column_cache[i].X1 + column_cache[i].Width - COLUMN_PADDING;
                column_cache[i].ResizeX2 = column_cache[i].ResizeX1 + 2;
                column_cache[i].Index = i;
                
                i++;
            }
            
            Array.Resize(ref column_cache, i);
        }
        
        private void OnColumnVisibilityChanged(object o, EventArgs args)
        {
            RegenerateColumnCache();
            QueueDraw();
        }
        
        private void ResizeColumn(double x)
        {
            CachedColumn resizing_column = column_cache[resizing_column_index];

            double resize_delta = x - resizing_column.ResizeX2;
            double subsequent_columns = column_cache.Length - resizing_column.Index - 1;
            double even_distribution = 0.0;
            
            for(int i = 0; i <= resizing_column_index; i++) {
                even_distribution += column_cache[i].Column.Width * resize_delta;
            }

            even_distribution /= subsequent_columns;

            resizing_column.Column.Width = (resizing_column.Width + resize_delta) / (double)list_alloc.Width;

            for(int i = resizing_column_index + 1; i < column_cache.Length; i++) {
                column_cache[i].Column.Width = (column_cache[i].Width - 
                    (column_cache[i].Column.Width * resize_delta) - 
                    even_distribution) / (double)list_alloc.Width;
            }
            
            RegenerateColumnCache();
            InvalidateHeaderWindow();
            InvalidateListWindow();
        }
        
        private Column GetColumnForResizeHandle(int x)
        {
            if(column_cache == null) {
                return null;
            }
            
            foreach(CachedColumn column in column_cache) {
                if(x >= column.ResizeX1 - 2 + left_border_alloc.Width && x <= column.ResizeX2 + 2 + left_border_alloc.Width ) {
                    return column.Column;
                }
            }
            
            return null;
        }
        
        private Column GetColumnAt(int x)
        {
            if(column_cache == null) {
                return null;
            }
            
            foreach(CachedColumn column in column_cache) {
                if(x >= column.X1 + left_border_alloc.Width && x <= column.X2 + left_border_alloc.Width) {
                    return column.Column;
                }
            }
            
            return null;
        }
        
        private CachedColumn GetCachedColumnForColumn(Column col)
        {
            foreach(CachedColumn ca_col in column_cache) {
                if(ca_col.Column == col) {
                    return ca_col;
                }
            }
            
            return CachedColumn.Zero;
        }
        
#endregion  

    }
}
