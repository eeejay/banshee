//
// DapContent.cs
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

using Hyena;
using Hyena.Widgets;

using Banshee.Dap;
using Banshee.Sources.Gui;
using Banshee.Preferences.Gui;

namespace Banshee.Dap.Gui
{
    public class DapContent : DapPropertiesDisplay
    {
        private DapSource dap;
        private DapActions actions;

        private VBox vbox;
        private WrapLabel dap_stats;

        public DapContent (DapSource source) : base (source)
        {
            dap = source;
            BuildWidgets ();
            BuildActions ();
        }

        private void BuildWidgets ()
        {
            vbox = new VBox ();
            Add (vbox);

            HBox header_box = new HBox ();
            header_box.PackStart (new Image (LargeIcon), false, false, 0);
            
            Label title = new Label ();
            title.Markup = String.Format ("<span size=\"x-large\" weight=\"bold\">{0}</span>", dap.Name);
            title.Xalign = 0f;
            header_box.PackStart (title, false, false, 0);
            
            vbox.PackStart (header_box, false, false, 0);
            
            vbox.PackStart (new Banshee.Preferences.Gui.NotebookPage (dap.Preferences), false, false, 0);

            vbox.PackStart (new HSeparator (), false, false, 0);

            dap_stats = new WrapLabel ();
            dap.Sync.Updated += delegate { dap_stats.Text = dap.Sync.ToString (); };
            dap_stats.Text = dap.Sync.ToString ();
            vbox.PackStart (dap_stats, false, false, 0);
            
            ShowAll ();
        }

        private void BuildActions ()
        {
            actions = new DapActions (dap);
            dap.Properties.Set<Banshee.Gui.BansheeActionGroup> ("ActiveSourceActions", actions);
        }
    }
}
