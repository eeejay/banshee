
/***************************************************************************
 *  TrackInfoPopup.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using Gtk;

namespace Banshee.Widgets
{
    public class TrackInfoPopup : Gtk.Window
    {
        private uint position;
        private uint duration;
        private TrackInfoHeader header;
        private Label position_label;
        private LinearProgress linear_progress;
    
        public TrackInfoPopup() : base(Gtk.WindowType.Popup)
        {
            BorderWidth = 8;
            AppPaintable = true;
            Resizable = false;

            HBox box = new HBox();
            box.Spacing = 10;
            
            header = new TrackInfoHeader(false, 52);
            
            HBox position_box = new HBox();
            position_box.Spacing = 10;
            
            position_label = new Label();
            position_label.Xalign = 0.0f;
            position_label.Ypad = 5;
            position_label.Yalign = 1.0f;
            
            VBox progress_box = new VBox();
            linear_progress = new LinearProgress();
            progress_box.PackStart(linear_progress, true, true, 6);

            position_box.PackStart(position_label, false, false, 0);
            position_box.PackStart(progress_box, true, true, 5);
            
            header.VBox.PackStart(position_box, false, false, 0);
            header.DefaultCover = null;
            box.PackStart(header, true, true, 0);
            
            Add(box);
            box.ShowAll();
            
            //Name = "gtk-tooltips";
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            GdkWindow.DrawRectangle(Style.ForegroundGC(StateType.Normal), true, Allocation);
            GdkWindow.DrawRectangle(Style.BackgroundGC(StateType.Normal), true, 
                Allocation.X + 1, Allocation.Y + 1, Allocation.Width - 2, Allocation.Height - 2);
            base.OnExposeEvent(evnt);
            return true;
        }
        
        private void UpdateSize()
        {
            QueueResize();
        }
        
        private void UpdatePosition()
        {
            linear_progress.Fraction = (double)position / (double)duration;
            position_label.Markup = String.Format("<small>{0:00}:{1:00} of {2:00}:{3:00}</small>",
                position / 60, position % 60, duration / 60, duration % 60); 
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
        
        public string Artist {
            set {
                header.Artist = value;
                UpdateSize();
            }
        }
        
        public string Album {
            set {
                header.Album = value;
                UpdateSize();
            }
        }
        
        public string TrackTitle {
            set {
                header.Title = value;
                UpdateSize();
            }
        }
        
        public string CoverArtFileName {
            set {
                header.Cover.FileName = value;
                UpdateSize();
            }
        }
    }
}
