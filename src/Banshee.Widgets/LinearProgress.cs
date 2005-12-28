/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  LinearProgress.cs
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
using Gtk;

namespace Banshee.Widgets
{
    public class LinearProgress : Gtk.DrawingArea
    {
        private double fraction;
        private static Gdk.GC bar_gc = null;

        public LinearProgress()
        {
            AppPaintable = true;
            fraction = 0;
            QueueDraw();
        }

        public static Gdk.Color ColorBlend(Gdk.Color a, Gdk.Color b, double blend)
        {
            if(blend < 0.0 || blend > 1.0) {
                throw new ApplicationException("blend < 0.0 || blend > 1.0");
            }
            
            double blendRatio = 1.0 - blend;

            int aR = a.Red >> 8;
            int aG = a.Green >> 8;
            int aB = a.Blue >> 8;

            int bR = b.Red >> 8;
            int bG = b.Green >> 8;
            int bB = b.Blue >> 8;

            double mR = aR + bR;
            double mG = aG + bG;
            double mB = aB + bB;

            double blR = mR * blendRatio;
            double blG = mG * blendRatio;
            double blB = mB * blendRatio;

            Gdk.Color color = new Gdk.Color((byte)blR, (byte)blG, (byte)blB);
            Gdk.Colormap.System.AllocColor(ref color, true, true);
            return color;
        }

        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(bar_gc == null) {
                bar_gc = new Gdk.GC(GdkWindow);
                Gdk.Color color = ColorBlend(Style.Background(StateType.Normal), 
                    Style.Foreground(StateType.Normal), 0.2);
                bar_gc.Background = color;
                bar_gc.Foreground = color;
            }
        
            DrawGdk();
            return false;
        }
        
        private void DrawGdk()
        {
            int bar_width = (int)((double)Allocation.Width * fraction - 3.0);
            GdkWindow.DrawRectangle(bar_gc, false, 0, 0, Allocation.Width - 1, Allocation.Height - 1);
            if(bar_width > 0) {
                GdkWindow.DrawRectangle(bar_gc, true, 2, 2, bar_width, Allocation.Height - 4);
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
    }
}
