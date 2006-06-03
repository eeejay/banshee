/***************************************************************************
 *  NotificationAreaIconPlugin.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc. (Aaron Bockover)
 *  Copyright (C) 2006 Sebastian Dröge <slomo@ubuntu.com>
 * 
 *  Written by Sebastian Dröge <slomo@ubuntu.com>
 *  
 *  Parts of the file copied from the old TrayIcon code,
 *  written by Aaron Bockover <aaron@aaronbock.net>
 *  
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
using GLib;
using GConf;
using Banshee.Base;
using Banshee.MediaEngine;
using Banshee.Widgets;
using Mono.Unix;

namespace Banshee.Plugins.NotificationAreaIcon {

    public class NotificationAreaIconPlugin : Banshee.Plugins.Plugin {

        protected override string ConfigurationName { get { return "NotificationAreaIcon"; } }
        public override string DisplayName { get { return Catalog.GetString("Notification Area Icon"); } }

        public override string Description {
            get {
                return Catalog.GetString("Shows the Notification Area Icon");
            }
        }

        public override string[] Authors {
            get {
                return new string[] {
                    "Sebastian Dr\u00f6ge",
                    "Aaron Bockover"
                };
            }
        }

        private EventBox event_box;
        private NotificationArea notif_area;
        private Menu menu;
        private uint ui_manager_id;

        private TrackInfoPopup popup;
        private bool can_show_popup = false;
        private bool cursor_over_trayicon = false;

        private static readonly uint SkipDelta = 10;
        private static readonly int VolumeDelta = 10;
        
        protected override void PluginInitialize() {
            Init();

            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("NotificationAreaIconMenu.xml");        
            menu = (Menu) Globals.ActionManager.UI.GetWidget("/NotificationAreaIconMenu");
            menu.ShowAll();
        
            popup = new TrackInfoPopup();
            PlayerEngineCore.EventChanged += OnPlayerEngineEventChanged;

            // When we're already playing fill the TrackInfoPopup with the current track
            if (PlayerEngineCore.CurrentState == PlayerEngineState.Playing) {
                FillPopup();
            }
        }

        protected override void PluginDispose() {
            if (notif_area != null) {
                notif_area.Destroy();
                event_box = null;
                notif_area = null;
            }
            PlayerEngineCore.EventChanged -= OnPlayerEngineEventChanged;
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
        }

        protected override void InterfaceInitialize() {
            InterfaceElements.MainWindow.KeyPressEvent += OnKeyPressEvent;
        }

        private void Init() {
            notif_area = new NotificationArea(Catalog.GetString("Banshee"));
            notif_area.DestroyEvent += OnDestroyEvent;

            event_box = new EventBox();
            event_box.ButtonPressEvent += OnNotificationAreaIconClick;
            event_box.EnterNotifyEvent += OnEnterNotifyEvent;
            event_box.LeaveNotifyEvent += OnLeaveNotifyEvent;
            event_box.ScrollEvent += OnMouseScroll;
            
            event_box.Add(new Gtk.Image(IconThemeUtils.LoadIcon(22, "music-player-banshee", "tray-icon")));
            
            notif_area.Add(event_box);
            notif_area.ShowAll();
        }

        private void OnDestroyEvent(object o, DestroyEventArgs args) {
            Init();
        }

        private void ShowHideMainWindow()
        {
            if (InterfaceElements.MainWindow.IsActive)
                InterfaceElements.MainWindow.Visible = false;
            else
                InterfaceElements.MainWindow.Present();
        }

        [GLib.ConnectBefore]
        private void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            bool handled = false;
            
	    if (args.Event.Key == Gdk.Key.w && (args.Event.State & Gdk.ModifierType.ControlMask) != 0) {
		    handled = true;
                    ShowHideMainWindow();
                    ResizeMoveWindow();
	    }
            
            args.RetVal = handled;
        }

        private void OnNotificationAreaIconClick(object o, ButtonPressEventArgs args) {
            switch(args.Event.Button) {
                case 1:
                    ShowHideMainWindow();
                    ResizeMoveWindow();
                    break;
                case 3:
                    menu.Popup(null, null,
                            new MenuPositionFunc(PositionMenu),
                            args.Event.Button, args.Event.Time);
                    break;
            }
        }

        private void PositionMenu(Menu menu, out int x, out int y, out bool push_in) {
            PositionWidget(menu, out x, out y, 0);
            push_in = true;
        }

        private void PositionWidget(Widget widget, out int x, out int y, int yPadding) {
            int button_y, panel_width, panel_height;
            Gtk.Requisition requisition = widget.SizeRequest();
            
            event_box.GdkWindow.GetOrigin(out x, out button_y);
            (event_box.Toplevel as Gtk.Window).GetSize(out panel_width, out panel_height);

            y = (button_y + panel_height + requisition.Height >= event_box.Screen.Height) 
                ? button_y - requisition.Height - yPadding
                : button_y + panel_height + yPadding;
        }

        private void OnMouseScroll(object o, ScrollEventArgs args) {
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

        private void HidePopup() {
            popup.Hide();
        }
        
        private void ShowPopup() {
            PositionPopup();
            popup.Show();
        }
        
        private void PositionPopup() {
            int x, y;
            Gtk.Requisition event_box_req = event_box.SizeRequest();
            Gtk.Requisition popup_req = popup.SizeRequest();
            PositionWidget(popup, out x, out y, 5);
            x = x - (popup_req.Width / 2) + (event_box_req.Width / 2);     
            if(x + popup_req.Width >= event_box.Screen.Width) { 
                x = event_box.Screen.Width - popup_req.Width - 5;
            }
            
            popup.Move(x, y);
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args) {
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
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args) {
            cursor_over_trayicon = false;
            HidePopup();
        }

        private void OnPlayerEngineEventChanged(object o, PlayerEngineEventArgs args) {
            switch (args.Event) {
                case PlayerEngineEvent.Iterate:
                    if(PlayerEngineCore.CurrentTrack != null) {
                        popup.Duration = (uint)PlayerEngineCore.CurrentTrack.Duration.TotalSeconds;
                        popup.Position = PlayerEngineCore.Position;
                    } else {
                        popup.Duration = 0;
                        popup.Position = 0;
                    }
                    break;
                case PlayerEngineEvent.StartOfStream:
                    FillPopup();
                    break;
                case PlayerEngineEvent.EndOfStream:
                    // only hide the popup when we don't play again after 250ms
                    GLib.Timeout.Add(250, delegate {
                        if (PlayerEngineCore.CurrentState != PlayerEngineState.Playing) {
                            popup.Duration = 0;
                            popup.Position = 0;
                            can_show_popup = false;
                            popup.Hide();
                         }
                         return false;
                    });
                    break;
            }
        }

        private void FillPopup() {
            can_show_popup = true;
            popup.Artist = PlayerEngineCore.CurrentTrack.DisplayArtist;
            popup.Album = PlayerEngineCore.CurrentTrack.DisplayAlbum;
            popup.TrackTitle = PlayerEngineCore.CurrentTrack.DisplayTitle;
            popup.CoverArtFileName = PlayerEngineCore.CurrentTrack.CoverArtFileName;
            popup.QueueDraw();
            if (!popup.Visible) {
                PositionPopup();
            }
        }



        //FIXME: GO AWAY UGLY COPY!!!
        private void ResizeMoveWindow() {
            Window WindowPlayer = InterfaceElements.MainWindow;
            int x = 0, y = 0, width = 0, height = 0;
            try {
                x = (int)Globals.Configuration.Get(GConfKeys.WindowX);
                y = (int)Globals.Configuration.Get(GConfKeys.WindowY);
                
                width = (int)Globals.Configuration.Get(GConfKeys.WindowWidth);
                height = (int)Globals.Configuration.Get(GConfKeys.WindowHeight);
            } catch(GConf.NoSuchKeyException) {
                width = 800;
                height = 600;
                x = 0;
                y = 0;
            }
            
            if(width != 0 && height != 0) {
                WindowPlayer.Resize(width, height);
            }
            
            if(x == 0 && y == 0) {
                WindowPlayer.SetPosition(Gtk.WindowPosition.Center);
            } else {
                WindowPlayer.Move(x, y);
            }
            
            try {
                if((bool)Globals.Configuration.Get(GConfKeys.WindowMaximized)) {
                    WindowPlayer.Maximize();
                } else {
                    WindowPlayer.Unmaximize();
                }
            } catch(GConf.NoSuchKeyException) {
            }
        }
        
    }
}
