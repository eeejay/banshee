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

namespace Banshee
{
    public class NotificationAreaIconContainer
    {
        private Tooltips tooltips;
        private EventBox traybox;

        private NotificationAreaIcon ticon;

        private Menu traymenu;
        
        private int menu_x;
        private int menu_y;

        public event EventHandler ClickEvent;
        public event ScrollEventHandler MouseScrollEvent;

        public NotificationAreaIconContainer()
        {
            ticon = new NotificationAreaIcon(Catalog.GetString("Banshee"));
            tooltips = new Tooltips();
            CreateMenu();
            Init();
        } 

        public void Init()
        {
            ticon.DestroyEvent += OnDestroyEvent;
            traybox = new EventBox();
            traybox.ButtonPressEvent += OnTrayIconClick;
            traybox.ScrollEvent += OnMouseScroll;
            traybox.Add(new Gtk.Image(IconThemeUtils.LoadIcon(22, "music-player-banshee", "tray-icon")));
            
            ticon.Add(traybox);
            ticon.ShowAll();
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
            int button_y, panel_width, panel_height;
            Gtk.Requisition requisition = menu.SizeRequest();
            
            traybox.GdkWindow.GetOrigin(out x, out button_y);
            (traybox.Toplevel as Gtk.Window).GetSize(out panel_width, out panel_height);
            
            if(button_y + panel_height + requisition.Height >= traybox.Screen.Height) {
                y = button_y - requisition.Height;
            } else {
                y = button_y + panel_height;
            }

            push_in = true;
        }
        
        private void OnDestroyEvent(object o, DestroyEventArgs args)
        {
            Init();
        }
        
        public string Tooltip {
            set { 
                tooltips.SetTip(traybox, value, null); 
            }
        }   
    }
}
