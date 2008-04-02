//
// CompositeTrackSourceContents.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.Reflection;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Widgets;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;

namespace Banshee.Sources.Gui
{
    public class CompositeTrackSourceContents : VBox, ITrackModelSourceContents
    {
        private ArtistListView artist_view;
        private AlbumListView album_view;
        private TrackListView track_view;
        
        private ArtistListModel artist_model;
        private AlbumListModel album_model;
        private TrackListModel track_model;

        private Dictionary<object, double> model_positions = new Dictionary<object, double> ();
        
        private Gtk.ScrolledWindow artist_scrolled_window;
        private Gtk.ScrolledWindow album_scrolled_window;
        private Gtk.ScrolledWindow track_scrolled_window;
        private bool view_is_built = false;
        
        private Paned container;
        private Widget browser_container;
        private InterfaceActionService action_service;
        private ActionGroup browser_view_actions;
        
        private static string menu_xml = @"
            <ui>
              <menubar name=""MainMenu"">
                <menu name=""ViewMenu"" action=""ViewMenuAction"">
                  <placeholder name=""BrowserViews"">
                    <menuitem name=""BrowserVisible"" action=""BrowserVisibleAction"" />
                    <separator />
                    <menuitem name=""BrowserTop"" action=""BrowserTopAction"" />
                    <menuitem name=""BrowserLeft"" action=""BrowserLeftAction"" />
                    <separator />
                  </placeholder>
                </menu>
              </menubar>
            </ui>
        ";
        
        public CompositeTrackSourceContents ()
        {
            string position = BrowserPosition.Get ();
            if (position == "top") {
                LayoutTop ();
            } else {
                LayoutLeft ();
            }
            
            if (ServiceManager.Contains ("InterfaceActionService")) {
                action_service = ServiceManager.Get<InterfaceActionService> ();
                
                browser_view_actions = new ActionGroup ("BrowserView");
                
                browser_view_actions.Add (new RadioActionEntry [] {
                    new RadioActionEntry ("BrowserLeftAction", null, 
                        Catalog.GetString ("Browser on Left"), null,
                        Catalog.GetString ("Show the artist/album browser to the left of the track list"), 0),
                    
                    new RadioActionEntry ("BrowserTopAction", null,
                        Catalog.GetString ("Browser on Top"), null,
                        Catalog.GetString ("Show the artist/album browser above the track list"), 1),
                }, position == "top" ? 1 : 0, OnViewModeChanged);
                
                browser_view_actions.Add (new ToggleActionEntry [] {
                    new ToggleActionEntry ("BrowserVisibleAction", null,
                        Catalog.GetString ("Show Browser"), "<control>B",
                        Catalog.GetString ("Show or hide the artist/album browser"), 
                        OnToggleBrowser, BrowserVisible.Get ())
                });
                
                action_service.AddActionGroup (browser_view_actions);
                //action_merge_id = action_service.UIManager.NewMergeId ();
                action_service.UIManager.AddUiFromString (menu_xml);
            }
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate {
                browser_container.Visible = ActiveSourceCanHasBrowser ? BrowserVisible.Get () : false; 
            };
            
            NoShowAll = true;
        }
        
        private void BuildCommon ()
        {
            if (view_is_built) {
                return;
            }
            
            artist_view = new ArtistListView ();
            album_view = new AlbumListView ();
            track_view = new TrackListView ();
        
            artist_view.HeaderVisible = false;
            album_view.HeaderVisible = false;
            
            if (Banshee.Base.ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
                artist_scrolled_window = new SmoothScrolledWindow ();
                album_scrolled_window = new SmoothScrolledWindow ();
                track_scrolled_window = new SmoothScrolledWindow ();
            } else {
                artist_scrolled_window = new Gtk.ScrolledWindow ();
                album_scrolled_window = new Gtk.ScrolledWindow ();
                track_scrolled_window = new Gtk.ScrolledWindow ();
            }
            
            artist_scrolled_window.Add (artist_view);
            artist_scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            artist_scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            
            album_scrolled_window.Add (album_view);
            album_scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            album_scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            
            track_scrolled_window.Add (track_view);
            track_scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            track_scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            
            track_view.SetModel (track_model);
            artist_view.SetModel (artist_model);
            album_view.SetModel (album_model);

            artist_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
            album_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
            
            view_is_built = true;
        }
        
        private void Reset ()
        {
            // Unparent the views' scrolled window parents so they can be re-packed in 
            // a new layout. The main container gets destroyed since it will be recreated.
            
            if (artist_scrolled_window != null) {
                Container artist_album_container = artist_scrolled_window.Parent as Container;
                if (artist_album_container != null) {
                    artist_album_container.Remove (artist_scrolled_window);
                    artist_album_container.Remove (album_scrolled_window);
                }
            }
            
            if (track_scrolled_window != null) {
                container.Remove (track_scrolled_window);
            }
            
            if (container != null) {
                Remove (container);
                container.Destroy ();
            }
            
            BuildCommon ();
        }

        private void LayoutLeft ()
        {
            Reset ();
            
            container = new HPaned ();
            VPaned artist_album_box = new VPaned ();
            
            artist_album_box.Add1 (artist_scrolled_window);
            artist_album_box.Add2 (album_scrolled_window);

            artist_album_box.Position = 350;
            
            container.Add1 (artist_album_box);
            container.Add2 (track_scrolled_window);
            
            browser_container = artist_album_box;
            
            container.Position = 275;
            ShowPack ();
        }
        
