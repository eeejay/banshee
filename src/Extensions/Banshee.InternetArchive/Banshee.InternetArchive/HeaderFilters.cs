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
        public HeaderFilters ()
        {
            Spacing = 6;

            BuildMediaTypeCombo ();
            BuildSortCombo ();
        }

        private void BuildMediaTypeCombo ()
        {
            var store = new TreeStore (typeof (string), typeof (string));
            foreach (var mediatype in IA.MediaType.Options.OrderBy (t => t.Name)) {
                if (mediatype.Id != "software") {
                    var iter = store.AppendValues (mediatype.Id, mediatype.Name);

                    if (mediatype.Children != null) {
                        foreach (var child in mediatype.Children.OrderBy (t => t.Name)) {
                            store.AppendValues (iter, child.Id, child.Name);
                        }
                    }
                }
            }

            var combo = new ComboBox ();
            combo.Model = store;

            var cell = new CellRendererText ();
            combo.PackStart (cell, true);
            combo.AddAttribute (cell, "text", 1);
            combo.Active = 0;

            PackStart (new Label (Catalog.GetString ("Show")), false, false, 0);
            PackStart (combo, false, false, 0);
        }

        private void BuildSortCombo ()
        {
            var combo = ComboBox.NewText ();

            combo.AppendText (Catalog.GetString ("Popular This Week"));
            combo.AppendText (Catalog.GetString ("Popular"));
            combo.AppendText (Catalog.GetString ("Rated"));

            PackStart (new Label (Catalog.GetString ("Sort by")), false, false, 0);
            PackStart (combo, false, false, 0);
        }
    }
}
