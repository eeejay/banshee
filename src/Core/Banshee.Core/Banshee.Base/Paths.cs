//
// Paths.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
        // TODO: A corlib version of this method will be committed to Mono's Environment.GetFolderPath,
        // so many many Mono versions in the future we will be able to drop this private copy and
        // use the pure .NET API - but it's here to stay for now (compat!)
        private static string ReadXdgUserDir (string key, string fallback)
        {
            string home_dir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            string config_dir = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
            
            string env_path = Environment.GetEnvironmentVariable (key);
            if (!String.IsNullOrEmpty (env_path)) {
                return env_path;
            }

            string user_dirs_path = Path.Combine (config_dir, "user-dirs.dirs");

            if (!File.Exists (user_dirs_path)) {
                return Path.Combine (home_dir, fallback);
            }

            try {
                using (StreamReader reader = new StreamReader (user_dirs_path)) {
                    string line;
                    while ((line = reader.ReadLine ()) != null) {
                        line = line.Trim ();
                        int delim_index = line.IndexOf ('=');
                        if (delim_index > 8 && line.Substring (0, delim_index) == key) {
                            string path = line.Substring (delim_index + 1).Trim ('"');
                            bool relative = false;

                            if (path.StartsWith ("$HOME/")) {
                                relative = true;
                                path = path.Substring (6);
                            } else if (path.StartsWith ("~")) {
                                relative = true;
                                path = path.Substring (1);
                            } else if (!path.StartsWith ("/")) {
                                relative = true;
                            }

                            return relative ? Path.Combine (home_dir, path) : path;
                        }
                    }
                }
            } catch (FileNotFoundException) {
            }
            
            return Path.Combine (home_dir, fallback);
        }
        
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
    
        public static string LegacyApplicationData {
            get {
                return Environment.GetFolderPath (Environment.SpecialFolder.Personal)
                    + Path.DirectorySeparatorChar
                    + ".gnome2"
                    + Path.DirectorySeparatorChar
                    + "banshee"
                    + Path.DirectorySeparatorChar;
            }
        }
        
        private static string application_data = Path.Combine (Environment.GetFolderPath (
            Environment.SpecialFolder.ApplicationData), "banshee");
        
        public static string ApplicationData {
            get { return application_data; }
        }
        
        public static string DefaultLibraryPath {
            get { return ReadXdgUserDir ("XDG_MUSIC_DIR", "Music"); }
        }
        
        public static string TempDir {
            get {
                string dir = Path.Combine (Paths.ApplicationData, "temp");
        
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
    }
}
