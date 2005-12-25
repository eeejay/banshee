/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  NotificationAreaIcon.cs
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
using System.Runtime.InteropServices;
using Gtk;
using Gdk;
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee
{
    public class NotificationAreaIconContainer
    {
        private EventBox traybox;

        private NotificationAreaIcon ticon;

        private Menu traymenu;
        
        private int menu_x;
        private int menu_y;

        public event EventHandler ClickEvent;
        public event ScrollEventHandler MouseScrollEvent;
        private TrackInfoPopup popup;
        private bool can_show_popup = false;
        private bool cursor_outside_box = true;

        public NotificationAreaIconContainer()
        {
            ticon = new NotificationAreaIcon(Catalog.GetString("Banshee"));
            CreateMenu();
            Init();
            popup = new TrackInfoPopup();
        } 

        public void Init()
        {
            ticon.DestroyEvent += OnDestroyEvent;
            traybox = new EventBox();
            traybox.ButtonPressEvent += OnTrayIconClick;
            traybox.EnterNotifyEvent += OnEnterNotifyEvent;
            traybox.LeaveNotifyEvent += OnLeaveNotifyEvent;
            traybox.ScrollEvent += OnMouseScroll;
            
            traybox.Add(new Gtk.Image(IconThemeUtils.LoadIcon(22, "music-player-banshee", "tray-icon")));
            
            ticon.Add(traybox);
            ticon.ShowAll();
        }
        
        private void ShowPopup()
        {
            int x, y;
            Gtk.Requisition traybox_req = traybox.SizeRequest();
            Gtk.Requisition popup_req = popup.SizeRequest();
            PositionWidget(popup, out x, out y, 5);
            
            x = x - (popup_req.Width / 2) + (traybox_req.Width / 2);
            
            popup.Move(x, y);
            popup.Show();
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args)
        {
            cursor_outside_box = false;
        
            if(!can_show_popup) {
                return;
            }
            
            can_show_popup = true;
            ShowPopup();
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
        {
            cursor_outside_box = true;
            popup.Hide();
        }
        
        private void OnMouseScroll(object o, ScrollEventArgs args)
        {
            if(MouseScrollEvent != null) {
                MouseScrollEvent(o, args);
            }
            
            args.RetVal = false;
        }

        private void OnTrayIconClick(object o, ButtonPressEventArgs args)
        {
            switch(args.Event.Button) {
                case 1:
                    if(ClickEvent != null) {
                        ClickEvent(o, args);
                    }
                    break;
                case 3:
                    menu_x = (int)args.Event.XRoot - (int)args.Event.X;
                    menu_y = (int)args.Event.YRoot - (int)args.Event.Y;
                    traymenu.Popup(null, null, 
                        new MenuPositionFunc(PositionMenu), 
                        args.Event.Button, args.Event.Time);
                    break;
            }

            args.RetVal = false;
        }

        private void CreateMenu()
        {
            traymenu = Globals.ActionManager.GetWidget("/TrayMenu") as Menu;
            traymenu.ShowAll();
        }
 
        private void PositionMenu(Menu menu, out int x, out int y, out bool push_in)
        {
            PositionWidget(menu, out x, out y, 0);
            push_in = true;
        }
        
        private void PositionWidget(Widget widget, out int x, out int y, int yPadding)
        {
            int button_y, panel_width, panel_height;
            Gtk.Requisition requisition = widget.SizeRequest();
            
            traybox.GdkWindow.GetOrigin(out x, out button_y);
            (traybox.Toplevel as Gtk.Window).GetSize(out panel_width, out panel_height);
            
            y = (button_y + panel_height + requisition.Height >= traybox.Screen.Height) 
                ? button_y - requisition.Height - yPadding
                : button_y + panel_height + yPadding;
        }
        
        private void OnDestroyEvent(object o, DestroyEventArgs args)
        {
            Init();
        }

        public void Update()
        {
            if(PlayerEngineCore.ActivePlayer == null || !PlayerEngineCore.ActivePlayer.Loaded) {
                popup.Duration = 0;
                popup.Position = 0;
                return;
            }
            
            popup.Duration = PlayerEngineCore.ActivePlayer.Length;
            popup.Position = PlayerEngineCore.ActivePlayer.Position;
        }

        public TrackInfo Track {
            set {
                if(value == null) { 
                    can_show_popup = false;
                    popup.Hide();
                    return;
                }
                
                can_show_popup = true;
                popup.Artist = value.DisplayArtist;
                popup.Album = value.DisplayAlbum;
                popup.TrackTitle = value.DisplayTitle;
                popup.CoverArtFileName = value.CoverArtFileName;
                
                popup.Hide();
                ShowPopup();
                
                GLib.Timeout.Add(6000, delegate {
                    if(cursor_outside_box) {
                        popup.Hide();
                    }
                    return false;
                });
            }
        } 
    }
}
