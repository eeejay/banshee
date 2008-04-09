/*************************************************************************** 
 *  PixbufColumnCell.cs
 *
 *  Copyright (C) 2008 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
 *             Mike Urbanski <michael.c.urbanski@gmail.com>
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

using Cairo;

using Banshee.Gui;
using Hyena.Data.Gui;

namespace Banshee.Podcasting.Gui
{
    public class PixbufColumnCell : ColumnCell
    {
        private Pixbuf pixbuf;    
        private int spacing = 4;
        
        protected Pixbuf Pixbuf {
            get { return pixbuf; }
            set { pixbuf = value; }
        }
        
        protected int Spacing {
            get { return spacing; }
            set { spacing = value; }
        }
        
        public PixbufColumnCell (string property) : base (property, true)
        {
            LoadPixbufs ();
        }
        
        protected virtual void LoadPixbufs ()
        {
        }
        
        public override void NotifyThemeChange ()
        {
            LoadPixbufs ();
        }

        public override void Render (CellContext context, 
                                     StateType state, 
                                     double cellWidth, 
                                     double cellHeight)
        {
            if (pixbuf == null) {
                return;
            }
        
            context.Context.Translate (0, 0.5);

            Cairo.Rectangle pixbuf_area = new Cairo.Rectangle (
                spacing, (cellHeight - pixbuf.Height) / 2, 
                pixbuf.Width, pixbuf.Height
            );
            
            CairoHelper.SetSourcePixbuf (
                context.Context, pixbuf, 
                pixbuf_area.X, pixbuf_area.Y
            );
            
            context.Context.Rectangle (pixbuf_area);
            context.Context.Fill ();
        }
    }
}