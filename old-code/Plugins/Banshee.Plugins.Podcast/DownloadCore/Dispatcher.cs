/***************************************************************************
 *  Dispatcher.cs
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
using System.IO;
using System.Threading;
using System.Collections;

using Mono.Gettext;

using Banshee.Plugins.Podcast;

namespace Banshee.Plugins.Podcast.Download
{
    internal class Dispatcher : IDisposable
    {
        private bool enabled;
        private bool initialized;

        private readonly object enabled_sync = new object ();
        private readonly object init_sync = new object ();

        private Thread dispatchThread;
        private object dispatchThreadMonitor = new object ();

        private TransferStatusManager tsm;
        private DownloadQueue download_queue;

        private Hashtable registered_downloads;  // [ DownloadInfo | DownloadTask ]

        private readonly object syncRoot = new object ();

        public int MaxDownloads {
            get
                { lock (tsm.SyncRoot)
                { return tsm.MaximumDownloads; } }
            set
                { lock (tsm.SyncRoot)
                { SetMaxDownloads (value); } }
        }

        public bool Enabled {
            get
                { lock (enabled_sync)
                { return enabled; } }
            set
                { lock (enabled_sync)
                { enabled = value; 
                  Monitor.Enter(dispatchThreadMonitor);
                  Monitor.Pulse(dispatchThreadMonitor);
                  Monitor.Exit(dispatchThreadMonitor);
                } }
        }

        public object SyncRoot { get
                                 { return syncRoot; } }

        internal Dispatcher (DownloadQueue downloadQueue, TransferStatusManager statusManager)
        {
            if (downloadQueue == null)
            {
                throw new ArgumentNullException ("downloadQueue");
            }
            else if (statusManager == null)
            {
                throw new ArgumentNullException ("statusManager");
            }

            Enabled = false;

            tsm = statusManager;
            download_queue = downloadQueue;

            registered_downloads = new Hashtable ();
        }

        public void Initialize ()
        {
            lock (init_sync)
            {
                dispatchThread = new Thread (PumpQueue);
                dispatchThread.IsBackground = true;
                dispatchThread.Start();

                initialized = true;
            }
        }

        public void Dispose ()
        {
            lock (init_sync)
            {
                if (initialized)
                {
                    Enabled = false;

                    if (dispatchThread.IsAlive)
                    {
                        dispatchThread.Abort ();
                    }
                    initialized = false;
                }
            }
        }

        public void Register (DownloadInfo dif, DownloadTask dt)
        {
            lock (registered_downloads.SyncRoot)
            {
                registered_downloads.Add (dif, dt);
            }
        }

        public void Drop (DownloadInfo dif)
        {
            lock (registered_downloads.SyncRoot)
            {
                registered_downloads.Remove (dif);
            }
        }

        private void SetMaxDownloads (int max)
        {
            if (max < 0)
            {
                throw new ArgumentOutOfRangeException(Catalog.GetString
                        ("Maximum number of concurrent downloads cannot be less than 0."));
            }

            int delta = 0;
            int lastRunning = -1;
            DownloadInfo dif = null;

            lock (download_queue.SyncRoot)
            {
                lock (tsm.SyncRoot)
                {
                    if (tsm.MaximumDownloads == max)
                    {
                        return;
                    }

                    tsm.MaximumDownloads = max;

                    if (tsm.CurrentDownloads > tsm.MaximumDownloads)
                    {
                        delta = tsm.CurrentDownloads - tsm.MaximumDownloads;
                    }

                    lastRunning = download_queue.IndexOfLastRunning ();

                    for (int i = 0; i < delta && lastRunning >= 0; ++i, --lastRunning)
                    {
                        dif = download_queue [lastRunning];
                        lock (dif.SyncRoot)
                        {
                            if (dif.State == DownloadState.Running)
                            {
                                dif.State = DownloadState.Queued;
                            }
                        }
                    }
                }
            }
        }

        private void PumpQueue ()
        {
            Thread.CurrentThread.Name = "PumpQueue";

            int index;
            DownloadInfo dif = null;

            while(true)
            {
                if (Enabled)
                {
                    dif = null;
                    lock (download_queue.SyncRoot)
                    {
                        lock (tsm.SyncRoot)
                        {
                            if (download_queue.Count > 0 &&
                                    download_queue.Count != tsm.CurrentDownloads)
                            {
                                if (tsm.CurrentDownloads < tsm.MaximumDownloads )
                                {
                                    index = download_queue.IndexOfFirstReady ();
                                    if (index >= 0)
                                    {
                                        dif = download_queue [index];
                                        dif.State = DownloadState.Running;
                                    }
                                }
                            }
                        }
                    }

                    if (dif != null)
                    {
                        StartDownloadTask (dif);
                    }
                } else {
                    Monitor.Enter(dispatchThreadMonitor);    
                    Monitor.Wait(dispatchThreadMonitor);
                    Monitor.Exit(dispatchThreadMonitor);
                }

                Thread.Sleep(32);
            }
        }

        private void StartDownloadTask (DownloadInfo dif)
        {
            if (dif == null)
            {
                return;
            }

            DownloadTask dt = null;

            lock (registered_downloads.SyncRoot)
            {
                if (registered_downloads.Contains (dif))
                {
                    dt = registered_downloads [dif] as DownloadTask;
                }
                else
                {
                    return;
                }
            }

            if (dt != null)
            {
                dt.Execute ();
            }
        }
    }
}
