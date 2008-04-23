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

using Migo.DownloadCore;

using Migo.TaskCore;
using Migo.TaskCore.Collections;

using Migo.Syndication.Data;

namespace Migo.Syndication
{
    public class FeedsManager : IFeedsManager, IDisposable
    {        
        private bool disposed;        
        
        private bool ticDirty;
        private long totalItemCount;
        
        private bool tuicDirty;
        private long totalUnreadItemCount;
        
        private List<Feed> feeds;
        private List<Feed> queuedFeeds;

        private IDbConnection conn;
        private AsyncCommandQueue<ICommand> commandQueue;
        
        private Dictionary<long,Feed> idFeedDict;
        private Dictionary<string,Feed> urlFeedDict;
        
        private Dictionary<FeedEnclosure,HttpFileDownloadTask> queuedDownloads;
        
        private ManualResetEvent downloadHandle;
        private DownloadManager downloadManager;
        
        private TaskList<FeedUpdateTask> updateList;
        private TaskGroup<FeedUpdateTask> updateGroup;
        
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
        
        public FeedBackgroundSyncStatus BackgroundSyncStatus 
        {
            get {
                lock (sync) {
                    return FeedBackgroundSyncStatus.Disabled;
                }
            }
        }        
        
        public long DefaultInterval 
        {
            get { lock (sync) { return 15; } }
            set { throw new NotImplementedException ("DefaultInterval"); }
        }

        public DownloadManager DownloadManager
        {
            get { return downloadManager; }
        }
        
        public ReadOnlyCollection<IFeed> Feeds 
        {             
            get { 
                lock (sync) {
                    // My god, would you look at that abortion.  No wonder 
                    // they wrapped LINQ around it.
                    // UPDATE to use LINQ ASAP.
                    return feeds.ConvertAll (
                        new Converter<Feed,IFeed> (delegate (Feed f) { return f as IFeed; })
                    ).AsReadOnly ();
                }
            } 
        }

        public long ItemCountLimit 
        {
            get { lock (sync) { return -1; } }
        }        
        
        public long TotalItemCount
        { 
            get {
                lock (sync) {
                    if (ticDirty) {
                    	totalItemCount = 0;
                        
                        foreach (IFeed f in feeds) {
                            totalItemCount += f.ItemCount;
                    	}
                        
                        ticDirty = false;                        
                    }
                    
                    return totalItemCount;
                }
            }
        }
                
        public long TotalUnreadItemCount 
        { 
            get {        
                lock (sync) {
                    if (tuicDirty) {
                    	totalUnreadItemCount = 0;
                        
                        foreach (IFeed f in feeds) {
                            totalUnreadItemCount += f.UnreadItemCount;
                    	}
                        
                        tuicDirty = false;
                    }
                    
                    return totalUnreadItemCount;
                }
            }
        }
        
        internal AsyncCommandQueue<ICommand> CommandQueue {
            get { return commandQueue; }
        }
        
        public FeedsManager (string dbPath, DownloadManager manager)
        {
            if (String.IsNullOrEmpty (dbPath)) {
                throw new ArgumentException ("dbPath is null or empty.");
            } else if (manager == null) {
                throw new ArgumentNullException ("manager");
            }
                        
            try {
                conn = SQLiteUtility.GetNewConnection (dbPath);
                conn.Open ();
                
                DatabaseManager.Init (conn);
                
                EnclosuresTableManager.Init ();
                ItemsTableManager.Init ();
                FeedsTableManager.Init ();                

                feeds = new List<Feed> ();
                queuedFeeds = new List<Feed> ();
                
                idFeedDict = new Dictionary<long,Feed> ();
                urlFeedDict = new Dictionary<string,Feed> ();
                
                ticDirty = tuicDirty = true;  

                downloadHandle = new ManualResetEvent (true);
                
                downloadManager = manager;

                downloadManager.Tasks.TaskAdded += OnDownloadTaskAdded;
                downloadManager.Tasks.TaskRemoved += OnDownloadTaskRemoved;
                
                downloadManager.Group.TaskStatusChanged += OnDownloadTaskStatusChangedHandler;

                queuedDownloads = new Dictionary<FeedEnclosure,HttpFileDownloadTask> ();
                
                updateList = new TaskList<FeedUpdateTask> ();
                updateGroup = new TaskGroup<FeedUpdateTask> (4, updateList);
                
                updateGroup.TaskStopped += TaskStoppedHandler;
                updateGroup.TaskAssociated += TaskAssociatedHandler;
                
                foreach (Feed f in FeedsTableManager.GetAllFeeds (this)) {
                    Associate (f);
                }
                
                commandQueue = new AsyncCommandQueue<ICommand> ();
            } catch (Exception e) {
                Console.WriteLine (e.Message);
                Console.WriteLine (e.StackTrace);
                throw new ApplicationException ("Unable to initialize FeedsManager");   
            } 
            Console.WriteLine ("FM - CON - END");
        }
        
