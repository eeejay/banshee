//
// ListView.cs
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

namespace Hyena.Data.Gui
{
    public partial class ListView<T> : ListViewBase, IListView<T>
    {
        public ListView ()
        {
            column_layout = new Pango.Layout (PangoContext);
            CanFocus = true;
            selection_proxy.Changed += delegate { InvalidateList (); };

            // TODO this is working well except a crasher bug in Gtk+ or Gtk#
            // See http://bugzilla.gnome.org/show_bug.cgi?id=524772
            //HasTooltip = true;
            //QueryTooltip += OnQueryTooltip;
        }

        /*private void OnQueryTooltip (object o, Gtk.QueryTooltipArgs args)
        {
            if (cell_context == null || cell_context.Layout == null) {
                return;
            }
            
            if (!args.KeyboardTooltip) {
                ColumnCellText cell;
                Column column;
                int row_index;
                
                if (GetEventCell<ColumnCellText> (args.X, args.Y, out cell, out column, out row_index)) {
                    CachedColumn cached_column = GetCachedColumnForColumn (column);
                    cell.UpdateText (cell_context, cached_column.Width);
                    if (cell.IsEllipsized) {
                        Gdk.Rectangle rect = new Gdk.Rectangle ();
                        rect.X = list_interaction_alloc.X + cached_column.X1;

                        // get the y of the event in list coords
                        rect.Y = args.Y - list_interaction_alloc.Y;

                        // get the top of the cell pointed to by list_y
                        rect.Y -= VadjustmentValue % RowHeight;
                        rect.Y -= rect.Y % RowHeight;

                        // convert back to widget coords
                        rect.Y += list_interaction_alloc.Y;

                        rect.Width = cached_column.Width; // TODO is this right even if the list is wide enough to scroll horizontally?
                        rect.Height = RowHeight; // TODO not right - could be smaller if at the top/bottom and only partially showing
                        
                        //System.Console.WriteLine ("Got ellipsized column text: {0} at {1};  event was at {3}, {4}", cell.Text, rect, rect.Y, args.X, args.Y);
                        args.Tooltip.Markup = GLib.Markup.EscapeText (cell.Text);
                        args.Tooltip.TipArea = rect;
                        args.RetVal = true;
                    }
                }
            }
        }*/
    }
}
