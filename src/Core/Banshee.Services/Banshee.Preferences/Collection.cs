//
// Collection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

namespace Banshee.Preferences
{
    public class Collection<T> : Root, IList<T> where T : Root
    {
        private List<T> list = new List<T> ();
        
        public Collection ()
        {
        }
        
        public T Add (T item)
        {
            lock (this) {
                list.Add (item);
                return item;
            }
        }
        
        public T FindOrAdd (T item)
        {
            lock (this) {
                return FindById (item.Id) ?? Add (item);
            }
        }
        
        public T this[string id] {
            get { return FindById (id); }
        }
        
        public T FindById (string id)
        {
            lock (this) {
                foreach (T item in this) {
                    if (item.Id == id) {
                        return item;
                    }
                }
                
                return null;
            }
        }
        
#region IList

        void IList<T>.Insert (int index, T item)
        {
            list.Insert (index, item);
        }
        
        void IList<T>.RemoveAt (int index)
        {
            list.RemoveAt (index);
        }

        int IList<T>.IndexOf (T item)
        {
            return list.IndexOf (item);
        }

        T IList<T>.this[int index] {
            get { return list[index]; }
            set { list[index] = value; }
        }

#endregion
        
#region ICollection

        void ICollection<T>.Add (T item)
        {
            list.Add (item);
        }
        
        bool ICollection<T>.Remove (T item)
        {
            return list.Remove (item);
        }
        
        void ICollection<T>.Clear ()
        {
            list.Clear ();
        }
        
        bool ICollection<T>.Contains (T item)
        {
            return list.Contains (item);
        }
        
        void ICollection<T>.CopyTo (T [] array, int arrayIndex)
        {
            list.CopyTo (array, arrayIndex);
        }

        public int Count {
            get { lock (this) { return list.Count; } }
        }
        
        bool ICollection<T>.IsReadOnly {
            get { return ((ICollection<T>)list).IsReadOnly; }
        }

#endregion

#region IEnumerable

        public IEnumerator<T> GetEnumerator ()
        {
            return list.GetEnumerator ();
        }
        
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

#endregion

    }
}
