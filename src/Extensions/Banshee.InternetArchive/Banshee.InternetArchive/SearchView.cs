//
// SearchView.cs
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
    public class SearchView : Gtk.HBox, Banshee.Sources.Gui.ISourceContents
    {
        private SearchSource source;
        private ListView<IA.SearchResult> list_view;

        public SearchView (SearchSource source)
        {
            this.source = source;

            list_view = new ResultListView ();
            var controller = new PersistentColumnController ("InternetArchive");
            list_view.ColumnController = controller;
            AddColumns ();
            controller.Load ();

            list_view.RowActivated += (o, a) => {
                ServiceManager.Get<InterfaceActionService> ()["InternetArchive.ViewItemDetails"].Activate ();
            };

            list_view.SetModel (source.Model);

            // Packing
            var sw = new Gtk.ScrolledWindow ();
            sw.Child = list_view;

            var last_hid_more = DateTime.MinValue;
            string more_text = Catalog.GetString ("Fetch more results from the Internet Archive?");
            list_view.Vadjustment.ValueChanged += (o, a) => {
                var adj = list_view.Vadjustment;
                if ((DateTime.Now - last_hid_more).TotalSeconds > 2 && adj.Value >= adj.Upper - adj.PageSize) {
                    if (source.TotalResults > source.Model.Count) {
                        source.SetStatus (more_text, true, false, "gtk-question");
                        source.CurrentMessage.AddAction (new MessageAction (Catalog.GetString ("Fetch More"), delegate { source.FetchMore (); }));
                    }
                }
            };

            source.MessageNotify += (o, a) => {
                if (source.CurrentMessage != null && source.CurrentMessage.Text == more_text && source.CurrentMessage.IsHidden) {
                    last_hid_more = DateTime.Now;
                }
            };

            PackStart (sw, true, true, 0);
            ShowAll ();
        }

        private class ResultListView : ListView<IA.SearchResult>
        {
            public ResultListView ()
            {
                RulesHint = true;
                IsEverReorderable = false;
            }

            protected override bool OnPopupMenu ()
            {
                ServiceManager.Get<InterfaceActionService> ()["InternetArchive.IaResultPopup"].Activate ();
                return true;
            }
        }

        private void AddColumns ()
        {
            var cols = new SortableColumn [] {
                Create ("Title",       Catalog.GetString ("Title"), 0.9, true, new ColumnCellText (null, true)),
                Create ("Creator",     Catalog.GetString ("Creator"), 0.15, true, new ColumnCellText (null, true)),
                Create ("Publisher",   Catalog.GetString ("Publisher"), 0.15, false, new ColumnCellText (null, true)),
                Create ("Description", Catalog.GetString ("Description"), 0.35, false, new ColumnCellText (null, true)),
                Create ("AvgRatingInt",Catalog.GetString ("Rating"), 0.15, true, new ColumnCellRating (null, true) { ReadOnly = true }),
                Create ("Year",        Catalog.GetString ("Year"), 0.15, true, new ColumnCellPositiveInt (null, true, 4, 4) { CultureFormatted = false }),
                Create ("Downloads",   Catalog.GetString ("Downloads"), 0.15, true, new ColumnCellPositiveInt (null, true, 2, 5)),
                Create ("Format",      Catalog.GetString ("Formats"), 0.15, false, new ColumnCellText (null, true)),
                Create ("LicenseUrl",  Catalog.GetString ("License"), 0.15, true, new ColumnCellCreativeCommons (null, true)),
                Create ("DateAdded",   Catalog.GetString ("Added"), 0.15, false, new ColumnCellDateTime (null, true) { Format = DateTimeFormat.ShortDate })
            };

            foreach (var col in cols) {
                list_view.ColumnController.Add (col);
            }
        }

        private SortableColumn Create (string property, string label, double width, bool visible, ColumnCell cell)
        {
            cell.Property = property;
            return new SortableColumn (label, cell, width, property, visible);
        }

#region ISourceContents

        public bool SetSource (ISource source)
        {
            this.source = source as SearchSource;

            if (this.source != null) {
                list_view.SetModel (this.source.Model);
                return true;
            }

            return false;
        }

        public void ResetSource ()
        {
            list_view.SetModel (null);
            source = null;
        }

        public ISource Source { get { return source; } }

        public Widget Widget { get { return this; } }

#endregion

    }
}
