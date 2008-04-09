/*************************************************************************** 
 *  PodcastItem.cs
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

using Hyena.Data.Sqlite;

using Banshee.MediaEngine;
using Banshee.ServiceStack;

using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;

using Migo.Syndication;

namespace Banshee.Podcasting.Data 
{
    public enum PodcastItemActivity : int {
        Downloading = 0,
        DownloadPending = 1,        
        DownloadFailed = 2,
        DownloadPaused = 3,        
        NewPodcastItem = 4,
        Video = 5,
        Downloaded = 6,
        None = 7,
        Playing = 8,
        Paused = 9
    }

    public class PodcastItem
    {
        [DatabaseColumn ("New", Constraints = DatabaseColumnConstraints.NotNull)]    
        private int _new;
        private int position;
             
        private IFeedItem item;
        private long feedItemID;      
        
        private PodcastFeed feed;
      
        private int trackID;
        private DatabaseTrackInfo track;
        
        private static BansheeModelProvider<PodcastItem> provider;
        
        public static BansheeModelProvider<PodcastItem> Provider {
            get { return provider; }
        }

        public PodcastFeed Feed {
            get { return feed; }
            internal set { feed = value; }
        }
        
        public IFeedItem Item {
            get { return item; }
            internal set {
                if (value != null) {
                    item = value;
                    feedItemID = item.LocalID;                    
                } else {
                    item = null;
                    feedItemID = 0;
                }
            }
        }
        
        public IFeedEnclosure Enclosure {
            get { 
                IFeedEnclosure ret = null;
            
                if (item != null) {
                    ret = item.Enclosure;
                }
                
                return ret;
            }
        }

        public PodcastItemActivity Activity {
            get {
                PodcastItemActivity ret = PodcastItemActivity.None;
            
                if (Track != null) {
                    if (ServiceManager.PlayerEngine.CurrentTrack == Track) {
                        if (ServiceManager.PlayerEngine.CurrentState == PlayerEngineState.Playing) {
                            ret = PodcastItemActivity.Playing;
                        } else if (ServiceManager.PlayerEngine.CurrentState == PlayerEngineState.Paused) {
                            ret = PodcastItemActivity.Paused;
                        }
                    } else {                             
                        if (New) {
                            ret = PodcastItemActivity.NewPodcastItem;
                        } else if ((Track.MediaAttributes & TrackMediaAttributes.VideoStream) != 
                                    TrackMediaAttributes.None) {
                            ret = PodcastItemActivity.Video;
                         } else {
                            ret = PodcastItemActivity.Downloaded;
                         }
                    } 
                } else {
                    switch (item.Enclosure.DownloadStatus) {
                    case FEEDS_DOWNLOAD_STATUS.FDS_PENDING: 
                        ret = PodcastItemActivity.DownloadPending;
                        break;
                    case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADING: 
                        ret = PodcastItemActivity.Downloading;
                        break;
                    case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOADED: 
                        ret = PodcastItemActivity.Downloaded;
                        break;    
                    case FEEDS_DOWNLOAD_STATUS.FDS_DOWNLOAD_FAILED: 
                        ret = PodcastItemActivity.DownloadFailed;
                        break;  
                    case FEEDS_DOWNLOAD_STATUS.FDS_PAUSED: 
                        ret = PodcastItemActivity.DownloadPaused;
                        break;                        
                    }
                }
                
                return ret;
            }
        }
        
        public string Title {
            get { return item.Title; }
        }
        
        public string PodcastTitle {
            get { return item.Parent.Title; }
        }
        
        public DateTime PubDate {
            get { return item.PubDate.ToLocalTime (); }
        }

        // drr...  I know.  FeedItemID should be the primary key, but, it's a 
        // long and sqlite is throwing a hissy fit, I'll look into it 
        // again later when time isn't short.
        private int id = 0;
        [DatabaseColumn ("ID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int ID {
            get { return id; }
            set { id = value; }
        }

        [DatabaseColumn ("FeedItemID", Constraints = DatabaseColumnConstraints.NotNull)]
        public long FeedItemID {
            get { return feedItemID; }
            private set { feedItemID = value; }
        }

        public bool New {
            get { return (_new != 0) ? true : false; }
            set { _new = (value) ? 1 : 0; }
        }
        
        [DatabaseColumn ("Position", Constraints = DatabaseColumnConstraints.NotNull)]
        public int Position {
            get { return position; }
            private set { 
                position = (value < 0) ? 0 : value;
            }
        }        

        [DatabaseColumn ("TrackID", Constraints = DatabaseColumnConstraints.NotNull)]
        public int TrackID {
            get { return trackID; }
            private set { trackID = value; }
        }
        
        public DatabaseTrackInfo Track {
            get {  
                if (track == null && trackID != 0) {
                    Console.WriteLine ("Fetching Track:  {0}", trackID);
                    track = DatabaseTrackInfo.Provider.FetchSingle (trackID);
                }             
                
                return track;
            }
            
            set {
                if (value != null) {
                    track = value;
                    trackID = track.TrackId;                    
                } else {
                    track = null;
                    trackID = 0;
                }
            }
        }

        static PodcastItem ()
        {
            try {
                if (!ServiceManager.DbConnection.TableExists ("PodcastItems")) {
                    ServiceManager.DbConnection.Execute (@"
                        CREATE TABLE PodcastItems (
                            ID         INTEGER PRIMARY KEY,
                            FeedItemID INTEGER NOT NULL DEFAULT 0,
                            TrackID    INTEGER NOT NULL DEFAULT 0,
                            New        INTEGER NOT NULL DEFAULT 0,
                            Position   INTEGER NOT NULL DEFAULT 0
                        );
                        
                        CREATE INDEX podcast_item_id_index ON PodcastItems(ID);                    
                        CREATE INDEX feed_item_id_index ON PodcastItems(FeedItemID);
                        CREATE INDEX track_id_index ON PodcastItems(TrackID);
                    ");
                }
                
                provider = new BansheeModelProvider<PodcastItem> (
                    ServiceManager.DbConnection, "PodcastItems"
                );   
            } catch (Exception e) { Console.WriteLine (e.Message); throw; }
        }   

        public PodcastItem ()
        {
        }

        public PodcastItem (IFeedItem item)
        {
            if (item == null) {
                throw new ArgumentNullException ("item");                
            }

            Item = item;
        }
        
        public void Save ()
        {
            provider.Save (this);
        }  
        
        public void Delete () 
        {
            PodcastItem.Delete (this);
        }
        
        private static string deleteBaseQuery = "DELETE FROM PodcastItems WHERE ID ";
        
        public static void Delete (PodcastItem pi)
        {
            if (pi.ID != 0) {
                ServiceManager.DbConnection.Execute (
                    new HyenaSqliteCommand (
                        deleteBaseQuery + "= ?", pi.ID
                    )
                );
            }  
        }        
        
        public static void Delete (IEnumerable<PodcastItem> pis)
        {
            List<int> piids = new List<int> ();
        
            foreach (PodcastItem pi in pis) {                    
                if (pi.ID != 0) {
                    piids.Add (pi.ID);
                }
            }
            
            StringBuilder builder = new StringBuilder (deleteBaseQuery + "IN (");
            
            if (piids.Count > 0) {
                foreach (int id in piids) {
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