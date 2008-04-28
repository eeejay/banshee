/*************************************************************************** 
 *  FeedsManager.cs
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

/*
    This needs to be completed and fixed.
    
    When I was writing this I was interrupted and wasn't able to get back to it 
    for six months.  There are numerous issues with race conditions.  I'm 
    resisting the urge to put in a bunch of hacks to patch the problem in favor 
    of going over it line by line and actually fixing it.
*/

using System;
using System.IO;
using System.Data;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena;
using Hyena.Data.Sqlite;

using Migo.DownloadCore;
using Migo.TaskCore;
using Migo.TaskCore.Collections;
using Migo.Syndication.Data;

namespace Migo.Syndication
{
    public class FeedsManager : IDisposable
    {        
        private bool disposed;        
        
        private bool ticDirty = true;
        private long totalItemCount;
        
        private bool tuicDirty = true;
        private long totalUnreadItemCount;
        
        private List<Feed> feeds;
        private List<Feed> queued_feeds;

        private AsyncCommandQueue<ICommand> command_queue;
        
        private Dictionary<long, Feed> id_feed_map;
        private Dictionary<string, Feed> url_feed_map;
        
        private Dictionary<FeedEnclosure, HttpFileDownloadTask> queued_downloads;
        
        private ManualResetEvent download_handle;
        private DownloadManager download_manager;
        
        private TaskList<FeedUpdateTask> update_list;
        private TaskGroup<FeedUpdateTask> update_group;
        
        private readonly object sync = new object (); 
        
        public event EventHandler<TaskEventArgs<HttpFileDownloadTask>> EnclosureDownloadCompleted;        
        
        public event EventHandler<FeedEventArgs> FeedAdded;
        public event EventHandler<FeedEventArgs> FeedDeleted;
        public event EventHandler<FeedDownloadCountChangedEventArgs> FeedDownloadCountChanged;                
        public event EventHandler<FeedDownloadCompletedEventArgs> FeedDownloadCompleted;
        public event EventHandler<FeedEventArgs> FeedDownloading;
        public event EventHandler<FeedItemCountChangedEventArgs> FeedItemCountChanged;
        public event EventHandler<FeedEventArgs> FeedRenamed;
        public event EventHandler<FeedEventArgs> FeedUrlChanged;        
        
        public event EventHandler<FeedItemEventArgs> FeedItemAdded;
        public event EventHandler<FeedItemEventArgs> FeedItemRemoved;
        
        internal static FeedsManager Instance;
        
        internal AsyncCommandQueue<ICommand> CommandQueue {
            get { return command_queue; }
        }
        
#region Public Properties

        public FeedBackgroundSyncStatus BackgroundSyncStatus {
            get {
                lock (sync) {
                    return FeedBackgroundSyncStatus.Disabled;
                }
            }
        }        
        
        // TODO interval for what, and in what unit?
        public long DefaultInterval {
            get { lock (sync) { return 15; } }
            set { throw new NotImplementedException ("DefaultInterval"); }
        }

        public DownloadManager DownloadManager {
            get { return download_manager; }
        }
        
        private ReadOnlyCollection<Feed> ro_feeds;
        public ReadOnlyCollection<Feed> Feeds {             
            get { 
                lock (sync) {
                    return ro_feeds ?? ro_feeds = new ReadOnlyCollection<Feed> (feeds);
                }
            }
        }

        public long ItemCountLimit {
            get { lock (sync) { return -1; } }
        }        
        
        public long TotalItemCount { 
            get {
                lock (sync) {
                    if (ticDirty) {
                    	totalItemCount = 0;
                        
                        foreach (Feed f in feeds) {
                            totalItemCount += f.ItemCount;
                    	}
                        
                        ticDirty = false;                        
                    }
                    
                    return totalItemCount;
                }
            }
        }
                
