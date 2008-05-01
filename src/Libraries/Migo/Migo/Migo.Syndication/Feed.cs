/*************************************************************************** 
 *  Feed.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
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
using System.Net;
using System.Threading;

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena;
using Hyena.Data.Sqlite;

using Migo.Net;
using Migo.TaskCore;
using Migo.DownloadCore;

namespace Migo.Syndication
{
    public enum FeedAutoDownload : int 
    {
        All = 0,
        One = 1,
        None = 2
    }

    // TODO remove this, way too redundant with DownloadStatus
    public enum PodcastFeedActivity : int {
        Updating = 0,
        UpdatePending = 1,        
        UpdateFailed = 2,
        ItemsDownloading = 4,        
        ItemsQueued = 5,        
        None = 6        
    }

    public class Feed
    {        
        private static SqliteModelProvider<Feed> provider;
        public static SqliteModelProvider<Feed> Provider {
            get { return provider; }
            set { provider = value; }
        }

        private static Feed all = new Feed ();
        public static Feed All {
            get { return all; }
        }

        private bool canceled;
        private bool deleted; 
        private bool updating;
        
        private AsyncWebClient wc;
        private ManualResetEvent updatingHandle = new ManualResetEvent (true);
        
        private readonly object sync = new object ();
       
        private long queuedDownloadCount;
        private long activeDownloadCount;        
        
        private List<FeedItem> items;
        private List<FeedItem> inactive_items;
        
        private FeedAutoDownload auto_download;
        private string copyright;
        private string description;
        private bool downloadEnclosuresAutomatically;
        private FeedDownloadStatus downloadStatus;
        private string downloadUrl;
        private string image;        
        private long interval;
        private bool isList;
        private string language;
        private DateTime lastBuildDate;
        private FeedDownloadError lastDownloadError;
        private DateTime lastDownloadTime;      
        private DateTime lastWriteTime;
        private string link;
        private string localEnclosurePath;
        private long localID = -1;
        private long maxItemCount = 200;
        private string name;
        private FeedsManager parent;        
        private DateTime pubDate;
        private FeedSyncSetting syncSetting;
        private string title;
        private long ttl;  
        private long unreadItemCount;  
        private string url;

        public event EventHandler<FeedEventArgs> FeedDeleted;
        public event EventHandler<FeedDownloadCompletedEventArgs> FeedDownloadCompleted;
        public event EventHandler<FeedDownloadCountChangedEventArgs> FeedDownloadCountChanged;                
        public event EventHandler<FeedEventArgs> FeedDownloading;
        public event EventHandler<FeedItemCountChangedEventArgs> FeedItemCountChanged;
        public event EventHandler<FeedEventArgs> FeedRenamed;
        public event EventHandler<FeedEventArgs> FeedUrlChanged;
        
        public event EventHandler<FeedItemEventArgs> FeedItemAdded;
        public event EventHandler<FeedItemEventArgs> FeedItemRemoved;
        
#region Database-bound Properties
        
        [DatabaseColumn]
        public string Copyright { 
            get { lock (sync) { return copyright; } } 
            set { copyright = value; }
        }
        
        [DatabaseColumn]
        public string Description { 
            get { lock (sync) { return description; } } 
            set { description = value; }
        }
        
        [DatabaseColumn]
        public bool DownloadEnclosuresAutomatically { 
            get { lock (sync) { return downloadEnclosuresAutomatically; } }  
            set { lock (sync) { downloadEnclosuresAutomatically = value; } }
        }
        
        [DatabaseColumn]
        public string DownloadUrl {
            get { lock (sync) { return downloadUrl; } }
            set { downloadUrl = value; }
        }     
        
        [DatabaseColumn]
        public string Image {
            get { lock (sync) { return image; } }
            set { image = value; }
        }

        [DatabaseColumn]
        public long Interval { 
            get { lock (sync) { return interval; } }
            set { 
                lock (sync) { interval = (value < 15) ? 1440 : value; }
            } 
        }
        
        [DatabaseColumn]
        public string Language { 
            get { lock (sync) { return language; } }
            set { language = value; }
        }
        
        [DatabaseColumn]
        public DateTime LastBuildDate { 
            get { lock (sync) { return lastBuildDate; } }
            set { lastBuildDate = value; }
        }
        
        [DatabaseColumn]
        public FeedDownloadError LastDownloadError { 
            get { lock (sync) { return lastDownloadError; } }
            set { lastDownloadError = value; }
        }
        
        [DatabaseColumn]
        public DateTime LastDownloadTime { 
            get { lock (sync) { return lastDownloadTime; } }
            set { lastDownloadTime = value; }
        }
        
        [DatabaseColumn]
        public DateTime LastWriteTime { 
            get { lock (sync) { return lastWriteTime; } }
            set { lastWriteTime = value; }
        }
        
        [DatabaseColumn]
        public string Link { 
            get { lock (sync) { return link; } }
            set { link = value; }
        }
        
        [DatabaseColumn]
        public string LocalEnclosurePath { 
            get { lock (sync) { return localEnclosurePath; } }
            set { 
                lock (sync) {
                    if (localEnclosurePath != value) {
                        localEnclosurePath = value;
                        Save ();                    	
                    }
                }
            }
        }
        
        [DatabaseColumn ("FeedID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public long DbId { 
            get { lock (sync) { return localID; } }
            set { lock (sync) { localID = value; } }
        }

        [DatabaseColumn]
        public long MaxItemCount {
            get { lock (sync) { return maxItemCount; } }
            set { lock (sync) { maxItemCount = value; } }
        }
            
        [DatabaseColumn]
        public string Name {
            get { lock (sync) { return name; } }
            
            private set {
                bool renamed = false;
                
                lock (sync) {
                    if (value == null) {
                       	throw new ArgumentNullException ("Name");
                    }   
                    
                    if (value != name) {
                    	name = value;
                        renamed = true;
                        Save ();
                    }
                }
                
                if (renamed) {
                    OnFeedRenamed ();                	
                }
            }            
        }
        
        [DatabaseColumn]
        public DateTime PubDate {
            get { lock (sync) { return pubDate; } }
            set { pubDate = value; }
        }
        
        [DatabaseColumn]
		public FeedSyncSetting SyncSetting {
            get { lock (sync) { return syncSetting; } } 
            set { syncSetting = value; }
        }

        [DatabaseColumn]
        public FeedAutoDownload AutoDownload {
            get { return auto_download; }
            set { auto_download = value; }
        }
        
        [DatabaseColumn]
        public string Title {
            get {
                lock (sync) { 
                    return String.IsNullOrEmpty (title) ? url : title; 
                }
            }
            set { title = value; }
        }
              
        [DatabaseColumn]
        public long Ttl {
            get { lock (sync) { return ttl; } }
            set { ttl = value; }
        }
        
        [DatabaseColumn]
        public string Url {
            get { lock (sync) { return url; } }
            set {
                if (String.IsNullOrEmpty (value)) {
                   	throw new ArgumentNullException ("Url");
                }   
                
                bool updated = false;
                string oldUrl = null;
                
                lock (sync) {                
                    if (value != url) {
                        lastDownloadTime = DateTime.MinValue;
                    	oldUrl = url;
                    	url = value;
                        updated = true;
                        Save ();
                    }
                }
                
                if (updated) {
                    parent.UpdateFeedUrl (oldUrl, this);                
                    OnFeedUrlChanged ();
                }
            }
        }
        
#endregion

#region Other Properties

        public long ActiveDownloadCount { 
            get { lock (sync) { return activeDownloadCount; } }
        }
        
        public long QueuedDownloadCount { 
            get { lock (sync) { return queuedDownloadCount; } }             
        }         

        // TODO remove this, way too redundant with DownloadStatus
        public PodcastFeedActivity Activity {
            get {
                PodcastFeedActivity ret = PodcastFeedActivity.None;
                
                if (this == All) {
                    return ret;
                }
                
                switch (DownloadStatus) {
                case FeedDownloadStatus.Pending: 
                    ret = PodcastFeedActivity.UpdatePending;
                    break;
                case FeedDownloadStatus.Downloading: 
                    ret = PodcastFeedActivity.Updating;
                    break;    
                case FeedDownloadStatus.DownloadFailed: 
                    ret = PodcastFeedActivity.UpdateFailed;
                    break;                         
                }
                
                if (ret != PodcastFeedActivity.Updating) {
                    if (ActiveDownloadCount > 0) {
                        ret = PodcastFeedActivity.ItemsDownloading;
                    } else if (QueuedDownloadCount > 0) {
                        ret = PodcastFeedActivity.ItemsQueued;
                    }
                }

                return ret;
            }
        }

        public FeedsManager Parent {
            get { lock (sync) { return parent; } }
        }
        
        public FeedDownloadStatus DownloadStatus { 
            get { lock (sync) { return downloadStatus; } }
        }
        
        public bool IsList {
            get { lock (sync) { return isList; } }
        }
        
        public long ItemCount {
            get { lock (sync) { return Items.Count; } }         
        }
        
        private bool items_loaded = false;
        
        private ReadOnlyCollection<FeedItem> ro_items;
        public ReadOnlyCollection<FeedItem> Items {
            get {
                lock (sync) {
                    if (!items_loaded) {
                        LoadItems ();
                    }
                    return ro_items ?? ro_items = new System.Collections.ObjectModel.ReadOnlyCollection<FeedItem> (items);                                
                }
            }
        }
        
        public long UnreadItemCount {
            get { lock (sync) { return unreadItemCount; } } 
            internal set {
                if (value < 0 /*|| value > maxItemCount*/ || value > ItemCount) {  
                    // max item count not yet implemented
                   	throw new ArgumentOutOfRangeException (
                        "UnreadItemCount:  Must be >= 0 and < MaxItemCount and <= ItemCount."
                    );
                }   
                
                if (value != unreadItemCount) {
                	unreadItemCount = value;
                }
            }
        }
        
