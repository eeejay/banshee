//
// PreferencePage.cs
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

using Banshee.Preferences;

namespace Banshee.Preferences.Gui
{
    public class NotebookPage : VBox
    {
        private Page page;
        public Page Page {
            get { return page; }
        }
        
        private Label tab_widget;
        public Widget TabWidget {
            get { return tab_widget; }
        }
        
        public NotebookPage (Page page)
        {
            this.page = page;
            
            BorderWidth = 5;
            Spacing = 10;
            
            tab_widget = new Label (page.Name);
            tab_widget.Show ();
            
            foreach (Section section in page) {
                AddSection (section);
            }
        }
        
        private void AddSection (Section section)
        {
            Frame frame = null;
            
            if (section.ShowLabel) {
                frame = new Frame ();
                Label label = new Label ();
                label.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (section.Name));
                label.UseUnderline = true;
                label.Show ();
                frame.LabelWidget = label;
                frame.LabelXalign = 0.0f;
                frame.LabelYalign = 0.5f;
                frame.Shadow = ShadowType.None;
                frame.Show ();
                PackStart (frame, false, false, 0);
            } 
            
            Alignment alignment = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
            alignment.TopPadding = (uint)(frame == null ? 0 : 5);
            alignment.LeftPadding = 12;
            alignment.Show ();
            
            if (frame != null) {
                frame.Add (alignment);
            } else {
                PackStart (alignment, false, false, 0);
            }
            
            SectionBox box = new SectionBox (section);
            box.Show ();
            
            alignment.Add (box);
        }
    }
}
