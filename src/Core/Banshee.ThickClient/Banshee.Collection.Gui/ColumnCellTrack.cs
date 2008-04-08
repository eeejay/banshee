//
// ColumnCellTrack.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Gtk;
using Cairo;

using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public class ColumnCellTrack : ColumnCell
    {
        public ColumnCellTrack () : base (null, true)
        {
        }
        
        public int ComputeRowHeight (Widget widget)
        {
            int lw, lh;
            Pango.Layout layout = new Pango.Layout (widget.PangoContext);
            layout.SetMarkup ("<b>W</b>\n<small><i>W</i></small>");
            layout.GetPixelSize (out lw, out lh);
            layout.Dispose ();
            return lh + 8;
        }
        
        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            if (BoundObject == null) {
                return;
            }
            
            if (!(BoundObject is TrackInfo)) {
                throw new InvalidCastException ("ColumnCellAlbum can only bind to AlbumInfo objects");
            }
            
            TrackInfo track = (TrackInfo)BoundObject;
            
            Pango.Layout layout = context.Layout;
            
            int x = 5, y = 0;
            int lw, lh;
            
            layout.Width = (int)((cellWidth - 2 * x) * Pango.Scale.PangoScale);
            layout.Ellipsize = Pango.EllipsizeMode.End;
            layout.FontDescription = context.Widget.PangoContext.FontDescription.Copy ();
            layout.SetMarkup (String.Format ("<b>{0}</b>\n<small><i>{1}</i></small>", 
                GLib.Markup.EscapeText (track.DisplayTrackTitle), 
                GLib.Markup.EscapeText (track.DisplayArtistName)));
            
            layout.GetPixelSize (out lw, out lh);
            
            y = (int)((cellHeight - lh) / 2);
            
            Style.PaintLayout (context.Widget.Style, context.Drawable, state, true, 
                context.Area, context.Widget, "text",
                context.Area.X + x, context.Area.Y + y, layout);
        }
    }
}
