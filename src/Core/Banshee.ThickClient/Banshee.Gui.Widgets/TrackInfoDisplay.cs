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
    
        private ArtworkManager artwork_manager;
        private Gdk.Pixbuf current_pixbuf;
        private Gdk.Pixbuf incoming_pixbuf;
        private Gdk.Pixbuf missing_pixbuf;
        
        private TrackInfo current_track;
        private TrackInfo incoming_track;        
        
        private DateTime transition_start; 
        private double transition_percent;
        private double transition_frames;
    
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
            
            Style = Style.Attach (GdkWindow);
            GdkWindow.SetBackPixmap (null, false);
            Style.SetBackground (GdkWindow, StateType.Normal);
        }
        
        protected override void OnUnrealized ()
        {   
            base.OnUnrealized ();
        }
        
        protected override void OnMapped ()
        {
            GdkWindow.Show ();
        }
        
        protected override void OnSizeRequested (ref Requisition requisition)
        {
            requisition.Width = 400;
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            
            if (IsRealized) {
                GdkWindow.MoveResize (allocation);
            }
        }
        
#endregion
        
#region Drawing
        
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
            
            if (transition_percent < 1.0) {
                double percent = transition_percent;
                if (current_track != null) {
                    percent = 1.0 - percent;
                    RenderStage (cr, incoming_track, incoming_pixbuf);
                    cr.PushGroup ();
                    RenderStage (cr, current_track, current_pixbuf);
                    cr.PopGroupToSource ();
                } else {
                    cr.PushGroup ();
                    RenderStage (cr, incoming_track, incoming_pixbuf);
                    cr.PopGroupToSource ();
                }
                cr.PaintWithAlpha (percent);            
            } else {
                RenderStage (cr, current_track, current_pixbuf);
            }
            
            ((IDisposable)cr.Target).Dispose ();
            ((IDisposable)cr).Dispose ();
        }
        
        private void RenderStage (Cairo.Context cr, TrackInfo track, Gdk.Pixbuf pixbuf)
        {
            if (pixbuf != null) {
                RenderCoverArt (cr, pixbuf);
            }
        }
        
        private void RenderCoverArt (Cairo.Context cr, Gdk.Pixbuf pixbuf)
        {
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
            cr.Color = new Color (1.0, 1.0, 1.0, 0.5);
            cr.Stroke ();
            
            cr.Rectangle (x + 0.5, y + 0.5, width - 1, height - 1);
            cr.Color = new Color (0.0, 0.0, 0.0, 0.65);
            cr.Stroke ();
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
            
            GLib.Timeout.Add (30, ComputeTransition);
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
                return false;
            }
            
            return true;
        }
        
#endregion
        
    }
}
