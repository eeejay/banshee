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
    public abstract class ArrowButton : ActionButton
    {
        private Gtk.Action action;
        private Button button;
        private ReliefStyle relief;
        private Rectangle arrow_alloc;
        
        protected ArrowButton () : this (null)
        {
        }
        
        protected ArrowButton (Gtk.Action action)
        {
            this.action = action;
            button = new Button ();
            button.FocusOnClick = false;
            button.Entered += delegate { base.Relief = ReliefStyle.Normal; };
            button.Left += delegate { base.Relief = relief; };
            button.StateChanged += delegate { State = button.State; };
            button.Clicked += delegate { OnActivate (); };
            arrow_alloc.Width = 5;
            arrow_alloc.Height = 3;
            TargetButton = button;
            ActionButtonStyle = ActionButtonStyle.IconOnly;
            IconSize = (int)Gtk.IconSize.LargeToolbar;
            Relief = base.Relief;
        }
        
        protected new virtual Gtk.Action Action {
            get { return action ?? ActionGroup.Active; }
        }
        
        public new ReliefStyle Relief {
            get { return relief; }
            set {
                relief = value;
                base.Relief = value;
                button.Relief = value;
            }
        }
        
        protected override void OnRealized ()
        {
            base.OnRealized ();
            button.Parent = this.Parent;
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
        
        protected override bool OnEnterNotifyEvent (EventCrossing evnt)
        {
            button.Relief = ReliefStyle.None;
            return base.OnEnterNotifyEvent (evnt);
        }
        
        protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
        {
            button.Relief = relief;
            return base.OnLeaveNotifyEvent (evnt);
        }
        
        protected override bool OnButtonPressEvent (EventButton evnt)
        {
            ShowMenu ();
            return true;
        }
        
        protected override void OnActivate ()
        {
            Action.Activate ();
        }

    }
}
