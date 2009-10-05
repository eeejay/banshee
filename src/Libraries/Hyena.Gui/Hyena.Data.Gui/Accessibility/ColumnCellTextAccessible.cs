
using System;

namespace Hyena.Data.Gui.Accessibility
{
    class ColumnCellTextAccessible : ColumnCellAccessible
    {
        public ColumnCellTextAccessible (object bound_object, ColumnCellText cell, ICellAccessibleParent parent): base (bound_object, cell as ColumnCell, parent)
        {
            Name = cell.GetTextAlternative (bound_object);
        }
    }
}
