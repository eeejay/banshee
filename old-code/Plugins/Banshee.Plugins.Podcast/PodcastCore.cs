/***************************************************************************
 *  PodcastCore.cs
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
using System.Net;
using System.Threading;
using System.Collections;

using Mono.Gettext;

using Gtk;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;
using Banshee.MediaEngine;

using Banshee.Plugins.Podcast.UI;
using Banshee.Plugins.Podcast.Download;

namespace Banshee.Plugins.Podcast
{
    public delegate void PodcastEventHandler (object sender, PodcastEventArgs args);
    public delegate void PodcastFeedEventHandler (object sender, PodcastFeedEventArgs args);

    public class PodcastEventArgs : EventArgs
    {
        private readonly PodcastInfo podcast;
        private readonly ICollection podcasts;

        public PodcastInfo Podcast { get
                                     { return podcast; } }
        public ICollection Podcasts { get
                                      { return podcasts; } }

        private PodcastEventArgs (PodcastInfo podcast, ICollection podcasts)
        {
            this.podcast = podcast;
            this.podcasts = podcasts;
        }

        public PodcastEventArgs (PodcastInfo podcast)
                : this (podcast, null) {}

        public PodcastEventArgs (ICollection podcasts)
                : this (null, podcasts) {}}

    public class PodcastFeedEventArgs : EventArgs
    {
        private readonly PodcastFeedInfo podcast_feed;
        private readonly ICollection podcast_feeds;

        public PodcastFeedInfo Feed { get
                                      { return podcast_feed; } }
        public ICollection Feeds { get
                                   { return podcast_feeds; } }

        private PodcastFeedEventArgs (PodcastFeedInfo podcastFeed, ICollection podcastFeeds)
        {
            podcast_feed = podcastFeed;
            podcast_feeds = podcastFeeds;
        }

        public PodcastFeedEventArgs (PodcastFeedInfo podcastFeed)
                : this (podcastFeed, null) {}
        public PodcastFeedEventArgs (ICollection podcastFeeds)
                : this (null, podcastFeeds) {}}

	public enum SyncPreference : int {
        All = 0,
        One = 1,
        None = 2
    }

    internal static class PodcastCore
    {
        private static PodcastSource source;
        private static Hashtable downloads;  // [DownloadInfo | PodcastInfo]
		private static PodcastErrorsSource errorSource;

        private static bool initialized;
        private static bool initializing;

        private static bool disposed;
        private static bool disposing;

        private static readonly object init_sync = new object ();

        public static PodcastPlugin Plugin;
        public static PodcastLibrary Library;
        public static PodcastFeedFetcher FeedFetcher;

        public static Source Source {
            get
            {
                return source as PodcastSource;
            }
        }

        public static bool IsInitialized {
            get
            {
                return initialized;
            }
        }

        internal static void Initialize (PodcastPlugin plugin)
        {
            lock (init_sync)
            {
                if (initializing || initialized)
                {
                    return;
                }
                
                disposed = false;
                initializing = true;
            }

            DownloadCore.Initialize ();
            DownloadCore.MaxDownloads = 2;

            DownloadCore.DownloadCompleted += OnDownloadCompletedHandler;
            DownloadCore.DownloadTaskStarted += OnDownloadTaskStartedHandler;
            DownloadCore.DownloadTaskStopped += OnDownloadTaskStoppedHandler;

            DownloadCore.DownloadDropped += OnDownloadDroppedHandler;
            DownloadCore.DownloadRegistered += OnDownloadRegisteredHandler;
            DownloadCore.RegistrationFailed += OnRegistrationFailedHandler;

            downloads = new Hashtable ();

            Library = new PodcastLibrary ();
            FeedFetcher = new PodcastFeedFetcher ();

            Plugin = plugin;

            PodcastDBManager.InitPodcastDatabase ();

            ServicePointManager.CertificatePolicy = new PodcastCertificatePolicy ();
                
            if(Globals.Library.IsLoaded)
            {
                LoadFromDatabase ();
            }
            else
            {
                Globals.Library.Reloaded += OnLibraryReloaded;
            } 
        }

        internal static void Dispose ()
        {
            lock (init_sync)
            {
                if (initialized && !disposing && !disposed && !initializing)
                {
                    disposing = true;
                } else {
                    return;
                }
            }

            try
            {
                if (FeedFetcher != null)
                {
                    FeedFetcher.Cancel ();
                }

                DownloadCore.DownloadCompleted -= OnDownloadCompletedHandler;
                DownloadCore.DownloadTaskStarted -= OnDownloadTaskStartedHandler;
                DownloadCore.DownloadTaskStopped -= OnDownloadTaskStoppedHandler;

                DownloadCore.DownloadDropped -= OnDownloadDroppedHandler;
                DownloadCore.DownloadRegistered -= OnDownloadRegisteredHandler;

                DownloadCore.RegistrationFailed -= OnRegistrationFailedHandler;

                DownloadCore.Dispose ();
                Library.Dispose ();
                
                DestroySource ();
            }
            finally
            {
                lock (init_sync)
                {
                    disposing = false;
                }
            }

            lock (init_sync)
            {
                initialized = false;
                disposed = true;
            }
        }

        internal static void UpdateAllFeeds ()
        {
            ArrayList feeds = null;

            if (Library.FeedCount > 0)
            {
                lock (Library.PodcastFeedSync)
                {
                    feeds = new ArrayList (Library.Feeds);
                }
            }

            if (feeds != null)
            {
                FeedFetcher.Update (feeds, false);
            }
        }

        private static DownloadInfo GetDownloadInfo (PodcastInfo pi)
        {
            return DownloadCore.CreateDownloadInfo ( pi.Url.ToString (),
                    pi.LocalDirectoryPath,
                    pi.Length);
        }

        internal static void QueuePodcastDownload (ICollection podcasts)
        {
            DownloadInfo tmp_dif;
            ArrayList podcast_difs = new ArrayList ();

            if (podcasts == null)
            {
                return;
            }

            lock (downloads.SyncRoot)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    if (pi == null || pi.DownloadInfo != null)
                    {
                        continue;
                    }

                    try
                    {
                        if (pi.CanDownload)
                        {
                            tmp_dif = GetDownloadInfo (pi);
                            pi.DownloadInfo = tmp_dif;
                            pi.DownloadFailed = false;
                            podcast_difs.Add (tmp_dif);
                            downloads.Add (tmp_dif, pi);
                        }
                    }
                    catch {
                        pi.DownloadFailed = true;
                        continue;
                    }
                }
        }

        if (podcast_difs.Count == 1)
            {
                DownloadCore.QueueDownload (podcast_difs [0] as DownloadInfo);
            }
            else if (podcast_difs.Count > 0)
            {
                DownloadCore.QueueDownload (podcast_difs);
            }
        }

        internal static void QueuePodcastDownload (PodcastInfo pi)
        {
            if (pi == null || pi.DownloadInfo != null)
            {
                return;
            }

            DownloadInfo dif;

            if (pi.CanDownload)
            {
                try
                {
                    dif = GetDownloadInfo (pi);
                    pi.DownloadInfo = dif;
                    pi.DownloadFailed = false;
                }
                catch {
                    pi.DownloadFailed = true;
                    return;
                }

                lock (downloads.SyncRoot)
                    {
                        downloads.Add (dif, pi);
                    }

                DownloadCore.QueueDownload (dif);
            }
        }

        internal static void CancelPodcastDownload (PodcastInfo pi)
        {
            if (pi == null || pi.DownloadInfo == null)
            {
                return;
            }

            DownloadInfo dif = pi.DownloadInfo;

            if (dif != null)
            {
                lock (downloads.SyncRoot)
                {
                    if (!downloads.Contains (dif))
                    {
                        return;
                    }

                    DownloadCore.Cancel (dif);
                }
            }
        }

        internal static void CancelPodcastDownload (ICollection podcasts)
        {
            if (podcasts == null)
            {
                return;
            }

            DownloadInfo tmp = null;
            ArrayList cancel_list = new ArrayList ();

            foreach (PodcastInfo pi in podcasts)
            {
                if (pi == null || pi.DownloadInfo == null)
                {
                    continue;
                }

                try
                {
                    if (pi.CanCancel)
                    {
                        tmp = pi.DownloadInfo;

                        if (tmp != null)
                        {
                            cancel_list.Add (tmp);
                        }
                    }
                }
                catch {
                    continue;
                }
            }

        if (cancel_list.Count == 1)
            {
                DownloadCore.Cancel (cancel_list [0] as DownloadInfo);
            }
            else if (cancel_list.Count > 1)
            {
                DownloadCore.Cancel (cancel_list);
            }
        }

        internal static void VisitPodcastAlley ()
        {
            Gnome.Url.Show (@"http://www.podcastalley.com");
        }

        internal static void RunSubscribeDialog ()
        {
            PodcastSubscribeDialog subscribe_dialog = new PodcastSubscribeDialog ();

            ResponseType response = (ResponseType) subscribe_dialog.Run ();
            subscribe_dialog.Destroy ();

            if (response == ResponseType.Ok)
            {
                Uri feedUri = null;

                if (subscribe_dialog.Url == String.Empty)
                {
                    return;
                }

                string url = subscribe_dialog.Url.Trim ('/').Trim ();
                SyncPreference sync = subscribe_dialog.SyncPreference;

                try
                {
                    // CreateUri should be in PodcastFeedInfo
                    feedUri = PodcastFeedFetcher.CreateUri (url);
                    SubscribeToPodcastFeed (feedUri.ToString (), sync);
                }
                catch (NotSupportedException)
                {

                    HigMessageDialog.RunHigMessageDialog (
                        null,
                        DialogFlags.Modal,
                        MessageType.Warning,
                        ButtonsType.Ok,
                        Catalog.GetString ("Uri Scheme Not Supported"),
                        Catalog.GetString ("Podcast feed URI scheme is not supported.")
                    );

                    return;
                }
                catch {

                    HigMessageDialog.RunHigMessageDialog (
                        null,
                        DialogFlags.Modal,
                        MessageType.Warning,
                        ButtonsType.Ok,
                        Catalog.GetString ("Invalid URL"),
                        Catalog.GetString ("Podcast feed URL is invalid.")
                    );

                    return;
                }
            }
        }

        internal static void SubscribeToPodcastFeed (string uri, SyncPreference sync)
        {
            PodcastFeedInfo feed = new PodcastFeedInfo (uri, sync);

            feed.UpdateFinished += InitialFeedUpdateHandler;

            if (feed != null)
            {
                ThreadAssist.Spawn ( delegate {
                                         FeedFetcher.Update (feed, true);
                                     });
            }
        }

        private static void LoadFromDatabase ()
        {
            DateTime stop;
            DateTime start;
            TimeSpan span;

            start = DateTime.Now;

            try
            {
                Library.Clear ();

                ICollection feeds = PodcastDBManager.LoadPodcastFeeds ();

                Library.AddPodcastFeeds (feeds, false);

                ArrayList podcast_list = new ArrayList ();

                foreach (PodcastFeedInfo feed in feeds)
                {
                    feed.UpdateStarted += OnFeedUpdateStatusChanged;
                    feed.UpdateFinished += OnFeedUpdateStatusChanged;

                    foreach (PodcastInfo pi in feed.Podcasts)
                    {
                        if (pi.IsActive)
                        {
                            podcast_list.Add (pi);
                        }
                    }
                }

                Library.AddPodcasts (podcast_list, false);
                
                InitSource ();

                stop = DateTime.Now;
                span = stop-start;

                lock (init_sync)
                {
                    initialized = true;
                }

                //Console.WriteLine (Catalog.GetString("Loading Podcast Feeds:  {0} ms"), span.Milliseconds);
            } catch(Exception e) {
                Console.WriteLine (Catalog.GetString("Unable to load Podcast DB"));
                Console.WriteLine(e);
            } finally
            {
                lock (init_sync)
                {
                    initializing = false;
                }
            }
        }

        private static void InitSource ()
        {
            if (source == null) {
                PlayerEngineCore.StateChanged += OnPlayerEngineStateChanged;
            
                source = new PodcastSource ();
                SourceManager.AddSource (source);

                source.Load ();
            }
        }
        
        private static void DestroySource ()
        {
            if (source != null) {
                PlayerEngineCore.StateChanged -= OnPlayerEngineStateChanged;

                SourceManager.RemoveSource (source);
                source.Dispose ();
                
                source = null;
            }
        }        

        private static void OnLibraryReloaded (object sender, EventArgs args)
        {
            Globals.Library.Reloaded -= OnLibraryReloaded;
            LoadFromDatabase ();
        }

        private static void InitialFeedUpdateHandler (object sender, UpdateActivityEventArgs args)
        {
            PodcastFeedInfo feed = sender as PodcastFeedInfo;

            if (feed != null)
            {

                feed.UpdateFinished -= InitialFeedUpdateHandler;

                feed.UpdateStarted += OnFeedUpdateStatusChanged;
                feed.UpdateFinished += OnFeedUpdateStatusChanged;

                Library.AddPodcastFeed (feed);
            }
        }

        private static void OnFeedUpdateStatusChanged (object sender, UpdateActivityEventArgs args)
        {
            source.Update ();
        }

        /*  private static void OnFeedPodcastsUpdated (object sender,
           FeedPodcastsUpdatedEventArgs args)
          {
           ICollection new_podcasts = args.NewPodcasts;
           ICollection removed_podcasts = args.RemovedPodcasts;
          
           if (removed_podcasts != null) {
            PodcastCore.Library.RemovePodcasts (removed_podcasts);
           }
           
           if (new_podcasts != null) {
            PodcastCore.Library.AddPodcasts (new_podcasts);
           }   
          }
        */
        private static void OnPlayerEngineStateChanged (object sender,
                PlayerEngineStateArgs args)
        {
            if(source != null) {
                source.Update ();
            }
        }

        private static void OnDownloadTaskStartedHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadInfo dif = args.DownloadInfo;
            DownloadTaskStartedOrStopped (dif, true);
        }

        private static void OnDownloadTaskStoppedHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadInfo dif = args.DownloadInfo;
            DownloadTaskStartedOrStopped (dif, false);
        }

        private static void OnDownloadRegisteredHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadEventAction (args, DownloadRegistered);
        }

        private static void DownloadRegistered (DownloadInfo dif)
        {
            if (downloads.Contains (dif))
            {
                PodcastInfo pi = downloads [dif] as PodcastInfo;

                if (pi == null)
                {
                    return;
                }

                pi.IsQueued = true;
            }
        }

        private static void OnRegistrationFailedHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadEventAction (args, DownloadRegistrationFailed);
        }

        private static void DownloadRegistrationFailed (DownloadInfo dif)
        {
            downloads.Remove (dif);
        }

        private static void OnDownloadDroppedHandler (object sender,
                DownloadEventArgs args)
        {
            DownloadEventAction (args, DownloadDropped);
        }

        private static void DownloadDropped (DownloadInfo dif)
        {
            if (downloads.Contains (dif))
            {
                PodcastInfo pi = downloads [dif] as PodcastInfo;

                if (pi == null)
                {
                    return;
                }

                pi.DownloadFailed = (dif.State == DownloadState.Failed);
                
                if (pi.DownloadFailed) {
           			PodcastErrorsSource.Instance.AddError (
           				dif.RemoteUri.ToString (), Catalog.GetString ("Download Failed"), null
           			);
           		}
                
                pi.DownloadInfo = null;
                pi.IsQueued = false;
                downloads.Remove (dif);
            }
        }

        private delegate void DownloadAction (DownloadInfo dif);

        private static void DownloadEventAction (DownloadEventArgs args,
                DownloadAction action)
        {
            if (args.DownloadInfo != null)
            {
                lock (downloads.SyncRoot)
                {
                    action (args.DownloadInfo);
                }
            }
            else if (args.Downloads != null)
            {
                lock (downloads.SyncRoot)
                {
                    foreach (DownloadInfo dif in args.Downloads)
                    {
                        if (dif != null)
                        {
                            action (dif);
                        }
                    }
                }
            }

            source.Update ();
        }

        private static void OnDownloadCompletedHandler (object sender,
                DownloadCompletedEventArgs args)
        {
            DownloadInfo dif = args.DownloadInfo;
            SafeUri local_uri = new SafeUri (args.LocalUri);

            if (dif == null || local_uri == null)
            {
                return;
            }

            PodcastInfo pi = null;

            lock (downloads.SyncRoot)
            {
                if (downloads.Contains (dif))
                {
                    pi = downloads [args.DownloadInfo] as PodcastInfo;
                }
            }

            if (pi != null)
            {
                TrackInfo ti = null;

                try
                {
                    try
                    {
                        ti = new LibraryTrackInfo(local_uri.LocalPath);
                    }
                    catch (ApplicationException)
                    {
                        ti = Globals.Library.TracksFnKeyed
                             [PathUtil.MakeFileNameKey(local_uri)] as TrackInfo;
                    }
                }
                catch (Exception e)
                {
                	PodcastErrorsSource.Instance.AddError (
              			local_uri.ToString (),
                		Catalog.GetString ("Unable to add file to library"),                				
                		e
           			);
                }

                pi.IsDownloaded = true;

                if (ti != null)
                {
                    pi.Track = ti;
                }
                else
                {
                    pi.DownloadFailed = true;
                    PodcastDBManager.Commit (pi);
                    return;
                }

                pi.LocalPath = local_uri.ToString ();
                PodcastDBManager.Commit (pi);
                pi.Feed.UpdateCounts ();
                
                ThreadAssist.ProxyToMain (delegate {
                                              Library.AddTrack (ti, pi, true);
                                          });
            }

            source.Update ();
        }

        private static void DownloadTaskStartedOrStopped (DownloadInfo dif, bool started)
        {
            lock (downloads.SyncRoot)
            {
                if (downloads.Contains (dif))
                {
                    PodcastInfo pi = downloads [dif] as PodcastInfo;
                    if (pi != null)
                    {
                        if (started)
                        {
                            pi.IsDownloading = true;
                        }
                        else
                        {
                            pi.IsDownloading = false;
                        }
                    }
                }
            }

            source.Update ();
        }
    }
}
