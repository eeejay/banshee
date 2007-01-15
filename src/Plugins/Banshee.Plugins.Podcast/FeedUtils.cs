/***************************************************************************
 *  FeedUtils.cs
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
using System.Net;
using System.IO;
using System.Xml;

namespace Banshee.Plugins.Podcast
{
    public static class FeedUtil
    {        
        // -- Adapted from Monopod
        public static bool KnownType (string type)
        {
/*          switch (type)
            {
              case "audio/mpeg":
                case "x-audio/mp3":
                case "audio/ogg":
                case "audio/m4a":
                case "audio/x-m4a":
                case "application/ogg":
                case "audio/x-wav":

                case "":
                    return true;
                default:
                    return true;
            }
*/
            // Don't like this one bit.
            return true;            
        }

        // -- Adapted from Monopod
        public static XmlDocument FetchXmlDocument (string uri, DateTime lastUpdated)
        {
            HttpWebResponse resp = null;

            try
            {
                string xmltext;

                resp = GetURI (uri, lastUpdated);

                if (resp == null)
                {
                    return null;
                }

                if (!(resp.StatusCode == HttpStatusCode.OK))
                {
                    if (resp != null)
                    {
                        resp.Close ();
                    }

                    return null;
                }

                StreamReader input = new StreamReader (resp.GetResponseStream ());
                xmltext = input.ReadToEnd ();
                input.Close ();

                XmlDocument xml = new XmlDocument ();

                xml.LoadXml (xmltext);

                return xml;

            }
            catch (WebException we)
            {
                Console.WriteLine (we.Message);
                HttpStatusCode resp_code = 0;

                if (we.Response != null)
                {
                    resp_code = ((HttpWebResponse) we.Response).StatusCode;
                    we.Response.Close ();
                }

                if (resp_code == HttpStatusCode.NotModified)
                {
                    throw new FeedNotModifiedException ();
                }

                return null;
            }
        }

        // -- Adapted from Monopod
        public static HttpWebResponse GetURI (string uri, DateTime lastUpdated)
        {
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (uri);
            request.Timeout = (30 * 1000); // 30 seconds
            request.UserAgent = "Banshee"; // Should be a global
            request.IfModifiedSince = lastUpdated;

            return (HttpWebResponse) request.GetResponse ();
        }

        // -- From Monopod
        public static string GetXmlNodeText (XmlNode xml, string tag)
        {
            XmlNode node;

            node = xml.SelectSingleNode (tag);

            if (node == null)
            {
                return String.Empty;
            }

            return node.InnerText;
        }
    }
}
