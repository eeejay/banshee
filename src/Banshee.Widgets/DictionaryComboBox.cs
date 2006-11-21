/***************************************************************************
 *  DictionaryComboBox.cs
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
using Gtk;

namespace Banshee.Widgets
{
    public class DictionaryComboBox<T> : ComboBox
    {
        private ListStore store;
        
        public DictionaryComboBox()
        {
            store = new ListStore(typeof(string), typeof(T));
            Model = store;
                        
            CellRendererText text_renderer = new CellRendererText();
            PackStart(text_renderer, true);
            AddAttribute(text_renderer, "text", 0);
        }
        
        public TreeIter Add(string key, T value)
        {
            return store.AppendValues(key, value);
        }
        
        public T ActiveValue {
            get { 
                TreeIter iter;
                if(GetActiveIter(out iter)) {
                    return (T)store.GetValue(iter, 1);
                }
                
                return default(T);
            }
            
            set {
                for(int i = 0, n = store.IterNChildren(); i < n; i++) {
                    TreeIter iter;
                    if(store.IterNthChild(out iter, i)) {
                        T compare = (T)store.GetValue(iter, 1);
                        if(value.Equals(compare)) {
                            SetActiveIter(iter);
                            return;
                        }
                    }
                }
            }
        }
    }
}