        public long TotalUnreadItemCount {
            get {        
                lock (sync) {
                    if (tuicDirty) {
                    	totalUnreadItemCount = 0;
                        
                        foreach (Feed f in feeds) {
                            totalUnreadItemCount += f.UnreadItemCount;
                    	}
                        
                        tuicDirty = false;
                    }
                    
                    return totalUnreadItemCount;
                }
            }
        }
        
#endregion

#region Constructor
        
        public FeedsManager (HyenaSqliteConnection connection, DownloadManager manager)
        {
            if (connection == null) {
                throw new ArgumentException ("connection is null");
            } else if (manager == null) {
                throw new ArgumentNullException ("manager");
            }
            
            // Hack to work around Feeds being needy and having to call all our internal methods, instead
            // of us just listening for their events.
            Instance = this;
            
            download_manager = manager;

            FeedEnclosure.Provider = new SqliteModelProvider<FeedEnclosure> (connection, "PodcastEnclosures");
            FeedItem.Provider = new SqliteModelProvider<FeedItem> (connection, "PodcastItems");
            Migo.Syndication.Feed.Provider = new SqliteModelProvider<Migo.Syndication.Feed> (connection, "PodcastSyndications");
            
            feeds = new List<Feed> ();
            queued_feeds = new List<Feed> ();
            id_feed_map = new Dictionary<long, Feed> ();
            url_feed_map = new Dictionary<string, Feed> ();

            download_handle = new ManualResetEvent (true);

            download_manager.Tasks.TaskAdded += OnDownloadTaskAdded;
            download_manager.Tasks.TaskRemoved += OnDownloadTaskRemoved;            
            download_manager.Group.TaskStatusChanged += OnDownloadTaskStatusChangedHandler;

            queued_downloads = new Dictionary<FeedEnclosure, HttpFileDownloadTask> ();
            update_list = new TaskList<FeedUpdateTask> ();
            update_group = new TaskGroup<FeedUpdateTask> (4, update_list);
            update_group.TaskStopped += TaskStoppedHandler;
            update_group.TaskAssociated += TaskAssociatedHandler;
            
            /*foreach (Feed feed in Feed.Provider.FetchAll ()) {
                Console.WriteLine ("Adding feed {0}", feed.Url);
                AddFeed (feed);
            }*/
            
            command_queue = new AsyncCommandQueue<ICommand> ();
        }
        
#endregion

#region Public Methods

        public Feed CreateFeed (string url)
        {
            Feed feed = null;
            url = url.Trim ().TrimEnd ('/');

            lock (sync) {   
                if (!url_feed_map.ContainsKey (url)) {
                    feed = new Feed (url);
                    feed.Save ();
                    AddFeed (feed);
                    OnFeedAdded (feed);
                }
            }
            
            return feed;
        }
        
        public HttpFileDownloadTask QueueDownload (FeedEnclosure enclosure) 
        {
            return QueueDownload (enclosure, true);         
        }
           
        public HttpFileDownloadTask QueueDownload (FeedEnclosure enclosure, bool queue)
        {
            if (enclosure == null) {
                throw new ArgumentNullException ("enc");
            }
            
            HttpFileDownloadTask task = null;       
            
            lock (sync) {
                if (disposed) {
                    return null;
                }
                
                if (!queued_downloads.ContainsKey (enclosure)) {                    
                    Feed parentFeed = enclosure.Item.Feed;                    
                    
                    if (parentFeed != null && !IsFeedOurs (parentFeed)) {                        
                        task = download_manager.CreateDownloadTask (enclosure.Url, enclosure);
                        //Console.WriteLine ("Task DL path:  {0}", task.LocalPath);
                        task.Name = String.Format ("{0} - {1}", parentFeed.Title, enclosure.Item.Title);
                        
                        task.Completed += OnDownloadTaskCompletedHandler;                    
                        
                        // Race condition...
                        // Should only be added when the task is associated or 
                        // it can be canceled before it is added to the progress manager.
                        
                        // Add a pre-association dict and move tasks to the 
                        // queued dict once they've been offically added.
                        
                        queued_downloads.Add (enclosure, task);
                    }                    
                }

                if (task != null && queue) {
                    download_manager.QueueDownload (task);                                                                    	
                }      
            }
                       
            return task;        
        }
        
