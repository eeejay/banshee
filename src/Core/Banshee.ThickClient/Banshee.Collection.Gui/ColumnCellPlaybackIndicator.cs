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

using Banshee.Streaming;
using Banshee.MediaEngine;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class ColumnCellPlaybackIndicator : ColumnCell
    {
        private enum Icon : int {
            Playing,
            Paused,
            Error,
            Protected
        }
        
        private const int pixbuf_size = 16;
        private const int pixbuf_spacing = 4;        
        private Gdk.Pixbuf [] pixbufs = new Gdk.Pixbuf[4];
        
        public ColumnCellPlaybackIndicator (string property) : this (property, true)
        {
        }
        
        public ColumnCellPlaybackIndicator (string property, bool expand) : base (property, expand)
        {
            LoadPixbufs ();
        }
        
        private void LoadPixbufs ()
        {
            for (int i = 0; i < pixbufs.Length; i++) {
                if (pixbufs[i] != null) {
                    pixbufs[i].Dispose ();
                    pixbufs[i] = null;
                }
            }
            
            pixbufs[(int)Icon.Playing] = IconThemeUtils.LoadIcon (pixbuf_size, "media-playback-start");
            pixbufs[(int)Icon.Paused] = IconThemeUtils.LoadIcon (pixbuf_size, "media-playback-pause");
            pixbufs[(int)Icon.Error] = IconThemeUtils.LoadIcon (pixbuf_size, "emblem-unreadable", "dialog-error");
            pixbufs[(int)Icon.Protected] = IconThemeUtils.LoadIcon (pixbuf_size, "emblem-readonly", "dialog-error");
        }
        
        public override void NotifyThemeChange ()
        {
            LoadPixbufs ();
        }

        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            TrackInfo track = BoundObject as TrackInfo;

            if (track == null)
                return;
        
            if (track.PlaybackError == StreamPlaybackError.None && !ServiceManager.PlayerEngine.IsPlaying (track)) {
                return;
            }
            
            Icon icon;
            
            if (track.PlaybackError == StreamPlaybackError.None) {
                icon = ServiceManager.PlayerEngine.CurrentState == PlayerState.Paused
                    ? Icon.Paused
                    : Icon.Playing;
            } else if (track.PlaybackError == StreamPlaybackError.Drm) {
                icon = Icon.Protected;
            } else {
                icon = Icon.Error;
            } 
            
            context.Context.Translate (0, 0.5);
            
            Gdk.Pixbuf render_pixbuf = pixbufs[(int)icon];
            
            Cairo.Rectangle pixbuf_area = new Cairo.Rectangle (pixbuf_spacing, 
                (cellHeight - render_pixbuf.Height) / 2, render_pixbuf.Width, render_pixbuf.Height);
            
            Gdk.CairoHelper.SetSourcePixbuf (context.Context, render_pixbuf, pixbuf_area.X, pixbuf_area.Y);
            context.Context.Rectangle (pixbuf_area);
            context.Context.Fill ();
        }
    }
}
