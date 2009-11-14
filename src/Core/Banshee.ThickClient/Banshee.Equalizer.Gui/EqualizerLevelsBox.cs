//
// EqualizerLevelsBox.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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

using Banshee.Widgets;

namespace Banshee.Equalizer.Gui
{
    public class EqualizerLevelsBox : VBox
    {
        public EqualizerLevelsBox (params string [] levels)
        {
            for (int i = 0; i < levels.Length && i < 3; i++) {
                Label label = CreateLabel (levels[i]);
                switch (i) {
                    case 0:
                        label.Yalign = 0.05f;
                         break;
                    case 1:
                        label.Yalign = 0.5f;
                        break;
                    case 2:
                    default:
                        label.Yalign = 0.95f;
                        break;
                }

                PackStart (label, true, true, 0);
            }
        }

        private Label CreateLabel (string value)
        {
            Label label = new Label ();
            label.Xalign = 1.0f;
            label.Markup = String.Format ("<small>{0}</small>", GLib.Markup.EscapeText (value));
            label.ModifyFg (StateType.Normal, Hyena.Gui.GtkUtilities.ColorBlend (
                Style.Foreground (StateType.Normal), Style.Background (StateType.Normal)));
            label.Show ();
            return label;
        }
    }
}
