/***************************************************************************
 *  PodcastFeedParser.cs
 *
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
using System.Xml;

namespace Banshee.Plugins.Podcast
{
    public abstract class PodcastFeedParser
    {
        protected XmlDocument xml_doc;

        protected string title;
        protected string description;
        protected string feed_link;
        protected string image_url;

        protected PodcastFeedInfo feed;
        protected PodcastInfo[] podcasts;

        protected PodcastFeedParser (XmlDocument doc, PodcastFeedInfo feed)
        {
            xml_doc = doc;
            podcasts = null;
            this.feed = feed;
        }

        public abstract void Parse ();

        public static PodcastFeedParser Create (XmlDocument doc, PodcastFeedInfo feed)
        {
            if (doc == null)
            {
                throw new ArgumentNullException ("doc");
            }
            else if (doc == null)
            {
                throw new ArgumentNullException ("feed");
            }

            // Only Rss is supported at this time, add logic for others when time is not short.
            return (PodcastFeedParser) new RssPodcastFeedParser (doc, feed);
        }

        public string Title {
            get
            {
                return title;
            }
        }

        public string Description {
            get
            {
                return description;
            }
        }

        public string Link {
            get
            {
                return feed_link;
            }
        }

        public string Image {
            get
            {
                return image_url;
            }
        }

        public PodcastFeedInfo Feed
        {
            get
            {
                return feed;
            }
        }

        public PodcastInfo[] Podcasts
        {
            get
            {
                return podcasts;
            }
        }
    }
}