        public void AsyncSyncAll ()
        {
            List<Feed> allFeeds = null;
            
            lock (sync) {
                if (feeds.Count > 0) {
                    allFeeds = new List<Feed> (this.feeds);
                }
            }

            if (allFeeds != null) {
                foreach (IFeed f in allFeeds) {
                    f.AsyncDownload ();
                }
            }
        }
        
        public void BackgroundSync (FeedBackgroundSyncAction action)
        {
            throw new NotImplementedException ("BackgroundSync");
        }
                
        public IFeed CreateFeed (string url)
        {
            Feed feed = null;
            string url_ = url.Trim ().TrimEnd ('/');

            lock (sync) {   
                if (!urlFeedDict.ContainsKey (url_)) {
                    feed = new Feed (this, url_);
                    feed.Commit ();
                    Associate (feed);
                    OnFeedAdded (feed);
                }
            }
            
            return feed;
        }
        
        public IFeed CreateFeed (string feedName, string feedUrl)
        {
            return null;   
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
          
        public void DeleteFeed (IFeed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");      	
            } else if (!IsCuckoosEgg (feed)) {
                feed.Delete ();
            }
        }
/*        
        public void DequeueDownloads (IEnumerable<IFeedEnclosure> enclosures) 
        {
            if (enclosures == null) {
                throw new ArgumentNullException ("enclosures");
            }
            
            IEnumerable<HttpFileDownloadTask> tasks =   
                FindDownloadTasks (enclosures);
                
            downloadManager.RemoveDownload (tasks);
        }
*/
        public void Dispose () 
        {
            if (SetDisposed ()) {               
                AutoResetEvent disposeHandle = new AutoResetEvent (false);
                Console.WriteLine ("FM - Dispose - 000");
    
                List<HttpFileDownloadTask> tasks = null;
    
                lock (sync) {
                    if (queuedDownloads.Count > 0) {
                        tasks = new List<HttpFileDownloadTask> (queuedDownloads.Values);
                    }
                }

                if (tasks != null) {
                    foreach (HttpFileDownloadTask t in tasks) {
                        t.Stop ();
                    }
                    
                    Console.WriteLine ("downloadHandle - WaitOne ()");
                    downloadHandle.WaitOne ();
                }

                if (updateGroup != null) {
                    updateGroup.CancelAsync ();                
                    
                    Console.WriteLine ("FM - Dispose - 000.5");
                    
                    updateGroup.Handle.WaitOne ();
                    
                    Console.WriteLine ("FM - Dispose - 001");
                    
                    updateGroup.Dispose (disposeHandle);
                    
                    Console.WriteLine ("FM - Dispose - 002");
                    
                    disposeHandle.WaitOne ();
                    
                    updateGroup.TaskStopped -= TaskStoppedHandler;
                    updateGroup.TaskAssociated -= TaskAssociatedHandler;
                    
                    updateGroup = null;
                    
                    Console.WriteLine ("FM - Dispose - 003");
                }
               
                updateList = null;
                
                Console.WriteLine ("FM - Dispose - 008");    
                                 
                if (downloadHandle != null) {
                    downloadHandle.Close ();
                    downloadHandle = null;
                }                
                
                Console.WriteLine ("FM - Dispose - 007");                    
                
                if (downloadManager != null) {
                    downloadManager.Tasks.TaskAdded -= OnDownloadTaskAdded;
                    downloadManager.Tasks.TaskRemoved -= OnDownloadTaskRemoved; 
                    downloadManager = null;
                }                          

                Console.WriteLine ("FM - Dispose - 004");                  

                if (commandQueue != null) {
                    commandQueue.Dispose ();
                    commandQueue = null;
                }
                
                Console.WriteLine ("FM - Dispose - 005");
                
                DatabaseManager.Dispose ();
                
                if (conn != null) {
                    conn.Close ();
                    conn = null;
                }              
                
                Console.WriteLine ("FM - Dispose - 006");                    
                
                disposeHandle.Close ();
            }
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
        
        public IFeed GetFeed (long feedID)
        {
            Feed ret = null;
            
            lock (sync) {
                idFeedDict.TryGetValue (feedID, out ret);
            }
            
            return ret;
        }
            
        public IFeed GetFeedByUrl (string url)
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
        }

        private void Associate (Feed feed)
        {
            if (feed != null && !idFeedDict.ContainsKey (feed.LocalID)) {
                idFeedDict.Add (feed.LocalID, feed);
                urlFeedDict.Add (feed.Url, feed);
                feeds.Add (feed);                
            }                 
        }

