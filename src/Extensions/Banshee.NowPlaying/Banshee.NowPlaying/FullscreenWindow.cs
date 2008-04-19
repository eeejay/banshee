// 
// FullScreenWindow.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Larry Ewing <lewing@novell.com>
//   Gabriel Burt <gburt@novell.com>
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

namespace Banshee.NowPlaying
{
    public class FullscreenWindow : Window
    {
        private Gtk.Window parent;
        
        public FullscreenWindow (Window parent) : base (parent.Title)
        {
            this.parent = parent;
            
            Deletable = false;
            KeepAbove = true;
            Decorated = false;
            TransientFor = null;
            
            Gdk.Screen screen = Screen;
            int monitor = screen.GetMonitorAtWindow (parent.GdkWindow);
            Gdk.Rectangle bounds = screen.GetMonitorGeometry (monitor);
            Move (bounds.X, 0);
            SetDefaultSize (bounds.Width, bounds.Height);
        }
        
        protected override void OnRealized ()
        {
            Events |= Gdk.EventMask.PointerMotionMask | Gdk.EventMask.PointerMotionHintMask;
            base.OnRealized ();
            
            Screen.SizeChanged += OnScreenSizeChanged;
        }
        
        protected override void OnUnrealized ()
        {
            base.OnUnrealized ();
            Screen.SizeChanged -= OnScreenSizeChanged;
        }
        
        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
            Hide ();
            return true;
        }
        
        protected override void OnShown ()
        {
            base.OnShown ();
            OnHideCursorTimeout ();
            parent.AddNotification ("is-active", ParentActiveNotification);
        }
        
        protected override void OnHidden ()
        {
            base.OnHidden ();
            parent.RemoveNotification ("is-active", ParentActiveNotification);
        }
        
        private void OnScreenSizeChanged (object o, EventArgs args)
        {
        }
        
        private void ParentActiveNotification (object o, GLib.NotifyArgs args)
        {
            // If our parent window is ever somehow activated while we are
            // visible, this will ensure we merge back into the parent
            if (parent.IsActive) {
                parent.GdkWindow.SkipPagerHint = false;
                parent.GdkWindow.SkipTaskbarHint = false;
                Hide ();
            } else {
                parent.GdkWindow.SkipPagerHint = true;
                parent.GdkWindow.SkipTaskbarHint = true;
            }
        }

        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            switch (evnt.Key) {
                case Gdk.Key.Escape: 
                    Unfullscreen ();
                    Hide ();
                    return true;
            }
            
            return base.OnKeyPressEvent (evnt);
        }
        
#region Mouse Cursor Polish

        private const int CursorUpdatePositionDelay = 500;   // How long (ms) before the cursor position is updated
        private const int CursorHideDelay = 2500;            // How long (ms) to remain stationary before it hides
        private const int CursorShowMovementThreshold = 150; // How far (px) to move before it shows again
        
        private uint hide_cursor_timeout_id;
        private uint cursor_update_position_timeout_id;
        private int hide_cursor_x;
        private int hide_cursor_y;
        private bool cursor_is_hidden = false;
        
        protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
        {
            if (cursor_is_hidden) {
                if (Math.Abs (hide_cursor_x - evnt.X) > CursorShowMovementThreshold || 
                    Math.Abs (hide_cursor_y - evnt.Y) > CursorShowMovementThreshold) {
                    ShowCursor ();
                } else {
                    if (cursor_update_position_timeout_id > 0) {
                        GLib.Source.Remove (cursor_update_position_timeout_id);
                    }
                    
                    cursor_update_position_timeout_id = GLib.Timeout.Add (CursorUpdatePositionDelay, 
                        OnCursorUpdatePositionTimeout);
                }        
            } else {
                if (hide_cursor_timeout_id > 0) {
                    GLib.Source.Remove (hide_cursor_timeout_id);
                }
                
                hide_cursor_timeout_id = GLib.Timeout.Add (CursorHideDelay, OnHideCursorTimeout);
            }
            
            return base.OnMotionNotifyEvent (evnt);
        }
        
        private bool OnCursorUpdatePositionTimeout ()
        {
            UpdateHiddenCursorPosition ();
            cursor_update_position_timeout_id = 0;
            return false;
        }
        
        private bool OnHideCursorTimeout ()
        {
            HideCursor ();
            hide_cursor_timeout_id = 0;
            return false;
        }
        
        private void UpdateHiddenCursorPosition ()
        {
            GetPointer (out hide_cursor_x, out hide_cursor_y);
        }
        
        private void ShowCursor ()
        {
            cursor_is_hidden = false;
            GdkWindow.Cursor = null;
        }
        
        private void HideCursor ()
        {
            if (GdkWindow == null) {
                return;
            }
            
            Gdk.Pixmap pixmap = Gdk.Pixmap.CreateBitmapFromData (GdkWindow, "0x0", 1, 1);
            if (pixmap == null) {
                return;
            }
            
            UpdateHiddenCursorPosition ();
            cursor_is_hidden = true;
            
            Gdk.Color color = new Gdk.Color (0, 0, 0);
            Gdk.Cursor cursor = new Gdk.Cursor (pixmap, pixmap, color, color, 0, 0);
            
            GdkWindow.Cursor = cursor;
            
            pixmap.Dispose ();
            cursor.Dispose ();  
        }
        
#endregion

    }
}
