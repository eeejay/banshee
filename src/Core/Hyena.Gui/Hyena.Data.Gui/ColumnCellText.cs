//
// ColumnCellText.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

namespace Hyena.Data.Gui
{
    public class ColumnCellText : ColumnCell
    {
        public delegate string DataHandler();
    
        private Pango.Layout layout;
        private DataHandler data_handler;
        
        public ColumnCellText(bool expand, DataHandler data_handler) : base(expand, -1)
        {
            this.data_handler = data_handler;
        }
    
        public ColumnCellText(bool expand, int fieldIndex) : base(expand, fieldIndex)
        {
        }
    
        public override void Render(Gdk.Drawable window, Cairo.Context cr, Widget widget, Gdk.Rectangle cell_area, 
            Gdk.Rectangle clip_area, StateType state)
        {
            if(data_handler == null && BoundObject == null) {
                return;
            }
            
            if(layout == null) {
                layout = new Pango.Layout(widget.PangoContext);
            }
        
            string object_str = data_handler == null ? BoundObject.ToString() : data_handler();
            int text_height, text_width;
            
            layout.SetText(object_str);
            layout.GetPixelSize(out text_width, out text_height);
            
            Style.PaintLayout(widget.Style, window, state, true, clip_area, widget, "column",
                cell_area.X + 4, cell_area.Y + ((cell_area.Height - text_height) / 2), layout);
        }
    }
}
