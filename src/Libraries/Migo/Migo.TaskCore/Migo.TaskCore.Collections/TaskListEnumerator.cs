/***************************************************************************
 *  TaskListEnumerator.cs
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
    public class TaskListEnumerator<T> : ITaskCollectionEnumerator<T>
        where T : Task
    {
        private TaskList<T> list;

        private int index;
        private int generation;

        public T Current
        {
            get {
                Check ();

                if (index < 0 || index >= list.Count) {
                    throw new ArgumentOutOfRangeException ("index");
                }

                return list[index];
            }
        }

        T ITaskCollectionEnumerator<T>.Current {
            get { return Current; }
        }

        object IEnumerator.Current {
            get { return Current; }
        }

        public TaskListEnumerator (TaskList<T> taskList)
        {
            if (taskList == null) {
                throw new ArgumentNullException ("taskList");
            }

            list = taskList;
            index = -1;
            generation = list.generation;
        }

        public void Dispose ()
        {
            Check ();
            list = null;
        }

        public void Reset ()
        {
            Check ();
            index = -1;
            generation = list.generation;
        }

        public bool MoveNext ()
        {
            Check ();
            return (++index < list.Count);
        }

        public bool MoveFirst (TaskStatus status)
        {
            Check ();

            int i;
            int retIndex;

            retIndex = i = -1;

            foreach (T t in list) {
                ++i;
                if (t != null) {
                    if (t.Status == status) {
                        retIndex = i;
                        break;
                    }
                }
            }

            return MoveIndex (retIndex);
        }

        public bool MoveLast (TaskStatus status)
        {
            Check ();

            T t;
            int i = list.Count;

            while (--i > -1) {
                t = list[i];

                if (t != null) {
                    if (t.Status == status) {
                        break;
                    }
                }
            }

            return MoveIndex (i);
        }

        private bool MoveIndex (int index)
        {
            bool ret = false;
            if (index < list.Count && index > -1) {
                this.index = index;
                ret = true;
            }

            return ret;
        }

        private void Check ()
        {
            if (list == null) {
                throw new ObjectDisposedException (GetType ().FullName);
            } else if (generation != list.generation) {
                throw new InvalidOperationException ("Invalid Enumerator");
            }
        }
    }
}
