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
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;

namespace Banshee.Moblin
{
    public class RecentAlbumsView : Table
    {
        const int icon_size = 98;
        const int cols = 5;
        const int rows = 3;
        
        private class AlbumButton : Button
        {
            private Image image = new Image ();
            private AlbumInfo album;
            
            public AlbumButton ()
            {
                Relief = ReliefStyle.None;
                Add (image);
                image.Show ();
            }
            
            protected override void OnClicked ()
            {
                var source = ServiceManager.SourceManager.MusicLibrary;
                var other_source = ServiceManager.SourceManager.VideoLibrary;
                if (source != null) {
                    if (other_source != null) {
                        // HACK the Nereid search bar pulls the source's query value when
                        // the active source is changed, so artificially ensure that happens
                        ServiceManager.SourceManager.SetActiveSource (other_source);
                    }

                    source.FilterType = TrackFilterType.None;
                    source.FilterQuery = String.Format ("artist=\"{0}\" album=\"{1}\"", Album.ArtistName, Album.Title);
                    ServiceManager.SourceManager.SetActiveSource (source);
                    ServiceManager.Get<MoblinService> ().PresentPrimaryInterface ();

                    var player = ServiceManager.PlayerEngine;
                    if (!player.IsPlaying () || player.CurrentState == Banshee.MediaEngine.PlayerState.Paused) {
                        ServiceManager.PlaybackController.Source = source;
                        ServiceManager.PlaybackController.Next ();
                    }
                }
            }
            
            public Gdk.Pixbuf Pixbuf {
                get { return image.Pixbuf; }
                set { image.Pixbuf = value; }
            }
            
            public AlbumInfo Album {
                get { return album; }
                set {
                    album = value;
                    TooltipMarkup = String.Format ("<b><big>{0}</big></b>\n<i>{1}</i>",
                        GLib.Markup.EscapeText (album.DisplayTitle),
                        GLib.Markup.EscapeText (album.DisplayArtistName));
                }
            }
        }

        private RecentAlbumsList recent;
        private List<AlbumButton> buttons;

        public RecentAlbumsView () : base (5, 3, false)
        {
            RowSpacing = ColumnSpacing = 12;
            Build ();

            recent = new RecentAlbumsList (cols * rows);
            recent.Changed += (o, a) => Reload ();
            Reload ();
            
            NoShowAll = true;
        }

        private void Build ()
        {
            buttons = new List<AlbumButton> ();

            for (uint j = 0; j < rows; j++) {
                for (uint i = 0; i < cols; i++) {
                    var button = new AlbumButton ();
                    buttons.Add (button);
                    Attach (button, i, i + 1, j, j + 1);
                }
            }
            
            Show ();
        }

        public void Reload ()
        {
            var artwork = ServiceManager.Get<ArtworkManager> ();

            for (int i = 0; i < cols * rows; i++) {
                if (i >= recent.Albums.Count) {
                    buttons[i].Hide ();
                    continue;
                }
                
                var album = recent.Albums[i];
                buttons[i].Album = album;
                buttons[i].Pixbuf = artwork.LookupScalePixbuf (album.ArtworkId, icon_size);
                buttons[i].Show ();
            }
        }
    }
}
