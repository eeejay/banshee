
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
    public class AudioscrobblerConfigPage : VBox
    {
        private AudioscrobblerPlugin plugin;
        private CheckButton toggle_enable_button;
        private PropertyTable table;
        private Entry user_entry;
        private Entry pass_entry;
        private Image logo;

        public AudioscrobblerConfigPage(AudioscrobblerPlugin plugin, bool showLogo) :  base()
        {
            this.plugin = plugin;
            BuildWidget(showLogo);
        }
        
        private void BuildWidget(bool showLogo)
        {
            Spacing = 10;
            
            Label title = new Label();
            title.Markup = String.Format("<big><b>{0}</b></big>", 
                GLib.Markup.EscapeText(Catalog.GetString("Audioscrobbler Reporting")));
            title.Xalign = 0.0f;
            
            Label label = new Label(plugin.Description);
            label.Wrap = true;
            
            Alignment alignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            alignment.LeftPadding = 10;
            alignment.RightPadding = 10;
            HButtonBox button_box = new HButtonBox();
            button_box.Spacing = 10;
            button_box.Layout = ButtonBoxStyle.Spread;
            Button create_account_button = new Button(Catalog.GetString("Create an account"));
            create_account_button.Clicked += delegate(object o, EventArgs args) {
                plugin.CreateAccount();
            };
            Button join_group_button = new Button(Catalog.GetString("Join the Banshee group"));
            join_group_button.Clicked += delegate(object o, EventArgs args) {
                plugin.JoinGroup();
            };
            button_box.PackStart(create_account_button, false, false, 0);
            button_box.PackStart(join_group_button, false, false, 0);
            alignment.Add(button_box);
            
            Frame frame = new Frame();
            Alignment frame_alignment = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            frame_alignment.BorderWidth = 5;
            frame_alignment.LeftPadding = 10;
            frame_alignment.RightPadding = 10;
            frame_alignment.BottomPadding = 10;
            toggle_enable_button = new CheckButton(Catalog.GetString("Enable song reporting"));
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
            
            logo = new Image();
            logo.Pixbuf = Gdk.Pixbuf.LoadFromResource("audioscrobbler-logo.png");
            logo.Xalign = 0.0f;
            
            PackStart(title, false, false, 0);
            PackStart(label, false, false, 0);
            PackStart(alignment, false, false, 0);
            PackStart(frame, true, true, 0);
            
            if(showLogo) {
                PackStart(logo, false, false, 0);
            }
            
            ShowAll();
        }
        
        internal Image Logo {
            get {
                return logo;
            }
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
