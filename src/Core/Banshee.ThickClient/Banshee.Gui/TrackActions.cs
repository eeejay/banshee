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
using Banshee.Playlist;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class TrackActions : ActionGroup
    {
        private InterfaceActionService action_service;
        private Dictionary<MenuItem, PlaylistSource> playlist_menu_map = new Dictionary<MenuItem, PlaylistSource> ();

        private IHasTrackSelection track_selector;
        public IHasTrackSelection TrackSelector {
            get { return track_selector; }
            set {
                if (track_selector != null && track_selector != value)
                    track_selector.TrackSelection.Changed -= HandleSelectionChanged;

                track_selector = value;

                if (track_selector != null) {
                    track_selector.TrackSelection.Changed += HandleSelectionChanged;
                    Sensitize ();
                }
            }
        }

        private static readonly string [] require_selection_actions = new string [] {
            "TrackPropertiesAction", "AddToPlaylistAction", "RemoveTracksAction"
        };
        
        public TrackActions (InterfaceActionService actionService) : base ("Track")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("TrackPropertiesAction", Stock.Edit,
                    Catalog.GetString ("_Track Properties"), null,
                    Catalog.GetString ("Edit metadata on selected songs"), OnTrackProperties),

                new ActionEntry ("AddToPlaylistAction", Stock.Add,
                    Catalog.GetString ("Add _to Playlist"), null,
                    Catalog.GetString ("Append selected songs to playlist or create new playlist from selection"),
                    OnAddToPlaylist),

                new ActionEntry ("AddToNewPlaylistAction", Stock.New,
                    Catalog.GetString ("New Playlist"), null,
                    Catalog.GetString ("Create new playlist from selected tracks"),
                    OnAddToNewPlaylist),

                new ActionEntry("RemoveTracksAction", Stock.Remove,
                    Catalog.GetString("_Remove"), "Delete",
                    Catalog.GetString("Remove selected song(s) from library"), OnRemoveTracks),
            });

            this["AddToPlaylistAction"].HideIfEmpty = false;

            action_service = actionService;
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            Sensitize ();
        }

        private void Sensitize ()
        {
            bool has_selection = TrackSelector.TrackSelection.Count > 0;
            foreach (string action in require_selection_actions)
                this [action].Sensitive = has_selection;
        }
            
        private void OnTrackProperties (object o, EventArgs args)
        {
            Console.WriteLine ("In OnTrackPropertiesAction");
            TrackEditor propEdit = new TrackEditor (TrackSelector.GetSelectedTracks ());
            propEdit.Saved += delegate {
                //ui.playlistView.QueueDraw();
            };
        }

        private void OnAddToPlaylist (object o, EventArgs args)
        {
            Gdk.Pixbuf pl_pb = Gdk.Pixbuf.LoadFromResource ("source-playlist-16.png");
            playlist_menu_map.Clear ();
            Source active_source = ServiceManager.SourceManager.ActiveSource;

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
            ServiceManager.SourceManager.DefaultSource.AddChildSource (playlist);

            ThreadAssist.Spawn (delegate {
                playlist.AddTracks (TrackSelector.TrackModel, TrackSelector.TrackSelection);
            });
        }

        private void OnAddToExistingPlaylist (object o, EventArgs args)
        {
            PlaylistSource playlist = playlist_menu_map[o as MenuItem];
            playlist.AddTracks (TrackSelector.TrackModel, TrackSelector.TrackSelection);
        }

        private void OnRemoveTracks (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;

            if (source is PlaylistSource) {
                (source as PlaylistSource).RemoveTracks (TrackSelector.TrackSelection);
            }

            TrackSelector.TrackSelection.Clear ();
        }
    }
}
