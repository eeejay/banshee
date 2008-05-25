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
        //NewPodcastItem = 4,
        //Video = 5,
        Downloaded = 6,
        None = 7
    }

    public class PodcastTrackInfo : DatabaseTrackInfo
    {
        private static BansheeModelProvider<PodcastTrackInfo> provider = new DatabaseTrackModelProvider<PodcastTrackInfo> (ServiceManager.DbConnection);
        public static new BansheeModelProvider<PodcastTrackInfo> Provider {
            get { return provider; }
        }
        
        public static PodcastTrackInfo GetByItemId (long item_id)
        {
            return Provider.FetchFirstMatching ("ExternalID = ?", item_id);
        }
        
        private bool @new;
        private int position;
        private long item_id;

#region Properties

        public Feed Feed {
            get { return Item.Feed; }
        }
        
        private FeedItem item;
        public FeedItem Item {
            get {
                if (item == null && item_id > 0) {
                    item = FeedItem.Provider.FetchSingle (item_id);
                }
                return item;
            }
            set { item = value; item_id = value.DbId; }
        }
        
        public DateTime PublishedDate {
            get { return Item.PubDate; }
        }
        
        public bool New {
            get { return @new; }
            set { @new = value; }
        }
        
        public int Position {
            get { return position; }
            set { position = value; }
        }
        
        [DatabaseColumn ("ExternalID")]
        public long ItemId {
            get { return item_id; }
            private set { item_id = value; }
        }
        
        //[VirtualDatabaseColumn ("Title", Item.Feed.Title, "ItemID", "ExternalID")]
        
        // Override these two so they aren't considered DatabaseColumns so we don't
        // join with CoreArtists/CoreAlbums
        /*public override string AlbumTitle {
            get { return Item.Feed.Title; }
        }
        
        public override string ArtistName {
            get { return Item.Author; }
        }*/
        
        public FeedEnclosure Enclosure {
            get { return (Item == null) ? null : Item.Enclosure; }
        }

        public PodcastItemActivity Activity {
            get {
                switch (Item.Enclosure.DownloadStatus) {
                case FeedDownloadStatus.Downloaded:
                    return PodcastItemActivity.Downloaded;
               
                case FeedDownloadStatus.DownloadFailed:
                    return PodcastItemActivity.Downloaded;
                    
                case FeedDownloadStatus.Downloading:
                    return PodcastItemActivity.Downloading;
                    
                case FeedDownloadStatus.Pending:
                    return PodcastItemActivity.DownloadPending;
                    
                case FeedDownloadStatus.Paused:
                    return PodcastItemActivity.DownloadPaused;

                default:
                    return PodcastItemActivity.None;   
                }
            }
        }
        
        public override string ArtworkId {
            get { return PodcastService.ArtworkIdFor (Feed); }
        }

#endregion

#region Constructors
    
        public PodcastTrackInfo () : base ()
        {
        }
        
        public PodcastTrackInfo (FeedItem feed_item) : base ()
        {
            Item = feed_item;
            SyncWithFeedItem ();
        }

#endregion

        public void Delete ()
        {
            Provider.Delete (this);
        }
        
        public override void IncrementPlayCount ()
        {
            base.IncrementPlayCount ();
            
            if (PlayCount > 0 && !Item.IsRead) {
                Item.IsRead = true;
                Item.Save ();
            }
        }
        
        public void SyncWithFeedItem ()
        {
            //Console.WriteLine ("Syncing item, enclosure == null? {0}", Item.Enclosure == null);
            ArtistName = Item.Author;
            AlbumTitle = Item.Feed.Title;
            TrackTitle = Item.Title;
            Year = Item.PubDate.Year;
            CanPlay = true;
            Genre = Genre ?? "Podcast";
            MediaAttributes |= TrackMediaAttributes.Podcast;
            ReleaseDate = Item.PubDate;
            MimeType = Item.Enclosure.MimeType;
            Duration = Item.Enclosure.Duration;
            FileSize = Item.Enclosure.FileSize;
            LicenseUri = Item.LicenseUri;
            Uri = new Banshee.Base.SafeUri (Item.Enclosure.LocalPath ?? Item.Enclosure.Url);
            
            if (!String.IsNullOrEmpty (Item.Enclosure.LocalPath)) {
                try {
                    TagLib.File file = Banshee.Streaming.StreamTagger.ProcessUri (Uri);
                    Banshee.Streaming.StreamTagger.TrackInfoMerge (this, file, true);
                } catch {}
            }
        }
        
        protected override void ProviderSave ()
        {
            Provider.Save (this);
        }
        
        protected override bool ProviderRefresh ()
        {
            return Provider.Refresh (this);
        }

        public static void DeleteWithFeedId (long feed_id)
        {
            /*PodcastItem item = Provider.FetchFirstMatching (String.Format (
                "primarysourceid = {0} and externalid = {1}", primary_id, feed_id
            ));

            if (item != null) {
                item.Delete ();
            }*/
        }

        /*public string Title {
            get { return item.Title; }
        }
        
        public string PodcastTitle {
            get { return item.Feed.Title; }
        }
        
        [VirtualDatabaseColumn ("PubDate", "PodcastItems", "TrackID", "TrackID")]
        public DateTime PubDate {
            get { return item.PubDate; }
            set { item.PubDate = value; }
        }
        
        [DatabaseColumn]
        public string Author {
            get { return item.Author; }
            set { item.Author = value; }
        }
        
        [DatabaseColumn ("FeedItemID", Constraints = DatabaseColumnConstraints.NotNull)]
        public long FeedItemID {
            get { return feedItemID; }
            private set { feedItemID = value; }
        }

        [DatabaseColumn ("New", Constraints = DatabaseColumnConstraints.NotNull)]   
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
        }        */
    }
}
