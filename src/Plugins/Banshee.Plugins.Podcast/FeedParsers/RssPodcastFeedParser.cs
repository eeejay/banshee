/***************************************************************************
 *  RssPodcastFeedParser.cs
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
using System.Collections;

using Mono.Gettext;

namespace Banshee.Plugins.Podcast
{
    public class RssPodcastFeedParser : PodcastFeedParser
    {
        public RssPodcastFeedParser (XmlDocument doc, PodcastFeedInfo feed) : base (doc, feed) {}

        public override void Parse ()
        {
            if (xml_doc == null)
            {
                throw new NullReferenceException ("xml_doc");
            }

            LoadPodcastFeedFromXml ();
            LoadPodcastsFromXml ();
        }

        // Adapted from Monopod
        private void LoadPodcastFeedFromXml ()
        {
            title = StringUtils.StripHTML (FeedUtil.GetXmlNodeText (xml_doc, "/rss/channel/title")).Trim ();

            if (title == "")
            {
                throw new ApplicationException (Catalog.GetString("Feed has no title"));
            }

            feed_link = FeedUtil.GetXmlNodeText (xml_doc, "/rss/channel/link").Trim ();
            image_url = FeedUtil.GetXmlNodeText (xml_doc, "/rss/channel/image/url").Trim ();
            description = StringUtils.StripHTML (FeedUtil.GetXmlNodeText (xml_doc, "/rss/channel/description").Trim ());
        }

        // Adapted from Monopod
        private void LoadPodcastsFromXml ()
        {
            string enc_url;
            string mime_type;
            string pubDate;

            Hashtable tmp_podcast_hash = new Hashtable ();

            PodcastInfo tmpPi;
            XmlNodeList items = xml_doc.SelectNodes ("//item");

            foreach (XmlNode item in items)
            {
                try
                {
                    enc_url = FeedUtil.GetXmlNodeText (item, "enclosure/@url").Trim ();

                    if (enc_url == String.Empty)
                    {
                        continue;
                    }

                    mime_type = FeedUtil.GetXmlNodeText (item, "enclosure/@type").Trim ();

                    if (!FeedUtil.KnownType (mime_type))
                    {
                        continue;
                    }

                    tmpPi = new PodcastInfo (feed, enc_url);
                    tmpPi.MimeType = mime_type;

                    try
                    {
                        tmpPi.Length = Convert.ToInt64(
                                           FeedUtil.GetXmlNodeText (item, "enclosure/@length").Trim()
                                       );

                    }
                    catch {
                        tmpPi.Length = 0;
                    }

                    tmpPi.Title = StringUtils.StripHTML (FeedUtil.GetXmlNodeText (item, "title").Trim());

                    tmpPi.Author = StringUtils.StripHTML (FeedUtil.GetXmlNodeText (item, "author").Trim());

                    tmpPi.Link = FeedUtil.GetXmlNodeText (item, "link").Trim();

                    tmpPi.Description = StringUtils.StripHTML (FeedUtil.GetXmlNodeText (item, "description").Trim());

                    pubDate = FeedUtil.GetXmlNodeText (item, "pubDate").Trim();
                    
                    try {
                       tmpPi.PubDate = RFC822DateTime.Parse (pubDate);
                    } catch {
                        tmpPi.PubDate = DateTime.MinValue;
                    }

                    // Some feeds actually release multiple episodes w\ the same file name. Gah!
                    if (tmp_podcast_hash.Contains (enc_url))
                        {
                            continue;
                        }

                        tmp_podcast_hash.Add (enc_url, tmpPi);
                    }
                catch
                {
                    continue;
                }
            }

            if (tmp_podcast_hash.Values.Count > 0)
            {
                PodcastInfo[] tmp_podcasts = new PodcastInfo [tmp_podcast_hash.Values.Count];
                tmp_podcast_hash.Values.CopyTo (tmp_podcasts, 0);

                Array.Sort (tmp_podcasts);

                podcasts = tmp_podcasts;
            }
        }
    }
}
