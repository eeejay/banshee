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
        private CheckButton toggle_notifications_button;

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
            
            toggle_notifications_button = new CheckButton(Catalog.GetString("Show notifications when song changes"));
            toggle_notifications_button.Active = plugin.ShowNotifications;
            toggle_notifications_button.Toggled += OnShowNotificationsToggled;

            box.Add(toggle_notifications_button);
            
            PackStart(box, false, false, 0);
            
            ShowAll();
        }

        private void OnShowNotificationsToggled(object o, EventArgs args)
        {
            plugin.ShowNotifications = toggle_notifications_button.Active;
        }
    }
}
