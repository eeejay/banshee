//
// ColumnHeaderCellText.cs
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
    public class ColumnHeaderCellText : ColumnCell
    {
        public delegate Column DataHandler();
        
        private Pango.Layout layout;
        private DataHandler data_handler;
        private bool has_sort;
        
        public ColumnHeaderCellText(DataHandler data_handler) : base(true, -1)
        {
            this.data_handler = data_handler;
        }
    
        public override void Render(Gdk.Drawable window, Cairo.Context cr, Widget widget, Gdk.Rectangle cell_area, 
            Gdk.Rectangle clip_area, StateType state)
        {
            if(data_handler == null) {
                return;
            }
            
            if(layout == null) {
                layout = new Pango.Layout(widget.PangoContext);
            }
        
            Column column = data_handler();
            int text_height, text_width;
            
            layout.SetText(column.Title);
            layout.GetPixelSize(out text_width, out text_height);
            
            Style.PaintLayout(widget.Style, window, state, true, clip_area, widget, "column",
                cell_area.X + 4, cell_area.Y + ((cell_area.Height - text_height) / 2), layout);
   
            if(has_sort) {
                int w = (int)((double)cell_area.Height / 3.5);
                int h = (int)((double)w / 1.6);
                int x1 = cell_area.X + text_width + 10;
                int x2 = x1 + w;
                int x3 = x1 + w / 2;
                int y1 = cell_area.Y + ((cell_area.Height - h) / 2);
                int y2 = y1 + h;
                
                cr.LineWidth = 0.75;
                
                if(((ISortableColumn)column).SortType == SortType.Ascending) {
                    cr.MoveTo(x1, y1);
                    cr.LineTo(x2, y1);
                    cr.LineTo(x3, y2);
                    cr.LineTo(x1, y1);
                } else {
                    cr.MoveTo(x3, y1);
                    cr.LineTo(x2, y2);
                    cr.LineTo(x1, y2);
                    cr.LineTo(x3, y1);
                }
                    
                cr.Color = new Color(1, 1, 1, 0.4);
                cr.FillPreserve();
                cr.Color = new Color(0, 0, 0, 1);
                cr.Stroke();
            }
        }
        
        public bool HasSort {
            get { return has_sort; }
            set { has_sort = value; }
        }
    }
}
