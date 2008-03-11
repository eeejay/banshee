//
// ColumnCellText.cs
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
using Cairo;

using Hyena.Gui;
using Hyena.Gui.Theming;

namespace Hyena.Data.Gui
{
    public class ColumnCellText : ColumnCell
    {
        public delegate string DataHandler ();
    
        private Pango.Weight font_weight = Pango.Weight.Normal;
        private Pango.EllipsizeMode ellipsize_mode = Pango.EllipsizeMode.End;
        private int text_width;
        private int text_height;
        private bool use_cairo_pango = !String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("USE_CAIRO_PANGO"));

        public ColumnCellText (string property, bool expand) : base (property, expand)
        {
        }
    
        public override void Render (CellContext context, StateType state, double cellWidth, double cellHeight)
        {
            context.Layout.Width = (int)((cellWidth - 8) * Pango.Scale.PangoScale);
            context.Layout.FontDescription = context.Widget.PangoContext.FontDescription.Copy ();
            context.Layout.FontDescription.Weight = font_weight;
            context.Layout.Ellipsize = ellipsize_mode;
            
            context.Layout.SetText (Text);
            context.Layout.GetPixelSize (out text_width, out text_height);
            
            if (use_cairo_pango) {
                context.Context.MoveTo (4, ((int)cellHeight - text_height) / 2);
                PangoCairoHelper.LayoutPath (context.Context, context.Layout);
                context.Context.Color = context.Theme.Colors.GetWidgetColor (GtkColorClass.Text, state);
                context.Context.Fill ();
            } else {
                Style.PaintLayout(context.Widget.Style, context.Drawable, state, true, context.Area,
                    context.Widget, "text", context.Area.X + 4, context.Area.Y + (((int)cellHeight - text_height) / 2),
                    context.Layout);
            }
        }
        
        protected virtual string Text {
            get {
                return BoundObject == null ? String.Empty : BoundObject.ToString();
            }
        }
        
        protected int TextWidth {
            get { return text_width; }
        }
        
        protected int TextHeight {
            get { return text_height; }
        }
        
        internal bool UseCairoPango {
            set { use_cairo_pango = value; }
        }
        
        public virtual Pango.Weight FontWeight {
            get { return font_weight; }
            set { font_weight = value; }
        }
        
        public virtual Pango.EllipsizeMode EllipsizeMode {
            get { return ellipsize_mode; }
            set { ellipsize_mode = value; }
        }
    }
}
