/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
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
 
namespace Banshee
{
    public class ActiveUserEvent : IDisposable
    {
        private Table table;
        private Image icon;
        private Label message_label;
        private ProgressBar progress_bar;
        private Button cancel_button;
        private Tooltips tips;
        
        private string name;
        private string message;
        
        private uint timeout_id = 0;
        private bool disposed = false;
        
        public event EventHandler Disposed;
        public event EventHandler CancelRequested;
        
        private bool cancel_requested;
     
        public ActiveUserEvent(string name) 
        {
            tips = new Tooltips();
            
            table = new Table(2, 2, false);
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
            
            message_label.Xalign = 0.0f;
            
            table.Attach(message_label, 0, 3, 0, 1, 
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
                
            table.Attach(icon, 0, 1, 1, 2, 
                AttachOptions.Shrink | AttachOptions.Fill,
                AttachOptions.Shrink | AttachOptions.Fill, 0, 0);
                
            table.Attach(progress_bar, 1, 2, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink, 0, 0);
                
            table.Attach(cancel_button, 2, 3, 1, 2,
                AttachOptions.Shrink | AttachOptions.Fill,
                AttachOptions.Shrink | AttachOptions.Fill, 0, 0);
                
            table.ColumnSpacing = 5;
            table.RowSpacing = 2;
            
            progress_bar.SetSizeRequest(0, -1);
            
            Name = name;
            Progress = 0.0;
            
            table.ShowAll();
            
            ActiveUserEventsManager.Instance.Register(this);
        }
        
        public void Cancel()
        {
            if(CancelRequested != null) {
                CancelRequested(this, new EventArgs());
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
            
            if(Disposed != null) {
                Disposed(this, new EventArgs());
            }
        }
        
        private bool OnTimeout()
        {
            progress_bar.Pulse();
            return true;
        }
        
        private void UpdateLabel()
        {
            Core.ProxyToMainThread(delegate {
                if(name == null) {
                    name = "Working";
                }
                
                if(message == null && name != null) {
                    message = name;
                } else if(message == null && name == null) {
                    message = "Performing Task";
                }
                
                message_label.Markup = String.Format("<small>{0}</small>", message);
                
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
        
        public double Progress {
            set {
                if(value <= 0.0 && !disposed) {
                    timeout_id = GLib.Timeout.Add(100, OnTimeout);
                } else if(timeout_id > 0) {
                    GLib.Source.Remove(timeout_id);
                    timeout_id = 0;
                }
            
                Core.ProxyToMainThread(delegate {
                    progress_bar.Fraction = value;
                    progress_bar.Text = String.Format("{0}%", (int)(value * 100.0));
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
                icon.Pixbuf = value;
            }
        }
        
        public Widget Widget {
            get {
                return table;
            }
        }
    }
}
