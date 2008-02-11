//
// LinkButton.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
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

using Lastfm;

namespace Lastfm.Gui
{
    public class LinkButton : Gtk.Button
    {
        private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
        private static Gdk.Color link_color = new Gdk.Color(0, 0, 0xff);
            
        static LinkButton()
        {
            Gdk.Colormap.System.AllocColor(ref link_color, true, true);
        }
     
        private Label label;
        private string label_text;
     
        public LinkButton(string text)
        {
            label = new Label();
            label.ModifyFg(Gtk.StateType.Normal, link_color);
            label.Show();
            
            Text = text;
            
            Add(label);
            Relief = ReliefStyle.None;
        }
        
        protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
        {
            GdkWindow.Cursor = hand_cursor;
            return base.OnEnterNotifyEvent(evnt);
        }

        protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
        {
            GdkWindow.Cursor = null;
            return base.OnLeaveNotifyEvent(evnt);
        }
        
        public string Text {
            get { return label_text; }
            set {
                label_text = value;
                label.Markup = String.Format("<u>{0}</u>", GLib.Markup.EscapeText(value));
            }
        }
    }
}
