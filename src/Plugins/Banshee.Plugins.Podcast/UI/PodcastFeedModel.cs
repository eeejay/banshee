/***************************************************************************
 *  PodcastFeedModel.cs
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

using Gtk;

using Banshee.Base;

namespace Banshee.Plugins.Podcast.UI
{
    internal class PodcastFeedModel : ListStore
    {
        private ArrayList add_queue;
        private ArrayList remove_queue;

        public PodcastFeedEventHandler FeedAdded;

        public PodcastFeedModel () : base (typeof(PodcastFeedInfo))
        {
            add_queue = new ArrayList ();
            remove_queue = new ArrayList ();
        }

        public void ClearModel()
        {
            Clear ();
            AppendValues (PodcastFeedInfo.All);
        }

        public PodcastFeedInfo PathPodcastFeedInfo (TreePath path)
        {
            TreeIter iter;

            if(!GetIter (out iter, path))
            {
                return null;
            }

            return IterPodcastFeedInfo (iter);
        }

        public PodcastFeedInfo IterPodcastFeedInfo (TreeIter iter)
        {
            return GetValue(iter, 0) as PodcastFeedInfo;
        }

        public void QueueAdd (PodcastFeedInfo feed)
        {
            lock (add_queue.SyncRoot)
            {
                add_queue.Add (feed);
            }

            GLib.Idle.Add (PumpAddQueue);
        }

        public void QueueAdd (ICollection feeds)
        {
            lock (add_queue.SyncRoot)
            {
                foreach (PodcastFeedInfo feed in feeds)
                {
                    add_queue.Add (feed);
                }
            }

            GLib.Idle.Add (PumpAddQueue);
        }

        public void QueueRemove (PodcastFeedInfo feed)
        {
            lock (remove_queue.SyncRoot)
            {
                remove_queue.Add (feed);
            }

            GLib.Idle.Add (PumpRemoveQueue);
        }

        public void QueueRemove (ICollection feeds)
        {
            lock (remove_queue.SyncRoot)
            {
                foreach (PodcastFeedInfo feed in feeds)
                {
                    remove_queue.Add (feed);
                }
            }

            GLib.Idle.Add (PumpRemoveQueue);
        }

        private void AddPodcastFeed (PodcastFeedInfo feed)
        {
            AddPodcastFeed (feed, true);
        }

        private void AddPodcastFeeds (ICollection feeds)
        {
            if (feeds == null)
            {
                return;
            }

            foreach (PodcastFeedInfo feed in feeds)
            {
                AddPodcastFeed (feed, false);
            }
        }

        private void AddPodcastFeed (PodcastFeedInfo feed, bool emitFeedAdded)
        {
            if (feed == null)
            {
                return;
            }

            feed.TitleUpdated += OnFeedTitleUpdated;
            feed.TreeIter = AppendValues (feed);

            if (emitFeedAdded)
            {
                EmitPodcastFeedAdded (feed);
            }
        }

        private void RemovePodcastFeed (PodcastFeedInfo feed)
        {
            if (feed != null)
            {
                TreeIter iter = feed.TreeIter;

                feed.TitleUpdated -= OnFeedTitleUpdated;

                if (IterIsValid (iter))
                {
                    Remove (ref iter);
                }
            }
        }

        private void RemovePodcastFeeds (ICollection feeds)
        {
            if (feeds == null)
            {
                return;
            }

            foreach (PodcastFeedInfo feed in feeds)
            {
                RemovePodcastFeed (feed);
            }
        }

        private delegate void SingleFeedAction (PodcastFeedInfo feed);
        private delegate void MultipleFeedAction (ICollection feeds);

        private bool PumpAddQueue ()
        {
            return PumpQueue (add_queue, AddPodcastFeed, AddPodcastFeeds);
        }

        private bool PumpRemoveQueue ()
        {
            return PumpQueue (remove_queue, RemovePodcastFeed, RemovePodcastFeeds);
        }

        private bool PumpQueue (ArrayList queue,
                                SingleFeedAction sfa, MultipleFeedAction mfa)
        {
            PodcastFeedInfo feed = null;
            ICollection feeds = null;

            int range_upper = -1;

            lock (queue.SyncRoot)
            {

                if (queue.Count == 0)
                {
                    return false;
                }

                int queue_count = queue.Count;
                range_upper = (queue_count >= 32) ? 31 : queue_count;

                if (queue_count == 1)
                {
                    feed = queue [0] as PodcastFeedInfo;
                }
                else if (queue_count > 1)
                {
                    feeds = queue.GetRange (0, range_upper);
                }
            }

            if (feed != null)
            {
                sfa (feed);
            }
            else if (feeds != null)
            {
                mfa (feeds);
            }

            lock (queue.SyncRoot)
            {
                queue.RemoveRange (0, range_upper);

                if (queue.Count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private void EmitPodcastFeedAdded (PodcastFeedInfo feed)
        {
            if (feed != null)
            {
                PodcastFeedEventHandler handler = FeedAdded;

                if (handler != null)
                {
                    handler (this, new PodcastFeedEventArgs (feed));
                }
            }
        }

        private void OnFeedTitleUpdated (object sender, FeedTitleUpdatedEventArgs args)
        {
            ThreadAssist.ProxyToMain ( delegate {
                                           Reorder ();
                                       });
        }
    }
}
