//
// HelpPage.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Mono.Unix;
using Gtk;

namespace Banshee.Gui.TrackEditor
{
    public class HelpPage : Alignment, ITrackEditorPage
    {
        private Box tab_widget;
        private TrackEditorDialog dialog;

        public HelpPage () : base (0.5f, 0.5f, 0.0f, 0.0f)
        {
            Image help = new Image ();
            help.Pixbuf = Gdk.Pixbuf.LoadFromResource ("jcastro.png");
            help.Show ();
            Add (help);

            tab_widget = new HBox ();
            tab_widget.Spacing = 2;
            tab_widget.PackStart (new Image (Stock.Help, IconSize.Menu), false, false, 0);
            tab_widget.PackStart (new Label (Title), true, true, 0);
            tab_widget.ShowAll ();
        }

        public void Initialize (TrackEditorDialog dialog)
        {
            this.dialog = dialog;
        }

        public void LoadTrack (EditorTrackInfo track)
        {
            dialog.Notebook.SetTabLabelPacking (this, false, false, PackType.End);
        }

        public int Order {
            get { return 10000; }
        }

        public string Title {
            get { return Catalog.GetString ("Help"); }
        }

        public Widget TabWidget {
            get { return tab_widget; }
        }

        public PageType PageType {
            get { return PageType.Edit; }
        }

        public Gtk.Widget Widget {
            get { return this; }
        }
    }
}