#endregion

#region Constructors

        public Feed (string url) : this ()
        {
            Uri uri;
            if (String.IsNullOrEmpty (url)) {
                throw new ArgumentException ("url:  Cannot be null or empty.");
            } else if (/* !Uri.IsWellFormedUriString (url, UriKind.Absolute) -- this is not yet implemented in Mono */
                !Uri.TryCreate (url, UriKind.Absolute, out uri)) {
                throw new ArgumentException ("url:  Is not a well formed Url.");                
            } else if (uri.Scheme != Uri.UriSchemeHttp && 
                       uri.Scheme != Uri.UriSchemeHttps) {
                throw new ArgumentException ("url:  Scheme must be either http or https.");                
            }

            this.url = url;
        }

        public Feed ()
        {
            parent = FeedsManager.Instance;
            downloadStatus = FeedDownloadStatus.None;
            inactive_items = new List<FeedItem> ();
            interval = parent.DefaultInterval;
            items = new List<FeedItem> ();
        }
        
#endregion

#region Internal Methods

        internal long DecrementActiveDownloadCount ()
        {
            return DecrementActiveDownloadCount (1);              
        }
        
        internal long DecrementActiveDownloadCount (int cnt)
        {
            //Console.WriteLine ("pre-lock DecrementActiveDownloadCount");
            
            lock (sync) {     
                //Console.WriteLine ("lock DecrementQueuedDownloadCount");                            
                activeDownloadCount -= cnt;
                
                OnFeedDownloadCountChanged (
                    FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS.FEDCF_ACTIVE_DOWNLOAD_COUNT_CHANGED
                );
                
                return activeDownloadCount;                 
            }                 
        }        
        
        internal long DecrementQueuedDownloadCount ()
        {
            return DecrementQueuedDownloadCount (1);            
        }

        internal long DecrementQueuedDownloadCount (int cnt)
        {
            //Console.WriteLine ("pre-lock DecrementQueuedDownloadCount");
            
            lock (sync) {
                //Console.WriteLine ("lock DecrementQueuedDownloadCount");                
                queuedDownloadCount -= cnt;
                
                OnFeedDownloadCountChanged (
                    FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS.FEDCF_QUEUED_DOWNLOAD_COUNT_CHANGED
                );
                
                return queuedDownloadCount; 
            }             
        }

        internal long IncrementActiveDownloadCount ()
        {
            return IncrementActiveDownloadCount (1);
        }

        internal long IncrementActiveDownloadCount (int cnt)
        {
            //Console.WriteLine ("pre-lock IncrementActiveDownloadCount");

            lock (sync) {
                //Console.WriteLine ("lock IncrementActiveDownloadCount");
                activeDownloadCount += cnt;
                
                OnFeedDownloadCountChanged (
                    FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS.FEDCF_ACTIVE_DOWNLOAD_COUNT_CHANGED
                );
                
                return activeDownloadCount;                 
            }
        }

        internal long IncrementQueuedDownloadCount ()
        {
            return IncrementQueuedDownloadCount (1);
        }           

        internal long IncrementQueuedDownloadCount (int cnt)
        {
            //Console.WriteLine ("pre-lock IncrementQueuedDownloadCount");
                        
            lock (sync) {
                //Console.WriteLine ("lock IncrementQueuedDownloadCount");
                queuedDownloadCount += cnt;        
                
                OnFeedDownloadCountChanged (
                    FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS.FEDCF_QUEUED_DOWNLOAD_COUNT_CHANGED
                );
                
                return queuedDownloadCount;                  
            }            
        }
        
        // Should ***ONLY*** be called by 'FeedUpdateTask'
        internal bool AsyncDownloadImpl ()
        {              
            bool ret = false;            

            lock (sync) {
                if (!(updating || SetUpdating ())) {                
                    return ret;                   
                }             
            }      
            
            OnFeedDownloading (); 
            
			try {                                                                       
				wc = new AsyncWebClient ();                  
				wc.Timeout = (30 * 1000); // 30 Seconds  
				wc.IfModifiedSince = lastDownloadTime.ToUniversalTime ();
				
				downloadStatus = FeedDownloadStatus.Downloading;                    
				wc.DownloadStringCompleted += OnDownloadStringCompleted;
				wc.DownloadStringAsync (new Uri (url));
                
				ret = true;
			} catch {
                downloadStatus = FeedDownloadStatus.DownloadFailed;                                                            

                if (wc != null) {
                    wc.DownloadStringCompleted -= OnDownloadStringCompleted;
                    wc = null;
                }
                
                lock (sync) {
                    ResetUpdating ();
                    OnFeedDownloadCompleted (
                        FeedDownloadError.DownloadFailed
                    );                     
                }  
			}
    
            return ret;
        }
        
        internal void SetItems (IEnumerable<FeedItem> itms)
        {
            if (itms == null) {
            	throw new ArgumentNullException ("itms");
            }

            lock (sync) {
                ClearItemsImpl ();
                Add (itms);
            }
        }
        
        
        internal void CancelDownload (FeedEnclosure enc) 
        {
            parent.CancelDownload (enc);
        }        

        internal HttpFileDownloadTask QueueDownload (FeedEnclosure enc) 
        {
            return parent.QueueDownload (enc);
        }

        internal void StopDownload (FeedEnclosure enc)
        {
            parent.StopDownload (enc);
        }
        
        internal void Remove (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }
            
            lock (sync) {
                if (items.Remove (item)) {
                    inactive_items.Add (item);
                    OnFeedItemRemoved (item);
                    UpdateItemCountsImpl (-1, (!item.IsRead) ? -1 : 0);
                }
            }
        }
        
        internal void Remove (IEnumerable<FeedItem> itms)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }
            
            long totalDelta = 0;            
            long unreadDelta = 0;
            
            List<FeedItem> removedItems = new List<FeedItem> ();                    
                    
            lock (sync) {            
                foreach (FeedItem i in itms) {
                    if (i != null) {
                        if (items.Remove (i)) {
                            --totalDelta;
                            
                            if (!i.IsRead) {
                                --unreadDelta;
                            }                  
                            
                            removedItems.Add (i);
                            inactive_items.Add (i);
                        }   
                    }
                }
                
                if (removedItems.Count > 0) {
                    OnFeedItemsRemoved (removedItems);
                }                
                
                if (totalDelta != 0) {
                    UpdateItemCountsImpl (totalDelta, unreadDelta);
                }                
            }
        }
        
        internal void UpdateItemCounts (long totalDelta, long unreadDelta)
        {
            lock (sync) {
                UpdateItemCountsImpl (totalDelta, unreadDelta);                    
            }
        }
        
