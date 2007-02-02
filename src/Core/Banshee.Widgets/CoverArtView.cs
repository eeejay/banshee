/***************************************************************************
 *  CoverArtView.cs
 *
 *  Copyright (C) 2005-2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
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
using Gdk;

namespace Banshee.Widgets
{
    public class CoverArtView : DrawingArea
    {
        private Pixbuf pixbuf;
        private double ratio;
        private int last_width;
        private bool enabled;
        
        public CoverArtView() : base()
        {
        }
        
        protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
        {
            if(last_width == evnt.Width) {
                return true;
            }
            
            last_width = evnt.Width;
            SetSizeRequest();
            
            return true;
        }

        private void SetSizeRequest()
        {
            SetSizeRequest(-1, (int)(ratio * (double)last_width));
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(pixbuf == null) {
                return true;
            }
            
            Pixbuf scaled_pixbuf = pixbuf.ScaleSimple(Allocation.Width, Allocation.Height, 
                Gdk.InterpType.Bilinear);
            GdkWindow.DrawPixbuf(Style.BackgroundGC(State), scaled_pixbuf, 0, 0, 
                0, 0, scaled_pixbuf.Width, scaled_pixbuf.Height, RgbDither.Normal, 0, 0);
                
            return true;
        }
        
        public bool Enabled {
            get {
                return enabled;
            }
            
            set {
                enabled = value;
                
                if(!enabled || pixbuf == null) {
                    Hide();
                    return;
                }
                
                if(pixbuf == null) {
                    Hide();
                }
                
                Show();
                QueueDraw();
            }
        }
        
        public string FileName {
            set {
                try {
                    if(value == null || !System.IO.File.Exists(value)) {
                        throw new ApplicationException("Invalid file name");
                    }
                    
                    pixbuf = new Pixbuf(value);
                    if(pixbuf == null) {
                        throw new ApplicationException("Could not create pixbuf");
                    }
                    
                    ratio = (double)pixbuf.Height / (double)pixbuf.Width;
                    SetSizeRequest();
                    
                    if(enabled) {
                        Show();
                        QueueDraw();
                    }
                } catch(Exception) {
                    pixbuf = null;
                    Hide();
                }
            }
        }
    }
}
