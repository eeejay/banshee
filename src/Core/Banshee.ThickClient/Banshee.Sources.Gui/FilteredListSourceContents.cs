//
// ListBrowserSourceContents.cs
//
// Authors:
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

using ScrolledWindow=Gtk.ScrolledWindow;

namespace Banshee.Sources.Gui
{
    public abstract class FilteredListSourceContents : VBox, ISourceContents
    {
        private object main_view;
        private Gtk.ScrolledWindow main_scrolled_window;
        
        private List<object> filter_views = new List<object> ();
        private List<ScrolledWindow> filter_scrolled_windows = new List<ScrolledWindow> ();
        
        private Dictionary<object, double> model_positions = new Dictionary<object, double> ();
        
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

        public FilteredListSourceContents ()
        {
            InitializeViews ();
        
            string position = BrowserPosition.Get ();
            if (position == "top") {
                LayoutTop ();
            } else {
                LayoutLeft ();
            }
            
            if (ServiceManager.Contains ("InterfaceActionService")) {
                action_service = ServiceManager.Get<InterfaceActionService> ();
                
                if (action_service.FindActionGroup ("BrowserView") == null) {
                    browser_view_actions = new ActionGroup ("BrowserView");
                    
                    browser_view_actions.Add (new RadioActionEntry [] {
                        new RadioActionEntry ("BrowserLeftAction", null, 
                            Catalog.GetString ("Browser on Left"), null,
                            Catalog.GetString ("Show the artist/album browser to the left of the track list"), 0),
                        
                        new RadioActionEntry ("BrowserTopAction", null,
                            Catalog.GetString ("Browser on Top"), null,
                            Catalog.GetString ("Show the artist/album browser above the track list"), 1),
                    }, position == "top" ? 1 : 0, null);
                    
                    browser_view_actions.Add (new ToggleActionEntry [] {
                        new ToggleActionEntry ("BrowserVisibleAction", null,
                            Catalog.GetString ("Show Browser"), "<control>B",
                            Catalog.GetString ("Show or hide the artist/album browser"), 
                            null, BrowserVisible.Get ())
                    });
                    
                    action_service.AddActionGroup (browser_view_actions);
                    //action_merge_id = action_service.UIManager.NewMergeId ();
                    action_service.UIManager.AddUiFromString (menu_xml);
                }
                
                (action_service.FindAction("BrowserView.BrowserLeftAction") as RadioAction).Changed += OnViewModeChanged;
                (action_service.FindAction("BrowserView.BrowserTopAction") as RadioAction).Changed += OnViewModeChanged;
                action_service.FindAction("BrowserView.BrowserVisibleAction").Activated += OnToggleBrowser;
            }
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate {
                browser_container.Visible = ActiveSourceCanHasBrowser ? BrowserVisible.Get () : false; 
            };
            
            NoShowAll = true;
        }
        
        protected abstract void InitializeViews ();
        
        protected void SetupMainView<T> (ListView<T> main_view)
        {
            main_scrolled_window = SetupView (main_view);
        }
        
        protected void SetupFilterView<T> (ListView<T> filter_view)
        {
            filter_scrolled_windows.Add (SetupView (filter_view));
            filter_view.HeaderVisible = false;
            filter_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
        }
        
        private ScrolledWindow SetupView (Widget view)
        {
            ScrolledWindow window = null;
            
            if (Banshee.Base.ApplicationContext.CommandLine.Contains ("smooth-scroll")) {
                window = new SmoothScrolledWindow ();
            } else {
                window = new ScrolledWindow ();
            }
            
            window.Add (view);
            window.HscrollbarPolicy = PolicyType.Automatic;
            window.VscrollbarPolicy = PolicyType.Automatic;

            return window;
        }
        