#endregion
        
        // TODO remove, unused?
/*        
        private void Add (FeedItem item)
        {
            Add (item, false);
        }

        private void Add (FeedItem item, bool commit)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }
            
            item.Parent = this;            
            
            if (commit) {
                item.Save ();
            }
                        
            if (item.LocalID == -1) {
                return;
            } else if (item.Active) {                                             
                items.Add (item);
                itemsByID.Add (item.LocalID, item);
                
                OnFeedItemAdded (item);
                UpdateItemCountsImpl (1, (!item.IsRead) ? 1 : 0);
            } else {
                inactiveItems.Add (item);                   
            }
        }
*/        

#region Private Methods

        private void LoadItems ()
        {
            if (DbId > 0 && !items_loaded) {
                Console.WriteLine ("Loading items");
                IEnumerable<FeedItem> items = FeedItem.Provider.FetchAllMatching (String.Format (
                    "{0}.FeedID = {1}", FeedItem.Provider.TableName, DbId
                ));
                
                foreach (FeedItem item in items) {
                    item.Feed = this;
                    item.LoadEnclosure ();
                }
                
                this.items.AddRange (items);
                Console.WriteLine ("Done loading items");
                items_loaded = true;
            }
        }

        private void Add (IEnumerable<FeedItem> itms)
        {
            Add (itms, false);
        }
        
        private void Add (IEnumerable<FeedItem> itms, bool commit)
        {
            if (items == null) {
                throw new ArgumentNullException ("itms");
            }      
            
            long totalCountDelta = 0;
            long unreadCountDelta = 0;
            
            List<FeedItem> newItems = new List<FeedItem> ();
            
            if (commit) {
                foreach (FeedItem item in itms)
                    item.Save ();
            }
            
            foreach (FeedItem i in itms) {
                i.Feed = this;             

                if (i.DbId == -1) {
                    continue;
                } else if (i.Active) {
                    ++totalCountDelta;
                            
                    if (!i.IsRead) {
                        ++unreadCountDelta;
                    }
                    
                    items.Add (i); 
                    newItems.Add (i);
                } else {
                    inactive_items.Add (i);   
                }
            }

            if (newItems.Count > 0) {
                OnFeedItemsAdded (newItems);
            }
            
            UpdateItemCountsImpl (totalCountDelta, unreadCountDelta);
        }
        
        private void ClearItemsImpl ()
        {
            items.Clear ();
            UnreadItemCount = 0;            
        }
        
        private void Update (IEnumerable<FeedItem> new_items, bool init)
        {
            if (init) {
                SetItems (new_items);
            } else {
                UpdateItems (new_items);         
            }
        }
        
        private void UpdateItems (IEnumerable<FeedItem> new_items)
        {
            ICollection<FeedItem> tmpNew = null;         
            List<FeedItem> zombies = new List<FeedItem> ();
         
            if (items.Count == 0 && inactive_items.Count == 0) {
                tmpNew = new List<FeedItem> (new_items);
            } else {
                // Get remote items that aren't in the items list
                tmpNew = Diff (items, new_items);
                
                // Of those, remove the ones that are in our inactive list
                tmpNew = Diff (inactive_items, tmpNew);
                
                // Get a list of inactive items that aren't in the remote list any longer
                ICollection<FeedItem> doubleKilledZombies = Diff (
                    new_items, inactive_items
                );

                foreach (FeedItem zombie in doubleKilledZombies) {
                    inactive_items.Remove (zombie);
                }
                
                zombies.AddRange (doubleKilledZombies);                    
                
                foreach (FeedItem fi in Diff (new_items, items)) {
                    if (fi.Enclosure != null &&
                        !String.IsNullOrEmpty (fi.Enclosure.LocalPath)) {
                        // A hack for the podcast plugin, keeps downloaded items 
                        // from being deleted when they are no longer in the feed.
                        continue;
                    }
                    
                    zombies.Add (fi);
                }
            }
            
            if (tmpNew.Count > 0) {
                Add (tmpNew, true);                
            }
            
            if (zombies.Count > 0) {
                foreach (FeedItem zombie in zombies) {
                    if (zombie.Active) {
                        zombie.Delete ();                        
                    } 
                }
                
                // TODO merge
                //ItemsTableManager.Delete (zombies);
            }
        }    

        // Written before LINQ, will update.
        private ICollection<FeedItem> Diff (IEnumerable<FeedItem> baseSet, 
                                            IEnumerable<FeedItem> overlay) {
            bool found;
            List<FeedItem> diff = new List<FeedItem> ();
            
            foreach (FeedItem opi in overlay) {
                found = false;
                
                foreach (FeedItem bpi in baseSet) {
                    if (opi.Title == bpi.Title &&
                        opi.Description == bpi.Description) {
                        found = true;
                        break;
                    }
                }

                if (!found) {  
                    diff.Add (opi);
                }
            }

            return diff;
        }
        
        private void UpdateItemCountsImpl (long totalDelta, long unreadDelta)
        {
            //ItemCount += totalDelta;                
            UnreadItemCount += unreadDelta;
            FEEDS_EVENTS_ITEM_COUNT_FLAGS flags = 0;
            
            if (totalDelta != 0) {   
                flags |= FEEDS_EVENTS_ITEM_COUNT_FLAGS.FEICF_TOTAL_ITEM_COUNT_CHANGED;                  
            }             

            if (unreadDelta != 0) {
                flags |= FEEDS_EVENTS_ITEM_COUNT_FLAGS.FEICF_UNREAD_ITEM_COUNT_CHANGED;
            }
                    
            if (flags != 0) {
                OnFeedItemCountChanged (flags);  
            }             
        }
        
        private bool SetDeleted ()
        {
            bool ret = false;
            
            if (!deleted) {
                ret = deleted = true;
            }
            
            return ret;
        }
        
        private bool SetUpdating ()
        {
            bool ret = false;
            
            if (!updating && !deleted) {
                updatingHandle.Reset ();
                ret = updating = true;
            }
            
            return ret;
        }        
        
        private bool ResetUpdating ()
        {
            bool ret = false;
            
            if (updating) {
                updating = false;
                ret = true;
                updatingHandle.Set ();                
            }
            
            return ret;    
        }
        
