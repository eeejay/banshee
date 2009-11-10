//
// PlaybackErrorQueryValueEntry.cs
//
// Author:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
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
using Gtk;

using Hyena.Query;
using Hyena.Query.Gui;

using Banshee.Widgets;
using Banshee.Query;

namespace Banshee.Query.Gui
{
    public class PlaybackErrorQueryValueEntry : QueryValueEntry
    {
        protected ComboBox combo;
        protected PlaybackErrorQueryValue query_value;
        protected Dictionary<int, int> combo_error_id_map = new Dictionary<int, int> ();

        public PlaybackErrorQueryValueEntry () : base ()
        {
        }

        public override QueryValue QueryValue {
            get { return query_value; }
            set {
                if (combo != null) {
                    Remove (combo);
                }

                combo = ComboBox.NewText();
                combo.WidthRequest = DefaultWidth;

                Add (combo);
                combo.Show ();

                query_value = (PlaybackErrorQueryValue)value;
                combo_error_id_map.Clear ();

                int count = 0;
                int active = 0;
                foreach (var item in query_value.Items) {
                    combo.AppendText (item.DisplayName);
                    combo_error_id_map.Add (count++, item.ID);
                    if (item.ID == (int)query_value.Value) {
                        active = count - 1;
                    }
                }

                combo.Changed += HandleValueChanged;
                combo.Active = active;
            }
        }

        protected void HandleValueChanged (object o, EventArgs args)
        {
            if (combo_error_id_map.ContainsKey (combo.Active)) {
                query_value.SetValue (combo_error_id_map [combo.Active]);
            }
        }
    }
}
