// 
// OverlayWindow.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Larry Ewing <lewing@novell.com>
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

using Hyena.Gui;

namespace Banshee.NowPlaying
{
    public class OverlayWindow : Window
    {
        private Window toplevel;
        
        private double x_align = 0.5;
        private double y_align = 1;
        private double width_scale;
        private bool composited;
        
        public OverlayWindow (Window toplevel) : this (toplevel, 0.0)
        {
        }
        
        public OverlayWindow (Window toplevel, double widthScale) : base (WindowType.Popup)
        {
            if (toplevel == null) {
                throw new ArgumentNullException ("toplevel", "An overlay must have a parent window");
            }
            
            if (width_scale < 0 || width_scale > 1) {
                throw new ArgumentOutOfRangeException ("widthScale", "Must be between 0 and 1 inclusive");
            }
        
            this.toplevel = toplevel;
            this.width_scale = widthScale;
            
            Decorated = false;
            DestroyWithParent = true;
            AllowGrow = true;
            KeepAbove = true;
            TransientFor = toplevel;
            
            toplevel.ConfigureEvent += OnToplevelConfigureEvent;
            toplevel.SizeAllocated += OnToplevelSizeAllocated;
        }
        
        private bool can_hide;
        public bool CanHide {
            get { return can_hide; }
        }
        
        protected override void OnRealized ()
        {
            composited = CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this);
            AppPaintable = composited;
            
            base.OnRealized ();
            
            ShapeWindow ();
            Relocate ();
        }

        protected override void OnMapped ()
        {
            base.OnMapped ();
            Relocate ();
        }

        protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
        {
            return base.OnConfigureEvent (evnt);
        }
        
        protected override void OnSizeRequested (ref Requisition requisition)
        {
            if (Child != null) {
                requisition = Child.SizeRequest ();
            }
            
            if (width_scale > 0 && width_scale <= 1 && TransientFor != null) {
                requisition.Width = (int)(TransientFor.Allocation.Width * width_scale);
            }
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            Relocate ();
            ShapeWindow ();
            QueueDraw ();
        }
        
        private void OnToplevelConfigureEvent (object o, ConfigureEventArgs args)
        {
            Relocate ();
        }
        
        private void OnToplevelSizeAllocated (object o, SizeAllocatedArgs args)
        {
            QueueResize ();
            Relocate ();
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (!composited || evnt.Window != GdkWindow) {
                return base.OnExposeEvent (evnt);
            }
            
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
            
            Gdk.Color color = Style.Background (State);
            
            ShapeSurface (cr, new Cairo.Color (color.Red / (double) ushort.MaxValue,
                color.Blue / (double) ushort.MaxValue, 
                color.Green / (double) ushort.MaxValue,
                0.85));
            
            ((IDisposable)cr).Dispose ();
            return base.OnExposeEvent (evnt);
        }
        
        protected virtual void ShapeSurface (Cairo.Context cr, Cairo.Color color)
        {
            cr.Operator = Cairo.Operator.Source;
            Cairo.Pattern pattern = new Cairo.SolidPattern (color, false);
            cr.Source = pattern;
            pattern.Destroy ();
            cr.Paint ();
        }

        private void ShapeWindow ()
        {
        }
        
        private void Relocate ()
        {
            if (!IsRealized || !toplevel.IsRealized) {
                return;
            }
            
            int x, y;
            
            toplevel.GdkWindow.GetOrigin (out x, out y);
            
            int x_origin = x;
            int y_origin = y;
            
            x += (int)(toplevel.Allocation.Width * x_align);
            y += (int)(toplevel.Allocation.Height * y_align);
            
            x -= (int)(Allocation.Width * 0.5);
            y -= (int)(Allocation.Height * 0.5);
            
            x = Math.Max (0, Math.Min (x, x_origin + toplevel.Allocation.Width - Allocation.Width));
            y = Math.Max (0, Math.Min (y, y_origin + toplevel.Allocation.Height - Allocation.Height));
            
            Move (x, y);
        }
    }
}
