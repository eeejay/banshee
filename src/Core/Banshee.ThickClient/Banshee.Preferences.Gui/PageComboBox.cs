// PageComboBox.cs
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
using Gtk;

using Banshee.ServiceStack;

namespace Banshee.Preferences.Gui
{ 
    public class PageComboBox : ComboBox
    {
        private ListStore model;
        private Notebook notebook;
        private IList<Page> pages;
        
        public PageComboBox (IList<Page> pages, Notebook notebook)
        {
            this.pages = pages;
            this.notebook = notebook;

            // icon, name, order, Page object itself
            model = new ListStore (typeof(string), typeof(string), typeof(int), typeof(Page));
            model.SetSortColumnId (2, SortType.Ascending);
            Model = model;

            CellRendererPixbuf icon = new CellRendererPixbuf ();
            PackStart (icon, false);
            AddAttribute (icon, "icon-name", 0);

            CellRendererText name = new CellRendererText ();
            PackStart (name, true);
            AddAttribute (name, "text", 1);

            foreach (Page page in pages) {
                model.AppendValues (page.IconName ?? "image-missing", page.Name, page.Order, page);
            }

            Active = 0;
            Show ();
        }

        public string ActivePageId {
            set {
                for (int i = 0; i < pages.Count; i++) {
                    if (pages[i].Id == value) {
                        Active = i;
                        break;
                    }
                }
            }
        }
        
        protected override void OnChanged ()
        {
            notebook.CurrentPage = Active;
        }
    } 
}
