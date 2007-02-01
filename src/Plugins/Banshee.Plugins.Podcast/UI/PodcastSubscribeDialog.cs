/*/***************************************************************************  
 *  PodcastSubscribeDialog.cs
 *
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
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

using Mono.Gettext;

using Banshee.Base;
using Banshee.Plugins.Podcast;

namespace Banshee.Plugins.Podcast.UI
{
    internal class PodcastSubscribeDialog : Dialog
    {
        private Entry url_entry;
        SyncPreferenceComboBox sync_combo;
        private Gtk.AccelGroup accel_group;

        public PodcastSubscribeDialog() :  base(
                    Catalog.GetString("Subscribe"),
                    null,
                    DialogFlags.Modal | DialogFlags.NoSeparator)
        {
            IconThemeUtils.SetWindowIcon (this);
            accel_group = new Gtk.AccelGroup();
            AddAccelGroup (accel_group);
            BuildWindow ();
        }

        private void BuildWindow()
        {
            this.Resizable = false;

            BorderWidth = 6;
            VBox.Spacing = 12;
            ActionArea.Layout = Gtk.ButtonBoxStyle.End;

            HBox box = new HBox();
            box.BorderWidth = 6;
            box.Spacing = 12;

            Image image = new Image (PodcastPixbufs.PodcastIcon48);
            image.Yalign = 0.0f;

            box.PackStart(image, false, true, 0);

            VBox content_box = new VBox();
            content_box.Spacing = 12;

            Label header = new Label();
            header.Markup = "<big><b>" + GLib.Markup.EscapeText (Catalog.GetString (
                                "Subscribe to New Podcast")) + "</b></big>";
            header.Justify = Justification.Left;
            header.SetAlignment(0.0f, 0.0f);

            Label message = new Label (Catalog.GetString (
                                           "Please enter the URL of the podcast to which you are subscribing."));
            message.Wrap = true;
            message.Justify = Justification.Left;
            message.SetAlignment (0.0f, 0.0f);

            Expander advanced_expander = new Expander ("Advanced");

            VBox expander_children = new VBox();
            expander_children.BorderWidth = 6;
            expander_children.Spacing = 6;

            Label sync_text = new Label (Catalog.GetString (
                                             "When new episodes are available:  "));
            sync_text.SetAlignment (0.0f, 0.0f);
            sync_text.Justify = Justification.Left;

            sync_combo = new SyncPreferenceComboBox ();

            expander_children.PackStart (sync_text);
            expander_children.PackStart (sync_combo);

            advanced_expander.Add (expander_children);

            url_entry = new Entry ();
            url_entry.ActivatesDefault = true;

            Table table = new Table (1, 2, false);
            table.RowSpacing = 6;
            table.ColumnSpacing = 12;

            table.Attach(new Label (Catalog.GetString ("URL:")), 0, 1, 0, 1,
                         AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);

            table.Attach (url_entry, 1, 2, 0, 1,
                          AttachOptions.Expand | AttachOptions.Fill,
                          AttachOptions.Shrink, 0, 0);

            table.Attach (advanced_expander, 0, 2, 1, 2,
                          AttachOptions.Expand | AttachOptions.Fill,
                          AttachOptions.Shrink, 0, 0);

            content_box.PackStart (header, false, true, 0);
            content_box.PackStart (message, false, true, 0);

            content_box.PackStart (table, false, true, 0);

            box.PackStart (content_box, false, true, 0);

            AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, false);
            AddButton (Catalog.GetString ("Subscribe"), ResponseType.Ok, true);

            box.ShowAll ();
            VBox.Add (box);
        }

        private void AddButton (string stock_id, Gtk.ResponseType response, bool is_default)
        {
            Gtk.Button button = new Gtk.Button (stock_id);
            button.CanDefault = true;
            button.Show ();

            AddActionWidget (button, response);

            if(is_default)
            {
                DefaultResponse = response;

                button.AddAccelerator ("activate", accel_group, (uint)Gdk.Key.Escape, 0,
                                       Gtk.AccelFlags.Visible);
            }
        }

        public string Url {
            get
            {
                return url_entry.Text;
            }
            
            set 
            {
                url_entry.Text = value;
            }
        }

        public SyncPreference SyncPreference
        {
            get
            {
                return sync_combo.ActiveSyncPreference;
            }
        }
    }
}
