//
// ColumnCellPlaybackIndicator.cs
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

using Banshee.Gui;
using Hyena.Data.Gui;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellPlaybackIndicator : ColumnCell
    {
        private const int pixbuf_size = 16;
        private const int pixbuf_spacing = 4;
        
        private bool is_header;
        private Gdk.Pixbuf pixbuf; 
        
        public ColumnCellPlaybackIndicator (bool isHeader, int fieldIndex) : base (true, fieldIndex)
        {
            is_header = isHeader;
            LoadPixbuf ();
        }
        
        private void LoadPixbuf ()
        {
            if (pixbuf != null) {
                pixbuf.Dispose ();
                pixbuf = null;
            }
            
            pixbuf = is_header 
                ? IconThemeUtils.LoadIcon (pixbuf_size, "audio-volume-high")
                : IconThemeUtils.LoadIcon (pixbuf_size, "media-playback-start");
        }
        
        public override void NotifyThemeChange ()
        {
            LoadPixbuf ();
        }

        public override void Render (Gdk.Drawable window, Cairo.Context cr, Widget widget, Gdk.Rectangle cell_area, 
            Gdk.Rectangle clip_area, StateType state)
        {
            if (!is_header && !ServiceManager.PlayerEngine.IsPlaying ((TrackInfo)BoundObject)) {
                return;
            }
            
            Gdk.Rectangle pixbuf_area = new Gdk.Rectangle ();
            pixbuf_area.Width = pixbuf_size;
            pixbuf_area.Height = pixbuf_size;
            pixbuf_area.X = cell_area.X + pixbuf_spacing;
            pixbuf_area.Y = cell_area.Y + ((cell_area.Height - pixbuf_area.Height) / 2);
            
            window.DrawPixbuf (widget.Style.BackgroundGC(StateType.Normal), pixbuf, 
                0, 0, pixbuf_area.X, pixbuf_area.Y, pixbuf_area.Width, pixbuf_area.Height, 
                Gdk.RgbDither.Normal, 0, 0);
        }
    }
}
