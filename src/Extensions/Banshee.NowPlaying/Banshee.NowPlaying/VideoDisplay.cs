//
// VideoDisplay.cs
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

using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Collection;

using Banshee.Gui;

namespace Banshee.NowPlaying
{   
    public class VideoDisplay : Gtk.EventBox
    {
        private Gdk.Pixbuf idle_pixbuf;
        private bool render_idle = true;

        private bool fullscreen = false;
        public bool FullScreen {
            get { return fullscreen; }
            set { fullscreen = value; }
        }

        private Gdk.Window video_window;
        public Gdk.Window VideoWindow {
            get { return video_window; }
        }
    
        public VideoDisplay ()
        {
            CreateVideoWindow ();
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
            ToggleIdleVisibility ();
        }
        
        private void CreateVideoWindow ()
        {
            Gdk.WindowAttr attributes = new Gdk.WindowAttr ();
            attributes.WindowType = Gdk.WindowType.Child;
            attributes.Visual = Visual;
            attributes.Wclass = Gdk.WindowClass.InputOutput;
            attributes.Colormap = Colormap;
            attributes.EventMask = (int)(
                Gdk.EventMask.VisibilityNotifyMask |
                Gdk.EventMask.ExposureMask);
            
            Gdk.WindowAttributesType attributes_mask = 
                Gdk.WindowAttributesType.X | 
                Gdk.WindowAttributesType.Y | 
                Gdk.WindowAttributesType.Visual | 
                Gdk.WindowAttributesType.Colormap;
                
            video_window = new Gdk.Window (null, attributes, attributes_mask);
            video_window.UserData = Handle;
                        
            video_window.SetBackPixmap (null, false);
            
            ServiceManager.PlayerEngine.VideoWindow = video_window.Handle;
        }
        
        protected override void OnRealized ()
        {
            base.OnRealized ();
            video_window.Reparent (Parent.GdkWindow, Allocation.X, Allocation.Y);
        }
        
        protected override void OnUnrealized ()
        {
            base.OnUnrealized ();
            video_window.Reparent (null, 0, 0);
        }

        protected override void OnMapped ()
        {
            base.OnMapped ();
            video_window.Show ();
        }
        
        protected override void OnUnmapped ()
        {
            video_window.Hide ();
            base.OnUnmapped ();
        }
        
        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            video_window.MoveResize (allocation);
            base.OnSizeAllocated (allocation);
            
            QueueDraw ();
        }
        
        protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
        {
            if (video_window != null && ServiceManager.PlayerEngine.SupportsVideo) {
                ServiceManager.PlayerEngine.VideoExpose (video_window.Handle, true);
            }
            
            return false;
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (video_window == null || !video_window.IsVisible) {
                return true;
            }
            
            if (!render_idle && ServiceManager.PlayerEngine.SupportsVideo) {
                ServiceManager.PlayerEngine.VideoExpose (video_window.Handle, false);
                return true;
            }
            
            if (idle_pixbuf == null) {
                idle_pixbuf = Gdk.Pixbuf.LoadFromResource ("idle-logo.png");
            }
            
            if (idle_pixbuf == null) {
                return true;
            }
            
            video_window.DrawPixbuf (Style.BackgroundGC (StateType.Normal), idle_pixbuf, 0, 0, 
                (Allocation.Width - idle_pixbuf.Width) / 2, (Allocation.Height - idle_pixbuf.Height) / 2, 
                idle_pixbuf.Width, idle_pixbuf.Height, Gdk.RgbDither.Normal, 0, 0);
            
            return true;
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            if (args.Event == PlayerEngineEvent.StartOfStream || args.Event == PlayerEngineEvent.EndOfStream) {
                ToggleIdleVisibility ();
            }
        }
        
        private void ToggleIdleVisibility ()
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
            render_idle = track == null || (track.MediaAttributes & TrackMediaAttributes.VideoStream) == 0;
            QueueDraw ();
        }
        
        public new void QueueDraw ()
        {
            base.QueueDraw ();
            if (video_window != null) {
                video_window.InvalidateRect (new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height), true);
            }
        }
    }
}
