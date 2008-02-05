//
// ListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using Hyena.Collections;
using Selection=Hyena.Collections.Selection;

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
        private const int InnerBorderWidth = 4;
        private const int FooterHeight = InnerBorderWidth;
    
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
        
        private Pango.Layout header_pango_layout;
        private Pango.Layout list_pango_layout;
        
        private Gdk.Rectangle list_alloc;
        private Gdk.Rectangle header_alloc;
        private Gdk.Rectangle footer_alloc;
        private Gdk.Rectangle left_border_alloc;
        private Gdk.Rectangle right_border_alloc;
        
        private bool header_visible = true;
        
        private int column_text_y;
        private int column_text_height;
        private Pango.Layout column_layout;
        private int resizing_column_index = -1;
        
        private IListModel<T> model;
        private ColumnController column_controller;
        private CachedColumn [] column_cache;
        
        private int focused_row_index = -1;
        private bool rules_hint = false;

        private Adjustment vadjustment;
        public Adjustment Vadjustment {
            get { return vadjustment; }
        }
        
        private SelectionProxy selection_proxy = new SelectionProxy ();
        public SelectionProxy SelectionProxy {
            get { return selection_proxy; }
        }

        public Selection Selection {
            get { return model.Selection; }
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
                    return InnerBorderWidth;
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
            get { return (int) Math.Ceiling (list_alloc.Height / (double) RowHeight); }
        }
        
        public ColumnController ColumnController {
            get { return column_controller; }
            set { 
                if (column_controller != null) {
                    column_controller.Updated -= OnColumnControllerUpdatedHandler;
                }
                
                column_controller = value;
                
                RegenerateColumnCache();
                QueueDraw();
                
                if (column_controller != null) {
                    column_controller.Updated += OnColumnControllerUpdatedHandler;
                }
            }
        }
        
        public virtual IListModel<T> Model {
            get { return model; }
        }
        
        public ListView()
        {
            column_layout = new Pango.Layout(PangoContext);
            CanFocus = true;
            selection_proxy.Changed += delegate {
                InvalidateListWindow ();
            };
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
            attributes.Width = InnerBorderWidth;
            attributes.Height = Allocation.Height - HeaderHeight - FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask);
            
            left_border_window = new Gdk.Window(GdkWindow, attributes, attributes_mask);
            left_border_window.UserData = Handle;
             
            // right border window
            attributes.X = Allocation.Width - 2 * InnerBorderWidth;
            attributes.Y = HeaderHeight;
            attributes.Width = InnerBorderWidth;
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
            attributes.X = InnerBorderWidth;
            attributes.Y = HeaderHeight;
            attributes.Width = Allocation.Width - 2 * InnerBorderWidth;
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
            WidgetFlags |= WidgetFlags.Mapped;
            left_border_window.Show();
            right_border_window.Show();
            list_window.Show();
            footer_window.Show();
            header_window.Show();
            GdkWindow.Show();
        }
        
        protected override void OnUnmapped ()
        {
            WidgetFlags ^= WidgetFlags.Mapped;
            left_border_window.Hide();
            right_border_window.Hide();
            list_window.Hide();
            footer_window.Hide();
            header_window.Hide();
            GdkWindow.Hide();
        }
        
        private void MoveResizeWindows(Gdk.Rectangle allocation)
        {
            header_alloc.Width = allocation.Width;
            header_alloc.Height = HeaderHeight;
            header_window.MoveResize(0, 0, header_alloc.Width, header_alloc.Height);
            
            footer_alloc.Width = allocation.Width;
            footer_alloc.Height = FooterHeight;
            footer_window.MoveResize(0, allocation.Height - footer_alloc.Height, footer_alloc.Width, footer_alloc.Height);
            
            left_border_alloc.Width = InnerBorderWidth;
            left_border_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height; 
            left_border_window.MoveResize(0, header_alloc.Height, left_border_alloc.Width, left_border_alloc.Height);
            
            right_border_alloc.Width = InnerBorderWidth;
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
                vadjustment.PageSize = list_alloc.Height;
                vadjustment.PageIncrement = list_alloc.Height;
                UpdateAdjustments(null, null);
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

        public void ScrollTo (double val)
        {
            vadjustment.Value = Math.Min (val, vadjustment.Upper - vadjustment.PageSize);
        }

        public void ScrollToRow (int row_index)
        {
            ScrollTo (GetYAtRow (row_index));
        }
        
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

        protected override bool OnKeyPressEvent(Gdk.EventKey press)
        {
            bool handled = false;

            switch(press.Key) {
                case Gdk.Key.a:
                    if ((press.State & Gdk.ModifierType.ControlMask) != 0) {
                        SelectionProxy.Selection.SelectAll ();
                        handled = true;
                    }
                    break;

                case Gdk.Key.A:
                    if ((press.State & Gdk.ModifierType.ControlMask) != 0) {
                        SelectionProxy.Selection.Clear ();
                        handled = true;
                    }
                    break;

                case Gdk.Key.k:
                case Gdk.Key.K:
                case Gdk.Key.Up:
                case Gdk.Key.KP_Up:
                    handled = KeyboardScroll (press.State, -1, true);
                    break;

                case Gdk.Key.j:
                case Gdk.Key.J:
                case Gdk.Key.Down:
                case Gdk.Key.KP_Down:
                    handled = KeyboardScroll (press.State, 1, true);
                    break;

                case Gdk.Key.Page_Up:
                case Gdk.Key.KP_Page_Up:
                    handled = KeyboardScroll (press.State, (int) (-vadjustment.PageIncrement / (double) RowHeight), false);
                    break;

                case Gdk.Key.Page_Down:
                case Gdk.Key.KP_Page_Down:
                    handled = KeyboardScroll (press.State, (int) (vadjustment.PageIncrement / (double) RowHeight), false);
                    break;

                case Gdk.Key.Home:
                case Gdk.Key.KP_Home:
                    handled = KeyboardScroll (press.State, -10000000, false);
                    break;

                case Gdk.Key.End:
                case Gdk.Key.KP_End:
                    handled = KeyboardScroll (press.State, 10000000, false);
                    break;

                case Gdk.Key.Return:
                case Gdk.Key.KP_Enter:
                case Gdk.Key.space:
                    if (focused_row_index != -1) {
                        Selection.Clear (false);
                        Selection.Select (focused_row_index);
                        OnRowActivated ();
                        handled = true;
                    }
                    break;
            }

            if (handled)
                return true;
            
            return base.OnKeyPressEvent(press);
        }

        private bool KeyboardScroll (Gdk.ModifierType modifier, int relative_row, bool align_y)
        {
            int row_limit;
            if (relative_row < 0) {
                if (focused_row_index == -1)
                    return false;
                row_limit = 0;
            } else {
                row_limit = Model.Count - 1;
            }

            if (focused_row_index == row_limit)
                return true;

            int row_index = Math.Min (Model.Count - 1, Math.Max (0, focused_row_index + relative_row));

            if ((modifier & Gdk.ModifierType.ControlMask) != 0) {
                // Don't change the selection
            } else if ((modifier & Gdk.ModifierType.ShiftMask) != 0) {
                // Behave like nautilus: if and arrow key + shift is pressed and the currently focused item
                // is not selected, select it and don't move the focus or vadjustment.
                // Otherwise, select the new row and scroll etc as necessary.
                if ((relative_row * relative_row != 1)) {
                    Selection.SelectFromFirst(row_index, true);
                } else if (Selection.Contains (focused_row_index)) {
                    Selection.SelectFromFirst(row_index, true);
                } else {
                    Selection.Select(focused_row_index);
                    return true;
                }
            } else {
                Selection.Clear(false);
                Selection.Select(row_index);
            }

            // Scroll if needed
            double y_at_row = GetYAtRow (row_index);
            if (align_y) {
                if (y_at_row < vadjustment.Value) {
                    ScrollTo (y_at_row);
                } else if ((y_at_row + RowHeight) > (vadjustment.Value + vadjustment.PageSize)) {
                    ScrollTo (y_at_row + RowHeight - (vadjustment.PageSize));
                }
            } else {
                ScrollTo (vadjustment.Value + ((row_index - focused_row_index) * RowHeight));
            }

            focused_row_index = row_index;
            InvalidateListWindow();
            return true;
        }
        
        private int last_click_row_index = -1;
        
        protected override bool OnButtonPressEvent(Gdk.EventButton press)
        {
            HasFocus = true;
            
            if (press.Window == header_window) {
                Column column = GetColumnForResizeHandle ((int) press.X);
                if (column != null) {
                    resizing_column_index = GetCachedColumnForColumn (column).Index;
                }
            } else if (press.Window == list_window && model != null) {
                GrabFocus ();
                
                int row_index = GetRowAtY ((int) press.Y);
                
                if (Selection.Count > 1 && Selection.Contains (row_index)) {
                    return true;
                }
                
                object item = model[row_index];
                if (item == null) {
                    return true;
                }

                if (press.Button == 1 && press.Type == Gdk.EventType.TwoButtonPress && row_index == last_click_row_index) {
                    OnRowActivated ();
                    last_click_row_index = -1;
                } else {
                    if ((press.State & Gdk.ModifierType.ControlMask) != 0) {
                        if (press.Button == 3) {
                            if (!Selection.Contains (row_index)) {
                                Selection.Select (row_index);
                            }
                        } else {
                            Selection.ToggleSelect(row_index);
                        }
                    } else if ((press.State & Gdk.ModifierType.ShiftMask) != 0) {
                        Selection.SelectFromFirst(row_index, true);
                    } else {
                        if (press.Button == 3) {
                            if (!Selection.Contains (row_index)) {
                                Selection.Clear(false);
                                Selection.Select(row_index);
                            }
                        } else {
                            Selection.Clear(false);
                            Selection.Select(row_index);
                        }
                    }

                    FocusRow(row_index);

                    if (press.Button == 3) {
                        last_click_row_index = -1;
                        OnPopupMenu ();
                    } else {
                        last_click_row_index = row_index;
                    }
                }
                
                InvalidateListWindow();
            }
            
            return true;
        }
        
        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
           // Console.WriteLine 
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
            } else if (evnt.Window == list_window && model != null && evnt.State == Gdk.ModifierType.None) {
                GrabFocus ();
                
                int row_index = GetRowAtY ((int)evnt.Y);
                object item = model[row_index];
                if (item == null) {
                    return true;
                }
                
                if (Selection.Count > 1 && Selection.Contains (row_index)) {
                    Selection.Clear(false);
                    Selection.Select(row_index);
                    FocusRow (row_index);
                    return true;
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
                vadjustment.Upper = (RowHeight * (model.Count));
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

        public new void QueueDraw ()
        {
            base.QueueDraw ();
            
            InvalidateHeaderWindow ();
            InvalidateListWindow ();
            InvalidateFooterWindow ();
        }
         
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
                if (header_pango_layout == null) {
                    header_pango_layout = Pango.CairoHelper.CreateLayout (header_cr);
                }
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
                if (list_pango_layout == null) {
                    list_pango_layout = Pango.CairoHelper.CreateLayout (list_cr);
                }
                PaintList(evnt, clip);
            }
            
            ((IDisposable)cr.Target).Dispose ();
            ((IDisposable)cr).Dispose();
        }
        
        private void PaintHeader(Gdk.Rectangle clip)
        {
            graphics.DrawHeaderBackground(header_cr, header_alloc, 2, header_visible);
            
            if(column_controller == null || !header_visible) {
                return;
            }
                
            Gdk.Rectangle cell_area = new Gdk.Rectangle();
            cell_area.Y = column_text_y;
            cell_area.Height = HeaderHeight - column_text_y;

            for(int ci = 0; ci < column_cache.Length; ci++) {            
                cell_area.X = column_cache[ci].X1 + left_border_alloc.Width;
                cell_area.Width = column_cache[ci].Width - COLUMN_PADDING;
                
                ColumnCell cell = column_cache[ci].Column.HeaderCell;
                
                if(cell is ColumnHeaderCellText && Model is ISortable) {
                    bool has_sort = ((ISortable)Model).SortColumn == column_cache[ci].Column as ISortableColumn 
                        && column_cache[ci].Column is ISortableColumn;
                    ((ColumnHeaderCellText)cell).HasSort = has_sort;
                    if(has_sort) {
                        graphics.DrawColumnHighlight(header_cr, cell_area, 3);
                    }
                }
                
                if (cell != null) {
                    header_cr.Save ();
                    header_cr.Translate (cell_area.X, cell_area.Y);
                    cell.Render (new CellContext (header_cr, header_pango_layout, this, header_window, 
                        graphics, cell_area), StateType.Normal, cell_area.Width, cell_area.Height);
                    header_cr.Restore ();
                }
                
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
        
        private void PaintList(Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            if (model == null) {
                return;
            }

            int vadjustment_value = (int) vadjustment.Value;
            int first_row = vadjustment_value / RowHeight;
            int last_row = Math.Min (model.Count, first_row + RowsInView);     

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
                } else {
                    if (selection_height > 0) {
                        graphics.DrawRowSelection (
                            list_cr, list_alloc.X, list_alloc.Y + selection_y, list_alloc.Width, selection_height);
                        selection_height = 0;
                    }
                    
                    if (rules_hint && ri % 2 != 0) {
                        graphics.DrawRowRule (list_cr, single_list_alloc.X, single_list_alloc.Y, 
                            single_list_alloc.Width, single_list_alloc.Height);
                    }
                    
                    PaintRow (ri, clip, single_list_alloc, StateType.Normal);
                }
                
                single_list_alloc.Y += single_list_alloc.Height;
            }
            
            if (selection_height > 0) {
                graphics.DrawRowSelection(
                    list_cr, list_alloc.X, list_alloc.Y + selection_y, list_alloc.Width, selection_height);
            }
            
            foreach (int ri in selected_rows) {
                single_list_alloc.Y = ri * single_list_alloc.Height - vadjustment_value;
                PaintRow (ri, clip, single_list_alloc, StateType.Selected);
            }
        }

        private void PaintRow(int row_index, Gdk.Rectangle clip, Gdk.Rectangle area, StateType state)
        {
            if(column_cache == null) {
                return;
            }
            
            object item = model[row_index];
            
            Gdk.Rectangle cell_area = new Gdk.Rectangle();
            cell_area.Height = RowHeight;
            cell_area.Y = area.Y;

            for(int ci = 0; ci < column_cache.Length; ci++) {
                cell_area.Width = column_cache[ci].Width;
                cell_area.X = column_cache[ci].X1;
                    
                PaintCell(item, ci, row_index, cell_area, cell_area, state);
            }
        }
        
        private void PaintCell(object item, int column_index, int row_index, Gdk.Rectangle area, 
            Gdk.Rectangle clip, StateType state)
        {
            ColumnCell cell = column_cache[column_index].Column.GetCell(0);
            cell.BindListItem(item);
            
            list_cr.Save ();
            list_cr.Translate (clip.X, clip.Y);
            cell.Render (new CellContext (list_cr, list_pango_layout, this, list_window, graphics, area), 
                state, area.Width, area.Height);
            list_cr.Restore ();
        }
        
        protected void InvalidateListWindow()
        {
            if(list_window != null) {
                list_window.InvalidateRect(list_alloc, false);
            }
        }
        
        protected void InvalidateHeaderWindow()
        {
            if(header_window != null) {
                header_window.InvalidateRect(header_alloc, false);
            }
        }
        
        protected void InvalidateFooterWindow()
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
                    handler (this, new RowActivatedArgs<T> (focused_row_index, model[focused_row_index]));
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

        private double GetYAtRow (int row)
        {
            double y = (double) RowHeight * row;
            return y;
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
        public void SetModel (IListModel<T> model)
        {
            SetModel (model, 0.0);
        }

        public void SetModel (IListModel<T> value, double vpos)
        {
            if(model == value) {
                return;
            }

            if(model != null) {
                model.Cleared -= OnModelClearedHandler;
                model.Reloaded -= OnModelReloadedHandler;
            }
            
            model = value;

            if(model != null) {
                model.Cleared += OnModelClearedHandler;
                model.Reloaded += OnModelReloadedHandler;
                selection_proxy.Selection = model.Selection;
            }
            
            RefreshViewForModel(vpos);
        }

        private void RefreshViewForModel(double? vpos)
        {
            UpdateAdjustments(null, null);

            if (vpos != null)
                ScrollTo ((double) vpos);
            else
                ScrollTo (vadjustment.Value);

            if (Model != null) {
                Selection.MaxIndex = Model.Count - 1;
            }
            
            if(Parent is ScrolledWindow) {
                Parent.QueueDraw();
            }
        }

        private void OnModelClearedHandler(object o, EventArgs args)
        {
            OnModelCleared ();
        }
        
        private void OnModelReloadedHandler(object o, EventArgs args)
        {
            OnModelReloaded ();
        }

        private void OnColumnControllerUpdatedHandler(object o, EventArgs args)
        {
            OnColumnControllerUpdated();
        }

        protected virtual void OnModelCleared()
        {
            RefreshViewForModel(null);
        }
        
        protected virtual void OnModelReloaded()
        {
            RefreshViewForModel(null);
        }
        
        protected virtual void OnColumnControllerUpdated()
        {
            RegenerateColumnCache();
            QueueDraw();
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