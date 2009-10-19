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
        private SearchSource source;

        private ComboBox sort_combo, media_type_combo;
        private TreeStore media_type_store;
        private Banshee.Widgets.SearchEntry search_entry;
        private Button search_button;

        private Dictionary<IA.FieldValue, TreeIter> mediatypes;
        private TreeIter all_iter;

        public Widget SearchEntry {
            get { return search_entry; }
        }

        public HeaderFilters (SearchSource source)
        {
            this.source = source;
            source.Updated += (o, a) => {
                var s = source.SearchDescription;
                if (s != null) {
                    var iter = s.MediaType == null ? all_iter : mediatypes[s.MediaType];
                    media_type_combo.SetActiveIter (iter);
                    sort_combo.Active = Math.Max (Array.IndexOf (sorts, s.Sort), 0);
                    search_entry.Query = s.Query ?? "";
                }
            };

            Spacing = 6;

            BuildMediaTypeCombo ();
            BuildSortCombo ();
            BuildSearchEntry ();
            BuildSearchButton ();
        }

        private void BuildMediaTypeCombo ()
        {
            var store = media_type_store = new TreeStore (typeof (IA.MediaType), typeof (string));
            var combo = media_type_combo = new ComboBox ();
            combo.Model = store;

            all_iter = store.AppendValues (null, Catalog.GetString ("All"));

            mediatypes = new Dictionary<IA.FieldValue, TreeIter> ();
            foreach (var mediatype in IA.MediaType.Options.OrderBy (t => t.Name)) {
                if (mediatype.Id != "software") {
                    var iter = store.AppendValues (mediatype, mediatype.Name);
                    mediatypes.Add (mediatype, iter);

                    if (mediatype.Children != null) {
                        foreach (var child in mediatype.Children.OrderBy (t => t.Name)) {
                            var child_iter = store.AppendValues (iter, child, child.Name);
                            mediatypes.Add (child, child_iter);

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
                WidthRequest = 150,
                Visible = true,
                EmptyMessage = String.Format (Catalog.GetString ("Optional Query"))
            };

            entry.Activated += (o, a) => { search_button.Activate (); };

            // Add 'filter' items
            var filter_fields = new List<IA.Field> ();
            int i = 0;
            foreach (var field in IA.Field.Fields.Where (f => (f != IA.Field.Collection && f != IA.Field.Identifier))) {
                filter_fields.Add (field);
                entry.AddFilterOption (i++, field.Name);
            }

            // Add the Help item
            entry.Menu.Append (new SeparatorMenuItem ());
            var help_item = new ImageMenuItem (Catalog.GetString ("_Help")) {
                Image = new Image ("gtk-help", IconSize.Menu)
            };
            help_item.Activated += delegate { Banshee.Web.Browser.Open ("http://www.archive.org/advancedsearch.php"); };
            entry.Menu.Append (help_item);
            entry.Menu.ShowAll ();

            entry.FilterChanged += (o, a) => {
                // Only prepend the filter: part if the query doesn't already end in a :
                string prev_query = entry.Query.EndsWith (":") ? null : entry.Query;
                entry.Query = String.Format ("{0}:{1}", filter_fields[entry.ActiveFilterID].Id, prev_query);

                var editable = entry.InnerEntry as Editable;
                if (editable != null) {
                    editable.Position = entry.Query.Length;
                }
            };

            PackStart (entry, false, false, 0);
        }

        private void BuildSortCombo ()
        {
            var combo = sort_combo = ComboBox.NewText ();

            foreach (var sort in sorts) {
                combo.AppendText (sort.Name);
            }
            combo.Active = 0;

            PackStart (new Label (Catalog.GetString ("Sort by:")), false, false, 0);
            PackStart (combo, false, false, 0);
        }

        private void BuildSearchButton ()
        {
            var button = search_button = new Hyena.Widgets.ImageButton (Catalog.GetString ("_Search"), Stock.Find);
            button.Clicked += (o, a) => UpdateSearch ();

            PackStart (button, false, false, 0);
        }

        private static IA.Sort [] sorts = { IA.Sort.DownloadsDesc, IA.Sort.WeekDesc, IA.Sort.DateCreatedDesc, IA.Sort.DateCreatedAsc, IA.Sort.DateAddedDesc, IA.Sort.AvgRatingDesc };

        private void UpdateSearch ()
        {
            IA.FieldValue media_type = null;
            TreeIter iter;
            if (media_type_combo.GetActiveIter (out iter)) {
                media_type = media_type_store.GetValue (iter, 0) as IA.FieldValue;
            }

            var settings = new SearchDescription (null, search_entry.Query, sorts[sort_combo.Active], media_type);
            source.SetSearch (settings);
        }
    }
}
