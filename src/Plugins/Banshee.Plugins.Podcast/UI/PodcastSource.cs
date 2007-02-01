/***************************************************************************
 *  PodcastSource.cs
 *
 *  Written by Mike Urbanski <michael.c.urbanski@gmail.com>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
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
using System.Data;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Gtk;
using Gdk;
using GLib;
using Gnome;

using Mono.Gettext;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;

using Banshee.Plugins.Podcast.Download;

using Banshee.Plugins.Podcast;

namespace Banshee.Plugins.Podcast.UI
{
    public class PodcastSource : Banshee.Sources.Source
    {
        private Gdk.Pixbuf icon;
        private Container viewWidget;

        private PodcastPlaylistView podcast_view;
        private PodcastPlaylistModel podcast_model;

        private PodcastFeedView podcast_feed_view;
        private PodcastFeedModel podcast_feed_model;

        private ScrolledWindow podcast_view_scroller;
        private ScrolledWindow podcast_feed_view_scroller;

        private HPaned feed_info_pane;
        private VPaned feed_playlist_pane;

        private ActionButton update_button;
        private ActionButton subscribe_button;

        private bool loaded = false;
        private bool loading = false;

        private PodcastFeedInfo previously_selected = PodcastFeedInfo.All;

        public override object TracksMutex {
            get {
                return PodcastCore.Library.TrackSync; 
            }
        }

        private Gtk.ActionGroup action_group = null;
        private const string PodcastPopupMenuPath = "/PodcastMenuPopup";
        
        public override string ActionPath {
            get {
                if(action_group != null) {
                    return PodcastPopupMenuPath;
                }
                
                Globals.ActionManager.UI.AddUiFromString(@"
                    <ui>
                        <popup name='PodcastMenuPopup' action='PodcastAction'>
                                <menuitem name='PodcastUpdateFeeds' action='PodcastUpdateFeedsAction' />
                                <menuitem name='PodcastSubscribe' action='PodcastSubscribeAction' />
                                <separator />
                                <menuitem name='PodcastVisitPodcastAlley' action='PodcastVisitPodcastAlleyAction' />
                        </popup>
                    </ui>                    
                ");
                
                return PodcastPopupMenuPath;
            }
        }

        public PodcastSource() : base(Catalog.GetString ("Podcasts"), 2)
        {
            icon = PodcastPixbufs.PodcastIcon22;
        }

        public void Load ()
        {
            if (!loaded && !loading)
            {
                try
                {
                    PodcastCore.Library.TrackAdded += OnLibraryTrackAdded;
                    PodcastCore.Library.TrackRemoved += OnLibraryTrackRemoved;

                    PodcastCore.Library.PodcastAdded += OnPodcastAddedHandler;
                    PodcastCore.Library.PodcastRemoved += OnPodcastRemovedHandler;

                    PodcastCore.Library.PodcastFeedAdded += OnPodcastFeedAddedHandler;
                    PodcastCore.Library.PodcastFeedRemoved += OnPodcastFeedRemovedHandler;

                    BuildView ();

                    TreeIter first_iter;
                    podcast_feed_model.GetIterFirst (out first_iter);
                    podcast_feed_view.Selection.SelectIter (first_iter);

                }
                catch (Exception e)
                {
                    Console.WriteLine (e.Message);
                }
                finally
                {
                    loading = false;
                }
                loaded = true;
            }
        }

        protected override void OnDispose()
        {
            PodcastCore.Library.TrackAdded -= OnLibraryTrackAdded;
            PodcastCore.Library.TrackRemoved -= OnLibraryTrackRemoved;

            PodcastCore.Library.PodcastAdded -= OnPodcastAddedHandler;
            PodcastCore.Library.PodcastRemoved -= OnPodcastRemovedHandler;

            PodcastCore.Library.PodcastFeedAdded -= OnPodcastFeedAddedHandler;
            PodcastCore.Library.PodcastFeedRemoved -= OnPodcastFeedRemovedHandler;

            DestroyView ();
        }

        public override void Activate()
        {
            InterfaceElements.ActionButtonBox.PackStart (subscribe_button, false, false, 0);
            InterfaceElements.ActionButtonBox.PackStart (update_button, false, false, 0);
        }

        public override void Deactivate()
        {
            InterfaceElements.ActionButtonBox.Remove (subscribe_button);
            InterfaceElements.ActionButtonBox.Remove (update_button);
            Commit ();
        }

        public override void Commit ()
        {
            try
            {
                GConfSchemas.PlaylistSeparatorPositionSchema.Set (
                    (int) feed_playlist_pane.Position
                );
            }
            catch {
            }
        }

        private void BuildView ()
        {
            podcast_view_scroller = new ScrolledWindow();

            podcast_view_scroller.ShadowType = ShadowType.In;
            podcast_view_scroller.VscrollbarPolicy = PolicyType.Automatic;
            podcast_view_scroller.HscrollbarPolicy = PolicyType.Automatic;

            podcast_feed_view_scroller = new ScrolledWindow();

            podcast_feed_view_scroller.ShadowType = ShadowType.In;
            podcast_feed_view_scroller.VscrollbarPolicy = PolicyType.Automatic;
            podcast_feed_view_scroller.HscrollbarPolicy = PolicyType.Automatic;

            podcast_model = new PodcastPlaylistModel ();
            podcast_feed_model = new PodcastFeedModel ();

            podcast_model.ClearModel ();
            podcast_feed_model.ClearModel ();

            podcast_model.QueueAdd (PodcastCore.Library.Podcasts);
            podcast_feed_model.QueueAdd (PodcastCore.Library.Feeds);

            podcast_view = new PodcastPlaylistView (podcast_model);
            podcast_view.ButtonPressEvent += OnPlaylistViewButtonPressEvent;

            podcast_feed_view = new PodcastFeedView (podcast_feed_model);
            podcast_feed_view.Selection.Changed += OnFeedViewSelectionChanged;
            podcast_feed_view.ButtonPressEvent += OnPodcastFeedViewButtonPressEvent;
            podcast_feed_view.SelectAll += OnSelectAllHandler;

            podcast_view_scroller.Add (podcast_view);
            podcast_feed_view_scroller.Add (podcast_feed_view);

            feed_info_pane = new HPaned ();
            feed_info_pane.Add1 (podcast_feed_view_scroller);
            // -- later-- feed_info_pane.Add2 ();

            feed_playlist_pane = new VPaned ();
            feed_playlist_pane.Add1 (feed_info_pane);
            feed_playlist_pane.Add2 (podcast_view_scroller);

            try
            {
                feed_playlist_pane.Position = 
                    GConfSchemas.PlaylistSeparatorPositionSchema.Get ();                        
            }
            catch {
                feed_playlist_pane.Position = 300;
                GConfSchemas.PlaylistSeparatorPositionSchema.Set (
                    feed_playlist_pane.Position
                );
            }

            update_button = new ActionButton (Globals.ActionManager ["PodcastUpdateFeedsAction"]);
            subscribe_button = new ActionButton (Globals.ActionManager ["PodcastSubscribeAction"]);
            subscribe_button.Pixbuf = PodcastPixbufs.PodcastIcon22;
            viewWidget = feed_playlist_pane;

            viewWidget.ShowAll ();
        }

        private void DestroyView ()
        {
            viewWidget = null;

            if (podcast_view != null)
            {
                podcast_view.Shutdown ();
            }

            podcast_view = null;
            podcast_model = null;

            podcast_feed_view = null;
            podcast_feed_model = null;

            feed_view_popup_menu = null;
        }

        public void Update ()
        {
            ThreadAssist.ProxyToMain (
                delegate {
                    podcast_view.QueueDraw ();
                    podcast_feed_view.QueueDraw ();
                }
            );
        }

        public override bool CanWriteToCD {
            get { return false; }
        }
        
        public override bool SearchEnabled {
            get { return false; }
        }
        
        public override IEnumerable<TrackInfo> Tracks {
            get
            {
                return PodcastCore.Library.Tracks;
            }
        }

        public override int Count {
            get { return PodcastCore.Library.TrackCount ;}
        }

        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
        
        public override bool ShowPlaylistHeader {
            get { return false; }
        }
        
        public override Widget ViewWidget {
            get { return viewWidget; }
        }

        private void OnLibraryTrackAdded (object sender, TrackEventArgs args)
        {
            ThreadAssist.ProxyToMain ( delegate {
                                           if (args.Track != null)
                                       {
                                           OnTrackAdded (args.Track)
                                               ;
                                               OnUpdated ();
                                           }
                                           else if (args.Tracks != null)
                                       {
                                           foreach (TrackInfo ti in args.Tracks)
                                               {
                                                   OnTrackAdded (ti);
                                               }
                                               OnUpdated ();
                                           }
                                       });
        }

        private void OnLibraryTrackRemoved (object sender, TrackEventArgs args)
        {
            ThreadAssist.ProxyToMain ( delegate {
                                           if (args.Track != null)
                                       {
                                           OnTrackRemoved (args.Track)
                                               ;
                                               OnUpdated ();
                                           }
                                           else if (args.Tracks != null)
                                       {
                                           foreach (TrackInfo ti in args.Tracks)
                                               {
                                                   OnTrackRemoved (ti);
                                               }
                                               OnUpdated ();
                                           }
                                       });
        }

        private void OnFeedViewSelectionChanged (object sender, EventArgs args)
        {
            PodcastFeedInfo selected_feed = GetSelectedFeed ();

            if (selected_feed == null ||
                    previously_selected == selected_feed)
            {
                return;
            }

            if (previously_selected != PodcastFeedInfo.All)
            {
                if (previously_selected.NewPodcasts > 0)
                {
                    previously_selected.Select ();
                }
            }

            podcast_view.FilterOnFeed (selected_feed);

            previously_selected = selected_feed;
        }

        [GLib.ConnectBefore]
        private void OnPodcastFeedViewButtonPressEvent (object o,
                ButtonPressEventArgs args)
        {
            if (args.Event.Window != podcast_feed_view.BinWindow)
                return;

            TreePath path;

            podcast_feed_view.GetPathAtPos ((int) args.Event.X,
                                            (int) args.Event.Y, out path);

            if (path == null)
            {
                if (args.Event.Button == 3)
                {
                    DefaultMenuPopup (args.Event.Time);
                }
                return;
            }

            podcast_feed_view.Selection.SelectPath (path);
            PodcastFeedInfo clicked_feed = podcast_feed_model.PathPodcastFeedInfo (path);

            switch(args.Event.Type)
            {
                case EventType.ButtonPress:
                    if (args.Event.Button == 3)
                    {
                        if (clicked_feed == null ||
                                clicked_feed == PodcastFeedInfo.All)
                        {
                            DefaultMenuPopup (args.Event.Time);
                        }
                        else
                        {
                            FeedViewMenuPopup (args.Event.Time, clicked_feed);
                        }
                    }

                    args.RetVal = false;
                    return;

                default:
                    args.RetVal = false;
                    return;
            }
        }

        [GLib.ConnectBefore]
        private void OnPlaylistViewButtonPressEvent (object o,
                ButtonPressEventArgs args)
        {
            if (args.Event.Window != podcast_view.BinWindow)
            {
                return;
            }

            TreePath view_path;

            podcast_view.GetPathAtPos((int) args.Event.X,
                                      (int) args.Event.Y, out view_path);

            if (view_path == null)
            {
                if (args.Event.Button == 3)
                {
                    DefaultMenuPopup (args.Event.Time);
                }
                return;
            }

            if (args.Event.Button == 3)
            {
                if (podcast_view.Selection.PathIsSelected (view_path) &&
                        (args.Event.State & (ModifierType.ControlMask |
                                             ModifierType.ShiftMask)) == 0)
                {

                    PodcastPlaylistViewMenuPopup (args.Event.Time);

                    args.RetVal = true;
                    return;
                }
                else if ((args.Event.State & (ModifierType.ControlMask |
                                              ModifierType.ShiftMask)) == 0)
                {

                    TreePath path;

                    podcast_view.GetPathAtPos ((int) args.Event.X,
                                               (int) args.Event.Y, out path);

                    podcast_view.Selection.UnselectAll ();
                    podcast_view.Selection.SelectPath (path);

                    PodcastPlaylistViewMenuPopup (args.Event.Time);

                    args.RetVal = false;
                    return;
                }
            }

            switch(args.Event.Type)
            {
                case EventType.TwoButtonPress:
                    if(args.Event.Button != 1
                            || (args.Event.State & (ModifierType.ControlMask
                                                    | ModifierType.ShiftMask)) != 0)
                        break;

                    TreePath model_path;

                    podcast_view.GetPodcastModelPathAtPos ((int) args.Event.X,
                                                           (int) args.Event.Y, out model_path);

                    podcast_view.Selection.UnselectAll ();
                    podcast_view.SelectPath (model_path);

                    podcast_model.PlayPath (model_path);

                    podcast_view.QueueDraw ();

                    args.RetVal = false;
                    break;

                default:
                    args.RetVal = false;
                    break;
            }
        }

        private void OnPodcastAddedHandler (object sender, PodcastEventArgs args)
        {
            PodcastAddedOrRemoved (args, true);
        }

        private void OnPodcastRemovedHandler (object sender, PodcastEventArgs args)
        {
            PodcastAddedOrRemoved (args, false);
        }

        private void PodcastAddedOrRemoved (PodcastEventArgs args, bool added)
        {
            if (args.Podcast != null)
            {
                PodcastInfo pi = args.Podcast;
                if (added)
                {
                    podcast_model.QueueAdd (pi);
                }
                else
                {
                    podcast_model.QueueRemove (pi);
                }
            }
            else if (args.Podcasts != null)
            {
                ICollection podcasts = args.Podcasts;

                if (added)
                {
                    podcast_model.QueueAdd (podcasts);
                }
                else
                {
                    podcast_model.QueueRemove (podcasts);
                }
            }

            Update ();
        }

        private void OnPodcastFeedAddedHandler (object sender, PodcastFeedEventArgs args)
        {
            PodcastFeedAddedOrRemoved (args, true);
        }

        private void OnPodcastFeedRemovedHandler (object sender, PodcastFeedEventArgs args)
        {
            PodcastFeedAddedOrRemoved (args, false);
        }

        private void PodcastFeedAddedOrRemoved (PodcastFeedEventArgs args, bool added)
        {
            if (args.Feed != null)
            {
                PodcastFeedInfo feed = args.Feed;

                if (added)
                {
                    podcast_feed_model.QueueAdd (feed);
                }
                else
                {
                    // Only single select is handled at the moment.
                    // This will need to be updated if multiple feed selection becomes available.
                    TreeIter iter = feed.TreeIter;

                    podcast_feed_model.QueueRemove (feed);
                    podcast_feed_model.IterNext (ref iter);

                    if (podcast_feed_model.GetPath (iter) != null)
                    {
                        podcast_feed_view.Selection.SelectIter (iter);
                        podcast_feed_view.ScrollToIter (iter);
                        return;
                    }

                    // Should not select first, should select previous.  Why
                    // is there no 'TreeModel.IterPrev' method?
                    podcast_feed_model.GetIterFirst (out iter);

                    if (podcast_feed_model.GetPath (iter) != null)
                    {
                        podcast_feed_view.Selection.SelectIter (iter);
                        podcast_feed_view.ScrollToIter (iter);
                    }
                }
            }
            else if (args.Feeds != null)
            {
                ICollection feeds = args.Feeds;

                if (added)
                {
                    podcast_feed_model.QueueAdd (feeds);
                }
                else
                {
                    podcast_feed_model.QueueRemove (feeds);
                }
            }

            Update ();
        }

        private Menu default_popup_menu;

        private ImageMenuItem default_new_menu_item;
        private ImageMenuItem default_update_all_menu_item;
        private ImageMenuItem default_podcastalley_link_menu_item;

        private void DefaultMenuPopup (uint time)
        {
            if(default_popup_menu == null)
            {

                default_popup_menu = new Menu ();

                default_new_menu_item = new ImageMenuItem (
                                            Catalog.GetString ("Subscribe to Podcast"));
                default_new_menu_item.Image = new Gtk.Image (Gtk.Stock.New, IconSize.Menu);
                default_new_menu_item.Activated += OnFeedMenuNewActivated;

                default_update_all_menu_item =
                    new ImageMenuItem (Catalog.GetString("Update All Podcasts"));
                default_update_all_menu_item.Image = new Gtk.Image (Gtk.Stock.Refresh, IconSize.Menu);
                default_update_all_menu_item.Activated += OnUpdateAllActivated;

                default_podcastalley_link_menu_item =
                    new ImageMenuItem (Catalog.GetString ("Visit Podcast Alley"));
                default_podcastalley_link_menu_item.Image =
                    new Gtk.Image (Gtk.Stock.JumpTo, IconSize.Menu);
                default_podcastalley_link_menu_item.Activated += OnVistPodcastAlleyActivated;

                default_popup_menu.Append (default_update_all_menu_item);
                default_popup_menu.Append (default_new_menu_item);
                default_popup_menu.Append (new SeparatorMenuItem ());
                default_popup_menu.Append (default_podcastalley_link_menu_item);

                default_popup_menu.ShowAll ();
            }

            default_popup_menu.Popup (null, null, null, 0, time);
            return;
        }

        private Menu feed_view_popup_menu;

        private ImageMenuItem feed_update_menu_item;
        private ImageMenuItem feed_new_menu_item;

        private ImageMenuItem feed_remove_menu_item;
        private ImageMenuItem feed_visit_link_menu_item;
        private ImageMenuItem feed_properties_menu_item;
        private CheckMenuItem feed_subscription_menu_item;

        // Should use ActionManager at some point
        private void FeedViewMenuPopup (uint time, PodcastFeedInfo feed)
        {
            if(feed_view_popup_menu == null)
            {

                feed_view_popup_menu = new Menu ();

                feed_subscription_menu_item = new CheckMenuItem (Catalog.GetString("Subscribed"));
                feed_subscription_menu_item.Toggled += OnFeedMenuSubscribeToggled;

                feed_update_menu_item = new ImageMenuItem (Catalog.GetString ("Update Podcast"));
                feed_update_menu_item.Image = new Gtk.Image (Gtk.Stock.Refresh, IconSize.Menu);
                feed_update_menu_item.Activated += OnFeedMenuUpdateCurrentActivated;

                feed_remove_menu_item = new ImageMenuItem (Catalog.GetString ("Delete Podcast"));
                feed_remove_menu_item.Image = new Gtk.Image (Gtk.Stock.Delete, IconSize.Menu);
                feed_remove_menu_item.Activated += OnFeedMenuRemoveActivated;

                feed_new_menu_item = new ImageMenuItem (Catalog.GetString ("Subscribe to Podcast"));
                feed_new_menu_item.Image = new Gtk.Image (Gtk.Stock.New, IconSize.Menu);
                feed_new_menu_item.Activated += OnFeedMenuNewActivated;

                feed_visit_link_menu_item = new ImageMenuItem (Catalog.GetString ("Homepage"));
                feed_visit_link_menu_item.Image = new Gtk.Image (Gtk.Stock.JumpTo, IconSize.Menu);
                feed_visit_link_menu_item.Activated += OnFeedMenuVisitLinkActivated;

                feed_properties_menu_item = new ImageMenuItem (Catalog.GetString ("Properties"));
                feed_properties_menu_item.Image = new Gtk.Image (Gtk.Stock.Properties, IconSize.Menu);
                feed_properties_menu_item.Activated += OnFeedMenuPropertiesActivated;

                feed_view_popup_menu.Append (feed_subscription_menu_item);
                feed_view_popup_menu.Append (new SeparatorMenuItem ());
                feed_view_popup_menu.Append (feed_remove_menu_item);
                feed_view_popup_menu.Append (feed_new_menu_item);
                feed_view_popup_menu.Append (new SeparatorMenuItem ());
                feed_view_popup_menu.Append (feed_visit_link_menu_item);
                feed_view_popup_menu.Append (feed_update_menu_item);
                feed_view_popup_menu.Append (new SeparatorMenuItem ());
                feed_view_popup_menu.Append (feed_properties_menu_item);

                feed_view_popup_menu.ShowAll ();
            }

            feed_subscription_menu_item.Toggled -= OnFeedMenuSubscribeToggled;
            feed_subscription_menu_item.Active = feed.IsSubscribed;
            feed_subscription_menu_item.Toggled += OnFeedMenuSubscribeToggled;

            if (feed.IsBusy)
            {
                // TODO Allow users to delete / unsubscribe from a busy feed.
                // This will be trivial once the download code is moved into PodcastFeedInfo.

                feed_subscription_menu_item.Sensitive = false;
                feed_remove_menu_item.Sensitive = false;
            }
            else
            {
                feed_remove_menu_item.Sensitive = true;
                feed_subscription_menu_item.Sensitive = true;
            }

            feed_view_popup_menu.Popup (null, null, null, 0, time);
            return;
        }

        private Menu playlist_view_popup_menu;

        private ImageMenuItem playlist_remove_item;
        private ImageMenuItem playlist_visit_link_menu_item;
        private ImageMenuItem podcast_properties_menu_item;

        private MenuItem select_all_menu_item;
        private MenuItem unselect_all_menu_item;

        private ImageMenuItem cancel_menu_item;
        private ImageMenuItem download_menu_item;
        private SeparatorMenuItem download_cancel_separator;

        // Should use ActionManager at some point
        private void PodcastPlaylistViewMenuPopup (uint time)
        {
            if (playlist_view_popup_menu == null)
            {
                playlist_view_popup_menu = new Menu ();

                cancel_menu_item = new ImageMenuItem (Catalog.GetString ("Cancel"));
                cancel_menu_item.Image = new Gtk.Image (Gtk.Stock.Cancel, IconSize.Menu);
                cancel_menu_item.Activated += OnCancelPodcastsActivated;

                download_menu_item = new ImageMenuItem (Catalog.GetString ("Download"));
                download_menu_item.Image = new Gtk.Image (Gtk.Stock.GoDown, IconSize.Menu);
                download_menu_item.Activated += OnDownloadPodcastsActivated;

                download_cancel_separator = new SeparatorMenuItem ();

                playlist_remove_item  = new ImageMenuItem (Catalog.GetString ("Remove Episodes(s)"));
                playlist_remove_item.Image = new Gtk.Image (Gtk.Stock.Remove, IconSize.Menu);
                playlist_remove_item.Activated += OnRemovePodcasts;

                select_all_menu_item = new MenuItem (Catalog.GetString ("Select All"));
                select_all_menu_item.Activated += OnSelectAllActivated;

                unselect_all_menu_item = new MenuItem (Catalog.GetString ("Select None"));
                unselect_all_menu_item.Activated += OnSelectNoneActivated;

                playlist_visit_link_menu_item = new ImageMenuItem (Catalog.GetString ("Link"));
                playlist_visit_link_menu_item.Image = new Gtk.Image (Gtk.Stock.JumpTo, IconSize.Menu);
                playlist_visit_link_menu_item.Activated += OnPlaylistMenuVisitLinkActivated;

                podcast_properties_menu_item = new ImageMenuItem (Catalog.GetString ("Properties"));
                podcast_properties_menu_item.Image = new Gtk.Image (Gtk.Stock.Properties, IconSize.Menu);
                podcast_properties_menu_item.Activated += OnPodcastMenuPropertiesActivated;

                playlist_view_popup_menu.Append (playlist_visit_link_menu_item);
                playlist_view_popup_menu.Append (playlist_remove_item);
                playlist_view_popup_menu.Append (new SeparatorMenuItem ());
                playlist_view_popup_menu.Append (select_all_menu_item);
                playlist_view_popup_menu.Append (unselect_all_menu_item);
                playlist_view_popup_menu.Append (download_cancel_separator);
                playlist_view_popup_menu.Append (cancel_menu_item);
                playlist_view_popup_menu.Append (download_menu_item);
                playlist_view_popup_menu.Append (new SeparatorMenuItem ());
                playlist_view_popup_menu.Append (podcast_properties_menu_item);

                playlist_view_popup_menu.ShowAll ();
            }

            if (podcast_view.Selection.CountSelectedRows () > 1)
            {
                podcast_properties_menu_item.Sensitive = false;
                playlist_visit_link_menu_item.Sensitive = false;
            }
            else
            {
                podcast_properties_menu_item.Sensitive = true;
                playlist_visit_link_menu_item.Sensitive = true;
            }

            bool show_cancel = false;
            bool show_remove = false;
            bool show_download = false;

            PodcastInfo[] selected_podcasts = GetSelectedPodcasts ();

            if (selected_podcasts != null)
            {
                foreach (PodcastInfo pi in selected_podcasts)
                {
                    if (!show_cancel && pi.CanCancel)
                    {
                        show_cancel = true;
                    }
                    else if (!show_download && pi.CanDownload)
                    {
                        show_download = true;
                    }

                    if (!show_remove &&
                            (pi.IsDownloaded || pi.CanDownload))
                    {
                        show_remove = true;
                    }

                    if (show_download && show_cancel && show_remove)
                    {
                        break;
                    }
                }
            }

            if (show_cancel || show_download)
            {
                download_cancel_separator.Visible = true;

                if (show_cancel)
                {
                    cancel_menu_item.Visible = true;
                }
                else
                {
                    cancel_menu_item.Visible = false;
                }

                if (show_download)
                {
                    download_menu_item.Visible = true;
                }
                else
                {
                    download_menu_item.Visible = false;
                }
            }
            else
            {
                cancel_menu_item.Visible = false;
                download_menu_item.Visible = false;
                download_cancel_separator.Visible = false;
            }

            if (show_remove)
            {
                playlist_remove_item.Sensitive = true;
            }
            else
            {
                playlist_remove_item.Sensitive = false;
            }

            playlist_view_popup_menu.Popup (null, null, null, 0, time);
        }

        // Everything below here is overly sepcific and will be replaced when a general
        // playlist standard is decided upon.

        private PodcastFeedInfo GetSelectedFeed ()
        {
            TreeIter iter;

            podcast_feed_view.Selection.GetSelected (out iter);

            if (!podcast_feed_model.IterIsValid (iter))
            {
                return null;
            }

            return podcast_feed_model.IterPodcastFeedInfo (iter);
        }

        private PodcastInfo[] GetSelectedPodcasts ()
        {
            int num_selected = podcast_view.Selection.CountSelectedRows ();

            if (num_selected == 0)
            {
                return null;
            }

            TreePath[] paths = podcast_view.Selection.GetSelectedRows ();

            if (paths == null)
            {
                return null;
            }

            TreePath[] model_paths;

            model_paths = podcast_view.GetPodcastModelPath (
                              podcast_view.Selection.GetSelectedRows()
                          );

            if (model_paths == null)
            {
                return null;
            }

            return podcast_model.PathPodcastInfo (model_paths);
        }

        private PodcastInfo GetSelectedPodcast ()
        {
            PodcastInfo[] podcasts = GetSelectedPodcasts ();

            if (podcasts == null || podcasts.Length == 0)
            {
                return null;
            }

            return podcasts [0] as PodcastInfo;
        }

        private void OnSelectAllHandler (object sender, EventArgs args)
        {
            podcast_view.Selection.SelectAll ();
        }

        // menu callbacks -----------------------------------------------//

        private void OnFeedMenuSubscribeToggled (object sender, EventArgs args)
        {
            PodcastFeedInfo feed = GetSelectedFeed ();

            if (feed != null)
            {
                if (feed.IsUpdating)
                {
                    return;
                }

                feed.IsSubscribed = !feed.IsSubscribed;
                feed.Commit ();
                podcast_view.Refilter ();
            }
        }

        private void OnFeedMenuVisitLinkActivated (object sender, EventArgs args)
        {
            PodcastFeedInfo feed = GetSelectedFeed ();

            if (feed != null)
            {
                Gnome.Url.Show (feed.Link.ToString ());
            }
        }

        private void OnFeedMenuRemoveActivated (object sender, EventArgs args)
        {
            PodcastFeedInfo feed = GetSelectedFeed ();

            if (feed != null)
            {
                PodcastCore.Library.RemovePodcastFeed (feed);
            }
        }

        private void OnFeedMenuNewActivated (object sender, EventArgs args)
        {
            ThreadAssist.ProxyToMain ( delegate {
                                           PodcastCore.RunSubscribeDialog ();
                                       });
        }

        private void OnFeedMenuPropertiesActivated (object sender, EventArgs args)
        {
            PodcastFeedInfo feed = GetSelectedFeed ();

            if (feed != null)
            {
                PodcastFeedPropertiesDialog prop_dialog = new PodcastFeedPropertiesDialog (feed);

                ThreadAssist.ProxyToMain ( delegate {
                                               prop_dialog.Show ();
                                           });
            }
        }

        private void OnFeedMenuUpdateCurrentActivated (object sender, EventArgs args)
        {
            PodcastFeedInfo feed = GetSelectedFeed ();

            if (feed != null)
            {
                if (!feed.IsSubscribed)
                {

                    feed.IsSubscribed = true;
                    feed.Commit ();

                    podcast_view.Refilter ();
                    podcast_view.QueueDraw ();
                }

                PodcastCore.FeedFetcher.Update (feed, true);
            }
        }

        private void OnUpdateAllActivated (object sender, EventArgs args)
        {
            PodcastCore.UpdateAllFeeds ();
        }

        private void OnVistPodcastAlleyActivated (object sender, EventArgs args)
        {
            PodcastCore.VisitPodcastAlley ();
        }

        private void OnRemovePodcasts (object sender, EventArgs args)
        {
            PodcastInfo[] podcasts = GetSelectedPodcasts ();

            if (podcasts != null)
            {
                if (podcasts.Length == 1)
                {
                    PodcastCore.Library.RemovePodcast (podcasts[0]);
                }
                else if (podcasts.Length > 1)
                {
                    PodcastCore.Library.RemovePodcasts (podcasts);
                }
            }
        }

        private void OnSelectAllActivated (object sender, EventArgs args)
        {
            podcast_view.Selection.SelectAll ();
        }

        private void OnSelectNoneActivated (object sender, EventArgs args)
        {
            podcast_view.Selection.UnselectAll ();
        }

        private void OnDownloadPodcastsActivated (object sender, EventArgs args)
        {
            PodcastInfo[] podcasts = GetSelectedPodcasts ();

            if (podcasts != null)
            {
                if (podcasts.Length == 1)
                {
                    PodcastCore.QueuePodcastDownload (podcasts[0]);
                }
                else if (podcasts.Length > 1)
                {
                    PodcastCore.QueuePodcastDownload (podcasts);
                }
            }
        }

        private void OnCancelPodcastsActivated (object sender, EventArgs args)
        {
            PodcastInfo[] podcasts = GetSelectedPodcasts ();

            if (podcasts != null)
            {
                if (podcasts.Length == 1)
                {
                    PodcastCore.CancelPodcastDownload (podcasts[0]);
                }
                else if (podcasts.Length > 1)
                {
                    PodcastCore.CancelPodcastDownload (podcasts);
                }
            }
        }

        private void OnPlaylistMenuVisitLinkActivated (object sender, EventArgs args)
        {
            PodcastInfo pi = GetSelectedPodcast ();

            if (pi != null)
            {
                // Link should be of type System.Uri.
                if (pi.Link != null && pi.Link != String.Empty)
                    try
                    {
                        Gnome.Url.Show (pi.Link);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine (e.Message);
                    }
            }
        }

        private void OnPodcastMenuPropertiesActivated (object sender, EventArgs args)
        {
            TreePath[] selection = podcast_view.Selection.GetSelectedRows ();
            TreePath path = podcast_view.GetPodcastModelPath(selection [0]);

            if (path != null)
            {
                PodcastInfo pi = podcast_model.PathPodcastInfo (path);

                if (pi != null)
                {
                    PodcastPropertiesDialog prop_dialog = new PodcastPropertiesDialog (pi);

                    prop_dialog.Show ();
                }
            }
        }
    }
}
