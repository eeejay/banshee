//
// FeedUpdater.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Migo.Net;
using Migo.TaskCore;
using Migo.TaskCore.Collections;

namespace Migo.Syndication
{
    public class FeedManager
    {
        private bool disposed;
        private Dictionary<Feed, FeedUpdateTask> update_feed_map;
        private TaskList<FeedUpdateTask> update_task_list;
        private TaskGroup<FeedUpdateTask> update_task_group;
        
#region Public Properties and Events

        public event Action<FeedItem> ItemAdded;
        public event Action<FeedItem> ItemChanged;
        public event Action<FeedItem> ItemRemoved;
        public event EventHandler FeedsChanged;

#endregion
        
#region Constructor
        
        public FeedManager ()
        {
            update_feed_map = new Dictionary<Feed, FeedUpdateTask> ();
            update_task_list = new TaskList<FeedUpdateTask> ();
            
            // Limit to 4 feeds downloading at a time
            update_task_group = new TaskGroup<FeedUpdateTask> (4, update_task_list);
            
            update_task_group.TaskStopped += OnUpdateTaskStopped;
            update_task_group.TaskAssociated += OnUpdateTaskAdded;
            
            // TODO
            // Start timeout to refresh feeds every so often
        }

#endregion
        
#region Public Methods

        public bool IsUpdating (Feed feed)
        {
            return update_feed_map.ContainsKey (feed);
        }

        public Feed CreateFeed (string url, FeedAutoDownload autoDownload)
        {
            Feed feed = null;
            url = url.Trim ().TrimEnd ('/');

            if (!Feed.Exists (url)) {
                feed = new Feed (url, autoDownload);
                feed.Save ();
                feed.Update ();
            }
            
            return feed;
        }

        public void QueueUpdate (Feed feed)
        {
            lock (update_task_group.SyncRoot) {
                if (disposed) {
                    return;
                }
                
                if (!update_feed_map.ContainsKey (feed)) {
                    FeedUpdateTask task = new FeedUpdateTask (feed);
                    update_feed_map[feed] = task;
                    lock (update_task_list.SyncRoot) { 
                        update_task_list.Add (task);
                    }
                }
            }        
        }
        
        public void CancelUpdate (Feed feed)
        {
            lock (update_task_group.SyncRoot) {
                if (update_feed_map.ContainsKey (feed)) {
                    update_feed_map[feed].CancelAsync ();
                }
            }
        }
        
        public void Dispose (System.Threading.AutoResetEvent disposeHandle)
        {
            lock (update_task_group.SyncRoot) {
                if (update_task_group != null) {
                    update_task_group.CancelAsync ();
                    update_task_group.Handle.WaitOne ();
                    update_task_group.Dispose (disposeHandle);
                    disposeHandle.WaitOne ();
                    
                    update_task_group.TaskStopped -= OnUpdateTaskStopped;
                    update_task_group.TaskAssociated -= OnUpdateTaskAdded;
                    update_task_group = null;
                }
               
                update_task_list = null;
                disposed = true;
            }
        }        
                
#endregion

#region Internal Methods

        internal void OnFeedsChanged ()
        {
            EventHandler handler = FeedsChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        internal void OnItemAdded (FeedItem item)
        {
            Action<FeedItem> handler = ItemAdded;
            if (handler != null) {
                handler (item);
            }
        }
        
        internal void OnItemChanged (FeedItem item)
        {
            Action<FeedItem> handler = ItemChanged;
            if (handler != null) {
                handler (item);
            }
        }
        
        internal void OnItemRemoved (FeedItem item)
        {
            Action<FeedItem> handler = ItemRemoved;
            if (handler != null) {
                handler (item);
            }
        }

#endregion

#region Private Methods

        private void OnUpdateTaskAdded (object sender, TaskEventArgs<FeedUpdateTask> e)
        {   
            lock (update_task_group.SyncRoot) {
                update_task_group.Execute ();
            }
        }

        private void OnUpdateTaskStopped (object sender, TaskEventArgs<FeedUpdateTask> e)
        {
            lock (update_task_group.SyncRoot) {
                FeedUpdateTask fut = e.Task as FeedUpdateTask;
                update_feed_map.Remove (fut.Feed);
                
                lock (update_task_list.SyncRoot) {
                    update_task_list.Remove (e.Task);
                }
            }
        }
        
#endregion

    }
}
