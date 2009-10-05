
using System;

namespace Hyena.Data.Gui.Accessibility
{
    public class ColumnCellAccessible: Atk.Object, Atk.ComponentImplementor
    {
        protected ColumnCell cell;
        protected object bound_object;

        public ColumnCellAccessible (object bound_object, ColumnCell cell, ICellAccessibleParent parent)
        {
            Role = Atk.Role.TableCell;
            this.bound_object = bound_object;
            this.cell = cell;
            Parent = (Atk.Object) parent;
        }

        protected override Atk.StateSet OnRefStateSet ()
        {
            Atk.StateSet states = base.OnRefStateSet ();
            states.AddState (Atk.StateType.Transient);
            states.AddState (Atk.StateType.Focusable);
            states.AddState (Atk.StateType.Enabled);
            states.AddState (Atk.StateType.Sensitive);
            states.AddState (Atk.StateType.Visible);

            if (((ICellAccessibleParent)Parent).IsCellShowing (this))
                states.AddState (Atk.StateType.Showing);

            if (((ICellAccessibleParent)Parent).IsCellFocused (this))
                states.AddState (Atk.StateType.Focused);

            if (((ICellAccessibleParent)Parent).IsCellSelected (this))
                states.AddState (Atk.StateType.Selected);

            return states;
        }

        protected override int OnGetIndexInParent ()
        {
            return ((ICellAccessibleParent)Parent).GetCellIndex (this);
        }

        public double Alpha {
            get { return 1.0; }
        }

        public bool SetSize (int w, int h)
        {
            return false;
        }

        public bool SetPosition (int x, int y, Atk.CoordType coordType)
        {
            return false;
        }

        public bool SetExtents (int x, int y, int w, int h, Atk.CoordType coordType)
        {
            return false;
        }

        public void RemoveFocusHandler (uint handlerId)
        {
        }

        public bool GrabFocus ()
        {
            return false;
        }

        public void GetSize (out int w, out int h)
        {
            Gdk.Rectangle rectangle = ((ICellAccessibleParent)Parent).GetCellExtents(this, Atk.CoordType.Screen);
            w = rectangle.Width;
            h = rectangle.Height;
        }

        public void GetPosition (out int x, out int y, Atk.CoordType coordType)
        {
            Gdk.Rectangle rectangle = ((ICellAccessibleParent)Parent).GetCellExtents(this, coordType);

            x = rectangle.X;
            y = rectangle.Y;
        }

        public void GetExtents (out int x, out int y, out int w, out int h, Atk.CoordType coordType)
        {
            Gdk.Rectangle rectangle = ((ICellAccessibleParent)Parent).GetCellExtents(this, coordType);

            x = rectangle.X;
            y = rectangle.Y;
            w = rectangle.Width;
            h = rectangle.Height;
        }

        public virtual Atk.Object RefAccessibleAtPoint (int x, int y, Atk.CoordType coordType)
        {
            return null;
        }

        public bool Contains (int x, int y, Atk.CoordType coordType)
        {
            return false;
        }

        public uint AddFocusHandler (Atk.FocusHandler handler)
        {
            return 0;
        }
    }
}
