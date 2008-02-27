
using System;
using System.Collections.Generic;

using Mono.Unix;
using Gtk;

using Banshee.Widgets;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Gui;
using Banshee.Sources.Gui;
using Banshee.Web;

using Lastfm;
using Lastfm.Data;

namespace Banshee.Lastfm.Radio
{
    public class LastfmSourceContents : VBox, ISourceContents
    {
        private LastfmSource lastfm;

        private NumberedList recently_loved;
        private NumberedList recently_played;
        //private NumberedList recommended_artists;

        static LastfmSourceContents () {
            DataCore.UserAgent = Banshee.Web.Browser.UserAgent;
            DataCore.CachePath = System.IO.Path.Combine (Banshee.Base.Paths.ApplicationData, "cache");
        }

        // "Coming Soon: Profile, Friends, Recently Loved Songs, etc")
        public LastfmSourceContents () : base ()
        {
        }

        public bool SetSource (Source src)
        {
            lastfm = src as LastfmSource;
            if (lastfm == null) {
                return false;
            }

            if (lastfm.Connection.Connected) {
                UpdateForUser (lastfm.Account.UserName);
            } else { 
                lastfm.Connection.StateChanged += HandleConnectionStateChanged;
            }

            return true;
        }

        public Source Source {
            get { return lastfm; }
        }

        public void ResetSource ()
        {
            lastfm = null;
        }

        public Widget Widget {
            get { return this; }
        }

        private string last_user;
        private void UpdateForUser (string username)
        {
            if (username == last_user) {
                return;
            }

            last_user = username;

            if (recently_loved != null) {
                Remove (recently_loved);
                Remove (recently_played);
            }

            recently_loved = new NumberedList (lastfm, Catalog.GetString ("Recently Loved Tracks"));
            recently_played = new NumberedList (lastfm, Catalog.GetString ("Recently Played Tracks"));
            //recommended_artists = new NumberedList (Catalog.GetString ("Recommended Artists"));

            PackStart (recently_loved, true, true, 0);
            PackStart (recently_played, true, true, 0);
            //PackStart (recommended_artists, true, true, 0);

            LastfmUserData user = new LastfmUserData (username);
            recently_loved.SetList (user.RecentLovedTracks);
            recently_played.SetList (user.RecentTracks);

            ShowAll ();
        }

        /*private void TileMenuPosition (Menu menu, out int x, out int y, out bool push_in)
        {
            push_in = false;
            x = 0;
            y = 0;
        }*/

        private void HandleConnectionStateChanged (object sender, ConnectionStateChangedArgs args)
        {
            if (args.State == ConnectionState.Connected) {
                Banshee.Base.ThreadAssist.ProxyToMain (delegate {
                    UpdateForUser (lastfm.Account.UserName);
                });
            }
        }

        public class TitledList : VBox
        {
            private Label title;

            public TitledList (string title_str) : base ()
            {
                title = new Label ();
                title.Xalign = 0;
                title.Ellipsize = Pango.EllipsizeMode.End;
                title.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (title_str));

                PackStart (title, false, false, 0);
                title.Show ();
            }
        }

        public class NumberedTileView : TileView
        {
            public NumberedTileView (int cols) : base (cols)
            {
            }

            private int i = 1;
            public void AddNumberedWidget (Tile tile)
            {
                tile.PrimaryText = String.Format ("{0}. {1}", i++, tile.PrimaryText);
                AddWidget (tile);
            }
        }

        protected class NumberedList : TitledList
        {
            protected ArtworkManager artwork_manager = ServiceManager.Get<ArtworkManager> ();

            protected LastfmSource lastfm;
            protected NumberedTileView tile_view;
            protected Dictionary<object, RecentTrack> widget_track_map = new Dictionary<object, RecentTrack> ();

            public NumberedList (LastfmSource lastfm, string name) : base (name)
            {
                this.lastfm = lastfm;
                tile_view = new NumberedTileView (1);
                PackStart (tile_view, true, true, 0);
                tile_view.Show ();
            }

            public void SetList (LastfmData<RecentTrack> tracks)
            {
                tile_view.ClearWidgets ();

                foreach (RecentTrack track in tracks) {
                    MenuTile tile = new MenuTile ();
                    widget_track_map [tile] = track;
                    tile.PrimaryText = track.Name;
                    tile.SecondaryText = track.Artist;
                    tile.ButtonPressEvent += OnTileActivated;

                    // Unfortunately the recently loved list doesn't include what album the song is on
                    if (!String.IsNullOrEmpty (track.Album)) {
                        AlbumInfo album = new AlbumInfo (track.Album);
                        album.ArtistName = track.Artist;

                        Gdk.Pixbuf pb = artwork_manager == null ? null : artwork_manager.LookupScale (album.ArtworkId, 40);
                        if (pb != null) {
                            tile.Pixbuf = pb;
                        }
                    }

                    tile_view.AddNumberedWidget (tile);
                    tile.Show ();
                }
            }

            private void OnTileActivated (object sender, EventArgs args)
            {
                RecentTrack track = widget_track_map [sender];
                lastfm.Actions.CurrentArtist = track.Artist;
                lastfm.Actions.CurrentAlbum = track.Album;
                lastfm.Actions.CurrentTrack = track.Name;

                Gtk.Menu menu = ServiceManager.Get<InterfaceActionService> ().UIManager.GetWidget ("/LastfmTrackPopup") as Menu;

                // For an event
                //menu.Append (new MenuItem ("Go to Last.fm Page"));
                //menu.Append (new MenuItem ("Add to Google Calendar"));

                // For a user
                //menu.Append (new MenuItem ("Go to Last.fm Page"));
                //menu.Append (new MenuItem ("Listen to Recommended Station"));
                //menu.Append (new MenuItem ("Listen to Loved Station"));
                //menu.Append (new MenuItem ("Listen to Neighbors Station"));

                menu.ShowAll (); 
                menu.Popup (null, null, null, 0, Gtk.Global.CurrentEventTime);
            }
        }
    }
}
