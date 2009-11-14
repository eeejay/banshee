//
// TileView.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.Collections.Generic;

using Gtk;

namespace Banshee.Widgets
{
    public class TileView : Gtk.Layout
    {
        private int current_column_count = 0;
        private int cached_widget_width = -1;
        private int cached_table_spacing = 0;
        private List<Table> cached_tables = new List<Table>();
        private List<Widget> widgets = new List<Widget>();

        public TileView(int initialColumnCount) : base(null, null)
        {
            current_column_count = initialColumnCount;

            Table table = new Table(1, 1, true);

            table.Show();
            cached_tables.Add(table);
            Add(table);

            Show ();
        }

        public void AddWidget(Widget widget)
        {
            widgets.Add(widget);
            LayoutTableDefault(cached_tables[0], widgets);
        }

        public void RemoveWidget(Widget widget)
        {
            widgets.Remove(widget);
            LayoutTableDefault(cached_tables[0], widgets);
        }

        public void ClearWidgets()
        {
            widgets.Clear();
            LayoutTableDefault(cached_tables[0], widgets);
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            Widget child = null;

            if(Children != null && Children.Length > 0) {
                child = Children[0];
            }

            if(child == null) {
                base.OnSizeAllocated(allocation);
                SetSize((uint)allocation.Width, (uint)allocation.Height);
                return;
            }

            if(cached_tables.Count == 0) {
                base.OnSizeAllocated(allocation);

                Gdk.Rectangle child_allocation;
                child_allocation.X = 0;
                child_allocation.Y = 0;
                child_allocation.Width = Math.Max(allocation.Width, child.Requisition.Width);
                child_allocation.Height = Math.Max(allocation.Height, child.Requisition.Height);

                child.SizeAllocate(child_allocation);
                SetSize((uint)child_allocation.Width, (uint)child_allocation.Height);
                return;
            }

            Table first_table = cached_tables[0];
            int usable_area = allocation.Width - (child.Requisition.Width - first_table.Requisition.Width);
            int new_column_count = RelayoutTablesIfNeeded(usable_area, current_column_count);

            if(current_column_count != new_column_count) {
                child.SizeRequest();
                current_column_count = new_column_count;
            }

            base.OnSizeAllocated(allocation);
            SetSizeRequest (child.Allocation.Width, child.Allocation.Height);
            SetSize ((uint)child.Allocation.Width, (uint)child.Allocation.Height);
        }

        private void RemoveContainerEntries(Container widget)
        {
            if(widget.Children == null) {
                return;
            }

            while(widget.Children.Length > 0) {
                widget.Remove(widget.Children[0]);
            }
        }

        private void ResizeTable(Table table, int columns, IList<Widget> list)
        {
            RemoveContainerEntries(table);

            double rows = (double)list.Count / (double)columns;
            double remainder = rows - (int)rows;

            if(remainder != 0.0) {
                rows++;
            }

            if(rows > 0 && columns > 0) {
                table.Resize((uint)rows, (uint)columns);
            }
        }

        private void RelayoutTable(Table table, IList<Widget> widgets)
        {
            uint maxcols = table.NColumns;
            uint row = 0, col = 0;

            foreach(Widget widget in widgets) {
                table.Attach(widget, col, col + 1, row, row + 1,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Expand | AttachOptions.Fill, 0, 0);

                if(++col == maxcols) {
                    col = 0;
                    row++;
                }
            }
        }

        private void LayoutTableDefault(Table table, IList<Widget>widgets)
        {
            ResizeTable(table, current_column_count, widgets);
            RelayoutTable(table, widgets);
        }

        private void RelayoutTables(int columnCount)
        {
            foreach(Table table in cached_tables) {
                //List<Widget> widgets = new List<Widget>(table.Children);
                //widgets.Reverse();
                ResizeTable(table, columnCount, widgets);
                RelayoutTable(table, widgets);
            }
        }

        private int CalculateColumnCount(int availableWidth)
        {
            if(cached_tables.Count < 1 || cached_tables[0].Children.Length < 1) {
                return 0;
            }

            Table table = cached_tables[0];
            Widget widget = table.Children[0];

            cached_widget_width = widget.Allocation.Width;
            cached_table_spacing = (int)table.DefaultColSpacing;

            return (availableWidth + cached_table_spacing) / (cached_widget_width + cached_table_spacing);
        }

        private int RelayoutTablesIfNeeded(int availableWidth, int currentColumnCount)
        {
            int column_count = CalculateColumnCount(availableWidth);

            if(column_count < 1) {
                column_count = 1;
            }

            if(currentColumnCount != column_count) {
                RelayoutTables(column_count);
                return column_count;
            }

            return currentColumnCount;
        }
    }
}
