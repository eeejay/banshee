//
// AddinTile.cs
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
using Mono.Addins;

using Hyena.Widgets;

namespace Banshee.Gui.Widgets
{    
    public class AddinTile : Table
    {
        private Addin addin;
        private Button activate_button;
        
        public AddinTile (Addin addin) : base (2, 3, false)
        {
            this.addin = addin;
            BuildTile ();
        }
        
        private void BuildTile ()
        {
            BorderWidth = 5;
            RowSpacing = 1;
            ColumnSpacing = 5;
            
            Label name = new Label ();
            name.Show ();
            name.Xalign = 0.0f;
            name.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (addin.Name));
            
            Attach (name, 1, 3, 0, 1, 
                AttachOptions.Expand | AttachOptions.Fill, 
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
            
            WrapLabel desc = new WrapLabel ();
            desc.Show ();
            desc.Markup = String.Format ("<small>{0}</small>", GLib.Markup.EscapeText (addin.Description.Description));
            
            Attach (desc, 1, 2, 1, 2,
                AttachOptions.Expand | AttachOptions.Fill, 
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
                
            HBox box = new HBox ();
            box.Show ();
            activate_button = new Button ("Disable");
            box.PackEnd (activate_button, false, false, 0);
            Attach (box, 2, 3, 1, 2, AttachOptions.Shrink, AttachOptions.Expand, 0, 0);
                
            Show ();
        }
        
        protected override void OnRealized ()
        {
            WidgetFlags |= WidgetFlags.NoWindow;
            GdkWindow = Parent.GdkWindow;
            base.OnRealized ();
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (State == StateType.Selected) {
                Gtk.Style.PaintFlatBox (Style, evnt.Window, State, ShadowType.None, evnt.Area, 
                    this, "row", Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
            }
            
            return base.OnExposeEvent (evnt);
        }
        
        public void Select (bool select)
        {
            State = select ? StateType.Selected : StateType.Normal;
            activate_button.Visible = select;
            activate_button.State = StateType.Normal;
            QueueResize ();
        }
    }
}
