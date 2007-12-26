//
// TrackActions.cs
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
using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Library;
using Banshee.Playlist;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Widgets;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class TrackActions : BansheeActionGroup
    {
        private InterfaceActionService action_service;
        private Dictionary<MenuItem, PlaylistSource> playlist_menu_map = new Dictionary<MenuItem, PlaylistSource> ();
        private RatingActionProxy rating_proxy;

        private static readonly string [] require_selection_actions = new string [] {
            "TrackContextMenuAction", "TrackPropertiesAction", "AddToPlaylistAction",
            "RemoveTracksAction", "RemoveTracksFromLibraryAction", "DeleteTracksFromDriveAction",
            "RateTracksAction", "SelectNoneAction"
        };
        
        private IHasTrackSelection track_selector;
        public IHasTrackSelection TrackSelector {
            get { return track_selector; }
            set {
                if (track_selector != null && track_selector != value) {
                    track_selector.TrackSelectionProxy.Changed -= HandleSelectionChanged;
                    track_selector.TrackSelectionProxy.SelectionChanged -= HandleSelectionChanged;
                }

                track_selector = value;

                if (track_selector != null) {
                    track_selector.TrackSelectionProxy.Changed += HandleSelectionChanged;
                    track_selector.TrackSelectionProxy.SelectionChanged += HandleSelectionChanged;
                    UpdateActions ();
                }
            }
        }

        public TrackActions (InterfaceActionService actionService) : base ("Track")
        {
            action_service = actionService;

            Add (new ActionEntry [] {
                new ActionEntry("TrackContextMenuAction", null, 
                    String.Empty, null, null, OnTrackContextMenu),

                new ActionEntry("SelectAllAction", null,
                    Catalog.GetString("Select _All"), "<control>A",
                    Catalog.GetString("Select all tracks"), OnSelectAll),
                    
                new ActionEntry("SelectNoneAction", null,
                    Catalog.GetString("Select _None"), "<control><shift>A",
                    Catalog.GetString("Unselect all tracks"), OnSelectNone),

                new ActionEntry ("TrackPropertiesAction", Stock.Edit,
                    Catalog.GetString ("_Edit Track Metadata"), null,
                    Catalog.GetString ("Edit metadata on selected tracks"), OnTrackProperties),

                new ActionEntry ("AddToPlaylistAction", Stock.Add,
                    Catalog.GetString ("Add _to Playlist"), null,
                    Catalog.GetString ("Append selected songs to playlist or create new playlist from selection"),
                    OnAddToPlaylist),

                new ActionEntry ("AddToNewPlaylistAction", Stock.New,
                    Catalog.GetString ("New Playlist"), null,
                    Catalog.GetString ("Create new playlist from selected tracks"),
                    OnAddToNewPlaylist),

                new ActionEntry ("RemoveTracksAction", Stock.Remove,
                    Catalog.GetString("_Remove"), "Delete",
                    Catalog.GetString("Remove selected track(s) from this source"), OnRemoveTracks),

                new ActionEntry ("RemoveTracksFromLibraryAction", null,
                    Catalog.GetString("Remove From _Library"), "",
                    Catalog.GetString("Remove selected track(s) from library"), OnRemoveTracksFromLibrary),

                new ActionEntry ("DeleteTracksFromDriveAction", null,
                    Catalog.GetString ("_Delete From Drive"), null,
                    Catalog.GetString ("Permanently delete selected song(s) from medium"), OnDeleteTracksFromDrive),

                new ActionEntry ("RateTracksAction", null,
                    String.Empty, null, null, OnRateTracks),

                new ActionEntry ("SearchMenuAction", Stock.Find,
                    Catalog.GetString ("_Search for Songs"), null,
                    Catalog.GetString ("Search for songs matching certain criteria"), null),

                new ActionEntry ("SearchForSameAlbumAction", null,
                    Catalog.GetString ("By Matching _Album"), null,
                    Catalog.GetString ("Search all songs of this album"), OnSearchForSameAlbum),

                new ActionEntry ("SearchForSameArtistAction", null,
                    Catalog.GetString ("By Matching A_rtist"), null,
                    Catalog.GetString ("Search all songs of this artist"), OnSearchForSameArtist),

                //new ActionEntry ("JumpToPlayingTrackAction", null,
                //    Catalog.GetString ("_Jump to playing song"), "<control>J",
                //    null, OnJumpToPlayingTrack),
            });

            action_service.UIManager.ActionsChanged += HandleActionsChanged;

            action_service.GlobalActions["EditMenuAction"].Activated += HandleEditMenuActivated;
            ServiceManager.SourceManager.ActiveSourceChanged += HandleActiveSourceChanged;

            this["AddToPlaylistAction"].HideIfEmpty = false;
        }

#region State Event Handlers

        private void HandleActiveSourceChanged (SourceEventArgs args)
        {
            UpdateActions ();
        }

        private void HandleActionsChanged (object sender, EventArgs args)
        {
            if (action_service.UIManager.GetAction ("/MainMenu/EditMenu") != null) {
                rating_proxy = new RatingActionProxy (action_service.UIManager, this["RateTracksAction"]);
                rating_proxy.AddPath ("/MainMenu/EditMenu", "AddToPlaylist");
                rating_proxy.AddPath ("/TrackContextMenu", "AddToPlaylist");
                action_service.UIManager.ActionsChanged -= HandleActionsChanged;
            }
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            UpdateActions ();
        }

        private void HandleEditMenuActivated (object sender, EventArgs args)
        {
            ResetRating ();
        }

#endregion

#region Utility Methods

        private void UpdateActions ()
        {
            Hyena.Collections.Selection selection = TrackSelector.TrackSelectionProxy.Selection;
            Source source = ServiceManager.SourceManager.ActiveSource;

            if (selection != null) {
                bool has_selection = selection.Count > 0;
                foreach (string action in require_selection_actions)
                    this[action].Sensitive = has_selection;

                bool has_single_selection = selection.Count == 1;

                this["SelectAllAction"].Sensitive = !selection.AllSelected;

                if (source != null) {
                    ITrackModelSource track_source = source as ITrackModelSource;
                    bool is_track_source = track_source != null;

                    UpdateActions (is_track_source && source.CanSearch, has_single_selection,
                       "SearchMenuAction", "SearchForSameArtistAction", "SearchForSameAlbumAction"
                    );

                    UpdateAction ("RemoveTracksAction", is_track_source,
                        has_selection && is_track_source && track_source.CanRemoveTracks, source
                    );

                    UpdateAction ("DeleteTracksFromDriveAction", is_track_source,
                        has_selection && is_track_source && track_source.CanDeleteTracks, source
                    );

                    UpdateAction ("RemoveTracksFromLibraryAction", source.Parent is LibrarySource, has_selection, null);
                }
            }
        }

        private void ResetRating ()
        {
            int rating = 0;

            // If there is only one track, get the preset rating
            if (TrackSelector.TrackSelectionProxy.Selection.Count == 1) {
                foreach (TrackInfo track in TrackSelector.GetSelectedTracks ()) {
                    rating = track.Rating;
                }
            }
            rating_proxy.Reset (rating);
        }

#endregion
            
#region Action Handlers

        private void OnSelectAll (object o, EventArgs args)
        {
            TrackSelector.TrackSelectionProxy.Selection.SelectAll ();
        }

        private void OnSelectNone (object o, EventArgs args)
        {
            TrackSelector.TrackSelectionProxy.Selection.Clear ();
        }


        private void OnTrackContextMenu (object o, EventArgs args)
        {
            ResetRating ();

            Gtk.Menu menu = action_service.UIManager.GetWidget ("/TrackContextMenu") as Menu;
            menu.Show (); 
            menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
        }

        private void OnTrackProperties (object o, EventArgs args)
        {
            TrackEditor propEdit = new TrackEditor (TrackSelector.GetSelectedTracks ());
            propEdit.Saved += delegate {
                //ui.playlistView.QueueDraw();
            };
        }

        // Called when the Add to Playlist action is highlighted.
        // Generates the menu of playlists to which you can add the selected tracks.
        private void OnAddToPlaylist (object o, EventArgs args)
        {
            Gdk.Pixbuf pl_pb = Gdk.Pixbuf.LoadFromResource ("source-playlist-16.png");
            playlist_menu_map.Clear ();
            Source active_source = ServiceManager.SourceManager.ActiveSource;

            // TODO find just the menu that was activated instead of modifying all proxies
            foreach (MenuItem menu in (o as Action).Proxies) {
                Menu submenu = new Menu ();
                menu.Submenu = submenu;

                submenu.Append (this ["AddToNewPlaylistAction"].CreateMenuItem ());
                submenu.Append (new SeparatorMenuItem ());
                foreach (Source child in ServiceManager.SourceManager.DefaultSource.Children) {
                    PlaylistSource playlist = child as PlaylistSource;
                    if (playlist != null) {
                        ImageMenuItem item = new ImageMenuItem (playlist.Name);
                        item.Image = new Gtk.Image (pl_pb);
                        item.Activated += OnAddToExistingPlaylist;
                        item.Sensitive = playlist != active_source;
                        playlist_menu_map[item] = playlist;
                        submenu.Append (item);
                    }
                }
                submenu.ShowAll ();
            }
        }

        private void OnAddToNewPlaylist (object o, EventArgs args)
        {
            // TODO generate name based on the track selection, or begin editing it
            PlaylistSource playlist = new PlaylistSource ("New Playlist");
            playlist.Save ();
            ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);

            ThreadAssist.Spawn (delegate {
                playlist.AddSelectedTracks (TrackSelector.TrackModel);
            });
        }

        private void OnAddToExistingPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = playlist_menu_map[o as MenuItem];
            playlist.AddSelectedTracks (TrackSelector.TrackModel);
        }

        private void OnRemoveTracks (object o, EventArgs args)
        {
            ITrackModelSource source = ServiceManager.SourceManager.ActiveSource as ITrackModelSource;

            if (!ConfirmRemove (source, false, source.TrackModel.Selection.Count))
                return;

            if (source != null && source.CanRemoveTracks) {
                source.RemoveSelectedTracks ();
            }
        }

        private void OnRemoveTracksFromLibrary (object o, EventArgs args)
        {
            ITrackModelSource source = ServiceManager.SourceManager.ActiveSource as ITrackModelSource;

            if (source != null) {
                LibrarySource library = source.Parent as LibrarySource;
                if (library != null) {
                    if (!ConfirmRemove (library, false, source.TrackModel.Selection.Count))
                        return;
                    library.RemoveSelectedTracks (source.TrackModel as TrackListDatabaseModel);
                }
            }
        }

        private void OnDeleteTracksFromDrive (object o, EventArgs args)
        {
            ITrackModelSource source = ServiceManager.SourceManager.ActiveSource as ITrackModelSource;

            if (!ConfirmRemove (source, true, source.TrackModel.Selection.Count))
                return;

            if (source != null && source.CanDeleteTracks) {
                source.DeleteSelectedTracks ();
            }
        }

        private void OnRateTracks (object o, EventArgs args)
        {
            int rating = rating_proxy.LastRating;
            foreach (TrackInfo track in TrackSelector.GetSelectedTracks ()) {
                track.Rating = rating;
                track.Save ();
            }
        }

        private void OnSearchForSameArtist (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            ITrackModelSource track_source = source as ITrackModelSource;
            foreach (TrackInfo track in TrackSelector.GetSelectedTracks ()) {
                source.FilterQuery = track_source.TrackModel.ArtistField.ToTermString (track.ArtistName);
                break;
            }
        }

        private void OnSearchForSameAlbum (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            ITrackModelSource track_source = source as ITrackModelSource;
            foreach (TrackInfo track in TrackSelector.GetSelectedTracks ()) {
                source.FilterQuery = track_source.TrackModel.AlbumField.ToTermString (track.AlbumTitle);
                break;
            }
        }

        private void OnJumpToPlayingTrack (object o, EventArgs args)
        {
        }

