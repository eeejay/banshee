
using System;
using Mono.Unix;

namespace Hyena.Data.Gui.Accessibility
{
    class ColumnHeaderCellTextAccessible: ColumnCellTextAccessible, Atk.ActionImplementor
    {
        private static string[] ActionDescriptions  = new string[] {"", Catalog.GetString("open context menu")};
        private static string[] ActionNamesLocalized = new string[] {Catalog.GetString("click"), Catalog.GetString("menu")};

        private enum Actions
        {
            CLICK,
            MENU,
            LAST
        };

        public ColumnHeaderCellTextAccessible (object bound_object, ColumnHeaderCellText cell, ICellAccessibleParent parent): base (bound_object, cell as ColumnCellText, parent)
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
            if (action >= ActionNamesLocalized.Length)
                return "";

            return ActionNamesLocalized[action];
        }

        public string GetName (int action)
        {
            if (action >= (int)Actions.LAST)
                return "";

            return ((Actions)action).ToString().ToLower();
        }

        public string GetDescription (int action)
        {
            if (action >= ActionDescriptions.Length)
                return "";

            return ActionDescriptions[action];
        }

        public string GetKeybinding (int action)
        {
            return "";
        }

        public int NActions
        {
            get {
                return (int)Actions.LAST;
            }
        }

        public bool DoAction (int action)
        {
            ICellAccessibleParent parent = (ICellAccessibleParent)Parent;
            switch ((Actions)action)
            {
            case Actions.MENU: parent.InvokeColumnHeaderMenu (this); break;
            case Actions.CLICK: parent.ClickColumnHeader (this); break;
            }
            if (action == (int)Actions.MENU)
                ((ICellAccessibleParent)Parent).InvokeColumnHeaderMenu(this);
            return true;
        }

        public bool SetDescription(int action, string description)
        {
            return false;
        }
    }
}
