/***************************************************************************
 *  DownloadManagerInterface.cs
 *
 *  Copyright (C) 2008 Michael C. Urbanski
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
using System.ComponentModel;

using Gtk;

using Migo.TaskCore;
using Migo.DownloadCore;

using System.Threading;

namespace Banshee.Podcasting.Gui
{
    public class DownloadManagerInterface : IDisposable
    {
        private DownloadManager manager;
        private DownloadUserJob downloadJob;
        //private DownloadManagerSource downloadSource;

        private readonly object sync = new object ();

        public DownloadManagerInterface (DownloadManager manager)
        {
            if (manager == null) {
                throw new ArgumentNullException ("manager");
            }

            this.manager = manager;
        }

        public void Dispose ()
        {
            lock (sync) {
                if (manager != null) {
                    manager.Group.Started -= OnManagerStartedHandler;
                    manager.Group.Stopped -= OnManagerStoppedHandler;
                    manager.Group.ProgressChanged -= OnManagerProgressChangedHandler;
                    manager.Group.StatusChanged -= OnManagerStatusChangedHandler;

                    manager = null;
                }
            }

            Gtk.Application.Invoke (delegate {
                lock (sync) {
                    if (downloadJob != null) {
                        downloadJob.CancelRequested -= OnCancelRequested;
                        downloadJob.Finish ();
                        downloadJob = null;
                        //SourceManager.RemoveSource (downloadSource);
                        //downloadSource = null;
                    }
                }
            });
        }

        public void Initialize ()
        {
            //downloadSource = new DownloadManagerSource (manager);
            manager.Group.Started += OnManagerStartedHandler;
            manager.Group.Stopped += OnManagerStoppedHandler;
            manager.Group.ProgressChanged += OnManagerProgressChangedHandler;
            manager.Group.StatusChanged += OnManagerStatusChangedHandler;
        }

        private void OnManagerStartedHandler (object sender, EventArgs e)
        {
            Gtk.Application.Invoke (delegate {
                lock (sync) {
                    if (downloadJob == null) {
                        //SourceManager.AddSource (downloadSource);

                        downloadJob = new DownloadUserJob ();
                        downloadJob.CancelRequested += OnCancelRequested;
                        downloadJob.Register ();
                    }
                }
            });
        }

        private void OnManagerStoppedHandler (object sender, EventArgs e)
        {
            Gtk.Application.Invoke (delegate {
                lock (sync) {
                    if (downloadJob != null) {
                        downloadJob.CancelRequested -= OnCancelRequested;
                        downloadJob.Finish ();
                        downloadJob = null;
                        //SourceManager.RemoveSource (downloadSource);
                    }
                }
            });
        }

        private void OnManagerProgressChangedHandler (object sender,
                                                      ProgressChangedEventArgs e)
        {
            Application.Invoke (delegate {
                lock (sync) {
                    if (downloadJob != null) {
                        downloadJob.UpdateProgress (e.ProgressPercentage);
                    }
                }
            });
        }

        private void OnManagerStatusChangedHandler (object sender,
                                                    GroupStatusChangedEventArgs e)
        {
            DownloadGroupStatusChangedEventArgs args = e as DownloadGroupStatusChangedEventArgs;

            Application.Invoke (delegate {
                lock (sync) {
                    if (downloadJob != null) {
                        downloadJob.UpdateStatus (args.RunningTasks, args.RemainingTasks, args.CompletedTasks, args.BytesPerSecond);
                    }
                }
            });
        }

        private void OnCancelRequested (object sender, EventArgs e)
        {
            manager.Group.CancelAsync ();
        }
    }
}
