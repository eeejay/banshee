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

using Hyena.Data.Gui;

using Banshee.Collection;

namespace Banshee.Collection.Gui
{
    public class TrackListView : ListView<TrackInfo>
    {
        private ColumnController column_controller;
        
        public TrackListView() : base()
        {
            column_controller = new ColumnController();
            column_controller.Append(new Column("Track", new ColumnCellText(true, 0), 0.10));
            column_controller.Append(new SortableColumn("Artist", new ColumnCellText(true, 1), 0.25, "artist"));
            column_controller.Append(new SortableColumn("Album", new ColumnCellText(true, 2), 0.25, "album"));
            column_controller.Append(new SortableColumn("Title", new ColumnCellText(true, 3), 0.25, "title"));
            column_controller.Append(new Column("Duration", new ColumnCellText(true, 4), 0.15));
            
            ColumnController = DefaultColumnController;
        }
        
        public ColumnController DefaultColumnController {
            get { return column_controller; }
        }
    }
}
