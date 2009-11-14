/***************************************************************************
 *  FilterableListModel.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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
using System.Reflection;

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena.Data;
using Hyena.Collections;
using Banshee.Collection;

namespace Banshee.Podcasting.Gui
{
    // It would be cool to move the filtering functionality to an external class
    // that could be applied to models directly, or chained together before being applied.
    /*public class FilterableListModel<T> : ListModel<T>
    {
        private Predicate<T> filter;
        private List<T> filteredList;

        public virtual Predicate<T> Filter {
            set {
                lock (SyncRoot) {
                    filter = value;
                    UpdateFilter ();
                }
            }
        }

        protected override List<T> List {
            get { lock (SyncRoot) { return filteredList ?? base.List; } }
        }

        public FilterableListModel ()
        {
        }

        public override void Clear ()
        {
            lock (SyncRoot) {
                base.List.Clear ();
                filteredList = null;

                OnCleared ();
            }
        }

        public override void Add (T item)
        {
            lock (SyncRoot) {
                base.List.Add (item);
                UpdateFilter ();
            }
        }

        public override void Add (IEnumerable<T> fe)
        {
            lock (SyncRoot) {
                base.List.AddRange (fe);
                UpdateFilter ();
            }
        }

        public override bool Contains (T item)
        {
            lock (SyncRoot) {
                return IndexOf (item) != -1;
            }
        }

        protected virtual void UpdateFilter ()
        {
            lock (SyncRoot) {
                if (filter != null) {
                    filteredList = base.List.FindAll (filter);
                } else {
                    filteredList = null;
                }

                Sort ();
                Reload ();
            }
        }

        public override void Remove (T item)
        {
            lock (SyncRoot) {
                base.List.Remove (item);
                UpdateFilter ();
            }
        }

        public override void Remove (IEnumerable<T> fe)
        {
            lock (SyncRoot) {
                foreach (T f in fe) {
                    base.List.Remove (f);
                }

                UpdateFilter ();
            }
        }

        public override T this[int index] {
            get {
                lock (SyncRoot) {
                    return (index < Count) ? List[index] : default (T);
                }
            }
        }

        public override int Count {
            get {
                lock (SyncRoot) {
                    return List.Count;
                }
            }
        }

        public override int IndexOf (T item)
        {
            lock (SyncRoot) {
                return List.IndexOf (item);
            }
        }
    }
    */
}