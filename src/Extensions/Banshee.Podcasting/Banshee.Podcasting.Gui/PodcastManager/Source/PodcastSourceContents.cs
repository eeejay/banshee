/***************************************************************************
 *  PodcastSourceContents.cs
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

using Gtk; 
 
using Hyena.Data; 
using Hyena.Data.Gui;

using Banshee.Base;
using Banshee.Configuration;

using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.ServiceStack;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Collection.Gui;

using Banshee.Podcasting.Data;

namespace Banshee.Podcasting.Gui
{
    public class PodcastSourceContents : VBox, ISourceContents
    {
 	    private PodcastSource source;
 	    
 	    private VPaned vpaned;

 	    private PodcastFeedView feedView;
 	    private PodcastItemView itemView;
 	    
 	    PersistentColumnController itemViewColumnController;
        
        public ISource Source {
            get { return source; } 
        }
        
        public Widget Widget {
            get { return this; } 
        }
 	
 	    public PodcastSourceContents (PodcastFeedView feedView, 
 	                                  PodcastItemView itemView)
 	    {
 	        if (feedView == null) {
 	            throw new ArgumentNullException ("feedView");   
 	        } else if (itemView == null) {
 	            throw new ArgumentNullException ("itemView");
 	        }
 	    
 	        this.feedView = feedView;
 	        this.itemView = itemView;
 	    
 	        InitializeWidget ();
 	    }
 	
        public bool SetSource (ISource source)
        {
            PodcastSource ps = source as PodcastSource;
            
            if (ps != null) {
                this.source = ps;
                
                itemViewColumnController.Source = ps;
                itemViewColumnController.Load ();

                feedView.HeaderVisible = true;
                itemView.HeaderVisible = true;
                
                feedView.SetModel (ps.FeedModel);
                itemView.SetModel (ps.TrackModel as PodcastListModel);
                
                return true;
            }
            
            return false;
        }
        
        public void ResetSource ()
        {
            SaveState ();           

            feedView.SetModel (null);
            itemView.SetModel (null);
            
            feedView.HeaderVisible = false;            
            itemView.HeaderVisible = false;
            
            itemViewColumnController.Source = null;
            source = null;
        }

        private void InitializeWidget ()
        {
            itemViewColumnController = 
                itemView.ColumnController as PersistentColumnController;
            
            ScrolledWindow podcastFeedScroller = new ScrolledWindow ();
            podcastFeedScroller.ShadowType = ShadowType.None;            
            podcastFeedScroller.HscrollbarPolicy = PolicyType.Automatic;
            podcastFeedScroller.VscrollbarPolicy = PolicyType.Automatic;
                        
            ScrolledWindow podcastItemScroller = new ScrolledWindow ();
            podcastItemScroller.ShadowType = ShadowType.None;            
            podcastItemScroller.HscrollbarPolicy = PolicyType.Automatic;
            podcastItemScroller.VscrollbarPolicy = PolicyType.Automatic;            
            
            podcastFeedScroller.Add (feedView);
            podcastItemScroller.Add (itemView);

            vpaned = new VPaned ();
            
            vpaned.Add1 (podcastFeedScroller);
            vpaned.Add2 (podcastItemScroller);            
            
            LoadState ();
            
            feedView.Show ();
            podcastFeedScroller.Show ();  

            itemView.Show ();
            podcastItemScroller.Show ();

            vpaned.Show ();
            
            PackStart (vpaned, true, true, 0);
        }
        
        private void LoadState ()
        {
            vpaned.Position = VPanedPositionSchema.Get ();        
        }

        private void SaveState ()
        {
            VPanedPositionSchema.Set (vpaned.Position);  
            PersistentColumnController itemCC = 
                itemView.ColumnController as PersistentColumnController;
            itemCC.Save ();
        }

        public static readonly SchemaEntry<int> VPanedPositionSchema = new SchemaEntry<int> (
            "plugins.podcasting", "vpaned_position", 120, "VPaned Position", ""
        );     
    }
}