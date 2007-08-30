//
// Column.cs
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
using System.Collections;
using System.Collections.Generic;
using Gtk;

namespace Hyena.Data.Gui
{
    public class Column : IEnumerable<ColumnCell>
    {
        private string title;
        private double width;
        private bool visible;
        private ColumnCell header_cell;
        private List<ColumnCell> cells = new List<ColumnCell>();
        
        public event EventHandler VisibilityChanged;
        
        public Column(string title, ColumnCell cell, double width) : this(null, title, cell, width)
        {
            this.header_cell = new ColumnHeaderCellText(HeaderCellDataHandler);
        }
        
        public Column(ColumnCell header_cell, string title, ColumnCell cell, double width)
        {
            this.title = title;
            this.width = width;
            this.visible = true;
            this.header_cell = header_cell;
            
            PackStart(cell);
        }
        
        private Column HeaderCellDataHandler()
        {
            return this;
        }
        
        public void PackStart(ColumnCell cell)
        {
            cells.Insert(0, cell);
        }
        
        public void PackEnd(ColumnCell cell)
        {
            cells.Add(cell);
        }
        
        public ColumnCell GetCell(int index) 
        {
            return cells[index];
        }
        
        protected virtual void OnVisibilityChanged()
        {
            EventHandler handler = VisibilityChanged;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return cells.GetEnumerator();
        }
        
        IEnumerator<ColumnCell> IEnumerable<ColumnCell>.GetEnumerator()
        {
            return cells.GetEnumerator();
        }
        
        public string Title {
            get { return title; }
            set { title = value; }
        }
        
        public double Width {
            get { return width; }
            set { width = value; }
        }
        
        public bool Visible {
            get { return visible; }
            set {
                bool old = Visible;
                visible = value;
                
                if(value != old) {
                    OnVisibilityChanged();
                }
            }
        }
        
        public ColumnCell HeaderCell {
            get { return header_cell; }
            set { header_cell = value; }
        }
    }
}