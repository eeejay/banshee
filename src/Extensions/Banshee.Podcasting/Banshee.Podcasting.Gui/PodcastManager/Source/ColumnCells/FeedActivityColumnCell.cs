/*************************************************************************** 
 *  PodcastFeedActivityColumn.cs
 *
 *  Copyright (C) 2008 Michael C. Urbanski
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

using Gdk;
using Gtk;
using Cairo;

using Hyena.Data.Gui;

using Banshee.Gui;
using Banshee.Podcasting.Data;

namespace Banshee.Podcasting.Gui
{
    public class FeedActivityColumnCell : PixbufColumnCell
    {
        Dictionary<PodcastFeedActivity,Pixbuf> pixbufs = 
            new Dictionary<PodcastFeedActivity,Pixbuf> ();
    
        public FeedActivityColumnCell (string property) : base (property)
        {
        }
        
        protected override void LoadPixbufs ()
        {
            if (pixbufs.Count == 0) {
                pixbufs.Add (PodcastFeedActivity.Updating, null);
                pixbufs.Add (PodcastFeedActivity.UpdateFailed, null);
                pixbufs.Add (PodcastFeedActivity.ItemsDownloading, null);
                pixbufs.Add (PodcastFeedActivity.ItemsQueued, null);
            } else {
                foreach (KeyValuePair<PodcastFeedActivity,Pixbuf> kvp in pixbufs) {
                    if (kvp.Value != null) {
                        kvp.Value.Dispose ();
                        pixbufs[kvp.Key] = null;
                    }       
                }   
            }
            
            Gtk.Image pp = new Gtk.Image ();

            pixbufs[PodcastFeedActivity.Updating] = 
                pp.RenderIcon (Stock.Refresh, IconSize.Menu, "");
            
            pixbufs[PodcastFeedActivity.UpdateFailed] =
                pp.RenderIcon (Stock.DialogError, IconSize.Menu, "");
            
            pixbufs[PodcastFeedActivity.ItemsDownloading] =
                pp.RenderIcon (Stock.GoForward, IconSize.Menu, "");
            
            pp.Sensitive = false;
            pixbufs[PodcastFeedActivity.ItemsQueued] = 
                pp.RenderIcon (Stock.GoForward, IconSize.Menu, "");
        }
        
        public override void Render (CellContext context, 
                                     StateType state, 
                                     double cellWidth, 
                                     double cellHeight)
        {
            PodcastFeedActivity bound = (PodcastFeedActivity)BoundObject;
            Pixbuf = (pixbufs.ContainsKey (bound)) ? pixbufs[bound] : null;
            
            base.Render (context, state, cellWidth, cellHeight);
        }
    }   
}
