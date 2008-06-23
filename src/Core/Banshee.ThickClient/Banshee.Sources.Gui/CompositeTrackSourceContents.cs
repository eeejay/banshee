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
using Banshee.Collection.Database;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;

namespace Banshee.Sources.Gui
{
    public class CompositeTrackSourceContents : FilteredListSourceContents, ITrackModelSourceContents
    {
        private QueryFilterView<string> genre_view;
        private ArtistListView artist_view;
        private AlbumListView album_view;
        private TrackListView track_view;

        public CompositeTrackSourceContents () : base ("albumartist")
        {
        }

        protected override void InitializeViews ()
        {
            SetupMainView (track_view = new TrackListView ());
            SetupFilterView (genre_view = new QueryFilterView<string> (Catalog.GetString ("Not Set")));
            SetupFilterView (artist_view = new ArtistListView ());
            SetupFilterView (album_view = new AlbumListView ());
        }
        
        protected override void ClearFilterSelections ()
        {
            if (genre_view.Model != null) {
                genre_view.Selection.Clear ();
            }
            if (artist_view.Model != null) {
                artist_view.Selection.Clear ();
            }
            if (album_view.Model != null) {
                album_view.Selection.Clear ();
            }
        }

        public void SetModels (TrackListModel track, IListModel<ArtistInfo> artist, IListModel<AlbumInfo> album, IListModel<QueryFilterInfo<string>> genre)
        {
            SetModel (track);
            SetModel (artist);
            SetModel (album);
            SetModel (genre);
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

        protected override bool ActiveSourceCanHasBrowser {
            get {
                if (!(ServiceManager.SourceManager.ActiveSource is ITrackModelSource)) {
                    return false;
                }
                
                return ((ITrackModelSource)ServiceManager.SourceManager.ActiveSource).ShowBrowser;
            }
        }

#region Implement ISourceContents

        public override bool SetSource (ISource source)
        {
            //Console.WriteLine ("CTSC.set_source 1");
            ITrackModelSource track_source = source as ITrackModelSource;
            if (track_source == null) {
                return false;
            }
            
            this.source = source;
            
            SetModel (track_view, track_source.TrackModel);
            
            foreach (IListModel model in track_source.FilterModels) {
                if (model is IListModel<ArtistInfo>)
                    SetModel (artist_view, (model as IListModel<ArtistInfo>));
                else if (model is IListModel<AlbumInfo>)
                    SetModel (album_view, (model as IListModel<AlbumInfo>));
                else if (model is IListModel<QueryFilterInfo<string>>)
                    SetModel (genre_view, (model as IListModel<QueryFilterInfo<string>>));
                else
                    Hyena.Log.DebugFormat ("CompositeTrackSourceContents got non-album/artist filter model: {0}", model);
            }
            
            track_view.HeaderVisible = true;
            //Console.WriteLine ("CTSC.set_source 2");
            return true;
        }

        public override void ResetSource ()
        {
            //Console.WriteLine ("CTSC.reset_source 1");
            source = null;
            track_view.SetModel (null);
            artist_view.SetModel (null);
            album_view.SetModel (null);
            genre_view.SetModel (null);
            track_view.HeaderVisible = false;
            //Console.WriteLine ("CTSC.reset_source 2");
        }

#endregion

    }
}
