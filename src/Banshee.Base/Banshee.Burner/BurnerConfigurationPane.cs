/***************************************************************************
 *  BurnerConfigurationPane.cs
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

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;
using Banshee.Cdrom;
using Banshee.Cdrom.Gui;

namespace Banshee.Burner
{
    public class BurnerConfigurationPane : Gtk.Table
    {
        private DiscUsageDisplay usage_display;
        private Label data_usage_label;
        private Label audio_usage_label;
        private TimeSpan time_usage = TimeSpan.Zero;
        private long size_usage = 0;
        private BurnerSource source;
        
        internal BurnerConfigurationPane(BurnerSource source) : base(2, 3, false)
        {
            this.source = source;
            
            source.TrackAdded += OnTrackAdded;
            source.TrackRemoved += OnTrackRemoved;
            source.Session.RecorderMediaChanged += delegate {
                UpdateUsageDisplay();
            };
            
            usage_display = new DiscUsageDisplay();
            usage_display.Capacity = 700 * 1024 * 1024;
            usage_display.Usage = 0;
            
            AddWidget(usage_display, 0, 1, 0, 2);
            
            SizeAllocated += OnSizeAllocated;
            
            Label label = new Label(Catalog.GetString("Audio Disc:"));
            label.Xalign = 1.0f;
            AddWidget(label, 1, 2, 0, 1);
            
            label = new Label(Catalog.GetString("Data Disc:"));
            label.Xalign = 1.0f;
            AddWidget(label, 1, 2, 1, 2);
            
            audio_usage_label = new Label();
            audio_usage_label.Xalign = 0.0f;
            AddWidget(audio_usage_label, 2, 3, 0, 1);
            
            data_usage_label = new Label();
            data_usage_label.Xalign = 0.0f;
            AddWidget(data_usage_label, 2, 3, 1, 2);
            
            UpdateUsageDisplay();

            ColumnSpacing = 12;
            ShowAll();
        }

        private void AddWidget(Widget widget, uint x1, uint x2, uint y1, uint y2)
        {
            Attach(widget, x1, x2, y1, y2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        }
        
        private void OnTrackAdded(object o, TrackEventArgs args)
        {
            time_usage += args.Track.Duration;
            
            try {
                System.IO.FileInfo info = new System.IO.FileInfo(args.Track.Uri.LocalPath);
                size_usage += info.Length;
            } catch {
            }
            
            UpdateUsageDisplay();
        }
        
        private void OnTrackRemoved(object o, TrackEventArgs args)
        {
            time_usage -= args.Track.Duration;
            
            try {
                System.IO.FileInfo info = new System.IO.FileInfo(args.Track.Uri.LocalPath);
                size_usage -= info.Length;
            } catch {
            }
            
            UpdateUsageDisplay();
        }
        
        private void UpdateUsageDisplay()
        {
            long capacity = source.Session.Recorder == null ? 0 : source.Session.Recorder.MediaSize;
            TimeSpan time_capacity = BurnerUtilities.DiscSizeToTime(capacity);
            
            usage_display.Usage = (long)time_usage.TotalSeconds;
            usage_display.Capacity = (long)time_capacity.TotalSeconds;
            
            audio_usage_label.Text = String.Format(Catalog.GetString("{0}:{1:00} of {2} minutes"),
                (time_usage.Hours * 60) + time_usage.Minutes, time_usage.Seconds, 
                (int)time_capacity.TotalMinutes);
                
            data_usage_label.Text = String.Format(Catalog.GetString("{0} of {1} MB"), 
                size_usage / 1048675, capacity / 1048576);
        }
        
        private void OnSizeAllocated(object o, SizeAllocatedArgs args)
        {
            SizeAllocated -= OnSizeAllocated;
            
            // hack to hopefully force the cairo hinting to not suck
            int height = args.Allocation.Height;
            if(height % 2 == 0) {
                height++;
            }
                
            height += 16;
            
            usage_display.SetSizeRequest(height, height);
        }
    }
}
