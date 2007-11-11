//
// TrackInfoDisplay.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Larry Ewing <lewing@novell.com> (Is my hero)
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
using System.Collections;
using Mono.Unix;

using Gtk;
using Cairo;

using Hyena.Data.Gui;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Gui.Widgets
{
    public class TrackInfoDisplay : Bin
    {
        private const int FADE_TIMEOUT = 1500;
        private const int PANGO_SCALE = 1024;
    
        private ArtworkManager artwork_manager;
        private Gdk.Pixbuf current_pixbuf;
        private Gdk.Pixbuf incoming_pixbuf;
        private Gdk.Pixbuf missing_pixbuf;
        
        private Cairo.Color cover_border_dark_color;
        private Cairo.Color cover_border_light_color;
        private Cairo.Color text_color;
        private Cairo.Color text_light_color;
        
        private TrackInfo current_track;
        private TrackInfo incoming_track;        
        
        private DateTime transition_start; 
        private double transition_percent;
        private double transition_frames;
        private uint transition_id;
        
        public TrackInfoDisplay ()
        {
            if (ServiceManager.Contains ("ArtworkManager")) {
                artwork_manager = ServiceManager.Get<ArtworkManager> ("ArtworkManager");
            }
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
        }

#region Widget Window Management
        
        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.Realized;
            
            Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.X = Allocation.X;
            attributes.Y = Allocation.Y;
            attributes.Width = Allocation.Width;
            attributes.Height = Allocation.Height;
            attributes.Visual = Visual;
            attributes.Wclass = Gdk.WindowClass.InputOutput;
            attributes.Colormap = Colormap;
            attributes.EventMask = (int)(Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask |
                Gdk.EventMask.PointerMotionMask |
                Gdk.EventMask.EnterNotifyMask |
                Gdk.EventMask.LeaveNotifyMask |
                Gdk.EventMask.ButtonPressMask |
                Gdk.EventMask.ButtonReleaseMask |
                Events);
            
            Gdk.WindowAttributesType attributes_mask = 
                Gdk.WindowAttributesType.X | 
                Gdk.WindowAttributesType.Y | 
                Gdk.WindowAttributesType.Visual | 
                Gdk.WindowAttributesType.Colormap;
                
            GdkWindow = new Gdk.Window (Parent.GdkWindow, attributes, attributes_mask);
            GdkWindow.UserData = Handle;
            
            GdkWindow.SetBackPixmap (null, false);
            Style.SetBackground (GdkWindow, StateType.Normal);
            Style = Style.Attach (GdkWindow);
        }
        
        protected override void OnMapped ()
        {
            GdkWindow.Show ();
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            if (IsRealized) {
                GdkWindow.MoveResize (allocation);
            }
            
            QueueDraw ();
        }
        
#endregion
        
#region Drawing

        protected override void OnStyleSet (Style previous)
        {
            text_color = CairoExtensions.GdkColorToCairoColor (Style.Text (StateType.Normal));          
            text_light_color = CairoExtensions.ColorAdjustBrightness (text_color, 0.5);
            cover_border_light_color = new Color (1.0, 1.0, 1.0, 0.5);
            cover_border_dark_color = new Color (0.0, 0.0, 0.0, 0.65);
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            foreach (Gdk.Rectangle rect in evnt.Region.GetRectangles ()) {
                PaintRegion (evnt, rect);
            }
            
            return true;
        }
                
        private void PaintRegion (Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();
            
            RenderAnimation (cr, clip);
            
            ((IDisposable)cr.Target).Dispose ();
            ((IDisposable)cr).Dispose ();
        }
        
        private void RenderAnimation (Cairo.Context cr, Gdk.Rectangle clip)
        {            
            if (transition_percent >= 1.0) {
                // We are not in a transition, just render
                RenderStage (cr, current_track, current_pixbuf);
                return;
            } 
            
            if (current_track == null) {
                // Fade in the whole stage, nothing to fade out
                CairoExtensions.PushGroup (cr);
                RenderStage (cr, incoming_track, incoming_pixbuf);
                CairoExtensions.PopGroupToSource (cr);
                cr.PaintWithAlpha (transition_percent);
                return;
            }
            
            // XFade only the cover art
            cr.Rectangle (0, 0, Allocation.Height, Allocation.Height);
            cr.Clip ();
            RenderCoverArt (cr, incoming_pixbuf);
            CairoExtensions.PushGroup (cr);
            RenderCoverArt (cr, current_pixbuf);
            CairoExtensions.PopGroupToSource (cr);
            cr.PaintWithAlpha (1.0 - transition_percent);
                 
            // Fade in/out the text
            cr.ResetClip ();
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();
                   
            if (transition_percent <= 0.5) {
                // Fade out old text
                CairoExtensions.PushGroup (cr);
                RenderTrackInfo (cr, current_track);
                CairoExtensions.PopGroupToSource (cr);
                cr.PaintWithAlpha (1.0 - (transition_percent * 2.0));
            } else {
                // Fade in new text
                CairoExtensions.PushGroup (cr);
                RenderTrackInfo (cr, incoming_track);
                CairoExtensions.PopGroupToSource (cr);
                cr.PaintWithAlpha ((transition_percent - 0.5) * 2.0);
            }
        }
        
        private void RenderStage (Cairo.Context cr, TrackInfo track, Gdk.Pixbuf pixbuf)
        {
           RenderCoverArt (cr, pixbuf);
           RenderTrackInfo (cr, track);
        }
        
        private void RenderCoverArt (Cairo.Context cr, Gdk.Pixbuf pixbuf)
        {
            if (pixbuf == null) {
                return;
            }
            
            double x, y, p_x, p_y, width, height;
            
            width = height = Allocation.Height;
            x = y = p_x = p_y = 0;
            p_x += pixbuf.Width < width ? (width - pixbuf.Width) / 2 : 0;
            p_y += pixbuf.Height < height ? (height - pixbuf.Height) / 2 : 0;
            
            Gdk.CairoHelper.SetSourcePixbuf (cr, pixbuf, p_x, p_y);
            cr.Rectangle (p_x, p_y, pixbuf.Width + p_x, pixbuf.Height + p_y);
            cr.Fill ();
            
            if (pixbuf == missing_pixbuf) {
                return;
            }
            
            cr.LineWidth = 1.0;
            cr.Antialias = Antialias.None;
            
            cr.Rectangle (x + 1.5, y + 1.5, width - 3, height - 3);
            cr.Color = cover_border_light_color;
            cr.Stroke ();
            
            cr.Rectangle (x + 0.5, y + 0.5, width - 1, height - 1);
            cr.Color = cover_border_dark_color;
            cr.Stroke ();
        }
        
        private void RenderTrackInfo (Cairo.Context cr, TrackInfo track)
        {
            if (track == null) {
                return;
            }
            
            double x = Allocation.Height + 10;
            double y = 0;
            double width = Allocation.Width - x;
            int l_width, l_height;

            cr.Antialias = Cairo.Antialias.Default;            
            
            Pango.Layout layout = Pango.CairoHelper.CreateLayout (cr);
            layout.Width = (int)width * PANGO_SCALE;
            layout.Ellipsize = Pango.EllipsizeMode.End;
            layout.FontDescription = PangoContext.FontDescription;
            
            layout.SetMarkup (String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (track.DisplayTrackTitle)));
            cr.MoveTo (x, y);
            Pango.CairoHelper.LayoutPath (cr, layout);
            cr.Color = text_color;
            cr.Fill ();
            
            string second_line = GetSecondLineText (track);
            if(second_line == null) {
                return;
            }
            
            layout.GetPixelSize (out l_width, out l_height);
            layout.SetMarkup (second_line);
            cr.MoveTo (x, y + l_height);
            Pango.CairoHelper.ShowLayout (cr, layout);
        }
        
        public new void QueueDraw ()
        {
            if (GdkWindow != null) {
                GdkWindow.InvalidateRect (new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height), false);
            }
            
            base.QueueDraw ();
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            if (args.Event == PlayerEngineEvent.StartOfStream) {
                TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
                if (track == null) {
                    incoming_track = null;
                    incoming_pixbuf = null;
                    return;
                }
                
                incoming_track = track;
                
                Gdk.Pixbuf pixbuf = artwork_manager.Lookup (track.ArtistAlbumId);
                if (pixbuf == null) {
                    if (missing_pixbuf == null) {
                        missing_pixbuf = IconThemeUtils.LoadIcon (32, "audio-x-generic");
                    }
                    incoming_pixbuf = missing_pixbuf;
                } else {
                    incoming_pixbuf = pixbuf.ScaleSimple (Allocation.Height, Allocation.Height, 
                        Gdk.InterpType.Bilinear);
                }

                BeginTransition ();
            }
        }
        
        private void BeginTransition ()
        {
            transition_start = DateTime.Now;
            transition_percent = 0.0;
            transition_frames = 0.0;
            
            if (transition_id == 0) {
                transition_id = GLib.Timeout.Add (30, ComputeTransition);
            }
        }
        
        private bool ComputeTransition ()
        {
            double elapsed = (DateTime.Now - transition_start).TotalMilliseconds;
            transition_percent = elapsed / FADE_TIMEOUT;
            transition_frames++;
            
            QueueDraw ();
            
            if (elapsed > FADE_TIMEOUT) {
                if (ApplicationContext.Debugging) {
                    Log.DebugFormat ("TrackInfoDisplay XFade: {0:0.00} FPS", 
                        transition_frames / ((double)FADE_TIMEOUT / 1000.0));
                }
                
                current_pixbuf = incoming_pixbuf;
                current_track = incoming_track;
                incoming_pixbuf = null;
                incoming_track = null;
                transition_id = 0;
                return false;
            }
            
            return true;
        }
        
        private string GetSecondLineText (TrackInfo track)
        {
            string markup_begin = String.Format ("<span color=\"{0}\" size=\"small\">", 
                CairoExtensions.ColorGetHex (text_light_color, false));
            string markup_end = "</span>";
            string markup = null;

            if (track.ArtistName != null && track.AlbumTitle != null) {
                // Translators: {0} and {1} are for markup, {2} and {3}
                // are Artist Name and Album Title, respectively;
                // e.g. 'by Parkway Drive from Killing with a Smile'
                markup = String.Format ("{0}by{1} {2} {0}from{1} {3}", markup_begin, markup_end, 
                    GLib.Markup.EscapeText (track.DisplayArtistName), 
                    GLib.Markup.EscapeText (track.DisplayAlbumTitle));
            } else if (track.ArtistName != null) {
                // Translators: {0} and {1} are for markup, {2} is for Artist Name;
                // e.g. 'by Parkway Drive'
                markup = String.Format ("{0}by{1} {2}", markup_begin, markup_end,
                    GLib.Markup.EscapeText (track.DisplayArtistName));
            } else if (track.AlbumTitle != null) {
                // Translators: {0} and {1} are for markup, {2} is for Album Title;
                // e.g. 'from Killing with a Smile'
                markup = String.Format ("{0}by{1} {2}", markup_begin, markup_end,
                    GLib.Markup.EscapeText (track.DisplayAlbumTitle));
            }
            
            if (markup == null) {
                return null;
            }
            
            return String.Format ("<span color=\"{0}\">{1}</span>",  
                CairoExtensions.ColorGetHex (text_color, false),
                markup);
        }
        
#endregion
        
    }
}
