//
// X11NotificationAreaBox.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Sebastian Dröge <slomo@circular-chaos.org>
//   Alexander Hixon <hixon.alexander@mediati.org>
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
using Mono.Unix;
using Gtk;

using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.NotificationArea
{
    public class X11NotificationAreaBox : X11NotificationArea, INotificationAreaBox
    {
        private EventBox event_box;
        private Image image;
        
        private TrackInfoPopup popup;
        private bool can_show_popup = false;
        private bool cursor_over_trayicon = false;
        
        public event EventHandler Disconnected;
        public event EventHandler Activated;
        public event PopupMenuHandler PopupMenuEvent;
        
        public Widget Widget {
            get { return event_box; }
        }
        
        public X11NotificationAreaBox () : base (Catalog.GetString ("Banshee"))
        {
            event_box = new EventBox ();
            image = new Image ();
            image.IconName = "music-player-banshee";
            event_box.Add (image);
            Add (event_box);
            
            event_box.ButtonPressEvent += OnButtonPressEvent;
            event_box.EnterNotifyEvent += OnEnterNotifyEvent;
            event_box.LeaveNotifyEvent += OnLeaveNotifyEvent;
            event_box.ScrollEvent += OnMouseScroll;
            
            ShowAll ();
        }
        
        public void PositionMenu (Menu menu, out int x, out int y, out bool push_in) 
        {
            PositionWidget (menu, out x, out y, 0);
            push_in = true;
        }
        
        private bool PositionWidget (Widget widget, out int x, out int y, int yPadding) 
        {
            int button_y, panel_width, panel_height;
            Gtk.Requisition requisition = widget.SizeRequest ();
            
            event_box.GdkWindow.GetOrigin (out x, out button_y);
            (event_box.Toplevel as Gtk.Window).GetSize (out panel_width, out panel_height);
            
            bool on_bottom = button_y + panel_height + requisition.Height >= event_box.Screen.Height;

            y = on_bottom
                ? button_y - requisition.Height - yPadding
                : button_y + panel_height + yPadding;
                
            return on_bottom;
        }
        
        private void HidePopup () 
        {
            if (popup == null) {
                return;
            }
            
            popup.Hide ();
            popup.Dispose ();
            popup = null;
        }
        
        private void ShowPopup () 
        {
            if (popup != null) {
                return;
            }
            
            popup = new TrackInfoPopup ();
            PositionPopup ();
            popup.Show ();
        }
        
        private void PositionPopup () 
        {
            int x, y;
            Gtk.Requisition event_box_req = event_box.SizeRequest ();
            Gtk.Requisition popup_req = popup.SizeRequest ();
            
            PositionWidget (popup, out x, out y, 5);
            
            x = x - (popup_req.Width / 2) + (event_box_req.Width / 2);     
            if (x + popup_req.Width >= event_box.Screen.Width) { 
                x = event_box.Screen.Width - popup_req.Width - 5;
            } else if (x < 5) {
                x = 5;
            }
            
            popup.Move (x, y);
        }
        
        private void OnButtonPressEvent (object o, ButtonPressEventArgs args)
        {
            if (args.Event.Type != Gdk.EventType.ButtonPress) {
                return;
            }
        
            switch (args.Event.Button) {
                case 1:
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        ServiceManager.PlaybackController.Next ();
                    } else {
                        OnActivated ();
                    }
                    break;
                case 2:
                    ServiceManager.PlayerEngine.TogglePlaying ();
                    break;
                case 3:
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        ServiceManager.PlaybackController.Next ();
                    } else {
                        OnPopupMenuEvent ();
                    }
                    break;
            }
        }
        
        private void OnMouseScroll (object o, ScrollEventArgs args)
        {
            switch (args.Event.Direction) {
                case Gdk.ScrollDirection.Up:
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        ServiceManager.PlayerEngine.Volume += (ushort)PlayerEngine.VolumeDelta;
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        ServiceManager.PlayerEngine.Position += PlayerEngine.SkipDelta;
                    } else {
                        ServiceManager.PlaybackController.Next ();
                    }
                    break;
                    
                case Gdk.ScrollDirection.Down:
                    if ((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        if (ServiceManager.PlayerEngine.Volume < (ushort)PlayerEngine.VolumeDelta) {
                            ServiceManager.PlayerEngine.Volume = 0;
                        } else {
                            ServiceManager.PlayerEngine.Volume -= (ushort)PlayerEngine.VolumeDelta;
                        }
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        ServiceManager.PlayerEngine.Position -= PlayerEngine.SkipDelta;
                    } else {
                        ServiceManager.PlaybackController.Previous ();
                    }
                    break;
            }
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args) 
        {
            cursor_over_trayicon = true;
            if (can_show_popup) {
                // only show the popup when the cursor is still over the
                // tray icon after 500ms
                GLib.Timeout.Add (500, delegate {
                    if (cursor_over_trayicon && can_show_popup) {
                        ShowPopup ();
                    }
                    return false;
                });
            }
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args) 
        {
            cursor_over_trayicon = false;
            HidePopup ();
        }
        
        public void PlayerEngineEventChanged (PlayerEngineEventArgs args) 
        {
            switch (args.Event) {
                case PlayerEngineEvent.StartOfStream:
                    can_show_popup = true;
                    break;
                    
                case PlayerEngineEvent.EndOfStream:
                    // only hide the popup when we don't play again after 250ms
                    GLib.Timeout.Add (250, delegate {
                        if (ServiceManager.PlayerEngine.CurrentState != PlayerEngineState.Playing) {
                            can_show_popup = false;
                            HidePopup ();
                         }
                         return false;
                    });
                    break;
            }
        }
        
        protected virtual void OnActivated ()
        {
            EventHandler handler = Activated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnPopupMenuEvent ()
        {
            PopupMenuHandler handler = PopupMenuEvent;
            if (handler != null) {
                handler (this, new PopupMenuArgs ());
            }
        }
        
        protected override bool OnDestroyEvent (Gdk.Event evnt)
        {
            bool result = base.OnDestroyEvent (evnt);
            
            EventHandler handler = Disconnected;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
            
            return result;
        }
    }
}
