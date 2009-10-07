//
// HeaderFilters.cs
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

using Gtk;
using Mono.Unix;

using IA=InternetArchive;

namespace Banshee.InternetArchive
{
    public class HeaderFilters : HBox
    {
        private Source source;

        private ComboBox sort_combo, media_type_combo;
        private TreeStore media_type_store;
        private Banshee.Widgets.SearchEntry search_entry;

        public HeaderFilters (Source source)
        {
            this.source = source;

            Spacing = 6;

            BuildMediaTypeCombo ();
            BuildSortCombo ();
            BuildSearchEntry ();
            BuildSearchButton ();

            UpdateSearch ();
        }

        private void BuildMediaTypeCombo ()
        {
            var store = media_type_store = new TreeStore (typeof (IA.MediaType), typeof (string));
            var combo = media_type_combo = new ComboBox ();
            combo.Model = store;

            foreach (var mediatype in IA.MediaType.Options.OrderBy (t => t.Name)) {
                if (mediatype.Id != "software") {
                    var iter = store.AppendValues (mediatype, mediatype.Name);

                    if (mediatype.Children != null) {
                        foreach (var child in mediatype.Children.OrderBy (t => t.Name)) {
                            var child_iter = store.AppendValues (iter, child, child.Name);

                            // FIXME should remember the last selected one in a schema or per-source in the db
                            if (child.Id == "etree")
                                combo.SetActiveIter (child_iter);
                        }
                    }
                }
            }

            var cell = new CellRendererText ();
            combo.PackStart (cell, true);
            combo.AddAttribute (cell, "text", 1);

            PackStart (new Label (Catalog.GetString ("Collection:")), false, false, 0);
            PackStart (combo, false, false, 0);
        }

        private void BuildSearchEntry ()
        {
            var entry = search_entry = new Banshee.Widgets.SearchEntry () {
                WidthRequest = 200,
                Visible = true,
                EmptyMessage = String.Format (Catalog.GetString ("Optional Query"))
            };

            PackStart (entry, false, false, 0);
        }

        private void BuildSortCombo ()
        {
            var combo = sort_combo = ComboBox.NewText ();

            combo.AppendText (Catalog.GetString ("Popular"));
            combo.AppendText (Catalog.GetString ("Popular This Week"));
            combo.AppendText (Catalog.GetString ("Highly Rated"));
            combo.AppendText (Catalog.GetString ("Year"));
            combo.Active = 0;

            PackStart (new Label (Catalog.GetString ("Sort by:")), false, false, 0);
            PackStart (combo, false, false, 0);
        }

        private void BuildSearchButton ()
        {
            var button = new Hyena.Widgets.ImageButton (Catalog.GetString ("_Search"), Stock.Find);
            button.Clicked += (o, a) => {
                UpdateSearch ();
                source.Reload ();
            };

            PackStart (button, false, false, 0);
        }

        private void UpdateSearch ()
        {
            source.Search.Sorts.Clear ();

            string [] sorts = { "downloads desc", "week asc", "avg_rating desc", "year asc" };
            source.Search.Sorts.Add (new IA.Sort () { Id = sorts[sort_combo.Active] });

            TreeIter iter;
            if (media_type_combo.GetActiveIter (out iter)) {
                var media_type = media_type_store.GetValue (iter, 0) as IA.FieldValue;
                source.Search.Query = media_type.ToString ();

                if (!String.IsNullOrEmpty (search_entry.Query)) {
                    source.Search.Query += String.Format (" AND (title:\"{0}\" OR description:\"{0}\" OR creator:\"{0}\")", search_entry.Query);
                }
            }
        }
    }
}
