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

namespace Migo.Syndication
{
    public class FeedsManager : IDisposable
    {        
        private bool disposed;

        private AsyncCommandQueue command_queue;
        
        private FeedManager feed_manager;
        private EnclosureManager enclosure_manager;
        
        private readonly object sync = new object (); 
        
        /*public event EventHandler<FeedEventArgs> FeedAdded;
        public event EventHandler<FeedEventArgs> FeedDeleted;
        public event EventHandler<FeedItemCountChangedEventArgs> FeedItemCountChanged;
        public event EventHandler<FeedEventArgs> FeedRenamed;
        public event EventHandler<FeedEventArgs> FeedUrlChanged;        
        
        public event EventHandler<FeedItemEventArgs> FeedItemAdded;
        public event EventHandler<FeedItemEventArgs> FeedItemRemoved;*/
        
        public static FeedsManager Instance;
        
        internal AsyncCommandQueue CommandQueue {
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
        }
        
        /*private ReadOnlyCollection<Feed> ro_feeds;
        public ReadOnlyCollection<Feed> Feeds {             
            get { 
                lock (sync) {
                    return ro_feeds ?? ro_feeds = new ReadOnlyCollection<Feed> (feeds);
                }
            }
        }*/
        
        public FeedManager FeedManager {
            get { return feed_manager; }
        }
        
        public EnclosureManager EnclosureManager {
            get { return enclosure_manager; }
        }
        
        private HyenaSqliteConnection connection;
        public HyenaSqliteConnection Connection {
            get { return connection; }
        }
        
        private string podcast_base_dir;
        public string PodcastStorageDirectory {
            get { return podcast_base_dir; }
            set { podcast_base_dir = value; }
        }

#endregion

#region Constructor
        
        public FeedsManager (HyenaSqliteConnection connection, DownloadManager downloadManager, string podcast_base_dir)
        {            
            // Hack to work around Feeds being needy and having to call all our internal methods, instead
            // of us just listening for their events.
            Instance = this;
            this.connection = connection;
            this.podcast_base_dir = podcast_base_dir;

            feed_manager = new FeedManager ();
            enclosure_manager = new EnclosureManager (downloadManager);
            
            Feed.Init ();
            FeedItem.Init ();
            FeedEnclosure.Init ();
            
            command_queue = new AsyncCommandQueue ();
        }
        
#endregion

#region Public Methods

        public void Dispose () 
        {
            if (SetDisposed ()) {               
                AutoResetEvent disposeHandle = new AutoResetEvent (false);

                feed_manager.Dispose (disposeHandle);
                enclosure_manager.Dispose (disposeHandle);

                if (command_queue != null) {
                    command_queue.Dispose ();
                    command_queue = null;
                }
                
                disposeHandle.Close ();
            }
        }

#endregion

#region Private Methods
        


        /*private void OnFeedItemEvent (EventHandler<FeedItemEventArgs> handler, 
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
        }  */
        
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

        // Should only be called by 'Feed'
/*        internal void RegisterCommand (ICommand command)
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
            OnFeedEventRaised (feed, FeedDeleted);
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

        internal void OnFeedItemCountChanged (Feed feed, FEEDS_EVENTS_ITEM_COUNT_FLAGS flags)
        {
            lock (sync) {
                if (feed == null) {
                    throw new ArgumentNullException ("feed");	
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
        
        internal void OnFeedUrlChanged (Feed feed)
        {
            OnFeedEventRaised (feed, FeedUrlChanged);
        }*/
        
#endregion 
    }   
}    