        public IEnumerable<HttpFileDownloadTask> QueueDownloads (IEnumerable<FeedEnclosure> encs)
        {
            if (encs == null) {
                throw new ArgumentNullException ("encs");
            }
            
            ICollection<HttpFileDownloadTask> encsCol = encs as ICollection<HttpFileDownloadTask>;
            
            List<HttpFileDownloadTask> tasks = (encsCol == null) ?
                new List<HttpFileDownloadTask> () : 
                new List<HttpFileDownloadTask> (encsCol.Count);
            
            HttpFileDownloadTask tmpTask = null;
            
            lock (sync) {   
                if (disposed) {
                    return tasks;
                }
                
                foreach (FeedEnclosure enc in encs) {
                    if (enc.SetDownloading ()) {
                        tmpTask = QueueDownload (enc, false);
                        
                        if (tmpTask == null) {
                            enc.ResetDownloading ();
                            continue;
                        }
                        
                        tasks.Add (tmpTask);
                    }
                }
                
                if (tasks.Count > 0) {
                    download_manager.QueueDownload (tasks);
                }
            }
            
            return tasks;
        }

        // TODO remove these? not used
        /*public void AsyncSyncAll ()
        {
            List<Feed> allFeeds = null;
            
            lock (sync) {
                if (feeds.Count > 0) {
                    allFeeds = new List<Feed> (feeds);
                }
            }

            if (allFeeds != null) {
                foreach (Feed f in allFeeds) {
                    f.AsyncDownload ();
                }
            }
        }
        
        public void DeleteFeed (long feedID)
        {
            Feed feed = null;
            
            lock (sync) {
                idFeedDict.TryGetValue(feedID, out feed);
            }
            
            if (feed != null) {
                DeleteFeed (idFeedDict[feedID]);
            }                
        }   
          
        public void DeleteFeed (Feed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");      	
            } else if (!IsFeedOurs (feed)) {
                feed.Delete ();
            }
        }
      
        public void DequeueDownloads (IEnumerable<FeedEnclosure> enclosures) 
        {
            if (enclosures == null) {
                throw new ArgumentNullException ("enclosures");
            }
            
            IEnumerable<HttpFileDownloadTask> tasks =   
                FindDownloadTasks (enclosures);
                
            downloadManager.RemoveDownload (tasks);
        }
        
        public bool ExistsFeed (string url)
        {
            lock (sync) {
                return urlFeedDict.ContainsKey (url);
            }
        }
        
        public bool ExistsFeed (long feedID)
        {
            lock (sync) {
                return idFeedDict.ContainsKey (feedID);
            }
        }
        
        public Feed GetFeed (long feedID)
        {
            Feed ret = null;
            
            lock (sync) {
                idFeedDict.TryGetValue (feedID, out ret);
            }
            
            return ret;
        }
            
        public Feed GetFeedByUrl (string url)
        {
            Feed ret = null;
            
            lock (sync) {
                urlFeedDict.TryGetValue (url, out ret);
            }
            
            return ret;
        }
        
        public bool IsSubscribed (string url)
        {
            lock (sync) {
                return urlFeedDict.ContainsKey (url); 
            }
        }*/

