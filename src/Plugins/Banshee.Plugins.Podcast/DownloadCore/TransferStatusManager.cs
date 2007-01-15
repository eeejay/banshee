/***************************************************************************
 *  TransferStatusManager.cs
 *
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
using System.Threading;

namespace Banshee.Plugins.Podcast.Download
{
    internal class TransferStatusManager : IDisposable
    {
        private int progress = 0;

        private int speed;
        private int speedPreviously;

        private long bytesDownloaded = 0;
        private long bytesDownloadedPreviously = 0;

        private int current_downloads = 0;

        private System.Timers.Timer transferTimer;
        private DateTime lastTick = DateTime.MinValue;

        private bool is_disposed = false;

        private readonly object syncRoot = new object ();

        public long TotalLength = 0;

        public int MaximumDownloads = 0;

        public int TotalDownloads = 0;
        public int FailedDownloads = 0;
        public int SuccessfulDownloads = 0;

        //  public event EventHandler MaxDownloadsUpdated;
        public event StatusUpdatedEventHandler StatusUpdated;
        public event DownloadCompletedEventHandler DownloadCompleted;

        public event DownloadEventHandler DownloadTaskStarted;
        public event DownloadEventHandler DownloadTaskStopped;
        public event DownloadEventHandler DownloadTaskFinished;

        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public int CompletedDownloads {
            get { return (FailedDownloads + SuccessfulDownloads); }
        }

        public int CurrentDownloads {
            get { return current_downloads; }
            
            internal set
            {
                if (value == current_downloads)
                {
                    return;
                }

                if (value == 0)
                {
                    transferTimer.Enabled = false;
                }
                else if (!transferTimer.Enabled)
                {
                    transferTimer.Enabled = true;
                }

                current_downloads = value;
            }
        }

        public object SyncRoot {
            get
            { return syncRoot; }
        }

        public int TransferRate {
            get
            { return speedPreviously; }
        }

        public long BytesDownloaded {
            get
            { return bytesDownloaded; }
            set
            { SetBytesDownloaded (value); }
        }

        public int Progress {
            get {
                int ret = 0; 

                if (progress <= -1 || progress >= 100) {
                    ret = -1;
                } else {
                    ret = progress;
                }
                
                return ret;
            }
        }

        public TransferStatusManager ()
        {
            lastTick = DateTime.Now;

            transferTimer = new System.Timers.Timer ();
            transferTimer.Elapsed += OnTransmissionTimerElapsedHandler;
            transferTimer.Interval = (1 * 1000); // 1 second
            transferTimer.Enabled = false;
        }

        ~TransferStatusManager ()
        {
            Dispose ();
        }

        public void Dispose ()
        {
            lock (syncRoot)
            {
                if (!is_disposed)
                {
                    transferTimer.Close ();
                    is_disposed = true;
                    GC.SuppressFinalize (this);
                }
            }
        }

        public void Reset ()
        {
            progress = 0;

            bytesDownloaded = 0;
            bytesDownloadedPreviously = 0;

            speed = 0;
            speedPreviously = 0;

            TotalLength = 0;
            CurrentDownloads = 0;

            TotalDownloads = 0;
            FailedDownloads = 0;
            SuccessfulDownloads = 0;
            lastTick = DateTime.MinValue;
        }

        public void Register (DownloadTask dt)
        {
            dt.Started += OnDownloadTaskStartedHandler;
            dt.Stopped += OnDownloadTaskStoppedHandler;
            dt.Finished += OnDownloadTaskFinishedHandler;

            dt.LengthChanged += OnDownloadLengthChangedHandler;
            dt.ProgressChanged += OnDiscreteDownloadProgressChangedHandler;

            ++TotalDownloads;
            TotalLength += dt.Length;

            UpdateProgress ();
            OnStatusUpdated ();
        }

        public void BytesDownloadedPreviously (long bytes)
        {
            bytesDownloaded += bytes;
            bytesDownloadedPreviously += bytes;
        }

        public void DeductBytesDownloaded (long bytes)
        {
            bytesDownloaded -= bytes;
            bytesDownloadedPreviously -= bytes;
        }

        private void SetBytesDownloaded (long bytes)
        {
            if (bytes != bytesDownloaded)
            {
                if (bytes < bytesDownloaded)
                {
                    long diff = (bytesDownloaded - bytes);
                    bytesDownloadedPreviously -= diff;
                }
                bytesDownloaded = bytes;
            }
        }

        private int CalculateTransferRate ()
        {
            int kbytesPerSecond;
            double secondsElapsed;
            long bytesThisInterval;

            TimeSpan duration;

            duration = (DateTime.Now - lastTick);
            secondsElapsed = duration.TotalSeconds;

            if (secondsElapsed == 0)
            {
                return 0;
            }

            bytesThisInterval = (bytesDownloaded - bytesDownloadedPreviously);
            kbytesPerSecond = (int)(((bytesThisInterval / 1024) / secondsElapsed));

            lastTick = DateTime.Now;
            bytesDownloadedPreviously = bytesDownloaded;

            return kbytesPerSecond;
        }


        internal void IdleDownloadCanceled (DownloadInfo dif, DownloadTask dt)
        {
            Finished (dif, dt);
        }

        private void Finished (DownloadInfo dif, DownloadTask dt)
        {
            if (dif == null || dt == null)
            {
                return;
            }

            if (dif.State == DownloadState.Canceled ||
                    dif.State == DownloadState.Failed)
            {

                lock (syncRoot)
                {
                    if (dif.State == DownloadState.Failed)
                    {
                        ++FailedDownloads;
                    }
                    else
                    {
                        --TotalDownloads;
                    }

                    DeductBytesDownloaded (dt.BytesReceived);
                    UpdateProgress ();
                    TotalLength -= dt.Length;
                }

                OnDownloadStateChanged (DownloadTaskFinished, new DownloadEventArgs (dif));

            }
            else if (dif.State == DownloadState.Completed)
            {
                lock (syncRoot)
                {
                    ++SuccessfulDownloads;
                }

                OnDownloadCompleted (dif);

                OnDownloadStateChanged (DownloadTaskFinished, new DownloadEventArgs (dif));
            }
        }

        protected virtual void OnTransmissionTimerElapsedHandler(object source, ElapsedEventArgs e)
        {
            lock (syncRoot)
            {
                speed = CalculateTransferRate ();
                speed = ((speed + speedPreviously) / 2);
                speedPreviously = speed;

                UpdateProgress ();
            }

            OnStatusUpdated ();
        }

        protected virtual void UpdateProgress ()
        {
            if (TotalLength != 0)
            {
                progress = Convert.ToInt32(((bytesDownloaded * 100) / TotalLength));
            }
        }

        protected virtual void OnDownloadLengthChangedHandler (object sender,
                DownloadLengthChangedEventArgs args)
        {
            lock (syncRoot)
            {
                TotalLength -= args.PreviousLength;
                TotalLength += args.CurrentLength;
            }
        }

        protected virtual void OnStatusUpdated ()
        {
            StatusUpdatedEventHandler handler = StatusUpdated;

            if (handler != null)
            {
                StatusUpdatedEventArgs args;

                lock (syncRoot)
                {
                    args = new StatusUpdatedEventArgs ( speed, TotalDownloads,
                                                        FailedDownloads, CompletedDownloads,
                                                        Progress, CurrentDownloads);
                }

                handler (this, args);
            }
        }

        protected virtual void OnDownloadStateChanged (DownloadEventHandler handler,
                DownloadEventArgs args)
        {
            DownloadEventHandler local_handler = handler;

            if (local_handler != null)
            {
                local_handler (this, args);
            }
        }

        protected virtual void OnDownloadTaskStartedHandler (object sender,
                DownloadEventArgs args)
        {
            lock (syncRoot)
            {
                ++CurrentDownloads;
            }

            //OnStatusUpdated ();
            OnDownloadStateChanged (DownloadTaskStarted, args);
        }

        protected virtual void OnDownloadTaskStoppedHandler (object sender,
                DownloadEventArgs args)
        {
            lock (syncRoot)
            {
                --CurrentDownloads;
            }

            //OnStatusUpdated ();
            OnDownloadStateChanged (DownloadTaskStopped, args);
        }

        protected virtual void OnDownloadTaskFinishedHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadInfo dif = args.DownloadInfo;
            DownloadTask dt = sender as DownloadTask;

            Finished (dif, dt);
            OnStatusUpdated ();
        }

        protected virtual void OnDiscreteDownloadProgressChangedHandler (object sender,
                DiscreteDownloadProgressEventArgs args)
        {
            long bytesThisInterval = args.BytesReceivedThisInterval;
            DownloadInfo dif = (DownloadInfo) args.UserState;

            lock (dif.SyncRoot)
            {
                if (!dif.Active)
                {
                    return;
                }
            }

            lock (syncRoot)
            {
                if (args.BytesReceived == bytesThisInterval)
                {
                    BytesDownloadedPreviously (bytesThisInterval);
                }
                else
                {
                    BytesDownloaded += bytesThisInterval;
                }
            }
        }

        protected virtual void OnDownloadCompleted (DownloadInfo dif)
        {
            DownloadCompletedEventHandler handler = DownloadCompleted;

            lock (syncRoot)
            {
                UpdateProgress ();
            }

            OnStatusUpdated ();

            if (handler != null)
            {
                handler (this, new DownloadCompletedEventArgs (dif, new Uri (dif.LocalPath)));
            }
        }
    }
}
