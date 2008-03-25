//
// Paths.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using Mono.Unix;

using Banshee.Configuration.Schema;
 
namespace Banshee.Base
{
    public class Paths
    {        
        public static string Combine (string first, params string [] components)
        {
            if (String.IsNullOrEmpty (first)) {
                throw new ArgumentException ("First component must not be null or empty", "first");
            } else if (components == null || components.Length < 1) {
                throw new ArgumentException ("One or more path components must be provided", "components");
            }
            
            string result = first;
            
            foreach (string component in components) {
                result = Path.Combine (result, component);
            }
            
            return result;
        }
        
        public static string MakePathRelativeToLibrary (string path)
        {
            string library_location = LibraryLocation; // TODO: Use CachedLibraryLocation?
            
            if (path.Length < library_location.Length + 1) {
                return null;
            }
            
            return path.StartsWith (library_location)
                ? path.Substring (library_location.Length + 1)
                : null;
        }
        
        private static string legacy_application_data = Path.Combine (Environment.GetFolderPath (
            Environment.SpecialFolder.ApplicationData), "banshee");
    
        public static string LegacyApplicationData {
            get { return legacy_application_data; }
        }
        
        private static string application_data = Path.Combine (Environment.GetFolderPath (
            Environment.SpecialFolder.ApplicationData), "banshee-1");
        
        public static string ApplicationData {
            get { 
                if (!Directory.Exists (application_data)) {
                    Directory.CreateDirectory (application_data);
                }
                
                return application_data; 
            }
        }
        
        private static string application_cache = Path.Combine (XdgBaseDirectorySpec.GetUserDirectory (
            "XDG_CACHE_HOME", ".cache"), "banshee-1");
        
        public static string ApplicationCache {
            get { return application_cache; }
        }
        
        public static string ExtensionCacheRoot {
            get { return Path.Combine (ApplicationCache, "extensions"); }
        }
        
        public static string DefaultLibraryPath {
            get { return XdgBaseDirectorySpec.GetUserDirectory ("XDG_MUSIC_DIR", "Music"); }
        }
        
        public static string TempDir {
            get {
                string dir = Path.Combine (ApplicationCache, "temp");
        
                if (File.Exists (dir)) {
                    File.Delete (dir);
                }
                
                Directory.CreateDirectory (dir);
                return dir;
            }
        }
        
        private static string cached_library_location;
        public static string LibraryLocation {
             get {
                string path = LibrarySchema.Location.Get (Paths.DefaultLibraryPath);
                if (String.IsNullOrEmpty (path)) {
                    path = Paths.DefaultLibraryPath;
                }
                
                LibraryLocation = path;
                return cached_library_location;
             }
             
             set {
                cached_library_location = value;
                LibrarySchema.Location.Set (cached_library_location); 
            }
        }
        
        public static string CachedLibraryLocation {
            get { return cached_library_location ?? LibraryLocation; }
        }
        
        private static string installed_application_prefix = null;
        public static string InstalledApplicationPrefix {
            get {
                if (installed_application_prefix == null) {
                    installed_application_prefix = Path.GetDirectoryName (
                        System.Reflection.Assembly.GetEntryAssembly ().Location);
                    DirectoryInfo entry_directory = new DirectoryInfo (installed_application_prefix);
                    
                    if (entry_directory != null && entry_directory.Parent != null && entry_directory.Parent.Parent != null) {
                        installed_application_prefix = entry_directory.Parent.Parent.FullName;
                    }
                }
                
                return installed_application_prefix;
            }
        }
        
        public static string InstalledApplicationDataRoot {
            get { return Path.Combine (InstalledApplicationPrefix, "share"); }
        }
        
        public static string InstalledApplicationData {
            get { return Path.Combine (InstalledApplicationDataRoot, "banshee-1"); }
        }
    }
}
