//
// FullScreenAdapter.cs
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
using System.Runtime.InteropServices;
using Gtk;

using Hyena;
using Banshee.NowPlaying;

namespace Banshee.NowPlaying.X11
{
    public class FullscreenAdapter : IFullscreenAdapter
    {
        private class BaconResize : GLib.InitiallyUnowned
        {
            [DllImport ("libbnpx11")]
            private static extern IntPtr bacon_resize_get_type ();
    
            public static new GLib.GType GType {
                get { return new GLib.GType (bacon_resize_get_type ()); }
            }
        
            public BaconResize (Gtk.Window window) : base (IntPtr.Zero)
            {
                this.window = window;
                
                GLib.Value window_val = new GLib.Value (window);
                CreateNativeObject (
                    new string [] { "video-widget" }, 
                    new GLib.Value [] { window_val }
                );
                window_val.Dispose ();
            }
            
            private Window window;
            public Window Window {
                get { return window; }
            }
            
            [DllImport ("libbnpx11")]
            private static extern void bacon_resize_resize (IntPtr handle);
            
            public void Resize ()
            {
                bacon_resize_resize (Handle);
            }
            
            [DllImport ("libbnpx11")]
            private static extern void bacon_resize_restore (IntPtr handle);
            
            public void Restore ()
            {
                bacon_resize_restore (Handle);
            }
            
            [GLib.Property ("have-xvidmode")]
            public bool HaveXVidMode {
                get {
                    GLib.Value value = GetProperty ("have-xvidmode");
                    bool ret = (bool)value;
                    value.Dispose ();
                    return ret;
                }
            }
        }
        
        private BaconResize resize;
        
        public void Fullscreen (Window window, bool fullscreen)
        {
            // Create the Bacon X11 Resizer if we haven't before or the window changes
            if (resize == null || resize.Window != window) {
                if (resize != null) {
                    resize.Dispose ();
                }
                
                resize = new BaconResize (window);
                Log.DebugFormat ("X11 Fullscreen Window Set (HaveXVidMode = {0})", resize.HaveXVidMode);
            }
            
            // Do the default GTK fullscreen operation
            if (fullscreen) {
                window.Fullscreen ();
            } else {
                window.Unfullscreen ();
            }
            
            // Skip if we don't support xvidmode, otherwise do the good resizing
            if (!resize.HaveXVidMode) {
                return;
            }
            
            if (fullscreen) {
                resize.Resize ();
            } else {
                resize.Restore ();
            }
        }
        
        public void Dispose ()
        {
            if (resize != null) {
                resize.Dispose ();
                resize = null;
            }
        }
    }
}
