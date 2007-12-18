//
// SourceActions.cs
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Playlist;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class SourceActions : ActionGroup
    {
        private InterfaceActionService action_service;

        private IHasSourceView source_view;
        public IHasSourceView SourceView {
            get { return source_view; }
            set { source_view = value; }
        }
        
        public SourceActions (InterfaceActionService actionService) : base ("Source")
        {
            action_service = actionService;

            Add (new ActionEntry [] {
                new ActionEntry("NewPlaylistAction", Stock.New,
                    Catalog.GetString("_New Playlist"), "<control>N",
                    Catalog.GetString("Create a new empty playlist"), OnNewPlaylist),

                new ActionEntry("SourceContextMenuAction", null, 
                    String.Empty, null, null, OnSourceContextMenu),

                new ActionEntry("ImportSourceAction", null,
                    Catalog.GetString("Import Source"), null,
                    Catalog.GetString("Import source to library"), OnImportSource),

                new ActionEntry("RenameSourceAction", "gtk-edit", 
                    "Rename", "F2", "Rename", OnRenameSource),

                new ActionEntry("UnmapSourceAction", Stock.Delete,
                    "Unmap", "<shift>Delete", null, OnUnmapSource),
                    
                new ActionEntry("SourcePropertiesAction", Stock.Properties,
                    "Source Properties", null, null, OnSourceProperties),
            });
                
            //ServiceManager.SourceManager.SourceUpdated += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceViewChanged += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceAdded += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceRemoved += OnPlayerEngineStateChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            action_service.GlobalActions["EditMenuAction"].Activated += HandleEditMenuActivated;
        }
            
#region State Event Handlers

        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            //this ["RenameSourceAction"].Sensitive = true;
            //action_service
        }

        private void HandleEditMenuActivated (object sender, EventArgs args)
        {
            UpdateActions ();
        }

#endregion

#region Action Event Handlers

        private void OnNewPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = new PlaylistSource ("New Playlist");
            ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);

            // TODO should begin editing the name after making it, but this changed
            // the ActiveSource to the new playlist and we don't want that.
            //SourceView.BeginRenameSource (playlist);
        }

        private void OnSourceContextMenu (object o, EventArgs args)
        {
            UpdateActions ();

            string path = SourceView.HighlightedSource.Properties.GetString ("GtkActionPath") ?? "/SourceContextMenu";
            Gtk.Menu menu = action_service.UIManager.GetWidget (path) as Menu;
            menu.Show (); 
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
        }
            
        private void OnImportSource (object o, EventArgs args)
        {
        }

        private void OnRenameSource (object o, EventArgs args)
        {
            SourceView.BeginRenameSource (SourceView.HighlightedSource);
        }

        private void OnUnmapSource (object o, EventArgs args)
        {
            IUnmapableSource source = SourceView.HighlightedSource as IUnmapableSource;
            if (source != null && source.CanUnmap && (!source.ConfirmBeforeUnmap || ConfirmUnmap (source)))
                source.Unmap ();
        }

        private void OnSourceProperties (object o, EventArgs args)
        {
        }

#endregion

#region Utility Methods

        private void UpdateActions ()
        {
            Source source = SourceView.HighlightedSource;

            IUnmapableSource unmapable = source as IUnmapableSource;
            this ["UnmapSourceAction"].Visible = (unmapable != null);
            if (unmapable != null) {
                this ["UnmapSourceAction"].Sensitive = unmapable.CanUnmap;
                this ["UnmapSourceAction"].Label = String.Format ("Remove {0}", source.Name);
            }

            // In addition to hiding, we set insensitive so any key bindings won't work

            this ["RenameSourceAction"].Visible = source.CanRename;
            this ["RenameSourceAction"].Sensitive = source.CanRename;

            this ["ImportSourceAction"].Visible = (source is IImportable);
            this ["ImportSourceAction"].Sensitive = (source is IImportable);
        }

        private static bool ConfirmUnmap(IUnmapableSource source)
        {
            string key = "no_confirm_unmap_" + source.GetType().Name.ToLower();
            bool do_not_ask = ConfigurationClient.Get<bool>("sources", key, false);
            
            if(do_not_ask) {
                return true;
            }
        
            Banshee.Widgets.HigMessageDialog dialog = new Banshee.Widgets.HigMessageDialog(
                ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow,
                Gtk.DialogFlags.Modal,
                Gtk.MessageType.Question,
                Gtk.ButtonsType.Cancel,
                String.Format(Catalog.GetString("Are you sure you want to delete this {0}?"),
                    source.GenericName.ToLower()),
                source.Name);
            
            dialog.AddButton(Gtk.Stock.Delete, Gtk.ResponseType.Ok, false);
            
            Gtk.Alignment alignment = new Gtk.Alignment(0.0f, 0.0f, 0.0f, 0.0f);
            alignment.TopPadding = 10;
            Gtk.CheckButton confirm_button = new Gtk.CheckButton(String.Format(Catalog.GetString(
                "Do not ask me this again"), source.GenericName.ToLower()));
            confirm_button.Toggled += delegate {
                do_not_ask = confirm_button.Active;
            };
            alignment.Add(confirm_button);
            alignment.ShowAll();
            dialog.LabelVBox.PackStart(alignment, false, false, 0);
            
            try {
                if(dialog.Run() == (int)Gtk.ResponseType.Ok) {
                    ConfigurationClient.Set<bool>("sources", key, do_not_ask);
                    return true;
                }
                
                return false;
            } finally {
                dialog.Destroy();
            }
        }

#endregion
        
    }
}