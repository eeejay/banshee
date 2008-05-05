//
// SegmentedBar.cs
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
using System.Collections.Generic;

using Gtk;
using Cairo;

using Hyena.Gui;

namespace Hyena.Widgets
{
    public class SegmentedBar : Widget
    {
        public struct Segment
        {
            private string title;
            private double percent;
            private Cairo.Color color;
            
            public Segment (string title, double percent, Cairo.Color color)
            {
                this.title = title;
                this.percent = percent;
                this.color = color;
            }
            
            public string Title {
                get { return title; }
                set { title = value; }
            }
            
            public double Percent {
                get { return percent; }
                set { percent = value; }
            }
            
            public Cairo.Color Color {
                get { return color; }
                set { color = value; }
            }
        }
        
        private List<Segment> segments = new List<Segment> ();
        private int bar_height = 25;
        private bool reflect = true;
        
        private Color remainder_color = CairoExtensions.RgbToColor (0xeeeeee);
    
        public SegmentedBar ()
        {
            WidgetFlags |= WidgetFlags.NoWindow;
        }
        
        protected override void OnRealized ()
        {
            GdkWindow = Parent.GdkWindow;
            base.OnRealized ();
        }
        
        protected override void OnSizeRequested (ref Requisition requisition)
        {
            requisition.Width = 200;
            requisition.Height = 0;
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            HeightRequest = reflect ? (int)Math.Ceiling (bar_height * 1.75) : bar_height;
            base.OnSizeAllocated (allocation);
        }
        
        public void AddSegmentRgba (string title, double percent, uint rgbaColor)
        {
            AddSegment (title, percent, CairoExtensions.RgbaToColor (rgbaColor));
        }
        
        public void AddSegmentRgb (string title, double percent, uint rgbColor)
        {
            AddSegment (title, percent, CairoExtensions.RgbToColor (rgbColor));
        }
        
        public void AddSegment (string title, double percent, Color color)
        {
            lock (segments) {
                segments.Add (new Segment (title, percent, color));
                QueueDraw ();
            }
        }
        
        public int BarHeight {
            get { return bar_height; }
            set {
                if (bar_height != value) {
                    bar_height = value;
                    QueueResize ();
                }
            }
        }
        
        public bool ShowReflection {
            get { return reflect; }
            set {
                if (reflect != value) {
                    reflect = value;
                    QueueResize ();
                }
            }
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (evnt.Window != GdkWindow) {
                return base.OnExposeEvent (evnt);
            }
            
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
            
            if (reflect) {
                CairoExtensions.PushGroup (cr);
            }
            
            cr.Operator = Operator.Over;
            cr.Translate (Allocation.X, Allocation.Y);
            cr.Rectangle (0, 0, Allocation.Width, 2 * bar_height);
            cr.Clip ();
            
            Pattern bar = RenderBar (Allocation.Width, bar_height);
            
            cr.Save ();
            cr.Source = bar;
            cr.Paint ();
            cr.Restore ();
            
            if (reflect) {
                cr.Save ();
    
                cr.Rectangle (0, bar_height, Allocation.Width, bar_height);
                cr.Clip ();
                
                Matrix matrix = new Matrix ();
                matrix.InitScale (1, -1);
                matrix.Translate (0, -(2 * bar_height) + 1);
                cr.Transform (matrix);
                
                cr.Pattern = bar;
                
                LinearGradient mask = new LinearGradient (0, 0, 0, bar_height);
                
                mask.AddColorStop (0.25, new Color (0, 0, 0, 0));
                mask.AddColorStop (0.5, new Color (0, 0, 0, 0.125));
                mask.AddColorStop (0.75, new Color (0, 0, 0, 0.4));
                mask.AddColorStop (1.0, new Color (0, 0, 0, 0.7));
                
                cr.Mask (mask);
                mask.Destroy ();
                
                cr.Restore ();
                
                CairoExtensions.PopGroupToSource (cr);
                cr.Paint ();
            }
            
            bar.Destroy ();
            ((IDisposable)cr.Target).Dispose ();
            ((IDisposable)cr).Dispose ();
            
            return true;
        }
        
        private Pattern RenderBar (int w, int h)
        {
            ImageSurface s = new ImageSurface (Format.Argb32, w, h);
            Context cr = new Context (s);
            RenderBar (cr, w, h, h / 2);
            Pattern pattern = new Pattern (s);
            s.Destroy ();
            ((IDisposable)cr).Dispose ();
            return pattern;
        }
        
        private void RenderBar (Context cr, int w, int h, int r)
        {
            RenderBarSegments (cr, w, h, r);
            RenderBarStrokes (cr, w, h, r);
        }
        
