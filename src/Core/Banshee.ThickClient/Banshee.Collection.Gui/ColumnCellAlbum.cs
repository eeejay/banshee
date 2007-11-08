//
// ColumnCellAlbum.cs
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
using Gtk;
using Cairo;

using Hyena.Data.Gui;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellAlbum : ColumnCell
    {
        private static int pixbuf_size = 52;
        private static int pixbuf_spacing = 4;
        
        private static Gdk.Pixbuf default_cover_pixbuf = Gdk.Pixbuf.LoadFromResource("browser-album-cover.png");
        
        public static int RowHeight {
            get { return 60; }
        }
    
        private Pango.Layout album_layout;
        private Pango.Layout artist_layout;
        
        private ArtworkManager artwork_manager;

        public ColumnCellAlbum() : base(true, 0)
        {
            artwork_manager = ServiceManager.Get<ArtworkManager> ("ArtworkManager");
        }
    
        public unsafe override void Render(Gdk.Drawable window, Cairo.Context cr, Widget widget, Gdk.Rectangle cell_area, 
            Gdk.Rectangle clip_area, StateType state)
        {
            if(BoundObject == null) {
                return;
            }
            
            if(!(BoundObject is AlbumInfo)) {
                throw new InvalidCastException("ColumnCellAlbum can only bind to AlbumInfo objects");
            }
            
            AlbumInfo album = (AlbumInfo)BoundObject;
            
            if(album_layout == null) {
                album_layout = new Pango.Layout(widget.PangoContext);
                album_layout.FontDescription = widget.PangoContext.FontDescription.Copy();
                album_layout.FontDescription.Weight = Pango.Weight.Bold;
            }
            
            if(artist_layout == null) {
                artist_layout = new Pango.Layout(widget.PangoContext);
            }
        
            int text_height, text_width;
            
            Gdk.Rectangle pixbuf_area = new Gdk.Rectangle();
            pixbuf_area.X = cell_area.X + pixbuf_spacing;
            pixbuf_area.Y = cell_area.Y + pixbuf_spacing;
            pixbuf_area.Width = pixbuf_size;
            pixbuf_area.Height = pixbuf_size;
                        
            Gdk.Pixbuf pixbuf = artwork_manager == null ? null : artwork_manager.Lookup(album.ArtworkId);
            
            if(pixbuf != null) {
                Gdk.Pixbuf scaled_pixbuf = pixbuf.ScaleSimple(pixbuf_area.Width - 2, 
                    pixbuf_area.Height - 2, Gdk.InterpType.Bilinear);
                
                window.DrawRectangle(widget.Style.BlackGC, true, pixbuf_area);
                window.DrawPixbuf(widget.Style.BackgroundGC(StateType.Normal), scaled_pixbuf,
                    0, 0, pixbuf_area.X + 1, pixbuf_area.Y + 1, scaled_pixbuf.Width, scaled_pixbuf.Height,
                    Gdk.RgbDither.Normal, 0, 0);
                
                /*window.DrawPixbuf(widget.Style.BackgroundGC(StateType.Normal), scaled_pixbuf,
                    0, 0, pixbuf_area.X + pixbuf_area.Width, pixbuf_area.Y, pixbuf_area.Width / 3, pixbuf_area.Height,
                    Gdk.RgbDither.Normal, 0, 0);
                
                LinearGradient grad = new LinearGradient(pixbuf_area.X + pixbuf_area.Width, pixbuf_area.Y, 
                    pixbuf_area.X + pixbuf_area.Width * 1.3, pixbuf_area.Y);
                grad.AddColorStop(0, new Color(0, 0, 0, 0.3));
                grad.AddColorStop(1, new Color(0, 0, 0, 1));
                
                cr.Rectangle(pixbuf_area.X + pixbuf_area.Width, pixbuf_area.Y, pixbuf_area.Width / 3, pixbuf_area.Height);
                cr.Clip();
                cr.Mask(grad);
                cr.Fill();*/
            } else {
                window.DrawPixbuf(widget.Style.BackgroundGC(StateType.Normal), default_cover_pixbuf,
                    0, 0, pixbuf_area.X + 5, pixbuf_area.Y + 6, default_cover_pixbuf.Width, default_cover_pixbuf.Height,
                    Gdk.RgbDither.Normal, 0, 0);
            }
            
            album_layout.SetText(album.Title);
            album_layout.GetPixelSize(out text_width, out text_height);
            
            Style.PaintLayout(widget.Style, window, state, true, clip_area, widget, "column",
                pixbuf_area.X + pixbuf_area.Width + pixbuf_spacing, 
                pixbuf_area.Y + 4, album_layout);
            
            if(!String.IsNullOrEmpty(album.ArtistName)) {
                artist_layout.SetMarkup(String.Format("<small>{0}</small>", GLib.Markup.EscapeText(album.ArtistName)));
            
                Style.PaintLayout(widget.Style, window, state, true, clip_area, widget, "column",
                    pixbuf_area.X + pixbuf_area.Width + pixbuf_spacing, 
                    pixbuf_area.Y + text_height + 4, artist_layout);
            }
        }
    }
}
