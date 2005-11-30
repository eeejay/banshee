/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  CoverArtThumbnail.cs
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
using Gdk;

namespace Banshee.Widgets
{
    public class CoverArtThumbnail : EventBox
    {
        private Gtk.Image image;
        private Pixbuf pixbuf;
        private int size;
        private CoverArtPopup popup;
        
        public CoverArtThumbnail(int size) : base()
        {
            popup = new CoverArtPopup();
            
            popup.CursorLeave += delegate(object o, EventArgs args)
            {
                popup.Hide();
            };
            
            image = new Gtk.Image();
            image.Show();
            Add(image);

            EnterNotifyEvent += OnEnterNotifyEvent;
            LeaveNotifyEvent += OnLeaveNotifyEvent;
            
            SetSizeRequest(size, size);
            
            this.size = size;
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args)
        {
            popup.Show();
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
        {
            popup.Hide();
        }
        
        private Pixbuf CreateThumbnail(Pixbuf srcPixbuf)
        {
            Pixbuf th_pixbuf = srcPixbuf.ScaleSimple(size - 4, size - 4, InterpType.Bilinear);
            Pixbuf container = new Pixbuf(Gdk.Colorspace.Rgb, true, 8, size, size);
            Pixbuf container2 = new Pixbuf(Gdk.Colorspace.Rgb, true, 8, size - 2, size - 2);
            
            container.Fill(0x00000077);
            container2.Fill(0xffffff55);
            
            container2.CopyArea(0, 0, container2.Width, container2.Height, container, 1, 1);
            th_pixbuf.CopyArea(0, 0, th_pixbuf.Width, th_pixbuf.Height, container, 2, 2);
            
            return container;
        }
       
        public string Label {
            set {
                popup.Label = value == null ? String.Empty : value;
            }
        }
        
        public string FileName {
            set {
                try {
                    if(value == null) {
                        throw new ApplicationException("No Artwork");
                    }
                    
                    pixbuf = new Pixbuf(value);
                    image.Pixbuf = CreateThumbnail(pixbuf);
                    popup.Image = pixbuf;
                    Show();
                } catch(Exception e) {
                    Hide();
                    throw new ApplicationException(e.Message);
                }
            }
        }
    }

    public class CoverArtPopup : Gtk.Window
    {
        private Gtk.Image image;
        private Label label;
        
        public event EventHandler CursorLeave;
        
        public CoverArtPopup() : base(Gtk.WindowType.Popup)
        {
            VBox vbox = new VBox();
            Add(vbox);
            
            Decorated = false;
            BorderWidth = 6;
            
            SetPosition(WindowPosition.CenterAlways);

            LeaveNotifyEvent += OnLeaveNotifyEvent;
           
            image = new Gtk.Image();
            label = new Label("");
            label.CanFocus = false;
            
            label.ModifyBg(StateType.Normal, new Color(0, 0, 0));
            label.ModifyFg(StateType.Normal, new Color(160, 160, 160));
            ModifyBg(StateType.Normal, new Color(0, 0, 0));
            ModifyFg(StateType.Normal, new Color(160, 160, 160));
            
            vbox.PackStart(image, true, true, 0);
            vbox.PackStart(label, false, false, 0);
            
            vbox.Spacing = 6;
            vbox.ShowAll();
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
        {
            if(CursorLeave != null) {
                CursorLeave(this, new EventArgs());
            }    
        }
        
        public Pixbuf Image {
            set {
                image.Pixbuf = value;
                SetSizeRequest(value.Width, value.Height);
                Resize(value.Width, value.Height);
            }
        }
        
        public string Label {
            set {
                try {
                    label.Markup = String.Format("<small><b>{0}</b></small>", GLib.Markup.EscapeText(value));
                } catch(Exception) {
                }
            }
        }
    }
}
