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

using Migo.Syndication;

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
    public class PodcastSourceContents : ListBrowserSourceContents
    {
        private PodcastItemView track_view;
        private PodcastFeedView feed_view;
        
        public PodcastSourceContents () : base ()
        {
        }

        protected override void InitializeViews ()
        {
            SetupMainView (track_view = new PodcastItemView ());
            SetupFilterView (feed_view = new PodcastFeedView ());
        }
        
        protected override void ClearFilterSelections ()
        {
            feed_view.Selection.Clear ();
        }

        protected override bool ActiveSourceCanHasBrowser {
            get {
                if (!(ServiceManager.SourceManager.ActiveSource is PodcastSource)) {
                    return false;
                }
                
                return ((PodcastSource)ServiceManager.SourceManager.ActiveSource).ShowBrowser;
            }
        }

#region Implement ISourceContents

        public override bool SetSource (ISource source)
        {
            PodcastSource track_source = source as PodcastSource;
            if (track_source == null) {
                return false;
            }
            
            this.source = source;
            
            SetModel (track_view, track_source.TrackModel);
            
            foreach (IListModel model in track_source.FilterModels) {
                if (model is IListModel<Feed>)
                    SetModel (feed_view, (model as IListModel<Feed>));
                else
                    Hyena.Log.DebugFormat ("PodcastContents got non-feed filter model: {0}", model);
            }
            
            track_view.HeaderVisible = true;
            return true;
        }

        public override void ResetSource ()
        {
            source = null;
            track_view.SetModel (null);
            feed_view.SetModel (null);
            track_view.HeaderVisible = false;
        }

#endregion        

        public static readonly SchemaEntry<int> VPanedPositionSchema = new SchemaEntry<int> (
            "plugins.podcasting", "vpaned_position", 120, "VPaned Position", ""
        );     
    }
}