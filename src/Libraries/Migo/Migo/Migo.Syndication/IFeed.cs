/*************************************************************************** 
 *  IFeed.cs
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Migo.Syndication
{    
    public interface IFeed : IComparable<IFeed>
    {
        event EventHandler<FeedEventArgs> FeedDeleted;
        event EventHandler<FeedDownloadCountChangedEventArgs> FeedDownloadCountChanged;                
        event EventHandler<FeedDownloadCompletedEventArgs> FeedDownloadCompleted;
        event EventHandler<FeedEventArgs> FeedDownloading;
        event EventHandler<FeedItemCountChangedEventArgs> FeedItemCountChanged;
        event EventHandler<FeedEventArgs> FeedRenamed;
        event EventHandler<FeedEventArgs> FeedUrlChanged;
        event EventHandler<FeedItemEventArgs> FeedItemAdded;
        event EventHandler<FeedItemEventArgs> FeedItemRemoved;  

        long ActiveDownloadCount { get; }        
        long QueuedDownloadCount { get; }        
        
        string Copyright { get; }
        string Description { get; }
        bool DownloadEnclosuresAutomatically { get; set; }
        FEEDS_DOWNLOAD_STATUS DownloadStatus { get; }
        string DownloadUrl { get; }     
        string Image { get; }

        // Unit of measure is in minutes.
        // The default interval is 1440 minutes (24 hours). 
        // The minimum download interval is 15 minutes.
        long Interval { get; set; }
        bool IsList { get; }
        long ItemCount { get; }
        ReadOnlyCollection<IFeedItem> Items { get; }
        string Language { get; }
        DateTime LastBuildDate { get; }
        FEEDS_DOWNLOAD_ERROR LastDownloadError { get; }
        DateTime LastDownloadTime { get; }      
        DateTime LastWriteTime { get; }
        string Link { get; }
        string LocalEnclosurePath { get; set; }
        long LocalID { get; }
        long MaxItemCount { get; set; }
        string Name { get; }
        IFeedsManager Parent { get; }
        DateTime PubDate { get; }
        FEEDS_SYNC_SETTING SyncSetting { get; }
        string Title { get; }
        
        // TTL is an optional feed element. The property value is 0 if not specified in the source.
        // The TTL value indicates how many minutes a feed should be cached before it is refreshed from the source.        
        long Ttl { get; }  
        long UnreadItemCount { get; }  
        string Url { get; set; }
        
        void AsyncDownload ();
        bool CancelAsyncDownload ();
        void Delete ();        
        void Delete (bool deleteEnclosures);        
        void Delete (IFeedItem item);       
        void Delete (IEnumerable<IFeedItem> items);
        void Download ();
        IFeedItem GetItem (long itemId);
        void MarkAllItemsRead (); 
    }   
}    