        public void Dispose () 
        {
            if (SetDisposed ()) {               
                AutoResetEvent disposeHandle = new AutoResetEvent (false);
                Console.WriteLine ("FM - Dispose - 000");
    
                List<HttpFileDownloadTask> tasks = null;
    
                lock (sync) {
                    if (queued_downloads.Count > 0) {
                        tasks = new List<HttpFileDownloadTask> (queued_downloads.Values);
                    }
                }

                if (tasks != null) {
                    foreach (HttpFileDownloadTask t in tasks) {
                        t.Stop ();
                    }
                    
                    Console.WriteLine ("downloadHandle - WaitOne ()");
                    download_handle.WaitOne ();
                }

                if (update_group != null) {
                    update_group.CancelAsync ();                
                    
                    Console.WriteLine ("FM - Dispose - 000.5");
                    
                    update_group.Handle.WaitOne ();
                    
                    Console.WriteLine ("FM - Dispose - 001");
                    
                    update_group.Dispose (disposeHandle);
                    
                    Console.WriteLine ("FM - Dispose - 002");
                    
                    disposeHandle.WaitOne ();
                    
                    update_group.TaskStopped -= TaskStoppedHandler;
                    update_group.TaskAssociated -= TaskAssociatedHandler;
                    
                    update_group = null;
                    
                    Console.WriteLine ("FM - Dispose - 003");
                }
               
                update_list = null;
                
                Console.WriteLine ("FM - Dispose - 008");    
                                 
                if (download_handle != null) {
                    download_handle.Close ();
                    download_handle = null;
                }                
                
                Console.WriteLine ("FM - Dispose - 007");                    
                
                if (download_manager != null) {
                    download_manager.Tasks.TaskAdded -= OnDownloadTaskAdded;
                    download_manager.Tasks.TaskRemoved -= OnDownloadTaskRemoved; 
                    download_manager = null;
                }                          

                Console.WriteLine ("FM - Dispose - 004");                  

                if (command_queue != null) {
                    command_queue.Dispose ();
                    command_queue = null;
                }
                
                disposeHandle.Close ();
            }
        }

#endregion

#region Private Methods

        private void AddFeed (Feed feed)
        {
            if (feed != null && !id_feed_map.ContainsKey (feed.DbId)) {
                id_feed_map[feed.DbId] = feed;
                url_feed_map[feed.Url] = feed;
                feeds.Add (feed);
            }                 
        }

        private void RemoveFeed (Feed feed)
        {
            if (feed != null && feeds.Remove (feed)) {
                url_feed_map.Remove (feed.Url);            
                id_feed_map.Remove (feed.DbId);                
            }
        }           
        
        private bool IsFeedOurs (Feed feed)
        {
            bool ret = true;
            Feed f = feed as Feed;
            
            if (f != null && f.Parent == this) {
                ret = false;
            }
            
            return ret;
        }

       
        private HttpFileDownloadTask FindDownloadTask (FeedEnclosure enc)
        {
            if (enc == null) {
                throw new ArgumentNullException ("enc");
            }
            
            return FindDownloadTaskImpl ((FeedEnclosure)enc);
        }
        
        private HttpFileDownloadTask FindDownloadTaskImpl (FeedEnclosure enc) 
        {
            HttpFileDownloadTask task = null;
            Feed parentFeed = enc.Item.Feed as Feed;                               
            
            if (parentFeed != null && 
                !IsFeedOurs (parentFeed) && 
                queued_downloads.ContainsKey (enc)) {
                task = queued_downloads[enc];
            }
            
            return task;
        }        


        private bool SetDisposed ()
        {
            bool ret = false;
                
            lock (sync) {
                if (!disposed) {
                    ret = disposed = true;   
                }
            }
                
            return ret;
        }        
        
        private void TaskAddedAction (HttpFileDownloadTask task)
        {
            Feed parentFeed = null;
            FeedEnclosure enc = task.UserState as FeedEnclosure;
                    
            if (enc != null) {
                lock (sync) { 
                    parentFeed = enc.Item.Feed;                  
                    
                    if (parentFeed != null && !IsFeedOurs (parentFeed) &&
                        queued_downloads.ContainsKey (enc)) {
                        if (queued_downloads.Count == 0) {
                            download_handle.Reset ();
                        }                        
                                                    
                        enc.DownloadStatus = FeedDownloadStatus.Pending;                        
                        parentFeed.IncrementQueuedDownloadCount ();                    
                    }
                }
            }        
        }
        
        private void OnDownloadTaskAdded (object sender, TaskAddedEventArgs<HttpFileDownloadTask> e)
        {
            if (e.Task != null) {
                TaskAddedAction (e.Task);
            } else if (e.Tasks != null) {
                foreach (HttpFileDownloadTask task in e.Tasks) {
                    TaskAddedAction (task);                                    
                }
            }
        }
        
