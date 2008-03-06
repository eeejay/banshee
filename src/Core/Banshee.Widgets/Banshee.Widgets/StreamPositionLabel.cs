//
// StreamPositionLabel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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

namespace Banshee.Widgets
{
    public class StreamPositionLabel : Alignment
    {
        private double buffering_progress;
        private bool is_buffering;
        private bool is_contacting;
        private SeekSlider seekRange;
        private string format_string = "<small>{0}</small>";
        private Pango.Layout layout;
        
        public StreamPositionLabel (SeekSlider seekRange) : base (0.0f, 0.0f, 1.0f, 1.0f)
        {
            AppPaintable = true;
            
            this.seekRange = seekRange;
            this.seekRange.ValueChanged += OnSliderUpdated;
            this.seekRange.DurationChanged += OnSliderUpdated;

            BuildLayouts ();
            UpdateLabel ();
        }
        
        private void BuildLayouts ()
        {
            layout = new Pango.Layout (PangoContext);
            layout.FontDescription = PangoContext.FontDescription.Copy ();
            layout.Ellipsize = Pango.EllipsizeMode.None;
            
            layout.SetMarkup (String.Format (format_string, "W"));
            
            int width, height;
            layout.GetPixelSize (out width, out height);
            HeightRequest = height;
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            int bar_width = (int)((double)Allocation.Width * buffering_progress);
            bool render_bar = false;
            
            if (bar_width > 0 && is_buffering) {
                bar_width -= 2 * Style.XThickness;
                render_bar = true;
                
                Gtk.Style.PaintBox (Style, GdkWindow, StateType.Normal, ShadowType.In, evnt.Area, this, null, 
                    Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
                
                if (bar_width > 0) {
                    Gtk.Style.PaintBox (Style, GdkWindow, StateType.Selected, ShadowType.EtchedOut, 
                        evnt.Area, this, "bar", 
                        Allocation.X + Style.XThickness, Allocation.Y + Style.YThickness, 
                        bar_width, Allocation.Height - 2 * Style.YThickness);
                }
            }
            
            int width, height;
            layout.GetPixelSize (out width, out height);
            
            int x = Allocation.X + ((Allocation.Width - width) / 2);
            int y = Allocation.Y + ((Allocation.Height - HeightRequest) / 2);
            Gdk.Rectangle rect = evnt.Area;
            
            if (render_bar) {
                width = bar_width + Style.XThickness;
                rect = new Gdk.Rectangle (evnt.Area.X, evnt.Area.Y, width, evnt.Area.Height);
                Gtk.Style.PaintLayout (Style, GdkWindow, StateType.Selected, true, rect, this, null, x, y, layout);
                
                rect.X += rect.Width;
                rect.Width = evnt.Area.Width - rect.Width;
            }
            
            Gtk.Style.PaintLayout (Style, GdkWindow, StateType.Normal, true, rect, this, null, x, y, layout);
            
            return true;
        }
        
        private void UpdateLabel ()
        {
            if (is_buffering) {
                double progress = buffering_progress * 100.0;
                UpdateLabel (Catalog.GetString("Buffering") + ": " + progress.ToString ("0.0") + "%");
            } else if (is_contacting) {
                UpdateLabel (Catalog.GetString ("Contacting..."));
            } else if (seekRange.Value == 0 && seekRange.Duration == 0) {
                UpdateLabel (Catalog.GetString ("Idle"));
            } else if (seekRange.Value == seekRange.Duration) {
                UpdateLabel (FormatDuration ((long)seekRange.Value));
            } else {
                UpdateLabel (String.Format (Catalog.GetString ("{0} of {1}"),
                    FormatDuration ((long)seekRange.Value), FormatDuration ((long)seekRange.Adjustment.Upper)));
            }
        }
        
        private void UpdateLabel (string text)
        {
            layout.SetMarkup (String.Format (format_string, GLib.Markup.EscapeText (text)));
            QueueDraw ();
        }
        
        private static string FormatDuration (long time)
        {
            time /= 1000;
            return (time > 3600 ? 
                    String.Format ("{0}:{1:00}:{2:00}", time / 3600, (time / 60) % 60, time % 60) :
                    String.Format ("{0}:{1:00}", time / 60, time % 60));
        }
        
        private void OnSliderUpdated (object o, EventArgs args)
        {
            UpdateLabel ();
        }
        
        public double BufferingProgress {
            get { return buffering_progress; }
            set {
                buffering_progress = Math.Max (0.0, Math.Min (1.0, value));
                UpdateLabel ();
                QueueDraw ();
            }
        }
        
        public bool IsBuffering {
            get { return is_buffering; }
            set { 
                if (is_buffering != value) {
                    is_buffering = value;
                    UpdateLabel ();
                    QueueDraw ();
                }
            }
        }
        
        public bool IsContacting {
            get { return is_contacting; }
            set { 
                if (is_contacting != value) {
                    is_contacting = value;
                    UpdateLabel ();
                    QueueDraw ();
                }
            }
        }
        
        public string FormatString {
            set { 
                format_string = value;
                UpdateLabel ();
            }
        }
    }
}
