//
// TrackListView.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using Mono.Unix;
using Gtk;

using Hyena.Data;
using Hyena.Data.Gui;
using Hyena.Gui;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Playlist;

using Banshee.Gui;

namespace Banshee.Collection.Gui
{
    public class TrackListView : BaseTrackListView
    {
        private ColumnController default_column_controller;
        
        public TrackListView () : base ()
        {
            default_column_controller = new DefaultColumnController ();
        }
        
        public override void SetModel (IListModel<TrackInfo> value, double vpos)
        {
            //Console.WriteLine ("TrackListView.SetModel for {0} with vpos {1}", value, vpos);
            
            if (value != null) {
                Source source = ServiceManager.SourceManager.ActiveSource;
                ColumnController controller = null;
                
                // Get the controller from this source, or its parent(s) if it doesn't have one
                while (source != null && controller == null) {
                    controller = source.Properties.Get<ColumnController> ("TrackView.ColumnController");
                    if (controller == null) {
                        string controller_xml = source.Properties.Get<string> ("TrackView.ColumnControllerXml");
                        if (controller_xml != null) {
                            controller = new XmlColumnController (controller_xml);
                            source.Properties.Remove ("TrackView.ColumnControllerXml");
                            source.Properties.Set<ColumnController> ("TrackView.ColumnController", controller);
                        }
                    }
                    source = source.Parent;
                }
                
                controller = controller ?? default_column_controller;
                
                PersistentColumnController persistent_controller = controller as PersistentColumnController;
                if (persistent_controller != null) {
                    //Hyena.Log.InformationFormat ("Setting controller source to {0}", ServiceManager.SourceManager.ActiveSource.Name);
                    persistent_controller.Source = ServiceManager.SourceManager.ActiveSource;
                }
                
                ColumnController = controller;
            }
            
            base.SetModel (value, vpos);
        }
    }
}
