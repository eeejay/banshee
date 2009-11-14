//
// ArtworkPopup.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using Gdk;

namespace Banshee.Gui.Widgets
{
    public class ArtworkPopup : Gtk.Window
    {
        private Gtk.Image image;
        private Label label;

        public ArtworkPopup() : base(Gtk.WindowType.Popup)
        {
            VBox vbox = new VBox();
            Add(vbox);

            Decorated = false;
            BorderWidth = 6;

            SetPosition(WindowPosition.CenterAlways);

            image = new Gtk.Image();
            label = new Label(String.Empty);
            label.CanFocus = false;
            label.Wrap = true;

            label.ModifyBg(StateType.Normal, new Color(0, 0, 0));
            label.ModifyFg(StateType.Normal, new Color(160, 160, 160));
            ModifyBg(StateType.Normal, new Color(0, 0, 0));
            ModifyFg(StateType.Normal, new Color(160, 160, 160));

            vbox.PackStart(image, true, true, 0);
            vbox.PackStart(label, false, false, 0);

            vbox.Spacing = 6;
            vbox.ShowAll();
        }

        public Pixbuf Image {
            set {
                int width = value.Width, height = value.Height;

                if(height >= Screen.Height * 0.75) {
                    width = (int)(width * ((Screen.Height * 0.75) / height));
                    height = (int)(Screen.Height * 0.75);
                }

                if(width >= Screen.Width * 0.75) {
                    height = (int)(height * ((Screen.Width * 0.75) / width));
                    width = (int)(Screen.Width * 0.75);
                }

                if(width != value.Width || height != value.Height) {
                    image.Pixbuf = value.ScaleSimple(width, height, InterpType.Bilinear);
                } else {
                    image.Pixbuf = value;
                }

                image.SetSizeRequest(image.Pixbuf.Width, image.Pixbuf.Height);
                label.SetSizeRequest(-1, -1);

                int text_w, text_h;
                label.Layout.GetPixelSize(out text_w, out text_h);
                if(image.Pixbuf.Width < text_w) {
                    label.SetSizeRequest(image.Pixbuf.Width, -1);
                }

                Resize(1, 1);
            }

            get { return image.Pixbuf; }
        }

        public string Label {
            set {
                try {
                    label.Markup = String.Format("<small><b>{0}</b></small>", GLib.Markup.EscapeText(value));
                } catch(Exception) {
                }
            }
        }
    }
}
