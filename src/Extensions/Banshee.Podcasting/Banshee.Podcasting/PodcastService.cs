/***************************************************************************
 *  PodcastService.cs
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
using Banshee.Configuration;

namespace Banshee.Podcasting
{
    public partial class PodcastService : IExtensionService, IDisposable, IDelayedInitializeService
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

        public PodcastService ()
        {
            // TODO translate Podcasts folder?
            tmp_enclosure_path = Path.Combine (Paths.LibraryLocation, "Podcasts");
            tmp_download_path = Path.Combine (Paths.ApplicationData, "downloads");
            Migo.Net.AsyncWebClient.DefaultUserAgent = Banshee.Web.Browser.UserAgent;
            
            download_manager = new DownloadManager (2, tmp_download_path);
            download_manager_iface = new DownloadManagerInterface (download_manager);
            download_manager_iface.Initialize ();    
    
            feeds_manager = new FeedsManager (ServiceManager.DbConnection, download_manager, Path.Combine (Banshee.Base.Paths.CachedLibraryLocation, "Podcasts"));
            
            feeds_manager.FeedManager.ItemAdded += OnItemAdded;
            feeds_manager.FeedManager.ItemChanged += OnItemChanged;
            feeds_manager.FeedManager.ItemRemoved += OnItemRemoved;
            feeds_manager.FeedManager.FeedsChanged += OnFeedsChanged;

            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, PlayerEvent.StateChange);

            InitializeInterface ();
        }

        private void MigrateIfPossible ()
        {
            if (DatabaseConfigurationClient.Client.Get<int> ("Podcast", "Version", 0) == 0) {
                if (ServiceManager.DbConnection.TableExists ("Podcasts") &&
                        ServiceManager.DbConnection.Query<int> ("select count(*) from podcastsyndications") == 0) {
                    Hyena.Log.Information ("Migrating Podcast Feeds and Items");
                    ServiceManager.DbConnection.Execute(@"
                        INSERT INTO PodcastSyndications (FeedID, Title, Url, Link,
                            Description, ImageUrl, LastBuildDate, SyncSetting)
                            SELECT 
                                PodcastFeedID,
                                Title,
                                FeedUrl,
                                Link,
                                Description,
                                Image,
                                strftime(""%s"", LastUpdated),
                                SyncPreference
                            FROM PodcastFeeds
                    ");

                    ServiceManager.DbConnection.Execute(@"
                        INSERT INTO PodcastItems (ItemID, FeedID, Title, Link, PubDate,
                            Description, Author, Active, Guid)
                            SELECT 
                                PodcastID,
                                PodcastFeedID,
                                Title,
                                Link,
                                strftime(""%s"", PubDate),
                                Description,
                                Author,
                                Active,
                                Url
                            FROM Podcasts
                    ");

                    // Note: downloaded*3 is because the value was 0 or 1, but is now 0 or 3 (FeedDownloadStatus.None/Downloaded)
                    ServiceManager.DbConnection.Execute(@"
                        INSERT INTO PodcastEnclosures (ItemID, LocalPath, Url, MimeType, FileSize, DownloadStatus)
                            SELECT 
                                PodcastID,
                                LocalPath,
                                Url,
                                MimeType,
                                Length,
                                Downloaded*3
                            FROM Podcasts
                    ");

                    // Finally, move podcast items from the Music Library to the Podcast source
                    int [] primary_source_ids = new int [] { ServiceManager.SourceManager.MusicLibrary.DbId };
                    int moved = 0;
                    foreach (FeedEnclosure enclosure in FeedEnclosure.Provider.FetchAllMatching ("LocalPath IS NOT NULL AND LocalPath != ''")) {
                        SafeUri uri = new SafeUri (enclosure.LocalPath);
                        int track_id = DatabaseTrackInfo.GetTrackIdForUri (
                            uri, Paths.MakePathRelative (uri.LocalPath, tmp_enclosure_path),
                            primary_source_ids
                        );

                        if (track_id > 0) {
                            PodcastTrackInfo track = PodcastTrackInfo.Provider.FetchSingle (track_id);
                            track.Item = enclosure.Item;
                            track.PrimarySourceId = source.DbId;
                            track.Save (false);
                            moved++;
                        }
                    }

                    if (moved > 0) {
                        ServiceManager.SourceManager.MusicLibrary.Reload ();
                        source.Reload ();
                    }

                    Hyena.Log.Information ("Done Migrating Podcast Feeds and Items");
                }
                DatabaseConfigurationClient.Client.Set<int> ("Podcast", "Version", 1);
            }

        }
        
        public void Initialize ()
        {
        }
        
        public void DelayedInitialize ()
        {
            // Migrate data from 0.13.2 podcast tables, if they exist
            MigrateIfPossible ();
              
            foreach (Feed feed in Feed.Provider.FetchAll ()) {
                feed.Update ();
                RefreshArtworkFor (feed);
            }
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

            if (download_manager_iface != null) {
                download_manager_iface.Dispose ();                
                download_manager_iface = null;
            }                
           
            if (feeds_manager != null) {   
                feeds_manager.Dispose ();
                feeds_manager = null;
            }

            if (download_manager != null) {            
                download_manager.Dispose ();
                download_manager = null;
            }
            
            DisposeInterface ();            
            
            lock (sync) {
                disposing = false;            
                disposed = true;
            }
        }
        
        private void RefreshArtworkFor (Feed feed)
        {
            if (feed.LastDownloadTime != DateTime.MinValue)
                Banshee.Kernel.Scheduler.Schedule (new PodcastImageFetchJob (feed), Banshee.Kernel.JobPriority.Highest);
        }
        
        private void OnItemAdded (FeedItem item)
        {
            if (item.Enclosure != null) {
                PodcastTrackInfo track = new PodcastTrackInfo (item);
                track.PrimarySource = source;
                track.Save (true);
                RefreshArtworkFor (item.Feed);
            } else {
                // We're only interested in items that have enclosures
                item.Delete (false);
            }
        }
        
        private void OnItemRemoved (FeedItem item)
        {
            PodcastTrackInfo track = PodcastTrackInfo.GetByItemId (item.DbId);
            if (track != null) {
                track.Delete ();
            }
        }
        
        private void OnItemChanged (FeedItem item)
        {
            PodcastTrackInfo track = PodcastTrackInfo.GetByItemId (item.DbId);
            if (track != null) {
                track.SyncWithFeedItem ();
                track.Save (true);
            }
        }
        
        private void OnFeedsChanged (object o, EventArgs args)
        {
            source.Reload ();
        }

        /*private void OnFeedAddedHandler (object sender, FeedEventArgs args)
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
        }*/

        /*private void OnFeedItemAddedHandler (object sender, FeedItemEventArgs args) 
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
        }*/
        
        public void AddFeedItem (FeedItem item)
        {
            if (item.Enclosure != null) {
                PodcastTrackInfo pi = new PodcastTrackInfo (item);
                pi.PrimarySource = source;
                pi.Save (true);
            } else {
                item.Delete (false);                      
            }
        }

        /*private void OnFeedItemRemovedHandler (object sender, FeedItemEventArgs e) 
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
        }*/

        /*private void OnFeedDownloadCompletedHandler (object sender, 
                                                     FeedDownloadCompletedEventArgs e) 
        {
            lock (sync) {
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
            }
        }*/
        
        /*private void OnTaskAssociated (object sender, EventArgs e)
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
            lock (sync) {
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
            }
        }*/    

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            lock (sync) {
                //source.Reload ();
            }
        }          

        public static string ArtworkIdFor (Feed feed)
        {
            return String.Format ("podcast-{0}", Banshee.Base.CoverArtSpec.EscapePart (feed.Title));
        }

        // Via Monopod
        /*private static string SanitizeName (string s)
        {
            // remove /, : and \ from names
            return s.Replace ('/', '_').Replace ('\\', '_').Replace (':', '_').Replace (' ', '_');
        }*/   
        
        public string ServiceName {
            get { return "PodcastService"; } 
        }
    }
}
