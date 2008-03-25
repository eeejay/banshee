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
using Cairo;
using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Gui.Theatrics;
using Hyena.Data.Gui;

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

namespace Banshee.Collection.Gui
{
    public class TrackListView : ListView<TrackInfo>
    {
        private PersistentColumnController column_controller;

        public TrackListView () : base ()
        {
            column_controller = new PersistentColumnController (String.Format ("{0}.{1}", 
                Banshee.ServiceStack.Application.ActiveClient.ClientId, "track_view_columns"));

            SortableColumn artist_column = new SortableColumn (Catalog.GetString ("Artist"), new ColumnCellText ("ArtistName", true), 0.225, "Artist", true);

            column_controller.AddRange (
                new Column (null, "indicator", new ColumnCellPlaybackIndicator (null), 0.05, true),
                new SortableColumn (Catalog.GetString ("Track"), new ColumnCellTrackNumber ("TrackNumber", true), 0.10, "Track", true),
                new SortableColumn (Catalog.GetString ("Title"), new ColumnCellText ("TrackTitle", true), 0.25, "Title", true),
                artist_column,
                new SortableColumn (Catalog.GetString ("Album"), new ColumnCellText ("AlbumTitle", true), 0.225, "Album", true),
                new SortableColumn (Catalog.GetString ("Composer"), new ColumnCellText ("Composer", true), 0.25, "Composer", false),
                new SortableColumn (Catalog.GetString ("Duration"), new ColumnCellDuration ("Duration", true), 0.15, "Duration", true),
                
                new SortableColumn (Catalog.GetString ("Year"), new ColumnCellText ("Year", true), 0.15, "Year", false),
                new SortableColumn (Catalog.GetString ("Genre"), new ColumnCellText ("Genre", true), 0.25, "Genre", false),
                new SortableColumn (Catalog.GetString ("Play Count"), new ColumnCellText ("PlayCount", true), 0.15, "PlayCount", false),
                new SortableColumn (Catalog.GetString ("Skip Count"), new ColumnCellText ("SkipCount", true), 0.15, "SkipCount", false),
                //new SortableColumn ("Rating", new RatingColumnCell (null, true), 0.15, "Rating"),
                new SortableColumn (Catalog.GetString ("Last Played"), new ColumnCellDateTime ("LastPlayed", true), 0.15, "LastPlayedStamp", false),
                new SortableColumn (Catalog.GetString ("Last Skipped"), new ColumnCellDateTime ("LastSkipped", true), 0.15, "LastSkippedStamp", false),
                new SortableColumn (Catalog.GetString ("Date Added"), new ColumnCellDateTime ("DateAdded", true), 0.15, "DateAddedStamp", false),
                new SortableColumn (Catalog.GetString ("Location"), new ColumnCellText ("Uri", true), 0.15, "Uri", false)
            );
            
            column_controller.Load ();
            
            ColumnController = DefaultColumnController;
            ColumnController.DefaultSortColumn = artist_column;

            RulesHint = true;
            RowSensitivePropertyName = "CanPlay";
            
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChanged;
            
            if (ServiceManager.Contains<GtkElementsService> ()) {
                ServiceManager.Get<GtkElementsService> ().ThemeChanged += delegate {
                    foreach (Column column in column_controller) {
                        if (column.HeaderCell != null) {
                            column.HeaderCell.NotifyThemeChange ();
                        }
                        
                        foreach (ColumnCell cell in column) {
                            cell.NotifyThemeChange ();
                        }
                    }
                    
                    QueueDraw ();
                };
            }
            
            ForceDragSourceSet = true;
            Reorderable = true;
        }

        protected override bool OnPopupMenu ()
        {
            ServiceManager.Get<InterfaceActionService> ().TrackActions["TrackContextMenuAction"].Activate ();
            return true;
        }
        
        private void OnPlayerEngineStateChanged (object o, PlayerEngineStateArgs args)
        {
            QueueDraw ();
        }
        
#region Drag and Drop

        protected override void OnDragSourceSet ()
        {
            base.OnDragSourceSet ();
            Drag.SourceSetIconName (this, "audio-x-generic");
        }

        protected override void OnDragDataGet (Gdk.DragContext context, SelectionData selection_data, uint info, uint time)
        {
            if (info != (int)ListViewDragDropTarget.TargetType.ModelSelection || Selection.Count <= 0) {
                return;
            }
        }

#endregion
        
        public ColumnController DefaultColumnController {
            get { return column_controller; }
        }
    }
}
