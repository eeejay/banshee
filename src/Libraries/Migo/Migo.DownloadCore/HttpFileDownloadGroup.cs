/***************************************************************************
 *  HttpFileDownloadGroup.cs
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
using System.Timers;  // migrate to System.Threading.Timer
using System.Threading;
using System.ComponentModel;
using System.Collections.Generic;

using Migo.Net;
using Migo.TaskCore;
using Migo.TaskCore.Collections;
// Write Dispose method

namespace Migo.DownloadCore
{
	public class HttpFileDownloadGroup : TaskGroup<HttpFileDownloadTask>
	{
	    DownloadGroupStatusManager dsm;

        private DateTime lastTick;	

        private bool disposed;
        private long transferRate;
        private long transferRatePreviously;
        private long bytesThisInterval = 0;

        private System.Timers.Timer transferTimer;
	
	    private Dictionary<HttpFileDownloadTask,long> transferRateDict;
	
        public HttpFileDownloadGroup (int maxDownloads, TaskCollection<HttpFileDownloadTask> tasks)
            : base (maxDownloads, tasks, new DownloadGroupStatusManager ())
        {
            dsm = StatusManager as DownloadGroupStatusManager;

            transferRateDict =
                new Dictionary<HttpFileDownloadTask,long> (dsm.MaxRunningTasks);

            InitTransferTimer ();
        }

        public override void Dispose ()
        {
            Dispose (null);
        }

        public override void Dispose (AutoResetEvent handle)
        {
            if (SetDisposed ()) {
                if (transferTimer != null) {
                    transferTimer.Enabled = false;
                    transferTimer.Elapsed -= OnTransmissionTimerElapsedHandler;
                    transferTimer.Dispose ();
                    transferTimer = null;
                }

                base.Dispose (handle);
            }
        }

        private bool SetDisposed ()
        {
            bool ret = false;

            lock (SyncRoot) {
                if (!disposed) {
                    ret = disposed = true;
                }
            }

            return ret;
        }

        protected override void OnStarted ()
        {
            lock (SyncRoot) {
                transferTimer.Enabled = true;
                base.OnStarted ();
            }
        }

        protected override void OnStopped ()
        {
            lock (SyncRoot) {
                transferTimer.Enabled = false;
                base.OnStopped ();
            }
        }

        protected override void OnTaskStarted (HttpFileDownloadTask task)
        {
            lock (SyncRoot) {
                transferRateDict.Add (task, task.BytesReceived);
                base.OnTaskStarted (task);
            }
        }

        protected override void OnTaskStopped (HttpFileDownloadTask task)
        {
            lock (SyncRoot) {
                if (transferRateDict.ContainsKey (task)) {
                    long bytesLastCheck = transferRateDict[task];
                    if (task.BytesReceived > bytesLastCheck) {
                        bytesThisInterval += (task.BytesReceived - bytesLastCheck);
                    }

                    transferRateDict.Remove (task);                	
                }

                base.OnTaskStopped (task);
            }
        }

        protected virtual void SetTransferRate (long bytesPerSecond)
        {
            lock (SyncRoot) {
                dsm.SetTransferRate (bytesPerSecond);
            }
        }

        private void InitTransferTimer ()
        {
            transferTimer = new System.Timers.Timer ();

            transferTimer.Elapsed += OnTransmissionTimerElapsedHandler;
            transferTimer.Interval = (1500 * 1); // 1.5 seconds
            transferTimer.Enabled = false;
        }

        private long CalculateTransferRate ()
        {
            long bytesPerSecond;

            TimeSpan duration = (DateTime.Now - lastTick);
            double secondsElapsed = duration.TotalSeconds;

            if ((int)secondsElapsed == 0) {
                return 0;
            }

            long tmpCur;
            long tmpPrev;

            foreach (HttpFileDownloadTask dt in CurrentTasks) {
                tmpCur = dt.BytesReceived;
                tmpPrev = transferRateDict[dt];
                transferRateDict[dt] = tmpCur;

                bytesThisInterval += (tmpCur - tmpPrev);
            }

            bytesPerSecond = (long) (
                (bytesThisInterval / secondsElapsed)
            );


            lastTick = DateTime.Now;
            bytesThisInterval = 0;

            return bytesPerSecond;
        }

        protected override void Reset ()
        {
            lastTick = DateTime.Now;	

            transferRate = -1;
            transferRatePreviously = -1;

            base.Reset ();
        }

        protected virtual void OnTransmissionTimerElapsedHandler (object source,
                                                                  ElapsedEventArgs e)
        {
            lock (SyncRoot) {
                UpdateTransferRate ();
            }
        }

        protected virtual void UpdateTransferRate ()
        {
            transferRate = CalculateTransferRate ();

            if (transferRatePreviously == 0) {
                transferRatePreviously = transferRate;
            }

            transferRate = ((transferRate + transferRatePreviously) / 2);
            SetTransferRate (transferRate);
            transferRatePreviously = transferRate;
        }
	}
}
