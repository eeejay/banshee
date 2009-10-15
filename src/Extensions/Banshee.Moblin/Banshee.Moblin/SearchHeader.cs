// 
// SearchHeader.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Mono.Unix;
using Gtk;

using Hyena.Gui;

using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Gui;

namespace Banshee.Moblin
{
    public class SearchHeader : HBox
    {
        public SearchHeader ()
        {
            Spacing = 10;
            BorderWidth = 10;
            PackStart (new Label () { Markup = String.Format ("<big><b>{0}</b></big>",
                GLib.Markup.EscapeText (Catalog.GetString ("Media"))) }, false, false, 0);

            var search = new SearchEntry ();
            search.Entry.Activated += (o, e) => {
                var source = ServiceManager.SourceManager.MusicLibrary;
                if (source != null) {
                    source.FilterType = (TrackFilterType)search.Entry.ActiveFilterID;
                    source.FilterQuery = search.Entry.Query;
                    ServiceManager.SourceManager.SetActiveSource (source);
                    ServiceManager.Get<GtkElementsService> ().PrimaryWindow.Present ();
                    search.Entry.Query = String.Empty;
                }
            };
            PackStart (search, true, true, 0);
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (!Visible || !IsMapped) {
                return true;
            }
            
            RenderBackground (evnt.Window, evnt.Region);
            foreach (var child in Children) {
                PropagateExpose (child, evnt);
            }
            
            return true;
        }
        
        private void RenderBackground (Gdk.Window window, Gdk.Region region)
        {   
            Cairo.Context cr = Gdk.CairoHelper.Create (window);
            
            cr.Color = new Cairo.Color (0xe7 / (double)0xff,
                0xea / (double)0xff, 0xfd / (double)0xff);
            
            CairoExtensions.RoundedRectangle (cr,
                Allocation.X, Allocation.Y,
                Allocation.Width, Allocation.Height, 5);
            
            cr.Fill ();
            
            CairoExtensions.DisposeContext (cr);
        }
    }
}
