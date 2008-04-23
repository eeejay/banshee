/***************************************************************************
 *  PodcastCore.cs
 *
 *  Copyright (C) 2008 Michael C. Urbanski
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.ServiceStack;

using Migo.TaskCore;
using Migo.Syndication;
using Migo.DownloadCore;

using Banshee.Podcasting.Gui;
using Banshee.Podcasting.Data;
using Banshee.Collection.Database;

namespace Banshee.Podcasting
{
    public partial class PodcastCore : IDisposable
    {  
        private readonly string tmpDownloadPath;
        private string tmpEnclosurePath; 
            
        private bool disposed;
        
        private DownloadManager dm;
        private DownloadManagerInterface dmInterface;
        
        private FeedsManager feedsManager;
        
        private PodcastFeedModel feedModel;
        private PodcastItemModel itemModel;
        
        private PodcastSource podcastSource;
        private PodcastImportManager importManager;
        
        private Dictionary<long,PodcastItem> itemDict;
        private Dictionary<long,PodcastFeed> feedDict;
        
        private readonly object sync = new object ();

        public PodcastCore ()
        {
            tmpEnclosurePath = Path.Combine (Paths.LibraryLocation, "Podcasts"); 
            tmpDownloadPath = Path.Combine (Paths.ApplicationData, "downloads");
            
            dm = new DownloadManager (2, tmpDownloadPath);  

            dmInterface = new DownloadManagerInterface (dm);
            dmInterface.Initialize ();    
    
            feedsManager = new FeedsManager (
                Path.Combine (Paths.ApplicationData, "syndication.db"), dm
            );

// All of these need to be moved into the FeedsManager as part of the larger 
// refactoring effort.
            dm.Group.TaskAssociated += OnTaskAssociated;                        
            dm.Group.TaskStatusChanged += OnTaskStatusChanged;
            
//            dm.Group.TaskStopped += OnTaskStoppedHandler;                
            dm.Group.TaskStarted += TaskStartedHandler;   
//
            feedsManager.FeedAdded += OnFeedAddedHandler;
            feedsManager.FeedDeleted += OnFeedRemovedHandler;
            feedsManager.FeedRenamed += OnFeedRenamedHandler;
            feedsManager.FeedUrlChanged += OnFeedRenamedHandler;
            
            feedsManager.FeedItemAdded += OnFeedItemAddedHandler;
            feedsManager.FeedItemRemoved += OnFeedItemRemovedHandler;
            feedsManager.FeedItemCountChanged += OnFeedItemCountChanged;
            
            feedsManager.FeedDownloading += OnFeedUpdatingHandler;
            feedsManager.FeedDownloadCompleted += OnFeedDownloadCompletedHandler;
            feedsManager.FeedDownloadCountChanged += OnFeedDownloadCountChangedHandler;            
            
            feedsManager.EnclosureDownloadCompleted += OnTaskStoppedHandler;
              
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChangedHandler;
     
            feedModel = new PodcastFeedModel ();
            itemModel = new PodcastItemModel ();

            List<IFeedItem> enclosureItems = null;
            List<PodcastItem> podcastItems = new List<PodcastItem> ();
            
            feedDict = new Dictionary<long,PodcastFeed> ();
            itemDict = new Dictionary<long,PodcastItem> ();

            IEnumerable<PodcastItem> pis = PodcastItem.Provider.FetchAll ();
            IEnumerable<PodcastFeed> feeds = PodcastFeed.Provider.FetchAll ();
/*
            //Dictionary<int,DatabaseTrackInfo> tracks = new Dictionary<int,DatabaseTrackInfo> ();                                
            ServiceManager.DbConnection.BeginTransaction ();
            
            try {
                foreach (PodcastItem p in pis) {
                    if (p.TrackID != 0) {
                        //p.Track = DatabaseTrackInfo.Provider.FetchSingle (p.TrackID);
                        //tracks.Add (p.TrackID, p.Track);                            
                    }
                }
                
                ServiceManager.DbConnection.CommitTransaction ();
            } catch {
                ServiceManager.DbConnection.RollbackTransaction ();
                throw;
            }
*/            
            foreach (PodcastFeed feed in feeds) {
                feedDict.Add (feed.FeedID, feed);                    
            }         
     
            foreach (PodcastItem item in pis) {
                itemDict.Add (item.FeedItemID, item);                    
            }
                
            PodcastFeed f = null;
            PodcastItem pi = null;
            
            foreach (IFeed feed in feedsManager.Feeds) {
                if (feedDict.ContainsKey (feed.LocalID)) {
                    f = feedDict[feed.LocalID];
                    f.Feed = feed;
                    
                    feedModel.Add (f);
                    enclosureItems = GetEnclosureItems (feed.Items);
        
                    if (enclosureItems.Count > 0) {
                        foreach (IFeedItem fi in enclosureItems) {
                            if (itemDict.ContainsKey (fi.LocalID)) {
                                pi = itemDict[fi.LocalID];
                                pi.Item = fi;
                                pi.Feed = feedDict[fi.Parent.LocalID];
                                podcastItems.Add (pi);
                            }
                        }                    
                    }                        
                }
            }
            
            itemModel.Add (podcastItems);
            InitializeInterface ();

            importManager = new PodcastImportManager (podcastSource);

            UpdateCount ();
        }
        
        bool disposing;
             
        public void Dispose ()
        {                        
            lock (sync) {
                if (disposing | disposed) {
                    return;
                } else {
                    disposing = true;               
                }
            }
            
            ServiceManager.PlayerEngine.StateChanged -= OnPlayerEngineStateChangedHandler;

            Log.Debug ("Disposing dmInterface");    
            if (dmInterface != null) {
                dmInterface.Dispose ();                
                dmInterface = null;
            }                
           
            Log.Debug ("Disposing feedsManager");
            if (feedsManager != null) {   
                feedsManager.Dispose ();

                feedsManager.FeedAdded -= OnFeedAddedHandler;       
                feedsManager.FeedDeleted -= OnFeedRemovedHandler;
                feedsManager.FeedRenamed -= OnFeedRenamedHandler;
                feedsManager.FeedUrlChanged -= OnFeedRenamedHandler;
                                
                feedsManager.FeedItemAdded -= OnFeedItemAddedHandler;
                feedsManager.FeedItemRemoved -= OnFeedItemRemovedHandler;
                feedsManager.FeedItemCountChanged -= OnFeedItemCountChanged;                
                
                feedsManager.FeedDownloading -= OnFeedUpdatingHandler;
                feedsManager.FeedDownloadCompleted -= OnFeedDownloadCompletedHandler;
                feedsManager.FeedDownloadCountChanged -= OnFeedDownloadCountChangedHandler;

                feedsManager = null;
            }
            
            Log.Debug ("Disposing dm");
            if (dm != null) {            
                dm.Dispose ();
            
                dm.Group.TaskAssociated -= OnTaskAssociated;                        
                dm.Group.TaskStatusChanged -= OnTaskStatusChanged;
                
                dm.Group.TaskStopped -= OnTaskStoppedHandler;                
                dm.Group.TaskStarted -= TaskStartedHandler;                   
                dm = null;
            }
            
            DisposeInterface ();            
            
            lock (sync) {
                disposing = false;            
                disposed = true;
            }
        }    

        private void UpdateCount ()
        {
            lock (sync) {
                if (!disposed || disposing) {
                    long itemCount = feedsManager.TotalItemCount;
                    long downloadedItems = itemCount - feedsManager.TotalUnreadItemCount;                  

                    int count = Math.Min (
                        (int)itemCount,
                        ((downloadedItems > 0) ? (int)downloadedItems : 0)
                    );
                    
                    podcastSource.CountSet = count;                    
                }
            }
        }

        private void OnFeedAddedHandler (object sender, FeedEventArgs e)
        {
            lock (sync) {
                if (feedDict.ContainsKey (e.Feed.LocalID)) {
                    PodcastFeed feed = feedDict[e.Feed.LocalID];
                    feedModel.Add (feed);
                    e.Feed.AsyncDownload ();                        
                }
            }
        }
        
        private void OnFeedRemovedHandler (object sender, FeedEventArgs e)
        {
            lock (sync) {
                if (feedDict.ContainsKey (e.Feed.LocalID)) {
                    PodcastFeed feed = feedDict[e.Feed.LocalID];
                    feedDict.Remove (feed.FeedID);
                    feedModel.Remove (feed);
                    feed.Delete ();
                }
            }
        }        

        private void OnFeedRenamedHandler (object sender, FeedEventArgs e)
        {
            lock (sync) {
                feedModel.Sort ();
            }
        }

        private void OnFeedUpdatingHandler (object sender, FeedEventArgs e)
        {   
            lock (sync) {
                feedModel.Reload ();
            }
        }

        private void OnFeedDownloadCountChangedHandler (object sender, 
                                                        FeedDownloadCountChangedEventArgs e)
        {
            lock (sync) {
                feedModel.Reload ();                
            }
        }

        private void OnFeedItemAddedHandler (object sender, FeedItemEventArgs e) 
        {
             lock (sync) {
                if (e.Item != null) {
                    if (e.Item.Enclosure != null) {
                        PodcastItem pi = new PodcastItem (e.Item);
                        pi.Feed = feedDict[e.Item.Parent.LocalID];                        
                        itemDict.Add (e.Item.LocalID, pi);
                        pi.Save ();
                        
                        itemModel.Add (pi);                            
                    } else {
                        e.Item.Delete ();                        
                    }
                } else if (e.Items != null) {
                    IEnumerable<IFeedItem> items = e.Items;
                    
                    PodcastItem pi = null;                            
                    List<PodcastItem> pis = new List<PodcastItem> ();

                    ServiceManager.DbConnection.BeginTransaction ();

                    try {      
                        foreach (IFeedItem fi in items) {
                            if (fi.Enclosure == null) {
                    	    	fi.Delete ();
                    	    } else {
                                pi = new PodcastItem (fi);
                                pi.Feed = feedDict[fi.Parent.LocalID];                                    
                                itemDict.Add (fi.LocalID, pi);
                                pis.Add (pi);
                                pi.Save ();
                            }
                        }
                        
                        itemModel.Add (pis);
                        ServiceManager.DbConnection.CommitTransaction ();
                    } catch (Exception ex) {
                        Console.WriteLine (ex.Message);
                        ServiceManager.DbConnection.RollbackTransaction ();
                        return;
                    }
                }
            }
        }

        private void OnFeedItemRemovedHandler (object sender, FeedItemEventArgs e) 
        {
            lock (sync) {
                PodcastItem pi = null;
                
                if (e.Item != null) {
                    if (itemDict.ContainsKey (e.Item.LocalID)) {
                        pi = itemDict[e.Item.LocalID];
                        itemModel.Remove (pi);
                        itemDict.Remove (pi.FeedItemID);
                        pi.Delete ();
                    }
                } else if (e.Items != null) {
                    List<PodcastItem> pis = new List<PodcastItem> ();
            
                    foreach (IFeedItem fi in e.Items) {
                        if (itemDict.ContainsKey (fi.LocalID)) {
                            pi = itemDict[fi.LocalID];
                            
                            pis.Add (pi);
                            itemDict.Remove (fi.LocalID);                            
                        }
                    }
                    
                    itemModel.Remove (pis);
                    PodcastItem.Delete (pis);
                }
                
                feedModel.Reload ();
            }
        } 

        private void OnFeedItemCountChanged (object sender, 
                                             FeedItemCountChangedEventArgs e)
        {
            UpdateCount ();
        }

        private void OnFeedDownloadCompletedHandler (object sender, 
                                                     FeedDownloadCompletedEventArgs e) 
        {
            lock (sync) {
                PodcastFeed f = feedDict[e.Feed.LocalID]; 
                
                if (e.Error == FeedDownloadError.None) {
                    if (String.IsNullOrEmpty(e.Feed.LocalEnclosurePath)) {
                        e.Feed.LocalEnclosurePath = Path.Combine (
                            tmpEnclosurePath, SanitizeName (e.Feed.Name)
                        );
                    }                    
                
                    if (f.SyncPreference != SyncPreference.None) {
                        ReadOnlyCollection<IFeedItem> items = e.Feed.Items;
                        
                        if (items != null) {
                            if (f.SyncPreference == SyncPreference.One && 
                                items.Count > 0) {
                                items[0].Enclosure.AsyncDownload ();
                            } else {
                                foreach (IFeedItem fi in items) {
                                    fi.Enclosure.AsyncDownload ();
                                }
                            }
                        }
                    }
                }
                
                feedModel.Reload ();                
            }            
        }
        
        private void OnTaskAssociated (object sender, EventArgs e)
        {
            lock (sync) {
                feedModel.Reload ();
                itemModel.Reload ();
            }
        }        
        
        private void OnTaskStatusChanged (object sender, 
        								  TaskStatusChangedEventArgs e)
        {
            lock (sync) {
                feedModel.Reload ();
                itemModel.Reload ();
            }
        }        
        
        private void TaskStartedHandler (object sender, 
                                         TaskEventArgs<HttpFileDownloadTask> e)
        {
            lock (sync) {
                feedModel.Reload ();
                itemModel.Reload ();
            }
        }        
        
        private void OnTaskStoppedHandler (object sender, 
                                           TaskEventArgs<HttpFileDownloadTask> e)
        {
            lock (sync) {
                if (e.Task != null && e.Task.Status == TaskStatus.Succeeded) {
                    FeedEnclosure enc = e.Task.UserState as FeedEnclosure;
                
                    if (enc != null) {
                        IFeedItem item = enc.Parent;
                        DatabaseTrackInfo track = null;
                        
                        if (itemDict.ContainsKey (item.LocalID)) {
                            PodcastItem pi = itemDict[item.LocalID];
                            track = importManager.ImportPodcast (enc.LocalPath);                            

                            if (track != null) {
                                pi.Track = track;
                                pi.New = true;
                                pi.Save ();
                            }
                            
                            item.IsRead = true;                            
                        }
                    }                    
                }
                
                feedModel.Reload ();
                itemModel.Reload ();
            }
        }        

        private void OnPlayerEngineStateChangedHandler (object sender, EventArgs e)
        {
            lock (sync) {
                itemModel.Reload ();
            }
        }          

        private void SubscribeToPodcast (Uri uri, SyncPreference syncPreference)
        {                     
            lock (sync) {
                IFeed feed = feedsManager.CreateFeed (uri.ToString ());
                
                if (feed != null) {
                    PodcastFeed pf = new PodcastFeed (feed);
                    pf.SyncPreference = syncPreference;
                    pf.Save ();
                    
                    feedDict.Add (pf.FeedID, pf);
                }
            }        
        }

        private List<IFeedItem> GetEnclosureItems (IEnumerable<IFeedItem> items)
        {            
            List<IFeedItem> encs = new List<IFeedItem> ();                
                
            foreach (IFeedItem i in items) {
                if (i.Enclosure != null) {                    
                    encs.Add (i);
                }
            }    
            
            return encs;
        }

        // Via Monopod
        private string SanitizeName (string s)
        {
            // remove /, : and \ from names
            return s.Replace ('/', '_').Replace ('\\', '_').Replace (':', '_').Replace (' ', '_');
        }        
    }
}
