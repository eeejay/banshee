//
// CustomActionProxy.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;
using Gtk;
using Mono.Unix;

using Hyena.Widgets;

namespace Banshee.Widgets
{
    public abstract class CustomActionProxy
    {
        protected Gtk.Action action;
        protected string [] paths;
        protected UIManager ui;

        private struct ActionMenuPath {
            public ActionMenuPath (string menu, string item)
            {
                MenuPath = menu;
                AfterMenuItem = item;
            }

            public string MenuPath;
            public string AfterMenuItem;
        }

        private Dictionary<string, ActionMenuPath> uninitialized_actions = new Dictionary<string, ActionMenuPath> ();

#region Public API

        public CustomActionProxy (UIManager ui, Gtk.Action proxiedAction)
        {
            this.action = proxiedAction;
            this.ui = ui;
        }

        public void AddPath (string menuPath)
        {
            AddPath (menuPath, null);
        }

        public void AddPath (string menuPath, string menuItemPath)
        {
            ActionMenuPath path = new ActionMenuPath (menuPath, menuItemPath);
            uninitialized_actions.Add (ui.GetAction (menuPath).Name, path);
            if (uninitialized_actions.Count == 1)
                ui.PreActivate += HandlePreActivate;
        }

#endregion

        private void HandlePreActivate (object sender, PreActivateArgs args)
        {
            if (uninitialized_actions.ContainsKey (args.Action.Name)) {
                ActionMenuPath path = uninitialized_actions[args.Action.Name];

                Widget item = (path.AfterMenuItem != null)
                    ? ui.GetWidget (String.Format ("{0}/{1}", path.MenuPath, path.AfterMenuItem))
                    : null;

                InsertProxy (args.Action, ui.GetWidget (path.MenuPath), item);

                uninitialized_actions.Remove (args.Action.Name);
                if (uninitialized_actions.Count == 0)
                    ui.PreActivate -= HandlePreActivate;
            }
        }

        protected virtual void InsertProxy (Gtk.Action menuAction, Widget parent, Widget afterItem)
        {
            int position = 0;
            Widget item = null;
            if (parent is MenuItem || parent is Menu) {
                Menu parent_menu = ((parent is MenuItem) ? (parent as MenuItem).Submenu : parent) as Menu;
                position = (afterItem != null) ? Array.IndexOf (parent_menu.Children, afterItem as MenuItem) + 1 : 0;
                item = GetNewMenuItem ();
                if (item != null) {
                    parent_menu.Insert (item, position);
                }
            } else if (parent is Toolbar) {
                ToolItem after_item = afterItem as ToolItem;
                if (after_item != null) {
                    position = (parent as Toolbar).GetItemIndex (after_item);
                    ToolItem tool_item = GetNewToolItem ();
                    if (tool_item != null) {
                        (parent as Toolbar).Insert (tool_item, position);
                        item = tool_item;
                    }
                }
            }

            if (item != null) {
                action.ConnectProxy (item);
            }
        }

        protected virtual ComplexMenuItem GetNewMenuItem ()
        {
            return null;
        }

        protected virtual ToolItem GetNewToolItem () {
            return null;
        }
    }
}
