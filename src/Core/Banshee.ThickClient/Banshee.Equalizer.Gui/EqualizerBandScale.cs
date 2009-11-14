//
// EqualizerBandScale.cs
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

namespace Banshee.Equalizer.Gui
{
    public class EqualizerBandScale : HBox
    {
        private uint band;
        private Scale scale;
        private Label label;
        private object tooltip_host;

        public event EventHandler ValueChanged;

        public EqualizerBandScale (uint band, int median, int min, int max, string labelText)
        {
            this.band = band;

            label = new Label ();
            label.Markup = String.Format ("<small>{0}</small>", GLib.Markup.EscapeText (labelText));
            label.Xalign = 0.0f;
            label.Yalign = 1.0f;
            label.Angle = 90.0;

            // new Adjustment (value, lower, upper, step_incr, page_incr, page_size);
            scale = new VScale (new Adjustment (median, min, max, max / 10, max / 10, 1));
            scale.DrawValue = false;
            scale.Inverted = true;
            scale.ValueChanged += OnValueChanged;

            scale.Show ();
            label.Show ();

            PackStart (scale, false, false, 0);
            PackStart (label, false, false, 0);

            tooltip_host = Hyena.Gui.TooltipSetter.CreateHost ();
        }

        private void OnValueChanged (object o, EventArgs args)
        {
            EventHandler handler = ValueChanged;
            if(handler != null) {
                handler(this, new EventArgs ());
            }

            Hyena.Gui.TooltipSetter.Set (tooltip_host, scale, ((int)Math.Round (scale.Value / 10.0)).ToString ());
        }

        public int Value {
            get { return (int)Math.Round (scale.Value); }
            set { scale.Value = (double) value; }
        }

        public bool LabelVisible {
            get { return label.Visible; }
            set { label.Visible = value; }
        }

        public uint Band {
            get { return band; }
        }
    }
}
