//
// DaapLoginDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Daap;
using Gtk;

using Banshee.Base;

namespace Banshee.Daap
{
    internal class DaapLoginDialog : Dialog
    {
        private string share_name;
        private bool show_username;
        private Entry username_entry;
        private Entry password_entry;
        private Gtk.AccelGroup accel_group;

        public DaapLoginDialog(string shareName, bool showUsername) :  base(
            Catalog.GetString("Login to Music Share"),
            null,
            DialogFlags.Modal | DialogFlags.NoSeparator,
            Stock.Cancel,
            ResponseType.Close)
        {
            share_name = shareName;
            show_username = showUsername;
            //IconThemeUtils.SetWindowIcon(this);
            accel_group = new Gtk.AccelGroup();
            AddAccelGroup(accel_group);
            BuildWindow();
        }

        private void BuildWindow()
        {
            BorderWidth = 5;
            VBox.Spacing = 12;
            ActionArea.Layout = Gtk.ButtonBoxStyle.End;

            HBox box = new HBox();
            box.BorderWidth = 5;
            box.Spacing = 15;

            Image image = new Image(Stock.Network, IconSize.Dialog);
            image.Yalign = 0.2f;
            box.PackStart(image, false, false, 0);

            VBox content_box = new VBox();
            content_box.Spacing = 12;

            Label header = new Label();
            header.Markup = "<big><b>" + GLib.Markup.EscapeText(Catalog.GetString(
                "Authentication Required")) + "</b></big>";
            header.Justify = Justification.Left;
            header.SetAlignment(0.0f, 0.5f);

            Label message = new Label(Catalog.GetString(String.Format(
                "Please provide login information to access {0}.", share_name)));
            message.Wrap = true;
            message.Justify = Justification.Left;
            message.SetAlignment(0.0f, 0.5f);

            content_box.PackStart(header, false, false, 0);
            content_box.PackStart(message, false, false, 0);

            username_entry = new Entry();
            password_entry = new Entry();
            password_entry.Visibility = false;

            uint yoff = show_username ? (uint)0 : (uint)1;

            Table table = new Table(2, 2, false);
            table.RowSpacing = 5;
            table.ColumnSpacing = 10;

            if(show_username) {
                table.Attach(new Label(Catalog.GetString("Username:")), 0, 1, 0, 1,
                    AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);

                table.Attach(username_entry, 1, 2, 0, 1,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            }

            table.Attach(new Label(Catalog.GetString("Password:")), 0, 1, 1 - yoff, 2 - yoff,
                AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);

            table.Attach(password_entry, 1, 2, 1 - yoff, 2 - yoff,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Shrink, 0, 0);

            content_box.PackStart(table, false, false, 0);

            box.PackStart(content_box, true, true, 0);

            /* Translators: this is a verb used as a button label, not a noun */
            AddButton(Catalog.GetString("Login"), ResponseType.Ok, true);

            box.ShowAll();
            VBox.Add(box);
        }

        private void AddButton(string stock_id, Gtk.ResponseType response, bool is_default)
        {
            Gtk.Button button = new Gtk.Button(stock_id);
            button.CanDefault = true;
            button.Show();

            AddActionWidget(button, response);

            if(is_default) {
                DefaultResponse = response;
                button.AddAccelerator("activate", accel_group, (uint)Gdk.Key.Return, 0, Gtk.AccelFlags.Visible);
            }
        }

        public string Username {
            get {
                return username_entry.Text == String.Empty ? null : username_entry.Text;
            }
        }

        public string Password {
            get {
                return password_entry.Text;
            }
        }
    }
}
