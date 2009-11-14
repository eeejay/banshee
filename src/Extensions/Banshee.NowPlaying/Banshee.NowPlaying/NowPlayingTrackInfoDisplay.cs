//
// NowPlayingTrackInfoDisplay.cs
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

using Hyena.Gui;
using Banshee.Gui.Widgets;

namespace Banshee.NowPlaying
{
    public class NowPlayingTrackInfoDisplay : LargeTrackInfoDisplay
    {
        private static Cairo.Color background_color = CairoExtensions.RgbToColor (0);
        private static Cairo.Color text_color = CairoExtensions.RgbToColor (0xffffff);
        private static Cairo.Color text_light_color = CairoExtensions.RgbToColor (0x777777);
        private static Gdk.Pixbuf idle_pixbuf;

        public NowPlayingTrackInfoDisplay ()
        {
        }

        protected NowPlayingTrackInfoDisplay (IntPtr native) : base (native)
        {
        }

        protected override Cairo.Color BackgroundColor {
            get { return background_color; }
        }

        protected override Cairo.Color TextColor {
            get { return text_color; }
        }

        protected override Cairo.Color TextLightColor {
            get { return text_light_color; }
        }

        protected override bool CanRenderIdle {
            get { return true; }
        }

        protected override void RenderIdle (Cairo.Context cr)
        {
            if (idle_pixbuf == null) {
                idle_pixbuf = Gdk.Pixbuf.LoadFromResource ("idle-logo.png");
            }

            if (idle_pixbuf == null) {
                return;
            }

            cr.Save ();
            cr.Translate (Allocation.X + ((Allocation.Width - idle_pixbuf.Width) / 2),
                Allocation.Y + ((Allocation.Height - idle_pixbuf.Height) / 2));
            Gdk.CairoHelper.SetSourcePixbuf (cr, idle_pixbuf, 0, 0);
            cr.Paint ();
            cr.Restore ();
        }
    }
}
