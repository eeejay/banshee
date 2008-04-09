/*************************************************************************** 
 *  RssFeedWrapper.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/
 
/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted,free of charge,to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"), 
 *  to deal in the Software without restriction,including without limitation  
 *  the rights to use,copy,modify,merge,publish,distribute,sublicense, 
 *  and/or sell copies of the Software,and to permit persons to whom the  
 *  Software is furnished to do so,subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS",WITHOUT WARRANTY OF ANY KIND,EXPRESS OR 
 *  IMPLIED,INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,DAMAGES OR OTHER 
 *  LIABILITY,WHETHER IN AN ACTION OF CONTRACT,TORT OR OTHERWISE,ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Xml;
using System.Collections.Generic;

namespace Migo.Syndication.Data
{        
    class RssFeedWrapper : IFeedWrapper
    {
        private XmlDocument doc; 
        
        private string url;
        private List<IFeedItemWrapper> items;        
        
        public string Copyright 
        { 
            get { return XmlUtils.GetXmlNodeText (doc, "/rss/channel/copyright"); }
        }
        
        public string Description 
        { 
            get { return XmlUtils.GetXmlNodeText (doc, "/rss/channel/description"); }
        }

        public string Image 
        { 
            get { return XmlUtils.GetXmlNodeText (doc, "/rss/channel/image/url"); }
        }

        public long Interval        
        { 
            get { return XmlUtils.GetInt64 (doc, "/rss/channel/interval"); }
        }

        public bool IsList 
        { 
            get { return false; }
        }
        
        public IEnumerable<IFeedItemWrapper> Items
        {
            get {
                if (items == null) {
                    XmlNodeList nodes = doc.SelectNodes ("//item");

                    if (nodes != null) {
                        RssFeedItemWrapper tmpWrapper;                
                        items = new List<IFeedItemWrapper> ();                
                        
                        foreach (XmlNode n in nodes) {
                            if (RssFeedItemWrapper.TryCreateWrapper (n, out tmpWrapper)) {
                                items.Add (tmpWrapper);                             
                            }
                        }
                    }                      
                }
                
                return items;
            }   
        }
        
        public string Language 
        { 
            get { return XmlUtils.GetXmlNodeText (doc, "/rss/channel/language"); }
        }
        
        public DateTime LastBuildDate 
        { 
            get { return XmlUtils.GetRfc822DateTime (doc, "/rss/channel/lastBuildDate"); }
        }

        public string Link 
        { 
            get { return XmlUtils.GetXmlNodeText (doc, "/rss/channel/link"); }
        }

        public string Name 
        {
            get { return Title; }     
        }        
        
        public DateTime PubDate 
        { 
            get { return XmlUtils.GetRfc822DateTime (doc, "/rss/channel/pubDate"); }
        }

        public string Title 
        { 
            get { return XmlUtils.GetXmlNodeText (doc, "/rss/channel/title"); }
        }
                
        public long Ttl 
        { 
            get { return XmlUtils.GetInt64 (doc, "/rss/channel/ttl"); }
        }  
        
        public string DownloadUrl 
        {
            get { return url; }   
        }        

        public string Url
        {
            get { return url; }   
        }
        
        /* IFeedWrapper interface specific properties not used during update */

        public FEEDS_SYNC_SETTING SyncSetting
        {
            get { return FEEDS_SYNC_SETTING.FSS_DEFAULT; }   
        }        
        
        public long MaxItemCount
        {
            get { return 200; }   
        }

        public long LocalID
        {
            get { return (-1); }   
        }        
        
        public string LocalEnclosurePath 
        {
            get { return String.Empty; }     
        }        
        
        public DateTime LastWriteTime 
        {
            get { return DateTime.MinValue; }     
        }             
        
        public DateTime LastDownloadTime 
        {
            get { return DateTime.Now.ToUniversalTime (); }     
        }           
        
        public FEEDS_DOWNLOAD_ERROR LastDownloadError
        {
            get { return FEEDS_DOWNLOAD_ERROR.FDE_NONE; }     
        }             
        
        public bool DownloadEnclosuresAutomatically 
        {
            get { return false; }   
        }
        
        /* End */
        
        public RssFeedWrapper (string url, string xml)
        {
            if (String.IsNullOrEmpty (xml)) {
                throw new ArgumentException ("xml:  Cannot be null or empty.");
            } else if (String.IsNullOrEmpty (url)) {
                throw new ArgumentException ("url:  Cannot be null or empty.");
            }
           
            doc = new XmlDocument ();
            
            try {
                doc.LoadXml (xml);
            } catch (XmlException) {
                throw new FormatException ("Invalid xml document.");                                  
            }
            
            this.url = url;
            
            CheckRss (); 
            Init ();            
        }
        
        public RssFeedWrapper (string url, XmlDocument doc)
        {
            if (doc != null) {
                throw new ArgumentNullException ("doc");
            } else if (String.IsNullOrEmpty (url)) {
                throw new ArgumentException ("url:  Cannot be null or empty.");
            }
            
            this.doc = doc;
            this.url = url;
            
            CheckRss ();
            Init ();
        }        
            
        private void CheckRss ()
        {            
            if (doc.SelectSingleNode ("/rss") == null) {
                throw new FormatException ("Invalid rss document.");                                  
            }   
        }
        
        private void Init ()
        {
            if (XmlUtils.GetXmlNodeText (doc, "/rss/channel/title") == String.Empty) {
                throw new FormatException (
                    "node: 'title', 'description', and 'link' nodes must exist."
                );                
            }
        }
        
        public static bool TryCreateWrapper (string url, string xml, out RssFeedWrapper result)
        {
            if (String.IsNullOrEmpty (xml)) {
                throw new ArgumentException ("xml:  Cannot be null or empty.");
            }
            
            XmlDocument doc = new XmlDocument ();
            doc.LoadXml (xml);
            
            return TryCreateWrapper (url, doc, out result);
        }
        
        public static bool TryCreateWrapper (string url, XmlDocument doc, out RssFeedWrapper result)
        {
            result = null;            
            bool ret = false;
            
            try {
                result = new RssFeedWrapper (url, doc);
                ret = true;
            } catch {}
            
            return ret;
        }        
    }
}
