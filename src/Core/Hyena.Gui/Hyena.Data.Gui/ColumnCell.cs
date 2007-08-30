//
// ColumnCell.cs
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
using System.Reflection;
using Gtk;
using Cairo;

namespace Hyena.Data.Gui
{
    public abstract class ColumnCell
    {
        private bool expand;
        private int field_index;
        private object bound_object;
            
        public ColumnCell(bool expand, int fieldIndex)
        {
            this.expand = expand;
            this.field_index = fieldIndex;
        }

        public void BindListItem(object item)
        {
            if(item == null) {
                return;
            }
            
            Type type = item.GetType();
            
            bound_object = null;
            
            object [] class_attributes = type.GetCustomAttributes(typeof(ListItemSetup), true);
            if(class_attributes != null && class_attributes.Length > 0) {
                bound_object = item;
                return;
            }
            
            foreach(PropertyInfo info in type.GetProperties()) {
                object [] attributes = info.GetCustomAttributes(typeof(ListItemSetup), false);
                if(attributes == null || attributes.Length == 0) {
                    continue;
                }
            
                if(((ListItemSetup [])attributes)[0].FieldIndex != field_index) {
                    continue;
                }
                
                bound_object = info.GetValue(item, null);
                return;
            }
            
            throw new ApplicationException("Cannot bind IListItem to cell: no ListItemSetup " + 
                "attributes were found on any properties.");
        }
        
        protected Type BoundType {
            get { return bound_object.GetType(); }
        }
        
        protected object BoundObject {
            get { return bound_object; }
        }
        
        public abstract void Render(Gdk.Drawable window, Cairo.Context cr, Widget widget, Gdk.Rectangle draw_area, 
            Gdk.Rectangle cell_area, StateType state);
        
        public bool Expand {
            get { return expand; }
            set { expand = value; }
        }
        
        public int FieldIndex {
            get { return field_index; }
            set { field_index = value; }
        }
    }
}
