//
// NowPlayingInterface.cs
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

using Mono.Unix;
using Gtk;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.NowPlaying
{
    public class NowPlayingInterface : VBox, ISourceContents
    {   
        private NowPlayingSource source;
        private VideoDisplay video_display;
        private Hyena.Widgets.RoundedFrame frame;
        private Gtk.Window video_window;
        private FullscreenAdapter fullscreen_adapter;
        private ScreensaverManager screensaver;

        public VideoDisplay VideoDisplay {
            get { return video_display; }
        }
        
        public NowPlayingInterface ()
        {
            GtkElementsService service = ServiceManager.Get<GtkElementsService> ();
            
            video_display = new XOverlayVideoDisplay ();
            
            // This is my really sweet hack - it's where the video widget
            // is sent when the source is not active. This keeps the video
            // widget from being completely destroyed, causing problems with
            // its internal windowing and GstXOverlay. It's also conveniently
            // the window that is used to do fullscreen video. Sweeeeeeeeeet. 
            video_window = new FullscreenWindow (service.PrimaryWindow);
            video_window.Hidden += OnFullscreenWindowHidden;
            video_window.Realize ();
            video_window.Add (video_display);
            
            frame = new Hyena.Widgets.RoundedFrame ();
            frame.SetFillColor (new Cairo.Color (0, 0, 0));
            frame.DrawBorder = false;
            frame.Show ();
            
            PackStart (frame, true, true, 0);
            
            fullscreen_adapter = new FullscreenAdapter ();
            screensaver = new ScreensaverManager ();
        }
        
        public override void Dispose ()
        {
            base.Dispose ();
            screensaver.Dispose ();
        }

        
        private void MoveVideoExternal (bool hidden)
        {
            if (video_display.Parent != video_window) {
                video_display.Visible = !hidden;
                video_display.Reparent (video_window);
            }
        }
        
        private void MoveVideoInternal ()
        {
            if (video_display.Parent != frame) {
                video_display.Reparent (frame);
                video_display.Show ();
            }
        }
        
        protected override void OnRealized ()
        {
            base.OnRealized ();
            MoveVideoInternal ();
        }
        
        protected override void OnUnrealized ()
        {
            MoveVideoExternal (false);
            base.OnUnrealized ();
        }

#region Video Fullscreen Override

        private ViewActions.FullscreenHandler previous_fullscreen_handler;

        private void DisableFullscreenAction ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.ToggleAction action = service.ViewActions["FullScreenAction"] as Gtk.ToggleAction;
            if (action != null) {
                action.Active = false;
            }
        }

        internal void OverrideFullscreen ()
        {
            FullscreenHandler (false);
            
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            previous_fullscreen_handler = service.ViewActions.Fullscreen;
            service.ViewActions.Fullscreen = FullscreenHandler;
            DisableFullscreenAction ();
        }

        internal void RelinquishFullscreen ()
        {
            FullscreenHandler (false);
            
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> (); 
            if (service == null || service.ViewActions == null) {
                return;
            }
            
            service.ViewActions.Fullscreen = previous_fullscreen_handler;
        }
        
        private void OnFullscreenWindowHidden (object o, EventArgs args)
        {
            MoveVideoInternal ();
            DisableFullscreenAction ();
        }

        private void FullscreenHandler (bool fullscreen)
        {
            if (fullscreen) {
                MoveVideoExternal (true);
                video_window.ShowAll ();
                fullscreen_adapter.Fullscreen (video_window, true);
                screensaver.Inhibit ();
            } else {
                screensaver.UnInhibit ();
                fullscreen_adapter.Fullscreen (video_window, false);
                video_window.Hide ();
            }
        }
        
#endregion
        
#region ISourceContents
        
        public bool SetSource (ISource src)
        {
            this.source = source as NowPlayingSource;
            return this.source != null;
        }

        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Widget Widget {
            get { return this; }
        }
        
#endregion

    }
}
