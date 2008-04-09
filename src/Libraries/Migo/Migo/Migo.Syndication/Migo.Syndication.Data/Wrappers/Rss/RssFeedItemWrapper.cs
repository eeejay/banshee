/*************************************************************************** 
 *  RssFeedItemWrapper.cs
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

namespace Migo.Syndication.Data
{        
    class RssFeedItemWrapper : IFeedItemWrapper
    {
        private string description;
        private bool enclosureParsed;
        private RssFeedEnclosureWrapper enclosure;  
        private XmlNode node;         
        private string title;         
       
        public bool Active 
        { 
            get { return true; }
        }        
        
        public string Author 
        { 
            get { return XmlUtils.GetXmlNodeText (node, "author"); }
        }
        
        public string Comments 
        { 
            get { return XmlUtils.GetXmlNodeText (node, "comments"); }
        }
        
        public string Description 
        { 
            get { return XmlUtils.GetXmlNodeText (node, "description"); }
        }
        
        public IFeedEnclosureWrapper Enclosure 
        {
            get { 
                if (!enclosureParsed) {
                    RssFeedEnclosureWrapper.TryCreateWrapper (node, out enclosure); 
                    enclosureParsed = true;
                }
                
                return enclosure;  
           }
        }
        
        public string Guid 
        { 
            get { return XmlUtils.GetXmlNodeText (node, "guid"); }
        }
        
        public bool IsRead 
        {
            get { return false; }   
        }
        
        public DateTime LastDownloadTime 
        {
            get { return DateTime.Now; }   
        }
        
        public string Link 
        { 
            get { return XmlUtils.GetXmlNodeText (node, "link"); }
        }
        
        public long LocalID 
        {
            get { return -1; }   
        }
        
        public DateTime Modified 
        { 
            get { return XmlUtils.GetRfc822DateTime (node, "dcterms:modified"); }
        }      

        public DateTime PubDate 
        { 
            get { return XmlUtils.GetRfc822DateTime (node, "pubDate"); }
        }       
        
        public string Title 
        { 
            get { return XmlUtils.GetXmlNodeText (node, "title"); }
        }           

        public static bool TryCreateWrapper (XmlNode node, out RssFeedItemWrapper result)
        {
            result = null;            
            bool ret = false;
            
            try {
                result = new RssFeedItemWrapper (node);
                ret = true;
            } catch {}
            
            return ret;
        }        

        public RssFeedItemWrapper (XmlNode node)
        {
            if (node == null) {
                throw new ArgumentNullException ("node");
            }
            
            description = XmlUtils.GetXmlNodeText (node, "description");                        
            title = XmlUtils.GetXmlNodeText (node, "title");                        
            
            if (description == String.Empty && title == String.Empty) {
                throw new FormatException (
                    "node:  Either 'title' or 'description' node must exist."
                );
            }
            
            this.node = node;
        }
    }   
}
