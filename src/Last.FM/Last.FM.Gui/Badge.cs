/***************************************************************************
 *  Badge.cs
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
using Mono.Unix;
using Gtk;

namespace Last.FM.Gui
{
    public class Badge : Gtk.EventBox
    {
        private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
        private static Gdk.Pixbuf pixbuf = Gdk.Pixbuf.LoadFromResource("badge.png");
        private static Gdk.Pixbuf pixbuf_hover = Gdk.Pixbuf.LoadFromResource("badge-hover.png");
        
        private Image image;
        private bool link = true;
        
        public Badge() : base()
        {
            image = new Image();
            image.Pixbuf = pixbuf;
            image.Xalign = 0.0f;
            image.Show();
            Add(image);
        }
                
        protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
        {
            if(link) {
                GdkWindow.Cursor = hand_cursor;
                image.Pixbuf = pixbuf_hover;
            }
            
            return base.OnEnterNotifyEvent(evnt);
        }

        protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
        {
            image.Pixbuf = pixbuf;
            GdkWindow.Cursor = null;
            
            return base.OnLeaveNotifyEvent(evnt);
        }
        
        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
            if(evnt.Button == 1) {
                Account.VisitHomePage();
            }
            
            return base.OnButtonReleaseEvent(evnt);
        }
        
        public bool Link {
            get { return link; }
            set { link = value; }
        }
        
        public static Gdk.Pixbuf Pixbuf {
            get { return pixbuf; }
        }
        
        public static Gdk.Pixbuf PixbufHover {
            get { return pixbuf_hover; }
        }
    }
}
