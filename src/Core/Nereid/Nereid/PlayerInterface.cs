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
        public PlayerInterface () : base ("Banshee")
        {
            WindowPosition = WindowPosition.Center;
            BorderWidth = 10;
            SetSizeRequest(1150, 750);
            
            SourceView source_view = new SourceView();
            
            VBox box = new VBox();
            HBox header = new HBox();
            
            box.Spacing = 10;
            header.Spacing = 5;
            
            Label filter_label = new Label("Filter:");
            Entry filter_entry = new Entry();
            Button clear_reload_button = new Button("Clear Filter & Reload");
            Button reload_button = new Button("Reload");
            Button clear_button = new Button("Clear Model");
            Label track_count_label = new Label();
            
            header.PackStart(filter_label, false, false, 0);
            header.PackStart(filter_entry, false, false, 0);
            /*header.PackStart(clear_reload_button, false, false, 0);
            header.PackStart(reload_button, false, false, 0);
            header.PackStart(clear_button, false, false, 0);
            header.PackStart(track_count_label, false, false, 0);*/
            
            CompositeTrackListView view = new CompositeTrackListView();
            view.TrackView.HeaderVisible = false;
            
            ServiceManager.SourceManager.ActiveSourceChanged += delegate {
                view.TrackModel = null;
                view.ArtistModel = null;
                view.AlbumModel = null;
                view.TrackView.HeaderVisible = false;
                
                if(ServiceManager.SourceManager.ActiveSource is ITrackModelSource) {
                    ITrackModelSource track_source = (ITrackModelSource)ServiceManager.SourceManager.ActiveSource;
                    view.TrackModel = track_source.TrackModel;
                    view.ArtistModel = track_source.ArtistModel;
                    view.AlbumModel = track_source.AlbumModel;
                    view.TrackView.HeaderVisible = true;
                }
            };
                    
            box.PackStart(header, false, false, 0);
            
            HPaned view_pane = new HPaned();
            ScrolledWindow source_scroll = new ScrolledWindow();
            source_scroll.ShadowType = ShadowType.In;
            source_scroll.Add(source_view);
            view_pane.Add1(source_scroll);
            view_pane.Add2(view);
            view_pane.Position = 200;
            box.PackStart(view_pane, true, true, 0);
            
            Add(box);
            
            ShowAll();
            
            uint filter_timeout = 0;
            
            filter_entry.Changed += delegate { 
                if(filter_timeout == 0) {
                    filter_timeout = GLib.Timeout.Add(25, delegate {
                        Source source = ServiceManager.SourceManager.ActiveSource;
                        if(!(source is ITrackModelSource)) {
                            filter_timeout = 0;
                            return false;
                        }
                        
                        TrackListModel track_model = ((ITrackModelSource)source).TrackModel;
                        
                        if(!(track_model is Hyena.Data.IFilterable)) {
                            filter_timeout = 0;
                            return false;
                        }
                        
                        Hyena.Data.IFilterable filterable = (Hyena.Data.IFilterable)track_model;
                        
                        filterable.Filter = filter_entry.Text; 
                        filterable.Refilter();
                        track_model.Reload();
                        filter_timeout = 0;
                        return false;
                    });
                }
            };
        }
        
        public override void Dispose ()
        {
            lock (this) {
                Hide ();
                base.Dispose ();
                Gtk.Application.Quit ();
            }
        }
        
        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
            Banshee.ServiceStack.Application.Shutdown ();
            return true;
        }

        string IService.ServiceName {
            get { return "NereidPlayerInterface"; }
        }
    }
}
