//
// PhotoFolderImportSource.cs
//
// Author:
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
using Mono.Unix;
using Gtk;

using Banshee.Base;

namespace Banshee.Library.Gui
{
    public class PhotoFolderImportSource : IImportSource
    {
        private string [] photo_folders;
        
        public PhotoFolderImportSource ()
        {
            string personal = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
            string desktop = Environment.GetFolderPath (Environment.SpecialFolder.Desktop);
            
            photo_folders = new string [] {
                Environment.GetFolderPath (Environment.SpecialFolder.MyPictures),
                Paths.Combine (desktop, "Photos"), Paths.Combine (desktop, "photos"),
                Paths.Combine (personal, "Photos"), Paths.Combine (personal, "photos")
            };
            
            // Make sure we don't accidentally scan the entire home or desktop directory
            for (int i = 0; i < photo_folders.Length; i++) {
                if (photo_folders[i] == personal || photo_folders[i] == desktop) {
                    photo_folders[i] = null;
                }
            }
        }
    
        public void Import ()
        {
            Hyena.Log.DebugFormat ("Importing photo folder: {0}", PhotoFolder);
            Banshee.ServiceStack.ServiceManager.Get<LibraryImportManager> ().Enqueue (PhotoFolder);
        }
        
        public string Name {
            get { return Catalog.GetString ("Videos from Photos Folder"); }
        }
        
        public string [] IconNames {
            get { return new string [] { "gtk-open" }; }
        }
        
        public bool CanImport {
            get { return PhotoFolder != null; }
        }
        
        private string PhotoFolder {
            get {
                foreach (string folder in photo_folders) {
                    if (folder != null && Banshee.IO.Directory.Exists (folder)) {
                        return folder;
                    }
                }
                return null;
            }
        }
        
        public int SortOrder {
            get { return 0; }
        }
    }
}
