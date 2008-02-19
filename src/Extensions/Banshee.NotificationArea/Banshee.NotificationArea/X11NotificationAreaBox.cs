//
// X11NotificationAreaBox.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.NotificationArea
{
    public class X11NotificationAreaBox : X11NotificationArea, INotificationAreaBox
    {
        private EventBox event_box;
        private Image image;
        
        public event EventHandler Disconnected;
        public event EventHandler Activated;
        public event PopupMenuHandler PopupMenuEvent;
        
        public X11NotificationAreaBox () : base (Catalog.GetString ("Banshee"))
        {
            event_box = new EventBox ();
            image = new Image ();
            image.IconName = "music-player-banshee";
            event_box.Add (image);
            Add (event_box);
            
            event_box.ButtonPressEvent += OnButtonPressEvent;
            //event_box.EnterNotifyEvent += OnEnterNotifyEvent;
            //event_box.LeaveNotifyEvent += OnLeaveNotifyEvent;
            //event_box.ScrollEvent += OnMouseScroll;
            
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
        
        /*
        private TrackInfoPopup popup;
        private bool can_show_popup = false;
        private bool cursor_over_trayicon = false;
        private bool show_notifications = false;
        private TrackInfo current_track = null;
        private string notify_last_title = null;
        private string notify_last_artist = null;

        private static readonly uint SkipDelta = 10;
        private static readonly int VolumeDelta = 10;
        
        protected override void PluginInitialize()
        {
        }
        
        protected override void InterfaceInitialize() 
        {

            
            popup = new TrackInfoPopup();
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;

            // When we're already playing fill the TrackInfoPopup with the current track
            if (PlayerEngineCore.CurrentState == PlayerEngineState.Playing) {
                FillPopup();
            }

            // Forcefully load this value
            show_notifications = ShowNotifications;
            elements_service.MainWindow.KeyPressEvent += OnKeyPressEvent;
        }

        protected override void PluginDispose() 
        {

        }

        public override Gtk.Widget GetConfigurationWidget()
        {            
            return new NotificationAreaIconConfigPage(this);
        }

        private void ShowNotification()
        {
            // This has to happen before the next if, otherwise the last_* members aren't set correctly.
            if(current_track == null || (notify_last_title == current_track.DisplayTrackTitle 
                && notify_last_artist == current_track.DisplayArtistName)) {
                return;
            }
            
            notify_last_title = current_track.DisplayTrackTitle;
            notify_last_artist = current_track.DisplayArtistName;

            if(cursor_over_trayicon || !show_notifications || elements_service.MainWindow.HasToplevelFocus) {
                return;
            }
            
            string message = String.Format("{0}\n<i>{1}</i>", 
                GLib.Markup.EscapeText(current_track.DisplayTrackTitle),
                GLib.Markup.EscapeText(current_track.DisplayArtistName));
            
            Gdk.Pixbuf image = null;
            
            try {
                if(current_track.CoverArtFileName != null) {
                    image = new Gdk.Pixbuf(current_track.CoverArtFileName);
                } 
            } catch {
            }
            
            if(image == null) {
                image = Branding.DefaultCoverArt;
            }
            
            image = image.ScaleSimple(42, 42, Gdk.InterpType.Bilinear);
            
            try {
                Notification nf = new Notification(Catalog.GetString("Now Playing"), message, image, event_box);
                nf.Urgency = Urgency.Low;
                nf.Timeout = 4500;
                nf.Show();
            } catch(Exception e) {
                LogCore.Instance.PushError(Catalog.GetString("Cannot show notification"), e.Message, false);
            }
        }

        [GLib.ConnectBefore]
        private void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            bool handled = false;
            
            if (args.Event.Key == Gdk.Key.w && (args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                handled = true;
                ShowHideMainWindow();
            }
            
            args.RetVal = handled;
        }

        

        private void OnItemRatingActivated(object o, EventArgs args)
        {
            if(PlayerEngineCore.CurrentTrack != null) {
                PlayerEngineCore.CurrentTrack.Rating = (uint)rating_menu_item.Value;
                PlayerEngineCore.TrackInfoUpdated();
            }
        }
        
        private void ToggleRatingMenuSensitive() 
        {
            if(PlayerEngineCore.CurrentTrack != null && (SourceManager.ActiveSource is LibrarySource || 
                SourceManager.ActiveSource is PlaylistSource ||
                SourceManager.ActiveSource is SmartPlaylistSource)) {
                rating_menu_item.Reset((int)PlayerEngineCore.CurrentTrack.Rating);
                rating_menu_item.Show();
            } else {
                rating_menu_item.Hide();
            }
        }

        private void OnMouseScroll(object o, ScrollEventArgs args) 
        {
            switch(args.Event.Direction) {
                case Gdk.ScrollDirection.Up:
                    if((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        PlayerEngineCore.Volume += (ushort)VolumeDelta;
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        PlayerEngineCore.Position += SkipDelta;
                    } else {
                        Globals.ActionManager["NextAction"].Activate();
                    }
                    break;
                case Gdk.ScrollDirection.Down:
                    if((args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
                        if (PlayerEngineCore.Volume < (ushort)VolumeDelta)
                            PlayerEngineCore.Volume = 0;
                        else
                            PlayerEngineCore.Volume -= (ushort)VolumeDelta;
                    } else if((args.Event.State & Gdk.ModifierType.ShiftMask) != 0) {
                        PlayerEngineCore.Position -= SkipDelta;
                    } else {
                        Globals.ActionManager["PreviousAction"].Activate();
                    }
                    break;
            }
        }

        private void HidePopup() 
        {
            popup.Hide();
        }
        
        private void ShowPopup() 
        {
            PositionPopup();
            popup.Show();
        }
        
        private void PositionPopup() 
        {
            int x, y;
            Gtk.Requisition event_box_req = event_box.SizeRequest();
            Gtk.Requisition popup_req = popup.SizeRequest();
            
            PositionWidget(popup, out x, out y, 5);
            
            x = x - (popup_req.Width / 2) + (event_box_req.Width / 2);     
            if(x + popup_req.Width >= event_box.Screen.Width) { 
                x = event_box.Screen.Width - popup_req.Width - 5;
            } else if(x < 5) {
                x = 5;
            }
            
            popup.Move(x, y);
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args) 
        {
            cursor_over_trayicon = true;
            if(can_show_popup) {
                // only show the popup when the cursor is still over the
                // tray icon after 500ms
                GLib.Timeout.Add(500, delegate {
                    if ((cursor_over_trayicon) && (can_show_popup)) {
                        ShowPopup();
                    }
                    return false;
                });
            }
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args) 
        {
            cursor_over_trayicon = false;
            HidePopup();
        }



        private void FillPopup() 
        {
            can_show_popup = true;
            popup.Artist = PlayerEngineCore.CurrentTrack.DisplayArtistName;
            popup.Album = PlayerEngineCore.CurrentTrack.AlbumTitle;
            popup.TrackTitle = PlayerEngineCore.CurrentTrack.DisplayTrackTitle;
            try {
                popup.CoverArtFileName = PlayerEngineCore.CurrentTrack.CoverArtFileName;
            } catch {
            }
            popup.QueueDraw();
            if (!popup.Visible) {
                PositionPopup();
            }
        }

        */
    }
}