#endregion

#region Public Methods
        
        public void AsyncDownload ()
        {
            bool update = false;
            
            lock (sync) {
                if (SetUpdating ()) {
                    update = true;
                    downloadStatus = FeedDownloadStatus.Pending;
                }
            }            
            
            if (update) {
                parent.QueueUpdate (this);                
            }
        }
        
        public bool CancelAsyncDownload ()
        {
            bool ret = false;            
            
            lock (sync) {
                if (SetCanceled ()) {                    
                    if (updating && wc != null) {
                        ret = true;                         
                        wc.CancelAsync ();
                    } else {
                        ret = true;
                        ResetUpdating ();                    
                        OnFeedDownloadCompleted (FeedDownloadError.Canceled);                                            
                    }
                }
            }
            
            return ret;
        }
        
        public int CompareTo (Feed right)
        {
            return title.CompareTo (right.Title);
        }        
        
        public void Delete ()
        {
            Delete (true);                    
        }
            
        public void Delete (bool deleteEnclosures)
        {
            bool del = false;            
            
            lock (sync) {
                if (SetDeleted ()) {                
                    if (updating) {
                        CancelAsyncDownload ();                      
                    }

                    FeedItem[] itms = items.ToArray ();
                    
                    foreach (FeedItem i in itms) {
                        i.DeleteImpl (false, deleteEnclosures);
                    }

                    Remove (itms);                    
                  
                    Provider.Delete (this);   
                    del = true;
                }
            }
            
            if (del) {
                updatingHandle.WaitOne ();
                
                if (deleteEnclosures) {
                	try {
                        FileAttributes attributes;
                        string[] files = Directory.GetFileSystemEntries (localEnclosurePath);
                        
                        foreach (string file in files) {
                            try {                            
                                attributes = File.GetAttributes (file) | FileAttributes.ReadOnly;
                                
                                if (attributes == FileAttributes.ReadOnly) {
                                    File.Delete (file);                                
                                }
                            } catch { continue; }
                        }

                        Directory.Delete (localEnclosurePath, false);
                    } catch {}
                }
                
                OnFeedDeleted ();            
            }
        }
        
        public void Delete (FeedItem item)
        {
            Delete (item, true);
        }
        
        public void Delete (FeedItem item, bool deleteEncFile)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");     
            }    
            
            FeedItem feedItem = item as FeedItem;
            
            if (feedItem != null && feedItem.Feed == this) {
                feedItem.Delete (deleteEncFile);
            }
        }        
        
        public void Delete (IEnumerable<FeedItem> items)
        {
            Delete (items, true);
        }
        
        public void Delete (IEnumerable<FeedItem> items, bool deleteEncFiles)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");     
            }            
            
            FeedItem tmpItem;            
            List<FeedItem> deletedItems = new List<FeedItem> ();
            
            lock (sync) {
                foreach (FeedItem item in items) {
                    tmpItem = item as FeedItem;

                    if (tmpItem != null && tmpItem.Feed == this) {
                        tmpItem.DeleteImpl (false, deleteEncFiles);
                        deletedItems.Add (tmpItem);

                        tmpItem.Active = false;
                        tmpItem.Save ();
                    }
                }
                
                if (deletedItems.Count > 0) {
                    Remove (deletedItems);
                }
            }
        }        
        
        public void Download ()
        {
            throw new NotImplementedException ("Download");
        }

        // TODO remove, unused
        /*public FeedItem GetItem (long itemID)
        {
            lock (sync) { 
                FeedItem item;
                item_id_map.TryGetValue (itemID, out item);
                return item as FeedItem;
            }
        }*/

        public void MarkAllItemsRead ()
        {
            lock (sync) {
                foreach (FeedItem i in items) {
                    i.IsRead = true;
                }
            }
        }

        public override string ToString ()
        {
            return String.Format (
                "Title:  {0} - Url:  {1}",
                Title, Url                                  
            );   
        }

        public void Save ()
        {
            Provider.Save (this);
        }   

        private bool SetCanceled ()
        {
            bool ret = false;
            
            if (!canceled && updating) {
                ret = canceled = true;
            }
            
            return ret;
        }
        
