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
using Gtk;

using Hyena.Gui;
using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.MediaEngine;

using Banshee.Gui;
using Banshee.Gui.Widgets;
using Banshee.Gui.Dialogs;
using Banshee.Widgets;
using Banshee.Collection.Gui;
using Banshee.Sources.Gui;

namespace Nereid
{
    public class PlayerInterface : Window, IService, IDisposable, IHasTrackSelection, IHasSourceView
    {
        // Major Layout Components
        private VBox primary_vbox;
        private Toolbar header_toolbar;
        private HPaned views_pane;
        private HBox footer_box;
        private ViewContainer view_container;
        
        // Major Interaction Components
        private SourceView source_view;
        private CompositeTrackListView composite_view;
        private ScrolledWindow object_view_scroll;
        private ObjectListView object_view;
        private Label status_label;
        
        // Cached service references
        private GtkElementsService elements_service;
        private InterfaceActionService action_service;
        
        public PlayerInterface () : base ("Banshee Music Player")
        {
            elements_service = ServiceManager.Get<GtkElementsService> ("GtkElementsService");
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            
            ConfigureWindow ();
            BuildPrimaryLayout ();
            ConnectEvents ();
            LoadSettings ();
            ResizeMoveWindow ();
            
            elements_service.PrimaryWindow = this;

            action_service.TrackActions.TrackSelector = this;
            action_service.SourceActions.SourceView = this;
            
            AddAccelGroup (action_service.UIManager.AccelGroup);
            
            composite_view.TrackView.HasFocus = true;
            
            Show ();
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
        
        private void ConfigureWindow ()
        {
            WindowPosition = WindowPosition.Center;
        }
        
        private void BuildPrimaryLayout ()
        {
            primary_vbox = new VBox ();
            
            Widget menu = action_service.UIManager.GetWidget ("/MainMenu");
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
            
            header_toolbar = (Toolbar)action_service.UIManager.GetWidget ("/HeaderToolbar");
            header_toolbar.ShowArrow = false;
            header_toolbar.ToolbarStyle = ToolbarStyle.BothHoriz;
            
            toolbar_alignment.Add (header_toolbar);
            toolbar_alignment.ShowAll ();
            
            primary_vbox.PackStart (toolbar_alignment, false, false, 0);
            
            ConnectedSeekSlider seek_slider = new ConnectedSeekSlider ();
            seek_slider.Show ();
            action_service.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/SeekSlider", seek_slider);
            
            TrackInfoDisplay track_info_display = new TrackInfoDisplay ();
            track_info_display.Show ();
            action_service.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/TrackInfoDisplay", track_info_display, true);
            
            ConnectedVolumeButton volume_button = new ConnectedVolumeButton ();
            volume_button.Show ();
            action_service.PopulateToolbarPlaceholder (header_toolbar, "/HeaderToolbar/VolumeButton", volume_button);
        }

        private void BuildViews ()
        {
            VBox source_box = new VBox ();
            
            views_pane = new HPaned ();
            view_container = new ViewContainer ();
            
            source_view = new SourceView ();
            composite_view = new CompositeTrackListView ();
            
            Hyena.Widgets.ScrolledWindow source_scroll = new Hyena.Widgets.ScrolledWindow ();
            source_scroll.AddWithFrame (source_view);       
            
            composite_view.TrackView.HeaderVisible = false;
            view_container.Content = composite_view;
            
            source_box.PackStart (source_scroll, true, true, 0);
            source_box.PackStart (new UserJobTileHost (), false, false, 0);
            source_box.Show ();
            
            source_view.SetSizeRequest (125, -1);
            view_container.SetSizeRequest (425, -1);
            
            views_pane.Pack1 (source_box, true, false);
            views_pane.Pack2 (view_container, true, false);
            
            source_box.ShowAll ();
            view_container.Show ();
            views_pane.Show ();
            
            primary_vbox.PackStart (views_pane, true, true, 0);
        }

        private void BuildFooter ()
        {
            footer_box = new HBox ();
            footer_box.Spacing = 2;

            status_label = new Label ();
            status_label.ModifyFg (StateType.Normal, Hyena.Gui.GtkUtilities.ColorBlend (
                status_label.Style.Foreground (StateType.Normal), status_label.Style.Background (StateType.Normal)));
            
            ActionButton song_properties_button = new ActionButton
                (action_service.TrackActions["TrackPropertiesAction"]);
            song_properties_button.IconSize = IconSize.Menu;
            song_properties_button.Padding = 0;
            song_properties_button.LabelVisible = false;
            
            //footer_box.PackStart (shuffle_toggle_button, false, false, 0);
            //footer_box.PackStart (repeat_toggle_button, false, false, 0);
            footer_box.PackStart (status_label, true, true, 0);
            footer_box.PackStart (song_properties_button, false, false, 0);

            Alignment align = new Alignment (0.5f, 0.5f, 1.0f, 1.0f);
            align.TopPadding = 2;
            align.BottomPadding = 0;
            align.Add (footer_box);
            align.ShowAll ();

            primary_vbox.PackStart (align, false, true, 0);
        }

#endregion

#region Configuration Loading/Saving        
        
        private void ResizeMoveWindow ()
        {
            int x = PlayerWindowSchema.XPos.Get ();
            int y = PlayerWindowSchema.YPos.Get (); 
            int width = PlayerWindowSchema.Width.Get ();
            int height = PlayerWindowSchema.Height.Get ();
           
            if(width != 0 && height != 0) {
                Resize (width, height);
            }

            if (x == 0 && y == 0) {
                SetPosition (WindowPosition.Center);
            } else {
                Move (x, y);
            }
            
            if (PlayerWindowSchema.Maximized.Get ()) {
                Maximize ();
            } else {
                Unmaximize ();
            }
        }
        
        private void LoadSettings ()
        {
            views_pane.Position = PlayerWindowSchema.SourceViewWidth.Get ();
        }
        
#endregion
        
#region Events and Logic Setup
        
        private void ConnectEvents ()
        {
            // Service events
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;

            action_service.TrackActions ["SearchForSameArtistAction"].Activated += OnProgrammaticSearch;
            action_service.TrackActions ["SearchForSameAlbumAction"].Activated += OnProgrammaticSearch;

            // UI events
            view_container.SearchEntry.Changed += OnSearchEntryChanged;
            views_pane.SizeRequested += delegate {
                PlayerWindowSchema.SourceViewWidth.Set (views_pane.Position);
            };
            
            composite_view.TrackView.RowActivated += delegate (object o, RowActivatedArgs<TrackInfo> args) {
                SetPlaybackControllerSource (ServiceManager.SourceManager.ActiveSource);
                ServiceManager.PlayerEngine.OpenPlay (args.RowValue);
            };

            source_view.RowActivated += delegate {
                SetPlaybackControllerSource (ServiceManager.SourceManager.ActiveSource);
                ServiceManager.PlaybackController.First ();
            };
            
            header_toolbar.ExposeEvent += OnHeaderToolbarExposeEvent;
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

            // Clear any models previously connected to the views
            if (!(source is ITrackModelSource)) {
                composite_view.SetModels (null, null, null);
                composite_view.TrackView.HeaderVisible = false;
            } else if (!(source is Hyena.Data.IObjectListModel)) {
                if (object_view != null) {
                    object_view.SetModel(null);
                }
            }
            
            // Connect the source models to the views if possible
            if (source is ITrackModelSource) {
                if (composite_view.TrackModel != null) {
                    composite_view.TrackModel.Reloaded -= HandleTrackModelReloaded;
                }
                ITrackModelSource track_source = (ITrackModelSource)source;
                composite_view.SetModels (track_source.TrackModel, track_source.ArtistModel, track_source.AlbumModel);
                composite_view.TrackModel.Reloaded += HandleTrackModelReloaded;
                composite_view.TrackView.HeaderVisible = true;
                view_container.Content = composite_view;
            } else if (source is Hyena.Data.IObjectListModel) {
                if (object_view == null) {
                    object_view_scroll = new ScrolledWindow ();
                    object_view = new Hyena.Data.Gui.ObjectListView ();
                    object_view_scroll.Add (object_view);
                    object_view.Show ();
                }
                
                object_view.SetModel((Hyena.Data.IObjectListModel)source);
                view_container.Content = object_view_scroll;
            }
            
            UpdateStatusBar ();
            view_container.SearchEntry.Ready = true;
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
        
        private void OnHeaderToolbarExposeEvent (object o, ExposeEventArgs args)
        {
            // This forces the toolbar to look like it's just a regular plain container
            // since the stock toolbar look makes Banshee look ugly.
            header_toolbar.GdkWindow.DrawRectangle (Style.BackgroundGC (header_toolbar.State), 
                true, header_toolbar.Allocation);
            
            // Manually expose all the toolbar's children
            foreach (Widget child in header_toolbar.Children) {
                header_toolbar.PropagateExpose (child, args.Event);
            }
        }
        
#endregion

#region Implement Interfaces

        // IHasTrackSelection
        public IEnumerable<TrackInfo> GetSelectedTracks ()
        {
            return new ModelSelection<TrackInfo> (composite_view.TrackModel, composite_view.TrackView.Selection);
        }

        public Hyena.Collections.SelectionProxy TrackSelectionProxy {
            get { return composite_view.TrackView.SelectionProxy; }
        }

        public TrackListDatabaseModel TrackModel {
            get { return composite_view.TrackModel as TrackListDatabaseModel; }
        }

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
        
        protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
        {
            int x, y, width, height;

            if((GdkWindow.State & Gdk.WindowState.Maximized) != 0) {
                return base.OnConfigureEvent (evnt);
            }
            
            GetPosition (out x, out y);
            GetSize (out width, out height);
           
            PlayerWindowSchema.XPos.Set (x);
            PlayerWindowSchema.YPos.Set (y);
            PlayerWindowSchema.Width.Set (width);
            PlayerWindowSchema.Height.Set (height);
            
            return base.OnConfigureEvent (evnt);
        }
        
        protected override bool OnWindowStateEvent (Gdk.EventWindowState evnt)
        {
            if ((evnt.NewWindowState & Gdk.WindowState.Withdrawn) == 0) {
                PlayerWindowSchema.Maximized.Set ((evnt.NewWindowState & Gdk.WindowState.Maximized) != 0);
            }
            
            return base.OnWindowStateEvent (evnt);
        }

        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
            Banshee.ServiceStack.Application.Shutdown ();
            return true;
        }
        
        private bool accel_group_active = true;
        
        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            bool focus_search = false;
            
            if (Focus is Entry && GtkUtilities.NoImportantModifiersAreSet ()) {
                if (accel_group_active) {
                    RemoveAccelGroup (action_service.UIManager.AccelGroup);
                    accel_group_active = false;
                 }
            } else {
                if (!accel_group_active) {
                    AddAccelGroup (action_service.UIManager.AccelGroup);
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
            UpdateStatusBar ();
        }

        private void SetPlaybackControllerSource (Source source)
        {
            // Set the source from which to play to the current source since
            // the user manually began playback from this source
            if (!(source is ITrackModelSource)) {
                source = ServiceManager.SourceManager.DefaultSource;
            }
            
            ServiceManager.PlaybackController.Source = (ITrackModelSource)source;    
        }

        private void UpdateStatusBar ()
        {
            if (ServiceManager.SourceManager.ActiveSource == null) {
                status_label.Text = String.Empty;
                return;
            }

            status_label.Text = ServiceManager.SourceManager.ActiveSource.GetStatusText ();
        }

#endregion

        string IService.ServiceName {
            get { return "NereidPlayerInterface"; }
        }
    }
}
