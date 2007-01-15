/***************************************************************************
 *  DownloadQueue.cs
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
using System.Collections;

using Mono.Gettext;

// remove
using System.Threading;

using Banshee.Plugins.Podcast;

namespace Banshee.Plugins.Podcast.Download
{
    public class DownloadQueue : IEnumerable
    {
        private ArrayList downloadQueue;

        //  public event EventHandler QueueEmpty;

        public object SyncRoot { get
                                 { return downloadQueue.SyncRoot; } }
        public int Count { get
                           { return downloadQueue.Count; } }

        public DownloadQueue ()
        {
            downloadQueue = new ArrayList ();
        }

        public bool Contains (DownloadInfo dif)
        {
            return downloadQueue.Contains (dif);
        }

        public DownloadInfo this [int index]
        {
            get
            {
                return downloadQueue[index] as DownloadInfo;
            }
        }

        public IEnumerator GetEnumerator ()
        {
            return downloadQueue.GetEnumerator ();
        }

        public void Enqueue (DownloadInfo dif)
        {
            if (dif.State != DownloadState.New)
            {
                throw new ArgumentException(Catalog.GetString("dif not in 'New' state."));
            }
            else if (downloadQueue.Contains (dif))
            {
                throw new ArgumentException(Catalog.GetString("Already queued, must be unique."));
            }
            else
            {
                dif.State = DownloadState.Ready;
                downloadQueue.Add (dif);
            }

            // emit queue changed.
            // emit new Item Queued for Transfer Manager
        }

        public void Dequeue (DownloadInfo dif)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            if (!downloadQueue.Contains (dif))
            {
                throw new ArgumentException(Catalog.GetString("Item not in queue."));
            }

            downloadQueue.Remove (dif);

            // emit QueueChangedEvent
        }

        public void RepositionDownload (DownloadInfo dif, int position)
        {
            // ArgumentException if dif not in queue
            // ArgumentNullException if dif null,
            // ArgumentOutOfRangeException if position out of range, etc.
            // emit QueueChanged event
        }

        public void RepositionDownloads (DownloadInfo[] difs, int position)
        {
            // ArgumentException if difs not in queue
            // ArgumentNullException if difs null,
            // ArgumentOutOfRangeException if position out of range, etc.
            // emit QueueChanged event
        }

        public int IndexOfFirstReady ()
        {
            DownloadInfo dif = null;
            int firstReady;

            for (firstReady = 0; firstReady < downloadQueue.Count; ++firstReady)
            {
                dif = downloadQueue [firstReady] as DownloadInfo;
                if (dif.State == DownloadState.Ready)
                {
                    return firstReady;
                }
            }

            return -1;
        }

        public int IndexOfLastRunning ()
        {
            DownloadInfo dif = null;
            int maxIndex = (downloadQueue.Count-1);
            int lastRunning;

            for (lastRunning = maxIndex; lastRunning >= 0; --lastRunning)
            {
                dif = downloadQueue [lastRunning] as DownloadInfo;

                if (dif.State == DownloadState.Running)
                {
                    return lastRunning;
                }
            }

            return -1;
        }

        public DownloadInfo[] ToArray ()
        {
            return downloadQueue.ToArray (typeof (DownloadInfo)) as DownloadInfo[];
        }

        /*
          private void OnEmpty () 
          {
           EventHandler handler = QueueEmpty;
           
           if (handler != null) {
            handler (this, new EventArgs ());
           }
          } 
        */
    }
}
