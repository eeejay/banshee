//
// CompositeTrackListView.cs
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

using Hyena.Data.Gui;

using Banshee.Collection;

namespace Banshee.Collection.Gui
{
    public class CompositeTrackListView : HPaned
    {
        private ArtistListView artist_view;
        private AlbumListView album_view;
        private TrackListView track_view;
        
        private ScrolledWindow artist_scrolled_window;
        private ScrolledWindow album_scrolled_window;
        private ScrolledWindow track_scrolled_window;
        
        private VPaned artist_album_box;
        
        public CompositeTrackListView()
        {
            artist_view = new ArtistListView();
            album_view = new AlbumListView();
            track_view = new TrackListView();
            
            artist_view.HeaderVisible = false;
            album_view.HeaderVisible = false;
            
            artist_view.Selection.Changed += OnBrowserViewSelectionChanged;
            album_view.Selection.Changed += OnBrowserViewSelectionChanged;
            
            artist_scrolled_window = new ScrolledWindow();
            artist_scrolled_window.Add(artist_view);
            artist_scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            artist_scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            
            album_scrolled_window = new ScrolledWindow();
            album_scrolled_window.Add(album_view);
            album_scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            album_scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            
            track_scrolled_window = new ScrolledWindow();
            track_scrolled_window.Add(track_view);
            track_scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            track_scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            
            artist_album_box = new VPaned();
            
            artist_album_box.Add1(artist_scrolled_window);
            artist_album_box.Add2(album_scrolled_window);
            artist_album_box.Position = 350;
            
            Add1(artist_album_box);
            Add2(track_scrolled_window);
            
            Position = 275;
            
            artist_album_box.ShowAll();
            track_view.Show();
        }
        
        protected virtual void OnBrowserViewSelectionChanged(object o, EventArgs args)
        {
            Hyena.Data.Gui.Selection selection = (Hyena.Data.Gui.Selection)o;
            object view = selection.Owner;
            TrackListModel model = track_view.Model as TrackListModel;
            
            if(selection.Count == 1 && selection.Contains(0) || selection.AllSelected) {
                if(view is ArtistListView && model != null) {
                    model.ArtistInfoFilter = null;
                } else if(view is AlbumListView && model != null) {
                    model.AlbumInfoFilter = null;
                }
                return;
            } else if(selection.Count == 0) {
                selection.Select(0);
            } else if(selection.Count > 0 && !selection.AllSelected) {
                selection.Unselect(0);
            }
            
            if(view is ArtistListView) {
                ArtistInfo [] artists = new ArtistInfo[selection.Count];
                int i = 0;
            
                foreach(int row_index in artist_view.Selection) {
                    artists[i++] = artist_view.Model.GetValue(row_index);
                }
            
                model.ArtistInfoFilter = artists;
                ((AlbumListModel)album_view.Model).ArtistInfoFilter = artists;
                
                album_view.Selection.Select(0);
                album_view.Selection.Clear();
            } else if(view is AlbumListView) {
                AlbumInfo [] albums = new AlbumInfo[selection.Count];
                int i = 0;
            
                foreach(int row_index in album_view.Selection) {
                    albums[i++] = album_view.Model.GetValue(row_index);
                }
            
                model.AlbumInfoFilter = albums;
            }
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
            set { track_view.Model = value; }
        }
        
        public ArtistListModel ArtistModel {
            get { return (ArtistListModel)artist_view.Model; }
            set { artist_view.Model = value; }
        }
        
        public AlbumListModel AlbumModel {
            get { return (AlbumListModel)album_view.Model; }
            set { album_view.Model = value; }
        }
    }
}
