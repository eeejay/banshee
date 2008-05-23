// 
// PlayerInterface.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Hyena.Gui;
using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.MediaEngine;
using Banshee.Configuration;

using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Gui.Dialogs;
using Banshee.Widgets;
using Banshee.Collection.Gui;
using Banshee.Sources.Gui;

namespace Nereid
{
    public class PlayerInterface : BaseClientWindow, IService, IDisposable, IHasSourceView
    {
        // Major Layout Components
        private VBox primary_vbox;
        private Toolbar header_toolbar;
        private Toolbar footer_toolbar;
        private HPaned views_pane;
        private ViewContainer view_container;
        
        // Major Interaction Components
        private SourceView source_view;
        private CompositeTrackSourceContents composite_view;
        private ObjectListSourceContents object_view;
        private Label status_label;
        
        public PlayerInterface () : base (Catalog.GetString ("Banshee Media Player"))
        {
        }
        
        protected override void Initialize ()
        {
            BuildPrimaryLayout ();
            ConnectEvents ();

            ActionService.SourceActions.SourceView = this;
            
            composite_view.TrackView.HasFocus = true;
            
            Show ();
        }
        
        protected override void UpdateTitle ()
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
            if (track != null) {
                // Translators: this is the window title when a track is playing
                //              {0} is the track title, {1} is the artist name
                Title = String.Format (Catalog.GetString ("{0} by {1}"), 
                    track.DisplayTrackTitle, track.DisplayArtistName);
            } else {
                Title = Catalog.GetString ("Banshee Media Player");
            }
            
            OnTitleChanged ();
        }
        
#region System Overrides 
        
        public override void Dispose ()
        {
            lock (this) {
                Hide ();
                base.Dispose ();
                Gtk.Application.Quit ();
            }
        }
        
#endregion        

#region Interface Construction
        
        private void BuildPrimaryLayout ()
        {
            primary_vbox = new VBox ();
            
            Widget menu = new MainMenu ();
            menu.Show ();
            primary_vbox.PackStart (menu, false, false, 0);
           
            BuildHeader ();
            BuildViews ();
            BuildFooter ();
            
            primary_vbox.Show ();
            Add (primary_vbox);
        }
        
        private void BuildHeader ()
        {
            Alignment toolbar_alignment = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
            toolbar_alignment.TopPadding = 3;
            toolbar_alignment.BottomPadding = 3;
            
            header_toolbar = (Toolbar)ActionService.UIManager.GetWidget ("/HeaderToolbar");
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
            
            toolbar_alignment.Add (header_toolbar);
            toolbar_alignment.ShowAll ();
            
            primary_vbox.PackStart (toolbar_alignment, false, false, 0);
            
            Widget next_button = new NextButton (ActionService);
            next_button.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/NextArrowButton", next_button);
            
            ConnectedSeekSlider seek_slider = new ConnectedSeekSlider ();
            seek_slider.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/SeekSlider", seek_slider);
            
            TrackInfoDisplay track_info_display = new TrackInfoDisplay ();
            track_info_display.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/TrackInfoDisplay", track_info_display, true);
            
            ConnectedVolumeButton volume_button = new ConnectedVolumeButton ();
            volume_button.Show ();
            ActionService.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/VolumeButton", volume_button);
        }

        private void BuildViews ()
        {
            VBox source_box = new VBox ();
            
            views_pane = new HPaned ();
            PersistentPaneController.Control (views_pane, SourceViewWidth);
            view_container = new ViewContainer ();
            
            source_view = new SourceView ();
            composite_view = new CompositeTrackSourceContents ();
            
            Hyena.Widgets.ScrolledWindow source_scroll = new Hyena.Widgets.ScrolledWindow ();
            source_scroll.AddWithFrame (source_view);       
            
            composite_view.TrackView.HeaderVisible = false;
            view_container.Content = composite_view;
            
            source_box.PackStart (source_scroll, true, true, 0);
            source_box.PackStart (new UserJobTileHost (), false, false, 0);
            
            source_view.SetSizeRequest (125, -1);
            view_container.SetSizeRequest (425, -1);
            
            views_pane.Pack1 (source_box, false, false);
            views_pane.Pack2 (view_container, true, false);
            
            source_box.ShowAll ();
            view_container.Show ();
            views_pane.Show ();
            
            primary_vbox.PackStart (views_pane, true, true, 0);
        }

