/*************************************************************************** 
 *  PodcastCoreInterface.cs
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
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Gtk;
using Mono.Unix;

using Hyena.Data;

using Banshee.Web;
using Banshee.Base;
using Banshee.Sources;
using Banshee.Streaming;
using Banshee.ServiceStack;

using Banshee.Gui;
using Banshee.Widgets;

using Hyena.Data.Gui;

using Banshee.Collection;
using Banshee.Collection.Database;

using Banshee.Podcasting.Gui;
using Banshee.Podcasting.Data;

namespace Banshee.Podcasting
{
    public partial class PodcastCore
    {                  
        private void InitializeInterface ()
        {
            BuildActions ();

            podcastSource = new PodcastSource (tmpEnclosurePath, feedModel, itemModel);
            
            podcastSource.FeedView.SelectionProxy.Changed += OnFeedSelectionChangedHandler;
            podcastSource.ItemView.RowActivated += OnPodcastItemRowActivatedHandler;            
            
            ServiceManager.SourceManager.AddSource (podcastSource);
        }         
        
        private void DisposeInterface ()
        {
            if (podcastSource != null) {
                podcastSource.FeedView.SelectionProxy.Changed -= OnFeedSelectionChangedHandler;
                podcastSource.ItemView.RowActivated -= OnPodcastItemRowActivatedHandler;              
                
                ServiceManager.SourceManager.RemoveSource (podcastSource);
                podcastSource = null;
            }              
        }     
        
        private void BuildActions ()
        {
            InterfaceActionService interfaceActions = 
                ServiceManager.Get<InterfaceActionService> ();
            
            BansheeActionGroup bag = new BansheeActionGroup ("Podcast");
            
            bag.AddImportant (new ActionEntry[] {
                new ActionEntry (
                    "PodcastUpdateAllAction", Stock.Refresh,
                     Catalog.GetString ("Update Podcasts"), null,//"<control><shift>U",
                     Catalog.GetString ("Update All Podcasts"), 
                     OnPodcastUpdateAllAction
                ),
                new ActionEntry (
                    "PodcastAddAction", Stock.New,
                     Catalog.GetString ("Subscribe to Podcast"),"<control><shift>F", 
                     Catalog.GetString ("Subscribe to a new podcast feed"),
                     OnPodcastAddAction
                )         
            });
            
            bag.Add (new ActionEntry [] {
                new ActionEntry (
                    "PodcastDeleteAction", Stock.Delete,
                     Catalog.GetString ("Delete"),
                     null, String.Empty, 
                     OnPodcastDeleteAction
                ),
                new ActionEntry (
                    "PodcastUpdateFeedAction", Stock.Refresh,
                     Catalog.GetString ("Update"),
                     null, String.Empty, 
                     OnPodcastUpdateAction
                ),
                new ActionEntry (
                    "PodcastHomepageAction", Stock.JumpTo,
                     Catalog.GetString ("Homepage"),
                     null, String.Empty, 
                     OnPodcastHomepageAction
                ),
                new ActionEntry (
                    "PodcastPropertiesAction", Stock.Properties,
                     Catalog.GetString ("Properties"),
                     null, String.Empty, 
                     OnPodcastPropertiesAction
                ),
                new ActionEntry (
                    "PodcastItemMarkNewAction", null,
                     Catalog.GetString ("Mark as New"), 
                     "<control><shift>N", String.Empty,
                     OnPodcastItemMarkNewAction
                ),
                new ActionEntry (
                    "PodcastItemMarkOldAction", null,
                     Catalog.GetString ("Mark as Old"),
                     "<control><shift>O", String.Empty,
                     OnPodcastItemMarkOldAction
                ),
                new ActionEntry (
                    "PodcastItemDownloadAction", Stock.GoDown,
                     Catalog.GetString ("Download"),
                     "<control><shift>D", String.Empty, 
                     OnPodcastItemDownloadAction
                ),
                new ActionEntry (
                    "PodcastItemCancelAction", Stock.Stop,
                     Catalog.GetString ("Cancel"),
                     "<control><shift>C", String.Empty, 
                     OnPodcastItemCancelAction
                ),
                new ActionEntry (
                    "PodcastItemDeleteAction", Stock.Remove,
                     Catalog.GetString ("Remove from Library"),
                     null, String.Empty, 
                     OnPodcastItemRemoveAction
                ),
                new ActionEntry (
                    "PodcastItemLinkAction", Stock.JumpTo,
                     Catalog.GetString ("Link"),
                     null, String.Empty, 
                     OnPodcastItemLinkAction
                ),
                new ActionEntry (
                    "PodcastItemPropertiesAction", Stock.Properties,
                     Catalog.GetString ("Properties"),
                     null, String.Empty, 
                     OnPodcastItemPropertiesAction
                )
            });            

            interfaceActions.UIManager.AddUiFromResource ("GlobalUI.xml");
            interfaceActions.AddActionGroup (bag);      
        }
        
        private void RunSubscribeDialog ()
        {        
            Uri feedUri = null;
            SyncPreference syncPreference;
            
            PodcastSubscribeDialog subscribeDialog = new PodcastSubscribeDialog ();
            
            ResponseType response = (ResponseType) subscribeDialog.Run ();
            syncPreference = subscribeDialog.SyncPreference;
            
            subscribeDialog.Destroy ();

            if (response == ResponseType.Ok) {
                string url = subscribeDialog.Url.Trim ().Trim ('/');
                
                if (String.IsNullOrEmpty (subscribeDialog.Url)) {
                    return;
                }
                
				if (!TryParseUrl (url, out feedUri)) {
                    HigMessageDialog.RunHigMessageDialog (
                        null,
                        DialogFlags.Modal,
                        MessageType.Warning,
                        ButtonsType.Ok,
                        Catalog.GetString ("Invalid URL"),
                        Catalog.GetString ("Podcast feed URL is invalid.")
                    );
				} else {
				    SubscribeToPodcast (feedUri, syncPreference); 
				}
            }        
        }
        
        private void RunConfirmDeleteDialog (bool feed, 
                                             int selCount, 
                                             out bool delete, 
                                             out bool deleteFiles)
        {
            
            delete = false;
            deleteFiles = false;        
            string header = null;
            int plural = (feed | (selCount > 1)) ? 2 : 1;

            if (feed) {
                header = Catalog.GetPluralString ("Delete Podcast?","Delete Podcasts?", selCount);
            } else {
                header = Catalog.GetPluralString ("Delete episode?", "Delete episodes?", selCount);
            }
                
            HigMessageDialog md = new HigMessageDialog (
                ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow,
                DialogFlags.DestroyWithParent, 
                MessageType.Question,
                ButtonsType.None, header, 
                Catalog.GetPluralString (
                    "Would you like to delete the associated file?",
                    "Would you like to delete the associated files?", plural                
                )
            );
            
            md.AddButton (Stock.Cancel, ResponseType.Cancel, true);
            md.AddButton (
                Catalog.GetPluralString (
                    "Keep File", "Keep Files", plural
                ), ResponseType.No, false
            );
            
            md.AddButton (Stock.Delete, ResponseType.Yes, false);
            
            try {
                switch ((ResponseType)md.Run ()) {
                case ResponseType.Yes:
                    deleteFiles = true;
                    goto case ResponseType.No;
                case ResponseType.No:
                    delete = true;
                    break;
                }                
            } finally {
                md.Destroy ();
            }       
        }
        
		private bool TryParseUrl (string url, out Uri uri)
		{
			uri = null;
			bool ret = false;
			
            try {
                uri = new Uri (url);
				
				if (uri.Scheme == Uri.UriSchemeHttp || 
				    uri.Scheme == Uri.UriSchemeHttps) {
					ret = true;
				}
            } catch {}
            
            return ret;			
		}

        private void OnFeedSelectionChangedHandler (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {              
                    if (feedModel.SelectedItems.Count == 0) {
                        feedModel.Selection.Select (0);
                    }
                    
                    if (feedModel.Selection.Contains (0)) {
                        itemModel.FilterOnFeed (PodcastFeed.All);
                    } else {
                        itemModel.FilterOnFeeds (feedModel.CopySelectedItems ());
                    }
                    
                    itemModel.Selection.Clear ();
                }
            }
        }
        
        private void OnPodcastItemRowActivatedHandler (object sender, 
                                                       RowActivatedArgs<PodcastItem> e)
        {
            lock (sync) {
                if (!disposed || disposing) {         
                    TrackInfo track = e.RowValue.Track;
                    
                    if (track != null) {
                        e.RowValue.New = false;
                        ServiceManager.PlayerEngine.OpenPlay (track);
                    }
                }
            }
        }

        private void OnPodcastAddAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {         
                    RunSubscribeDialog ();
                }
            }
        }
        
        private void OnPodcastUpdateAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {          
                    ReadOnlyCollection<PodcastFeed> feeds = feedModel.CopySelectedItems ();
                    
                    if (feeds != null) {
                        foreach (PodcastFeed f in feeds) {
                            if (f != PodcastFeed.All) {
                                f.Feed.AsyncDownload ();                                
                            }
                        }            	
                    }
                }
            }
        }        
        
        private void OnPodcastUpdateAllAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    foreach (PodcastFeed podcast in feedModel.Copy ()) {
                        podcast.Feed.AsyncDownload ();
                    }
                }
            }
        }      
        
        private void OnPodcastDeleteAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    bool deleteFeed;
                    bool deleteFiles;                    
                    ReadOnlyCollection<PodcastFeed> feeds = feedModel.CopySelectedItems ();
                    
                    if (feeds != null) {                    
                        RunConfirmDeleteDialog (
                            true, feeds.Count, out deleteFeed, out deleteFiles
                        );
                        
                        
                        if (deleteFeed) {
                            feedModel.Selection.Clear ();
                            itemModel.Selection.Clear ();
                            
                            foreach (PodcastFeed f in feeds) {
                                f.Feed.Delete (deleteFiles);   
                            }                                 
                        }                   
                    }                    
                }
            }
        }        

        private void OnPodcastItemRemoveAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    bool deleteItems;
                    bool deleteFiles;                    
                    ReadOnlyCollection<PodcastItem> items = itemModel.CopySelectedItems ();        
                    
                    if (items != null) {                             
                        RunConfirmDeleteDialog (
                            false, items.Count, 
                            out deleteItems, out deleteFiles
                        );
                        
                        if (deleteItems) {
                            itemModel.Selection.Clear ();
                            itemModel.Remove (items);
                            
                            foreach (PodcastItem i in items) {
                                i.Item.Delete (deleteFiles);
                            }
                        }    
                    }   
                }
            }
        }  

        private void OnPodcastHomepageAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    ReadOnlyCollection<PodcastFeed> feeds = feedModel.CopySelectedItems ();
                    
                    if (feeds != null && feeds.Count == 1) {
           	            string link = feeds[0].Feed.Link;
           	            
           	            if (!String.IsNullOrEmpty (link)) {
                            Banshee.Web.Browser.Open (link);           	                
           	            }
                    }                 
                }
            }       
        }   

        private void OnPodcastPropertiesAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    ReadOnlyCollection<PodcastFeed> feeds = feedModel.CopySelectedItems ();
                    
                    if (feeds != null && feeds.Count == 1) {
                        new PodcastFeedPropertiesDialog (feeds[0]).Run ();
                    }                 
                }
            }  
        }  

        private void OnPodcastItemPropertiesAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    ReadOnlyCollection<PodcastItem> items = itemModel.CopySelectedItems ();
                    
                    if (items != null && items.Count == 1) {
                        new PodcastPropertiesDialog (items[0]).Run ();
                    }                 
                }
            }  
        } 

        private void OnPodcastItemMarkNewAction (object sender, EventArgs e)
        {
            MarkPodcastItemSelection (true);
        }
        
        private void OnPodcastItemMarkOldAction (object sender, EventArgs e)
        {
            MarkPodcastItemSelection (false); 
        }     
        
        private void MarkPodcastItemSelection (bool markNew) 
        {
            lock (sync) {
                if (!disposed || disposing) {                    
                    ReadOnlyCollection<PodcastItem> items = itemModel.CopySelectedItems ();

                    if (items != null) {
                        ServiceManager.DbConnection.BeginTransaction ();
                        
                        try {                    
                            foreach (PodcastItem pi in items) {
                                if (pi.Track != null && pi.New != markNew) {
                                    pi.New = markNew;
                                    pi.Save ();
                                }
                            }
                            
                            ServiceManager.DbConnection.CommitTransaction ();
                        } catch {
                            ServiceManager.DbConnection.RollbackTransaction ();
                        }                        
                        
                        itemModel.Reload ();                        
                    }                
                }
            }
        }
        
        private void OnPodcastItemCancelAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {                    
                    ReadOnlyCollection<PodcastItem> items = itemModel.CopySelectedItems ();

                    if (items != null) {
                        foreach (PodcastItem pi in items) {
                            pi.Enclosure.CancelAsyncDownload ();
                        }
                    }                
                }
            }
        }        
        
        private void OnPodcastItemDownloadAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    ReadOnlyCollection<PodcastItem> items = itemModel.CopySelectedItems ();

                    if (items != null) {
                        foreach (PodcastItem pi in items) {
                            pi.Enclosure.AsyncDownload ();
                        }            	
                    }                 
                }
            }       
        }
        
        private void OnPodcastItemLinkAction (object sender, EventArgs e)
        {
            lock (sync) {
                if (!disposed || disposing) {
                    ReadOnlyCollection<PodcastItem> items = itemModel.CopySelectedItems ();

                    if (items != null && items.Count == 1) {
           	            string link = items[0].Item.Link;
           	            
           	            if (!String.IsNullOrEmpty (link)) {
                            Banshee.Web.Browser.Open (link);           	                
           	            }
                    }                 
                }
            }       
        }   
    }
}