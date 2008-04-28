/***************************************************************************
 *  PodcastManagerSource.cs
 *
 *  Copyright (C) 2007 Michael C. Urbanski
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
using System.Collections.Generic;

using Gtk;
using Gdk;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Data.Sqlite;

using Banshee.Gui;
using Banshee.Base;
using Banshee.Database;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Collection.Database;

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.Podcasting.Data;

using Migo.Syndication;

namespace Banshee.Podcasting.Gui
{
    public class PodcastListModel : DatabaseTrackListModel, IListModel<PodcastItem>
    {
        public PodcastListModel (BansheeDbConnection conn, IDatabaseTrackModelProvider provider) : base (conn, provider)
        {
        }
        
        public new PodcastItem this[int index] {
            get {
                lock (this) {
                    return cache.GetValue (index) as PodcastItem;
                }
            }
        }
    }
    
    public class PodcastSource : PrimarySource
    {
        private PodcastFeedModel feed_model;
        
        private PodcastFeedView feed_view;
        public PodcastFeedView FeedView {
            get { return feed_view; }
        }
        
        private PodcastItemView item_view;
        public PodcastItemView ItemView {
            get { return item_view; }
        }
        
        private string baseDirectory;
        public override string BaseDirectory {
            get { return baseDirectory; }
        }

        public override bool CanRename {
            get { return false; }
        }

        public override bool CanAddTracks {
            get { return false; }
        }
        
        public PodcastFeedModel FeedModel {
            get { return feed_model; }
        }

        public PodcastSource () : this (null)
        {
        }

        public PodcastSource (string baseDirectory) : base ("PodcastLibrary", Catalog.GetString ("Podcasts"), "PodcastLibrary", 100)
        {
            this.baseDirectory = baseDirectory;

            Properties.SetString ("Icon.Name", "podcast-icon-22");
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/PodcastSourcePopup");
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);

            feed_view = new PodcastFeedView ();
            item_view = new PodcastItemView ();
            Properties.Set<ISourceContents> (
                "Nereid.SourceContents", 
                new PodcastSourceContents (feed_view, item_view)
            );
        }
        
        protected override bool HasArtistAlbum {
            get { return false; }
        }
        
        protected override void InitializeTrackModel ()
        {
            DatabaseTrackModelProvider<PodcastItem> track_provider =
                new DatabaseTrackModelProvider<PodcastItem> (ServiceManager.DbConnection);

            DatabaseTrackModel = new PodcastListModel (ServiceManager.DbConnection, track_provider);

            TrackCache = new DatabaseTrackModelCache<PodcastItem> (ServiceManager.DbConnection,
                    UniqueId, track_model, track_provider);
                    
            feed_model = new PodcastFeedModel ();
            
            AfterInitialized ();
        }

        public override void Reload ()
        {
            feed_model.Reload ();
            TrackModel.Reload ();
        }

/*
        public new TrackListModel TrackModel {
            get { return null; }
        }

        public override void RemoveSelectedTracks ()
        {
        }

        public override void DeleteSelectedTracks ()
        {
            throw new InvalidOperationException ();
        }

        public override bool CanRemoveTracks {
            get { return false; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool ConfirmRemoveTracks {
            get { return false; }
        }
        
        public override bool ShowBrowser {
            get { return false; }
        }
*/
    }
}