        private void  OnDownloadTaskCompletedHandler (object sender, 
                                                      TaskCompletedEventArgs e)
        {
            HttpFileDownloadTask task = sender as HttpFileDownloadTask;             
            FeedEnclosure enc = task.UserState as FeedEnclosure;

            if (enc != null) {   
                if (e.Error != null || task.Status == TaskStatus.Failed) {
                    enc.DownloadStatus = FeedDownloadStatus.DownloadFailed;
                } else if (!e.Cancelled) {
                    if (task.Status == TaskStatus.Succeeded) {
                        try {                        
                            enc.SetFileImpl (
                                task.RemoteUri.ToString (), 
                                Path.GetDirectoryName (task.LocalPath), 
                                task.MimeType, 
                                Path.GetFileName (task.LocalPath)
                            );                             
                        } catch {
                            enc.LastDownloadError = FeedDownloadError.DownloadFailed;
                            enc.DownloadStatus = FeedDownloadStatus.DownloadFailed;  
                        }
                    }
                }
            }
            
            OnEnclosureDownloadCompleted (task);            
        }
        
        private void DownloadTaskRemoved (FeedEnclosure enc, 
                                          HttpFileDownloadTask task,
                                          bool decQueuedCount)
        {
            if (queued_downloads.ContainsKey (enc)) {
                queued_downloads.Remove (enc);    
                task.Completed -= OnDownloadTaskCompletedHandler;                                            
                
                if (decQueuedCount) {
                    enc.Item.Feed.DecrementQueuedDownloadCount ();
                }
                
                if (queued_downloads.Count == 0) {
                	if (download_handle != null) {
                	    download_handle.Set ();
                	}
                }
            }
        }
        
        private void OnDownloadTaskRemoved (object sender, 
                                            TaskRemovedEventArgs<HttpFileDownloadTask> e)
        {
            if (e.Task != null) {
                FeedEnclosure enc = e.Task.UserState as FeedEnclosure;
                    
                if (enc != null) {
                    lock (sync) {
                        DownloadTaskRemoved (enc, e.Task, true);
                    }
                }
            } else if (e.Tasks != null) {
                Feed tmpParent = null;
                FeedEnclosure tmpEnclosure = null;
                List<FeedEnclosure> tmpList = null;
                
                Dictionary<Feed, List<FeedEnclosure>> feedDict =
                    new Dictionary<Feed,List<FeedEnclosure>> ();
                
                lock (sync) {
                    foreach (HttpFileDownloadTask t in e.Tasks) {
                        tmpEnclosure = t.UserState as FeedEnclosure;
                        
                        if (tmpEnclosure != null) {
                            tmpParent = tmpEnclosure.Item.Feed;
                            
                            if (!feedDict.TryGetValue (tmpParent, out tmpList)) {
                                tmpList = new List<FeedEnclosure> ();
                                feedDict.Add (tmpParent, tmpList);  
                            }
                            
                            tmpList.Add (tmpEnclosure);
                            DownloadTaskRemoved (tmpEnclosure, t, false);
                        }
                    }
                    
                    foreach (KeyValuePair<Feed,List<FeedEnclosure>> kvp in feedDict) {
                        kvp.Key.DecrementQueuedDownloadCount (kvp.Value.Count);
                    }
                }
            }                       
        }        

