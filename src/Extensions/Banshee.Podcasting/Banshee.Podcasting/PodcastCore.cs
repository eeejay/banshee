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

using Banshee.MediaEngine;
using Banshee.Podcasting.Gui;
using Banshee.Podcasting.Data;
using Banshee.Collection.Database;

namespace Banshee.Podcasting
{
    public partial class PodcastCore : IDisposable
    {  
        private readonly string tmp_download_path;
        private string tmp_enclosure_path; 
            
        private bool disposed;
        
        private DownloadManager download_manager;
        private DownloadManagerInterface download_manager_iface;
        
        private FeedsManager feeds_manager;
        
        private PodcastSource source;
        //private PodcastImportManager import_manager;
        
        private readonly object sync = new object ();

        public PodcastCore ()
        {
            // TODO translate Podcasts folder?
            tmp_enclosure_path = Path.Combine (Paths.LibraryLocation, "Podcasts");
            tmp_download_path = Path.Combine (Paths.ApplicationData, "downloads");
            
            download_manager = new DownloadManager (2, tmp_download_path);  

            download_manager_iface = new DownloadManagerInterface (download_manager);
            download_manager_iface.Initialize ();    
    
            feeds_manager = new FeedsManager (ServiceManager.DbConnection, download_manager);

            download_manager.Group.TaskAssociated += OnTaskAssociated;                        
            download_manager.Group.TaskStatusChanged += OnTaskStatusChanged;
            
//            dm.Group.TaskStopped += OnTaskStoppedHandler;                
            download_manager.Group.TaskStarted += TaskStartedHandler;   

            feeds_manager.FeedAdded += OnFeedAddedHandler;
            feeds_manager.FeedDeleted += OnFeedRemovedHandler;
            feeds_manager.FeedRenamed += OnFeedRenamedHandler;
            feeds_manager.FeedUrlChanged += OnFeedRenamedHandler;
            
            feeds_manager.FeedItemAdded += OnFeedItemAddedHandler;
            feeds_manager.FeedItemRemoved += OnFeedItemRemovedHandler;
            feeds_manager.FeedItemCountChanged += OnFeedItemCountChanged;
            
            feeds_manager.FeedDownloading += OnFeedUpdatingHandler;
            feeds_manager.FeedDownloadCompleted += OnFeedDownloadCompletedHandler;
            feeds_manager.FeedDownloadCountChanged += OnFeedDownloadCountChangedHandler;
            feeds_manager.EnclosureDownloadCompleted += OnTaskStoppedHandler;
              
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.StateChange);

            InitializeInterface ();

            //import_manager = new PodcastImportManager (source);
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
            
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);

            Log.Debug ("Disposing dmInterface");    
            if (download_manager_iface != null) {
                download_manager_iface.Dispose ();                
                download_manager_iface = null;
            }                
           
            Log.Debug ("Disposing feedsManager");
            if (feeds_manager != null) {   
                feeds_manager.Dispose ();

                feeds_manager.FeedAdded -= OnFeedAddedHandler;       
                feeds_manager.FeedDeleted -= OnFeedRemovedHandler;
                feeds_manager.FeedRenamed -= OnFeedRenamedHandler;
                feeds_manager.FeedUrlChanged -= OnFeedRenamedHandler;
                                
                feeds_manager.FeedItemAdded -= OnFeedItemAddedHandler;
                feeds_manager.FeedItemRemoved -= OnFeedItemRemovedHandler;
                feeds_manager.FeedItemCountChanged -= OnFeedItemCountChanged;                
                
                feeds_manager.FeedDownloading -= OnFeedUpdatingHandler;
                feeds_manager.FeedDownloadCompleted -= OnFeedDownloadCompletedHandler;
                feeds_manager.FeedDownloadCountChanged -= OnFeedDownloadCountChangedHandler;

                feeds_manager = null;
            }
            
            Log.Debug ("Disposing dm");
            if (download_manager != null) {            
                download_manager.Dispose ();
            
                download_manager.Group.TaskAssociated -= OnTaskAssociated;                        
                download_manager.Group.TaskStatusChanged -= OnTaskStatusChanged;
                
                download_manager.Group.TaskStopped -= OnTaskStoppedHandler;                
                download_manager.Group.TaskStarted -= TaskStartedHandler;                   
                download_manager = null;
            }
            
            DisposeInterface ();            
            
