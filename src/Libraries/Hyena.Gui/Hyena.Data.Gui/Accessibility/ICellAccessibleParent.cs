
using System;

namespace Hyena.Data.Gui.Accessibility
{
    public interface ICellAccessibleParent
    {
        Gdk.Rectangle GetCellExtents (ColumnCellAccessible cell, Atk.CoordType coord_type);
        int GetCellIndex (ColumnCellAccessible cell);
        bool IsCellShowing (ColumnCellAccessible cell);
        bool IsCellFocused (ColumnCellAccessible cell);
        bool IsCellSelected (ColumnCellAccessible cell);
    }
}
