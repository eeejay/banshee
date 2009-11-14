//
// RatingQueryValueEntry.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Gtk;

using Hyena.Query;
using Hyena.Query.Gui;
using Hyena.Widgets;

using Banshee.Widgets;
using Banshee.Query;

namespace Banshee.Query.Gui
{
    public class RatingQueryValueEntry : QueryValueEntry
    {
        protected RatingEntry entry;
        protected RatingQueryValue query_value;

        public RatingQueryValueEntry () : base ()
        {
            entry = new RatingEntry ();
            entry.AlwaysShowEmptyStars = true;
            entry.Changed += HandleValueChanged;

            Add (entry);
        }

        public override QueryValue QueryValue {
            get { return query_value; }
            set {
                entry.Changed -= HandleValueChanged;
                query_value = value as RatingQueryValue;
                entry.Value = (int) (query_value.IsEmpty ? query_value.DefaultValue : query_value.IntValue);
                query_value.SetValue (entry.Value);
                entry.Changed += HandleValueChanged;
            }
        }

        protected void HandleValueChanged (object o, EventArgs args)
        {
            query_value.SetValue (entry.Value);
        }
    }
}
