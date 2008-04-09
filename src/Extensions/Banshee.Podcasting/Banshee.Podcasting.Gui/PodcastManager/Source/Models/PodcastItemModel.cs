/***************************************************************************
 *  PodcastItemModel.cs
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

using Hyena.Data;
using Banshee.Podcasting.Data;

namespace Banshee.Podcasting.Gui
{
    public static class PodcastItemSortKeys
    {
        public const string PodcastTitle = "PodcastTitle";
        public const string PubDate = "PubDate";
        public const string Title = "Title";        
    }

    public class PodcastItemModel : FilterableListModel<PodcastItem>
    {      
        public PodcastItemModel ()
        {
        }
        
        public void FilterOnFeed (PodcastFeed feed)
        {
            if (feed == null || feed == PodcastFeed.All) {
                Filter = null;
            } else {
                Filter = delegate (PodcastItem item) {
                    return (item.Feed.FeedID == feed.FeedID);
                };            
            }
        }
        
        public void FilterOnFeeds (ICollection<PodcastFeed> feeds)
        {
            if (feeds != null) {
                Filter = delegate (PodcastItem item) {
                    return (feeds.Contains (item.Feed));
                };                           
            } else {
                Filter = null;
            }
        }       
        
        public override void Sort ()
        {
            lock (SyncRoot) {
                if (SortColumn == null) {
                    return;
                }
                
                switch (SortColumn.SortKey) {
                case PodcastItemSortKeys.PodcastTitle:
                    List.Sort (new PodcastTitleComparer (SortColumn.SortType));
                    break;
                case PodcastItemSortKeys.PubDate:
                    List.Sort (new PubDateComparer (SortColumn.SortType));
                    break;
                case PodcastItemSortKeys.Title:
                    List.Sort (new TitleComparer (SortColumn.SortType));
                    break;                    
                }  
            }
        }
        
        private class PodcastTitleComparer : SortTypeComparer<PodcastItem>
        {
            public PodcastTitleComparer (SortType type) : base (type)
            {
            }
            
            public override int Compare (PodcastItem lhs, PodcastItem rhs)
            {
                int ret = String.Compare (lhs.PodcastTitle, rhs.PodcastTitle);
                
                // Just incase two podcasts have the same title...
                // found this when I tinyurl'd a feed.
                if (ret == 0) {
                    ret = String.Compare (
                        lhs.Item.Parent.Url, rhs.Item.Parent.Url
                    );
                }

                if (ret == 0) {
                    ret = DateTime.Compare (rhs.PubDate, lhs.PubDate);
                } else if (SortType == SortType.Ascending) {
                    ret *= (-1);
                }
                
                return ret;
            }
        }
        
        private class PubDateComparer : SortTypeComparer<PodcastItem>
        {
            public PubDateComparer (SortType type) : base (type)
            {
            }
            
            public override int Compare (PodcastItem lhs, PodcastItem rhs)
            {
                int ret = DateTime.Compare (lhs.PubDate, rhs.PubDate);
                return (SortType == SortType.Ascending) ? ret * (-1) : ret;
            }
        }        
        
        private class TitleComparer : SortTypeComparer<PodcastItem>
        {
            public TitleComparer (SortType type) : base (type)
            {
            }
            
            public override int Compare (PodcastItem lhs, PodcastItem rhs)
            {
                int ret = String.Compare (lhs.Title, rhs.Title);
                return (SortType == SortType.Ascending) ? ret * (-1) : ret;
            }
        }        
    }
}