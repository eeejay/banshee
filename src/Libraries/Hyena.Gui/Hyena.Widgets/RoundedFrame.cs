//
// RoundedFrame.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Cairo;

using Hyena.Gui;
using Hyena.Data.Gui;

namespace Hyena.Widgets
{
    public class RoundedFrame : Bin
    {
        private ListViewGraphics graphics;
        private int frame_width = 3;
        
        private Widget child;
        private Gdk.Rectangle child_allocation;
        
        public RoundedFrame ()
        {
        }
        
#region Gtk.Widget Overrides

        protected override void OnRealized ()
        {
            base.OnRealized ();
            
            graphics = new ListViewGraphics (this);
            graphics.RefreshColors ();
        }

        protected override void OnSizeRequested (ref Requisition requisition)
        {
            if (child == null) {
                return;
            }
            
            int width = requisition.Width;
            int height = requisition.Height;
                
            child.GetSizeRequest (out width, out height);
            if (width == -1 || height == -1) {
                width = height = 80;
            }
                
            SetSizeRequest (width + ((int)BorderWidth + frame_width) * 2, 
                height + ((int)BorderWidth + frame_width) * 2);
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            child_allocation = new Gdk.Rectangle ();
            
            if (child == null || !child.Visible) {
                return;
            }
                
            child_allocation.X = (int)BorderWidth + frame_width;
            child_allocation.Y = (int)BorderWidth + frame_width;
            child_allocation.Width = (int)Math.Max (1, Allocation.Width - child_allocation.X * 2);
            child_allocation.Height = (int)Math.Max (1, Allocation.Height - child_allocation.Y - 
                (int)BorderWidth - frame_width);
                
            child_allocation.X += Allocation.X;
            child_allocation.Y += Allocation.Y;
                
            child.SizeAllocate (child_allocation);
        }
        
        protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
        {
            // This is to satisfy the gtk_widget_set_scroll_adjustments 
            // inside of GtkScrolledWindow so it doesn't complain about 
            // its child not being scrollable.
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (!IsDrawable) {
                return false;
            }
 
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
                
            try {
                DrawFrame (cr, evnt.Area);
                if (child != null) {
                    PropagateExpose (child, evnt);
                }
                return false;
            } finally {
                ((IDisposable)cr.Target).Dispose ();
                ((IDisposable)cr).Dispose ();
            }
        }
        
        private void DrawFrame (Cairo.Context cr, Gdk.Rectangle clip)
        {
            int x = child_allocation.X - frame_width;
            int y = child_allocation.Y - frame_width;
            int width = child_allocation.Width + 2 * frame_width;
            int height = child_allocation.Height + 2 * frame_width;
            
            graphics.DrawFrame (cr, new Gdk.Rectangle (x, y, width, height), true);
        }

#endregion

#region Gtk.Container Overrides

        protected override void OnAdded (Widget widget)
        {
            child = widget;
            base.OnAdded (widget);
        }

        protected override void OnRemoved (Widget widget)
        {
            if (child == widget) {
                child = null;
            }

            base.OnRemoved (widget);
        }

#endregion

    }
}
