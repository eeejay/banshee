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
    public class ColumnHeaderCellText : ColumnCellText, IHeaderCell
    {
        public delegate Column DataHandler ();
        
        private DataHandler data_handler;
        private bool has_sort;
        
        public ColumnHeaderCellText (DataHandler data_handler) : base(null, true)
        {
            this.data_handler = data_handler;
        }
    
        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            if(data_handler == null) {
                return;
            }
            
            context.Context.Translate (0.5, 0.5);

            if (!has_sort) {
                base.Render (context, state, cellWidth - 10, cellHeight);
                return;
            }
            
            int w = (int)(cellHeight / 3.5);
            int h = (int)((double)w / 1.6);
            int x1 = (int)cellWidth - w - 10;
            int x2 = x1 + w;
            int x3 = x1 + w / 2;
            int y1 = ((int)cellHeight - h) / 2;
            int y2 = y1 + h;
            
            base.Render (context, state, cellWidth - 2 * w - 10, cellHeight);
            
            context.Context.Translate (-0.5, -0.5);
            
            if (((ISortableColumn)data_handler ()).SortType == SortType.Ascending) {
                context.Context.MoveTo (x1, y1);
                context.Context.LineTo (x2, y1);
                context.Context.LineTo (x3, y2);
                context.Context.LineTo (x1, y1);
            } else {
                context.Context.MoveTo (x3, y1);
                context.Context.LineTo (x2, y2);
                context.Context.LineTo (x1, y2);
                context.Context.LineTo (x3, y1);
            }
                
            context.Context.Color = new Color (1, 1, 1, 0.4);
            context.Context.FillPreserve ();
            context.Context.Color = new Color (0, 0, 0, 1);
            context.Context.Stroke ();
        }
        
        protected override string Text {
            get { return data_handler ().Title; }
        }
        
        public bool HasSort {
            get { return has_sort; }
            set { has_sort = value; }
        }
        
        public int MinWidth {
            get { return TextWidth + 25; }
        }
    }
}
