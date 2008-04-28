//
// RssParser.cs
//
// Authors:
//   Mike Urbanski <michael.c.urbanski@gmail.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Mike Urbanski
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Collections.Generic;

using Migo.Syndication;

namespace Migo.Syndication.Data
{
    public class RssParser
    {
        private XmlDocument doc;
        
        public RssParser (string url, string xml)
        {
            doc = new XmlDocument ();
            try {
                doc.LoadXml (xml);
            } catch (XmlException) {
                throw new FormatException ("Invalid xml document.");                                  
            }
            CheckRss ();
        }
        
        public RssParser (string url, XmlDocument doc)
        {
            this.doc = doc;
            CheckRss ();
        }
    
        public Feed CreateFeed ()
        {
            return UpdateFeed (new Feed ());
        }
        
        public Feed UpdateFeed (Feed feed)
        {
            try {
                feed.Copyright        = XmlUtils.GetXmlNodeText (doc, "/rss/channel/copyright");
                feed.Image            = XmlUtils.GetXmlNodeText (doc, "/rss/channel/image/url");
                feed.Description      = XmlUtils.GetXmlNodeText (doc, "/rss/channel/description");
                feed.Interval         = XmlUtils.GetInt64 (doc, "/rss/channel/interval"); 
                feed.Language         = XmlUtils.GetXmlNodeText (doc, "/rss/channel/language");
                feed.LastBuildDate    = XmlUtils.GetRfc822DateTime (doc, "/rss/channel/lastBuildDate");
                feed.Link             = XmlUtils.GetXmlNodeText (doc, "/rss/channel/link"); 
                feed.PubDate          = XmlUtils.GetRfc822DateTime (doc, "/rss/channel/pubDate");
                feed.Title            = XmlUtils.GetXmlNodeText (doc, "/rss/channel/title");
                feed.Ttl              = XmlUtils.GetInt64 (doc, "/rss/channel/ttl");
                feed.LastWriteTime    = DateTime.MinValue;
                feed.LastDownloadTime = DateTime.Now;

                return feed;
            } catch (Exception e) {
                 Hyena.Log.Exception (e);
            }
             
            return null;
        }
        
        public IEnumerable<FeedItem> GetFeedItems ()
        {
            XmlNodeList nodes = null;
            try {
                nodes = doc.SelectNodes ("//item");
            } catch (Exception e) {
                Hyena.Log.Exception (e);
            }
            
            if (nodes != null) {
                foreach (XmlNode node in nodes) {
                    FeedItem item = null;
                    
                    try {
                        item = ParseItem (node);
                    } catch (Exception e) {
                        Hyena.Log.Exception (e);
                    }
                    
                    if (item != null) {
                        yield return item;
                    }
                }
            }
        }
        
        public static FeedItem ParseItem (XmlNode node)
        {
            try {
                FeedItem item = new FeedItem ();
                item.Description = XmlUtils.GetXmlNodeText (node, "description");                        
                item.Title = XmlUtils.GetXmlNodeText (node, "title");                        
            
                if (String.IsNullOrEmpty (item.Description) && String.IsNullOrEmpty (item.Title)) {
                    throw new FormatException ("node:  Either 'title' or 'description' node must exist.");
                }
                
                item.Author            = XmlUtils.GetXmlNodeText (node, "author");
                item.Comments          = XmlUtils.GetXmlNodeText (node, "comments");
                item.Guid              = XmlUtils.GetXmlNodeText (node, "guid");
                item.Link              = XmlUtils.GetXmlNodeText (node, "link");
                item.Modified          = XmlUtils.GetRfc822DateTime (node, "dcterms:modified");
                item.PubDate           = XmlUtils.GetRfc822DateTime (node, "pubDate");
                item.LastDownloadTime  = DateTime.Now;
                
                item.Enclosure = ParseEnclosure (node);
                
                return item;
             } catch (Exception e) {
                 Hyena.Log.Exception (e);
             }
             
             return null;
        }
        
        public static FeedEnclosure ParseEnclosure (XmlNode node)
        {
            try {
                FeedEnclosure enclosure = new FeedEnclosure ();
                enclosure.Url = XmlUtils.GetXmlNodeText (node, "enclosure/@url");
                enclosure.Length = Math.Max (0, XmlUtils.GetInt64 (node, "enclosure/@length"));
                enclosure.MimeType = XmlUtils.GetXmlNodeText (node, "enclosure/@type");
                return enclosure;
             } catch (Exception e) {
                 Hyena.Log.Exception (e);
             }
             
             return null;
        }
        
        private void CheckRss ()
        {            
            if (doc.SelectSingleNode ("/rss") == null) {
                throw new FormatException ("Invalid rss document.");                                  
            }
            
            if (XmlUtils.GetXmlNodeText (doc, "/rss/channel/title") == String.Empty) {
                throw new FormatException (
                    "node: 'title', 'description', and 'link' nodes must exist."
                );                
            }
        }
    }
}
