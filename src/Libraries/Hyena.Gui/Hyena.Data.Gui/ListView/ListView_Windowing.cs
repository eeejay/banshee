//
// ListView_Windowing.cs
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
        private Gdk.Window list_window;
        private Gdk.Window header_window;
        private Gdk.Window footer_window;
        private Gdk.Window left_border_window;
        private Gdk.Window right_border_window;
        
        private Gdk.Rectangle list_alloc;
        private Gdk.Rectangle header_alloc;
        private Gdk.Rectangle footer_alloc;
        private Gdk.Rectangle left_border_alloc;
        private Gdk.Rectangle right_border_alloc;
       
        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized;
            
            Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
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
                
            GdkWindow = new Gdk.Window (Parent.GdkWindow, attributes, attributes_mask);
            GdkWindow.UserData = Handle;
            
            // left border window
            attributes.X = 0;
            attributes.Y = HeaderHeight;
            attributes.Width = InnerBorderWidth;
            attributes.Height = Allocation.Height - HeaderHeight - FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask);
            
            left_border_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            left_border_window.UserData = Handle;
             
            // right border window
            attributes.X = Allocation.Width - 2 * InnerBorderWidth;
            attributes.Y = HeaderHeight;
            attributes.Width = InnerBorderWidth;
            attributes.Height = Allocation.Height - HeaderHeight - FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask);
            
            right_border_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
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
                
            header_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            header_window.UserData = Handle;
            
            // footer window
            attributes.X = 0;
            attributes.Y = Allocation.Height - FooterHeight - HeaderHeight;
            attributes.Width = Allocation.Width;
            attributes.Height = FooterHeight;
            attributes.EventMask = (int)(
                Gdk.EventMask.ExposureMask |
                Events);
                
            footer_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
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
                
            list_window = new Gdk.Window (GdkWindow, attributes, attributes_mask);
            list_window.UserData = Handle;
            
            // style and move the windows
            Style = Style.Attach (GdkWindow);
            GdkWindow.SetBackPixmap (null, false);
            
            Style.SetBackground (GdkWindow, StateType.Normal);
            Style.SetBackground (header_window, StateType.Normal);
            Style.SetBackground (footer_window, StateType.Normal);
            
            left_border_window.Background = Style.Base (State);
            right_border_window.Background = Style.Base (State);
            list_window.Background = Style.Base (State);
            
            MoveResizeWindows (Allocation);
            
            // graphics context for drawing theme parts
            graphics = new ListViewGraphics (this);
            graphics.RefreshColors ();
            
            OnDragSourceSet ();
        }
        
        protected override void OnUnrealized ()
        {
            WidgetFlags ^= WidgetFlags.Realized;
            
            left_border_window.UserData = IntPtr.Zero;
            left_border_window.Destroy ();
            left_border_window = null;
            
            right_border_window.UserData = IntPtr.Zero;
            right_border_window.Destroy ();
            right_border_window = null;
            
            header_window.UserData = IntPtr.Zero;
            header_window.Destroy ();
            header_window = null;
            
            footer_window.UserData = IntPtr.Zero;
            footer_window.Destroy ();
            footer_window = null;
            
            list_window.UserData = IntPtr.Zero;
            list_window.Destroy ();
            list_window = null;
            
            base.OnUnrealized ();
        }
        
        protected override void OnMapped ()
        {
            WidgetFlags |= WidgetFlags.Mapped;
            
            left_border_window.Show ();
            right_border_window.Show ();
            list_window.Show ();
            footer_window.Show ();
            header_window.Show ();
            GdkWindow.Show ();
        }
        
        protected override void OnUnmapped ()
        {
            WidgetFlags ^= WidgetFlags.Mapped;
            
            left_border_window.Hide ();
            right_border_window.Hide ();
            list_window.Hide ();
            footer_window.Hide ();
            header_window.Hide ();
            GdkWindow.Hide ();
        }
        
        private void MoveResizeWindows (Gdk.Rectangle allocation)
        {
            header_alloc.Width = allocation.Width;
            header_alloc.Height = HeaderHeight;
            header_window.MoveResize (0, 0, header_alloc.Width, header_alloc.Height);
            
            footer_alloc.Width = allocation.Width;
            footer_alloc.Height = FooterHeight;
            footer_window.MoveResize (0, allocation.Height - footer_alloc.Height, footer_alloc.Width, footer_alloc.Height);
            
            left_border_alloc.Width = InnerBorderWidth;
            left_border_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height; 
            left_border_window.MoveResize (0, header_alloc.Height, left_border_alloc.Width, left_border_alloc.Height);
            
            right_border_alloc.Width = InnerBorderWidth;
            right_border_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height;
            right_border_window.MoveResize (allocation.Width - right_border_alloc.Width, header_alloc.Height, 
                right_border_alloc.Width, right_border_alloc.Height);
            
            list_alloc.Width = allocation.Width - 2 * left_border_alloc.Width;
            list_alloc.Height = allocation.Height - header_alloc.Height - footer_alloc.Height; 
            list_window.MoveResize (left_border_alloc.Width, header_alloc.Height, list_alloc.Width, list_alloc.Height);
        }
        
        protected override void OnSizeRequested (ref Requisition requisition)
        {
            requisition.Width = 0;
            requisition.Height = HeaderHeight;
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            bool resized_width = Allocation.Width != allocation.Width;
            
            base.OnSizeAllocated (allocation);
            
            if (IsRealized) {
                GdkWindow.MoveResize (allocation);
                MoveResizeWindows (allocation);
            }
           
            if (vadjustment != null) {
                vadjustment.PageSize = list_alloc.Height;
                vadjustment.PageIncrement = list_alloc.Height;
                UpdateAdjustments (null, null);
            }
            
            if (resized_width) {
                InvalidateHeaderWindow ();
                InvalidateFooterWindow ();
            }
            
            if (Model is ICareAboutView) {
                ((ICareAboutView)Model).RowsInView = RowsInView;
            }
            
            InvalidateListWindow ();
            RegenerateColumnCache ();
        }
        
        private int RowsInView {
            get { return (int) Math.Ceiling (list_alloc.Height / (double) RowHeight) + 1; }
        }
    }
}
