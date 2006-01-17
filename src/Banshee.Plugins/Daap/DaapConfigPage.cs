/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DaapConfigPage.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using GConf;
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Plugins.Daap 
{
    public class DaapConfigPage : VBox
    {
        public DaapConfigPage()
        {
            HBox box = new HBox();
            
            Spacing = 10;
            
            Label title = new Label();
            title.Markup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(Catalog.GetString("Music Sharing")));
            title.Xalign = 0.0f;
            
            CheckButton enabled_checkbutton = new CheckButton(Catalog.GetString("Share my music library with others"));
            enabled_checkbutton.Active = DaapCore.IsServerRunning;
            box.Sensitive = enabled_checkbutton.Active;
            enabled_checkbutton.Toggled += delegate(object o, EventArgs args) {
                box.Sensitive = enabled_checkbutton.Active;
                if(enabled_checkbutton.Active) {
                    DaapCore.StartServer();
                } else {
                    DaapCore.StopServer();
                }
            };
            
            Alignment alignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            alignment.LeftPadding = 22;
            box.Spacing = 10;
            alignment.Add(box);
            
            Entry share_entry = new Entry();
            share_entry.Text = DaapCore.ServerName;
            share_entry.FocusOutEvent += delegate(object o, FocusOutEventArgs args) {
                DaapCore.ServerName = share_entry.Text;
            };
            
            box.PackStart(new Label(Catalog.GetString("Share name:")), false, false, 0);
            box.PackStart(share_entry, false, false, 0);
            
            PackStart(title, false, false, 0);
            PackStart(enabled_checkbutton, false, false, 0);
            PackStart(alignment, false, false, 0);
            
            ShowAll();
        }
    }
}
