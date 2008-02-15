//
// LastfmData.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
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
using System.IO;
using System.Net;
using System.Web;
using System.Xml;
using ICSharpCode.SharpZipLib.GZip;

using Hyena;

namespace Lastfm.Data
{
    public enum CacheDuration
    {
        None = 0,
        Normal,
        Infinite
    }

    public enum TopType {
        Overall,
        ThreeMonth,
        SixMonth,
        TwelveMonth,
    }

    public abstract class LastfmData
    {
        private const int CACHE_VERSION = 2;
        private static bool first_instance = true;

        public static string UserAgent = null; //Banshee.Web.Browser.UserAgent;
        public static string CachePath = null; //Path.Combine (Banshee.Base.Paths.UserPluginDirectory, "recommendation");
        public static TimeSpan NormalCacheTime = TimeSpan.FromHours (2);

        protected XmlDocument doc;
        protected string data_url;
        protected string cache_file;
        protected CacheDuration cache_duration;

        public LastfmData (string dataUrlFragment) : this (dataUrlFragment, CacheDuration.Normal)
        {
        }

        public LastfmData (string dataUrlFragment, CacheDuration cacheDuration)
        {
            if (CachePath == null || UserAgent == null) {
                throw new NotSupportedException ("LastfmData.CachePath and/or LastfmData.Useragent are null.  Applications must set this value.");
            }

            if (first_instance) {
                first_instance = false;
                CheckForCacheWipe();
                SetupCache();
            }

            this.data_url = HostInjectionHack (String.Format ("http://ws.audioscrobbler.com/1.0/{0}", dataUrlFragment));
            this.cache_file = GetCachedPathFromUrl (data_url);
            this.cache_duration = cacheDuration;

            // Download the content if necessary
            DownloadContent ();

            // Load the XML from the new or cached local file
            doc = new XmlDocument ();
            using (StreamReader reader = new StreamReader (cache_file)) {
                doc.Load (reader);
            }
        }

        public string DataUrl {
            get { return data_url; }
        }

        protected static string TopTypeToParam (TopType type)
        {
            switch (type) {
                case TopType.Overall:       return "overall";
                case TopType.ThreeMonth:    return "3month";
                case TopType.SixMonth:      return "6month";
                case TopType.TwelveMonth:   return "12month";
            }
            return null;
        }

#region Private methods

        private void DownloadContent ()
        {
            // See if we have a valid cached copy
            if (cache_duration != CacheDuration.None) {
                if (File.Exists (cache_file)) {
                    DateTime last_updated_time = File.GetLastWriteTime (cache_file);
                    if (cache_duration == CacheDuration.Infinite || DateTime.Now - last_updated_time < NormalCacheTime) {
                        return;
                    }
                }
            }
            
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (data_url);
            request.UserAgent = UserAgent;
            request.KeepAlive = false;
            
            using (HttpWebResponse response = (HttpWebResponse) request.GetResponse ()) {
                using (Stream stream = GetResponseStream (response)) {
                    using (FileStream file_stream = File.Open (cache_file, FileMode.Create)) {
                        using (BufferedStream buffered_stream = new BufferedStream (file_stream)) {
                            byte [] buffer = new byte[8192];
                            int read;
                            
                            while (true) {
                                read = stream.Read (buffer, 0, buffer.Length);
                                if (read <= 0) {
                                    break;
                                }
                                
                                buffered_stream.Write (buffer, 0, read);
                            }
                        }
                    }
                }
            }
        }

        private static Stream GetResponseStream (HttpWebResponse response) 
        {
            return response.ContentEncoding == "gzip"
                ? new GZipInputStream (response.GetResponseStream ())
                : response.GetResponseStream ();
        }

    
        private static string GetCachedPathFromUrl (string url)
        {
            string hash = url.GetHashCode ().ToString ("X").ToLower ();
            return Path.Combine (Path.Combine (CachePath, hash.Substring (0, 2)), hash);
        }

        // FIXME: This is to (try to) work around a bug in last.fm's XML
        // Some XML nodes with URIs as content do not return a URI with a
        // host name. It appears that in these cases the hostname is to be
        // static3.last.fm, but it may not always be the case - this is
        // a bug in last.fm, but we attempt to work around it here
        // http://bugzilla.gnome.org/show_bug.cgi?id=408068

        private static string HostInjectionHack (string url)
        {
            if (url.StartsWith ("http:///storable/")) {
                url = url.Insert (7, "static3.last.fm");
            }

            return url;
        }

        private void SetupCache()
        {
            bool clean = false;
            
            if(!Directory.Exists(CachePath)) {
                clean = true;
                Directory.CreateDirectory(CachePath);
            }
            
            // Create our cache subdirectories.
            for(int i = 0; i < 256; ++i) {
                string subdir = i.ToString("x");
                if(i < 16) {
                    subdir = "0" + subdir;
                }
                
                subdir = System.IO.Path.Combine(CachePath, subdir);
                
                if(!Directory.Exists(subdir)) {
                    Directory.CreateDirectory(subdir);
                }
            }
            
            //RecommendationPlugin.CacheVersion.Set (CACHE_VERSION);
            
            if(clean) {
                Log.Debug("Recommendation Plugin", "Created a new cache layout");
            }
        }

        private void CheckForCacheWipe()
        {
            //bool wipe = false;
            
            if(!Directory.Exists(CachePath)) {
                return;
            }
            
            /*if (RecommendationPlugin.CacheVersion.Get() < CACHE_VERSION) {
                Directory.Delete(CachePath, true);
                Log.Debug("Recommendation Plugin", "Destroyed outdated cache");
            }*/
        }

#endregion

    }
}

