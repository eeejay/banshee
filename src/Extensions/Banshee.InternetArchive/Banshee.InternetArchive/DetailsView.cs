//
// DetailsView.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Collections.Generic;
using System.Linq;

using Mono.Unix;
using Gtk;

using Hyena.Collections;
using Hyena.Data.Sqlite;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Database;
using Banshee.Gui;
using Banshee.Library;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Playlist;
using Banshee.Preferences;
using Banshee.ServiceStack;
using Banshee.Sources;

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class DetailsView : Gtk.HBox, Banshee.Sources.Gui.ISourceContents
    {
        private DetailsSource source;
        private IA.Details details;
        private Item item;

        public DetailsView (DetailsSource source, Item item)
        {
            this.source = source;
            this.item = item;

            Spacing = 6;
        }

        public void UpdateDetails ()
        {
            details = item.Details;
            BuildInfoBox ();
            BuildFilesBox ();
            ShowAll ();
        }

#region ISourceContents

        public bool SetSource (ISource source)
        {
            this.source = source as DetailsSource;
            return this.source != null;
        }

        public void ResetSource ()
        {
        }

        public ISource Source { get { return source; } }

        public Widget Widget { get { return this; } }

#endregion

        private List<Expander> expanders = new List<Expander> ();
        private Expander CreateExpander (string label)
        {
            var expander = new Expander (label) {
                Expanded = true
            };

            expanders.Add (expander);

            return expander;
        }

        private void BuildInfoBox ()
        {
            var frame = new Hyena.Widgets.RoundedFrame ();
            var vbox = new VBox ();
            vbox.Spacing = 0;

            // Title
            /*var title = new Label () {
                Xalign = 0f,
                Markup = String.Format ("<big><b>{0}</b></big>", GLib.Markup.EscapeText (item.Title))
            };*/

            // Description
            var desc_exp = CreateExpander (Catalog.GetString ("Description"));

            var desc = new Hyena.Widgets.WrapLabel () {
                Markup = String.Format ("<small>{0}</small>", GLib.Markup.EscapeText (details.Description))
            };

            desc_exp.Child = desc;

            // Details
            var expander = CreateExpander (Catalog.GetString ("Details"));
            var table = new Banshee.Gui.TrackEditor.StatisticsPage () {
                ShadowType = ShadowType.None,
                BorderWidth = 0
            };

            // Keep the table from needing to vertically scroll
            table.Child.SizeRequested += (o, a) => {
                table.SetSizeRequest (a.Requisition.Width, a.Requisition.Height);
            };

            AddToTable (table, Catalog.GetString ("Venue:"), details.Venue);
            AddToTable (table, Catalog.GetString ("Location:"), details.Coverage);
            if (details.DateCreated != DateTime.MinValue) {
                AddToTable (table, Catalog.GetString ("Date:"), details.DateCreated);
            } else {
                AddToTable (table, Catalog.GetString ("Year:"), details.Year);
            }
            AddToTable (table, Catalog.GetString ("Publisher:"), details.Publisher);
            AddToTable (table, Catalog.GetString ("Subject:"), details.Subject);

            table.AddSeparator ();

            AddToTable (table, Catalog.GetString ("Downloads, overall:"), details.DownloadsAllTime);
            AddToTable (table, Catalog.GetString ("Downloads, past month:"), details.DownloadsLastMonth);
            AddToTable (table, Catalog.GetString ("Downloads, past week:"), details.DownloadsLastWeek);

            table.AddSeparator ();

            AddToTable (table, Catalog.GetString ("Added:"),      details.DateAdded);
            AddToTable (table, Catalog.GetString ("Added by:"),   details.AddedBy);
            AddToTable (table, Catalog.GetString ("Source:"),     details.Source);
            AddToTable (table, Catalog.GetString ("Recorded by:"),details.Taper);
            AddToTable (table, Catalog.GetString ("Lineage:"),    details.Lineage);
            AddToTable (table, Catalog.GetString ("Transferred by:"), details.Transferer);

            expander.Child = table;

            // Reviews
            Expander reviews = null;
            if (details.NumReviews > 0) {
                reviews = CreateExpander (Catalog.GetString ("Reviews"));
                var reviews_box = new VBox () { Spacing = 6 };
                reviews.Child = reviews_box;

                var sb = new System.Text.StringBuilder ();
                foreach (var review in details.Reviews) {
                    var review_item = new Hyena.Widgets.WrapLabel ();

                    var title = review.Title;
                    if (title != null) {
                        sb.AppendFormat ("<small><b>{0}</b> ({1:0.0})</small>", GLib.Markup.EscapeText (title), review.Stars);
                    }

                    var body = review.Body;
                    if (body != null) {
                        if (title != null) {
                            sb.Append ("\n");
                        }

                        body = body.Replace ("\r\n", "\n");
                        body = body.Replace ("\n\n", "\n");
                        sb.AppendFormat ("<small>{0}</small>", GLib.Markup.EscapeText (body));
                    }

                    review_item.Markup = sb.ToString ();
                    sb.Length = 0;

                    reviews_box.PackStart (review_item, false, false, 0);
                }
            }

            // Packing
            vbox.PackStart (desc_exp, true, true,  0);
            vbox.PackStart (new HSeparator (), false, false,  6);
            vbox.PackStart (expander, true, true,  0);
            vbox.PackStart (new HSeparator (), false, false,  6);
            if (reviews != null) {
                vbox.PackStart (reviews, true, true, 0);
            }

            var vbox2 = new VBox ();
            vbox2.PackStart (vbox, false, false, 0);

            var sw = new Gtk.ScrolledWindow () { ShadowType = ShadowType.None };
            sw.AddWithViewport (vbox2);
            (sw.Child as Viewport).ShadowType = ShadowType.None;
            frame.Child = sw;
            frame.ShowAll ();

            StyleSet += delegate {
                sw.Child.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
                sw.Child.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
            };

            PackStart (frame, true, true, 0);
        }

        private void AddToTable (Banshee.Gui.TrackEditor.StatisticsPage table, string label, object val)
        {
            if (val != null) {
                if (val is long) {
                    table.AddItem (label, ((long)val).ToString ("N0"));
                } else if (val is DateTime) {
                    var dt = (DateTime)val;
                    if (dt != DateTime.MinValue) {
                        var str = dt.TimeOfDay == TimeSpan.Zero
                            ? dt.ToShortDateString ()
                            : dt.ToString ("g");
                        table.AddItem (label, str);
                    }
                } else {
                    table.AddItem (label, val.ToString ());
                }
            }
        }

        private void BuildFilesBox ()
        {
            var vbox = new VBox ();
            vbox.Spacing = 6;

            var file_list = new BaseTrackListView () {
                HeaderVisible = true,
                IsEverReorderable = false
            };

            var files_model = source.TrackModel as MemoryTrackListModel;
            var columns = new DefaultColumnController ();
            columns.TrackColumn.Title = "#";
            var file_columns = new ColumnController ();
            file_columns.AddRange (
                columns.IndicatorColumn,
                columns.TrackColumn,
                columns.TitleColumn,
                columns.DurationColumn,
                columns.FileSizeColumn
            );

            foreach (var col in file_columns) {
                col.Visible = true;
            }

            var file_sw = new Gtk.ScrolledWindow ();
            file_sw.Child = file_list;

            var files = new List<TrackInfo> ();

            string [] format_blacklist = new string [] { "zip", "m3u", "metadata", "fingerprint", "checksums", "text" };
            var formats = new List<string> ();
            foreach (var f in details.Files) {
                var track = new TrackInfo () {
                    Uri         = new SafeUri (f.Location),
                    FileSize    = f.Size,
                    TrackNumber = f.Track,
                    ArtistName  = f.Creator,
                    TrackTitle  = f.Title,
                    BitRate     = f.BitRate,
                    MimeType    = f.Format,
                    Duration    = f.Length
                };

                files.Add (track);

                if (f.Format != null && !formats.Contains (f.Format)) {
                    if (!format_blacklist.Any (fmt => f.Format.ToLower ().Contains (fmt))) {
                        formats.Add (f.Format);
                    }
                }
            }

            // HACK to fix up the times; sometimes the VBR MP3 file will have it but Ogg won't
            foreach (var a in files) {
                foreach (var b in files) {
                    if (a.TrackTitle == b.TrackTitle) {
                        a.Duration = b.Duration = a.Duration > b.Duration ? a.Duration : b.Duration;
                    }
                }
            }

            // Make these columns snugly fix their data
            (columns.TrackColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (files.Max (f => f.TrackNumber));
            (columns.FileSizeColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (files.Max (f => f.FileSize));
            //(columns.FileSizeColumn.GetCell (0) as ColumnCellText).Expand = false;
            (columns.DurationColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (files.Max (f => f.Duration));

            string max_title = "";
            var sorted_by_title = files.OrderBy (f => f.TrackTitle == null ? 0 : f.TrackTitle.Length).ToList ();
            var nine_tenths = sorted_by_title[(int)Math.Floor (.90 * sorted_by_title.Count)].TrackTitle;
            var max = sorted_by_title[sorted_by_title.Count - 1].TrackTitle;
            max_title = ((double)max.Length >= (double)(2.0 * (double)nine_tenths.Length)) ? nine_tenths : max;
            (columns.TitleColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (max_title);

            file_list.ColumnController = file_columns;
            file_list.SetModel (files_model);

            var format_list = ComboBox.NewText ();
            foreach (var fmt in formats) {
                format_list.AppendText (fmt);
            }

            format_list.Changed += (o, a) => {
                files_model.Clear ();

                var selected_fmt = format_list.ActiveText;
                foreach (var track in files) {
                    if (track.MimeType == selected_fmt) {
                        files_model.Add (track);
                    }
                }

                files_model.Reload ();
            };

            if (formats.IndexOf ("VBR MP3") != -1) {
                format_list.Active = formats.IndexOf ("VBR MP3");
            }

            vbox.PackStart (file_sw, true, true, 0);
            vbox.PackStart (format_list, false, false, 0);
           
            file_list.SizeAllocated += (o, a) => {
                int target_list_width = file_list.MaxWidth;
                if (file_sw.VScrollbar != null && file_sw.VScrollbar.IsMapped) {
                    target_list_width += file_sw.VScrollbar.Allocation.Width + 2;
                }

                if (a.Allocation.Width != target_list_width) {
                    file_sw.SetSizeRequest (target_list_width, -1);
                }
            };

            //PackStart (vbox, true, true, 0);
            PackStart (vbox, false, false, 0);
        }
    }
}
