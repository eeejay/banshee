//
// ActionButton.cs
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
    public enum ActionButtonStyle
    {
        IconOnly,
        TextOnly,
        IconAndText
    }
    
    public abstract class ActionButton : Button
    {
        private Button target_button;
        private HBox box = new HBox ();
        private Label label = new Label ();
        private Image image = new Image ();
        private ActionButtonStyle? style;
        
        protected ActionButton ()
        {
            ActionGroup.Changed += OnActionChanged;
            
            Relief = ReliefStyle.None;
            box.Spacing = 4;
            image.IconSize = (int)Gtk.IconSize.Menu;
            label.UseUnderline = true;
            
            TargetButton = this;
            ActionButtonStyle = ActionButtonStyle.IconAndText;
            
            SetActiveItem (ActionGroup.Active);
        }
        
        protected abstract IRadioActionGroup ActionGroup { get; }
        
        private void OnActionChanged (object o, ChangedArgs args)
        {
            SetActiveItem (args.Current);
        }
        
        private void SetActiveItem (RadioAction action)
        {
            if (action == null) {
                return;
            }

            image.IconName = action.IconName;
            label.TextWithMnemonic = action.Label;
            target_button.Sensitive = action.Sensitive && action.Visible;
        }
        
        protected void ShowMenu ()
        {
            BuildMenu ().Popup (null, null, PositionMenu, 1, Gtk.Global.CurrentEventTime);
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
            int monitor_num = Screen.GetMonitorAtWindow (target_button.GdkWindow);
            Gdk.Rectangle monitor = Screen.GetMonitorGeometry (monitor_num < 0 ? 0 : monitor_num);

            target_button.GdkWindow.GetOrigin (out x, out y);
            
            y += target_button.Allocation.Y;
            x += target_button.Allocation.X + (Direction == TextDirection.Ltr
                ? Math.Max (target_button.Allocation.Width - menu_req.Width, 0)
                : - (menu_req.Width - target_button.Allocation.Width));
            
            if (y + target_button.Allocation.Height + menu_req.Height <= monitor.Y + monitor.Height) {
                y += target_button.Allocation.Height;
            } else if (y - menu_req.Height >= monitor.Y) {
                y -= menu_req.Height;
            } else if (monitor.Y + monitor.Height - (y + target_button.Allocation.Height) > y) {
                y += target_button.Allocation.Height;
            } else {
                y -= menu_req.Height;
            }

            push_in = false;
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
        
        public Button TargetButton {
            get { return target_button; }
            protected set {
                if (target_button != null) {
                    target_button.Remove (box);
                }
                target_button = value;
                target_button.Add (box);
            }
        }
        
        protected int IconSize {
            get { return image.IconSize; }
            set { image.IconSize = value; }
        }
        
        protected ActionButtonStyle ActionButtonStyle {
            get { return style ?? ActionButtonStyle.IconAndText; }
            set {
                if (style != null) {
                    if (style.Value != ActionButtonStyle.TextOnly) {
                        box.Remove (image);
                    }
                    if (style.Value != ActionButtonStyle.IconOnly) {
                        box.Remove (label);
                    }
                }
                
                style = value;
                
                if (style.Value != ActionButtonStyle.TextOnly) {
                    box.PackStart (image, false, false, 0);
                }
                if (style.Value != ActionButtonStyle.IconOnly) {
                    box.PackStart (label, true, true, 0);
                }
                box.ShowAll ();
            }
        }
    }
}
