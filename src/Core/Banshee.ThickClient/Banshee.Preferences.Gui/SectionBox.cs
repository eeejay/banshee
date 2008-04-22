//
// SectionBox.cs
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
    public class SectionBox : Table
    {
        public SectionBox (Section section) : base (1, 2, false)
        {
            ColumnSpacing = 10;
            RowSpacing = 5;
        
            foreach (PreferenceBase preference in section) {
                Widget widget = WidgetFactory.GetWidget (preference);
                if (widget == null) {
                    continue;
                }
                
                AddWidget (preference, widget);
            }
        }
        
        private void AddWidget (PreferenceBase preference, Widget widget)
        {
            uint start_row = NRows;
            uint start_col = 0;
            
            if (!(widget is CheckButton) && preference.ShowLabel) {
                AttachLabel (preference.Name, start_row);
                start_col++;
            }
            
            widget.Show ();
            Attach (widget, start_col, 2, start_row, start_row + 1, 
                AttachOptions.Expand | AttachOptions.Fill, 
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
        }
        
        private void AttachLabel (string text, uint start_row)
        {
            if (String.IsNullOrEmpty (text)) {
                return;
            }
        
            Label label = new Label (String.Format ("{0}:", text));
            label.UseUnderline = true;
            label.Xalign = 0.0f;
            label.Show ();
            
            Attach (label, 0, 1, start_row, start_row + 1, 
                AttachOptions.Fill, 
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
        }
    }
}
