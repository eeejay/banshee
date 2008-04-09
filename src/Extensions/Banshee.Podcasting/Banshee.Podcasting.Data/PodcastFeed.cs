/*************************************************************************** 
 *  PodcastFeed.cs
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
using System.Text;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data.Sqlite;

using Banshee.Database;
using Banshee.ServiceStack;

using Banshee.Collection;
using Banshee.Collection.Database;

using Migo.Syndication;

namespace Banshee.Podcasting.Data 
{
    public enum SyncPreference : int 
    {
        All = 0,
        One = 1,
        None = 2
    }

    public enum PodcastFeedActivity : int {
        Updating = 0,
        UpdatePending = 1,        
        UpdateFailed = 2,
        ItemsDownloading = 4,        
        ItemsQueued = 5,        
        None = 6        
    }

    public class PodcastFeed
    {
        private IFeed feed;
        private long feedID;

        [DatabaseColumn ("SyncPreference", Constraints = DatabaseColumnConstraints.NotNull)]
        private int syncPreference;
        
        private static PodcastFeed all = new PodcastFeed ();
        private static BansheeModelProvider<PodcastFeed> provider;
        
        public static BansheeModelProvider<PodcastFeed> Provider {
            get { return provider; }
        }

        public PodcastFeedActivity Activity {
            get {
                PodcastFeedActivity ret = PodcastFeedActivity.None;
                
                if (this == PodcastFeed.All) {
                    return ret;
                }
                
                switch (feed.DownloadStatus) {
                case FEEDS_DOWNLOAD_STATUS.FDS_PENDING: 
                    ret = PodcastFeedActivity.UpdatePending;
                    break;
                case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADING: 
                    ret = PodcastFeedActivity.Updating;
                    break;    
                case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOAD_FAILED: 
                    ret = PodcastFeedActivity.UpdateFailed;
                    break;                         
                }
                
                if (ret != PodcastFeedActivity.Updating) {
                    if (feed.ActiveDownloadCount > 0) {
                        ret = PodcastFeedActivity.ItemsDownloading;
                    } else if (feed.QueuedDownloadCount > 0) {
                        ret = PodcastFeedActivity.ItemsQueued;
                    }
                }

                return ret;
            }
        }

        public static PodcastFeed All {
            get { return all; }
        }

        public IFeed Feed {
            get { return feed; }
            set {
                if (value != null) {
                    feed = value;
                    feedID = feed.LocalID;                    
                } else {
                    feed = null;
                    feedID = 0;
                }
            }
        }

        public string Title {
            get { 
                if (feed != null) {
                    long itemCount = feed.ItemCount;
                    long downloadedItems = itemCount - feed.UnreadItemCount;
                
                    return String.Format (
                        "{0} - ({1}/{2})", feed.Title, downloadedItems, itemCount); 
                } else {
                    return Catalog.GetString ("All");
                }
            }
        }

        // drr...  I know.  FeedID should be the primary key, but, it's a 
        // long and sqlite is throwing a hissy fit, I'll look into it 
        // again later when time isn't short.
        private int id = 0;
        [DatabaseColumn ("ID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int ID {
            get { return id; }
            set { id = value; }
        }

        [DatabaseColumn ("FeedID", Constraints = DatabaseColumnConstraints.NotNull)]
        public long FeedID {
            get { return feedID; }
            private set { feedID = value; }
        }

        public SyncPreference SyncPreference {
            get { return (SyncPreference) syncPreference; }
            set { syncPreference = (int) value; }
        }

        static PodcastFeed ()
        {
            try {
                if (!ServiceManager.DbConnection.TableExists ("PodcastFeeds")) {
                    ServiceManager.DbConnection.Execute (@"
                        CREATE TABLE PodcastFeeds (
                            ID             INTEGER PRIMARY KEY,
                            FeedID         INTEGER NOT NULL DEFAULT 0,
                            SyncPreference INTEGER NOT NULL DEFAULT 0
                        );
                        
                        CREATE INDEX podcast_feed_id_index ON PodcastFeeds(ID);                    
                        CREATE INDEX feed_id_index ON PodcastFeeds(FeedID);
                    ");
                }             

                provider = new BansheeModelProvider<PodcastFeed> (
                    ServiceManager.DbConnection, "PodcastFeeds"
                );   
            } catch/* (Exception e)*/ { /*Console.WriteLine (e.Message);*/ throw; }
        }   

        public PodcastFeed ()
        {
        }

        public PodcastFeed (IFeed feed)
        {
            if (feed == null) {
                throw new ArgumentNullException ("feed");                
            }

            Feed = feed;
        }

        public void Save ()
        {
            provider.Save (this);
        }  
        
        public void Delete () 
        {
            PodcastFeed.Delete (this);
        }
        
        private static string deleteBaseQuery = "DELETE FROM PodcastFeeds WHERE ID ";
        
        public static void Delete (PodcastFeed feed)
        {
            if (feed.ID != 0) {
                ServiceManager.DbConnection.Execute (
                    new HyenaSqliteCommand (
                        deleteBaseQuery + "= ?", feed.ID
                    )
                );
            }  
        }        
        
        public static void Delete (IEnumerable<PodcastFeed> feeds)
        {
            List<int> feedIDs = new List<int> ();
        
            foreach (PodcastFeed feed in feeds) {                    
                if (feed.ID != 0) {
                    feedIDs.Add (feed.ID);
                }
            }
            
            StringBuilder builder = new StringBuilder (deleteBaseQuery + "IN (");
            
            if (feedIDs.Count > 0) {
                foreach (int id in feedIDs) {
                    builder.AppendFormat ("{0},", id);
                }
                
                builder.Remove (builder.Length-1, 1);
                builder.Append (");");
                
                ServiceManager.DbConnection.Execute (
                    new HyenaSqliteCommand (builder.ToString ())
                );                
            }
        }
    }
}