        private void Disassociate (Feed feed)
        {
            if (feed != null &&
                feeds.Remove (feed)) {				
                urlFeedDict.Remove (feed.Url);            
                idFeedDict.Remove (feed.LocalID);                
            }
        }           
        
        private bool IsCuckoosEgg (IFeed feed)
        {
            bool ret = true;
            Feed f = feed as Feed;
            
            if (f != null && f.Parent == this) {
                ret = false;
            }
            
            return ret;
        }
        
        internal void CancelDownload (IFeedEnclosure enc)
        {
            lock (sync) {
                HttpFileDownloadTask task = FindDownloadTask (enc);            
                        
                if (task != null) {
                    // Look into multi-cancel later      
                    task.CancelAsync ();
                }            
            }
        }
        
        internal void StopDownload (IFeedEnclosure enc)
        {   
            lock (sync) {
                HttpFileDownloadTask task = FindDownloadTask (enc);            
                        
                if (task != null) {
                    task.Stop ();
                }   
            }
        }        
        
        private HttpFileDownloadTask FindDownloadTask (IFeedEnclosure enc)
        {
            if (enc == null) {
                throw new ArgumentNullException ("enc");
            }
            
            return FindDownloadTaskImpl ((FeedEnclosure)enc);
        }
        
        private HttpFileDownloadTask FindDownloadTaskImpl (FeedEnclosure enc) 
        {
            HttpFileDownloadTask task = null;
            Feed parentFeed = enc.Parent.Parent as Feed;                               
            
            if (parentFeed != null && 
                !IsCuckoosEgg (parentFeed) && 
                queuedDownloads.ContainsKey (enc)) {
                task = queuedDownloads[enc];
            }
            
            return task;
        }        
/*        
        private IEnumerable<HttpFileDownloadTask> FindDownloadTasks (IEnumerable<IFeedEnclosure> enclosures)
        {            
            ICollection<HttpFileDownloadTask> encsCol = 
                enclosures as ICollection<HttpFileDownloadTask>;
            
            List<HttpFileDownloadTask> ret = (encsCol == null) ?
                new List<HttpFileDownloadTask> () : 
                new List<HttpFileDownloadTask> (encsCol.Count);
            
            HttpFileDownloadTask tmpTask = null;
            
            lock (sync) {
                foreach (IFeedEnclosure enc in enclosures) {
                    tmpTask = FindDownloadTaskImpl ((FeedEnclosure)enc);
                    
                    if (tmpTask != null) {
                        ret.Add (tmpTask);
                    }
                }
            }
            
            return ret;
        }
*/
        public HttpFileDownloadTask QueueDownload (IFeedEnclosure enc) 
        {
            return QueueDownload (enc, true);         
        }
           
        public HttpFileDownloadTask QueueDownload (IFeedEnclosure enc, bool queue)
        {
            if (enc == null) {
                throw new ArgumentNullException ("enc");
            }
         
            FeedEnclosure fenc = enc as FeedEnclosure;
            
            if (fenc == null) {
                throw new ArgumentException ("Must be derived from FeedEnclosure", "enc");
            }
            
            HttpFileDownloadTask task = null;       
            
            lock (sync) {
                if (disposed) {
                    return null;
                }
                
                if (!queuedDownloads.ContainsKey (fenc)) {                    
                    Feed parentFeed = enc.Parent.Parent as Feed;                    
                    
                    if (parentFeed != null && !IsCuckoosEgg (parentFeed)) {                        
                        task = downloadManager.CreateDownloadTask (fenc.Url, fenc);
                        //Console.WriteLine ("Task DL path:  {0}", task.LocalPath);
                        task.Name = String.Format (
                            "{0} - {1}", parentFeed.Title, fenc.Parent.Title
                        );
                        
                        task.Completed += OnDownloadTaskCompletedHandler;                    
                        
                        // Race condition...
                        // Should only be added when the task is associated or 
                        // it can be canceled before it is added to the progress manager.
                        
                        // Add a pre-association dict and move tasks to the 
                        // queued dict once they've been offically added.
                        
                        queuedDownloads.Add (fenc, task);
                    }                    
                }

                if (task != null && queue) {
                    downloadManager.QueueDownload (task);                                                                    	
                }      
            }
                       
            return task;        
        }
        
