//
// TrackListView.cs
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

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

namespace Banshee.Collection.Gui
{
    public class TrackListView : ListView<TrackInfo>
    {
        private ColumnController column_controller;
        
        public TrackListView() : base()
        {
            column_controller = new ColumnController();
            column_controller.Append(new Column(new ColumnCellPlaybackIndicator(true, 0), "indicator", 
                new ColumnCellPlaybackIndicator(false, 0), 0.05));
                
            column_controller.Append(new SortableColumn("Track", new ColumnCellText(true, 1), 0.10, "track"));
            column_controller.Append(new SortableColumn("Artist", new ColumnCellText(true, 2), 0.225, "artist"));
            column_controller.Append(new SortableColumn("Album", new ColumnCellText(true, 3), 0.225, "album"));
            column_controller.Append(new SortableColumn("Title", new ColumnCellText(true, 4), 0.25, "title"));
            column_controller.Append(new Column("Duration", new ColumnCellDuration(true, 5), 0.15));
            
            ColumnController = DefaultColumnController;
            RulesHint = true;
            
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChanged;
            
            if (ServiceManager.Contains ("GtkElementsService")) {
                ServiceManager.Get<Banshee.Gui.GtkElementsService> ("GtkElementsService").ThemeChanged += delegate {
                    foreach (Column column in column_controller) {
                        column.HeaderCell.NotifyThemeChange ();
                        foreach (ColumnCell cell in column) {
                            cell.NotifyThemeChange ();
                        }
                    }
                    
                    QueueDraw ();
                };
            }
        }

        protected override bool OnPopupMenu ()
        {
            ServiceManager.Get<InterfaceActionService> ("InterfaceActionService").TrackActions["TrackContextMenuAction"].Activate ();
            return true;
        }
        
        private void OnPlayerEngineStateChanged (object o, PlayerEngineStateArgs args)
        {
            QueueDraw ();
        }
        
        public ColumnController DefaultColumnController {
            get { return column_controller; }
        }
    }
}
