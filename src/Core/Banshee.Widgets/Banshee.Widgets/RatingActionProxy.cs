//
// RatingActionProxy.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using Mono.Unix;

using Hyena.Widgets;

namespace Banshee.Widgets
{
    public class RatingActionProxy : CustomActionProxy
    {
        private List<RatingMenuItem> rating_items = new List<RatingMenuItem> ();
        private int last_rating;

        public RatingActionProxy (UIManager ui, Gtk.Action action) : base (ui, action)
        {
        }

        //protected override void InsertProxy (Action menuAction, Widget menu, MenuItem afterItem)
        //{
        //    base.InsertProxy (menuAction, menu, afterItem);
        //}

        protected override ComplexMenuItem GetNewMenuItem ()
        {
            RatingMenuItem item = new RatingMenuItem ();
            item.RatingEntry.Changing += HandleChanging;
            rating_items.Add (item);
            return item;
        }

        private void HandleChanging (object o, EventArgs args)
        {
            last_rating = (o as RatingEntry).Value;
        }

        public void Reset (int value)
        {
            foreach (RatingMenuItem item in rating_items) {
                item.Reset (value);
            }
        }

        public int LastRating {
            get { return last_rating; }
        }
    }
}
