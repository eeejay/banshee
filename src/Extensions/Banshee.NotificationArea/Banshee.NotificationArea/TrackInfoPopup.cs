//
// TrackInfoPopup.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

using Banshee.Base;
using Banshee.Gui.Widgets;
using Banshee.Widgets;
using Banshee.ServiceStack;
using Banshee.Gui;
using Hyena;

namespace Banshee.NotificationArea
{
    public class TrackInfoPopup : Gtk.Window
    {
        private uint position;
        private uint duration;
        private TrackInfoDisplay header;
        private HBox header_box = new HBox ();
    
        public TrackInfoPopup () : base (Gtk.WindowType.Popup)
        {
            BorderWidth = 4;
            AppPaintable = true;
            Resizable = false;
            
            header = new TrackInfoDisplay ();
            header.SetSizeRequest (300, 46);
            header_box.PackStart (header, true, true, 0);
            header.Show ();
            
            Add (header_box);
            header_box.Show ();
        }
        
        public override void Dispose ()
        {
            header.Dispose ();
            base.Dispose ();
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            Gtk.Style.PaintFlatBox (Style, GdkWindow, StateType.Normal, ShadowType.Out, evnt.Area, this, "tooltip", 
                0, 0, Allocation.Width, Allocation.Height);
            return base.OnExposeEvent (evnt);
        }
    }
}
