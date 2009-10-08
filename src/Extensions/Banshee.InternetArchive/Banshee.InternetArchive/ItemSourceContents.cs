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

        public ItemSourceContents (Item item)
        {
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
            var title = new Label () {
                Markup = String.Format ("<big><b>{0}</b></big>", GLib.Markup.EscapeText (item.Title))
            };

            // Description
            var desc = new Hyena.Widgets.WrapLabel () {
                Markup = item.Description
            };

            var desc_sw = new Gtk.ScrolledWindow ();
            desc_sw.AddWithViewport (desc);

            // Reviews
            var reviews = new Label () {
                Markup = String.Format ("<big><b>{0}</b></big>", GLib.Markup.EscapeText (Catalog.GetString ("Reviews")))
            };

            vbox.PackStart (title,   false, false, 0);
            vbox.PackStart (desc_sw, true,  true,  0);
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
            var format_sw = new Gtk.ScrolledWindow ();
            format_sw.Child = format_list;
            var format_cols = new ColumnController ();
            format_cols.Add (new Column ("format", new ColumnCellText (null, true), 1.0));
            format_list.ColumnController = format_cols;

            var file_list = new ListView<TrackInfo> () {
                HeaderVisible = false
            };

            var files_model = new MemoryTrackListModel ();
            var columns = new DefaultColumnController ();
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

            file_list.ColumnController = file_columns;

            var file_sw = new Gtk.ScrolledWindow ();
            file_sw.Child = file_list;

            var format_model = new MemoryListModel<string> ();
            var files = new List<TrackInfo> ();

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
                    format_model.Add (f.Format);
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

            vbox.PackStart (format_sw, false, false, 0);
            vbox.PackStart (file_sw,   true,  true,  0);

            PackStart (vbox, true, true, 0);
        }
    }
}
