/***************************************************************************
 *  PodcastFeedFetcher.cs
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
using System.Xml;
using System.Threading;
using System.Collections;

using Mono.Gettext;

using Gtk;

using Banshee.Base;
using Banshee.Widgets;

using Banshee.Plugins.Podcast.UI;

namespace Banshee.Plugins.Podcast
{
    public delegate void UpdateActivityEventHandler (object sender, UpdateActivityEventArgs args);

    public class UpdateActivityEventArgs : EventArgs
    {
        private readonly PodcastFeedInfo podcast_feed;

        public PodcastFeedInfo Feed
        {
            get { return podcast_feed; }
        }

        public UpdateActivityEventArgs (PodcastFeedInfo podcastFeed)
        {
            podcast_feed = podcastFeed;
        }
    }

    internal class PodcastFeedFetcher
    {
        private ActiveUserEvent userEvent;

        private bool updating;
        private int totalFeeds;
        private int currentFeed;
        private ArrayList update_queue;

        private readonly object update_sync = new object ();

        private delegate void PodcastFeedUpdater (ICollection feeds);

        public bool IsUpdating {
            get
            {
                return updating;
            }

            private set
            {
                lock (update_sync)
                {
                    updating = value;
                }
            }
        }

        public PodcastFeedFetcher ()
        {
            update_queue = new ArrayList ();
        }

        public static Uri CreateUri (string uri)
        {
            Uri tmpUri;

            try
            {
                tmpUri = new Uri (uri);
            }
            catch {
                throw;
            }

            if (!(tmpUri.Scheme == Uri.UriSchemeHttp ||
                        tmpUri.Scheme == Uri.UriSchemeHttps))
                {
                    throw new  NotSupportedException (Catalog.GetString("Uri scheme not supported."));
                }

            return tmpUri;
        }

        public void Update (PodcastFeedInfo feed)
        {
            Update (feed, true);
        }

        public void Update (ICollection feeds)
        {
            Update (feeds, true);
        }

        public void Update (ICollection feeds, bool queue_if_busy)
        {
            if (feeds == null)
            {
                return;
            }

            DoUpdate (feeds, queue_if_busy);
        }

        public void Update (PodcastFeedInfo feed, bool queue_if_busy)
        {
            if (feed == null)
            {
                return;
            }

            // drrr...
            PodcastFeedInfo[] wrapper = new PodcastFeedInfo [1];
            wrapper.SetValue (feed, 0);

            DoUpdate (wrapper, queue_if_busy);
        }

        private void DoUpdate (ICollection feeds, bool queue_if_busy)
        {
            lock (update_sync)
            {
                if (!updating)
                {
                    updating = true;

                    QueueAdd (feeds, false);

                    currentFeed = 0;
                    totalFeeds = update_queue.Count;

                    ThreadAssist.ProxyToMain (delegate {
                                                  try
                                                  {
                                                      CreateUserEvent ();
                                                      ThreadAssist.Spawn (new ThreadStart (UpdateThread));
                                                  }
                                                  catch {
                                                      EndUpdate ();
                                                      return;
                                                  }
                                              });

                }
                else if (queue_if_busy && !userEvent.IsCancelRequested)
                {
                    QueueAdd (feeds, true);
                }
            }
        }

        private void QueueAdd (ICollection feeds, bool updateStatus)
        {
            foreach (PodcastFeedInfo feed in feeds)
            {
                if (feed != null && feed.IsSubscribed)
                {
                    if (!update_queue.Contains (feed))
                    {
                        update_queue.Add (feed);
                        ++totalFeeds;
                    }
                }
            }

            if (updateStatus)
            {
                UpdateUserEvent ();
            }
        }

        public void UpdateThread ()
        {
            try
            {
                PodcastFeedInfo tmp_feed = null;

                while (true)
                {
                    lock (update_sync)
                    {
                        if (currentFeed == totalFeeds) {
                            return;
                        }
                        if (userEvent.IsCancelRequested) {
                            return;
                        } else {
                            tmp_feed = update_queue [currentFeed++] as PodcastFeedInfo;
                        }
                    }

                    try
                    {
                        UpdateUserEvent ( String.Format (
                                              Catalog.GetString ("Updating \"{0}\""), tmp_feed.Title
                                          ));

                        if (tmp_feed.IsSubscribed)
                        {
                            tmp_feed.Update ();
                        }
                    }
                    catch {
                    } finally {
                        userEvent.Progress = (double) (currentFeed) / (double) totalFeeds;
                    }
                }
            }
            catch {
            } finally
            {
                EndUpdate ();
            }
        }

        private void EndUpdate ()
        {
            lock (update_sync)
            {
                DestroyUserEvent ();
                update_queue.Clear ();
                updating = false;
            }
        }

        private void CreateUserEvent()
        {
            lock (update_sync)
            {
                if (userEvent == null)
                {
                    userEvent = new ActiveUserEvent (Catalog.GetString("Podcast Feed Update"));

                    userEvent.Icon = PodcastPixbufs.PodcastIcon22;

                    userEvent.Header = Catalog.GetString ("Updating");
                    userEvent.Message = Catalog.GetString ("Preparing to update feeds");
                    userEvent.Progress = 0.0;

                    userEvent.CancelRequested += OnUserEventCancelRequestedHandler;
                }
            }
        }

        private void DestroyUserEvent()
        {
            lock (update_sync)
            {
                if (userEvent != null)
                {
                    if (!userEvent.IsCancelRequested)
                    {
                        Thread.Sleep (500);
                    }

                    userEvent.Dispose();
                    userEvent = null;
                }
            }
        }

        public void Cancel ()
        {
            lock (update_sync)
            {
                if (userEvent != null)
                {
                    userEvent.Cancel ();
                }
            }
        }

        private void UpdateUserEvent ()
        {
            UpdateUserEvent (null);
        }

        private void UpdateUserEvent (string message)
        {
            lock (update_sync)
            {
                if(userEvent != null)
                {
                    if (!userEvent.IsCancelRequested)
                    {
                        userEvent.Header = String.Format (
                                               Catalog.GetString ("Updating podcast feed {0} of {1}"),
                                               currentFeed, totalFeeds
                                           );

                        if (message != null)
                        {
                            userEvent.Message = message;
                        }
                    }
                }
            }
        }

        private void OnUserEventCancelRequestedHandler (object sender, EventArgs args)
        {
            lock (update_sync)
            {
                if(userEvent != null)
                {
                    userEvent.CanCancel = false;
                    userEvent.Progress = 0.0;
                    userEvent.Header = Catalog.GetString("Canceling updates");
                    userEvent.Message = Catalog.GetString("Waiting for update to terminate");
                }
            }
        }
    }
}