        private void RenderBarSegments (Context cr, int w, int h, int r)
        {
            CairoCorners corners_left = CairoCorners.TopLeft | CairoCorners.BottomLeft;
            CairoCorners corners_right = CairoCorners.TopRight | CairoCorners.BottomRight;
            
            double x = 0;
            
            for (int i = 0; i < segments.Count; i++) {
                LinearGradient grad = MakeSegmentGradient (h, segments[i].Color);
                double s_w = w * segments[i].Percent;
                CairoCorners corners = CairoCorners.None;
                
                if (i == 0) {
                    corners |= corners_left;
                } else if (i == segments.Count - 1 && x + s_w == w) {
                    corners |= corners_right;
                }
                
                CairoExtensions.RoundedRectangle (cr, x, 0, s_w, h, r, corners);
                cr.Pattern = grad;
                cr.Fill ();
                grad.Destroy ();
                
                x += s_w;
            }
            
            if (x < w) {
                CairoExtensions.RoundedRectangle (cr, x, 0, w - x, h, r, corners_right);
                cr.Pattern = MakeSegmentGradient (h, remainder_color);
                cr.Fill ();
                cr.Pattern.Destroy ();
            }
        }
        
        private void RenderBarStrokes (Context cr, int w, int h, int r)
        {
            LinearGradient stroke = MakeSegmentGradient (h, CairoExtensions.RgbaToColor (0x00000040));
            LinearGradient seg_sep_light = MakeSegmentGradient (h, CairoExtensions.RgbaToColor (0xffffff20));
            LinearGradient seg_sep_dark = MakeSegmentGradient (h, CairoExtensions.RgbaToColor (0x00000020));
            
            cr.LineWidth = 1;
            
            double seg_w = 20;
            double x = seg_w > r ? seg_w : r;
            
            while (x <= w - r) {
                cr.MoveTo (x - 0.5, 1);
                cr.LineTo (x - 0.5, h - 1);
                cr.Pattern = seg_sep_light;
                cr.Stroke ();
                
                cr.MoveTo (x + 0.5, 1);
                cr.LineTo (x + 0.5, h - 1);
                cr.Pattern = seg_sep_dark;
                cr.Stroke ();
                
                x += seg_w;
            }
            
            CairoExtensions.RoundedRectangle (cr, 0.5, 0.5, w - 1, h - 1, r);
            cr.Pattern = stroke;
            cr.Stroke ();
            
            stroke.Destroy ();
            seg_sep_light.Destroy ();
            seg_sep_dark.Destroy ();
        }
        
        private LinearGradient MakeSegmentGradient (int h, Color color)
        {
            LinearGradient grad = new LinearGradient (0, 0, 0, h);
            grad.AddColorStop (0, CairoExtensions.ColorShade (color, 1.1));
            grad.AddColorStop (0.35, CairoExtensions.ColorShade (color, 1.2));
            grad.AddColorStop (1, CairoExtensions.ColorShade (color, 0.8));
            return grad;
        }
    }
    
    [TestModule ("Segmented Bar")]
    internal class SegmentedBarTestModule : Window
    {
        private SegmentedBar bar;
        private VBox box;
        public SegmentedBarTestModule () : base ("Segmented Bar")
        {
            BorderWidth = 10;
            
            box = new VBox ();
            box.Spacing = 10;
            Add (box);
            
            bar = new SegmentedBar ();
            bar.AddSegmentRgb ("Audio", 0.20, 0x3465a4);
            bar.AddSegmentRgb ("Video", 0.55, 0x73d216);
            bar.AddSegmentRgb ("Other", 0.10, 0xf57900);
            
            HBox controls = new HBox ();
            controls.Spacing = 5;
            
            Label label = new Label ("Height:");
            controls.PackStart (label, false, false, 0);
            
            SpinButton height = new SpinButton (new Adjustment (bar.BarHeight, 5, 100, 1, 1, 1), 1, 0);
            height.Activated += delegate { bar.BarHeight = height.ValueAsInt; };
            height.Changed += delegate { bar.BarHeight = height.ValueAsInt; };
            controls.PackStart (height, false, false, 0);
            
            CheckButton reflect = new CheckButton ("Reflection");
            reflect.Active = bar.ShowReflection;
            reflect.Toggled += delegate { bar.ShowReflection = reflect.Active; };
            controls.PackStart (reflect, false, false, 0);
            
            box.PackStart (controls, false, false, 0);
            box.PackStart (new HSeparator (), false, false, 0);
            box.PackStart (bar, false, false, 0);
            box.ShowAll ();
            
            SetSizeRequest (350, -1);
            
            Gdk.Geometry limits = new Gdk.Geometry ();
            limits.MinWidth = SizeRequest ().Width;
            limits.MaxWidth = Gdk.Screen.Default.Width;
            limits.MinHeight = -1;
            limits.MaxHeight = -1;
            SetGeometryHints (this, limits, Gdk.WindowHints.MaxSize | Gdk.WindowHints.MinSize);
        }
    }
}