        public IEnumerable<HttpFileDownloadTask> QueueDownloads (IEnumerable<IFeedEnclosure> encs)
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
                    downloadManager.QueueDownload (tasks);
                }
            }
            
            return tasks;
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
                
                if (!queuedFeeds.Contains (feed)) {
                    queuedFeeds.Add (feed);
                    lock (updateList.SyncRoot) { 
                        updateList.Add (new FeedUpdateTask (feed));
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
                        if (!queuedFeeds.Contains (f)) {
                            queuedFeeds.Add (f);
                            tasks.Add (new FeedUpdateTask (f));
                        }
                    }
                }
                
                if (tasks != null && tasks.Count > 0) {
                    lock (updateList.SyncRoot) {
                        updateList.AddRange (tasks);                  
                    }   
                }
            }
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
                    parentFeed = enc.Parent.Parent as Feed;                  
                    
                    if (parentFeed != null && !IsCuckoosEgg (parentFeed) &&
                        queuedDownloads.ContainsKey (enc)) {
                        if (queuedDownloads.Count == 0) {
                            downloadHandle.Reset ();
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
            if (queuedDownloads.ContainsKey (enc)) {
                queuedDownloads.Remove (enc);    
                task.Completed -= OnDownloadTaskCompletedHandler;                                            
                
                if (decQueuedCount) {
                    ((Feed)enc.Parent.Parent).DecrementQueuedDownloadCount ();
                }
                
                if (queuedDownloads.Count == 0) {
                	if (downloadHandle != null) {
                	    downloadHandle.Set ();                	    
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
                            tmpParent = ((Feed)tmpEnclosure.Parent.Parent);
                            
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
                ((Feed)enc.Parent.Parent).IncrementActiveDownloadCount ();                    
                break;  
            case TaskStatus.Stopped: goto case TaskStatus.Cancelled;
            }

            if (statusInfo.OldStatus == TaskStatus.Running) {
                ((Feed)enc.Parent.Parent).DecrementActiveDownloadCount ();                    
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
            lock (updateGroup.SyncRoot) {
                updateGroup.Execute ();
            }
        }        
        
        private void TaskStoppedHandler (object sender, 
                                         TaskEventArgs<FeedUpdateTask> e)
        {
            lock (sync) {
                FeedUpdateTask fut = e.Task as FeedUpdateTask;
                queuedFeeds.Remove (fut.Feed);
                
                lock (updateList.SyncRoot) {
                    updateList.Remove (e.Task);
                }
            }
        }
        
        // Should only be called by 'Feed'
        internal void RegisterCommand (ICommand command)
        {
             AsyncCommandQueue<ICommand> cmdQCpy = commandQueue;
            
            if (cmdQCpy != null && command != null) {
            	cmdQCpy.Register (command);
            }
        }
        
        private void OnFeedAdded (Feed feed)
        {
            AsyncCommandQueue<ICommand> cmdQCpy = commandQueue;
            
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
                    Disassociate (feed);
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
                commandQueue.Register (
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
                commandQueue.Register (
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
        
        internal void OnFeedItemAdded (IFeed feed, IFeedItem item)
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
        
        internal void OnFeedItemsAdded (IFeed feed, IEnumerable<IFeedItem> items)
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

        internal void OnFeedItemRemoved (IFeed feed, IFeedItem item)
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
                         
                    if (queuedDownloads.TryGetValue ((FeedEnclosure)item.Enclosure, out task)) {
                        task.CancelAsync ();
                    }
                }
            }            
            
            if (handler != null) {
                OnFeedItemEvent (handler, new FeedItemEventArgs (feed, item));
            }                 
        }
        
        internal void OnFeedItemsRemoved (IFeed feed, IEnumerable<IFeedItem> items)
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
                        if (queuedDownloads.TryGetValue ((FeedEnclosure)item.Enclosure, out task)) {
                            task.CancelAsync ();
                        }
                    }
                }
            }
            
            if (handler != null) {
                OnFeedItemEvent (handler, new FeedItemEventArgs (feed, items));
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
            
            commandQueue.Register (
                new EventWrapper<FeedItemEventArgs> (handler, this, e)
            );            
            
            //handler (this, e);           
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
                    commandQueue.Register (
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
                urlFeedDict.Remove (oldUrl);
                urlFeedDict.Add (feed.Url, feed);
            }        
        }
        
        internal void OnFeedUrlChanged (Feed feed)
        {
            OnFeedEventRaised (feed, FeedUrlChanged);
        }

        private void OnFeedEventRaised (Feed feed, EventHandler<FeedEventArgs> handler)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");	
            }
            
            EventHandler<FeedEventArgs> handlerCpy = handler;
            
            if (handlerCpy != null) {
                commandQueue.Register (
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
                AsyncCommandQueue<ICommand> cmdQCpy = commandQueue;
                
                if (cmdQCpy != null) {
                    cmdQCpy.Register (new EventWrapper<TaskEventArgs<HttpFileDownloadTask>> (
                	    handler, this, new TaskEventArgs<HttpFileDownloadTask> (task))
                	);
                }        
            }                         
        }        
    }   
}    
