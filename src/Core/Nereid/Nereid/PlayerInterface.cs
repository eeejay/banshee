// 
// PlayerInterface.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Gtk;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Database;
using Banshee.Collection;
using Banshee.Collection.Database;

using Banshee.Collection.Gui;
using Banshee.Sources.Gui;

namespace Nereid
{
    public class PlayerInterface : Window, IService, IDisposable
    {
        // Major Layout Components
        private VBox primary_vbox;
        private HBox header_hbox;
        private HPaned views_pane;
        private ViewContainer view_container;
        
        // Major Interaction Components
        private SourceView source_view;
        private CompositeTrackListView track_view;
        
        public PlayerInterface () : base ("Banshee Music Player")
        {
            ConfigureWindow ();
            BuildPrimaryLayout ();
            ConnectEvents ();
            LoadSettings ();
            ResizeMoveWindow ();
            
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
            BorderWidth = 10;
        }
        
        private void BuildPrimaryLayout ()
        {
            primary_vbox = new VBox ();
            
            BuildHeader ();
            BuildViews ();
            
            primary_vbox.Show ();
            Add (primary_vbox);
        }
        
        private void BuildHeader ()
        {
            header_hbox = new HBox ();
            header_hbox.Show ();

            primary_vbox.PackStart (header_hbox, false, false, 0);
        }
        
        private void BuildViews ()
        {
            views_pane = new HPaned ();
            view_container = new ViewContainer ();
            
            source_view = new SourceView ();
            track_view = new CompositeTrackListView ();
            
            ScrolledWindow source_scroll = new ScrolledWindow ();
            source_scroll.ShadowType = ShadowType.In;
            source_scroll.Add (source_view);       
            
            track_view.TrackView.HeaderVisible = false;
            view_container.Content = track_view;
            
            views_pane.Add1 (source_scroll);
            views_pane.Add2 (view_container);            
            views_pane.ShowAll ();
            
            primary_vbox.PackStart (views_pane, true, true, 0);
        }
        
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
        
#endregion
        
#region Events and Logic Setup
        
        private void ConnectEvents ()
        {
            // Service events
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            
            // UI events
            view_container.SearchEntry.Changed += OnSearchEntryChanged;
            views_pane.SizeRequested += delegate {
                PlayerWindowSchema.SourceViewWidth.Set (views_pane.Position);
            };
        }
        
#endregion
        
#region Service Event Handlers
        
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            
            view_container.SearchSensitive = source != null && source.CanSearch;
            
            if (source == null) {
                return;
            }
            
            view_container.SearchEntry.Ready = false;
            view_container.SearchEntry.CancelSearch ();

            if (source.FilterQuery != null && source.FilterType != TrackFilterType.None) {
                view_container.SearchEntry.Query = source.FilterQuery;
                view_container.SearchEntry.ActivateFilter((int)source.FilterType);
            }
            
            view_container.Title = source.Name;
            
            // Clear any models previously connected to the views            
            track_view.TrackModel = null;
            track_view.ArtistModel = null;
            track_view.AlbumModel = null;
            track_view.TrackView.HeaderVisible = false;
            
            // Connect the source models to the views if possible
            if(source is ITrackModelSource) {
                ITrackModelSource track_source = (ITrackModelSource)source;
                track_view.TrackModel = track_source.TrackModel;
                track_view.ArtistModel = track_source.ArtistModel;
                track_view.AlbumModel = track_source.AlbumModel;
                track_view.TrackView.HeaderVisible = true;
            }
            
            view_container.SearchEntry.Ready = true;
        }
        
#endregion
        
#region UI Event Handlers
        
        private void OnSearchEntryChanged (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            if (source == null) {
                return;
            }
            
            source.FilterType = (TrackFilterType)view_container.SearchEntry.ActiveFilterID;
            source.FilterQuery = view_container.SearchEntry.Query;
            
            if(!(source is ITrackModelSource)) {
                return;
            }
                        
            TrackListModel track_model = ((ITrackModelSource)source).TrackModel;
                        
            if(!(track_model is Hyena.Data.IFilterable)) {
                return;
            }
                        
            Hyena.Data.IFilterable filterable = (Hyena.Data.IFilterable)track_model;
            filterable.Filter = view_container.SearchEntry.Query; 
            filterable.Refilter();
            track_model.Reload();
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
            if((evnt.NewWindowState & Gdk.WindowState.Withdrawn) == 0) {
                PlayerWindowSchema.Maximized.Set((evnt.NewWindowState & Gdk.WindowState.Maximized) != 0);
            }
            
            return base.OnWindowStateEvent (evnt);
        }

        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
            Banshee.ServiceStack.Application.Shutdown ();
            return true;
        }

#endregion

        string IService.ServiceName {
            get { return "NereidPlayerInterface"; }
        }
    }
}
