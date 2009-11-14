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

using Hyena.Gui;
using Banshee.Preferences;

namespace Banshee.Preferences.Gui
{
    public class SectionBox : Table
    {
        private object tp_host;

        public SectionBox (Section section) : base (1, 2, false)
        {
            ColumnSpacing = 10;
            RowSpacing = 5;

            foreach (PreferenceBase preference in section) {
                Widget widget = WidgetFactory.GetWidget (preference);
                if (widget == null) {
                    continue;
                }

                AddWidget (preference, widget, WidgetFactory.GetMnemonicWidget (preference));
            }
        }

        private void AddWidget (PreferenceBase preference, Widget widget, Widget mnemonic_widget)
        {
            uint start_row = NRows;
            uint start_col = 0;

            Label label = null;

            if (!(widget is CheckButton) && preference.ShowLabel) {
                label = AttachLabel (preference.Name, start_row);
                start_col++;
            }

            widget.Show ();
            Attach (widget, start_col, 2, start_row, start_row + 1,
                AttachOptions.Expand | AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            if (label != null) {
                label.MnemonicWidget = mnemonic_widget ?? widget;
            }

            if (!String.IsNullOrEmpty (preference.Description)) {
                if (tp_host == null) {
                     tp_host = TooltipSetter.CreateHost ();
                }

                TooltipSetter.Set (tp_host, widget, preference.Description);
                if (label != null) {
                    TooltipSetter.Set (tp_host, label, preference.Description);
                }
            }
        }

        private Label AttachLabel (string text, uint start_row)
        {
            if (String.IsNullOrEmpty (text)) {
                return null;
            }

            Label label = new Label (String.Format ("{0}:", text));
            label.UseUnderline = true;
            label.Xalign = 0.0f;
            label.Show ();

            Attach (label, 0, 1, start_row, start_row + 1,
                AttachOptions.Fill,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);

            return label;
        }
    }
}
