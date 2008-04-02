
/***************************************************************************
 *  RadialProgress.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Runtime.InteropServices;
using Gtk;
using Cairo;

namespace Banshee.Widgets
{
    public class RadialProgress : Gtk.DrawingArea
    {
#if HAVE_CAIRO
        private bool draw_ticks;
        private double fraction;

        public RadialProgress()
        {
            try {
                gdk_cairo_create(IntPtr.Zero);
            } catch {
                throw new ApplicationException("Cairo unsupported");
            }
            
            AppPaintable = true;
        }

        [DllImport("libgdk-x11-2.0.so.0")]
        private static extern IntPtr gdk_cairo_create(IntPtr raw);

        private static Cairo.Context CreateCairoDrawable(Gdk.Drawable drawable)
        {
            if(drawable == null) {
                return null;
            }
            
            Cairo.Context context = new Cairo.Context(gdk_cairo_create(drawable.Handle));
            if(context == null) {
                throw new ApplicationException("Could not create Cairo.Context");
            }

            return context;
        }
        
        private static Cairo.Color GdkColorToCairoColor(Gdk.Color color)
        {
            return GdkColorToCairoColor(color, 1.0);
        }
        
        private static Cairo.Color GdkColorToCairoColor(Gdk.Color color, double alpha)
        {
            return new Cairo.Color(
                (double)(color.Red >> 8) / 255.0,
                (double)(color.Green >> 8) / 255.0,
                (double)(color.Blue >> 8) / 255.0,
                alpha);
        }

        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!IsRealized) {
                return false;
            }
            
            Cairo.Context cr = CreateCairoDrawable(GdkWindow);
            cr.Rectangle(evnt.Area.X, evnt.Area.Y, evnt.Area.Width, evnt.Area.Height);
            cr.Clip();
            Draw(cr);
            ((IDisposable)cr).Dispose();
            return false;
        }

        private void Draw(Cairo.Context cr)
        {
            double x = Allocation.Width / 2.0;
            double y = Allocation.Height / 2.0;
            double radius = Math.Min(Allocation.Width / 2, Allocation.Height / 2) - 5;
            double a1 = 3 * Math.PI / 2;
            double a2 = a1 + 2 * Math.PI * fraction;
            double sh_offset = Math.Round(Math.Sqrt(radius) / 2);
            
            Color fill_color_a = GdkColorToCairoColor(Style.Background(StateType.Selected));
            Color fill_color_b = GdkColorToCairoColor(Style.Foreground(StateType.Selected));
            Color fill_color_c = GdkColorToCairoColor(Style.Background(StateType.Normal));
            Color stroke_color = GdkColorToCairoColor(Style.Foreground(StateType.Normal));
            Color tick_color = GdkColorToCairoColor(Style.Foreground(StateType.Normal), 0.5);

            RadialGradient bg_gradient = new RadialGradient(x, y, 0, x, y, radius);
            bg_gradient.AddColorStop(0, fill_color_c);
            bg_gradient.AddColorStop(1, new Color(1, 1, 1));
            
            RadialGradient fg_gradient = new RadialGradient(x, y, 0, x, y, radius * 2);
            fg_gradient.AddColorStop(0, fill_color_a);
            fg_gradient.AddColorStop(1, fill_color_b);
            
            RadialGradient sh_gradient = new RadialGradient(x, y, radius / 1.1, x + sh_offset, y + sh_offset, radius);
            sh_gradient.AddColorStop(0, GdkColorToCairoColor(Style.Foreground(StateType.Normal), 0.6));
            sh_gradient.AddColorStop(1, GdkColorToCairoColor(Style.Foreground(StateType.Normal), 0.0));
            
            cr.Antialias = Antialias.Subpixel;
            cr.LineWidth = Math.Sqrt(radius) * 0.2;
            
            cr.Arc(x + sh_offset, y + sh_offset, radius, 0, 2 * Math.PI);
            cr.Pattern = sh_gradient;
            cr.Fill();
            
            cr.Arc(x, y, radius, 0, 2 * Math.PI);
            cr.Pattern = bg_gradient;
            cr.FillPreserve();
            
            cr.Color = tick_color;
            cr.Stroke();
            
            if(fraction < 1.0) {
                cr.LineTo(x, y);
                cr.Arc(x, y, radius, a1, a2);
                cr.LineTo(x, y);
            } else {
                cr.Arc(x, y, radius, 0, 2 * Math.PI);
            }
            
            cr.Pattern = fg_gradient;
            cr.FillPreserve();
            
            cr.Color = stroke_color;
            cr.Stroke();

            if(draw_ticks) {
                cr.Color = tick_color;
                DrawTicks(cr, x, y, radius);
            }
        }

        private void DrawTicks(Cairo.Context cr, double x, double y, double radius)
        {
            int tick_count = 24;
            
            for(int i = 0; i < tick_count; i++) {
                int inset;
                cr.Save();

                if(i % 3 == 0) {
                    inset = (int)(0.3 * radius);
                } else {
                    inset = (int)(0.2 * radius);
                    cr.LineWidth *= 0.5;
                }

                cr.MoveTo(x + (radius - inset) * Math.Cos(i * Math.PI / (tick_count / 2)),
                    y + (radius - inset) * Math.Sin(i * Math.PI / (tick_count / 2)));
                cr.LineTo(x + radius * Math.Cos(i * Math.PI / (tick_count / 2)),
                    y + radius * Math.Sin(i * Math.PI / (tick_count / 2)));

                cr.Stroke();
                cr.Restore();
            }
        }

        public bool TimeMode {
            get {
                return draw_ticks;
            }

            set {
                draw_ticks = value;
                QueueDraw();
            }
        }
        
        public double Fraction {
            get {
                return fraction;
            }
            
            set {
                fraction = Math.Max(0.0, Math.Min(1.0, value));
                QueueDraw();
            }
        }
#endif
    }
}

