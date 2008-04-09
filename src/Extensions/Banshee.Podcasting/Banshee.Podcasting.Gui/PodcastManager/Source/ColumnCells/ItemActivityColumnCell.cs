/*************************************************************************** 
 *  PodcastItemActivityColumn.cs
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
    public class PodcastItemActivityColumn : PixbufColumnCell
    {
        Dictionary<PodcastItemActivity,Pixbuf> pixbufs = 
            new Dictionary<PodcastItemActivity,Pixbuf> ();
    
        public PodcastItemActivityColumn (string property) : base (property)
        {
        }
        
        protected override void LoadPixbufs ()
        {
            if (pixbufs.Count == 0) {
                pixbufs.Add (PodcastItemActivity.NewPodcastItem, null);
                pixbufs.Add (PodcastItemActivity.DownloadPending, null);
                pixbufs.Add (PodcastItemActivity.Downloading, null);
                pixbufs.Add (PodcastItemActivity.DownloadFailed, null);
                pixbufs.Add (PodcastItemActivity.Video, null);                                
            } else {
                foreach (KeyValuePair<PodcastItemActivity,Pixbuf> kvp in pixbufs) {
                    if (kvp.Value != null) {
                        kvp.Value.Dispose ();
                        pixbufs[kvp.Key] = null;
                    }       
                }   
            }
            
            Gtk.Image i = new Gtk.Image ();
            
            pixbufs[PodcastItemActivity.NewPodcastItem] = 
                IconThemeUtils.LoadIcon ("podcast-new", 16);
            
            pixbufs[PodcastItemActivity.DownloadFailed] =
                i.RenderIcon (Stock.DialogError, IconSize.Menu, "");
            
            pixbufs[PodcastItemActivity.Downloading] =
                i.RenderIcon (Stock.GoForward, IconSize.Menu, "");
            
            pixbufs[PodcastItemActivity.Playing] =
                i.RenderIcon (Stock.MediaPlay, IconSize.Menu, "");            
            
            pixbufs[PodcastItemActivity.Paused] =
                i.RenderIcon (Stock.MediaPause, IconSize.Menu, "");             
            
            pixbufs[PodcastItemActivity.Video] = 
                IconThemeUtils.LoadIcon ("video-x-generic", 16);
            
            i.Sensitive = false;
            pixbufs[PodcastItemActivity.DownloadPending] = 
                i.RenderIcon (Stock.GoForward, IconSize.Menu, "");
                
            pixbufs[PodcastItemActivity.NewPodcastItem] = 
                Gdk.Pixbuf.LoadFromResource ("podcast-new-16.png");
        }
        
        public override void Render (CellContext context, 
                                     StateType state, 
                                     double cellWidth, 
                                     double cellHeight)
        {
            PodcastItemActivity bound = (PodcastItemActivity)BoundObject;
            Pixbuf = (pixbufs.ContainsKey (bound)) ? pixbufs[bound] : null;
            
            base.Render (context, state, cellWidth, cellHeight);
        }
    }
}