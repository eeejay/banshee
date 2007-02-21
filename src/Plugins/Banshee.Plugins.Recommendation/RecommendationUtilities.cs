/***************************************************************************
 *  RecommendationPlugin.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Fredrik Hedberg
 *             Aaron Bockover
 *             Lukas Lipka
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
using System.IO;
using System.Net;
using System.Web;

using Banshee.Database;

namespace Banshee.Plugins.Recommendation
{
    internal static class Utilities
    {
        internal static string CACHE_PATH = System.IO.Path.Combine(
            Banshee.Base.Paths.UserPluginDirectory, "recommendation");
        
        private static TimeSpan CACHE_TIME = TimeSpan.FromHours(2);
    
        internal static string GetCachedPathFromUrl(string url)
        {            
            string hash = url.GetHashCode().ToString("X").ToLower();
            return System.IO.Path.Combine(System.IO.Path.Combine(CACHE_PATH, hash.Substring(0, 2)), hash);
        }

        // FIXME: This is to (try to) work around a bug in last.fm's XML
        // Some XML nodes with URIs as content do not return a URI with a
        // host name. It appears that in these cases the hostname is to be
        // static3.last.fm, but it may not always be the case - this is
        // a bug in last.fm, but we attempt to work around it here
        // http://bugzilla.gnome.org/show_bug.cgi?id=408068

        internal static string HostInjectionHack(string url)
        {
            if(url.StartsWith("http:///storable/")) {
                url = url.Insert(7, "static3.last.fm");
            }

            return url;
        }

        internal static string RequestContent(string url)
        {
            string path = GetCachedPathFromUrl(HostInjectionHack(url));
            DownloadContent(url, path, false);

            StreamReader reader = new StreamReader(path);
            string content = reader.ReadToEnd();
            reader.Close();
            
            return content;
        }

        internal static void DownloadContent(string url, string path, bool static_content)
        {
            if(File.Exists(path)) {
                DateTime last_updated_time = File.GetLastWriteTime (path);
                if(static_content || DateTime.Now - last_updated_time < CACHE_TIME) {
                    return;
                }
            }
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HostInjectionHack(url));
            request.UserAgent = Banshee.Web.Browser.UserAgent;
            request.KeepAlive = false;
            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();

            FileStream file_stream = File.OpenWrite(path);
            BufferedStream buffered_stream = new BufferedStream(file_stream);

            byte [] buffer = new byte[8192];
            int read;
            
            while(true) {
                read = stream.Read(buffer, 0, buffer.Length);
                if(read <= 0) {
                    break;
                }
                
                buffered_stream.Write(buffer, 0, read);
            }

            buffered_stream.Close();
            response.Close();
        }

        internal static int GetTrackId(string artist, string title)
        {
            DbCommand command = new DbCommand(@"
                SELECT TrackId 
                    FROM Tracks 
                    WHERE Artist LIKE :artist 
                        AND Title LIKE :title 
                    LIMIT 1",
                "artist", artist,
                "title", title);

            object result = Banshee.Base.Globals.Library.Db.QuerySingle(command);
            return result == null ? -1 : (int)result;
        }
    }
}
