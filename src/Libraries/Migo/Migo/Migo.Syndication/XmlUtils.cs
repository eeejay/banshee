/*************************************************************************** 
 *  XmlUtils.cs
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

namespace Migo.Syndication
{        
    public static class XmlUtils
    {
        public static XmlNamespaceManager GetNamespaceManager (XmlDocument doc)
        {
            XmlNamespaceManager mgr = new XmlNamespaceManager (doc.NameTable);
            return mgr;
        }
    
        public static string GetXmlNodeText (XmlNode node, string tag)
        {
            XmlNode n = node.SelectSingleNode (tag);
            return (n == null) ? String.Empty : n.InnerText.Trim ();
        }
        
        public static string GetXmlNodeText (XmlNode node, string tag, XmlNamespaceManager mgr)
        {
            XmlNode n = node.SelectSingleNode (tag, mgr);
            return (n == null) ? String.Empty : n.InnerText.Trim ();
        }
        
        public static DateTime GetRfc822DateTime (XmlNode node, string tag)
        {
            DateTime ret = DateTime.MinValue;
            string result = XmlUtils.GetXmlNodeText (node, tag);

            if (!String.IsNullOrEmpty (result)) {
                Rfc822DateTime.TryParse (result, out ret);
            }
                    
            return ret;              
        }
        
        public static long GetInt64 (XmlNode node, string tag)
        {
            long ret = 0;
            string result = XmlUtils.GetXmlNodeText (node, tag);

            if (!String.IsNullOrEmpty (result)) {
                Int64.TryParse (result, out ret);
            }
                    
            return ret;              
        }
        
        
        public static TimeSpan GetITunesDuration (XmlNode node, XmlNamespaceManager mgr)
        {
            return GetITunesDuration (GetXmlNodeText (node, "itunes:duration", mgr));
        }
        
        public static TimeSpan GetITunesDuration (string duration)
        {
            if (String.IsNullOrEmpty (duration)) {
                return TimeSpan.Zero;
            }

            int hours = 0, minutes = 0, seconds = 0;
            string [] parts = duration.Split (':');
            
            if (parts.Length > 0)
                seconds = Int32.Parse (parts[parts.Length - 1]);
                
            if (parts.Length > 1)
                minutes = Int32.Parse (parts[parts.Length - 2]);
                
            if (parts.Length > 2)
                hours = Int32.Parse (parts[parts.Length - 3]);
            
            return TimeSpan.FromSeconds (hours * 3600 + minutes * 60 + seconds);
        }
    }
}