            lock (sync) {
                disposing = false;            
                disposed = true;
            }
        }

        private void OnFeedAddedHandler (object sender, FeedEventArgs args)
        {
            lock (sync) {
                source.FeedModel.Add (args.Feed);
            }
        }
        
        private void OnFeedRemovedHandler (object sender, FeedEventArgs args)
        {
            lock (sync) {
                source.FeedModel.Remove (args.Feed);
                args.Feed.Delete ();
            }
        }        

        private void OnFeedRenamedHandler (object sender, FeedEventArgs args)
        {
            lock (sync) {
                source.FeedModel.Sort ();
            }
        }

        private void OnFeedUpdatingHandler (object sender, FeedEventArgs args)
        {   
            lock (sync) {
                source.FeedModel.Reload ();
            }
        }

        private void OnFeedDownloadCountChangedHandler (object sender, FeedDownloadCountChangedEventArgs args)
        {
            lock (sync) {
                source.FeedModel.Reload ();                
            }
        }

        private void OnFeedItemAddedHandler (object sender, FeedItemEventArgs args) 
        {
            lock (sync) {
                if (args.Item != null) {
                    AddFeedItem (args.Item);
                } else if (args.Items != null) {
                    foreach (FeedItem item in args.Items) {
                        AddFeedItem (item);
                    }
                }
            }
        }
        
        private void AddFeedItem (FeedItem item)
        {
            if (item.Enclosure != null) {
                PodcastItem pi = new PodcastItem (item);
                pi.PrimarySource = source;
                pi.Save ();                       
            } else {
                item.Delete ();                        
            }
        }

        private void OnFeedItemRemovedHandler (object sender, FeedItemEventArgs e) 
        {
            lock (sync) {
                if (e.Item != null) {
                    PodcastItem.DeleteWithFeedId (e.Item.DbId);
                } else if (e.Items != null) {
                    foreach (FeedItem fi in e.Items) {
                        PodcastItem.DeleteWithFeedId (fi.DbId);
                    }
                }
                
                source.Reload ();
            }
        } 

        private void OnFeedItemCountChanged (object sender, 
                                             FeedItemCountChangedEventArgs e)
        {
            //UpdateCount ();
        }

        private void OnFeedDownloadCompletedHandler (object sender, 
                                                     FeedDownloadCompletedEventArgs e) 
        {
            /*lock (sync) {
                Feed f = feedDict[e.Feed.DbId]; 
                
                if (e.Error == FeedDownloadError.None) {
                    if (String.IsNullOrEmpty(e.Feed.LocalEnclosurePath)) {
                        e.Feed.LocalEnclosurePath = Path.Combine (
                            tmp_enclosure_path, SanitizeName (e.Feed.Name)
                        );
                    }                    
                
                    if (f.AutoDownload != FeedAutoDownload.None) {
                        ReadOnlyCollection<FeedItem> items = e.Feed.Items;
                        
                        if (items != null) {
                            if (f.AutoDownload == FeedAutoDownload.One && 
                                items.Count > 0) {
                                items[0].Enclosure.AsyncDownload ();
                            } else {
                                foreach (FeedItem fi in items) {
                                    fi.Enclosure.AsyncDownload ();
                                }
                            }
                        }
                    }
                }
                
                source.Reload ();                
            }*/    
        }
        
        private void OnTaskAssociated (object sender, EventArgs e)
        {
            lock (sync) {
                source.Reload ();
            }
        }        
        
        private void OnTaskStatusChanged (object sender, 
        								  TaskStatusChangedEventArgs e)
        {
            lock (sync) {
                source.Reload ();
            }
        }        
        
        private void TaskStartedHandler (object sender, 
                                         TaskEventArgs<HttpFileDownloadTask> e)
        {
            lock (sync) {
                source.Reload ();
            }
        }        
        
        private void OnTaskStoppedHandler (object sender, 
                                           TaskEventArgs<HttpFileDownloadTask> e)
        {
            // TODO merge
            /*lock (sync) {
                if (e.Task != null && e.Task.Status == TaskStatus.Succeeded) {
                    FeedEnclosure enc = e.Task.UserState as FeedEnclosure;
                
                    if (enc != null) {
                        FeedItem item = enc.Item;
                        DatabaseTrackInfo track = null;
                        
                        
                        
                        if (itemDict.ContainsKey (item.DbId)) {
                            PodcastItem pi = itemDict[item.DbId];
                            track = import_manager.ImportPodcast (enc.LocalPath);                            

                            if (track != null) {
                                pi.Track = track;
                                pi.New = true;
                                pi.Save ();
                            }
                            
                            item.IsRead = true;                            
                        }
                    }                    
                }
                
                source.Reload ();
            }*/
        }        

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            lock (sync) {
                //source.Reload ();
            }
        }          

        private void SubscribeToPodcast (Uri uri, FeedAutoDownload syncPreference)
        {                     
            lock (sync) {
                Feed feed = feeds_manager.CreateFeed (uri.ToString ());
                
                if (feed != null) {
                    feed.AutoDownload = syncPreference;
                    feed.Save ();
                }
            }        
        }

        // Via Monopod
        /*private static string SanitizeName (string s)
        {
            // remove /, : and \ from names
            return s.Replace ('/', '_').Replace ('\\', '_').Replace (':', '_').Replace (' ', '_');
        }*/   
    }
}
