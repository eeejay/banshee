//
// ConnectedRepeatComboBox.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Gtk;

using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Gui.Widgets
{
    public class RepeatActionButton : Button
    {
        private HBox box = new HBox ();
        private Image image = new Image ();
        private Label label = new Label ();

        public RepeatActionButton () : base ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            service.PlaybackActions.RepeatActions.Changed += OnActionChanged;

            Relief = ReliefStyle.None;
            
            label.UseUnderline = true;
            image.IconSize = (int)IconSize.Menu;

            box.Spacing = 4;
            box.PackStart (image, false, false, 0);
            box.PackStart (label, true, true, 0);
            box.ShowAll ();
            Add (box);
            
            SetActiveItem (service.PlaybackActions.RepeatActions.Active);
        }

        private void OnActionChanged (object o, ChangedArgs args)
        {
            SetActiveItem ((RadioAction)args.Current);
        }

        private void SetActiveItem (RadioAction action)
        {
            if (action == null) {
                return;
            }

            image.IconName = action.IconName;
            label.TextWithMnemonic = action.Label;
            box.Sensitive = action.Sensitive && action.Visible;
        }

        protected override void OnActivated ()
        {
            BuildMenu ().Popup (null, null, PositionMenu, 1, Gtk.Global.CurrentEventTime);
        }

        protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
        {
            if (evnt.Button == 1 || evnt.Button == 3) {
                BuildMenu ().Popup (null, null, PositionMenu, 1, evnt.Time);
            }

            return true;
        }

        private Menu BuildMenu ()
        {
            Menu menu = new Menu ();
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            foreach (RadioAction action in service.PlaybackActions.RepeatActions) {
                menu.Append (action.CreateMenuItem ());
            }

            menu.ShowAll ();
            return menu;
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
