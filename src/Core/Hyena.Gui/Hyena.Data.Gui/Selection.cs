//
// Selection.cs
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
using System.Collections.Generic;

// TODO: Should this go into Hyena.Data? It's not technically GUI bound
//       but really is only useful in that context

namespace Hyena.Data.Gui
{
    public class Selection : IEnumerable<int>
    {
        private Dictionary<int, bool> selection = new Dictionary<int, bool>();
        private bool all_selected;
        
        public event EventHandler Changed;
        
        private object owner;
        
        public Selection()
        {
        }
        
        protected virtual void OnChanged()
        {
            EventHandler handler = Changed;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        public void ToggleSelect(int index)
        {
            lock(this) {
                if(selection.ContainsKey(index)) {
                    selection.Remove(index);
                } else {
                    selection.Add(index, true);
                }
                
                all_selected = false;
                OnChanged();
            }
        }
        
        public void Select(int index)
        {
            Select(index, true);
        }
        
        public void Select(int index, bool raise)
        { 
            lock(this) {
                if(!selection.ContainsKey(index)) { 
                    selection.Add(index, true);
                    all_selected = false;
                    
                    if(raise) {
                        OnChanged();
                    }
                }
            }
        }
        
        public void Unselect(int index)
        {
            lock(this) {
                if(selection.Remove(index)) {
                    all_selected = false;
                    OnChanged();
                }
            }
        }
                    
        public bool Contains(int index)
        {
            lock(this) {
                return selection.ContainsKey(index);
            }
        }
        
        public void SelectRange(int start, int end)
        {
            SelectRange(start, end, false);
        }
        
        public void SelectRange(int start, int end, bool all)
        {
            for(int i = start; i <= end; i++) {
                Select(i, false);
            }
            
            all_selected = all;
            
            OnChanged();
        }

        public void Clear() {
            Clear(true);
        }
        
        public void Clear(bool raise)
        {
            lock(this) {
                if(selection.Count > 0) {
                    selection.Clear();
                    all_selected = false;

                    if(raise) {
                        OnChanged();
                    }
                }
            }
        }
        
        public int Count {
            get { return selection.Count; }
        }
        
        public bool AllSelected {
            get { return all_selected; }
        }
        
        public object Owner {
            get { return owner; }
            set { owner = value; }
        }
        
        public IEnumerator<int> GetEnumerator()
        {
            return selection.Keys.GetEnumerator();
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return selection.Keys.GetEnumerator();
        }
    }
}