#endregion

        private static bool ConfirmRemove (ITrackModelSource source, bool delete, int selCount)
        {
            bool ret = false;
            string header = null;
            string message = null;
            string button_label = null;
            
            if (delete) {
                header = String.Format (
                    Catalog.GetPluralString (
                        "Are you sure you want to permanently delete this song?",
                        "Are you sure you want to permanently delete the selected {0} songs?", selCount
                    ), selCount
                );
                message = Catalog.GetString ("If you delete the selection, it will be permanently lost.");
                button_label = "gtk-delete";
            } else {
                header = String.Format (Catalog.GetString ("Remove selection from {0}?"), source.Name);
                message = String.Format (
                    Catalog.GetPluralString (
                        "Are you sure you want to remove the selected song from your {1}?",
                        "Are you sure you want to remove the selected {0} songs from your {1}?", selCount
                    ), selCount, source.GenericName
                );
                button_label = "gtk-remove";
            }
                
            HigMessageDialog md = new HigMessageDialog (
                ServiceManager.Get<GtkElementsService> ("GtkElementsService").PrimaryWindow,
                DialogFlags.DestroyWithParent, delete ? MessageType.Warning : MessageType.Question,
                ButtonsType.None, header, message
            );
            md.AddButton ("gtk-cancel", ResponseType.No, false);
            md.AddButton (button_label, ResponseType.Yes, false);
            
            try {
                if (md.Run () == (int) ResponseType.Yes) {
                    ret = true;
                }
            } finally {
                md.Destroy ();
            }
            return ret;
        }
    }
}
