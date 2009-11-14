//
// DataCore.cs
//
// Authors:
//   Gabriel Burt
//   Fredrik Hedberg
//   Aaron Bockover
//   Lukas Lipka
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
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.GZip;

using Hyena;

namespace Lastfm.Data
{
    public sealed class DataCore
    {
        private const int CACHE_VERSION = 2;
        public static string UserAgent = null; //Banshee.Web.Browser.UserAgent;
        public static string CachePath = null; //Path.Combine (Banshee.Base.Paths.UserPluginDirectory, "recommendation");
        public static TimeSpan NormalCacheTime = TimeSpan.FromHours (2);

        private static bool initialized = false;

        internal static void Initialize ()
        {
            if (!initialized) {
                initialized = true;

                if (CachePath == null || UserAgent == null) {
                    throw new NotSupportedException ("Lastfm.Data.DataCore.CachePath and/or Lastfm.Data.DataCore.Useragent are null.  Applications must set this value.");
                }

                CheckForCacheWipe();
                SetupCache();
            }
        }

        public static string DownloadContent (string data_url)
        {
            return DownloadContent (data_url, CacheDuration.Infinite);
        }

        public static string DownloadContent (string data_url, CacheDuration cache_duration)
        {
            return DownloadContent (data_url, GetCachedPathFromUrl (data_url), cache_duration);
        }

        internal static string DownloadContent (string data_url, string cache_file, CacheDuration cache_duration)
        {
            if (String.IsNullOrEmpty (data_url) || String.IsNullOrEmpty (cache_file)) {
                return null;
            }

            data_url = FixLastfmUrl (data_url);
            // See if we have a valid cached copy
            if (cache_duration != CacheDuration.None) {
                if (File.Exists (cache_file)) {
                    DateTime last_updated_time = File.GetLastWriteTime (cache_file);
                    if (cache_duration == CacheDuration.Infinite || DateTime.Now - last_updated_time < DataCore.NormalCacheTime) {
                        return cache_file;
                    }
                }
            }

            HttpWebRequest request = (HttpWebRequest) WebRequest.Create (data_url);
            request.UserAgent = DataCore.UserAgent;
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
            return cache_file;
        }


        public static string GetCachedPathFromUrl (string url)
        {
            if (url == null) {
                return null;
            }

            string hash = FixLastfmUrl (url).GetHashCode ().ToString ("X").ToLower ();
            if (hash.Length > 2) {
                return Path.Combine (Path.Combine (DataCore.CachePath, hash.Substring (0, 2)), hash);
            } else {
                return String.Empty;
            }
        }

        private static Stream GetResponseStream (HttpWebResponse response)
        {
            return response.ContentEncoding == "gzip"
                ? new GZipInputStream (response.GetResponseStream ())
                : response.GetResponseStream ();
        }

        // FIXME: This is to (try to) work around a bug in last.fm's XML
        // Some XML nodes with URIs as content do not return a URI with a
        // host name. It appears that in these cases the hostname is to be
        // static3.last.fm, but it may not always be the case - this is
        // a bug in last.fm, but we attempt to work around it here
        // http://bugzilla.gnome.org/show_bug.cgi?id=408068

        internal static string FixLastfmUrl (string url)
        {
            if (url.StartsWith ("http:///storable/")) {
                url = url.Insert (7, "static3.last.fm");
            }

            return url;
        }

        private static void SetupCache()
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

        private static void CheckForCacheWipe()
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
    }
}

