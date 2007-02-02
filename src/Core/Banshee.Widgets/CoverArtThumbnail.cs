/***************************************************************************
 *  CoverArtThumbnail.cs
 *
 *  Copyright (C) 2005-2007 Novell, Inc.
 *  Written by Aaron Bockover <abockover@novell.com>
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
        private Pixbuf no_artwork_pixbuf;
        private int size;
        private CoverArtPopup popup;
        private bool loaded;
        private bool in_popup;
        private bool in_eventbox;
        
        public CoverArtThumbnail(int size) : base()
        {
            popup = new CoverArtPopup();
            
            popup.CursorLeave += delegate(object o, EventArgs args)
            {
                popup.Hide();
            };

            popup.EnterNotifyEvent += OnPopupEnterNotifyEvent;
            popup.LeaveNotifyEvent += OnPopupLeaveNotifyEvent;
            
            image = new Gtk.Image();
            image.Show();
            Add(image);

            EnterNotifyEvent += OnEnterNotifyEvent;
            LeaveNotifyEvent += OnLeaveNotifyEvent;
            
            SetSizeRequest(size, size);
            
            this.size = size;
        }

        private void OnPopupEnterNotifyEvent(object o, EnterNotifyEventArgs args)
        {
            in_popup = true;
        }
        
        private void OnPopupLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
        {
            in_popup = false;
            popup.Hide();
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args)
        {
            in_eventbox = true;
            GLib.Timeout.Add(500, delegate {
                if(in_eventbox && loaded) {
                    popup.Show();
                }
                return false;
            });
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
        {
            in_eventbox = false;
            GLib.Timeout.Add (100, delegate {
                if (!in_popup) {
                    popup.Hide();
                }

                return false;
            });
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
        
        public Pixbuf NoArtworkPixbuf {
            set {
                no_artwork_pixbuf = value;
            }
        }
        
        public string FileName {
            set {
                try {
                    if(value == null) {
                        throw new ApplicationException("No Artwork");
                    }
                    
                    SetPixbuf(new Pixbuf(value));
                    loaded = true;
                } catch(Exception e) {
                    loaded = false;
                    if(no_artwork_pixbuf != null) {
                        SetPixbuf(no_artwork_pixbuf);
                        return;
                    }
                    
                    Hide();
                    throw new ApplicationException(e.Message);
                }
            }
        }
        
        private void SetPixbuf(Pixbuf pixbuf)
        {
            this.pixbuf = pixbuf;
            image.Pixbuf = CreateThumbnail(this.pixbuf);
            popup.Image = this.pixbuf;
            Show();
        }
    
        private class CoverArtPopup : Gtk.Window
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
                    int width = value.Width, height = value.Height;
                    
                    if(height >= Screen.Height * 0.75) {
                        width = (int)(width * ((Screen.Height * 0.75) / height));
                        height = (int)(Screen.Height * 0.75);
                    }

                    if(width >= Screen.Width * 0.75) {
                        height = (int)(height * ((Screen.Width * 0.75) / width));
                        width = (int)(Screen.Width * 0.75);
                    }

                    if(width != value.Width || height != value.Height) {
                        image.Pixbuf = value.ScaleSimple(width, height, InterpType.Bilinear);
                    } else {
                        image.Pixbuf = value;
                    }
                    
                    image.SetSizeRequest(image.Pixbuf.Width, image.Pixbuf.Height);
                    Resize(1, 1);
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
}
