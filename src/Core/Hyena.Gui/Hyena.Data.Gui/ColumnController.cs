//
// ColumnController.cs
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

namespace Hyena.Data.Gui
{    
    public class ColumnController : IEnumerable<Column>
    {
        private List<Column> columns = new List<Column>();
        
        public void Append(Column column)
        {
            columns.Add(column);
        }
        
        public void Insert(Column column, int index)
        {
            columns.Insert(index, column);
        }
        
        public void Remove(Column column)
        {
            columns.Remove(column);
        }
        
        public void Remove(int index)
        {
            columns.RemoveAt(index);
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return columns.GetEnumerator();
        }
        
        IEnumerator<Column> IEnumerable<Column>.GetEnumerator()
        {
            return columns.GetEnumerator();
        }
        
        public Column this[int index] {
            get { return columns[index] as Column; }
        }
        
        public int Count {
            get { return columns.Count; }
        }
    }
}