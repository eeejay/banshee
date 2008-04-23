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

using Migo.Net;
using Migo.TaskCore;
using Migo.DownloadCore;
using Migo.Syndication.Data;

namespace Migo.Syndication
{    
    public class Feed : IFeed
    {        
        private bool canceled;
        private bool deleted; 
        private bool updating;
        
        private AsyncWebClient wc;
        private ManualResetEvent updatingHandle = new ManualResetEvent (true);
        
        private readonly object sync = new object ();
       
        private long queuedDownloadCount;
        private long activeDownloadCount;        
        
        private string copyright;
        private string description;
        private bool downloadEnclosuresAutomatically;
        private FeedDownloadStatus downloadStatus;
        private string downloadUrl;
        private string image;        
        private long interval;
        private List<FeedItem> inactiveItems;
        private bool isList;
        private long itemCount;
        private List<FeedItem> items;
        private Dictionary<long,FeedItem> itemsByID;        
        private string language;
        private DateTime lastBuildDate;
        private FeedDownloadError lastDownloadError;
        private DateTime lastDownloadTime;      
        private DateTime lastWriteTime;
        private string link;
        private string localEnclosurePath;
        private long localID;
        private long maxItemCount;
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
        
        public long ActiveDownloadCount 
        { 
            get { lock (sync) { return activeDownloadCount; } }
        }
        
        public long QueuedDownloadCount 
        { 
            get { lock (sync) { return queuedDownloadCount; } }             
        }         
        
        public string Copyright 
        { 
            get { lock (sync) { return copyright; } } 
        }
        
        public string Description 
        { 
            get { lock (sync) { return description; } } 
        }
        
        public bool DownloadEnclosuresAutomatically 
        { 
            get { lock (sync) { return downloadEnclosuresAutomatically; } }  
            set { lock (sync) { downloadEnclosuresAutomatically = value; } }
        }
        
        public FeedDownloadStatus DownloadStatus 
        { 
            get { lock (sync) { return downloadStatus; } }
        }
        
        public string DownloadUrl 
        { 
            get { lock (sync) { return downloadUrl; } }
        }     
        
        public string Image 
        { 
            get { lock (sync) { return image; } }
        }

        public long Interval        
        { 
            get { lock (sync) { return interval; } }
            set { 
                lock (sync) { interval = (value < 15) ? 1440 : value; }
            } 
        }
        
        public bool IsList 
        { 
            get { lock (sync) { return isList; } }
        }
        
        public long ItemCount 
        { 
            get { lock (sync) { return itemCount; } }

            internal set { 
                lock (sync) {
                    if (value < 0 /*|| value > maxItemCount*/) { // implement later
                       	throw new ArgumentOutOfRangeException ("ItemCount:  Must be >= 0 and < MaxItemCount.");
                    }   
                }
                
                itemCount = value;
            }             
        }
        
        // I want LINQ -_-
        public ReadOnlyCollection<IFeedItem> Items { 
            get { 
                lock (sync) {  
                    List<IFeedItem> tmpItems = items.ConvertAll ( 
                        new Converter<FeedItem,IFeedItem> (
                            delegate (FeedItem fi) { return fi as IFeedItem; }
                        )
                    );
                    
                    tmpItems.Sort (
                        delegate (IFeedItem lhs, IFeedItem rhs) {
                            return DateTime.Compare (rhs.PubDate, lhs.PubDate);
                        }
                    );
                    
                    return tmpItems.AsReadOnly ();                                     
                }
            } 
        }
        
        public string Language 
        { 
            get { lock (sync) { return language; } }
        }
        
        public DateTime LastBuildDate 
        { 
            get { lock (sync) { return lastBuildDate; } }
        }
        
        public FeedDownloadError LastDownloadError 
        { 
            get { lock (sync) { return lastDownloadError; } }
        }
        
        public DateTime LastDownloadTime 
        { 
            get { lock (sync) { return lastDownloadTime; } }
        }
        
