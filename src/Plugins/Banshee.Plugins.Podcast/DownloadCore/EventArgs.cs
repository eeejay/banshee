/***************************************************************************
 *  EventArgs.cs
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

namespace Banshee.Plugins.Podcast.Download
{
    public class DiscreteDownloadProgressEventArgs : DownloadProgressChangedEventArgs
    {
        private readonly long bytes_received_this_interval;

        public long BytesReceivedThisInterval
        {
            get
            { return bytes_received_this_interval; }
        }

        public DiscreteDownloadProgressEventArgs (int progressPercentage,
                object userState, long bytesReceived, long totalBytesToReceive,
                long bytesReadThisinterval)
                : base (progressPercentage, userState, bytesReceived, totalBytesToReceive)
        {
            bytes_received_this_interval = bytesReadThisinterval;
        }
    }

    public class StatusUpdatedEventArgs : EventArgs
    {
        protected readonly int speed;
        protected readonly int progress;
        protected readonly int totalDownloads;
        protected readonly int failedDownloads;
        protected readonly int currentDownloads;
        protected readonly int downloadsComplete;

        public int Speed { get
                           { return speed; } }
        public int Progress { get
                              { return progress; } }
        public int TotalDownloads { get
                                    { return totalDownloads; } }
        public int FailedDownloads { get
                                     { return failedDownloads; } }
        public int CurrentDownloads { get
                                      { return currentDownloads; } }
        public int DownloadsComplete { get
                                       { return downloadsComplete; } }

        public StatusUpdatedEventArgs (int speed, int totalDownloads, int failedDownloads,
                                       int downloadsComplete, int progress, int currentDownloads)
        {
            this.speed = speed;
            this.progress = progress;
            this.totalDownloads = totalDownloads;
            this.downloadsComplete = downloadsComplete;
            this.failedDownloads = failedDownloads;
            this.currentDownloads = currentDownloads;
        }
    }

    public class DownloadEventArgs : EventArgs
    {
        private readonly DownloadInfo dif;
        private readonly ICollection downloads;

        public DownloadInfo DownloadInfo { get
                                           { return dif; } }
        public ICollection  Downloads { get
                                        { return downloads; } }

        public DownloadEventArgs (DownloadInfo downloadInfo, ICollection downloads)
        {
            dif = downloadInfo;
            this.downloads = downloads;
        }

        public DownloadEventArgs (DownloadInfo downloadInfo)
                : this (downloadInfo, null) {}
        public DownloadEventArgs (ICollection downloads)
                : this (null, downloads) {}}

    public class DownloadCompletedEventArgs : EventArgs
    {
        private readonly Uri uri;
        private readonly DownloadInfo dif;

        public Uri LocalUri { get
                              { return uri; } }
        public DownloadInfo DownloadInfo { get
                                           { return dif; } }

        public DownloadCompletedEventArgs (DownloadInfo downloadInfo, Uri localUri)
        {
            uri = localUri;
            dif = downloadInfo;
        }
    }

}
