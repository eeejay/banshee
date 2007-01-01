/***************************************************************************
 *  DiscUsageDisplay.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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

using Gtk;
using Cairo;

namespace Banshee.Widgets
{
    public class DiscUsageDisplay : Gtk.DrawingArea
    {
        private static Color bound_color_a = new Cairo.Color(1, 0x66 / (double)0xff, 0x00);
        private static Color bound_color_b = new Cairo.Color(1, 0xcc / (double)0xff, 0x00, 0.3);
        private static Color disc_color_a = new Cairo.Color(1, 1, 1);
        private static Color disc_color_b = new Cairo.Color(0.95, 0.95, 0.95);
        private static Color red_color = new Cairo.Color(0xcc / (double)0xff, 0, 0, 0.5);

        private RadialGradient bg_gradient;
        private RadialGradient fg_gradient_full;
        private RadialGradient fg_gradient;
        private RadialGradient bound_gradient;
        
        private Color fill_color_a;
        private Color fill_color_b;
        private Color fill_color_c;
        private Color stroke_color;
        private Color inner_stroke_color;
        private Color text_color;
        private Color text_bg_color;
        
        private static readonly double a1 = 3 * Math.PI / 2;        
        private double x, y, radius, a2, base_line_width;

        private long capacity;
        private long usage;

        public DiscUsageDisplay()
        {
            AppPaintable = true;
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
        
        protected override void OnStyleSet(Gtk.Style style)
        {
            fill_color_a = GdkColorToCairoColor(Style.Background(StateType.Selected));
            fill_color_b = GdkColorToCairoColor(Style.Foreground(StateType.Selected));
            fill_color_c = GdkColorToCairoColor(Style.Background(StateType.Normal));
            stroke_color = GdkColorToCairoColor(Style.Foreground(StateType.Normal), 0.6);
            inner_stroke_color = GdkColorToCairoColor(Style.Foreground(StateType.Normal), 0.4);
            text_color = GdkColorToCairoColor(Style.Foreground(StateType.Normal), 0.8);
            text_bg_color = GdkColorToCairoColor(Style.Background(StateType.Normal), 0.6);
        }
        
        protected override void OnSizeAllocated(Gdk.Rectangle rect)
        {
            x = rect.Width / 2.0;
            y = rect.Height / 2.0;
            radius = Math.Min(rect.Width / 2, rect.Height / 2) - 5;
            base_line_width = Math.Sqrt(radius) * 0.2;
            
            bg_gradient = new RadialGradient(x, y, 0, x, y, radius);
            bg_gradient.AddColorStop(0, disc_color_a);
            bg_gradient.AddColorStop(1, disc_color_b);
            
            fg_gradient = new RadialGradient(x, y, 0, x, y, radius * 2);
            fg_gradient.AddColorStop(0, fill_color_a);
            fg_gradient.AddColorStop(1, fill_color_b);
                        
            fg_gradient_full = new RadialGradient(x, y, 0, x, y, radius);
            fg_gradient_full.AddColorStop(0, disc_color_b);
            fg_gradient_full.AddColorStop(1, red_color);
            
            bound_gradient = new RadialGradient(x, y, 0, x, y, radius * 2);
            bound_gradient.AddColorStop(0, bound_color_a);
            bound_gradient.AddColorStop(1, bound_color_b);
        
            base.OnSizeAllocated(rect);
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!IsRealized) {
                return false;
            }
            
            Cairo.Context cr = Gdk.CairoHelper.Create(GdkWindow);
            
            foreach(Gdk.Rectangle rect in evnt.Region.GetRectangles()) {
                cr.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
                cr.Clip();
                Draw(cr);
            }
            
            ((IDisposable)cr).Dispose();
            return false;
        }
        
        private void Draw(Cairo.Context cr)
        {
            cr.Antialias = Antialias.Subpixel;
            cr.LineWidth = base_line_width / 1.5;
            
            cr.Arc(x, y, radius, 0, 2 * Math.PI);
            cr.Pattern = bg_gradient;
            cr.Fill();
             
            /*cr.LineTo(x, y);
            cr.Arc(x, y, radius, a1 + 2 * Math.PI * 0.92, a1);
            cr.LineTo(x, y);
            cr.Pattern = bound_gradient;
            cr.Fill();
            cr.Stroke();*/
            
            if(Capacity > 0) {
                if(Fraction < 1.0) {
                    cr.LineTo(x, y);
                    cr.Arc(x, y, radius, a1, a2);
                    cr.LineTo(x, y);
                } else {
                    cr.Arc(x, y, radius, 0, 2 * Math.PI);
                }
                
                cr.Pattern = Fraction >= 1.0 ? fg_gradient_full : fg_gradient;
                cr.FillPreserve();
                          
                cr.Color = stroke_color;
                cr.Stroke();
            }
            
            cr.Arc(x, y, radius / 2.75, 0, 2 * Math.PI);
            cr.Color = fill_color_c;
            cr.FillPreserve();
            cr.Color = new Cairo.Color(1, 1, 1, 0.75);
            cr.FillPreserve();
            
            cr.LineWidth = base_line_width / 1.5;
            
            cr.Color = stroke_color;
            cr.Stroke();
            
            cr.Arc(x, y, radius / 5.5, 0, 2 * Math.PI);
            cr.Color = fill_color_c;
            cr.FillPreserve();
            
            cr.LineWidth = base_line_width / 2;
            
            cr.Color = inner_stroke_color;
            cr.Stroke();
            
            cr.Arc(x, y, radius, 0, 2 * Math.PI);
            cr.Stroke();
            
            if(Capacity <= 0) {
                // this sucks balls
                cr.Rectangle(0, 0, Allocation.Width, Allocation.Height);
                cr.Color = text_bg_color;
                cr.FillPreserve();
            
                cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Bold);
                cr.Color = text_color;
                cr.SetFontSize(Allocation.Width * 0.2);
                DrawText(cr, Mono.Unix.Catalog.GetString("Insert\nDisc"), 3);
            }
        }
        
        private void DrawText(Cairo.Context cr, string text, double lineSpacing)
        {
            string [] lines = text.Split('\n');
            double [] cuml_heights = new double[lines.Length];
            double y_start = 0.0;
            
            for(int i = 0; i < lines.Length; i++) {
                TextExtents extents = cr.TextExtents(lines[i]);
                double height = extents.Height + (i > 0 ? lineSpacing : 0);
                cuml_heights[i] = i > 0 ? cuml_heights[i - 1] + height : height;
            }
            
            y_start = (Allocation.Height / 2) - (cuml_heights[cuml_heights.Length - 1] / 2);
            
            for(int i = 0; i < lines.Length; i++) {
                TextExtents extents = cr.TextExtents(lines[i]);
            
                double x = (Allocation.Width / 2) - (extents.Width / 2);
                double y = y_start + cuml_heights[i];
        
                cr.MoveTo(x, y);
                cr.ShowText(lines[i]);
            }   
        }

        private void CalculateA2()
        {
            a2 = a1 + 2 * Math.PI * Fraction;
        }
        
        private double Fraction {
            get { return (double)Usage / (double)Capacity; }
        }
        
        public long Capacity {
            get { return capacity; }
            set { 
                capacity = value;
                CalculateA2();
                QueueDraw();
            }
        }
        
        public long Usage {
            get { return usage; }
            set {
                usage = value;
                CalculateA2();
                QueueDraw();
            }
        }
    }
}
