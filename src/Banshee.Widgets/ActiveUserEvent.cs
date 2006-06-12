
/***************************************************************************
 *  ActiveUserEvent.cs
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
using Mono.Unix;
 
namespace Banshee.Widgets
{
    public class ActiveUserEvent : IDisposable
    {
        private Table table;
        private Image icon;
        private Label header_label;
        private Label message_label;
        private ProgressBar progress_bar;
        private Button cancel_button;
        private Tooltips tips;
        
        private double progress = 0.0;
        private string name;
        private string message;
        private string header;
        
        private uint timeout_id = 0;
        private uint slow_timeout_id = 0;
        private bool disposed = false;
        
        public event EventHandler Disposed;
        public event EventHandler CancelRequested;
        
        private bool cancel_requested;
        private bool can_cancel;

        public ActiveUserEvent(string name) : this (name, false) {}

        public ActiveUserEvent(string name, bool delay_show)
        {
            tips = new Tooltips();
            
            table = new Table(3, 2, false);
            header_label = new EllipsizeLabel();
            message_label = new EllipsizeLabel();
            progress_bar = new ProgressBar();
            icon = new Image();
            cancel_button = new Button();
            cancel_button.Add(new Image("gtk-cancel", IconSize.Menu));
            cancel_button.Clicked += delegate(object o, EventArgs args) 
            {
                HigMessageDialog md = new HigMessageDialog(null, 
                    DialogFlags.Modal, MessageType.Question,
                    ButtonsType.YesNo,
                    Catalog.GetString("Cancel Operation"),
                    String.Format(Catalog.GetString(
                        "Are you sure you want to cancel the '{0}' operation?"), name));
                        
                if(md.Run() == (int)ResponseType.Yes) {
                    Cancel();
                }
        
                md.Destroy();
            };
            
            header_label.Xalign = 0.0f;
            message_label.Xalign = 0.0f;
            
            table.Attach(header_label, 0, 3, 0, 1, 
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
            
            table.Attach(message_label, 0, 3, 1, 2, 
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
                
            table.Attach(icon, 0, 1, 2, 3, 
                AttachOptions.Shrink | AttachOptions.Fill,
                AttachOptions.Shrink | AttachOptions.Fill, 0, 0);
                
            table.Attach(progress_bar, 1, 2, 2, 3,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink, 0, 0);
                
            table.Attach(cancel_button, 2, 3, 2, 3,
                AttachOptions.Shrink | AttachOptions.Fill,
                AttachOptions.Shrink | AttachOptions.Fill, 0, 0);
                
            table.ColumnSpacing = 5;
            table.RowSpacing = 2;
            
            progress_bar.SetSizeRequest(0, -1);
            
            Name = name;
            Progress = 0.0;
            
            table.ShowAll();
            
            if (delay_show)
                slow_timeout_id = GLib.Timeout.Add(1000, OnCheckForDisplay);
            else
                ActiveUserEventsManager.Instance.Register(this);
        }
        
        public void Cancel()
        {
            EventHandler handler = CancelRequested; 
            if(handler != null) {
                handler(this, new EventArgs());
            }    
            
            cancel_requested = true;       
        }
        
        public void Dispose()
        {
            disposed = true;
            
            if(timeout_id > 0) {
                GLib.Source.Remove(timeout_id);
                timeout_id = 0;
            }

            if(slow_timeout_id > 0) {
                GLib.Source.Remove(slow_timeout_id);
                slow_timeout_id = 0;
            }
            
            if(Disposed != null) {
                Disposed(this, new EventArgs());
            }
        }

        private bool OnCheckForDisplay()
        {
            if (disposed)
                return false;

            // If the event has not made enough progress, show this event
            if (Progress < 0.33)
                ActiveUserEventsManager.Instance.Register(this);

            return false;
        }
        
        private bool OnTimeout()
        {
            progress_bar.Pulse();
            return true;
        }
        
        private void UpdateLabel()
        {
            Gtk.Application.Invoke(delegate {
                header_label.Visible = header != null;
                
                if(header != null) {
                    header_label.Markup = String.Format("<small><b>{0}</b></small>", GLib.Markup.EscapeText(header));
                }
                
                if(message == null && name != null) {
                    message = name;
                } else if(message == null && name == null) {
                    message = "Performing Task";
                }
                
                message_label.Markup = String.Format("<small>{0}</small>", GLib.Markup.EscapeText(message));
                
                string tip = name + ": " + message;
                tips.SetTip(message_label, tip, tip);
                tips.SetTip(icon, tip, tip);
            });
        }

        public string Name {
            set {
                name = value;
                UpdateLabel();
            }
        }
        
        public string Message {
            set {
                message = value;
                UpdateLabel();
            }
        }
        
        public string Header {
            set {
                header = value;
                UpdateLabel();
            }
        }
        
        public double Progress {
            set {
                if(value <= 0.0 && !disposed && timeout_id == 0) {
                    timeout_id = GLib.Timeout.Add(100, OnTimeout);
                } else if(timeout_id > 0) {
                    GLib.Source.Remove(timeout_id);
                    timeout_id = 0;
                }
            
                Gtk.Application.Invoke(delegate {
                    progress_bar.Fraction = value;
                    progress = value;
                    progress_bar.Text = String.Format("{0}%", (int)(value * 100.0));
                });
            }
            
            get {
                return progress;
            }
        }
        
        public bool CanCancel {
            get {
                return can_cancel;
            }
            
            set {
                can_cancel = value;
                Gtk.Application.Invoke(delegate {
                    cancel_button.Sensitive = value;
                });
            }
        }
        
        public bool IsCancelRequested {
            get {
                return cancel_requested;
            }
        }
        
        public Gdk.Pixbuf Icon {
            set {
                Gtk.Application.Invoke(delegate {
                    icon.Pixbuf = value;
                });
            }
        }
        
        public Widget Widget {
            get {
                return table;
            }
        }
    }
}
