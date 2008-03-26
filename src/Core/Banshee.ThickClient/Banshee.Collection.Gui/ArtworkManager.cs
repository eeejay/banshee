//
// ArtworkManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Mono.Unix;

using Gdk;

using Hyena;
using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ArtworkManager : IService
    {
        public ArtworkManager ()
        {
            try {
                MigrateLegacyAlbumArt ();
            } catch (Exception e) {
                Log.Error ("Could not migrate old album artwork to new location.", e.Message);
            }
        }
        
        private void MigrateLegacyAlbumArt ()
        {
            if (Directory.Exists (CoverArtSpec.RootPath)) {
                return;
            }
            
            // FIXME: Replace with Directory.Move for release
            
            Directory.CreateDirectory (CoverArtSpec.RootPath);
            
            string legacy_artwork_path = Path.Combine (Paths.LegacyApplicationData, "covers");
            int artwork_count = 0;
            
            if (Directory.Exists (legacy_artwork_path)) {
                foreach (string path in Directory.GetFiles (legacy_artwork_path)) {
                    string dest_path = Path.Combine (CoverArtSpec.RootPath, Path.GetFileName (path));
                        
                    File.Copy (path, dest_path);
                    artwork_count++;
                }
            }
            
            Log.Debug (String.Format ("Migrated {0} album art images.", artwork_count));
        }
        
        public Pixbuf Lookup (string artist, string album)
        {
            return Lookup (CoverArtSpec.CreateArtistAlbumId (artist, album));
        }
        
        public Pixbuf Lookup (string id)
        {
            return LookupScale (id, 0);
        }
        
        public Pixbuf LookupScale (string id, int size)
        {
            if (id == null) {
                return null;
            }
            
            string path = CoverArtSpec.GetPathForSize (id, size);
            if (File.Exists (path)) {
                if (Path.GetExtension (path) == "cover") {
                    try {
                        Pixbuf pixbuf = new Pixbuf (path);
                        if (pixbuf.Width < 50 || pixbuf.Height < 50) {
                            File.Delete (path);
                            return null;
                        }
                        
                        pixbuf.Save (Path.ChangeExtension (path, "jpg"), "jpeg");
                        return pixbuf;
                    } catch {
                        File.Delete (path);
                    }
                    
                    return null;
                }
                
                try {
                    return new Pixbuf (path);
                } catch {
                    return null;
                }
            }
            
            string orig_path = CoverArtSpec.GetPathForSize (id, 0);
            if (File.Exists (orig_path)) {
                try {
                    Pixbuf pixbuf = new Pixbuf (orig_path);
                    Pixbuf scaled_pixbuf = pixbuf.ScaleSimple (size, size, Gdk.InterpType.Bilinear);
                    Directory.CreateDirectory (Path.GetDirectoryName (path));
                    scaled_pixbuf.Save (path, "jpeg");
                    ArtworkRenderer.DisposePixbuf (pixbuf);
                    return scaled_pixbuf;
                } catch {}
            }
            
            return null;
        }
        
        string IService.ServiceName {
            get { return "ArtworkManager"; }
        }
    }
}
