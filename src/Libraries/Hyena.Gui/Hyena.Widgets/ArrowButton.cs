//
// ArrowButton.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com>
//
// Copyright (C) 2008 Scott Peterson
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
using Gdk;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Widgets
{
    public abstract class ArrowButton : ActionGroupButton
    {
        private Gtk.Action action;
        private ActionButton button;
        private ReliefStyle relief;
        private Rectangle arrow_alloc;
        
        protected ArrowButton () : this (null, null)
        {
        }
        
        protected ArrowButton (Gtk.Action action) : this (action, null)
        {
        }
        
        protected ArrowButton (Toolbar toolbar) : this (null, toolbar)
        {
        }
        
        protected ArrowButton (Gtk.Action action, Toolbar toolbar)
        {
            base.ActionButtonStyle = ActionButtonStyle.None;
            
            this.action = action;
            
            arrow_alloc.Width = 5;
            arrow_alloc.Height = 3;
            
            button = new ActionButton (action);
            button.Entered += delegate { base.Relief = ReliefStyle.Normal; };
            button.Left += delegate { base.Relief = relief;};
            button.StateChanged += delegate { State = button.State; };
            
            ActionGroup.Changed += delegate { button.Action = this.action ?? ActionGroup.Active; };
            Toolbar = toolbar;
        }
        
        public new Gtk.Action Action {
            get { return button.Action; }
            set {
                action = value;
                button.Action = action;
            }
        }
        
        public override ActionButtonStyle ActionButtonStyle {
            get { return button == null ? base.ActionButtonStyle : button.ActionButtonStyle; }
            set {
                if (button == null) {
                    base.ActionButtonStyle = value;
                } else {
                    button.ActionButtonStyle = value;
                }
            }
        }
        
        public override Toolbar Toolbar {
            get { return button.Toolbar; }
            set {
                if (button == null) {
                    return;
                }
                button.Toolbar = value;
                if (value != null) {
                    Relief = value.ReliefStyle;
                }
            }
        }
        
        public new ReliefStyle Relief {
            get { return relief; }
            set {
                relief = value;
                base.Relief = value;
                button.Relief = value;
            }
        }
        
        public override IconSize IconSize {
            get { return button.IconSize; }
            set { button.IconSize = value; }
        }
        
        protected override void OnRealized ()
        {
            base.OnRealized ();
            button.Parent = Parent;
            button.Realize ();
        }
        
        protected override void OnUnrealized ()
        {
            button.Unrealize ();
            base.OnUnrealized ();
        }
        
        protected override void OnMapped ()
        {
            base.OnMapped ();
            button.ShowAll ();
        }

        protected override void OnUnmapped ()
        {
            button.Hide ();
            base.OnUnmapped ();
        }
        
        protected override void OnSizeRequested (ref Requisition requisition)
        {
            requisition = button.SizeRequest ();
            requisition.Width += 13;
        }
        
        protected override void OnSizeAllocated (Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);
            allocation.Width -= 13;
            arrow_alloc.X = allocation.Right + 3;
            arrow_alloc.Y = allocation.Top + allocation.Height / 2;
            button.SizeAllocate (allocation);
        }

        protected override bool OnExposeEvent (EventExpose evnt)
        {
            base.OnExposeEvent (evnt);
            button.SendExpose (evnt);
            Gtk.Style.PaintArrow (Style, GdkWindow, State, ShadowType.None, arrow_alloc, this, null,
                ArrowType.Down, true, arrow_alloc.X, arrow_alloc.Y, arrow_alloc.Width, arrow_alloc.Height);
            return true;
        }
        
        protected override void OnEntered ()
        {
            button.Relief = ReliefStyle.None;
            base.OnEntered ();
        }
        
        protected override void OnLeft ()
        {
            button.Relief = relief;
            base.OnLeft ();
        }
    }
}
