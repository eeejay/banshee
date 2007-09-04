//
// ArtworkManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Gdk;

namespace Banshee.Collection.Gui
{
    public class ArtworkManager
    {
        private static ArtworkManager instance;
        public static ArtworkManager Instance {
            get { 
                if(instance == null) {
                    instance = new ArtworkManager();
                }
                
                return instance;
            }
        }
        
        private Dictionary<string, Pixbuf> artwork = new Dictionary<string, Pixbuf>();
        
        public ArtworkManager()
        {
        }
        
        public Pixbuf Lookup(string artist, string album)
        {
            return Lookup(AlbumInfo.CreateArtistAlbumId(artist, album));
        }
        
        public Pixbuf Lookup(string id)
        {
            if(id == null) {
                return null;
            }
            
            if(artwork.ContainsKey(id)) {
                return artwork[id];
            }
            
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                Path.Combine("album-art", String.Format("{0}.jpg", id)));
            
            if(File.Exists(path)) {
                Pixbuf pixbuf = new Pixbuf(path);
                artwork.Add(id, pixbuf);
                return pixbuf;
            }
            
            return null;
        }
    }
}
