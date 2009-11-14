/***************************************************************************
 *  ListModel.cs
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
    public abstract class SortTypeComparer<T> : IComparer<T>
    {
        private readonly SortType type;

        public SortType SortType {
            get { return type; }
        }

        public SortTypeComparer (SortType type)
        {
            this.type = type;
        }

        public abstract int Compare (T lhs, T rhs);
    }

    /*public class ListModel<T> : BansheeListModel<T>, ISortable
    {
        private ISortableColumn sortColumn;

        private List<T> list = new List<T> ();
        private readonly object sync;

        public object SyncRoot {
            get { return sync; }
        }

        protected virtual List<T> List {
            get { return list; }
        }

        public override Selection Selection {
            get { lock (sync) { return selection; } }
        }

        public override ModelSelection<T> SelectedItems {
            get { lock (sync) { return base.SelectedItems; } }
        }

        public virtual ISortableColumn SortColumn {
            get { return sortColumn; }
            protected set { sortColumn = value; }
        }

        public ListModel ()
        {
            selection = new Selection ();
            sync = ((ICollection)list).SyncRoot;
        }

        public override void Clear ()
        {
            lock (sync) {
                list.Clear ();
                OnCleared ();
            }
        }

        public override void Reload ()
        {
            OnReloaded ();
        }

        public virtual void Add (T item)
        {
            lock (sync) {
                list.Add (item);
                Sort ();
                Reload ();
            }
        }

        public virtual void Add (IEnumerable<T> fe)
        {
            lock (sync) {
                list.AddRange (fe);
                Sort ();
                Reload ();
            }
        }

        public virtual bool Contains (T item)
        {
            lock (sync) {
                return IndexOf (item) != -1;
            }
        }

        public virtual ReadOnlyCollection<T> Copy ()
        {
            lock (sync) { return new ReadOnlyCollection<T> (list); }
        }

        public virtual ReadOnlyCollection<T> CopySelectedItems ()
        {
            List<T> items = null;

            lock (sync) {
                ModelSelection<T> selected = SelectedItems;

                if (selected.Count > 0) {
                    items = new List<T> (selected.Count);

                    foreach (T t in selected) {
                        items.Add (t);
                    }
                }
            }

            return (items != null) ?
                new ReadOnlyCollection<T> (items) : null;
        }

        public virtual void Remove (T item)
        {
            lock (sync) {
                list.Remove (item);
                Sort ();
                Reload ();
            }
        }

        public virtual void Remove (IEnumerable<T> fe)
        {
            lock (sync) {
                foreach (T f in fe) {
                    list.Remove (f);
                }

                Sort ();
                Reload ();
            }
        }

        public override T this[int index] {
            get {
                lock (sync) {
                    return (index < list.Count) ? list[index] : default (T);
                }
            }
        }

        public override int Count {
            get {
                lock (sync) {
                    return list.Count;
                }
            }
        }

        public virtual int IndexOf (T item)
        {
            lock (sync) {
                return list.IndexOf (item);
            }
        }

        public virtual void Sort ()
        {
        }

        public virtual void Sort (ISortableColumn column)
        {
            lock (sync) {
                if (column == SortColumn) {
                    column.SortType = (column.SortType == SortType.Ascending) ?
                        SortType.Descending : SortType.Ascending;
                } else {
                    SortColumn = column;
                }

                Sort ();
            }
        }
    }*/
}