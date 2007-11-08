/***************************************************************************
 *  StreamPositionLabel.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Mono.Unix;
using Gtk;

namespace Banshee.Widgets
{
    public class StreamPositionLabel : Alignment
    {
        private Gdk.GC bar_gc;
        private double buffering_progress;
        private bool is_buffering;
        private bool is_contacting;
        private SeekSlider seekRange;
        private Label label;
        private string format_string = "<small>{0}</small>";
        
        public StreamPositionLabel(SeekSlider seekRange) : base(0.0f, 0.0f, 1.0f, 1.0f)
        {
            AppPaintable = true;
            
            label = new Label();
            label.Show();
            Add(label);
            
            this.seekRange = seekRange;
            this.seekRange.ValueChanged += OnSliderUpdated;
            this.seekRange.DurationChanged += OnSliderUpdated;
            
            UpdateLabel();
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(bar_gc == null) {
                bar_gc = new Gdk.GC(GdkWindow);
                Gdk.Color color = DrawingUtilities.ColorBlend(Style.Background(StateType.Normal), 
                    Style.Foreground(StateType.Normal), 0.2);
                bar_gc.Background = color;
                bar_gc.Foreground = color;
            }
            
            int bar_width = (int)((double)Allocation.Width * buffering_progress);
            if(bar_width > 0 && is_buffering) {
                GdkWindow.DrawRectangle(bar_gc, true, Allocation.X, Allocation.Y, bar_width, Allocation.Height);
            }
            
            return base.OnExposeEvent(evnt);
        }
        
        private void UpdateLabel()
        {
            if(is_buffering) {
                double progress = buffering_progress * 100.0;
                UpdateLabel(Catalog.GetString("Buffering") + ": " + progress.ToString("0.0") + "%");
            } else if(is_contacting) {
                UpdateLabel(Catalog.GetString("Contacting..."));
            } else if(seekRange.Value == 0 && seekRange.Duration == 0) {
                UpdateLabel(Catalog.GetString("Idle"));
            } else if(seekRange.Value == seekRange.Duration) {
                UpdateLabel(FormatDuration((long)seekRange.Value));
            } else {
                UpdateLabel(String.Format(Catalog.GetString("{0} of {1}"),
                    FormatDuration((long)seekRange.Value), FormatDuration((long)seekRange.Adjustment.Upper)));
            }
        }
        
        private void UpdateLabel(string text)
        {
            label.Markup = String.Format(format_string, GLib.Markup.EscapeText(text));
        }
        
        private static string FormatDuration(long time)
        {
            time /= 1000;
            return (time > 3600 ? 
                    String.Format("{0}:{1:00}:{2:00}", time / 3600, (time / 60) % 60, time % 60) :
                    String.Format("{0}:{1:00}", time / 60, time % 60));
        }
        
        private void OnSliderUpdated(object o, EventArgs args)
        {
            UpdateLabel();
        }
        
        public double BufferingProgress {
            get { return buffering_progress; }
            set {
                buffering_progress = Math.Max(0.0, Math.Min(1.0, value));
                UpdateLabel();
                QueueDraw();
            }
        }
        
        public bool IsBuffering {
            get { return is_buffering; }
            set { 
                if(is_buffering != value) {
                    is_buffering = value;
                    UpdateLabel();
                    QueueDraw();
                }
            }
        }
        
        public bool IsContacting {
            get { return is_contacting; }
            set { 
                if(is_contacting != value) {
                    is_contacting = value;
                    UpdateLabel();
                    QueueDraw();
                }
            }
        }
        
        public string FormatString {
            set { 
                format_string = value;
                UpdateLabel();
            }
        }
    }
}
