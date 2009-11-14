/***************************************************************************
 *  TaskCollection.cs
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
    public abstract class TaskCollection<T> : ICollection<T>
        where T : Task
    {
        private AsyncCommandQueue commandQueue;

        public EventHandler<ReorderedEventArgs> Reordered;

        public EventHandler<TaskAddedEventArgs<T>> TaskAdded;
        public EventHandler<TaskRemovedEventArgs<T>> TaskRemoved;

        public abstract bool CanReorder
        {
            get; // is Move implemented
        }

        public virtual AsyncCommandQueue CommandQueue
        {
            get { return commandQueue; }
            set { commandQueue = value; }
        }

        public abstract int Count {
            get;
        }

        public abstract bool IsReadOnly {
            get;
        }

        public abstract bool IsSynchronized {
            get;
        }

        public abstract object SyncRoot {
            get;
        }

        public TaskCollection () {}

        public abstract IEnumerator<T> GetEnumerator ();

        // This needs to be looked into.
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public abstract T this [int index]
        {
            get; set;
        }

        public abstract void Add (T task);
        public abstract bool Remove (T task);
        public abstract void Remove (IEnumerable<T> tasks);

        public abstract void CopyTo (T[] array, int index);
        public abstract void CopyTo (Array array, int index);

        public abstract void Clear ();
        public abstract bool Contains (T task);

        protected virtual IEnumerable<KeyValuePair<int,int>> DetectContinuity (int[] indices)
        {
            if (indices == null) {
                throw new ArgumentNullException ("indices");
            } else if (indices.Length == 0) {
                return null;
            }

            int cnt;
            int len = indices.Length;
            List<KeyValuePair<int,int>> ret = new List<KeyValuePair<int,int>>();

            int i = len-1;

            while (i > 0) {
                cnt = 1;
                while (indices[i] == indices[i-1]+1)
                {
                    ++cnt;
                    if (--i == 0) {
                        break;
                    }
                }

                ret.Add (new KeyValuePair<int,int>(indices[i--], cnt));
            }

            return ret;
        }

        protected virtual void OnReordered (int[] newOrder)
        {
            EventHandler<ReorderedEventArgs> handler = Reordered;

            if (handler != null) {
                ReorderedEventArgs e = new ReorderedEventArgs (newOrder);

                if (commandQueue != null) {
                	commandQueue.Register (
                	    new EventWrapper<ReorderedEventArgs> (handler, this, e)
                	);
                } else {
                    handler (this, e);
                }
            }
        }

        protected virtual void OnTaskAdded (int pos, T task)
        {
            EventHandler<TaskAddedEventArgs<T>> handler = TaskAdded;

            if (handler != null) {
                TaskAddedEventArgs<T> e = new TaskAddedEventArgs<T> (pos, task);

                if (commandQueue != null) {
                	commandQueue.Register (
                	    new EventWrapper<TaskAddedEventArgs<T>> (handler, this, e)
                	);
                } else {
                    handler (this, e);
                }
            }
        }

        protected virtual void OnTasksAdded (ICollection<KeyValuePair<int,T>> pairs)
        {
            EventHandler<TaskAddedEventArgs<T>> handler = TaskAdded;

            if (handler != null) {
                TaskAddedEventArgs<T> e = new TaskAddedEventArgs<T> (pairs);

                if (commandQueue != null) {
                	commandQueue.Register (new EventWrapper<TaskAddedEventArgs<T>> (
                	    handler, this, e)
                	);
                } else {
                    handler (this, e);
                }
            }
        }

        protected virtual void OnTaskRemoved (int index, T task)
        {
            EventHandler<TaskRemovedEventArgs<T>> handler = TaskRemoved;

            if (handler != null) {
                TaskRemovedEventArgs<T> e =
                    new TaskRemovedEventArgs<T> (index, task);

                if (commandQueue != null) {
                	commandQueue.Register (
                	    new EventWrapper<TaskRemovedEventArgs<T>> (handler, this, e)
                	);
            	} else {
            	    handler (this, e);
            	}
            }
        }

        protected virtual void OnTasksRemoved (ICollection<T> tasks,
                                               IEnumerable<KeyValuePair<int,int>> indices)
        {
            EventHandler<TaskRemovedEventArgs<T>> handler = TaskRemoved;

            if (handler != null) {
                TaskRemovedEventArgs<T> e = new TaskRemovedEventArgs<T> (indices, tasks);

                if (commandQueue != null) {
                	commandQueue.Register (
                	    new EventWrapper<TaskRemovedEventArgs<T>> (handler, this, e)
                	);
                } else {
                    handler (this, e);
                }
            }
        }
    }
}
