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
        private VBox header_box = new VBox ();
        
        private Label position_label;
        private LinearProgress linear_progress;
    
        public TrackInfoPopup () : base (Gtk.WindowType.Popup)
        {
            BorderWidth = 4;
            AppPaintable = true;
            Resizable = false;
            
            // Position label and linear progress bar
            HBox position_box = new HBox ();
            //position_box.Spacing = 10;
            
            position_label = new Label ();
            position_label.Xalign = 0.0f;
            position_label.Ypad = 5;
            position_label.Yalign = 1.0f;
            position_label.ModifyFg (StateType.Normal, this.Style.Base(StateType.Active));
            
            VBox progress_box = new VBox ();
            linear_progress = new LinearProgress ();
            progress_box.PackStart (linear_progress, true, true, 6);
            
            position_box.PackStart (position_label, false, false, 6);
            position_box.PackStart (progress_box, true, true, 6);
            
            header = new TrackInfoDisplay ();
            header.SetSizeRequest (320, 64);
            
            Alignment alignment = new Alignment (1.0f, 1.0f, 0.0f, 0.0f);
            alignment.SetPadding (6, 3, 6, 3);
            alignment.Add (header);
            alignment.Show ();
            
            header_box.PackStart (alignment, true, true, 0);
            header_box.PackStart (position_box, false, false, 0);
            header.Show ();
            position_box.ShowAll ();
            
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
        
        private void UpdatePosition()
        {
            linear_progress.Fraction = (double)position / (double)duration;
            position_label.Markup = String.Format("<small>{0} of {1}</small>",
                    DateTimeUtil.FormatDuration(position), DateTimeUtil.FormatDuration(duration)); 
        }
        
        public uint Duration {
            set {
                duration = value;
                UpdatePosition();
            }
        }
        
        public uint Position {
            set {
                position = value;
                UpdatePosition();
            }
        }
    }
}
