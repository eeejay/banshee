//
// LargeTrackInfoDisplay.cs
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
using System.Collections.Generic;

using Gtk;
using Cairo;

using Hyena.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;

namespace Banshee.Gui.Widgets
{
    public class LargeTrackInfoDisplay : TrackInfoDisplay
    {
        private Gdk.Rectangle text_alloc;
    
        public LargeTrackInfoDisplay ()
        {
        }
        
        protected LargeTrackInfoDisplay (IntPtr native) : base (native)
        {
        }
        
        protected override int MissingIconSizeRequest {
            get { return 128; }
        }
        
        protected virtual int MaxArtworkSize {
            get { return 300; }
        }
        
        protected virtual int Spacing {
            get { return 30; }
        }
        
        protected override int ArtworkSizeRequest {
            get { return Math.Min (Math.Min (Allocation.Height, Allocation.Width), MaxArtworkSize); }
        }

        protected virtual Gdk.Rectangle RenderAllocation {
            get {
                int width = ArtworkSizeRequest * 2 + Spacing;
                int height = (int)Math.Ceiling (ArtworkSizeRequest * 1.2);
                int x = Allocation.X + (Allocation.Width - width) / 2;
                int y = Allocation.Y + (Allocation.Height - height) / 2;
                return new Gdk.Rectangle (x, y, width, height);
            }
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            QueueDraw ();
        }
       
        protected override void RenderCoverArt (Cairo.Context cr, Gdk.Pixbuf pixbuf)
        {
            if (pixbuf == null) {
                return;
            }
            
            Gdk.Rectangle alloc = RenderAllocation;
            int asr = ArtworkSizeRequest;
            int reflect = (int)(pixbuf.Height * 0.2);
            int surface_w = pixbuf.Width;
            int surface_h = pixbuf.Height + reflect;
            int x = alloc.X + alloc.Width - asr;
            int y = alloc.Y;
            
            Surface surface = SurfaceLookup (pixbuf);
            if (surface == null) {
                surface = CreateSurfaceForPixbuf (cr, pixbuf, reflect);
                SurfaceCache (pixbuf, surface);
            }
                
            cr.Rectangle (x, y, asr, alloc.Height);
            cr.Color = BackgroundColor;
            cr.Fill ();
            
            x += (asr - surface_w) / 2;
            y += surface_h > asr ? 0 : (asr - surface_h) / 2;
            
            cr.SetSource (surface, x, y);
            cr.Paint ();
        }
        
        private Surface CreateSurfaceForPixbuf (Cairo.Context window_cr, Gdk.Pixbuf pixbuf, int reflect)
        {
            Surface surface = window_cr.Target.CreateSimilar (window_cr.Target.Content, 
                pixbuf.Width, pixbuf.Height + reflect);
            Cairo.Context cr = new Context (surface);
            
            cr.Save ();
            
            Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, 0, 0);
            cr.Paint ();
            
            cr.Rectangle (0, pixbuf.Height, pixbuf.Width, reflect);
            cr.Clip ();
            
            Matrix matrix = new Matrix ();
            matrix.InitScale (1, -1);
            matrix.Translate (0, -(2 * pixbuf.Height) + 1);
            cr.Transform (matrix);
            
            Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, 0, 0);
            cr.Paint ();
            
            cr.Restore ();
            
            Color bg_transparent = BackgroundColor;
            bg_transparent.A = 0.65;
            
            LinearGradient mask = new LinearGradient (0, pixbuf.Height, 0, pixbuf.Height + reflect);
            mask.AddColorStop (0, bg_transparent);
            mask.AddColorStop (1, BackgroundColor);
            
            cr.Rectangle (0, pixbuf.Height, pixbuf.Width, reflect);
            cr.Pattern = mask;
            cr.Fill ();
            
            ((IDisposable)cr).Dispose ();
            return surface;
        }
        
        protected override void RenderTrackInfo (Context cr, TrackInfo track, bool renderTrack, bool renderArtistAlbum)
        {
            if (track == null) {
                return;
            }

            Gdk.Rectangle alloc = RenderAllocation;
            int width = ArtworkSizeRequest;
            int fl_width, fl_height, sl_width, sl_height, tl_width, tl_height;

            string first_line = GetFirstLineText (track);
            
            // FIXME: This is incredibly bad, but we don't want to break
            // translations right now. It needs to be replaced for 1.4!!
            string line = GetSecondLineText (track);
            string second_line = line, third_line = line;
            int split_pos = line.LastIndexOf ("<span");
            if (split_pos >= 0) {
                second_line = line.Substring (0, split_pos - 1) + "</span>";
                third_line = String.Format ("<span color=\"{0}\">{1}",
                    CairoExtensions.ColorGetHex (TextColor, false),
                    line.Substring (split_pos, line.Length - split_pos));
            }

            // Set up the text layouts
            Pango.Layout first_line_layout = null;
            CairoExtensions.CreateLayout (this, cr, ref first_line_layout);
            first_line_layout.Width = (int)(width * Pango.Scale.PangoScale);
            first_line_layout.Ellipsize = Pango.EllipsizeMode.End;
            first_line_layout.Alignment = Pango.Alignment.Right;

            int base_size = first_line_layout.FontDescription.Size;
                        
            Pango.Layout second_line_layout = first_line_layout.Copy ();
            Pango.Layout third_line_layout = first_line_layout.Copy ();

            first_line_layout.FontDescription.Size = (int)(base_size * Pango.Scale.XLarge);
            
            // Compute the layout coordinates
            first_line_layout.SetMarkup (first_line);
            first_line_layout.GetPixelSize (out fl_width, out fl_height);
            second_line_layout.SetMarkup (second_line);
            second_line_layout.GetPixelSize (out sl_width, out sl_height);
            third_line_layout.SetMarkup (third_line);
            third_line_layout.GetPixelSize (out tl_width, out tl_height);

            text_alloc.X = alloc.X;
            text_alloc.Width = width;
            text_alloc.Height = fl_height + sl_height + tl_height;
            text_alloc.Y = alloc.Y + (ArtworkSizeRequest - text_alloc.Height) / 2;

            // Render the layouts
            cr.Antialias = Cairo.Antialias.Default;
            
            if (renderTrack) {
                cr.MoveTo (text_alloc.X, text_alloc.Y);
                cr.Color = TextColor;
                PangoCairoHelper.ShowLayout (cr, first_line_layout);
            }

            if (!renderArtistAlbum) {
                first_line_layout.Dispose ();
                second_line_layout.Dispose ();
                third_line_layout.Dispose ();
                return;
            }
            
            cr.MoveTo (text_alloc.X, text_alloc.Y + fl_height);
            PangoCairoHelper.ShowLayout (cr, second_line_layout);
            
            cr.MoveTo (text_alloc.X, text_alloc.Y + fl_height + sl_height);
            PangoCairoHelper.ShowLayout (cr, third_line_layout);
            
            first_line_layout.Dispose ();
            second_line_layout.Dispose ();
            third_line_layout.Dispose ();
        }
        
        protected override void Invalidate ()
        {
            if (CurrentPixbuf == null || CurrentTrack == null || IncomingPixbuf == null || IncomingTrack == null) {
                QueueDraw ();
            } else {
                Gdk.Rectangle alloc = RenderAllocation;
                QueueDrawArea (text_alloc.X, text_alloc.Y, text_alloc.Width, text_alloc.Height);
                QueueDrawArea (alloc.X + text_alloc.Width + Spacing, alloc.Y, 
                    alloc.Width - text_alloc.Width - Spacing, alloc.Height);
            }
        }
    }
}
