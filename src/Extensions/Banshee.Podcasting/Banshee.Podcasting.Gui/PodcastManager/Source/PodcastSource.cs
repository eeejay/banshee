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

using Banshee.Gui;
using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Collection.Database;

using Banshee.Sources;
using Banshee.Sources.Gui;

using Banshee.Podcasting.Data;

using Migo.Syndication;

namespace Banshee.Podcasting.Gui
{
    public class PodcastSource : PrimarySource
    {        
        private int count;

        private string baseDirectory;

 	    private PodcastFeedView feedView;
 	    private PodcastItemView itemView;     
 	    
        private PodcastFeedModel feedModel;
        private PodcastItemModel itemModel;           
        
        private string tuid = "podcasting";
        
        private readonly object sync = new object (); 

        public override string BaseDirectory {
            get { return baseDirectory; }
        }
        
        internal string BaseDirectorySet {
            set { baseDirectory = value; }        
        }

        public override bool CanRename {
            get { return false; }
        }
               
        public override int Count {
            get { lock (sync) { return count; } }
        }
        
        public int CountSet {
            set { 
                lock (sync) {
                    if (count != value) {
                        count = value;
                        OnUpdated ();
                    }
                } 
            }        
        }
       
        public PodcastFeedModel FeedModel {
            get { return feedModel; }
        }
        
        public PodcastItemModel ItemModel {
            get { return itemModel; }
        }        

        public PodcastFeedView FeedView {
            get { return feedView; }
        }
        
        public PodcastItemView ItemView {
            get { return itemView; }
        }        

        protected override string TypeUniqueId {
            get { return tuid; }
        }

        public override bool CanSearch 
        {
            get { return false; }
        }

        public PodcastSource (PodcastFeedModel feedModel,
                              PodcastItemModel itemModel) : 
                              this (null, feedModel, itemModel)
        {
        }

        public PodcastSource (string baseDirectory,
                              PodcastFeedModel feedModel,
                              PodcastItemModel itemModel) : base (
                              "PodcastLibrary",
                              Catalog.GetString ("Podcasts"), 
                              "PodcastLibrary", 100)
        {
            if (feedModel == null) {
                throw new ArgumentNullException ("feedModel");
            } else if (itemModel == null) {
                throw new ArgumentNullException ("itemModel");
            }
            
            this.baseDirectory = baseDirectory;
            // track_model
            album_model = null;
            artist_model = null;
            
            this.feedModel = feedModel;
            this.itemModel = itemModel;

            feedView = new PodcastFeedView ();
            itemView = new PodcastItemView ();

            Properties.SetString ("Icon.Name", "podcast-icon-22");
            Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            Properties.SetString ("GtkActionPath", "/PodcastSourcePopup");
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);

            Properties.Set<ISourceContents> (
                "Nereid.SourceContents", 
                new PodcastSourceContents (feedView, itemView)
            );
        }

        public override void Reload ()
        {
            itemModel.Reload ();
            feedModel.Reload ();
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
