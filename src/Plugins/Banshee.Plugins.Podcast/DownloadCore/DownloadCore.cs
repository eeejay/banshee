/***************************************************************************
 *  DownloadCore.cs
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
using System.Collections;

using Mono.Gettext;

using Gtk;

using Banshee.Base;
using Banshee.Widgets;
using Banshee.Plugins;

namespace Banshee.Plugins.Podcast.Download
{
    public enum DownloadState : int
    {
        Canceled = 0,
        CancelRequested = 1,
        Completed = 2,
        Failed = 3,
        New = 4,
        Paused = 5,
        Queued = 6,
        Ready = 7,
        Running = 8
    }

    public class RegistrationException : Exception
    {
        public RegistrationException (string message) : base (message) {}}

    public static class DownloadCore
    {
        // This is redundant
        private static Hashtable downloads; // [ Uri path+uri | DownloadInfo ]

        private static Dispatcher dispatcher;
        private static TransferStatusManager tsm;
        private static DownloadQueue download_queue;

        private static bool disposed;
        private static bool disposing;

        private static bool initialized;
        private static bool initializing;

        private static bool cancel_requested;

        private static ActiveUserEvent userEvent;

        // Come up with a better solution for efficiently handling mass cancelations
        private static ArrayList dropped = new ArrayList ();
        private static ArrayList mass_drop_queue = new ArrayList ();

        private static readonly object mass_drop_sync = new object ();
        private static readonly object register_drop_sync = new object ();

        public static event DownloadEventHandler DownloadDropped;
        public static event DownloadEventHandler DownloadRegistered;

        public static event DownloadEventHandler RegistrationFailed;

        // This should all be changed or the 'TransferStatusManager' should be static //
        public static event DownloadEventHandler DownloadTaskStarted
        {
            add
                { lock (tsm.SyncRoot)
                { tsm.DownloadTaskStarted += value; } }
            remove
                { lock (tsm.SyncRoot)
                { tsm.DownloadTaskStarted -= value; } }
        }

        public static event DownloadEventHandler DownloadTaskStopped
        {
            add
                { lock (tsm.SyncRoot)
                { tsm.DownloadTaskStopped += value; } }
            remove
                { lock (tsm.SyncRoot)
                { tsm.DownloadTaskStopped -= value; } }
        }

        public static event DownloadEventHandler DownloadTaskFinished
        {
            add
                { lock (tsm.SyncRoot)
                { tsm.DownloadTaskFinished += value; } }
            remove
                { lock (tsm.SyncRoot)
                { tsm.DownloadTaskFinished -= value; } }
        }

        public static event DownloadCompletedEventHandler DownloadCompleted
        {
            add
                { lock (tsm.SyncRoot)
                { tsm.DownloadCompleted += value; } }
            remove
                { lock (tsm.SyncRoot)
                { tsm.DownloadCompleted -= value; } }
        }

        public static event StatusUpdatedEventHandler StatusUpdated
        {
            add
                { lock (tsm.SyncRoot)
                { tsm.StatusUpdated += value; } }
            remove
                { lock (tsm.SyncRoot)
                { tsm.StatusUpdated -= value; } }
        }

        public static event DownloadProgressChangedEventHandler DownloadProgressChanged
        {
            add
                { lock (tsm.SyncRoot)
                { tsm.DownloadProgressChanged += value; } }
            remove
                { lock (tsm.SyncRoot)
                { tsm.DownloadProgressChanged -= value; } }
        }
        // ------------------------------------------------------------- //

        public static int MaxDownloads {
            get
            { return dispatcher.MaxDownloads; }
            set
            { dispatcher.MaxDownloads = value; }
        }

        public static bool Canceling {
            get
            { return cancel_requested; }
        }

        private static void Register (DownloadInfo dif, bool emitEvent)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            lock (register_drop_sync)
            {
                if (disposing || disposed)
                {
                    throw new RegistrationException (Catalog.GetString("DownloadCore is shutting down."));
                }
                else if (dif.State != DownloadState.New)
                {
                    throw new RegistrationException (Catalog.GetString("dif not in 'New' state."));
                }

                DownloadTask dt = null;

                lock (downloads.SyncRoot)
                {
                    try
                    {
                        dt = DownloadTask.Create (dif);
                    }
                    catch (NotSupportedException)
                    {
                        throw new RegistrationException (Catalog.GetString("Uri scheme not supported."));
                    }
                    catch (Exception e)
                    {
                        throw new RegistrationException (e.Message);
                    }

                    if (downloads.ContainsKey (dif.UniqueKey))
                    {
                        throw new RegistrationException (Catalog.GetString("Download already queued."));
                    }

                    if (downloads.Count == 0)
                    {
                        CreateUserEvent();
                        dispatcher.Enabled = true;
                    }

                    downloads.Add (dif.UniqueKey, dt);
                }

                lock (tsm.SyncRoot)
                {
                    tsm.Register (dt);
                }

                lock (dispatcher.SyncRoot)
                {
                    dispatcher.Register (dif, dt);
                }

                lock (download_queue.SyncRoot)
                {
                    download_queue.Enqueue (dif);
                }

            }

            if (emitEvent)
            {
                EmitDownloadRegistered (new DownloadEventArgs (dif));
            }
        }

        private static void Drop (DownloadInfo dif, bool emitEvent)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            lock (register_drop_sync)
            {
                lock (downloads.SyncRoot)
                {
                    downloads.Remove (dif.UniqueKey);
                }

                lock (dispatcher.SyncRoot)
                {
                    dispatcher.Drop (dif);
                }

                lock (download_queue.SyncRoot)
                {
                    download_queue.Dequeue (dif);
                }

                lock (downloads.SyncRoot)
                {
                    if (downloads.Count == 0)
                    {
                        dispatcher.Enabled = false;

                        DestroyUserEvent ((dif.State == DownloadState.Canceled));

                        lock (tsm.SyncRoot)
                        {
                            tsm.Reset ();
                        }
                    }
                }
            }

            if (emitEvent)
            {
                EmitDownloadDropped (new DownloadEventArgs (dif));
            }
        }

        public static DownloadInfo CreateDownloadInfo (string uri, string path, long length)
        {
            if (uri == null || uri == String.Empty)
            {
                throw new ArgumentException (Catalog.GetString("uri is empty"));
            }
            else if (path == null || path == String.Empty)
            {
                throw new ArgumentException (Catalog.GetString("path is empty"));
            }

            DownloadInfo dif = new DownloadInfo (uri, path, length);

            return dif;
        }

        public static ICollection QueueDownload (ICollection difs)
        {
            if (Canceling)
            {
                return null;
            }

            ArrayList registered_downloads = new ArrayList ();
            ArrayList unregistered_downloads = new ArrayList ();

            foreach (DownloadInfo dif in difs)
            {
                try
                {
                    Register (dif, false);
                    registered_downloads.Add (dif);
                }
                catch {
                    unregistered_downloads.Add (dif);
                    continue;
                }
            }

        if (unregistered_downloads.Count == 1)
            {
                EmitRegistrationFailed (new DownloadEventArgs (
                                            unregistered_downloads [0] as DownloadInfo)
                                       );
            }
            else if (unregistered_downloads.Count > 1)
            {
                EmitRegistrationFailed (new DownloadEventArgs (unregistered_downloads));
            }

            if (registered_downloads.Count == 1)
            {
                EmitDownloadRegistered (new DownloadEventArgs (
                                            registered_downloads [0] as DownloadInfo)
                                       );
            }
            else if (registered_downloads.Count > 1)
            {
                EmitDownloadRegistered (new DownloadEventArgs (registered_downloads));
            }

            return (ICollection) registered_downloads;
        }

        public static bool QueueDownload (DownloadInfo dif)
        {
            if (Canceling)
            {
                return false;
            }

            bool ret = false;

            try
            {
                Register (dif, true);
                ret = true;
            }
            catch (Exception e)
            {
                Console.WriteLine (e.Message);
                EmitRegistrationFailed (new DownloadEventArgs (dif));
            }

            return ret;
        }

        public static void Cancel (DownloadInfo dif)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            if (dif.State == DownloadState.Canceled ||
                    dif.State == DownloadState.CancelRequested)
            {
                return;
            }

            if (dif.State != DownloadState.Running)
            {
                DownloadTask dt = null;
                lock (downloads.SyncRoot)
                {
                    if (downloads.ContainsKey (dif.UniqueKey))
                    {
                        dif.State = DownloadState.Canceled;
                        dt = downloads [dif.UniqueKey] as DownloadTask;
                    }
                }

                if (dt != null)
                {
                    tsm.IdleDownloadCanceled (dif, dt);
                }

            }
            else
            {
                dif.State = DownloadState.CancelRequested;
            }
        }

        public static void Cancel (ICollection downloads)
        {
            if (downloads == null)
            {
                throw new ArgumentNullException ("downloads");
            }

            lock (mass_drop_sync)
            {
                foreach (DownloadInfo dif in downloads)
                {
                    if (dif != null)
                    {
                        mass_drop_queue.Add (dif);
                    }
                }
            }

            foreach (DownloadInfo dif in downloads)
            {
                Cancel (dif);
            }
        }

        public static void CancelAll ()
        {
            DownloadInfo[] downloads = null;

            dispatcher.Enabled = false;

            lock (register_drop_sync)
            {
                lock (download_queue.SyncRoot)
                {
                    downloads = download_queue.ToArray ();
                }
            }

            Cancel (downloads);
        }

        public static void Pause (DownloadInfo dif)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            lock (download_queue.SyncRoot)
            {
                if (download_queue.Contains (dif))
                {
                    dif.State = DownloadState.Paused;
                }
            }
        }

        public static void Unpause (DownloadInfo dif)
        {
            if (dif == null)
            {
                throw new ArgumentNullException ("dif");
            }

            lock (download_queue.SyncRoot)
            {
                if (download_queue.Contains (dif))
                {

                    lock (dif.SyncRoot)
                    {
                        if (dif.State == DownloadState.Paused)
                        {
                            dif.State = DownloadState.Ready;
                        }
                    }
                }
            }
        }

        private static void CreateUserEvent ()
        {
            ThreadAssist.ProxyToMain(CreateUserEventProxy);
        }

        private static void CreateUserEventProxy(object o, EventArgs args)
        {
            if(userEvent == null) {
                userEvent = new ActiveUserEvent(Catalog.GetString("Download"));
                userEvent.Icon = IconThemeUtils.LoadIcon (Stock.Network, 22);
                userEvent.Header = Catalog.GetString ("Downloading Files");
                userEvent.Message = Catalog.GetString ("Initializing downloads");
                userEvent.CancelRequested += OnUserEventCancelRequestedHandler;
                cancel_requested = false;
            }
        }


        private static void DestroyUserEvent (bool canceled)
        {
            // TODO:  Track down deadlock.
            // Issue appears when run on main thread during app shutdown.
            //ThreadAssist.ProxyToMain ( delegate {
            if(userEvent != null)
            {
                if (!canceled)
                {
                    Thread.Sleep (250);
                }

                userEvent.Dispose ();
                userEvent = null;
                cancel_requested = false;
            }
            //});
        }

        private static void OnUserEventCancelRequestedHandler (object sender, EventArgs args)
        {
            ThreadAssist.ProxyToMain(OnUserEventCancelRequestedHandlerProxy);
        }

        private static void OnUserEventCancelRequestedHandlerProxy(object o, EventArgs args)
        {
            if(userEvent != null) {
                userEvent.CancelRequested -= OnUserEventCancelRequestedHandler;
                cancel_requested = true;
                userEvent.CanCancel = false;
                userEvent.Progress = 0.0;
                userEvent.Header = Catalog.GetString ("Canceling Downloads");
                userEvent.Message = Catalog.GetString ("Waiting for downloads to terminate");

                ThreadAssist.Spawn(new ThreadStart(CancelAll));
            }
        }

        private static void OnDownloadTaskFinishedHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadInfo dif = args.DownloadInfo;
            DownloadInfo[] difs = null;

            if (dif == null)
            {
                return;
            }

            bool drop_queued = false;

            lock (mass_drop_sync)
            {
                if (mass_drop_queue.Contains (dif))
                {
                    dropped.Add (dif);
                    drop_queued = true;
                }

                if (mass_drop_queue.Count > 0 &&
                        dropped.Count == mass_drop_queue.Count)
                {
                    difs = dropped.ToArray (typeof (DownloadInfo)) as DownloadInfo[];

                    dropped.Clear ();
                    mass_drop_queue.Clear ();
                }
            }

            if (difs != null)
            {
                ArrayList dropped_downloads = new ArrayList (difs.Length);

                foreach (DownloadInfo d in difs)
                {
                    try
                    {
                        Drop (d, false);
                        dropped_downloads.Add (d);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                if (dropped_downloads.Count == 1)
                {
                    EmitDownloadDropped (new DownloadEventArgs (
                                             dropped_downloads [0] as DownloadInfo)
                                        );
                }
                else if (dropped_downloads.Count > 1)
                {
                    EmitDownloadDropped (new DownloadEventArgs (dropped_downloads));
                }

                EmitDownloadDropped (new DownloadEventArgs (dropped_downloads));
            }
            else if (!drop_queued)
            {
                try
                {
                    Drop (dif, true);
                }
                catch {}
            }
    }

    private static void OnStatusUpdatedHandler (object sender, StatusUpdatedEventArgs args)
        {
            string message;
            string disp_progress;
            double progress;
            
            if (args.Progress != -1) {
                progress = (double) args.Progress / 100;
            } else {
                progress = 0.0;
            }

            // TODO  This could be shortened up
            if (args.FailedDownloads == 0)
            {
                disp_progress = String.Format (Catalog.GetPluralString (
                   "Downloading File",
                   "Downloading Files ({0} of {1} completed)",
                   args.TotalDownloads),
                   args.DownloadsComplete, args.TotalDownloads
                );
            }
            else
            {
                disp_progress = String.Format (Catalog.GetString (
                    "Downloading Files ({0} of {1} completed)\n{2} failed"),
                    args.DownloadsComplete, args.TotalDownloads, args.FailedDownloads);
            }


	        message = String.Format (Catalog.GetPluralString (
	            "Currently transfering 1 file at {0} kB/s",
				"Currently transfering {1} files at {0} kB/s", 
				args.CurrentDownloads),
                args.Speed, args.CurrentDownloads
            );

            ThreadAssist.ProxyToMain ( delegate {
                                           if (userEvent != null)
                                       {
                                           if (!cancel_requested)
                                               {
                                                   userEvent.Header = disp_progress;
                                                   userEvent.Message = message;
                                                   userEvent.Progress = progress;
                                               }
                                           }
                                       });
        }

        public static void Initialize ()
        {
            lock (register_drop_sync)
            {
                if (initializing || initialized)
                {
                    return;
                }

                initializing = true;
            }

            try
            {
                tsm = new TransferStatusManager ();
                tsm.DownloadTaskFinished += OnDownloadTaskFinishedHandler;
                tsm.StatusUpdated += OnStatusUpdatedHandler;

                download_queue = new DownloadQueue ();
                dispatcher = new Dispatcher (download_queue, tsm);
                downloads = new Hashtable ();

                dispatcher.Initialize ();
            }
            finally
            {
                lock (register_drop_sync)
                {
                    initializing = false;
                }
            }

            lock (register_drop_sync)
            {
                initialized = true;
            }

            // Attach NetworkStateChange event handlers
        }

        public static void Dispose ()
        {
            lock (register_drop_sync)
            {
                if (initialized && !disposing)
                {
                    disposing = true;
                }
            }

            try
            {
                if (downloads != null)
                {
                    CancelAll ();

                    int count = 0;

                    do
                    {
                        lock (downloads.SyncRoot)
                        {
                            count = downloads.Count;
                        }
                    }
                    while (count != 0);
                }

                if (dispatcher != null)
                {
                    dispatcher.Dispose ();
                    dispatcher = null;
                }

                download_queue = null;

                if (tsm != null)
                {
                    tsm.Dispose ();
                    tsm = null;
                }
            }
            finally
            {
                lock (register_drop_sync)
                {
                    disposing = false;
                }
            }

            lock (register_drop_sync)
            {
                disposed = false;
                initialized = false;
            }
        }

        private static void EmitDownloadRegistered (DownloadEventArgs args)
        {
            EmitDownloadEvent (DownloadRegistered, args);
        }

        private static void EmitDownloadDropped (DownloadEventArgs args)
        {
            EmitDownloadEvent (DownloadDropped, args);
        }

        private static void EmitRegistrationFailed (DownloadEventArgs args)
        {
            EmitDownloadEvent (RegistrationFailed, args);
        }

        private static void EmitDownloadEvent (DownloadEventHandler handler, DownloadEventArgs args)
        {
            if (handler != null)
            {
                handler (null, args);
            }
        }
    }
}
