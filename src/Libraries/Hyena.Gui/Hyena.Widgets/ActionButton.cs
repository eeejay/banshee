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
using Gtk;

using Hyena.Gui;

namespace Hyena.Widgets
{
    public enum ActionButtonStyle
    {
        Icon,
        Text,
        Both,
        BothHoriz,
        None
    }
    
    public class ActionButton : Button
    {
        private Gtk.Action action;
        private Label label = new Label ();
        private Image image = new Image ();
        private HBox hbox = new HBox ();
        private VBox vbox = new VBox ();
        private Box box;
        private Toolbar toolbar;
        private ActionButtonStyle style;
        
        public ActionButton () : this (null, null)
        {
        }
        
        public ActionButton (Toolbar toolbar) : this (null, toolbar)
        {
        }
        
        public ActionButton (Gtk.Action action) : this (action, null)
        {
        }
        
        public ActionButton (Gtk.Action action, Toolbar toolbar)
        {
            FocusOnClick = false;
            ActionButtonStyle = ActionButtonStyle.Icon;
            
            Action = action;
            Toolbar = toolbar;
            
            label.UseUnderline = true;
            hbox.Spacing = 4;
            vbox.Spacing = 4;
        }
        
        public virtual ActionButtonStyle ActionButtonStyle {
            get { return style; }
            set {
                if (box != null) {
                    if (style != ActionButtonStyle.Text) {
                        box.Remove (image);
                    }
                    if (style != ActionButtonStyle.Icon) {
                        box.Remove (label);
                    }
                    Remove (box);
                }
                
                style = value;
                
                if (style == ActionButtonStyle.None) {
                    box = null;
                    return;
                }
                
                box = style == ActionButtonStyle.BothHoriz ? (Box)hbox : vbox;
                
                if (style != ActionButtonStyle.Text) {
                    box.PackStart (image, false, false, 0);
                }
                if (style != ActionButtonStyle.Icon) {
                    box.PackStart (label, true, true, 0);
                }
                
                Add (box);
                box.ShowAll ();
            }
        }
        
        public virtual new Gtk.Action Action {
            get { return action; }
            set {
                action = value;
                if (action == null) {
                    if (image.IconName != null) {
                        image.IconName = null;
                    }
                    if (image.Stock != null) {
                        image.Stock = null;
                    }
                    label.TextWithMnemonic = String.Empty;
                    Sensitive = false;
                } else {
                    if (!String.IsNullOrEmpty (action.IconName)) {
                        image.IconName = action.IconName;
                    } else if (!String.IsNullOrEmpty (action.StockId)) {
                        image.Stock = action.StockId;
                    }
                    label.TextWithMnemonic = action.Label;
                    Sensitive = action.Sensitive && action.Visible;
                }
            }
        }
        
        public virtual Toolbar Toolbar {
            get { return toolbar; }
            set {
                if (toolbar != null) {
                    toolbar.StyleChanged -= OnToolbarStyleChanged;
                }
                
                toolbar = value;
                
                if (toolbar != null) {
                    toolbar.StyleChanged += OnToolbarStyleChanged;
                    Relief = toolbar.ReliefStyle;
                    OnToolbarStyleChanged (null, null);
                }
            }
        }
        
        public virtual IconSize IconSize {
            get { return (IconSize)image.IconSize; }
            set { image.IconSize = (int)value; }
        }
        
        protected override void OnActivated ()
        {
            action.Activate ();
        }
        
        protected override void OnClicked ()
        {
            action.Activate ();
        }
        
        private void OnToolbarStyleChanged (object o, StyleChangedArgs args)
        {
            switch (toolbar.ToolbarStyle) {
            case ToolbarStyle.Icons:
                ActionButtonStyle = ActionButtonStyle.Icon;
                break;
            case ToolbarStyle.Text:
                ActionButtonStyle = ActionButtonStyle.Text;
                break;
            case ToolbarStyle.Both:
                ActionButtonStyle = ActionButtonStyle.Both;
                break;
            case ToolbarStyle.BothHoriz:
                ActionButtonStyle = ActionButtonStyle.BothHoriz;
                break;
            }
        }
    }
}
