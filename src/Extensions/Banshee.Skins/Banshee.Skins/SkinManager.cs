//
// SkinManager.cs
//
// Author:
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

using Mono.Unix;
using Gtk;

using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using System.IO;

namespace Banshee.Skins
{
    public class SkinManager : IExtensionService
    {
        private BansheeActionGroup actions;
        private InterfaceActionService action_service;
        private CheckMenuItem no_skin_item;

        private string skins_directory = Path.Combine (Path.Combine (Banshee.Base.Paths.ApplicationData, "plugins"), "skins");
        private string user_gtkrc = Path.Combine (Banshee.Base.Paths.ApplicationData, "gtkrc"); 
        
        public SkinManager ()
        {
        }

        public void Initialize ()
        {
            action_service = ServiceManager.Get<InterfaceActionService> ();
            action_service.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("SkinsMenuAction", null,
                    Catalog.GetString ("Change Skin"), null,
                    Catalog.GetString ("Choose a different visual theme for Banshee"), OnSkinsMenu),

                new ActionEntry ("InstallSkinsAction", null,
                    Catalog.GetString ("Install Skins"), null,
                    Catalog.GetString ("Find and install more skins"), null)
            });

            action_service.UIManager.AddUiFromResource ("GlobalUI.xml");
            action_service.GlobalActions ["InstallSkinsAction"].Sensitive = false;

            if (!Banshee.IO.Directory.Exists (skins_directory)) {
                Banshee.IO.Directory.Create (skins_directory);
            }

            Gtk.Rc.AddDefaultFile (user_gtkrc);
            if (System.IO.File.Exists (user_gtkrc)) {
                Gtk.Rc.ReparseAllForSettings (ServiceManager.Get<GtkElementsService> ().PrimaryWindow.Settings, true);
            }
        }
        
        public void Dispose ()
        {
            /*if (ClearOnQuitSchema.Get ()) {
                OnClearPlayQueue (this, EventArgs.Empty);
            }*/
        }

        private bool first = true;
        private void OnSkinsMenu (object sender, EventArgs args)
        {
            MenuItem item = action_service.UIManager.GetWidget ("/MainMenu/ViewMenu/ViewMenuAdditions/SkinsMenu") as Gtk.MenuItem;
            Menu menu = item.Submenu as Gtk.Menu;

            if (first) {
                first = false;
                no_skin_item = new CheckMenuItem (Catalog.GetString ("No Skin"));
                no_skin_item.DrawAsRadio = true;
                no_skin_item.Activated += OnSkinSelected;

                menu.Prepend (new SeparatorMenuItem ());

                foreach (FileInfo file in new DirectoryInfo (skins_directory).GetFiles ("*.gtkrc")) {
                    string name = file.Name.Replace (".gtkrc", "");
                    Console.WriteLine ("adding {0}", name);
                    CheckMenuItem skin_item = new CheckMenuItem (name);
                    skin_item.DrawAsRadio = true;
                    skin_item.Activated += OnSkinSelected;
                    menu.Prepend (skin_item);
                }

                menu.Prepend (no_skin_item);
                UpdateChecks ();
                menu.ShowAll ();
            } else {
                UpdateChecks ();
            }
        }

        private void UpdateChecks ()
        {
            MenuItem item = action_service.UIManager.GetWidget ("/MainMenu/ViewMenu/ViewMenuAdditions/SkinsMenu") as Gtk.MenuItem;
            Menu menu = item.Submenu as Gtk.Menu;

            string current_skin = CurrentSkinSchema.Get ();
            foreach (Widget widget in menu.Children) {
                CheckMenuItem skin_item = widget as CheckMenuItem;
                if (skin_item != null) {
                    skin_item.Activated -= OnSkinSelected;
                    skin_item.Active = (current_skin == (skin_item.Child as Label).Text);
                    skin_item.Activated += OnSkinSelected;
                }
            }

            no_skin_item.Activated -= OnSkinSelected;
            no_skin_item.Active = (current_skin == "none");
            no_skin_item.Activated += OnSkinSelected;
        }

        private void OnSkinSelected (object sender, EventArgs args)
        {
            if (sender == no_skin_item) {
                ChangeSkin ("none");
            } else {
                ChangeSkin (((sender as MenuItem).Child as Label).Text);
            }
        }

        private void ChangeSkin (string name)
        {
            if (name == CurrentSkinSchema.Get ())
                return;
            
            CurrentSkinSchema.Set (name);

            System.IO.File.Delete (user_gtkrc);
            if (name != "none") {
                System.IO.File.Copy (Path.Combine (skins_directory, String.Format ("{0}.gtkrc", name)), user_gtkrc);
            }

            Gtk.Rc.ReparseAllForSettings (ServiceManager.Get<GtkElementsService> ().PrimaryWindow.Settings, true);
        }

        public string ServiceName {
            get { return "SkinManager"; }
        }
        
        public static readonly SchemaEntry<string> CurrentSkinSchema = new SchemaEntry<string> (
            "plugins.skins", "current_skin",
            "none",
            "Name of the current skin",
            "Name of the current skin."
        );
    }
}