        private void TaskStatusChanged (TaskStatusChangedInfo statusInfo)
        {
            HttpFileDownloadTask task = statusInfo.Task as HttpFileDownloadTask;
            
            if (task == null) {
                return;
            }
            
            FeedEnclosure enc = task.UserState as FeedEnclosure;
            
            if (enc == null) {
                return;
            }
            
            //Console.WriteLine ("OnDownloadTaskStatusChangedHandler - old state:  {0}", statusInfo.OldStatus);                    
            //Console.WriteLine ("OnDownloadTaskStatusChangedHandler - new state:  {0}", statusInfo.NewStatus);                    
            switch (statusInfo.NewStatus) {
            case TaskStatus.Cancelled:
                enc.DownloadStatus = FeedDownloadStatus.None;
                break;
            case TaskStatus.Failed:
                enc.DownloadStatus = FeedDownloadStatus.DownloadFailed;
                break;
            case TaskStatus.Paused: 
                enc.DownloadStatus = FeedDownloadStatus.Paused;
                break;
            case TaskStatus.Ready:
                enc.DownloadStatus = FeedDownloadStatus.Pending;
                break;
            case TaskStatus.Running:
                enc.DownloadStatus = FeedDownloadStatus.Downloading;
                enc.Item.Feed.IncrementActiveDownloadCount ();                    
                break;  
            case TaskStatus.Stopped: goto case TaskStatus.Cancelled;
            }

            if (statusInfo.OldStatus == TaskStatus.Running) {
                enc.Item.Feed.DecrementActiveDownloadCount ();                    
            }
        }

        private void OnDownloadTaskStatusChangedHandler (object sender, 
                                                         TaskStatusChangedEventArgs e)
        {
            if (e.StatusChanged != null) {
                lock (sync) {
                    TaskStatusChanged (e.StatusChanged);
                }
            } else {
                lock (sync) {
                    foreach (TaskStatusChangedInfo statusInfo in e.StatusesChanged) {
                        TaskStatusChanged (statusInfo);                        
                    }
                }                
            }
        }
      
        private void TaskAssociatedHandler (object sender, 
                                            TaskEventArgs<FeedUpdateTask> e)
        {   
            lock (update_group.SyncRoot) {
                update_group.Execute ();
            }
        }        
        
        private void TaskStoppedHandler (object sender, 
                                         TaskEventArgs<FeedUpdateTask> e)
        {
            lock (sync) {
                FeedUpdateTask fut = e.Task as FeedUpdateTask;
                queued_feeds.Remove (fut.Feed);
                
                lock (update_list.SyncRoot) {
                    update_list.Remove (e.Task);
                }
            }
        }
        
        private void OnFeedItemEvent (EventHandler<FeedItemEventArgs> handler, 
                                      FeedItemEventArgs e)
        {
            if (handler == null) {
                return;
            } else if (e == null) {
                throw new ArgumentNullException ("e");
            }
            
            command_queue.Register (
                new EventWrapper<FeedItemEventArgs> (handler, this, e)
            );            
            
            //handler (this, e);           
        }        
        
        private void OnFeedEventRaised (Feed feed, EventHandler<FeedEventArgs> handler)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");	
            }
            
            EventHandler<FeedEventArgs> handlerCpy = handler;
            
            if (handlerCpy != null) {
                command_queue.Register (
                    new EventWrapper<FeedEventArgs> (
                        handler, this, new FeedEventArgs (feed)
                    )
                );              	
            	//handler (this, new FeedEventArgs (feed));
            }
        }  
        
        private void OnEnclosureDownloadCompleted (HttpFileDownloadTask task)
        {
            EventHandler<TaskEventArgs<HttpFileDownloadTask>> handler = EnclosureDownloadCompleted;
        
            if (handler != null) {
                AsyncCommandQueue<ICommand> cmdQCpy = command_queue;
                
                if (cmdQCpy != null) {
                    cmdQCpy.Register (new EventWrapper<TaskEventArgs<HttpFileDownloadTask>> (
                	    handler, this, new TaskEventArgs<HttpFileDownloadTask> (task))
                	);
                }        
            }                         
        }  

        /*private IEnumerable<HttpFileDownloadTask> FindDownloadTasks (IEnumerable<FeedEnclosure> enclosures)
        {            
            ICollection<HttpFileDownloadTask> encsCol = 
                enclosures as ICollection<HttpFileDownloadTask>;
            
            List<HttpFileDownloadTask> ret = (encsCol == null) ?
                new List<HttpFileDownloadTask> () : 
                new List<HttpFileDownloadTask> (encsCol.Count);
            
            HttpFileDownloadTask tmpTask = null;
            
            lock (sync) {
                foreach (FeedEnclosure enc in enclosures) {
                    tmpTask = FindDownloadTaskImpl ((FeedEnclosure)enc);
                    
                    if (tmpTask != null) {
                        ret.Add (tmpTask);
                    }
                }
            }
            
            return ret;
        }*/

