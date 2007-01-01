/***************************************************************************
 *  NotificationAreaIconDialog.cs
 *
 *  Copyright (C) 2006 Ruben Vermeersch <ruben@savanne.be>
 *  Written by Ruben Vermeersch <ruben@savanne.be>
 *
 *  Based on the config page found in the NotificationAreaIconPlugin, written by
 *  Aaron Bockover.
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

using Gtk;
using GLib;
using System;
using Mono.Unix;
using Banshee.Base;

namespace Banshee.Plugins.NotificationAreaIcon 
{
    public class NotificationAreaIconConfigPage : VBox
    {
        private NotificationAreaIconPlugin plugin;

        public NotificationAreaIconConfigPage(NotificationAreaIconPlugin plugin) : base()
        {
            this.plugin = plugin;
            BuildWidget();
        }
        
        private void BuildWidget()
        {
            Spacing = 10;
            
            Label title = new Label();
            title.Markup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(Catalog.GetString("Notification Area Icon")));
            title.Xalign = 0.0f;
            
            PackStart(title, false, false, 0);

            VBox box = new VBox();
            box.Spacing = 5;
            
            CheckButton show_notifications = new CheckButton(Catalog.GetString("Show notifications when song changes"));
            show_notifications.Active = plugin.ShowNotifications;
            show_notifications.Toggled += delegate { plugin.ShowNotifications = show_notifications.Active; };

            CheckButton quit_on_close = new CheckButton(Catalog.GetString("Quit Banshee when title bar close button is clicked"));
            quit_on_close.Active = plugin.QuitOnClose;
            quit_on_close.Toggled += delegate { plugin.QuitOnClose = quit_on_close.Active; };

            box.Add(show_notifications);
            box.Add(quit_on_close);
            
            PackStart(box, false, false, 0);
            
            ShowAll();
        }
    }
}
