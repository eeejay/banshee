/*
 *  Copyright (c) 2006 Sebastian Dröge <slomo@circular-chaos.org> 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using Gtk;
using Banshee.Base;
using Banshee.Widgets;
using Mono.Unix;

namespace Banshee.PlayerMigration
{
    public class PlayerImportDialog : Dialog
    {
        private readonly PlayerImport [] player_imports = new PlayerImport [] {
            new RhythmboxPlayerImport(),
            new AmarokPlayerImport()
        };

        private ListStore list_store;
        private int num_selected = 0;
        private Button import_button;
        private Button cancel_button;

        public PlayerImportDialog () : base ()
        {
            Title = String.Empty;
            TransientFor = (Window) InterfaceElements.MainWindow;
            DestroyWithParent = true;
            HasSeparator = false;
            Resizable = false;
            BorderWidth = 5;
            VBox.Spacing = 8;
            
            Label header = new Label();
            header.Markup = String.Format("<big><b>{0}</b></big>", GLib.Markup.EscapeText(
                Catalog.GetString("Migrate From Other Media Players")));
            header.SetAlignment(0.0f, 0.5f);
            
            Label message = new Label(Catalog.GetString("Select any supported alternate media " + 
                "players that you wish to migrate into Banshee."));
            message.Justify = Gtk.Justification.Left;
            message.LineWrap = true;
            message.SetAlignment(0.0f, 0.5f);
            
            VBox vbox = new VBox();
            vbox.BorderWidth = 5;
            vbox.Spacing = 8;
            
            vbox.PackStart(header, false, false, 0);
            vbox.PackStart(message, false, false, 0);

            ScrolledWindow sw = new ScrolledWindow ();
            sw.ShadowType = ShadowType.In;
            list_store = new ListStore (typeof(bool), typeof(string), typeof(object));
            TreeView tv = new TreeView (list_store);
            tv.HeadersVisible = false;

            CellRendererToggle crt = new CellRendererToggle ();
            crt.Activatable = true;
            crt.Toggled += CrtToggled;
            tv.AppendColumn ("Import", crt, "active", 0);
            tv.AppendColumn ("Player", new CellRendererText (), "text", 1);

            foreach (PlayerImport player_import in player_imports) {
                if (player_import.CanImport)
                        list_store.AppendValues (false, player_import.Name, player_import);
            }

            if (list_store.IterNChildren() == 0) {
               LogCore.Instance.PushInformation(
                        Catalog.GetString ("Unable to Locate Supported Media Player"),
                        Catalog.GetString ("Banshee was unable to locate any libraries from alternate supported media players from which to import."));
                throw new Exception ("Unable to locate any supported library");
            }

            sw.Add (tv);
            
            vbox.PackStart(sw, true, true, 0);
            VBox.PackStart(vbox, true, true, 0);
            VBox.ShowAll();

            cancel_button = new Button (Stock.Cancel);
            cancel_button.Clicked += delegate {
                Respond (ResponseType.Cancel);
            };
            cancel_button.ShowAll ();
            AddActionWidget (cancel_button, ResponseType.Cancel);
            cancel_button.CanDefault = true;
            cancel_button.GrabFocus ();

            import_button = new Button ();
            import_button.Label = Catalog.GetString ("Migrate");
            import_button.Image = Image.NewFromIconName (Stock.Open, IconSize.Button);
            import_button.Clicked += OnImportClicked;
            import_button.Sensitive = false;
            import_button.ShowAll ();
            AddActionWidget (import_button, ResponseType.Ok);

            DefaultResponse = ResponseType.Cancel;
            
            tv.HasFocus = true;
        }

        private void CrtToggled (object o, ToggledArgs args) {
            TreeIter iter;

            if (list_store.GetIter (out iter, new TreePath (args.Path))) {
                bool old = (bool) list_store.GetValue (iter, 0);
                list_store.SetValue (iter, 0, !old);
                if (old)
                    num_selected--;
                else
                    num_selected++;
            }
            import_button.Sensitive = (num_selected > 0) ? true : false;
        }

        private void OnImportClicked (object o, EventArgs args)
        {
            foreach (object[] row in list_store) {
                if ((bool) row[0])
                    ((PlayerImport) row[2]).Import ();
            }
            Respond (ResponseType.Ok);
        }
    }
}