        private void BuildFooter ()
        {
            footer_toolbar = (Toolbar)ActionService.UIManager.GetWidget ("/FooterToolbar");
            footer_toolbar.ShowArrow = false;
            footer_toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;

            EventBox status_event_box = new EventBox ();
            status_event_box.ButtonPressEvent += delegate (object o, ButtonPressEventArgs args) {
                Source source = ServiceManager.SourceManager.ActiveSource;
                if (source != null) {
                    source.CycleStatusFormat ();
                    UpdateSourceInformation ();
                }
            };
            status_label = new Label ();
            status_event_box.Add (status_label);
            
            Alignment status_align = new Alignment (0.5f, 0.5f, 1.0f, 1.0f);
            status_align.Add (status_event_box);

            RepeatActionButton repeat_button = new RepeatActionButton ();
            repeat_button.SizeAllocated += delegate (object o, Gtk.SizeAllocatedArgs args) {
                status_align.LeftPadding = (uint)args.Allocation.Width;
            };

            ActionService.PopulateToolbarPlaceholder (footer_toolbar, "/FooterToolbar/StatusBar", status_align, true);
            ActionService.PopulateToolbarPlaceholder (footer_toolbar, "/FooterToolbar/RepeatButton", repeat_button);

            footer_toolbar.ShowAll ();
            primary_vbox.PackStart (footer_toolbar, false, true, 0);
        }

#endregion
        
#region Events and Logic Setup
        
        private void ConnectEvents ()
        {
            // Service events
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            ServiceManager.SourceManager.SourceUpdated += OnSourceUpdated;
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent, 
                PlayerEvent.StartOfStream |
                PlayerEvent.TrackInfoUpdated |
                PlayerEvent.EndOfStream);
            
            ActionService.TrackActions ["SearchForSameArtistAction"].Activated += OnProgrammaticSearch;
            ActionService.TrackActions ["SearchForSameAlbumAction"].Activated += OnProgrammaticSearch;

            // UI events
            view_container.SearchEntry.Changed += OnSearchEntryChanged;
            views_pane.SizeRequested += delegate {
                SourceViewWidth.Set (views_pane.Position);
            };
            
            source_view.RowActivated += delegate {
                Source source = ServiceManager.SourceManager.ActiveSource;
                if (source is ITrackModelSource) {
                    ServiceManager.PlaybackController.NextSource = (ITrackModelSource)source;
                    // Allow changing the play source without stopping the current song by
                    // holding ctrl when activating a source. After the song is done, playback will
                    // continue from the new source.
                    if (GtkUtilities.NoImportantModifiersAreSet (Gdk.ModifierType.ControlMask)) {
                        ServiceManager.PlaybackController.Next ();
                    }
                }
            };
            
            header_toolbar.ExposeEvent += OnToolbarExposeEvent;
            footer_toolbar.ExposeEvent += OnToolbarExposeEvent;
        }
        
#endregion

#region Service Event Handlers

        private void OnProgrammaticSearch (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            view_container.SearchEntry.Ready = false;
            view_container.SearchEntry.Query = source.FilterQuery;
            view_container.SearchEntry.Ready = true;
        }
        
        private TrackListModel previous_track_model = null;
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;

            view_container.SearchSensitive = source != null && source.CanSearch;
            
            if (source == null) {
                return;
            }
            
            view_container.Title = source.Name;
            view_container.SearchEntry.Ready = false;
            view_container.SearchEntry.CancelSearch ();

            if (source.FilterQuery != null) {
                view_container.SearchEntry.Query = source.FilterQuery;
                view_container.SearchEntry.ActivateFilter ((int)source.FilterType);
            }

            if (view_container.Content != null) {
                view_container.Content.ResetSource ();
            }

            if (previous_track_model != null) {
                previous_track_model.Reloaded -= HandleTrackModelReloaded;
                previous_track_model = null;
            }

            if (source is ITrackModelSource) {
                previous_track_model = (source as ITrackModelSource).TrackModel;
                previous_track_model.Reloaded += HandleTrackModelReloaded;
            }

            // Connect the source models to the views if possible
            if (source.Properties.Contains ("Nereid.SourceContents")) {
                view_container.Content = source.Properties.Get<ISourceContents> ("Nereid.SourceContents");
                view_container.Content.SetSource (source);
                view_container.Show ();
            } else if (source is ITrackModelSource) {
                view_container.Content = composite_view;
                view_container.Content.SetSource (source);
                view_container.Show ();
            } else if (source is Hyena.Data.IObjectListModel) {
                if (object_view == null) {
                    object_view = new ObjectListSourceContents ();
                }
                
                view_container.Content = object_view;
                view_container.Content.SetSource (source);
                view_container.Show ();
            } else {
                view_container.Hide ();
            }

            // Associate the view with the model
            if (view_container.Visible && view_container.Content is ITrackModelSourceContents) {
                ITrackModelSourceContents track_content = view_container.Content as ITrackModelSourceContents;
                source.Properties.Set<IListView<TrackInfo>>  ("Track.IListView", track_content.TrackView);
                source.Properties.Set<IListView<ArtistInfo>> ("Artist.IListView", track_content.ArtistView);
                source.Properties.Set<IListView<AlbumInfo>>  ("Album.IListView", track_content.AlbumView);
            }

            view_container.Header.Visible = source.Properties.Contains ("Nereid.SourceContents.HeaderVisible") ?
                source.Properties.Get<bool> ("Nereid.SourceContents.HeaderVisible") : true;

            view_container.DiskUsageVisible = (source is IDiskUsageReporter);
            
            UpdateSourceInformation ();
            view_container.SearchEntry.Ready = true;
        }

        private void OnSourceUpdated (SourceEventArgs args)
        {
            if (args.Source == ServiceManager.SourceManager.ActiveSource) {
                UpdateSourceInformation ();
                view_container.Title = args.Source.Name;
            }
        }
        
        private void OnPlayerEvent (PlayerEventArgs args) 
        {
            UpdateTitle ();
        }
        
