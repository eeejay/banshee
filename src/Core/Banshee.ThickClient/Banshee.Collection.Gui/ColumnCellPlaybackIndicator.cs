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

using Hyena.Data.Gui;
using Banshee.Gui;

using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellPlaybackIndicator : ColumnCell
    {
        private const int pixbuf_size = 16;
        private const int pixbuf_spacing = 4;
        
        private Gdk.Pixbuf pixbuf;
        private Gdk.Pixbuf pixbuf_paused;
        
        public ColumnCellPlaybackIndicator (string property) : base (property, true)
        {
            LoadPixbuf ();
        }
        
        private void LoadPixbuf ()
        {
            if (pixbuf != null) {
                pixbuf.Dispose ();
                pixbuf = null;
            }
            
            if (pixbuf_paused != null) {
                pixbuf_paused.Dispose ();
                pixbuf_paused = null;
            }
            
            pixbuf = IconThemeUtils.LoadIcon (pixbuf_size, "media-playback-start");
            pixbuf_paused = IconThemeUtils.LoadIcon (pixbuf_size, "media-playback-pause");
        }
        
        public override void NotifyThemeChange ()
        {
            LoadPixbuf ();
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            if (!ServiceManager.PlayerEngine.IsPlaying ((TrackInfo)BoundObject)) {
                return;
            }
            
            Gdk.Pixbuf render_pixbuf = pixbuf;
            if (ServiceManager.PlayerEngine.CurrentState == PlayerState.Paused) {
                render_pixbuf = pixbuf_paused;
            }
            
            context.Context.Translate (0, 0.5);
            
            Cairo.Rectangle pixbuf_area = new Cairo.Rectangle (pixbuf_spacing, 
                (cellHeight - render_pixbuf.Height) / 2, render_pixbuf.Width, render_pixbuf.Height);
            
            Gdk.CairoHelper.SetSourcePixbuf (context.Context, render_pixbuf, pixbuf_area.X, pixbuf_area.Y);
            context.Context.Rectangle (pixbuf_area);
            context.Context.Fill ();
        }
    }
}
