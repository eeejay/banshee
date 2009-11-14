/***************************************************************************
 *  TaskStatusChangedEventArgs.cs
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

namespace Migo.TaskCore
{
    public class TaskStatusChangedEventArgs : EventArgs
    {
        private readonly TaskStatusChangedInfo statusChanged;
        private readonly TaskStatusChangedInfo[] statusesChanged;

        public TaskStatusChangedInfo StatusChanged
        {
            get { return statusChanged; }
        }

        public TaskStatusChangedInfo[] StatusesChanged
        {
            get { return statusesChanged; }
        }

        public TaskStatusChangedEventArgs (TaskStatusChangedInfo status) : this (true, status, null){}
        public TaskStatusChangedEventArgs (TaskStatusChangedInfo[] statuses) : this (false, null, statuses){}

        private TaskStatusChangedEventArgs (bool isSingle,
                                            TaskStatusChangedInfo status,
                                            TaskStatusChangedInfo[] statuses)
        {
            if (isSingle && status == null) {
                throw new ArgumentNullException ("status");
            } else if (!isSingle && statuses == null) {
                throw new ArgumentNullException ("statuses");
            }

            this.statusChanged = status;
            this.statusesChanged = statuses;
        }
    }
}