        private void Reset ()
        {
            //Hyena.Log.Information ("ListBrowser Reset");
            // Unparent the views' scrolled window parents so they can be re-packed in 
            // a new layout. The main container gets destroyed since it will be recreated.
            
            if (filter_scrolled_windows.Count > 0) {
                Container filter_container = filter_scrolled_windows[0].Parent as Container;
                if (filter_container != null) {
                    foreach (ScrolledWindow window in filter_scrolled_windows) {
                        filter_container.Remove (window);
                    }
                } //else
                    //Hyena.Log.Information ("No filter container");
            } //else
                //Hyena.Log.Information ("No filter windows");
            
            if (container != null && main_scrolled_window != null) {
                container.Remove (main_scrolled_window);
            } //else
                //Hyena.Log.Information ("No main container");
            
            if (container != null) {
                Remove (container);
                container.Destroy ();
            }
        }

        private void LayoutLeft ()
        {
            //Hyena.Log.Information ("ListBrowser LayoutLeft");
            Reset ();
            
            container = new HPaned ();
            VBox filter_box = new VBox ();
            filter_box.Spacing = 10;
            
            foreach (ScrolledWindow window in filter_scrolled_windows) {
                filter_box.PackStart (window, true, true, 0);
            }
            
            //filter_box.Position = 350;
            //PersistentPaneController.Control (filter_box, "browser.left.artist_album_box");
            
            container.Add1 (filter_box);
            container.Add2 (main_scrolled_window);
            
            browser_container = filter_box;
            
            container.Position = 275;
            PersistentPaneController.Control (container, "browser.left");
            ShowPack ();
        }
        
        private void LayoutTop ()
        {
            //Hyena.Log.Information ("ListBrowser LayoutTop");
            Reset ();
            
            container = new VPaned ();
            HBox filter_box = new HBox ();
            filter_box.Spacing = 10;
            
            foreach (ScrolledWindow window in filter_scrolled_windows) {
                filter_box.PackStart (window, true, true, 0);
            }
            
            container.Add1 (filter_box);
            container.Add2 (main_scrolled_window);
            
            browser_container = filter_box;
            
            container.Position = 175;
            PersistentPaneController.Control (container, "browser.top");
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
            //Hyena.Log.InformationFormat ("ListBrowser mode toggled, val = {0}", args.Current.Value);
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
            
            browser_container.Visible = action.Active && ActiveSourceCanHasBrowser;
            BrowserVisible.Set (action.Active);
            
            if (!browser_container.Visible) {
                ClearFilterSelections ();
            }
        }
        
        protected abstract void ClearFilterSelections ();
        
        protected virtual void OnBrowserViewSelectionChanged (object o, EventArgs args)
        {
            Hyena.Collections.Selection selection = (Hyena.Collections.Selection) o;

            if (selection.AllSelected) {
                foreach (IListView view in filter_views) {
                    if (view.Selection == selection) {
                        view.ScrollTo (0);
                    }
                }
            }
        }

        protected void SetModel<T> (IListModel<T> model)
        {
            ListView<T> view = FindListView <T> ();
            if (view != null) {
                SetModel (view, model);
            } else {
                Hyena.Log.DebugFormat ("Unable to find view for model {0}", model);
            }
        }
        
        protected void SetModel<T> (ListView<T> view, IListModel<T> model)
        {
            if (view.Model != null) {
                model_positions[view.Model] = view.Vadjustment.Value;
            }
            
            if (!model_positions.ContainsKey (model))
                    model_positions[model] = 0.0;
            
            view.SetModel (model, model_positions[model]);
        }
        
        private ListView<T> FindListView<T> ()
        {
            if (main_view is ListView<T>)
                return (ListView<T>) main_view;
        
            foreach (object view in filter_views)
                if (view is ListView<T>)
                    return (ListView<T>) view;

            return null;
        }

        protected abstract bool ActiveSourceCanHasBrowser { get; }

#region Implement ISourceContents

        protected ISource source;

        public abstract bool SetSource (ISource source);

        public abstract void ResetSource ();

        public ISource Source {
            get { return source; }
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
