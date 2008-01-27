/***************************************************************************
 *  PodcastLibrary.cs
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
using System.Collections.Generic;

using Mono.Gettext;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Plugins.Podcast
{
    public class PodcastLibrary : IDisposable
    {
        private Hashtable podcasts; // [string-(url) | PodcastInfo]

        private ArrayList podcast_feeds;
        private Hashtable podcast_feeds_keyed; // [string-(url) | PodcastFeedInfo]

        // TODO:  Locking this collection is redundant, change it
        private Dictionary<TrackInfo, PodcastInfo> tracks_keyed; // [TrackInfo | PodcastInfo]

        private readonly object podcast_feed_sync = new object ();

        public object PodcastSync {
            get
            {
                return podcasts.SyncRoot;
            }
        }

        public object PodcastFeedSync {
            get
            {
                return podcast_feed_sync;
            }
        }

		public object TrackSync {
		    get 
		    {
		        return ((IDictionary)tracks_keyed).SyncRoot;
		    }
		}

        public event TrackEventHandler TrackAdded;
        public event TrackEventHandler TrackRemoved;

        public event PodcastEventHandler PodcastAdded;
        public event PodcastEventHandler PodcastRemoved;

        public event PodcastFeedEventHandler PodcastFeedAdded;
        public event PodcastFeedEventHandler PodcastFeedRemoved;

        public int FeedCount
        {
            get
                { lock (podcast_feed_sync)
                { return podcast_feeds.Count; } }
        }

        public int PodcastCount
        {
            get
                { lock (podcasts.SyncRoot)
                { return podcasts.Count; } }
        }

        public int TrackCount
        {
            get
            {
                lock (TrackSync)
                {
                    return tracks_keyed.Keys.Count;
                }
            }
        }

        public ICollection Feeds
        {
            get
            {
                return (ICollection) podcast_feeds;
            }
        }

        public ICollection Podcasts
        {
            get
            {
                return (ICollection) podcasts.Values;
            }
        }

        public IEnumerable<TrackInfo> Tracks {
            get
            {
                lock (((IDictionary)tracks_keyed).SyncRoot)
                {
                    return tracks_keyed.Keys;
                }
            }
        }

        public PodcastLibrary ()
        {
            podcast_feeds = new ArrayList ();

            podcasts = new Hashtable ();
            tracks_keyed = new Dictionary<TrackInfo, PodcastInfo> ();

            podcast_feeds_keyed = new Hashtable ();

            Globals.Library.TrackRemoved += OnLibraryTrackRemoved;
        }

        public void Dispose ()
        {
            Globals.Library.TrackRemoved -= OnLibraryTrackRemoved;
        }

        public bool ContainsPodcastKey (string key)
        {
            lock (podcasts.SyncRoot)
            {
                return podcasts.ContainsKey (key);
            }
        }

        public bool ContainsPodcastFeedKey (string key)
        {
            lock (podcast_feed_sync)
            {
                return podcast_feeds_keyed.ContainsKey (key);
            }
        }

        public bool AddPodcastFeed (PodcastFeedInfo feed)
        {
            return AddPodcastFeed (feed, true);
        }

        public int AddPodcastFeeds (ICollection feeds)
        {
            return AddPodcastFeeds (feeds, true);
        }

        public bool AddPodcastFeed (PodcastFeedInfo feed, bool update)
        {
            if (feed == null)
            {
                throw new ArgumentNullException ("feed");
            }

            bool new_feed = false;

            if (feed != null)
            {
                lock (PodcastFeedSync)
                {
                    new_feed = AddPodcastFeedToLibrary (feed, true);
                }

                if (update && new_feed)
                {
                    EmitPodcastFeedAdded (new PodcastFeedEventArgs (feed));
                }
            }

            return new_feed;
        }

        public int AddPodcastFeeds (ICollection feeds, bool update)
        {
            if (feeds == null)
            {
                throw new ArgumentNullException ("feeds");
            }

            ArrayList new_feeds = new ArrayList ();

            if (feeds != null)
            {
                lock (podcast_feed_sync)
                {
                    foreach (PodcastFeedInfo feed in feeds)
                    {
                        if (AddPodcastFeedToLibrary (feed, false))
                        {
                            new_feeds.Add (feed);
                        }
                    }

                    if (new_feeds.Count > 0)
                    {
                        podcast_feeds.Sort ();
                    }
                }

                if (update && (new_feeds.Count > 0))
                {
                    PodcastFeedEventArgs args;

                    if (new_feeds.Count == 1)
                    {
                        args = new PodcastFeedEventArgs (new_feeds[0] as PodcastFeedInfo);
                    }
                    else
                    {
                        args = new PodcastFeedEventArgs (new_feeds.ToArray (
                                                             typeof(PodcastFeedInfo)) as PodcastFeedInfo[]
                                                        );
                    }

                    EmitPodcastFeedAdded (args);
                }
            }

            return new_feeds.Count;
        }

        private bool AddPodcastFeedToLibrary (PodcastFeedInfo feed, bool sort)
        {
            bool ret = false;

            if (feed == null)
            {
                return ret;
            }

            lock (podcast_feed_sync)
            {
                if (podcast_feeds_keyed.ContainsKey (feed.Key))
                {
                    ret = false;
                }
                else
                {
                    podcast_feeds.Add (feed);
                    podcast_feeds_keyed.Add (feed.Key, feed);

                    if (feed.ID == 0)
                    {
                        feed.Commit ();
                    }

                    feed.TitleUpdated += OnFeedTitleUpdated;
                    feed.UrlUpdated += OnFeedUrlUpdated;

                    if (sort)
                    {
                        podcast_feeds.Sort ();
                    }

                    ret = true;
                }
            }

            return ret;
        }
        //-------------------------------------------------
        public bool RemovePodcastFeed (PodcastFeedInfo feed)
        {
            return RemovePodcastFeed (feed, true);
        }

        public int RemovePodcastFeeds (ICollection feeds)
        {
            return RemovePodcastFeeds (feeds, true);
        }

        public bool RemovePodcastFeed (PodcastFeedInfo feed, bool update)
        {
            if (feed == null)
            {
                throw new ArgumentNullException ("feeds");
            }

            bool removed = false;

            lock (PodcastFeedSync)
            {
                removed = RemovePodcastFeedFromLibrary (feed);
            }

            if (update && removed)
            {
                PodcastFeedEventArgs args = new PodcastFeedEventArgs (feed);

                EmitPodcastFeedRemoved (args);
            }

            return false;
        }

        public int RemovePodcastFeeds (ICollection feeds, bool update)
        {
            if (feeds == null)
            {
                throw new ArgumentNullException ("feeds");
            }

            ArrayList removed_feeds = new ArrayList ();

            lock (PodcastFeedSync)
            {
                foreach (PodcastFeedInfo feed in feeds)
                {
                    if (feed != null)
                    {
                        if (RemovePodcastFeedFromLibrary (feed))
                        {
                            removed_feeds.Add (feed);
                        }
                    }
                }

                if (update && (removed_feeds.Count > 0))
                {
                    PodcastFeedEventArgs args;

                    if (removed_feeds.Count == 1)
                    {
                        args = new PodcastFeedEventArgs (removed_feeds[0] as PodcastFeedInfo);
                    }
                    else
                    {
                        args = new PodcastFeedEventArgs (removed_feeds);
                    }

                    EmitPodcastFeedRemoved (args);
                }
            }

            return removed_feeds.Count;
        }

        private bool RemovePodcastFeedFromLibrary (PodcastFeedInfo feed)
        {
            if (feed == null)
            {
                throw new ArgumentNullException ("feed");
            }

            lock (feed.SyncRoot)
            {
                if (feed.IsBusy)
                {
                    return false;
                }
                else
                {
                    feed.Deactivate ();
                }
            }

            if (feed.PodcastCount > 0)
            {
                RemovePodcasts (feed.Podcasts);
            }

            feed.Delete ();

            podcast_feeds.Remove (feed);
            podcast_feeds_keyed.Remove (feed.Key);

            return true;
        }
        //---------------------------------------------------------

        public bool AddPodcast (PodcastInfo pi)
        {
            return AddPodcast (pi, true);
        }

        public ICollection AddPodcasts (ICollection podcasts)
        {
            return AddPodcasts (podcasts, true);
        }

        public bool AddPodcast (PodcastInfo pi, bool update)
        {
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            bool new_podcast = false;

            if (pi != null)
            {
                lock (PodcastSync)
                {
                    new_podcast = AddPodcastToLibrary (pi);
                }

                if (update && new_podcast)
                {
                    PodcastEventArgs args = new PodcastEventArgs (pi);

                    EmitPodcastAdded (args);
                }
            }

            return false;
        }

        public ICollection AddPodcasts (ICollection podcasts, bool update)
        {
            if (podcasts == null)
            {
                throw new ArgumentNullException ("podcasts");
            }

            ArrayList newUniquePodcasts = new ArrayList ();

            if (podcasts != null)
            {
                lock (PodcastSync)
                {
                    foreach (PodcastInfo pi in podcasts)
                    {
                        if (AddPodcastToLibrary (pi))
                        {
                            newUniquePodcasts.Add (pi);
                        }
                    }
                }

                if (update && (newUniquePodcasts.Count > 0))
                {
                    PodcastEventArgs args;

                    if (newUniquePodcasts.Count == 1)
                    {
                        args = new PodcastEventArgs (newUniquePodcasts[0] as PodcastInfo);
                    }
                    else
                    {
                        args = new PodcastEventArgs (newUniquePodcasts);
                    }

                    EmitPodcastAdded (args);
                }
            }

            return (ICollection) newUniquePodcasts;
        }

        private bool AddPodcastToLibrary (PodcastInfo pi)
        {
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            bool ret = false;

            if (pi == null)
            {
                return ret;
            }

            if (!podcasts.ContainsKey (pi.Key) && pi.IsActive)
            {
                //bool commit = false;

                podcasts.Add (pi.Key, pi);

                /*if (pi.ID == 0) {
                commit = true;
                }*/

                if (pi.Track != null)
                {
                    AddTrack (pi.Track, pi, true);
                } /*else if (pi.Track == null && pi.IsDownloaded) {
                     pi.IsDownloaded = false;
                     commit = true;
                    }*/


                // TODO move this into PodcastInfo
                if (pi.ID == 0)
                {
                    pi.ID = PodcastDBManager.Commit (pi);
                }

                ret = true;
            }

            return ret;
        }

        public bool RemovePodcast (PodcastInfo pi)
        {
            return RemovePodcast (pi, true);
        }

        public int RemovePodcasts (ICollection podcasts)
        {
            return RemovePodcasts (podcasts, true);
        }

        public bool RemovePodcast (PodcastInfo pi, bool update)
        {
            if (podcasts == null)
            {
                throw new ArgumentNullException ("podcasts");
            }

            bool removed = false;

            if (pi != null)
            {
                lock (PodcastSync)
                {
                    removed = RemovePodcastFromLibrary (pi, true);
                }

                if (update && removed)
                {
                    PodcastEventArgs args = new PodcastEventArgs (pi);

                    pi.Feed.UpdateCounts ();
                    EmitPodcastRemoved (args);
                }
            }

            return false;
        }

        public int RemovePodcasts (ICollection podcasts, bool update)
        {
            if (podcasts == null)
            {
                throw new ArgumentNullException ("podcasts");
            }

            ArrayList removed_podcasts = new ArrayList ();

            lock (PodcastSync)
            {
                foreach (PodcastInfo pi in podcasts)
                {
                    if (pi != null)
                    {
                        try
                        {
                            if (RemovePodcastFromLibrary (pi, false))
                            {
                                removed_podcasts.Add (pi);
                            }
                        } catch {
                            continue;
                        }
                    }
                }

                if (update && (removed_podcasts.Count > 0))
                {
                    PodcastEventArgs args;

                    if (removed_podcasts.Count == 1) {
                        args = new PodcastEventArgs (removed_podcasts[0] as PodcastInfo);
                    } else {
                        args = new PodcastEventArgs (removed_podcasts);
                    }

                    try {
                        UpdateParentFeeds (removed_podcasts);
                        EmitPodcastRemoved (args);
                    } finally {
                        PodcastDBManager.Deactivate (removed_podcasts);
                    }
                }
            }

            return removed_podcasts.Count;
        }

        private bool RemovePodcastFromLibrary (PodcastInfo pi, bool commit_podcast)
        {
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            if (pi.IsQueued)
            {
                return false;
            }

            podcasts.Remove (pi.Key);

            if (pi.Track != null)
            {
                ThreadAssist.ProxyToMain (delegate {
                                              RemoveTrack (pi.Track, true);
                                          });
            }

            if (!pi.IsActive)
            {
                return true;
            }
            else
            {
                pi.IsActive = false;
            }

            if (commit_podcast)
            {
                PodcastDBManager.Deactivate (pi);
            }

            return true;
        }

        public bool AddTrack (TrackInfo ti, PodcastInfo pi, bool raiseUpdate)
        {
            bool ret = false;

            if (ti == null)
            {
                throw new ArgumentNullException ("ti");
            }
            else if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            if (ti.Album == null || ti.Album == String.Empty)
            {
                ti.Album = pi.Feed.Title;
            }
            else if (ti.Artist == null || ti.Artist == String.Empty)
            {
                ti.Artist = pi.Feed.Title;
            }
            else if (ti.Title == null || ti.Title == String.Empty)
            {
                ti.Title = pi.Title;
            }

            lock (TrackSync)
            {
                if (!tracks_keyed.ContainsKey (ti))
                {
                    tracks_keyed.Add (ti, pi);
                    ret = true;
                }
            }

            if (ret && raiseUpdate)
            {
                TrackEventArgs args = new TrackEventArgs ();
                args.Track = ti;

                EmitTrackAdded (args);
            }

            return ret;
        }

        public bool RemoveTrack (TrackInfo ti, bool raiseUpdate)
        {
            if (ti == null)
            {
                throw new ArgumentNullException ("ti");
            }

            bool ret = false;

            if (BadTrackHash (ti))
            {
                return ret;
            }
            
            lock (TrackSync)
            {
                if (tracks_keyed.ContainsKey (ti)) {
                    try
                    {
                        tracks_keyed.Remove (ti);
                        ret = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine (e.Message);
                        return ret;
                    }
                }
            }

            if (raiseUpdate)
            {
                TrackEventArgs args = new TrackEventArgs ();
                args.Track = ti;

                EmitTrackRemoved (args);
            }

            return ret;
        }

        public PodcastFeedInfo AddPodcastFeedFromUri (string feedUri)
        {
            Uri tmp_uri = new Uri (feedUri);
            return AddPodcastFeedFromUri (tmp_uri);
        }

        /*public PodcastFeedInfo AddPodcastFeedFromUri (Uri feedUri, SyncPreference syncPref)
        {
         
        }*/

        public PodcastFeedInfo AddPodcastFeedFromUri (Uri feedUri)
        {
            if (feedUri == null)
            {
                throw new ArgumentNullException ("feedUri");
            }

            try
            {
                PodcastFeedInfo feed = null;
                string key = feedUri.ToString ();

                lock (podcast_feed_sync)
                {
                    if (podcast_feeds_keyed.ContainsKey (key))
                    {
                        // Should users be notified of this?
                        Console.WriteLine (Catalog.GetString("Already Subscribed"));
                        feed = podcast_feeds_keyed [key] as PodcastFeedInfo;
                        feed.IsSubscribed = true;
                    }
                    else
                    {
                        feed = new PodcastFeedInfo (key);
                        AddPodcastFeed (feed);
                    }
                }

                return feed;
            }
            catch {
                return null;
            }
        }

        public void Clear ()
        {
            lock (podcasts.SyncRoot)
            {
                podcasts.Clear ();
            }

            lock (podcast_feed_sync)
            {
                podcast_feeds.Clear ();
                podcast_feeds_keyed.Clear ();
            }

            ThreadAssist.ProxyToMain (delegate {
                                          lock (TrackSync)
                                          {
                                              tracks_keyed.Clear ();
                                          }
                                      });
        }

        private void OnLibraryTrackRemoved (object sender, LibraryTrackRemovedArgs args)
        {
            ArrayList podcast_remove_list = new ArrayList ();

            lock (TrackSync)
            {
                PodcastInfo tmpPi;

                foreach (TrackInfo ti in args.Tracks)
                {
                    if (ti != null)
                    {
                        tmpPi = null;                            
                        
                        if (BadTrackHash (ti))
                        {
                            continue;
                        }
                                                        
                        if (tracks_keyed.ContainsKey (ti)) {
                            tmpPi = tracks_keyed [ti] as PodcastInfo;
                        }
                        
                        if (tmpPi != null)
                        {
                            podcast_remove_list.Add (tmpPi);
                        }
                    }
                }
            }

            if (podcast_remove_list.Count > 0)
            {
                RemovePodcasts (podcast_remove_list);
                UpdateParentFeeds (podcast_remove_list);
            }
        }

        private bool BadTrackHash (TrackInfo ti)
        {
            bool ret = false;

            if (ti.Album == null || ti.Album == String.Empty)
            {
                ret = true;
            }
            else if (ti.Artist == null || ti.Artist == String.Empty)
            {
                ret = true;
            }
            else if (ti.Title == null || ti.Title == String.Empty)
            {
                ret = true;
            }

            return ret;
        }

        private void UpdateParentFeeds (ICollection podcasts)
        {
            if (podcasts == null)
            {
                throw new ArgumentNullException ("podcasts");
            }

            ArrayList parent_feeds = new ArrayList ();

            foreach (PodcastInfo pi in podcasts)
            {
                if (!parent_feeds.Contains (pi.Feed))
                {
                    parent_feeds.Add (pi.Feed);
                }
            }

            foreach (PodcastFeedInfo feed in parent_feeds)
            {
                feed.UpdateCounts ();
            }
        }

        private void OnFeedUrlUpdated (object sender, FeedUrlUpdatedEventArgs args)
        {
            PodcastFeedInfo feed = sender as PodcastFeedInfo;

            lock (podcast_feed_sync)
            {
                podcast_feeds_keyed.Remove (args.OldUrl);
                podcast_feeds_keyed.Add (args.NewUrl, feed);
            }
        }

        private void OnFeedTitleUpdated (object sender, FeedTitleUpdatedEventArgs args)
        {
            lock (podcast_feed_sync)
            {
                podcast_feeds.Sort ();
            }
        }

        private void EmitPodcastAdded (PodcastEventArgs args)
        {
            PodcastEventHandler handler = PodcastAdded;
            EmitPodcastEvent (handler, args);
        }

        private void EmitPodcastRemoved (PodcastEventArgs args)
        {
            PodcastEventHandler handler = PodcastRemoved;
            EmitPodcastEvent (handler, args);
        }

        private void EmitPodcastEvent (PodcastEventHandler handler, PodcastEventArgs args)
        {
            if (handler != null)
            {
                handler (this, args);
            }
        }

        private void EmitPodcastFeedAdded (PodcastFeedEventArgs args)
        {
            EmitPodcastFeedEvent (PodcastFeedAdded, args);
        }

        private void EmitPodcastFeedRemoved (PodcastFeedEventArgs args)
        {
            EmitPodcastFeedEvent (PodcastFeedRemoved, args);
        }

        private void EmitPodcastFeedEvent (PodcastFeedEventHandler handler,
                                           PodcastFeedEventArgs args)
        {
            if (handler != null)
            {
                handler (this, args);
            }
        }

        private void EmitTrackAdded (TrackEventArgs args)
        {
            TrackEventHandler handler = TrackAdded;

            if (handler != null)
            {
                handler (this, args);
            }
        }

        private void EmitTrackRemoved (TrackEventArgs args)
        {
            TrackEventHandler handler = TrackRemoved;

            if (handler != null)
            {
                handler (this, args);
            }
        }
    }
}