#endregion

#region Private Event Handlers
        
        // Wow, this sucks, see the header FeedsManager Header. 
        private void OnDownloadStringCompleted (object sender, 
                                                Migo.Net.DownloadStringCompletedEventArgs args) 
        {
            FeedDownloadError error = FeedDownloadError.None;            

            try {
                lock (sync) {                              
                    try { 
                        if (args.Error != null) {
                            error = FeedDownloadError.DownloadFailed;                                                        
                            
                            WebException we = args.Error as WebException;   

                            if (we != null) {
                                HttpWebResponse resp = we.Response as HttpWebResponse;
                                
                                if (resp != null) {
                                    switch (resp.StatusCode) {
                                        case HttpStatusCode.NotFound:
                                        case HttpStatusCode.Gone:
                                            error = FeedDownloadError.DoesNotExist;
                                            break;                                
                                        case HttpStatusCode.NotModified:
                                            error = FeedDownloadError.None;                                                        
                                            break;
                                        case HttpStatusCode.Unauthorized:
                                            error = FeedDownloadError.UnsupportedAuth;
                                            break;                                
                                        default:
                                            error = FeedDownloadError.DownloadFailed;
                                            break;
                                    }
                                }
                            }
                        } else if (canceled | deleted) {
                            error = FeedDownloadError.Canceled;                        
                        } else {
                            try {
                                RssParser parser = new RssParser (Url, args.Result);
                                parser.UpdateFeed (this);
                                Update (parser.GetFeedItems (), false);
                            } catch (FormatException e) {
                                Log.Exception (e);
                                error = FeedDownloadError.InvalidFeedFormat;
                            }                          
                        }                        
                    } catch (Exception e) {
                        //Console.WriteLine ("Update error");
                        Console.WriteLine (e.Message);
                        Console.WriteLine (e.StackTrace);                        
                        throw;
                    } finally {  
                        canceled = updating = false;
                        wc.DownloadStringCompleted -= OnDownloadStringCompleted;
                        wc = null; 
                    }
                } 
            } finally {
                try {
                    lastDownloadError = error;
                                    
                    if (lastDownloadError == FeedDownloadError.None) {
                        downloadStatus = FeedDownloadStatus.Downloaded;                         
                    } else {
                        downloadStatus = FeedDownloadStatus.DownloadFailed;                                                    
                    }
                        
                    Save ();
                    
                    OnFeedDownloadCompleted (error);
                } finally {                
                    updatingHandle.Set ();
                }
            }
        }

