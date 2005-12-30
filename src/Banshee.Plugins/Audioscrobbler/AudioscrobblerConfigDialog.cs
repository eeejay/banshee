/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AudioscrobblerConfigDialog.cs
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
using GConf;
using Mono.Unix;

using Banshee.Base;
using Banshee.Widgets;

namespace Banshee.Plugins.Audioscrobbler 
{
    public class AudioscrobblerConfigDialog : Dialog
    {
        private AudioscrobblerPlugin plugin;
        private Button create_account_button;
        private CheckButton toggle_enable_button;
        private PropertyTable table;
        private Entry user_entry;
        private Entry pass_entry;

        public AudioscrobblerConfigDialog(AudioscrobblerPlugin plugin) :  base(
            Catalog.GetString("Configure Audioscrobbler"),
            null,
            DialogFlags.Modal | DialogFlags.NoSeparator,
            Stock.Close,
            ResponseType.Close)
        {
            this.plugin = plugin;
            IconThemeUtils.SetWindowIcon(this);
            
            BuildWindow();
        }
        
        private void BuildWindow()
        {
            VBox box = new VBox();
            box.Spacing = 10;
            Resizable = false;
            
            Label title = new Label();
            title.Markup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(Catalog.GetString("Audioscrobbler")));
            title.Xalign = 0.0f;
            
            Label label = new Label(plugin.Description);
            label.Wrap = true;
            
            Alignment alignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            alignment.LeftPadding = 20;
            HBox button_box = new HBox();
            create_account_button = new Button(Catalog.GetString("Create a free Last.fm account"));
            create_account_button.Clicked += delegate(object o, EventArgs args) {
                plugin.CreateAccount();
            };
            button_box.PackStart(create_account_button, false, false, 0);
            alignment.Add(button_box);
            
            Frame frame = new Frame();
            Alignment frame_alignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            frame_alignment.BorderWidth = 5;
            frame_alignment.LeftPadding = 10;
            frame_alignment.RightPadding = 10;
            frame_alignment.BottomPadding = 10;
            toggle_enable_button = new CheckButton("Enable Audioscrobbler");
            toggle_enable_button.Active = plugin.Enabled;
            toggle_enable_button.Toggled += OnEnableToggled;
            frame.LabelWidget = toggle_enable_button;
            frame.ShadowType = ShadowType.EtchedIn;
            
            table = new PropertyTable();
            table.RowSpacing = 5;
            table.Sensitive = toggle_enable_button.Active;
            
            user_entry = table.AddEntry(Catalog.GetString("Last.fm Username"), "", false);
            pass_entry = table.AddEntry(Catalog.GetString("Last.fm Password"), "", false);
            pass_entry.Visibility = false;
            user_entry.Text = plugin.Username;
            pass_entry.Text = plugin.Password;
            user_entry.Changed += OnUserPassChanged;
            pass_entry.Changed += OnUserPassChanged;
            
            frame_alignment.Add(table);
            frame.Add(frame_alignment);
            
            box.PackStart(title, false, false, 0);
            box.PackStart(label, false, false, 0);
            box.PackStart(alignment, false, false, 0);
            box.PackStart(frame, true, true, 0);
            
            VBox.Remove(ActionArea);
            
            HBox bottom_box = new HBox();
            Image logo = new Image();
            logo.Pixbuf = Gdk.Pixbuf.LoadFromResource("audioscrobbler-logo.png");
            logo.Xalign = 0.0f;
            bottom_box.PackStart(logo, true, true, 5);
            bottom_box.PackStart(ActionArea, false, false, 0);
            bottom_box.ShowAll();
            VBox.PackEnd(bottom_box, false, false, 0);
            
            box.ShowAll();
            VBox.Add(box);
            VBox.Spacing = 10;
            BorderWidth = 10;
        }
        
        private void OnUserPassChanged(object o, EventArgs args)
        {
            plugin.Username = user_entry.Text;
            plugin.Password = pass_entry.Text;
        }
        
        private void OnEnableToggled(object o, EventArgs args)
        {
            plugin.Enabled = toggle_enable_button.Active;
            table.Sensitive = toggle_enable_button.Active;
        }
    }
}
