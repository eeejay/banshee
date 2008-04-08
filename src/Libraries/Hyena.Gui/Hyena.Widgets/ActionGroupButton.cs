//
// ActionGroupButton.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;

using Hyena.Gui;

namespace Hyena.Widgets
{
    public abstract class ActionGroupButton : ActionButton
    {
        protected abstract IRadioActionGroup ActionGroup { get; }
        
        protected ActionGroupButton () : this (null)
        {
        }
        
        protected ActionGroupButton (Toolbar toolbar) : base (toolbar)
        {
            ActionGroup.Changed += delegate { Action = ActionGroup.Active; };
            Action = ActionGroup.Active;
        }
        
        protected void ShowMenu ()
        {
            BuildMenu ().Popup (null, null, PositionMenu, 1, Gtk.Global.CurrentEventTime);
        }
        
        protected override void OnActivated ()
        {
            ShowMenu ();
        }
        
        protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
        {
            ShowMenu ();
            return true;
        }
        
        private Menu BuildMenu ()
        {
            Menu menu = new Menu ();
            foreach (Widget widget in MenuItems) {
                menu.Append (widget);
            }

            menu.ShowAll ();
            return menu;
        }
        
        protected virtual IEnumerable<Widget> MenuItems {
            get {
                foreach (RadioAction action in ActionGroup) {
                    yield return action.CreateMenuItem ();
                }
            }
        }

        private void PositionMenu (Menu menu, out int x, out int y, out bool push_in) 
        {
            Gtk.Requisition menu_req = menu.SizeRequest ();
            int monitor_num = Screen.GetMonitorAtWindow (GdkWindow);
            Gdk.Rectangle monitor = Screen.GetMonitorGeometry (monitor_num < 0 ? 0 : monitor_num);

            GdkWindow.GetOrigin (out x, out y);
            
            y += Allocation.Y;
            x += Allocation.X + (Direction == TextDirection.Ltr
                ? Math.Max (Allocation.Width - menu_req.Width, 0)
                : - (menu_req.Width - Allocation.Width));
            
            if (y + Allocation.Height + menu_req.Height <= monitor.Y + monitor.Height) {
                y += Allocation.Height;
            } else if (y - menu_req.Height >= monitor.Y) {
                y -= menu_req.Height;
            } else if (monitor.Y + monitor.Height - (y + Allocation.Height) > y) {
                y += Allocation.Height;
            } else {
                y -= menu_req.Height;
            }

            push_in = false;
        }
    }
}
