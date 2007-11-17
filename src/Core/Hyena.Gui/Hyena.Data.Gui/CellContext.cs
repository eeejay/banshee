//
// CellContext.cs
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

namespace Hyena.Data.Gui
{
    public class CellContext
    {
        private Cairo.Context context;
        private Pango.Layout layout;
        private Gtk.Widget widget;
        private Gdk.Drawable drawable;
        private ListViewGraphics graphics;
        private Gdk.Rectangle area;
        
        public CellContext (Cairo.Context context, Pango.Layout layout, Gtk.Widget widget,
            Gdk.Drawable drawable, ListViewGraphics graphics, Gdk.Rectangle area)
        {
            this.context = context;
            this.layout = layout;
            this.widget = widget;
            this.drawable = drawable;
            this.graphics = graphics;
            this.area = area;
        }
        
        public Cairo.Context Context {
            get { return context; }
        }

        public Pango.Layout Layout {
            get { return layout; }
        }

        public Gtk.Widget Widget {
            get { return widget; }
        }

        public Gdk.Drawable Drawable {
            get { return drawable; }
        }

        public ListViewGraphics Graphics {
            get { return graphics; }
        }

        public Gdk.Rectangle Area {
            get { return area; }
        }
    }
}
