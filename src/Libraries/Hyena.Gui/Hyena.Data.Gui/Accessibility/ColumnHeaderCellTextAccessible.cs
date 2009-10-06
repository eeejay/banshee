
using System;
using Mono.Unix;

namespace Hyena.Data.Gui.Accessibility
{
    class ColumnHeaderCellTextAccessible: ColumnCellTextAccessible, Atk.ActionImplementor
    {
        private static string [] action_descriptions  = new string[] {"", Catalog.GetString ("open context menu")};
        private static string [] action_names_localized = new string[] {Catalog.GetString ("click"), Catalog.GetString ("menu")};

        private enum Actions {
            Click,
            Menu,
            Last
        };

        public ColumnHeaderCellTextAccessible (object bound_object, ColumnHeaderCellText cell, ICellAccessibleParent parent)
            : base (bound_object, cell as ColumnCellText, parent)
        {
            Role = Atk.Role.TableColumnHeader;
        }

        protected override Atk.StateSet OnRefStateSet ()
        {
            Atk.StateSet states = base.OnRefStateSet ();
            states.RemoveState (Atk.StateType.Selectable);
            states.RemoveState (Atk.StateType.Transient);
            return states;
        }

        public string GetLocalizedName (int action)
        {
            if (action >= action_names_localized.Length)
                return "";

            return action_names_localized[action];
        }

        public string GetName (int action)
        {
            if (action >= (int)Actions.Last)
                return "";

            return ((Actions)action).ToString ().ToLower ();
        }

        public string GetDescription (int action)
        {
            if (action >= action_descriptions.Length)
                return "";

            return action_descriptions[action];
        }

        public string GetKeybinding (int action)
        {
            return "";
        }

        public int NActions {
            get { return (int)Actions.Last; }
        }

        public bool DoAction (int action)
        {
            ICellAccessibleParent parent = (ICellAccessibleParent)Parent;
            switch ((Actions)action) {
                case Actions.Menu: parent.InvokeColumnHeaderMenu (this); break;
                case Actions.Click: parent.ClickColumnHeader (this); break;
            }

            if (action == (int)Actions.Menu) {
                ((ICellAccessibleParent)Parent).InvokeColumnHeaderMenu (this);
            }

            return true;
        }

        public bool SetDescription (int action, string description)
        {
            return false;
        }
    }
}
