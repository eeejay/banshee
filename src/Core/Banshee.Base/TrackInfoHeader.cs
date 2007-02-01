/***************************************************************************
 *  TrackInfoHeader.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using Mono.Unix;
using Gtk;

namespace Banshee.Widgets
{
    public class TrackInfoHeader : HBox
    {
        private string artist;
        private string album;
        private string title;
        private string more_info_uri;
    
        private Label artist_album_label;
        private LinkLabel title_label;
        private CoverArtThumbnail cover;
        private VBox box;
        
        public TrackInfoHeader() : this(true, 36)
        {
        }
        
        public TrackInfoHeader(bool ellipsize, int size) : base()
        {
            ConstructWidget(ellipsize, size);
        }
        
        private void ConstructWidget(bool ellipsize, int size)
        {
            Spacing = 8;
            
            cover = new CoverArtThumbnail(size);
            cover.NoArtworkPixbuf = Banshee.Base.Branding.DefaultCoverArt;
            PackStart(cover, false, false, 0);
            cover.Hide();
        
            box = new VBox();
            box.Spacing = 2;
        
            artist_album_label = ellipsize ? new EllipsizeLabel() : new Label();
            artist_album_label.Show();
            artist_album_label.Xalign = 0.0f;
            artist_album_label.Yalign = 0.5f;
            artist_album_label.Selectable = true;
            
            title_label = new LinkLabel();
            if(ellipsize) {
                title_label.Ellipsize = Pango.EllipsizeMode.End;
            }
            
            title_label.Show();
            title_label.Xalign = 0.0f;
            title_label.Yalign = 0.5f;
            title_label.Selectable = true;
            title_label.ActAsLink = false;
            title_label.Open = Banshee.Web.Browser.Open;
            
            box.PackStart(title_label, false, false, 0);
            box.PackStart(artist_album_label, false, false, 0);
            
            PackStart(box, true, true, 0);
            box.ShowAll();
            Hide();
        }
        
        private void UpdateDisplay()
        {
            Gdk.Color blend = Banshee.Base.Utilities.ColorBlend(
                artist_album_label.Style.Background(StateType.Normal),
                artist_album_label.Style.Foreground(StateType.Normal));
            string hex_blend = String.Format("#{0:x2}{1:x2}{2:x2}", blend.Red, blend.Green, blend.Blue);
                        
            if(album == null || album == String.Empty) {
                artist_album_label.Markup = String.Format(
                    "<span color=\"{0}\">{1}</span>  {2}",
                    hex_blend, 
                    Catalog.GetString("by"),
                    GLib.Markup.EscapeText(artist));
            } else {
                artist_album_label.Markup = String.Format(
                    "<span color=\"{0}\">{1}</span>  {3}  <span color=\"{0}\">{2}</span>  {4}",
                    hex_blend, 
                    Catalog.GetString("by"),
                    Catalog.GetString("from"),
                    GLib.Markup.EscapeText(artist), 
                    GLib.Markup.EscapeText(album));
            }
            
            title_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(title));
            title_label.ActAsLink = more_info_uri != null;
            title_label.UriString = more_info_uri;
            
            ShowAll();
        }

        public string Artist {
            set {
                artist = value;
                UpdateDisplay();
            }
        }
        
        public string Title {
            set {
                title = value;
                UpdateDisplay();
            }
        }
        
        public string Album {
            set {
                album = value;
                UpdateDisplay();
            }
        }
        
        public string MoreInfoUri {
            set {
                more_info_uri = value;
                UpdateDisplay();
            }
        }
        
        public CoverArtThumbnail Cover {
            get { return cover; }
        }
        
        public Gdk.Pixbuf DefaultCover {
            set {
                if(value != null) {
                    cover.NoArtworkPixbuf = value;
                    cover.Show();
                } else {
                    cover.Hide();
                }
            }
        }

        public bool Selectable {
            set {
                artist_album_label.Selectable = value;
                title_label.Selectable = value;
            }
        }
        
        public VBox VBox {
            get { return box; }
        }
        
        public void SetIdle()
        {
            box.Hide();
        }
    }
}
