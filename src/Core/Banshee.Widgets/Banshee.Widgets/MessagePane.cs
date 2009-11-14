//
// MessagePane.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

// Derived from Beagle's 'Base' Widget: beagle/search/Pages/Base.cs

using System;
using Mono.Unix;
using Gtk;

namespace Banshee.Widgets
{
    public class MessagePane : Table
    {
        private Image headerIcon;
        private Label header;
        private Gdk.Pixbuf arrow;

        public MessagePane () : base (1, 2, false)
        {
            RowSpacing = ColumnSpacing = 12;

            headerIcon = new Image ();
            headerIcon.Yalign = 0.0f;
            Attach (headerIcon, 0, 1, 0, 1, 0, AttachOptions.Fill, 0, 0);

            header = new Label ();
            header.SetAlignment (0.0f, 0.5f);
            Attach (header, 1, 2, 0, 1, AttachOptions.Expand | AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            ShowAll ();
        }

        public Gdk.Pixbuf HeaderIcon {
            set { headerIcon.Pixbuf = value; }
        }

        public Gdk.Pixbuf ArrowIcon {
            set { arrow = value; }
        }

        public string HeaderIconStock {
            set { headerIcon.SetFromStock (value, IconSize.Dnd); }
        }

        public string HeaderMarkup {
            set { header.Markup = value; }
        }

        private void AttachArrow (Gdk.Pixbuf arrow)
        {
            uint row = NRows;

            Image image = arrow == null ? new Image (this.arrow) : new Image (arrow);
            image.Yalign = 0.0f;
            image.Xalign = 1.0f;
            image.Show ();
            Attach (image, 0, 1, row, row + 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
        }

        public void Append (string tip)
        {
            Append (tip, true);
        }

        public void Append (string tip, bool showArrow)
        {
            Append (tip, showArrow, null);
        }

        public void Append (string tip, bool showArrow, Gdk.Pixbuf arrow)
        {
            uint row = NRows;
            Label label;

            if (showArrow) {
                AttachArrow (arrow);
            }

            label = new Label ();
            label.Markup = tip;
            label.SetAlignment (0.0f, 0.5f);
            label.LineWrap = true;
            label.ModifyFg (StateType.Normal, label.Style.Foreground (StateType.Insensitive));
            label.Show ();
            Attach (label, 1, 2, row, row + 1, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
        }

        public void Append (Widget widget)
        {
            Append (widget, 0, 0, false);
        }

        public void Append (Widget widget, AttachOptions xoptions, AttachOptions yoptions, bool showArrow)
        {
            Append(widget, xoptions, yoptions, showArrow, null);
        }

        public void Append (Widget widget, AttachOptions xoptions, AttachOptions yoptions,
            bool showArrow, Gdk.Pixbuf arrow)
        {
            uint row = NRows;

            if (showArrow) {
                AttachArrow (arrow);
            }

            Attach (widget, 1, 2, row, row + 1, xoptions, yoptions, 0, 0);
            widget.ModifyBg (StateType.Normal, Style.Base (StateType.Normal));
        }

        public void Clear ()
        {
            foreach (Widget child in Children) {
                if (child != headerIcon && child != header) {
                    Remove (child);
                }
            }

            Resize (1, 2);
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            Requisition tableReq = Requisition;
            allocation.X += Math.Max ((allocation.Width - tableReq.Width) / 2, 0);
            allocation.Y += Math.Max ((int)((allocation.Height - tableReq.Height) * 0.35), 0);
            allocation.Width = tableReq.Width;
            allocation.Height = tableReq.Height;
            SizeAllocate (allocation);
        }
    }
}