        private void LayoutTop ()
        {
            Reset ();
            
            container = new VPaned ();
            HBox artist_album_box = new HBox ();
            artist_album_box.Spacing = 10;
            
            artist_album_box.PackStart (artist_scrolled_window, true, true, 0);
            artist_album_box.PackStart (album_scrolled_window, true, true, 0);
            
            container.Add1 (artist_album_box);
            container.Add2 (track_scrolled_window);
            
            browser_container = artist_album_box;
            
            container.Position = 175;
            ShowPack ();
        }
        
        private void ShowPack ()
        {
            PackStart (container, true, true, 0);
            NoShowAll = false;
            ShowAll ();
            NoShowAll = true;
            browser_container.Visible = BrowserVisible.Get ();
        }
        
        private void OnViewModeChanged (object o, ChangedArgs args)
        {
            if (args.Current.Value == 0) {
                LayoutLeft ();
                BrowserPosition.Set ("left");
            } else {
                LayoutTop ();
                BrowserPosition.Set ("top");
            }
        }
                
        private void OnToggleBrowser (object o, EventArgs args)
        {
            ToggleAction action = (ToggleAction)o;
            artist_view.Selection.Clear ();
            album_view.Selection.Clear ();
            browser_container.Visible = action.Active && ActiveSourceCanHasBrowser;
            BrowserVisible.Set (action.Active);
        }
        
        protected virtual void OnBrowserViewSelectionChanged (object o, EventArgs args)
        {
            Hyena.Collections.Selection selection = (Hyena.Collections.Selection)o;
            TrackListModel model = track_view.Model as TrackListModel;

            if (selection.AllSelected) {
                if (model != null && o == artist_view.Selection ) {
                    artist_view.ScrollTo (0);
                } else if (model != null && o == album_view.Selection) {
                    album_view.ScrollTo (0);
                }
                return;
            }
        }

        public void SetModels (TrackListModel track, ArtistListModel artist, AlbumListModel album)
        {
            // Save the old vertical positions
            if (track_model != null) {
                model_positions[track_model] = track_view.Vadjustment.Value;
            }
            
            if (artist_model != null) {
                model_positions[artist_model] = artist_view.Vadjustment.Value;
            }
            
            if (album_model != null) {
                model_positions[album_model] = album_view.Vadjustment.Value;
            }
            
            // Set the new models and restore positions
            track_model = track;
            artist_model = artist;
            album_model = album;

            // Initialize the new positions if needed
            if (track_model != null) {
                if (!model_positions.ContainsKey (track_model)) {
                    model_positions[track_model] = 0.0;
                }
                
                track_view.SetModel (track_model, model_positions[track_model]);
            }

            if (artist_model != null) {
                if (!model_positions.ContainsKey (artist_model)) {
                    model_positions[artist_model] = 0.0;
                }
                
                artist_view.SetModel (artist_model, model_positions[artist_model]);
            }

            if (album_model != null) {
                if (!model_positions.ContainsKey (album_model)) {
                    model_positions[album_model] = 0.0;
                }
                
                album_view.SetModel (album_model, model_positions[album_model]);
            }
        }
        
        IListView<TrackInfo> ITrackModelSourceContents.TrackView {
            get { return track_view; }
        }
        
        IListView<ArtistInfo> ITrackModelSourceContents.ArtistView {
            get { return artist_view; }
        }
        
        IListView<AlbumInfo> ITrackModelSourceContents.AlbumView {
            get { return album_view; }
        }

        public TrackListView TrackView {
            get { return track_view; }
        }
        
        public ArtistListView ArtistView {
            get { return artist_view; }
        }
        
        public AlbumListView AlbumView {
            get { return album_view; }
        }
        
        public TrackListModel TrackModel {
            get { return (TrackListModel)track_view.Model; }
        }
        
        public ArtistListModel ArtistModel {
            get { return (ArtistListModel)artist_view.Model; }
        }

        public AlbumListModel AlbumModel {
            get { return (AlbumListModel)album_view.Model; }
        }

        private bool ActiveSourceCanHasBrowser {
            get {
                if (!(ServiceManager.SourceManager.ActiveSource is ITrackModelSource)) {
                    return false;
                }
                
                return ((ITrackModelSource)ServiceManager.SourceManager.ActiveSource).ShowBrowser;
            }
        }

#region Implement ISourceContents

        private ITrackModelSource track_source;

        public bool SetSource (ISource source)
        {
            track_source = source as ITrackModelSource;
            if (track_source == null) {
                return false;
            }

            SetModels (track_source.TrackModel, track_source.ArtistModel, track_source.AlbumModel);
            track_view.HeaderVisible = true;
            return true;
        }

        public void ResetSource ()
        {
            track_source = null;
            SetModels (null, null, null);
            track_view.HeaderVisible = false;
        }

        public ISource Source {
            get { return track_source; }
        }

        public Widget Widget {
            get { return this; }
        }

#endregion
        
        public static readonly SchemaEntry<bool> BrowserVisible = new SchemaEntry<bool> (
            "browser", "visible",
            true,
            "Artist/Album Browser Visibility",
            "Whether or not to show the Artist/Album browser"
        );
        
        public static readonly SchemaEntry<string> BrowserPosition = new SchemaEntry<string> (
            "browser", "position",
            "left",
            "Artist/Album Browser Position",
            "The position of the Artist/Album browser; either 'top' or 'left'"
        );
    }
}
