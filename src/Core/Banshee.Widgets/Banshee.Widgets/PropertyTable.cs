
/***************************************************************************
 *  PropertyTable.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Gtk;

namespace Banshee.Widgets
{
    public class PropertyTable : Table
    {
        public PropertyTable() : base(1, 1, false)
        {

        }

        public void AddWidget(string key, Widget widget, bool boldLabel)
        {
            uint rows = NRows;

            if(key != null) {
                Label keyLabel = new Label();
                if(boldLabel) {
                    keyLabel.Markup = "<b>" + GLib.Markup.EscapeText(key) + "</b>:";
                } else {
                    keyLabel.Text = key;
                }
                keyLabel.Xalign = 0.0f;

                Attach(keyLabel, 0, 1, rows, rows + 1);
            }

            Attach(widget, 1, 2, rows, rows + 1);
        }

        public void AddWidget(string key, Widget widget)
        {
            AddWidget(key, widget, true);
        }

        public void AddSeparator()
        {
            HSeparator sep = new HSeparator();
            Attach(sep, 0, 2, NRows, NRows + 1);
            sep.HeightRequest = 10;
        }

        public void AddLabel(string key, object value, bool boldLabel)
        {
            if(value == null)
                return;

            Label valLabel = new Label(value.ToString());
            valLabel.Xalign = 0.0f;
            valLabel.UseUnderline = false;
            valLabel.Selectable = true;

            AddWidget(key, valLabel, boldLabel);
        }

        public void AddLabel(string key, object value)
        {
            AddLabel(key, value, true);
        }

        public Entry AddEntry(string key, object value, bool boldLabel)
        {
            Entry valEntry = new Entry();
            valEntry.Text = value == null ? String.Empty : value.ToString();

            AddWidget(key, valEntry, boldLabel);

            return valEntry;
        }

        public Entry AddEntry(string key, object value)
        {
            return AddEntry(key, value, true);
        }
    }
}
