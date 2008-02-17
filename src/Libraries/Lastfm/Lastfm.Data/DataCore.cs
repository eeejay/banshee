//
// DataCore.cs
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

