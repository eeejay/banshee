
using System;
using System.Linq;
using System.Collections.Generic;

using Hyena.Data.Gui;

namespace Hyena.Data.Gui.Accessibility
{
    public partial class ListViewAccessible<T> : Hyena.Gui.BaseWidgetAccessible,
             ICellAccessibleParent
    {
        private ListView<T> list_view;
        private Dictionary<int, ColumnCellAccessible> cell_cache;

        public ListViewAccessible (GLib.Object widget): base (widget as Gtk.Widget)
        {
            list_view = widget as ListView<T>;
            // TODO replace with list_view.Name?
            Name = "ListView";
            Description = "ListView";
            Role = Atk.Role.Table;
            Parent = list_view.Parent.RefAccessible ();

            cell_cache = new Dictionary<int, ColumnCellAccessible> ();

            list_view.ModelChanged += (o, a) => OnModelChanged ();
            list_view.Model.Reloaded += (o, a) => OnModelChanged ();
            OnModelChanged ();

            list_view.Selection.FocusChanged += OnSelectionFocusChanged;
            list_view.ActiveColumnChanged += OnSelectionFocusChanged;

            ListViewAccessible_Selection ();
            ListViewAccessible_Table ();
        }

        protected override Atk.StateSet OnRefStateSet ()
        {
            Atk.StateSet states = base.OnRefStateSet ();
            states.AddState (Atk.StateType.ManagesDescendants);

            return states;
        }


        protected override int OnGetIndexInParent ()
        {
            for (int i=0; i < Parent.NAccessibleChildren; i++)
                if (Parent.RefAccessibleChild (i) == this)
                    return i;

            return -1;
        }

        protected override int OnGetNChildren ()
        {
            return n_columns * n_rows + n_columns;
        }

        protected override Atk.Object OnRefChild (int index)
        {
            ColumnCellAccessible child;

            if (cell_cache.ContainsKey (index))
            {
                return cell_cache[index];
            }
            if (0 > index - n_columns)
            {
                child = (ColumnCellAccessible) list_view.ColumnController.Where (c => c.Visible).ElementAtOrDefault (index).HeaderCell.GetAccessible (this);
            }
            else
            {
                int column = (index - n_columns)%n_columns;
                int row = (index - n_columns)/n_columns;
                var cell = list_view.ColumnController.Where (c => c.Visible).ElementAtOrDefault (column).GetCell (0);
                cell.BindListItem (list_view.Model[row]);
                child = (ColumnCellAccessible) cell.GetAccessible (this);
            }

            cell_cache.Add (index, child);

            return child;
        }

        public override Atk.Object RefAccessibleAtPoint (int x, int y, Atk.CoordType coordType)
        {
            int row, col;
            list_view.GetCellAtPoint (x, y, coordType, out row, out col);
            return RefAt (row, col);
        }

        private void OnModelChanged ()
        {
            GLib.Signal.Emit (this, "model_changed");
            cell_cache.Clear ();
            /*var handler = ModelChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }*/
        }

        private void OnSelectionFocusChanged (object o, EventArgs a)
        {
            Atk.Object cell;

            if (list_view.HeaderFocused)
                cell = OnRefChild (list_view.ActiveColumn);
            else
                cell = RefAt (list_view.Selection.FocusedIndex, list_view.ActiveColumn);

            GLib.Signal.Emit (this, "active-descendant-changed", cell.Handle);
        }

        private int n_columns {
            get { return list_view.ColumnController.Count (c => c.Visible); }
            set {}
        }

        private int n_rows {
            get { return list_view.Model.Count; }
            set {}
        }

        # region ICellAccessibleParent

        public int GetCellIndex (ColumnCellAccessible cell)
        {
            foreach (KeyValuePair<int, ColumnCellAccessible> kv in cell_cache)
            {
                if ((ColumnCellAccessible)kv.Value == cell)
                    return (int)kv.Key;
            }

            return -1;
        }

        public Gdk.Rectangle GetCellExtents (ColumnCellAccessible cell, Atk.CoordType coord_type)
        {
            int cache_index = GetCellIndex (cell);
            int minval = Int32.MinValue;
            if (cache_index == -1)
                return new Gdk.Rectangle (minval, minval, minval, minval);

            if (cache_index - n_columns >= 0)
            {
                int column = (cache_index - NColumns)%NColumns;
                int row = (cache_index - NColumns)/NColumns;
                return list_view.GetColumnCellExtents (row, column, true, coord_type);
            } else
            {
                return list_view.GetColumnHeaderCellExtents (cache_index, true, coord_type);
            }
        }

        public bool IsCellShowing (ColumnCellAccessible cell)
        {
            Gdk.Rectangle cell_extents = GetCellExtents (cell, Atk.CoordType.Window);

            if (cell_extents.X == Int32.MinValue && cell_extents.Y == Int32.MinValue)
                return false;

            return true;
        }

        public bool IsCellFocused (ColumnCellAccessible cell)
        {
            int cell_index = GetCellIndex (cell);
            if (cell_index%NColumns != 0)
                return false; // Only 0 column cells get focus now.

            int row = cell_index/NColumns;

            return row == list_view.Selection.FocusedIndex;
        }

        public bool IsCellSelected (ColumnCellAccessible cell)
        {
            int cell_index = GetCellIndex (cell);
            return IsChildSelected (cell_index);
        }

        public void InvokeColumnHeaderMenu (ColumnCellAccessible cell)
        {
            list_view.InvokeColumnHeaderMenu (GetCellIndex (cell));
        }

        public void ClickColumnHeader (ColumnCellAccessible cell)
        {
            list_view.ClickColumnHeader (GetCellIndex (cell));
        }

        # endregion ICellAccessibleParent

    }
}
