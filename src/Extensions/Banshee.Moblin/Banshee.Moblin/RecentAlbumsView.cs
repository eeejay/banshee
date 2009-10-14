// 
// RecentAlbumsView.cs
//  
// Author:
//   Gabriel Burt <gburt@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using Gtk;

using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;

namespace Banshee.Moblin
{
    public class RecentAlbumsView : Table
    {
        const int icon_size = 48;
        const int cols = 3;
        const int rows = 4;

        private RecentAlbumsList recent;
        private List<Image> images;

        public RecentAlbumsView () : base (3, 4, true)
        {
            RowSpacing = ColumnSpacing = 12;
            Build ();

            recent = new RecentAlbumsList (cols*rows);
            recent.Changed += (o, a) => Reload ();
            Reload ();
        }

        private void Build ()
        {
            images = new List<Image> ();

            for (uint j = 0; j < rows; j++) {
                for (uint i = 0; i < cols; i++) {
                    var image = new Image ();
                    images.Add (image);
                    Attach (image, i, i+1, j, j+1);
                }
            }

            ShowAll ();
        }

        public void Reload ()
        {
            var artwork = ServiceManager.Get<ArtworkManager> ();

            for (int i = 0; i < recent.Albums.Count; i++) {
                var album = recent.Albums[i];
                images[i].Pixbuf = artwork.LookupScalePixbuf (album.ArtworkId, icon_size);
                images[i].Show ();
            }

            for (int i = recent.Albums.Count; i < cols*rows; i++) {
                images[i].Hide ();
            }
        }
    }
}
