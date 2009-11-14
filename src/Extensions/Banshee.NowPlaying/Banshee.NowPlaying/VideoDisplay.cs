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

namespace Banshee.NowPlaying
{
    public abstract class VideoDisplay : Gtk.Widget, IVideoDisplay
    {
        private bool is_idle = true;

        public event EventHandler IdleStateChanged;

        public bool IsIdle {
            get { return is_idle; }
        }

        public VideoDisplay ()
        {
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                PlayerEvent.StartOfStream |
                PlayerEvent.EndOfStream);

            ToggleIdleVisibility ();
        }

        protected abstract Gdk.Window RenderWindow { get; }

        protected abstract void ExposeVideo (Gdk.EventExpose evnt);

        protected override void OnDestroyed ()
        {
            base.OnDestroyed ();
            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);
        }

        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            RenderWindow.DrawRectangle (Style.BlackGC, true,
                new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height));

            if (RenderWindow == null || !RenderWindow.IsVisible) {
                return true;
            }

            if (!is_idle && ServiceManager.PlayerEngine.VideoDisplayContextType != VideoDisplayContextType.Unsupported) {
                ExposeVideo (evnt);
            }

            return true;
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            ToggleIdleVisibility ();
        }

        private void ToggleIdleVisibility ()
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
            is_idle = track == null || (track.MediaAttributes & TrackMediaAttributes.VideoStream) == 0;
            QueueDraw ();

            OnIdleStateChanged ();
        }

        protected virtual void OnIdleStateChanged ()
        {
            EventHandler handler = IdleStateChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public new void QueueDraw ()
        {
            base.QueueDraw ();
            if (RenderWindow != null) {
                RenderWindow.InvalidateRect (new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height), true);
            }
        }
    }
}
