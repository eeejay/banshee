/***************************************************************************
 *  TaskList.cs
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
using System.Collections;
using System.Collections.Generic;

using Migo.TaskCore;

namespace Migo.TaskCore.Collections
{
    public class TaskList<T> : TaskCollection<T>, IList<T>, IEnumerable<T>, IEnumerable
        where T : Task
    {
        private List<T> list;
        internal int generation;

        private readonly object syncRoot = new object ();

        public override bool CanReorder {
            get { return true; }
        }

        public override int Count {
            get { return list.Count; }
        }

        public override bool IsReadOnly {
            get { return false; }
        }

        public override bool IsSynchronized {
            get { return false; }
        }

        public override object SyncRoot {
            get { return syncRoot; }
        }

        public TaskList ()
        {
            list = new List<T> ();
            generation = 0;
        }

        public override T this [int index] {
            get {
                CheckIndex (index);
                return list[index];
            }

            set {
                CheckIndex (index);
                list[index] = value;
            }
        }

        public override void Add (T task)
        {
            CheckTask (task);

            int index = list.Count;

            list.Add (task);
            ++generation;

            OnTaskAdded (index, task);
        }

        public void AddRange (IEnumerable<T> tasks)
        {
            CheckTasks (tasks);

            int index = list.Count;
            list.AddRange (tasks);
            ++generation;

            OnTasksAdded (CreatePairs (index, tasks));
        }

        public override void Clear ()
        {
            RemoveRange (0, list.Count);
        }

        public override bool Contains (T task)
        {
            bool ret = false;

            if (task != null) {
                ret = list.Contains (task);
            }

            return ret;
        }

        public override void CopyTo (T[] array, int index)
        {
            CheckCopyArgs (array, index);
            list.CopyTo (array, index);
        }

        public override void CopyTo (Array array, int index)
        {
            CheckCopyArgs (array, index);
            Array.Copy (list.ToArray (), 0, array, index, list.Count);
        }

        public override IEnumerator<T> GetEnumerator ()
        {
            return new TaskListEnumerator<T> (this);
        }

        public int IndexOf (T task)
        {
            int ret = -1;

            if (task != null) {
                ret = list.IndexOf (task);
            }

            return ret;
        }

        public void Insert (int index, T task)
        {
            CheckTask (task);
            CheckDestIndex (index);

            list.Insert (index, task);
            ++generation;

            OnTaskAdded (index, task);
        }

        public void InsertRange (int index, IEnumerable<T> tasks)
        {
            CheckDestIndex (index);
            CheckTasks (tasks);

            list.InsertRange (index, tasks);
            ++generation;

            OnTasksAdded (CreatePairs (index, tasks));
        }

        public void Move (int sourceIndex, int destIndex)
        {
            if (sourceIndex == destIndex) {
                return;
            } else if (sourceIndex >= list.Count || sourceIndex < 0) {
                throw new ArgumentOutOfRangeException ("sourceIndex");
            } else if (destIndex > list.Count || destIndex < 0) {
                throw new ArgumentOutOfRangeException ("destIndex");
            }

            Dictionary<Task,int> oldOrder = SaveOrder ();

            T tmpTask;

            tmpTask = list[sourceIndex];
            list.RemoveAt (sourceIndex);

            int maxIndex = list.Count;

            if (destIndex > maxIndex) {
                list.Insert (maxIndex, tmpTask);
            } else {
                list.Insert (destIndex, tmpTask);
            }

            ++generation;
            OnReordered (NewOrder (oldOrder));
        }

        public void Move (int destIndex, int[] sourceIndices)
        {
            if (destIndex > list.Count || destIndex < 0) {
                throw new ArgumentOutOfRangeException ("destIndex");
            }

            int maxIndex = list.Count;
            List<T> tmpList = new List<T> (sourceIndices.Length);

            foreach (int i in sourceIndices)
            {
                if (i < 0 || i > maxIndex) {
                    throw new ArgumentOutOfRangeException ("sourceIndices");
                }

                // A possible performance enhancement is to check for
                // contiguous regions in the source and remove those regions
                // at once.
                tmpList.Add (list[i]);
            }

            Dictionary<Task,int> oldOrder = SaveOrder ();

            if (tmpList.Count > 0)
            {
                int offset = 0;
                Array.Sort (sourceIndices);

                foreach (int i in sourceIndices)
                {
                    try {
                        list.RemoveAt (i-offset);
                    } catch { continue; }

                    ++offset;
                }

                maxIndex = list.Count;

                if (destIndex > maxIndex) {
                    list.InsertRange (maxIndex, tmpList);
                } else {
                    list.InsertRange (destIndex, tmpList);
                }

                ++generation;
                OnReordered (NewOrder (oldOrder));
            }
        }

        public override bool Remove (T task)
        {
            bool ret = false;
            int index = list.IndexOf (task);

            if (index != -1) {
                list.RemoveAt (index);

                ret = true;
                ++generation;
                OnTaskRemoved (index, task);
            }

            return ret;
        }

        public override void Remove (IEnumerable<T> tasks)
        {
            if (tasks == null) {
                throw new ArgumentNullException ("tasks");
            }

            int lastIndex;
            int currentIndex;
            List<int> indices = new List<int> ();
            List<T> removedTasks = new List<T> ();
            Dictionary<T,int> lastPosition = new Dictionary<T,int> ();

            foreach (T task in tasks) {
                lastIndex = lastPosition.ContainsKey (task) ?
                    lastPosition[task] : -1;

                currentIndex = list.IndexOf (task, lastIndex+1);

                if (currentIndex != -1) {
                    removedTasks.Add (task);
                    indices.Add (currentIndex);

                    if (lastIndex != -1) {
                        lastPosition[task] = currentIndex;
                    } else {
                        lastPosition.Add (task, currentIndex);
                    }
                }
            }

            if (indices.Count > 0) {
                indices.Sort ();

                for (int i = indices.Count-1; i >= 0; --i) {
                    list.RemoveAt (i);
                }

                ++generation;
                OnTasksRemoved (removedTasks, DetectContinuity (indices.ToArray ()));
            }
        }

        public void RemoveAt (int index)
        {
            CheckIndex (index);

            T task = list[index];
            list.RemoveAt (index);
            ++generation;

            OnTaskRemoved (index, task);
        }

        public void RemoveRange (int index, int count)
        {
            if (count == 0) {
                return;
            }

            CheckIndex (index);

            if (count < 0 || count > list.Count) {
                throw new ArgumentOutOfRangeException ("count");
            } else if (count+index > list.Count) {
                throw new ArgumentException (
                    "index and count exceed length of list"
                );
            }

            List<T> tasks = list.GetRange (index, count);

            list.RemoveRange (index, count);
            ++generation;

            int[] indices = new int[count];

            for (int i = 0; i < count; ++i) {
                Console.WriteLine (index);
                indices[i] = index++;
            }

            OnTasksRemoved (tasks, DetectContinuity (indices));
        }

        private void CheckCopyArgs (Array array, int index)
        {
            if (array == null) {
                throw new ArgumentNullException ("array");
            } else if (index < 0) {
                throw new ArgumentOutOfRangeException (
                    "Value of index must be greater than or equal to 0"
                );
            } else if (array.Rank > 1) {
                throw new ArgumentException (
                    "array must not be multidimensional"
                );
            } else if (index >= array.Length) {
                throw new ArgumentException (
                    "index exceeds array length"
                );
            } else if (list.Count > (array.Length-index)) {
                throw new ArgumentException (
                    "index and count exceed length of array"
                );
            }
        }

        private void CheckDestIndex (int index)
        {
            if (index < 0 || index > list.Count) {
                throw new ArgumentOutOfRangeException ("index");
            }
        }

        private void CheckIndex (int index)
        {
            if (index < 0 || index >= list.Count) {
                throw new ArgumentOutOfRangeException ("index");
            }
        }

        private void CheckTask (T task)
        {
            if (task == null) {
                throw new ArgumentNullException ("task");
            }
        }

        private void CheckTasks (IEnumerable<T> tasks)
        {
            if (tasks == null) {
                throw new ArgumentNullException ("tasks");
            }

            foreach (Task t in tasks) {
                if (t == null) {
                    throw new ArgumentException (
                        "No task in tasks may be null"
                    );
                }
            }
        }

        private ICollection<KeyValuePair<int,T>> CreatePairs (int index, IEnumerable<T> tasks)
        {
            List<KeyValuePair<int,T>> pairs = new List<KeyValuePair<int,T>> ();

            foreach (T task in tasks) {
                pairs.Add (new KeyValuePair<int,T> (index++, task));
            }

            return pairs;
        }

        private int[] NewOrder (Dictionary<Task,int> oldOrder)
        {
            int i = -1;
            int[] newOrder = new int[list.Count];

            foreach (Task t in list) {
                newOrder[++i] = oldOrder[t];
            }

            return newOrder;
        }

        private Dictionary<Task,int> SaveOrder ()
        {
            Dictionary<Task,int> ret = new Dictionary<Task,int> (list.Count);

            int i = -1;

            foreach (Task t in list) {
                ret[t] = ++i;
            }

            return ret;
        }
    }
}
