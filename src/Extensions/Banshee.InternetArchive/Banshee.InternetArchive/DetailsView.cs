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

        private Widget CreateSection (string label, Widget child)
        {
            var section = new Section (label, child);
            return section;
        }

        private class Section : VBox
        {
            public Section (string label, Widget child)
            {
                Spacing = 6;

                var header = new SectionHeader (label, child);
                PackStart (header, false, false, 0);
                PackStart (child, false, false, 0);
            }
        }

        private class SectionHeader : EventBox
        {
            HBox box;
            Arrow arrow;
            Label label;
            Widget child;

            public SectionHeader (string headerString, Widget child)
            {
                this.child = child;

                AppPaintable = true;
                CanFocus = true;

                box = new HBox ();
                box.Spacing = 6;
                box.BorderWidth = 4;
                label = new Label ("<b>" + headerString + "</b>") { Xalign = 0f, UseMarkup = true };
                arrow = new Arrow (ArrowType.Down, ShadowType.None);

                box.PackStart (arrow, false, false, 0);
                box.PackStart (label, true, true, 0);

                State = StateType.Selected;

                bool changing_style = false;
                StyleSet += (o, a) => {
                    if (!changing_style) {
                        changing_style = true;
                        ModifyBg (StateType.Normal, Style.Background (StateType.Selected));
                        changing_style = false;
                    }
                };

                Child = box;

                ButtonPressEvent += (o, a) => Toggle ();
                KeyPressEvent += (o, a) => {
                    var key = a.Event.Key;
                    switch (key) {
                        case Gdk.Key.Return:
                        case Gdk.Key.KP_Enter:
                        case Gdk.Key.space:
                            Toggle ();
                            a.RetVal = true;
                            break;
                    }
                };
            }

            private void Toggle ()
            {
                Expanded = !Expanded;
            }

            private bool expanded = true;
            private bool Expanded {
                get { return expanded; }
                set {
                    arrow.ArrowType = value ? ArrowType.Down : ArrowType.Right;
                    child.Visible = value;
                    expanded = value;
                }
            }
        }

        private void BuildInfoBox ()
        {
            var frame = new Hyena.Widgets.RoundedFrame ();
            var vbox = new VBox ();
            vbox.Spacing = 6;
            vbox.BorderWidth = 2;

            // Description
            var desc = new Hyena.Widgets.WrapLabel () {
                //Markup = String.Format ("<small>{0}</small>", GLib.Markup.EscapeText (Hyena.StringUtil.RemoveHtml (details.Description)))
                Markup = String.Format ("{0}", GLib.Markup.EscapeText (Hyena.StringUtil.RemoveHtml (details.Description)))
            };

            var desc_exp = CreateSection (Catalog.GetString ("Description"), desc);

            // Details
            var table = new Banshee.Gui.TrackEditor.StatisticsPage () {
                ShadowType = ShadowType.None,
                BorderWidth = 0
            };

            table.NameRenderer.Scale = Pango.Scale.Medium;
            table.ValueRenderer.Scale = Pango.Scale.Medium;

            // Keep the table from needing to vertically scroll
            table.Child.SizeRequested += (o, a) => {
                table.SetSizeRequest (a.Requisition.Width, a.Requisition.Height);
            };

            AddToTable (table, Catalog.GetString ("Creator:"), details.Creator);
            AddToTable (table, Catalog.GetString ("Venue:"), details.Venue);
            AddToTable (table, Catalog.GetString ("Location:"), details.Coverage);
            if (details.DateCreated != DateTime.MinValue) {
                AddToTable (table, Catalog.GetString ("Date:"), details.DateCreated);
            } else {
                AddToTable (table, Catalog.GetString ("Year:"), details.Year);
            }
            AddToTable (table, Catalog.GetString ("Publisher:"), details.Publisher);
            AddToTable (table, Catalog.GetString ("Keywords:"), details.Subject);
            AddToTable (table, Catalog.GetString ("License URL:"), details.LicenseUrl);
            AddToTable (table, Catalog.GetString ("Language:"), details.Language);

            table.AddSeparator ();

            AddToTable (table, Catalog.GetString ("Downloads, overall:"), details.DownloadsAllTime);
            AddToTable (table, Catalog.GetString ("Downloads, past month:"), details.DownloadsLastMonth);
            AddToTable (table, Catalog.GetString ("Downloads, past week:"), details.DownloadsLastWeek);

            table.AddSeparator ();

            AddToTable (table, Catalog.GetString ("Added:"),      details.DateAdded);
            AddToTable (table, Catalog.GetString ("Added by:"),   details.AddedBy);
            AddToTable (table, Catalog.GetString ("Source:"),     details.Source);
            AddToTable (table, Catalog.GetString ("Contributor:"), details.Contributor);
            AddToTable (table, Catalog.GetString ("Recorded by:"),details.Taper);
            AddToTable (table, Catalog.GetString ("Lineage:"),    details.Lineage);
            AddToTable (table, Catalog.GetString ("Transferred by:"), details.Transferer);

            var expander = CreateSection (Catalog.GetString ("Details"), table);

            // Reviews
            Widget reviews = null;
            if (details.NumReviews > 0) {
                var reviews_box = new VBox () { Spacing = 12, BorderWidth = 0 };
                reviews = CreateSection (String.Format (Catalog.GetPluralString (
                    "Reviews ({0} reviewer)", "Reviews ({0} reviewers)", details.NumReviews), details.NumReviews
                ), reviews_box);

                string [] stars = {
                    "\u2606\u2606\u2606\u2606\u2606",
                    "\u2605\u2606\u2606\u2606\u2606",
                    "\u2605\u2605\u2606\u2606\u2606",
                    "\u2605\u2605\u2605\u2606\u2606",
                    "\u2605\u2605\u2605\u2605\u2606",
                    "\u2605\u2605\u2605\u2605\u2605"
                };

                var sb = new System.Text.StringBuilder ();
                foreach (var review in details.Reviews) {
                    //sb.Append ("<small>");

                    var review_txt = new Hyena.Widgets.WrapLabel ();

                    var title = review.Title;
                    if (title != null) {
                        sb.AppendFormat ("<b>{0}</b>\n", GLib.Markup.EscapeText (title));
                    }

                    // Translators: {0} is the unicode-stars-rating, {1} is the name of a person who reviewed this item, and {1} is a date/time string
                    sb.AppendFormat (Catalog.GetString ("{0} by {1} on {2}"),
                        stars[review.Stars],
                        GLib.Markup.EscapeText (review.Reviewer), 
                        GLib.Markup.EscapeText (review.DateReviewed.ToLocalTime ().ToShortDateString ())
                    );

                    var body = review.Body;
                    if (body != null) {
                        body = body.Replace ("\r\n", "\n");
                        body = body.Replace ("\n\n", "\n");
                        sb.Append ("\n");
                        sb.Append (GLib.Markup.EscapeText (body));
                    }

                    //sb.Append ("</small>");
                    review_txt.Markup = sb.ToString ();
                    sb.Length = 0;

                    reviews_box.PackStart (review_txt, false, false, 0);
                }
            }

            // Packing
            vbox.PackStart (desc_exp, true, true,  0);
            //vbox.PackStart (new HSeparator (), false, false,  6);
            vbox.PackStart (expander, true, true,  0);
            //vbox.PackStart (new HSeparator (), false, false,  6);
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

            sw.Child.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
            sw.Child.ModifyFg (StateType.Normal, Style.Text (StateType.Normal));
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
                        var local_dt = dt.ToLocalTime ();
                        var str = dt.TimeOfDay == TimeSpan.Zero
                            ? local_dt.ToShortDateString ()
                            : local_dt.ToString ("g");
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

            var tracks = new List<TrackInfo> ();

            var files = new List<IA.DetailsFile> (details.Files);

            string [] format_blacklist = new string [] { "metadata", "fingerprint", "checksums", "xml", "m3u", "dublin core", "unknown" };
            var formats = new List<string> ();
            foreach (var f in files) {
                var track = new TrackInfo () {
                    Uri         = new SafeUri (f.Location),
                    FileSize    = f.Size,
                    TrackNumber = f.Track,
                    ArtistName  = f.Creator ?? details.Creator,
                    AlbumTitle  = item.Title,
                    TrackTitle  = f.Title,
                    BitRate     = f.BitRate,
                    MimeType    = f.Format,
                    Duration    = f.Length
                };

                // Fix up duration/track#/title
                if ((f.Length == TimeSpan.Zero || f.Title == null || f.Track == 0) && !f.Location.Contains ("zip") && !f.Location.EndsWith ("m3u")) {
                    foreach (var b in files) {
                        if ((f.Title != null && f.Title == b.Title)
                                || (f.OriginalFile != null && b.Location != null && b.Location.EndsWith (f.OriginalFile))
                                || (f.OriginalFile != null && f.OriginalFile == b.OriginalFile)) {
                            if (track.Duration == TimeSpan.Zero)
                                track.Duration = b.Length;

                            if (track.TrackTitle == null)
                                track.TrackTitle = b.Title;

                            if (track.TrackNumber == 0)
                                track.TrackNumber = b.Track;

                            if (track.Duration != TimeSpan.Zero && track.TrackTitle != null && track.TrackNumber != 0)
                                break;
                        }
                    }
                }

                track.TrackTitle = track.TrackTitle ?? System.IO.Path.GetFileName (f.Location);

                tracks.Add (track);

                if (f.Format != null && !formats.Contains (f.Format)) {
                    if (!format_blacklist.Any (fmt => f.Format.ToLower ().Contains (fmt))) {
                        formats.Add (f.Format);
                    }
                }
            }

            // Make these columns snugly fix their data
            if (tracks.Count > 0) {
                SetWidth (columns.TrackColumn,    tracks.Max (f => f.TrackNumber), 0);
                SetWidth (columns.FileSizeColumn, tracks.Max (f => f.FileSize), 0);
                SetWidth (columns.DurationColumn, tracks.Max (f => f.Duration), TimeSpan.Zero);
            }

            string max_title = "     ";
            if (tracks.Count > 0) {
                var sorted_by_title = tracks.OrderBy (f => f.TrackTitle == null ? 0 : f.TrackTitle.Length).ToList ();
                string nine_tenths = sorted_by_title[(int)Math.Floor (.90 * sorted_by_title.Count)].TrackTitle ?? "";
                string max = sorted_by_title[sorted_by_title.Count - 1].TrackTitle ?? "";
                max_title = ((double)max.Length >= (double)(2.0 * (double)nine_tenths.Length)) ? nine_tenths : max;
            }
            (columns.TitleColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (max_title);

            file_list.ColumnController = file_columns;
            file_list.SetModel (files_model);

            // Order the formats according to the preferences
            string format_order = String.Format (", {0}, {1}, {2},", SearchSource.VideoTypes.Get (), SearchSource.AudioTypes.Get (), SearchSource.TextTypes.Get ()).ToLower ();

            var sorted_formats = formats.Select (f => new { Format = f, Order = Math.Max (format_order.IndexOf (", " + f.ToLower () + ","), format_order.IndexOf (f.ToLower ())) })
                                        .OrderBy (o => o.Order == -1 ? Int32.MaxValue : o.Order);

            var format_list = ComboBox.NewText ();
            format_list.RowSeparatorFunc = (model, iter) => {
                return (string)model.GetValue (iter, 0) == "---";
            };

            bool have_sep = false;
            foreach (var fmt in sorted_formats) {
                if (fmt.Order == -1 && !have_sep) {
                    have_sep = true;
                    if (format_list.Model.IterNChildren () > 0) {
                        format_list.AppendText ("---");
                    }
                }

                format_list.AppendText (fmt.Format);
            }

            format_list.Changed += (o, a) => {
                files_model.Clear ();

                var selected_fmt = format_list.ActiveText;
                foreach (var track in tracks) {
                    if (track.MimeType == selected_fmt) {
                        files_model.Add (track);
                    }
                }

                files_model.Reload ();
            };

            if (formats.Count > 0) {
                format_list.Active = 0;
            }

            vbox.PackStart (file_sw, true, true, 0);
            vbox.PackStart (format_list, false, false, 0);
           
            file_list.SizeAllocated += (o, a) => {
                int target_list_width = file_list.MaxWidth;
                if (file_sw.VScrollbar != null && file_sw.VScrollbar.IsMapped) {
                    target_list_width += file_sw.VScrollbar.Allocation.Width + 2;
                }

                // Don't let the track list be too wide
                target_list_width = Math.Min (target_list_width, (int) (0.5 * (double)Allocation.Width));

                if (a.Allocation.Width != target_list_width && target_list_width > 0) {
                    file_sw.SetSizeRequest (target_list_width, -1);
                }
            };

            PackStart (vbox, false, false, 0);
        }

        private void SetWidth<T> (Column col, T max, T zero)
        {
            (col.GetCell (0) as ColumnCellText).SetMinMaxStrings (max, max);
            if (zero.Equals (max)) {
                col.Visible = false;
            }
        }
    }
}