#endregion

#region Internal Methods
        
        internal void CancelDownload (FeedEnclosure enc)
        {
            lock (sync) {
                HttpFileDownloadTask task = FindDownloadTask (enc);            
                        
                if (task != null) {
                    // Look into multi-cancel later      
                    task.CancelAsync ();
                }            
            }
        }
        
        internal void StopDownload (FeedEnclosure enc)
        {   
            lock (sync) {
                HttpFileDownloadTask task = FindDownloadTask (enc);            
                        
                if (task != null) {
                    task.Stop ();
                }   
            }
        }     
        
        internal void QueueUpdate (Feed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            }

            lock (sync) {
                if (disposed) {
                    return;
                }
                
                if (!queued_feeds.Contains (feed)) {
                    queued_feeds.Add (feed);
                    lock (update_list.SyncRoot) { 
                        update_list.Add (new FeedUpdateTask (feed));
                    }
                }            
            }             
        }
        
        internal void QueueUpdate (ICollection<Feed> feeds)
        {
            if (feeds == null) {
                throw new ArgumentNullException ("feeds");
            }
            
            lock (sync) {      
                if (disposed) {
                    return;
                }
                
                List<FeedUpdateTask> tasks = null;
                
                if (feeds.Count > 0) {
                    tasks = new List<FeedUpdateTask> (feeds.Count);
                        
                    foreach (Feed f in feeds) {
                        if (!queued_feeds.Contains (f)) {
                            queued_feeds.Add (f);
                            tasks.Add (new FeedUpdateTask (f));
                        }
                    }
                }
                
                if (tasks != null && tasks.Count > 0) {
                    lock (update_list.SyncRoot) {
                        update_list.AddRange (tasks);                  
                    }   
                }
            }
        }        
        
        // Should only be called by 'Feed'
        internal void RegisterCommand (ICommand command)
        {
             AsyncCommandQueue<ICommand> cmdQCpy = command_queue;
            
            if (cmdQCpy != null && command != null) {
            	cmdQCpy.Register (command);
            }
        }
        
        private void OnFeedAdded (Feed feed)
        {
            AsyncCommandQueue<ICommand> cmdQCpy = command_queue;
            
            if (cmdQCpy != null) {            
                cmdQCpy.Register (new CommandWrapper (delegate {
                    OnFeedEventRaised (feed, FeedAdded);
                }));
            }
        }
        
        internal void OnFeedDeleted (Feed feed)
        {      
            try {
                lock (sync) {                        
                    RemoveFeed (feed);
                }
            } finally {
                OnFeedEventRaised (feed, FeedDeleted);                
            }
        }
        
        internal void OnFeedDownloadCompleted (Feed feed, FeedDownloadError error)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");	
            }
            
            EventHandler<FeedDownloadCompletedEventArgs> handler = FeedDownloadCompleted;
                
            if (handler != null) {
                command_queue.Register (
                    new EventWrapper<FeedDownloadCompletedEventArgs> (
                        handler, this, 
                        new FeedDownloadCompletedEventArgs (feed, error)
                    )
                );    
            }          
        }

        internal void OnFeedDownloadCountChanged (Feed feed, FEEDS_EVENTS_DOWNLOAD_COUNT_FLAGS flags)
        {
            EventHandler<FeedDownloadCountChangedEventArgs> handler = FeedDownloadCountChanged;
                     
            if (handler != null) {             
                command_queue.Register (
                    new EventWrapper<FeedDownloadCountChangedEventArgs> (
                        handler, this, 
                        new FeedDownloadCountChangedEventArgs (feed, flags)
                    )
                );
            }
        }       
        
        internal void OnFeedDownloading (Feed feed)
        {
            OnFeedEventRaised (feed, FeedDownloading);
        }
        
        internal void OnFeedItemAdded (Feed feed, FeedItem item)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            } else if (item == null) {
                throw new ArgumentNullException ("item");
            }

            EventHandler<FeedItemEventArgs> handler = FeedItemAdded;            
            
            if (handler != null) {
                OnFeedItemEvent (handler, new FeedItemEventArgs (feed, item));
            }                           
        }
        
        internal void OnFeedItemsAdded (Feed feed, IEnumerable<FeedItem> items)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            } else if (items == null) {
                throw new ArgumentNullException ("items");
            } 

            EventHandler<FeedItemEventArgs> handler = FeedItemAdded;            
            
            if (handler != null) {
                OnFeedItemEvent (handler, new FeedItemEventArgs (feed, items));
            }               
        }        

        internal void OnFeedItemRemoved (Feed feed, FeedItem item)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            } else if (item == null) {
                throw new ArgumentNullException ("item");
            }

            EventHandler<FeedItemEventArgs> handler = FeedItemRemoved;            
            
            if (item.Enclosure != null) {
                lock (sync) {
                    HttpFileDownloadTask task;                
                         
                    if (queued_downloads.TryGetValue ((FeedEnclosure)item.Enclosure, out task)) {
                        task.CancelAsync ();
                    }
                }
            }            
            
            if (handler != null) {
                OnFeedItemEvent (handler, new FeedItemEventArgs (feed, item));
            }                 
        }
        
        internal void OnFeedItemsRemoved (Feed feed, IEnumerable<FeedItem> items)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");
            } else if (items == null) {
                throw new ArgumentNullException ("items");
            }
            
            EventHandler<FeedItemEventArgs> handler = FeedItemRemoved;

            lock (sync) {
                HttpFileDownloadTask task;  
                
                foreach (FeedItem item in items) {                
                    if (item.Enclosure != null) {                    
                        if (queued_downloads.TryGetValue ((FeedEnclosure)item.Enclosure, out task)) {
                            task.CancelAsync ();
                        }
                    }
                }
            }
            
            if (handler != null) {
                OnFeedItemEvent (handler, new FeedItemEventArgs (feed, items));
            }               
        }              
        
        internal void OnFeedItemCountChanged (Feed feed, FEEDS_EVENTS_ITEM_COUNT_FLAGS flags)
        {
            lock (sync) {
                if (feed == null) {
                    throw new ArgumentNullException ("feed");	
                }

                if ((FEEDS_EVENTS_ITEM_COUNT_FLAGS.FEICF_TOTAL_ITEM_COUNT_CHANGED | flags) != 0) {
                    ticDirty = true;
                } 
                    
                if ((FEEDS_EVENTS_ITEM_COUNT_FLAGS.FEICF_UNREAD_ITEM_COUNT_CHANGED | flags) != 0) {
                    tuicDirty = true;                       
                }
                
                EventHandler<FeedItemCountChangedEventArgs> handler = FeedItemCountChanged;                
                
                if (handler != null) {
                    command_queue.Register (
                        new EventWrapper<FeedItemCountChangedEventArgs> (
                            handler, this, 
                            new FeedItemCountChangedEventArgs (feed, flags)
                        )
                    );       
                }
            }    
        }                
                
        internal void OnFeedRenamed (Feed feed)
        {
            OnFeedEventRaised (feed, FeedRenamed);
        }
        
        internal void UpdateFeedUrl (string oldUrl, Feed feed)
        {
            lock (sync) {
                url_feed_map.Remove (oldUrl);
                url_feed_map.Add (feed.Url, feed);
            }        
        }
        
        internal void OnFeedUrlChanged (Feed feed)
        {
            OnFeedEventRaised (feed, FeedUrlChanged);
        }
        
#endregion 
    }   
}    
