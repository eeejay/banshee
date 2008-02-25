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

using Hyena;
using Hyena.Gui;
using Hyena.Gui.Theatrics;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Gui.Widgets
{
    public class TrackInfoDisplay : Bin
    {
        private ArtworkManager artwork_manager;
        private Gdk.Pixbuf current_pixbuf;
        private Gdk.Pixbuf incoming_pixbuf;
        private Gdk.Pixbuf missing_pixbuf;
        
        private Cairo.Color background_color;
        private Cairo.Color text_color;
        private Cairo.Color text_light_color;
        
        private TrackInfo current_track;
        private TrackInfo incoming_track;        

        private SingleActorStage stage = new SingleActorStage ();
        
        private ArtworkPopup popup;
        private uint popup_timeout_id;
        private bool in_popup;
        private bool in_thumbnail_region;
        
        protected TrackInfoDisplay (IntPtr native) : base (native)
        {
        }
        
        public TrackInfoDisplay ()
        {
            stage.Iteration += OnStageIteration;
        
            if (ServiceManager.Contains ("ArtworkManager")) {
                artwork_manager = ServiceManager.Get<ArtworkManager> ("ArtworkManager");
            }
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
        }
        
        public override void Dispose ()
        {
            ServiceManager.PlayerEngine.EventChanged -= OnPlayerEngineEventChanged;
            stage.Iteration -= OnStageIteration;
            HidePopup ();
            
            base.Dispose ();
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
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask |
                Gdk.EventMask.PointerMotionMask |
                Gdk.EventMask.EnterNotifyMask |
                Gdk.EventMask.LeaveNotifyMask |
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
        
        protected override void OnUnrealized ()
        {
            WidgetFlags ^= WidgetFlags.Realized;
            GdkWindow.UserData = IntPtr.Zero;
            GdkWindow.Destroy ();
            GdkWindow = null;
        }

        protected override void OnMapped ()
        {
            WidgetFlags |= WidgetFlags.Mapped;
            GdkWindow.Show ();
        }
        
        protected override void OnUnmapped ()
        {
            WidgetFlags ^= WidgetFlags.Mapped;
            GdkWindow.Hide ();
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            if (IsRealized) {
                GdkWindow.MoveResize (allocation);
            }
            
            if (current_track == null) {
                LoadCurrentTrack ();
            }
            
            QueueDraw ();
        }
        
#endregion

#region Interaction Events

        protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
        {
            in_thumbnail_region = evnt.X <= Allocation.Height;
            return ShowHideCoverArt ();
        }
        
        protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
        {
            in_thumbnail_region = false;
            return ShowHideCoverArt ();
        }
        
        protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
        {
            in_thumbnail_region = evnt.X <= Allocation.Height;
            return ShowHideCoverArt ();
        }
        
        private void OnPopupEnterNotifyEvent (object o, EnterNotifyEventArgs args)
        {
            in_popup = true;
        }
        
        private void OnPopupLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
        {
            in_popup = false;
            HidePopup ();
        }
        
        private bool ShowHideCoverArt ()
        {
            if (!in_thumbnail_region) {
                if (popup_timeout_id > 0) {
                    GLib.Source.Remove (popup_timeout_id);
                    popup_timeout_id = 0;
                }
                
                GLib.Timeout.Add (100, delegate {
                    if (!in_popup) {
                        HidePopup ();
                    }

                    return false;
                });
            } else {
                if (popup_timeout_id > 0) {
                    return false;
                }
                
                popup_timeout_id = GLib.Timeout.Add (500, delegate {
                    if (in_thumbnail_region) {
                        UpdatePopup ();
                    }
                    
                    popup_timeout_id = 0;
                    return false;
                });
            }
            
            return true;
        }

#endregion
        
#region Drawing

        protected override void OnStyleSet (Style previous)
        {
            text_color = CairoExtensions.GdkColorToCairoColor (Style.Text (StateType.Normal));          
            text_light_color = CairoExtensions.ColorAdjustBrightness (text_color, 0.5);
            background_color = CairoExtensions.GdkColorToCairoColor (Style.Background (StateType.Normal));
            
            if (missing_pixbuf != null) {
                missing_pixbuf.Dispose ();
                missing_pixbuf = null;
            }
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (!Visible || !IsMapped) {
                return true;
            }
        
            Style.ApplyDefaultBackground (GdkWindow, true, StateType.Normal, evnt.Area, 
                0, 0, Allocation.Width, Allocation.Height);
        
            Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window);
        
            foreach (Gdk.Rectangle rect in evnt.Region.GetRectangles ()) {
                PaintRegion (cr, evnt, rect);
            }

            CairoExtensions.DisposeContext (cr);
            cr = null;
            
            return true;
        }
                
        private void PaintRegion (Cairo.Context cr, Gdk.EventExpose evnt, Gdk.Rectangle clip)
        {
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();
            
            if (incoming_track != null || current_track != null) {
                RenderAnimation (cr, clip);
            }
        }
        
        private void RenderAnimation (Cairo.Context cr, Gdk.Rectangle clip)
        {
            if (stage.Actor == null) {
                // We are not in a transition, just render
                RenderStage (cr, current_track, current_pixbuf);
                return;
            } 
            
            if (current_track == null) {
                // Fade in the whole stage, nothing to fade out
                CairoExtensions.PushGroup (cr);
                RenderStage (cr, incoming_track, incoming_pixbuf);
                CairoExtensions.PopGroupToSource (cr);
                
                cr.PaintWithAlpha (stage.Actor.Percent);
                return;
            }
            
            // XFade only the cover art
            cr.Rectangle (0, 0, Allocation.Height, Allocation.Height);
            cr.Clip ();

            RenderCoverArt (cr, incoming_pixbuf);
            
            CairoExtensions.PushGroup (cr);
            RenderCoverArt (cr, current_pixbuf);
            CairoExtensions.PopGroupToSource (cr);
            
            cr.PaintWithAlpha (1.0 - stage.Actor.Percent);
            
            // Fade in/out the text
            cr.ResetClip ();
            cr.Rectangle (clip.X, clip.Y, clip.Width, clip.Height);
            cr.Clip ();
            
            bool same_artist_album = incoming_track != null ? incoming_track.ArtistAlbumEqual (current_track) : false;
            
            if (same_artist_album) {
                RenderTrackInfo (cr, incoming_track, false, true);
            }
                   
            if (stage.Actor.Percent <= 0.5) {
                // Fade out old text
                CairoExtensions.PushGroup (cr);
                RenderTrackInfo (cr, current_track, true, !same_artist_album);
                CairoExtensions.PopGroupToSource (cr);
               
                cr.PaintWithAlpha (1.0 - (stage.Actor.Percent * 2.0));
            } else {
                // Fade in new text
                CairoExtensions.PushGroup (cr);
                RenderTrackInfo (cr, incoming_track, true, !same_artist_album);
                CairoExtensions.PopGroupToSource (cr);
                
                cr.PaintWithAlpha ((stage.Actor.Percent - 0.5) * 2.0);
            }
        }
        
        private void RenderStage (Cairo.Context cr, TrackInfo track, Gdk.Pixbuf pixbuf)
        {
            RenderCoverArt (cr, pixbuf);
            RenderTrackInfo (cr, track, true, true);
        }
        
        private void RenderCoverArt (Cairo.Context cr, Gdk.Pixbuf pixbuf)
        {
            ArtworkRenderer.RenderThumbnail (cr, pixbuf, false, 0, 0, Allocation.Height, Allocation.Height, 
                pixbuf != missing_pixbuf, 0, pixbuf == missing_pixbuf, background_color);
        }
        
        private void RenderTrackInfo (Cairo.Context cr, TrackInfo track, bool renderTrack, bool renderArtistAlbum)
        {
            if (track == null) {
                return;
            }
            
            double x = Allocation.Height + 10, y = 0;
            double width = Allocation.Width - x;
            int fl_width, fl_height, sl_width, sl_height;

            // Set up the text layouts
            Pango.Layout first_line_layout = Pango.CairoHelper.CreateLayout (cr);
            first_line_layout.Width = (int)(width * Pango.Scale.PangoScale);
            first_line_layout.Ellipsize = Pango.EllipsizeMode.End;
            first_line_layout.FontDescription = PangoContext.FontDescription.Copy ();
            
            Pango.Layout second_line_layout = first_line_layout.Copy ();
            
            // Compute the layout coordinates
            first_line_layout.SetMarkup (GetFirstLineText (track));
            first_line_layout.GetPixelSize (out fl_width, out fl_height);
            second_line_layout.SetMarkup (GetSecondLineText (track));
            second_line_layout.GetPixelSize (out sl_width, out sl_height);
            
            y = (Allocation.Height - (fl_height + sl_height)) / 2;
            
            // Render the layouts
            cr.Antialias = Cairo.Antialias.Default;
            
            if (renderTrack) {
                cr.MoveTo (x, y);
                Pango.CairoHelper.LayoutPath (cr, first_line_layout);
                cr.Color = text_color;
                cr.Fill ();
            }

            if (!renderArtistAlbum) {
                return;
            }
            
            cr.MoveTo (x, y + fl_height);
            Pango.CairoHelper.ShowLayout (cr, second_line_layout);
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
            if (args.Event == PlayerEngineEvent.StartOfStream || args.Event == PlayerEngineEvent.TrackInfoUpdated) {
                LoadCurrentTrack ();
            }
        }
        
        private void LoadCurrentTrack ()
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;

            if (track == current_track) {
                return;
            } else if (track == null) {
                incoming_track = null;
                incoming_pixbuf = null;
                return;
            }

            incoming_track = track;

            Gdk.Pixbuf pixbuf = artwork_manager.LookupScale (track.ArtistAlbumId, Allocation.Height);
            
            if (pixbuf == null) {
                if (missing_pixbuf == null) {
                    missing_pixbuf = IconThemeUtils.LoadIcon (32, "audio-x-generic");
                }
                incoming_pixbuf = missing_pixbuf;
            } else {
                incoming_pixbuf = pixbuf;
            }
            
            if (stage.Actor == null) {
                stage.Reset ();
            }
        }
        
        private double last_fps = 0.0;
        
        private void OnStageIteration (object o, EventArgs args)
        {
            QueueDraw ();
            
            if (stage.Actor != null) {
                last_fps = stage.Actor.FramesPerSecond;
                return;
            }
            
            if (ApplicationContext.Debugging) {
                Log.DebugFormat ("TrackInfoDisplay RenderAnimation: {0:0.00} FPS", last_fps);
            }
            
            if (current_pixbuf != incoming_pixbuf && current_pixbuf != missing_pixbuf) {
                ArtworkRenderer.DisposePixbuf (current_pixbuf);
            }
            
            current_pixbuf = incoming_pixbuf;
            current_track = incoming_track;
            
            incoming_track = null;
            
            UpdatePopup ();
        }
        
        private string GetFirstLineText (TrackInfo track)
        {
            return String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (track.DisplayTrackTitle));
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
            } else if (track.AlbumTitle != null) {
                // Translators: {0} and {1} are for markup, {2} is for Album Title;
                // e.g. 'from Killing with a Smile'
                markup = String.Format ("{0}from{1} {2}", markup_begin, markup_end,
                    GLib.Markup.EscapeText (track.DisplayAlbumTitle));
            } else {
                // Translators: {0} and {1} are for markup, {2} is for Artist Name;
                // e.g. 'by Parkway Drive'
                markup = String.Format ("{0}by{1} {2}", markup_begin, markup_end,
                    GLib.Markup.EscapeText (track.DisplayArtistName));
            }
            
            return String.Format ("<span color=\"{0}\">{1}</span>",  
                CairoExtensions.ColorGetHex (text_color, false),
                markup);
        }
        
        private bool UpdatePopup ()
        {
            if (current_track == null || artwork_manager == null) {
                HidePopup ();
                return false;
            }
            
            Gdk.Pixbuf pixbuf = artwork_manager.Lookup (current_track.ArtistAlbumId);
         
            if (pixbuf == null) {
                HidePopup ();
                return false;
            }
            
            if (popup == null) {
                popup = new ArtworkPopup ();
                popup.EnterNotifyEvent += OnPopupEnterNotifyEvent;
                popup.LeaveNotifyEvent += OnPopupLeaveNotifyEvent;
            }
            
            popup.Label = String.Format ("{0} - {1}", current_track.DisplayArtistName, 
                current_track.DisplayAlbumTitle);
            popup.Image = pixbuf;
                
            if (in_thumbnail_region) {
                popup.Show ();
            }
            
            return true;
        }
        
        private void HidePopup ()
        {
            if (popup != null) {
                ArtworkRenderer.DisposePixbuf (popup.Image);
                popup.Destroy ();
                popup.EnterNotifyEvent -= OnPopupEnterNotifyEvent;
                popup.LeaveNotifyEvent -= OnPopupLeaveNotifyEvent;
                popup = null;
            }
        }
        
#endregion
        
    }
}
