/***************************************************************************
 *  PodcastPropertiesDialog.cs
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
using System.Collections;

using Mono.Unix;

using Gtk;
using Pango;

using Banshee.Podcasting.Data;
using Banshee.Collection.Database;

namespace Banshee.Podcasting.Gui
{
    internal class PodcastPropertiesDialog : Dialog
    {
        private PodcastTrackInfo pi;

        public PodcastPropertiesDialog (DatabaseTrackInfo track)
        {
            PodcastTrackInfo pi = PodcastTrackInfo.From (track);
            if (pi == null)
            {
                throw new ArgumentNullException ("pi");
            }

            this.pi = pi;

            Title = track.TrackTitle;
            BuildWindow ();
            //IconThemeUtils.SetWindowIcon (this);
        }

        private void BuildWindow()
        {
            BorderWidth = 6;
            VBox.Spacing = 12;
            HasSeparator = false;

            HBox content_box = new HBox();
            content_box.BorderWidth = 6;
            content_box.Spacing = 12;

            Table table = new Table (2, 6, false);
            table.RowSpacing = 6;
            table.ColumnSpacing = 12;

            ArrayList labels = new ArrayList ();

            Label feed_label = new Label ();
            feed_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(Catalog.GetString ("Podcast:")));
            labels.Add (feed_label);

            Label pubdate_label = new Label ();
            pubdate_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(Catalog.GetString ("Date:")));
            labels.Add (pubdate_label);

            Label url_label = new Label ();
            url_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(Catalog.GetString ("URL:")));
            labels.Add (url_label);

            Label description_label = new Label ();
            description_label.Markup = String.Format("<b>{0}</b>", GLib.Markup.EscapeText(Catalog.GetString ("Description:")));
            labels.Add (description_label);

            Label feed_title_text = new Label (pi.Feed.Title);
            labels.Add (feed_title_text);

            Label pubdate_text = new Label (pi.Item.PubDate.ToString ("f"));
            labels.Add (pubdate_text);

            Label url_text = new Label (pi.Item.Link);
            labels.Add (url_text);
            url_text.Wrap = true;
            url_text.Selectable = true;
            url_text.Ellipsize = Pango.EllipsizeMode.End;

            string description_string = (String.IsNullOrEmpty (pi.Item.Description)) ?
                                        Catalog.GetString ("No description available") :
                                        pi.Item.Description;

            if (!description_string.StartsWith ("\""))
            {
                description_string =  "\""+description_string;
            }

            if (!description_string.EndsWith ("\""))
            {
                description_string = description_string+"\"";
            }

            Label description_text = new Label (description_string);
            description_text.Wrap = true;
            description_text.Selectable = true;

            labels.Add (description_text);

            table.Attach (
                feed_label, 0, 1, 0, 1,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );

            table.Attach (
                pubdate_label, 0, 1, 1, 2,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );

            table.Attach (
                url_label, 0, 1, 3, 4,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );

            table.Attach (
                description_label, 0, 1, 5, 6,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );

            table.Attach (
                feed_title_text, 1, 2, 0, 1,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );

            table.Attach (
                pubdate_text, 1, 2, 1, 2,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );


            table.Attach (
                url_text, 1, 2, 3, 4,
                AttachOptions.Fill, AttachOptions.Fill, 0, 0
            );

            table.Attach (description_text, 1, 2, 5, 6,
                          AttachOptions.Expand | AttachOptions.Fill,
                          AttachOptions.Expand | AttachOptions.Fill, 0, 0
                         );

            foreach (Label l in labels)
            {
                AlignAndJustify (l);
            }

            content_box.PackStart (table, true, true, 0);

            Button ok_button = new Button (Stock.Close);
            ok_button.CanDefault = true;
            ok_button.Show ();

            AddActionWidget (ok_button, ResponseType.Close);

            DefaultResponse = ResponseType.Ok;
            ActionArea.Layout = ButtonBoxStyle.End;

            content_box.ShowAll ();
            VBox.Add (content_box);

            Response += OnResponse;
        }

        private void AlignAndJustify (Label label)
        {
            label.SetAlignment (0f, 0f);
            label.Justify = Justification.Left;
        }

        private void OnResponse(object sender, ResponseArgs args)
        {
            (sender as Dialog).Response -= OnResponse;
            (sender as Dialog).Destroy();
        }
    }
}
