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
using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellAlbum : ColumnCell
    {
        private static int pixbuf_size = 48;
        private static int pixbuf_spacing = 4;
        
        private static Gdk.Pixbuf default_cover_pixbuf = IconThemeUtils.LoadIcon (48, "media-optical");
        
        public static int RowHeight {
            get { return 54; }
        }
    
        private ArtworkManager artwork_manager;

        public ColumnCellAlbum () : base (true, 0)
        {
            artwork_manager = ServiceManager.Get<ArtworkManager> ("ArtworkManager");
        }
    
        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            if (BoundObject == null) {
                return;
            }
            
            if (!(BoundObject is AlbumInfo)) {
                throw new InvalidCastException("ColumnCellAlbum can only bind to AlbumInfo objects");
            }
            
            AlbumInfo album = (AlbumInfo)BoundObject;
            
            Gdk.Pixbuf pixbuf = artwork_manager == null ? null : artwork_manager.Lookup (album.ArtworkId);
            bool is_default = false;
            int pixbuf_size = (int)cellHeight - 8;
            int x = pixbuf_spacing;
            int y = ((int)cellHeight - pixbuf_size) / 2;
            
            if (pixbuf == null) {
                pixbuf = default_cover_pixbuf;
                is_default = true;
            } else {
                pixbuf = pixbuf.ScaleSimple (pixbuf_size, pixbuf_size, Gdk.InterpType.Bilinear);
            }
            
            ArtworkRenderer.RenderThumbnail (context.Context, pixbuf, x, y, pixbuf_size, pixbuf_size, 
                !is_default, ListViewGraphics.BorderRadius);
            
            int fl_width = 0, fl_height = 0, sl_width = 0, sl_height = 0;
            
            Pango.Layout first_line_layout = context.Layout;
            
            first_line_layout.Width = (int)((cellWidth - cellHeight - x - 10) * Pango.Scale.PangoScale);
            first_line_layout.Ellipsize = Pango.EllipsizeMode.End;
            first_line_layout.FontDescription = context.Widget.PangoContext.FontDescription.Copy ();
            
            Pango.Layout second_line_layout = first_line_layout.Copy ();
            
            first_line_layout.FontDescription.Weight = Pango.Weight.Bold;
            second_line_layout.FontDescription.Size = (int)(second_line_layout.FontDescription.Size * Pango.Scale.Small);
            
            first_line_layout.SetText (album.Title);
            first_line_layout.GetPixelSize (out fl_width, out fl_height);
            
            if (album.ArtistName != null) {
                second_line_layout.SetText (album.ArtistName);
                second_line_layout.GetPixelSize (out sl_width, out sl_height);
            }
            
            x = ((int)cellHeight - x) + 10;
            y = (int)((cellHeight - (fl_height + sl_height)) / 2);
            
            Style.PaintLayout (context.Widget.Style, context.Drawable, state, true, 
                context.Area, context.Widget, "text",
                context.Area.X + x, context.Area.Y + y, first_line_layout);
            
            if (album.ArtistName != null) {
                Style.PaintLayout (context.Widget.Style, context.Drawable, state, true, 
                    context.Area, context.Widget, "text",
                    context.Area.X + x, context.Area.Y + y + fl_height, second_line_layout);
            }        
        }
    }
}