/*      May add support for individual add in the future, but right now IEnumerables work           
        private void OnFeedItemAdded (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }
            
            parent.RegisterCommand (new CommandWrapper (delegate { 
                EventHandler<FeedItemEventArgs> handler = FeedItemAdded;
                
                parent.OnFeedItemAdded (this, item);
                
                if (handler != null) {
                    OnFeedItemEvent (handler, new FeedItemEventArgs (this, item));
                }                          
            }));  
        }
*/        

        private void OnFeedItemsAdded (IEnumerable<FeedItem> items)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }        

            parent.RegisterCommand (new CommandWrapper (delegate { 
                EventHandler<FeedItemEventArgs> handler = FeedItemAdded;
                
                parent.OnFeedItemsAdded (this, items);
                
                if (handler != null) {
                    OnFeedItemEvent (handler, new FeedItemEventArgs (this, items));
                }                          
            }));               
        }    
        
        private void OnFeedItemRemoved (FeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");
            }   

            parent.RegisterCommand (new CommandWrapper (delegate { 
                EventHandler<FeedItemEventArgs> handler = FeedItemRemoved;
                
                parent.OnFeedItemRemoved (this, item);
                
                if (handler != null) {
                    OnFeedItemEvent (handler, new FeedItemEventArgs (this, item));
                }                          
            }));                        
        }
        
        private void OnFeedItemsRemoved (IEnumerable<FeedItem> items)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");
            }        
            
            parent.RegisterCommand (new CommandWrapper (delegate { 
                EventHandler<FeedItemEventArgs> handler = FeedItemRemoved;
                
                parent.OnFeedItemsRemoved (this, items);
                
                if (handler != null) {
                    OnFeedItemEvent (handler, new FeedItemEventArgs (this, items));
                }                          
            }));
        }        
        
        private void OnFeedItemEvent (EventHandler<FeedItemEventArgs> handler,  
                                                             FeedItemEventArgs e)
        {
            handler (this, e);            
        }
        
        private void OnFeedDeleted ()
        {
            parent.RegisterCommand (new CommandWrapper (delegate { 
                parent.OnFeedDeleted (this);            
                OnFeedEventRaised (FeedDeleted);                
            }));            
        }

        private void OnFeedDownloadCountChanged (FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS flags)
        {             
            parent.RegisterCommand (new CommandWrapper (delegate { 
                EventHandler<FeedDownloadCountChangedEventArgs> handler = FeedDownloadCountChanged;            
                
                parent.OnFeedDownloadCountChanged (this, flags);              
                
                if (handler != null) {
                    handler (
                        this, new FeedDownloadCountChangedEventArgs (this, flags)
                    );
                }
            }));
        }        
        
        private void OnFeedDownloadCompleted (FeedDownloadError error)
        {
            parent.RegisterCommand (new CommandWrapper (delegate { 
                EventHandler<FeedDownloadCompletedEventArgs> handler = FeedDownloadCompleted;                
                
                parent.OnFeedDownloadCompleted (this, error);
                
                if (handler != null) {
                    handler (this, new FeedDownloadCompletedEventArgs (this, error));
                }                
            }));
        }
        
        private void OnFeedDownloading ()
        {
            parent.RegisterCommand (new CommandWrapper (delegate { 
                parent.OnFeedDownloading (this);            
                OnFeedEventRaised (FeedDownloading);                
            }));
        }
        
        private void OnFeedItemCountChanged (FEEDS_EVENTS_ITEM_COUNT_FLAGS flags)
        {
            parent.RegisterCommand (new CommandWrapper (delegate {
                EventHandler<FeedItemCountChangedEventArgs> handler = FeedItemCountChanged;
                
                parent.OnFeedItemCountChanged (this, flags);
                            
                if (handler != null) {
                    handler (this, new FeedItemCountChangedEventArgs (this, flags));          
                }                 
            }));
        }                
                
        private void OnFeedRenamed ()
        {
            parent.RegisterCommand (new CommandWrapper (delegate { 
                parent.OnFeedRenamed (this);            
                OnFeedEventRaised (FeedRenamed);
            }));
        }
        
        private void OnFeedUrlChanged ()
        {            
            parent.RegisterCommand (new CommandWrapper (delegate {
                parent.OnFeedUrlChanged (this);                    
                OnFeedEventRaised (FeedUrlChanged);
            }));
        }

        // Drr, isn't handler a copy already?  Do something?
        private void OnFeedEventRaised (EventHandler<FeedEventArgs> handler)
        {
            EventHandler<FeedEventArgs> handlerCpy = handler;
            
            if (handlerCpy != null) {
                handlerCpy (this, new FeedEventArgs (this));
            }
        }

#endregion        

    }
}    
