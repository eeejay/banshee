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

using Hyena;
using Hyena.Query;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.Playlist.Gui;
using Banshee.Playlists.Formats;
using Banshee.SmartPlaylist;
using Banshee.Query;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class SourceActions : BansheeActionGroup
    {
        private IHasSourceView source_view;
        public IHasSourceView SourceView {
            get { return source_view; }
            set { source_view = value; }
        }

        public Source ActionSource {
            get { return ((SourceView == null) ? null :  SourceView.HighlightedSource) ?? ActiveSource; }
        }

        public override PrimarySource ActivePrimarySource {
            get { return (SourceView.HighlightedSource as PrimarySource) ?? base.ActivePrimarySource; }
        }

        public SourceActions (InterfaceActionService actionService) : base (actionService, "Source")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("NewPlaylistAction", null,
                    Catalog.GetString ("_New Playlist"), "<control>N",
                    Catalog.GetString ("Create a new empty playlist"), OnNewPlaylist),

                new ActionEntry ("NewSmartPlaylistAction", null,
                    Catalog.GetString ("New _Smart Playlist"), null,
                    Catalog.GetString ("Create a new smart playlist"), OnNewSmartPlaylist),

                /*new ActionEntry ("NewSmartPlaylistFromSearchAction", null,
                    Catalog.GetString ("New _Smart Playlist _From Search"), null,
                    Catalog.GetString ("Create a new smart playlist from the current search"), OnNewSmartPlaylistFromSearch),*/

                new ActionEntry ("SourceContextMenuAction", null, 
                    String.Empty, null, null, OnSourceContextMenu),

                new ActionEntry ("ImportSourceAction", null,
                    Catalog.GetString ("Import to Library"), null,
                    Catalog.GetString ("Import source to library"), OnImportSource),

                new ActionEntry ("RenameSourceAction", null,
                    Catalog.GetString ("Rename"), "F2", Catalog.GetString ("Rename"), OnRenameSource),

                new ActionEntry ("ExportPlaylistAction", null,
                    Catalog.GetString ("Export Playlist..."), null,
                    Catalog.GetString ("Export a playlist"), OnExportPlaylist),

                new ActionEntry ("UnmapSourceAction", null,
                    Catalog.GetString ("Unmap"), "<shift>Delete", null, OnUnmapSource),
                    
                new ActionEntry ("SourcePropertiesAction", null,
                    Catalog.GetString ("Source Properties"), null, null, OnSourceProperties),
            });

            this["NewPlaylistAction"].IconName = Stock.New;
            this["UnmapSourceAction"].IconName = Stock.Delete;
            this["SourcePropertiesAction"].IconName = Stock.Properties;

            AddImportant (
                new ActionEntry ("RefreshSmartPlaylistAction", Stock.Refresh,
                    Catalog.GetString ("Refresh"), null,
                    Catalog.GetString ("Refresh this randomly sorted smart playlist"), OnRefreshSmartPlaylist)
            );
            
            Add (new Gtk.ActionEntry [] {
                new Gtk.ActionEntry ("SortChildrenAction", null, 
                    Catalog.GetString ("Sort Children"), null, null, null),
                    
                new Gtk.ActionEntry ("SortChildrenNameAscAction", null, 
                    Catalog.GetString ("Name Ascending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.NameComparer (), true); }),
                    
                new Gtk.ActionEntry ("SortChildrenNameDescAction", null, 
                    Catalog.GetString ("Name Descending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.NameComparer (), false); }),
                    
                new Gtk.ActionEntry ("SortChildrenSizeAscAction", null, 
                    Catalog.GetString ("Size Ascending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.SizeComparer (), true); }),
                    
                new Gtk.ActionEntry ("SortChildrenSizeDescAction", null, 
                    Catalog.GetString ("Size Descending"), null, null, 
                    delegate { ActionSource.SortChildSources (new Source.SizeComparer (), false); })
            });
                
            //ServiceManager.SourceManager.SourceUpdated += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceViewChanged += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceAdded += OnPlayerEngineStateChanged;
            //ServiceManager.SourceManager.SourceRemoved += OnPlayerEngineStateChanged;
            ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;
            Actions.GlobalActions["EditMenuAction"].Activated += HandleEditMenuActivated;
        }
            
#region State Event Handlers

        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            UpdateActions ();
            
            if (last_source != null) {
                last_source.Updated -= HandleActiveSourceUpdated;
            }
            
            if (ActiveSource != null) {
                ActiveSource.Updated += HandleActiveSourceUpdated;
            }
        }

        private void HandleEditMenuActivated (object sender, EventArgs args)
        {
            UpdateActions ();
        }
        
        private void HandleActiveSourceUpdated (object o, EventArgs args)
        {
            UpdateActions (true);
        }

#endregion

#region Action Event Handlers

        private void OnNewPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = new PlaylistSource ("New Playlist", ActivePrimarySource.DbId);
            playlist.Save ();
            playlist.PrimarySource.AddChildSource (playlist);
            playlist.NotifyUser ();
            //SourceView.BeginRenameSource (playlist);
        }


        private void OnNewSmartPlaylist (object o, EventArgs args)
        {
            Editor ed = new Editor (ActivePrimarySource);
            ed.RunDialog ();
        }

        /*private void OnNewSmartPlaylistFromSearch (object o, EventArgs args)
        {
            Source active_source = ServiceManager.SourceManager.ActiveSource;
            QueryNode node = UserQueryParser.Parse (active_source.FilterQuery, BansheeQuery.FieldSet);

            if (node == null) {
                return;
            }

            // If the active source is a playlist or smart playlist, add that as a condition to the query
            QueryListNode list_node = null;
            if (active_source is PlaylistSource) {
                list_node = new QueryListNode (Keyword.And);
                list_node.AddChild (QueryTermNode.ParseUserQuery (BansheeQuery.FieldSet, String.Format ("playlistid:{0}", (active_source as PlaylistSource).DbId)));
                list_node.AddChild (node);
            } else if (active_source is SmartPlaylistSource) {
                list_node = new QueryListNode (Keyword.And);
                list_node.AddChild (QueryTermNode.ParseUserQuery (BansheeQuery.FieldSet, String.Format ("smartplaylistid:{0}", (active_source as SmartPlaylistSource).DbId)));
                list_node.AddChild (node);
            }

            SmartPlaylistSource playlist = new SmartPlaylistSource (active_source.FilterQuery);
            playlist.ConditionTree = list_node ?? node;
            playlist.Save ();
            ServiceManager.SourceManager.Library.AddChildSource (playlist);
            playlist.NotifyUpdated ();

            // TODO should begin editing the name after making it, but this changed
            // the ActiveSource to the new playlist and we don't want that.
            //SourceView.BeginRenameSource (playlist);
        }*/

        private void OnSourceContextMenu (object o, EventArgs args)
        {
            UpdateActions ();

            string path = ActionSource.Properties.Get<string> ("GtkActionPath") ?? "/SourceContextMenu";
            Gtk.Menu menu = Actions.UIManager.GetWidget (path) as Menu;
            if (menu == null || menu.Children.Length == 0) {
                SourceView.ResetHighlight ();
                UpdateActions ();
                return;
            }

            int visible_children = 0;
            foreach (Widget child in menu)
                if (child.Visible)
                    visible_children++;

            if (visible_children == 0) {
                SourceView.ResetHighlight ();
                UpdateActions ();
                return;
            }

            menu.Show (); 
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
            menu.SelectionDone += delegate {
                SourceView.ResetHighlight ();
                UpdateActions ();
            };
        }

        private void OnImportSource (object o, EventArgs args)
        {
            (ActionSource as IImportSource).Import ();
        }

        private void OnRenameSource (object o, EventArgs args)
        {
            SourceView.BeginRenameSource (ActionSource);
        }

        private void OnExportPlaylist (object o, EventArgs args)
        {
            AbstractPlaylistSource source = ActionSource as AbstractPlaylistSource;
            if (source == null) {
                return;
            }

            PlaylistExportDialog chooser = new PlaylistExportDialog (source.Name, PrimaryWindow);

            string uri = null;
            PlaylistFormatDescription format = null;
            int response = chooser.Run ();            
            if (response == (int) ResponseType.Ok) {                    
                uri = chooser.Uri;
                // Get the format that the user selected.
                format = chooser.GetExportFormat ();
            }             
            chooser.Destroy (); 

            if (uri == null) {
                // User cancelled export.
                return;
            }
            
            try {
                IPlaylistFormat playlist = (IPlaylistFormat)Activator.CreateInstance (format.Type);
                SafeUri suri = new SafeUri (uri);
                if (suri.IsLocalPath) {
                    playlist.BaseUri = new Uri (System.IO.Path.GetDirectoryName (suri.LocalPath));
                    Console.WriteLine (playlist.BaseUri.LocalPath);
                }
                playlist.Save (Banshee.IO.File.OpenWrite (new SafeUri (uri), true), source);
            } catch (Exception e) {
                Console.WriteLine (e);
                Log.Error (Catalog.GetString ("Could not export playlist"), e.Message, true);
            }
        }

        private void OnUnmapSource (object o, EventArgs args)
        {
            IUnmapableSource source = ActionSource as IUnmapableSource;
            if (source != null && source.CanUnmap && (!source.ConfirmBeforeUnmap || ConfirmUnmap (source)))
                source.Unmap ();
        }

        private void OnRefreshSmartPlaylist (object o, EventArgs args)
        {
            SmartPlaylistSource playlist = ActionSource as SmartPlaylistSource;

            if (playlist != null && playlist.CanRefresh) {
                playlist.RefreshAndReload ();
            }
        }

        private void OnSourceProperties (object o, EventArgs args)
        {
            if (ActionSource.Properties.Contains ("SourceProperties.GuiHandler")) {
                ActionSource.Properties.Get<Source.OpenPropertiesDelegate> ("SourceProperties.GuiHandler") ();
                return;
            }

            SmartPlaylistSource source = ActionSource as SmartPlaylistSource;
            if (source != null) {
                Editor ed = new Editor (source);
                ed.RunDialog ();
            }
        }

#endregion

#region Utility Methods

        private Source last_source = null;
        private void UpdateActions ()
        {
            UpdateActions (false);
        }
        
        private void UpdateActions (bool force)
        {
            Source source = ActionSource;

            if ((force || source != last_source) && source != null) {
                IUnmapableSource unmapable = (source as IUnmapableSource);
                IImportSource import_source = (source as IImportSource);
                SmartPlaylistSource smart_playlist = (source as SmartPlaylistSource);
                PrimarySource primary_source = (source as PrimarySource) ?? (source.Parent as PrimarySource);

                UpdateAction ("UnmapSourceAction", unmapable != null, unmapable != null && unmapable.CanUnmap, source);
                UpdateAction ("RenameSourceAction", source.CanRename, true, null);
                UpdateAction ("ImportSourceAction", import_source != null, import_source != null && import_source.CanImport, source);
                UpdateAction ("ExportPlaylistAction", source is AbstractPlaylistSource, true, source);
                UpdateAction ("SourcePropertiesAction", source.HasProperties, true, source);
                UpdateAction ("RefreshSmartPlaylistAction", smart_playlist != null && smart_playlist.CanRefresh, true, source);

                UpdateAction ("NewPlaylistAction", primary_source != null && primary_source.SupportsPlaylists, true, source);
                UpdateAction ("NewSmartPlaylistAction", primary_source != null && primary_source.SupportsPlaylists, true, source);
                /*UpdateAction ("NewSmartPlaylistFromSearchAction", (source is LibrarySource || source.Parent is LibrarySource),
                        !String.IsNullOrEmpty (source.FilterQuery), source);*/
                    
                ActionGroup browser_actions = Actions.FindActionGroup ("BrowserView");
                if (browser_actions != null) {
                    IFilterableSource filterable_source = source as IFilterableSource;
                    bool has_browser = filterable_source != null && filterable_source.AvailableFilters.Count > 0;
                    UpdateAction (browser_actions["BrowserTopAction"], has_browser);
                    UpdateAction (browser_actions["BrowserLeftAction"], has_browser);
                    UpdateAction (browser_actions["BrowserVisibleAction"], has_browser);
                }

                last_source = source;
            }
            
            if (source != null) {
                UpdateAction ("SortChildrenAction", source.Children.Count > 1, true, null);
            }
        }

        private static bool ConfirmUnmap (IUnmapableSource source)
        {
            string key = "no_confirm_unmap_" + source.GetType ().Name.ToLower ();
            bool do_not_ask = ConfigurationClient.Get<bool> ("sources", key, false);
            
            if (do_not_ask) {
                return true;
            }
        
            Banshee.Widgets.HigMessageDialog dialog = new Banshee.Widgets.HigMessageDialog (
                ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow,
                Gtk.DialogFlags.Modal,
                Gtk.MessageType.Question,
                Gtk.ButtonsType.Cancel,
                String.Format (Catalog.GetString ("Are you sure you want to delete this {0}?"),
                    source.GenericName.ToLower ()),
                source.Name);
            
            dialog.AddButton (Gtk.Stock.Delete, Gtk.ResponseType.Ok, false);
            
            Gtk.Alignment alignment = new Gtk.Alignment (0.0f, 0.0f, 0.0f, 0.0f);
            alignment.TopPadding = 10;
            Gtk.CheckButton confirm_button = new Gtk.CheckButton (String.Format (Catalog.GetString (
                "Do not ask me this again"), source.GenericName.ToLower ()));
            confirm_button.Toggled += delegate {
                do_not_ask = confirm_button.Active;
            };
            alignment.Add (confirm_button);
            alignment.ShowAll ();
            dialog.LabelVBox.PackStart (alignment, false, false, 0);
            
            try {
                if (dialog.Run () == (int)Gtk.ResponseType.Ok) {
                    ConfigurationClient.Set<bool> ("sources", key, do_not_ask);
                    return true;
                }
                
                return false;
            } finally {
                dialog.Destroy ();
            }
        }

#endregion
        
    }
}