#endregion

#region UI Event Handlers
        
        private void OnSearchEntryChanged (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source == null)
                return;
            
            source.FilterType = (TrackFilterType)view_container.SearchEntry.ActiveFilterID;
            source.FilterQuery = view_container.SearchEntry.Query;
        }
        
        private void OnToolbarExposeEvent (object o, ExposeEventArgs args)
        {
            Toolbar toolbar = (Toolbar)o;

            // This forces the toolbar to look like it's just a regular part
            // of the window since the stock toolbar look makes Banshee look ugly.
            Style.ApplyDefaultBackground (toolbar.GdkWindow, true, State, 
                args.Event.Area, toolbar.Allocation.X, toolbar.Allocation.Y, 
                toolbar.Allocation.Width, toolbar.Allocation.Height);

            // Manually expose all the toolbar's children
            foreach (Widget child in toolbar.Children) {
                toolbar.PropagateExpose (child, args.Event);
            }
        }
        
#endregion

#region Implement Interfaces

        // IHasTrackSelection
        /*public IEnumerable<TrackInfo> GetSelectedTracks ()
        {
            return new ModelSelection<TrackInfo> (composite_view.TrackModel, composite_view.TrackView.Selection);
        }

        public Hyena.Collections.SelectionProxy TrackSelectionProxy {
            get { return composite_view.TrackView.SelectionProxy; }
        }

        public DatabaseTrackListModel TrackModel {
            get { return composite_view.TrackModel as DatabaseTrackListModel; }
        }*/

        // IHasSourceView
        public Source HighlightedSource {
            get { return source_view.HighlightedSource; }
        }

        public void BeginRenameSource (Source source)
        {
            source_view.BeginRenameSource (source);
        }
        
        public void ResetHighlight ()
        {
            source_view.ResetHighlight ();
        }

#endregion
        
#region Gtk.Window Overrides

        private bool accel_group_active = true;
        
        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            bool focus_search = false;
            
            if (Focus is Entry && (GtkUtilities.NoImportantModifiersAreSet () && 
                evnt.Key != Gdk.Key.Control_L && evnt.Key != Gdk.Key.Control_R)) {
                if (accel_group_active) {
                    RemoveAccelGroup (ActionService.UIManager.AccelGroup);
                    accel_group_active = false;
                 }
            } else {
                if (!accel_group_active) {
                    AddAccelGroup (ActionService.UIManager.AccelGroup);
                    accel_group_active = true;
                }
            }
            
            switch (evnt.Key) {
                case Gdk.Key.f:
                    if (Gdk.ModifierType.ControlMask == (evnt.State & Gdk.ModifierType.ControlMask)) {
                        focus_search = true;
                    }
                    break;

                case Gdk.Key.S:  case Gdk.Key.s:
                case Gdk.Key.F3: case Gdk.Key.slash:
                    focus_search = true;
                    break;
            }

            if (focus_search && !view_container.SearchEntry.HasFocus && !source_view.EditingRow) {
                view_container.SearchEntry.HasFocus = true;
                return true;
            }
            
            return base.OnKeyPressEvent (evnt);
        }

#endregion

#region Helper Functions

        private void HandleTrackModelReloaded (object sender, EventArgs args)
        {
            UpdateSourceInformation ();
        }

        private void UpdateSourceInformation ()
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source == null) {
                status_label.Text = String.Empty;
                return;
            }

            if (source is IDiskUsageReporter) {
                IDiskUsageReporter reporter = source as IDiskUsageReporter;

                double fraction = (double)reporter.BytesUsed / (double)reporter.BytesCapacity;

                view_container.DiskUsageBar.Fraction = fraction;
                view_container.DiskUsageBar.Text = String.Format(
                    Catalog.GetString("{0} of {1}"),
                    new Hyena.Query.FileSizeQueryValue (reporter.BytesUsed).ToUserQuery (),
                    new Hyena.Query.FileSizeQueryValue (reporter.BytesCapacity).ToUserQuery ()
                );

                //string tooltip = dapSource.DiskUsageString + " (" + dapSource.DiskAvailableString + ")";
                //toolTips.SetTip (dapDiskUsageBar, tooltip, tooltip);
            }

            status_label.Text = source.GetStatusText ();
        }

#endregion

#region Configuration Schemas

        public static readonly SchemaEntry<int> SourceViewWidth = new SchemaEntry<int> (
            "player_window", "source_view_width",
            175,
            "Source View Width",
            "Width of Source View Column."
        );

        public static readonly SchemaEntry<bool> ShowCoverArt = new SchemaEntry<bool> (
            "player_window", "show_cover_art",
            true,
            "Show cover art",
            "Show cover art below source view if available"
        );

#endregion

        string IService.ServiceName {
            get { return "NereidPlayerInterface"; }
        }
    }
}
