/***************************************************************************
 *  PodcastFeedInfo.cs
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
using System.Xml;
using System.Threading;
using System.Collections;

using Mono.Gettext;

using Gtk;

using Banshee.Plugins;

namespace Banshee.Plugins.Podcast
{
    public class FeedNotModifiedException : Exception {}
    
    public delegate void FeedPodcastsUpdatedEventHandler (object sender, FeedPodcastsUpdatedEventArgs args);

    public delegate void FeedUrlUpdatedEventHandler (object sender, FeedUrlUpdatedEventArgs args);
    public delegate void FeedTitleUpdatedEventHandler (object sender, FeedTitleUpdatedEventArgs args);

    public class FeedPodcastsUpdatedEventArgs : EventArgs
    {
        private readonly PodcastInfo[] new_podcasts;
        private readonly PodcastInfo[] zombie_podcasts;

        public PodcastInfo[] NewPodcasts { get
                                           { return new_podcasts; } }
        public PodcastInfo[] RemovedPodcasts { get
                                               { return zombie_podcasts; } }

        public FeedPodcastsUpdatedEventArgs (PodcastInfo[] newPodcasts,
                                             PodcastInfo[] removedPodcasts)
        {
            new_podcasts = newPodcasts;
            zombie_podcasts = removedPodcasts;
        }
    }

    public class FeedTitleUpdatedEventArgs : EventArgs
    {
        private readonly string old_title;
        private readonly string new_title;

        public string OldTitle { get
                                 { return old_title; } }
        public string NewTitle { get
                                 { return new_title; } }

        public FeedTitleUpdatedEventArgs (string oldTitle, string newTitle)
        {
            old_title = oldTitle;
            new_title = newTitle;
        }
    }

    public class FeedUrlUpdatedEventArgs : EventArgs
    {
        private readonly string old_url;
        private readonly string new_url;

        public string OldUrl { get
                               { return old_url; } }
        public string NewUrl { get
                               { return new_url; } }

        public FeedUrlUpdatedEventArgs (string oldUrl, string newUrl)
        {
            old_url = oldUrl;
            new_url = newUrl;
        }
    }

    public class PodcastFeedInfo : IComparable
    {
        private int feed_id;
        private Uri feed_url;

        private string title;

        // TODO Link & Image should be of type System.Uri.
        private string link;
        private string image;
        private string description;

        private bool subscribed;
        private DateTime lastUpdate;
        private SyncPreference sync_preference;

        private TreeIter tree_iter;

        private bool updating;

        private ArrayList podcasts;

        private int new_podcast_count = 0;
        private int total_podcasts = 0;
        private int downloaded_podcasts = 0;

        private  int active_downloads = 0;
        private  int queued_downloads = 0;

        private readonly object sync_root = new object ();
        private readonly object count_sync = new object ();
        private readonly object update_sync = new object ();

        public static readonly PodcastFeedInfo All = new PodcastFeedInfo ();

        public event UpdateActivityEventHandler UpdateStarted;
        public event UpdateActivityEventHandler UpdateFinished;

        public event FeedUrlUpdatedEventHandler UrlUpdated;
        public event FeedTitleUpdatedEventHandler TitleUpdated;
        public event FeedPodcastsUpdatedEventHandler PodcastsUpdated;

        public delegate void UpdateActivityEventHandler (object sender,
                UpdateActivityEventArgs args);

        public int CompareTo (object o)
        {
            PodcastFeedInfo rhs = o as PodcastFeedInfo;

            if (rhs == null)
            {
                return 1;
            }

            return String.Compare (this.Title.ToLower (), rhs.Title.ToLower ());
        }

        // Should be a method
        public PodcastInfo[] Podcasts {
            get
            {
                lock (podcasts.SyncRoot)
                {
                    return podcasts.ToArray (typeof(PodcastInfo)) as PodcastInfo[];
                }
            }
        }

        public int PodcastCount {
            get
            {
                lock (podcasts.SyncRoot)
                {
                    return podcasts.Count;
                }
            }
        }

        private PodcastFeedInfo () {}

        public PodcastFeedInfo (int id, string feedUrl, bool subscribed, SyncPreference sync)
        {
            feed_id = id;
            feed_url = new Uri (feedUrl);
            this.subscribed = subscribed;
            sync_preference = sync;

            podcasts = new ArrayList ();
        }

        public PodcastFeedInfo (int id, string title, string feedUrl, string link,
                                string description, string image, DateTime lastUpdate, bool subscribed,
                                SyncPreference sync)
                : this (id, feedUrl, subscribed, sync)
        {
            this.title = title;
            this.description = description;
            this.link = link;
            this.image = image;
            this.lastUpdate = lastUpdate;
        }

        public PodcastFeedInfo (string url) : this (0, url, true, SyncPreference.One) {}
        public PodcastFeedInfo (string url, SyncPreference sync)
                : this (0, url, true, sync) {}

        public void Add (PodcastInfo pi)
        {
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            AddPodcast (pi, true);
        }

        public void Add (ICollection pis)
        {
            if (pis == null)
            {
                throw new ArgumentNullException ("pis");
            }

            lock (podcasts.SyncRoot)
            {
                foreach (PodcastInfo pi in pis)
                {
                    AddPodcast (pi, false);
                }

                podcasts.Sort ();
                UpdateCounts ();
            }
        }

        private void AddPodcast (PodcastInfo pi, bool update)
        {
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            lock (podcasts.SyncRoot)
            {
                podcasts.Add (pi);

                if (update)
                {
                    podcasts.Sort ();
                    UpdateCounts ();
                }
            }
        }

        private void Remove (PodcastInfo pi)
        {
            lock (podcasts.SyncRoot)
            {
                RemovePodcast (pi, true);
            }
        }

        private void Remove (ICollection podcasts)
        {
            if (podcasts == null)
            {
                throw new ArgumentNullException ("podcasts");
            }

            lock (podcasts.SyncRoot)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    try
                    {
                        RemovePodcast (pi, false);
                    }
                    catch {
                        continue;
                    }
                }

            PodcastDBManager.Delete (podcasts)
                ;
                UpdateCounts ();
            }
        }

        private void RemovePodcast (PodcastInfo pi, bool update)
        {
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            lock (podcasts.SyncRoot)
            {
                podcasts.Remove (pi);

                if (update)
                {
                    PodcastDBManager.Delete (pi);
                    UpdateCounts ();
                }
            }
        }

        public void Deactivate ()
        {
            lock (podcasts.SyncRoot)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    pi.IsActive = false;
                }
            }
        }

        internal void Select ()
        {
            lock (count_sync)
            {
                new_podcast_count = 0;
            }
        }

        internal int IncrementActiveDownloads ()
        {
            lock (count_sync)
            { return ++active_downloads; }
        }

        internal int DecrementActiveDownloads ()
        {
            lock (count_sync)
            { return --active_downloads; }
        }

        internal int IncrementQueuedDownloads ()
        {
            lock (count_sync)
            { return ++queued_downloads; }
        }

        internal int DecrementQueuedDownloads ()
        {
            lock (count_sync)
            { return --queued_downloads; }
        }

        internal void UpdateCounts ()
        {
            int active = 0;
            int downloaded = 0;

            Select ();

            lock (podcasts.SyncRoot)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    if (pi.IsActive)
                    {
                        ++active;

                        if (pi.IsDownloaded)
                        {
                            ++downloaded;
                        }
                    }

                    total_podcasts = active;
                    downloaded_podcasts = downloaded;
                }
            }
        }

        public void Update ()
        {
            try
            {
                lock (update_sync)
                {
                    if (!updating)
                    {
                        updating = true;
                        FeedUpdateStarted ();
                    }
                    else
                    {
                        return;
                    }
                }

                DoUpdate ();

            } catch {
            } finally {
                IsUpdating = false;
                FeedUpdateFinished ();
            }
        }

        private void DoUpdate ()
        {
            PodcastFeedParser feed_parser;

            try
            {
                XmlDocument doc;

                doc = FeedUtil.FetchXmlDocument (feed_url.ToString ().Trim(), lastUpdate);

                if (doc == null)
                {
                    return;
                }

                feed_parser = PodcastFeedParser.Create (doc, this);
                feed_parser.Parse ();
                
            } catch (FeedNotModifiedException) {
                
                SyncPodcasts ();
                return;
                
            } catch {
                return;
            }

            Title = feed_parser.Title;
            link = feed_parser.Link;
            image = feed_parser.Image;
            description = feed_parser.Description;
            
            Commit ();

            PodcastInfo[] remote_podcasts = feed_parser.Podcasts;

            if (remote_podcasts != null)
            {
                UpdatePodcasts (remote_podcasts);
            }

            SyncPodcasts ();

            lastUpdate = DateTime.Now;
            Commit ();
        }

        private void UpdatePodcasts (PodcastInfo[] remotePodcasts)
        {
            PodcastInfo[] new_podcasts = null;
            PodcastInfo[] zombie_podcasts = null;

            bool updated = false;
            ICollection tmp_new = null;
            ICollection tmp_remove = null;

            lock (podcasts.SyncRoot)
            {
                if (podcasts.Count > 0)
                {
                    tmp_new = Diff (podcasts, remotePodcasts);
                    tmp_remove = Diff (remotePodcasts, podcasts);
                }
                else
                {
                    tmp_new = remotePodcasts;
                }
            }

            if (tmp_remove != null)
            {
                ArrayList double_killed_zombies = new ArrayList ();

                foreach (PodcastInfo zombie in tmp_remove)
                {
                    if ((!zombie.IsDownloaded || !zombie.IsActive) &&
                            !zombie.IsQueued)
                    {
                        zombie.IsActive = false;
                        double_killed_zombies.Add (zombie);
                    }
                }

                if (double_killed_zombies.Count > 0)
                {
                    zombie_podcasts = double_killed_zombies.ToArray (
                                          typeof(PodcastInfo)) as PodcastInfo[];
                }
            }

            if (tmp_new != null)
            {
                ICollection add_list = PodcastCore.Library.AddPodcasts (tmp_new);

                if (add_list.Count > 0)
                {
                    Add (add_list);
                    NewPodcasts = add_list.Count;
                    updated = true;
                }

                if (updated)
                {
                    new_podcasts = new PodcastInfo [add_list.Count];
                    add_list.CopyTo (new_podcasts, 0);
                }
            }

            if (zombie_podcasts != null)
            {
                Remove (zombie_podcasts);
                PodcastCore.Library.RemovePodcasts (zombie_podcasts);
                updated = true;
            }

            if (updated)
            {
                EmitFeedUpdated (new_podcasts, zombie_podcasts);
            }
        }

        private ICollection Diff (ICollection baseSet, ICollection overlay)
        {
            bool found;
            ArrayList diff = new ArrayList ();

            // This could do with a re-working
            foreach (PodcastInfo opi in overlay)
            {
                found = false;
                foreach (PodcastInfo bpi in baseSet)
                {
                    if (opi.Url.ToString () == bpi.Url.ToString ())
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    diff.Add (opi);
                }
            }

            return (diff.Count == 0) ? null : (ICollection) diff;
        }

        private void SyncPodcasts ()
        {
            if (sync_preference == SyncPreference.None)
            {
                return;
            }

            lock (podcasts.SyncRoot)
            {
                if (sync_preference == SyncPreference.One)
                {
                    if (podcasts.Count > 0)
                    {
                        PodcastInfo pi = podcasts [0] as PodcastInfo;
                        QueuePodcastDownload (pi);
                    }
                }
                else if (sync_preference == SyncPreference.All)
                {
                    PodcastCore.QueuePodcastDownload (podcasts);
                }
            }
        }

        private void QueuePodcastDownload (PodcastInfo pi)
        {
            if (pi.CanDownload)
            {
                PodcastCore.QueuePodcastDownload (pi);
            }
        }

        internal void Commit ()
        {
            int insertion = PodcastDBManager.Commit (this);

            if (feed_id == 0)
            {
                feed_id = insertion;
            }
        }

        internal void Delete ()
        {
            lock (podcasts.SyncRoot)
            {
                podcasts.Clear ();
            }

            PodcastDBManager.Delete (this);
        }

        private void EmitFeedUpdated (PodcastInfo[] newPodcasts, PodcastInfo[] removedPodcasts)
        {
            FeedPodcastsUpdatedEventHandler handler = PodcastsUpdated;

            if (handler != null)
            {
                handler (this, new FeedPodcastsUpdatedEventArgs (newPodcasts, removedPodcasts));
            }
        }

        private void EmitFeedTitleUpdated (string oldTitle, string newTitle)
        {
            FeedTitleUpdatedEventHandler handler = TitleUpdated;
            FeedTitleUpdatedEventArgs args = new FeedTitleUpdatedEventArgs (oldTitle, newTitle);

            if (handler != null)
            {
                handler (this, args);
            }
        }

        private void EmitFeedUrlUpdated (string oldUrl, string newUrl)
        {
            FeedUrlUpdatedEventHandler handler = UrlUpdated;
            FeedUrlUpdatedEventArgs args = new FeedUrlUpdatedEventArgs (oldUrl, newUrl);

            if (handler != null)
            {
                handler (this, args);
            }
        }

        private void FeedUpdateStarted ()
        {
            UpdateActivityEventHandler handler = UpdateStarted;
            EmitFeedUpdateStatusChanged (this, handler);
        }

        private void FeedUpdateFinished ()
        {
            UpdateActivityEventHandler handler = UpdateFinished;
            EmitFeedUpdateStatusChanged (this, handler);
        }

        private void EmitFeedUpdateStatusChanged (PodcastFeedInfo feed,
                UpdateActivityEventHandler handler)
        {
            if (handler != null)
            {
                handler (this, new UpdateActivityEventArgs (feed));
            }
        }

        public string Key {
            get
            {
                return feed_url.ToString ();
            }
        }

        public TreeIter TreeIter {
            get
            {
                return tree_iter;
            }

            internal set
            {
                tree_iter = value;
            }
        }

        public int TotalPodcasts {
            get
            {
                return total_podcasts;
            }
        }

        public int DownloadedPodcasts {
            get
            {
                return downloaded_podcasts;
            }
        }

        public int ID {
            get
            {
                return feed_id;
            }
        }

        public string Title {
            get
            {
                return (title == null || title == String.Empty) ?
                       feed_url.ToString () : title;
            }

            private set
            {
                if (title != value)
                {
                    string old_title = Title;
                    title = value;
                    Console.WriteLine (Catalog.GetString("Title Property Changed"));
                    EmitFeedTitleUpdated (old_title, title);
                }
            }
        }

        public string Link {
            get
            {
                return link;
            }
        }

        public string Description {
            get
            {
                return description;
            }
        }

        public Uri Url {
            get
            {
                return feed_url;
            }

            set
            {
                feed_url = value;
            }
        }

        public string Image {
            get
            {
                return image;
            }
        }

        public DateTime LastUpdated {
            get
            {
                return lastUpdate;
            }
        }

        public bool IsSubscribed {
            get
            {
                return subscribed;
            }

            set
            {
                subscribed = value;
            }
        }

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

        public int QueuedDownloads {

            get
            {
                return queued_downloads;
            }
        }

        public int ActiveDownloads {

            get
            {
                return active_downloads;
            }
        }

        public int NewPodcasts {

            get
            {
                return new_podcast_count;
            }

            private set
            {
                lock (count_sync)
                {
                    new_podcast_count = value;
                }
            }
        }

        public SyncPreference SyncPreference {
            get
            {
                return sync_preference;
            }

            set
            {
                sync_preference = value;
            }
        }

        public bool IsBusy {
            get
            {
                return (updating | (queued_downloads > 0));
            }
        }

        internal object SyncRoot {
            get
            {
                return sync_root;
            }
        }
    }
}