        public DateTime LastWriteTime 
        { 
            get { lock (sync) { return lastWriteTime; } }
        }
        
        public string Link 
        { 
            get { lock (sync) { return link; } }
        }
        
        public string LocalEnclosurePath 
        { 
            get { lock (sync) { return localEnclosurePath; } }
            
            set { 
                lock (sync) {
                    if (localEnclosurePath != value) {
                        localEnclosurePath = value;
                        Commit ();                    	
                    }
                }
            }
        }
        
        public long LocalID 
        { 
            get { lock (sync) { return localID; } }
            internal set { lock (sync) { localID = value; } }
        }

        public long MaxItemCount
        { 
            get { lock (sync) { return maxItemCount; } }
            set { lock (sync) { maxItemCount = value; } }
        }
            
        public string Name 
        { 
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
                        Commit ();
                    }
                }
                
                if (renamed) {
                    OnFeedRenamed ();                	
                }
            }            
        }

        public IFeedsManager Parent
        {
            get { lock (sync) { return parent; } }
        }
        
        public DateTime PubDate 
        { 
            get { lock (sync) { return pubDate; } }
        }
        
		public FeedSyncSetting SyncSetting 
		{ 
            get { lock (sync) { return syncSetting; } } 
        }
        
        public string Title 
        { 
            get { 
                lock (sync) { 
                    return String.IsNullOrEmpty (title) ?
                        url : title; 
                } 
            }
        }
              
        public long Ttl 
        { 
            get { lock (sync) { return ttl; } }
        }  
        
        public long UnreadItemCount 
        { 
            get { lock (sync) { return unreadItemCount; } } 
            
            internal set {
                if (value < 0 /*|| value > maxItemCount*/ || value > itemCount) {  
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
        
        public string Url 
        { 
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
                        Commit ();
                    }
                }
                
                if (updated) {
                    parent.UpdateFeedUrl (oldUrl, this);                
                    OnFeedUrlChanged ();
                }
            }
        }
        
        internal Feed (FeedsManager parent, string url) : this (parent)
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
        
        internal Feed (FeedsManager parent)
        {
            if (parent == null) {
                throw new ArgumentNullException ("parent");
            }
            
            this.parent = parent;

            downloadStatus = FeedDownloadStatus.None;  
            inactiveItems = new List<FeedItem> ();
            interval = parent.DefaultInterval; 
            isList = false;            
            itemCount = 0;            
            items = new List<FeedItem> ();
            itemsByID = new Dictionary<long,FeedItem> ();
            localID = -1;   
            maxItemCount = 200; //parent.ItemCountLimit;        IGNORED FOR NOW     
            syncSetting = FeedSyncSetting.Default;            
            unreadItemCount = 0;       
            copyright = String.Empty;
            description = String.Empty;
            downloadUrl = String.Empty;
            image = String.Empty;      
            language = String.Empty;
            link = String.Empty;
            localEnclosurePath = String.Empty;
            name = String.Empty;
            title = String.Empty;
            url = String.Empty;
        }

        internal Feed (FeedsManager parent, IFeedWrapper wrapper) : this (parent)
        {
            if (wrapper == null) {
                throw new ArgumentNullException ("wrapper");
            }
            
            url = wrapper.Url;                
            Update (wrapper, true);  
        }

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
                item.Commit ();
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
            
            List<IFeedItem> newItems = new List<IFeedItem> ();
            
            if (commit) {
                ItemsTableManager.Commit (itms);
            }
            
            foreach (FeedItem i in itms) {
                i.Parent = this;                

                if (i.LocalID == -1) {
                    continue;
                } else if (i.Active) {
                    itemsByID.Add (i.LocalID, i);                        
                    ++totalCountDelta;
                            
                    if (!i.IsRead) {
                        ++unreadCountDelta;
                    }
                    
                    items.Add (i); 
                    newItems.Add (i);
                } else {
                    inactiveItems.Add (i);   
                }
            }

            if (newItems.Count > 0) {
                OnFeedItemsAdded (newItems);
            }
            
            UpdateItemCountsImpl (totalCountDelta, unreadCountDelta);
        }         
        
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
        
        private void ClearItemsImpl ()
        {
            items.Clear ();                
            itemsByID.Clear ();
                
            ItemCount = 0;
            UnreadItemCount = 0;            
        }
        
        public int CompareTo (IFeed right)
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
                  
                    FeedsTableManager.Delete (this);   
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
        
        public void Delete (IFeedItem item)
        {
            Delete (item, true);
        }
        
        public void Delete (IFeedItem item, bool deleteEncFile)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");     
            }    
            
            FeedItem feedItem = item as FeedItem;
            
            if (feedItem != null && feedItem.Parent == this) {
                feedItem.Delete (deleteEncFile);
            }
        }        
        
        public void Delete (IEnumerable<IFeedItem> items)
        {
            Delete (items, true);
        }
        
        public void Delete (IEnumerable<IFeedItem> items, bool deleteEncFiles)
        {
            if (items == null) {
                throw new ArgumentNullException ("items");     
            }            
            
            FeedItem tmpItem;            
            List<FeedItem> deletedItems = new List<FeedItem> ();
            
            lock (sync) {
                foreach (IFeedItem item in items) {
                    tmpItem = item as FeedItem;

                    if (tmpItem != null && tmpItem.Parent == this) {
                        tmpItem.DeleteImpl (false, deleteEncFiles);
                        deletedItems.Add (tmpItem);
                    }
                }
                
                ItemsTableManager.Deactivate (deletedItems);            
                
                if (deletedItems.Count > 0) {
                    Remove (deletedItems);
                }
            }
        }        
        
        public void Download ()
        {
            throw new NotImplementedException ("Download");
        }

        public IFeedItem GetItem (long itemID)
        {
            lock (sync) { 
                FeedItem item;
                itemsByID.TryGetValue (itemID, out item);
                return item as IFeedItem;
            }
        }

        public void MarkAllItemsRead ()
        {
            lock (sync) {
                foreach (IFeedItem i in items) {
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

        private void Update (IFeedWrapper wrapper, bool init)
        {
            if (wrapper == null) {
                throw new ArgumentNullException ("wrapper");
            }
            
            copyright = wrapper.Copyright;
            description = wrapper.Description;
            downloadUrl = wrapper.DownloadUrl;
            image = wrapper.Image;
            Interval = wrapper.Interval;
            isList = wrapper.IsList;
            language = wrapper.Language;
            lastBuildDate = wrapper.LastBuildDate;
            lastDownloadTime = wrapper.LastDownloadTime;                      
            lastWriteTime = lastBuildDate;  // This is not correct!!!!!
            link = wrapper.Link;
            localEnclosurePath = wrapper.LocalEnclosurePath;            
            pubDate = wrapper.PubDate;
            title = wrapper.Title;
            ttl = wrapper.Ttl;
            
            List<FeedItem> itms = new List<FeedItem> ();
            
            if (wrapper.Items != null) {            
                FeedItem tmpItem = null;

                foreach (IFeedItemWrapper i in wrapper.Items) {
                    try {
                        tmpItem = CreateFeedItem (i);                    
                        
                        if (tmpItem != null) {
                            itms.Add (tmpItem);
                        }
                    } catch {}
                }                            
            }            
            
            if (init) {
                SetItems (itms);
                localID = wrapper.LocalID;
                name = wrapper.Name;
            } else {
                Name = wrapper.Name;
                UpdateItems (itms);         
            }
        }
        
        private void UpdateItems (ICollection<FeedItem> remoteItems)
        {            
            ICollection<FeedItem> tmpNew = null;         
            List<FeedItem> zombies = new List<FeedItem> ();         
         
            if (items.Count == 0 && inactiveItems.Count == 0) {
                tmpNew = remoteItems;            
            } else {
                tmpNew = Diff (items, remoteItems);
                tmpNew = Diff (inactiveItems, tmpNew);
                
                ICollection<FeedItem> doubleKilledZombies = Diff (
                    remoteItems, inactiveItems
                );                 
                
                foreach (FeedItem zombie in doubleKilledZombies) {
                    inactiveItems.Remove (zombie);
                }
                
                zombies.AddRange (doubleKilledZombies);                    
                
                foreach (FeedItem fi in Diff (remoteItems, items)) {
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
                
                // ZombieRifle.Polish ();
                ItemsTableManager.Delete (zombies);
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

        internal void Commit ()
        {
            if (localID <= 0) {
                localID = FeedsTableManager.Insert (this);
            } else {
                try {
                    FeedsTableManager.Update (this);
                } catch {
                    //Console.WriteLine (e.StackTrace);
                    throw;
                }
            }
        }   
        
        private FeedItem CreateFeedItem (IFeedItemWrapper wrapper)
        {
            FeedItem ret = null;
            
            try {
                ret = new FeedItem (this, wrapper);
            } catch {}
            
            return ret; 
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
                    inactiveItems.Add (item);                    
                    itemsByID.Remove (item.LocalID);
                    
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
            
            List<IFeedItem> removedItems = new List<IFeedItem> ();                    
                    
            lock (sync) {            
                foreach (FeedItem i in itms) {
                    if (i != null) {
                        if (items.Remove (i)) {
                            --totalDelta;
                            
                            if (!i.IsRead) {
                                --unreadDelta;
                            }                  
                            
                            removedItems.Add (i);
                            inactiveItems.Add (i);                            
                            itemsByID.Remove (i.LocalID);
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
        
        private bool SetCanceled ()
        {
            bool ret = false;
            
            if (!canceled && updating) {
                ret = canceled = true;
            }
            
            return ret;
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
        
        internal void UpdateItemCounts (long totalDelta, long unreadDelta)
        {
            lock (sync) {
                UpdateItemCountsImpl (totalDelta, unreadDelta);                    
            }
        }
        
        private void UpdateItemCountsImpl (long totalDelta, long unreadDelta)
        {
            ItemCount += totalDelta;                
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
        
        // Wow, this sucks, see the header FeedsManager Header. 
        private void OnDownloadStringCompleted (object sender, 
                                                Migo.Net.DownloadStringCompletedEventArgs e) 
        {
            FeedDownloadError error = FeedDownloadError.None;            

            try {
                lock (sync) {                              
                    try { 
                        if (e.Error != null) {
                            error = FeedDownloadError.DownloadFailed;                                                        
                            
                            WebException we = e.Error as WebException;   

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
                                IFeedWrapper wrapper = new RssFeedWrapper (Url, e.Result);                             
                                Update (wrapper, false);
                            } catch (FormatException) {
                                //Console.WriteLine ("FormatException:  {0}", fe.Message);
                                error = FeedDownloadError.InvalidFeedFormat;
                            }                          
                        }                        
                    } catch (Exception e2) {
                        //Console.WriteLine ("Update error");
                        Console.WriteLine (e2.Message);
                        Console.WriteLine (e2.StackTrace);                        
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
                        
                    Commit ();
                    
                    OnFeedDownloadCompleted (error);
                } finally {                
                    updatingHandle.Set ();
                }
            }
        }

/*      May add support for individual add in the future, but right now IEnumerables work           
        private void OnFeedItemAdded (IFeedItem item)
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
        private void OnFeedItemsAdded (IEnumerable<IFeedItem> items)
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
        
        private void OnFeedItemRemoved (IFeedItem item)
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
        
        private void OnFeedItemsRemoved (IEnumerable<IFeedItem> items)
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
    }
}    
