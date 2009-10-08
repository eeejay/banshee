//
// ItemSourceContents.cs
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
    public class ItemSourceContents : Gtk.HBox, Banshee.Sources.Gui.ISourceContents
    {
        private ItemSource source;
        Item item;

        public ItemSourceContents (ItemSource source, Item item)
        {
            this.source = source;
            this.item = item;

            Spacing = 6;

            BuildInfoBox ();
            BuildFilesBox ();

            ShowAll ();
        }

#region ISourceContents

        public bool SetSource (ISource source)
        {
            this.source = source as ItemSource;
            return this.source != null;
        }

        public void ResetSource ()
        {
        }

        public ISource Source { get { return source; } }

        public Widget Widget { get { return this; } }

#endregion

        private void BuildInfoBox ()
        {
            var frame = new Hyena.Widgets.RoundedFrame ();
            var vbox = new VBox ();
            vbox.Spacing = 6;

            // Title
            /*var title = new Label () {
                Xalign = 0f,
                Markup = String.Format ("<big><b>{0}</b></big>", GLib.Markup.EscapeText (item.Title))
            };*/

            // Description
            var desc = new Hyena.Widgets.WrapLabel () {
                Markup = item.Description
            };

            //var expander = new Expander (Catalog.GetString ("Details"));
            // A table w/ this info, inside the expander?
            // Notes / DateCreated / venue / publisher / source / taper / lineage

            //var desc_sw = new Gtk.ScrolledWindow ();
            //desc_sw.AddWithViewport (desc);

            // Reviews
            var reviews = new Label () {
                Xalign = 0f,
                Markup = String.Format ("<big><b>{0}</b></big>", GLib.Markup.EscapeText (Catalog.GetString ("Reviews")))
            };

            //vbox.PackStart (title,   false, false, 0);
            //vbox.PackStart (desc_sw, true,  true,  0);
            vbox.PackStart (desc, true,  true,  0);
            vbox.PackStart (reviews, false, false, 0);
            frame.Child = vbox;

            PackStart (frame, true, true, 0);
        }

        private void BuildFilesBox ()
        {
            var vbox = new VBox ();
            vbox.Spacing = 6;

            var format_list = new Hyena.Data.Gui.ListView<string> () {
                HeaderVisible = false
            };

            format_list.SizeRequested += (o, a) => {
                //a.Requisition.Height += (format_list.Model.Count * format_list.RowHeight);
                var new_req = new Requisition () {
                    Width = a.Requisition.Width,
                    Height = a.Requisition.Height + (format_list.Model.Count * format_list.RowHeight)
                };
                a.Requisition = new_req;
            };

            var format_cols = new ColumnController ();
            format_cols.Add (new Column ("format", new ColumnCellText (null, true), 1.0));

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

            var format_model = new MemoryListModel<string> ();
            var files = new List<TrackInfo> ();

            string [] format_blacklist = new string [] { "zip", "m3u", "metadata", "fingerprint", "checksums", "text" };
            foreach (var f in item.Files) {
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

                if (format_model.IndexOf (f.Format) == -1) {
                    if (!format_blacklist.Any (fmt => f.Format.ToLower ().Contains (fmt))) {
                        format_model.Add (f.Format);
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
            (columns.FileSizeColumn.GetCell (0) as ColumnCellText).Expand = false;
            (columns.DurationColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (files.Max (f => f.Duration));

            string max_title = "";
            var sorted_by_title = files.OrderBy (f => f.TrackTitle == null ? 0 : f.TrackTitle.Length).ToList ();
            //var nine_tenths = sorted_by_title[(int)Math.Floor (.90 * sorted_by_title.Count)].TrackTitle;
            var max = sorted_by_title[sorted_by_title.Count - 1].TrackTitle;
            //max_title = max.Length >= 2.0 * nine_tenths.Length ? nine_tenths : max;
            max_title = max;//max.Length >= 2.0 * nine_tenths.Length ? nine_tenths : max;
            (columns.TitleColumn.GetCell (0) as ColumnCellText).SetMinMaxStrings (sorted_by_title[0].TrackTitle, max_title);

            file_list.ColumnController = file_columns;
            format_list.ColumnController = format_cols;
            file_list.SetModel (files_model);
            format_list.SetModel (format_model);

            format_list.SelectionProxy.Changed += (o, a) => {
                files_model.Clear ();

                var selected_formats = new ModelSelection<string> (format_model, format_list.Selection);
                foreach (var track in files) {
                    if (selected_formats.Contains (track.MimeType)) {
                        files_model.Add (track);
                    }
                }

                files_model.Reload ();
            };

            if (format_model.IndexOf ("VBR MP3") != -1) {
                format_list.Selection.Select (format_model.IndexOf ("VBR MP3"));
            }


            // Action buttons
            var button = new Button ("Add to Library") {
                Relief = ReliefStyle.None
            };

            /*var menu = new Menu ();
            menu.Append (new MenuItem ("Add to Music Library"));
            menu.Append (new MenuItem ("Add to Video Library"));
            menu.Append (new MenuItem ("Add to Podcast Library"));
            menu.ShowAll ();

            var add_to_library = new MenuButton (button, menu, true);*/
            //var action_frame = new RoundedFrame ();
            var action_box = new HBox () { Spacing = 6 };
            action_box.PackStart (button, false, false, 0);
            //action_frame.Add (action_box);
            action_box.ShowAll ();

            vbox.PackStart (format_list, false, false, 0);
            vbox.PackStart (file_sw,   true,  true,  0);
            //vbox.PackStart (action_box, false, false, 0);

            PackStart (vbox, true, true, 0);
        }
    }
}
