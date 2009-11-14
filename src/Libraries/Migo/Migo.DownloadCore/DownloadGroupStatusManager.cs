/***************************************************************************
 *  DownloadGroupStatusManager.cs
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
using System.Timers;
using System.Collections.Generic;

using Migo.TaskCore;

namespace Migo.DownloadCore
{
    public class DownloadGroupStatusManager : GroupStatusManager
    {
        private long bytesPerSecond;

        public DownloadGroupStatusManager ()
            : this (0,0) {}

        public DownloadGroupStatusManager (int totalDownloads, int maxRunningDownloads)
            : base (totalDownloads, maxRunningDownloads) {}

        public override void Reset ()
        {
            bytesPerSecond = 0;
            base.Reset ();
        }

        public virtual void SetTransferRate (long bytesPerSecond)
        {
            if (this.bytesPerSecond != bytesPerSecond) {
                this.bytesPerSecond = bytesPerSecond;
                OnStatusChanged ();
            }
        }

        protected override void OnStatusChanged ()
        {
            base.OnStatusChanged (
                new DownloadGroupStatusChangedEventArgs (
                    RemainingTasks, RunningTasks,
                    CompletedTasks, bytesPerSecond
                ) as GroupStatusChangedEventArgs
            );
        }
    }
}
