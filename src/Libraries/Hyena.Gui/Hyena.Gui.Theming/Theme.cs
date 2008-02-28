//
// Theme.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections.Generic;
using Gtk;
using Gdk;
using Cairo;

using Hyena.Gui;

namespace Hyena.Gui.Theming
{
    public abstract class Theme
    {
        private Stack<ThemeContext> contexts = new Stack<ThemeContext> ();
        private GtkColors colors;

        private Cairo.Color selection_fill;
        private Cairo.Color selection_stroke;
        
        private Cairo.Color view_fill;
        private Cairo.Color view_fill_transparent;

        public GtkColors Colors {
            get { return colors; }
        }

        public Theme (Widget widget) : this (widget, new GtkColors ())
        {
        }

        public Theme (Widget widget, GtkColors colors)
        {
            this.colors = colors;
            this.colors.Refreshed += delegate { OnColorsRefreshed (); };
            this.colors.Widget = widget;

            PushContext ();
        }

        protected virtual void OnColorsRefreshed ()
        {
            selection_fill = colors.GetWidgetColor (GtkColorClass.Dark, StateType.Active);
            selection_stroke = colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected);
            
            view_fill = colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal);
            view_fill_transparent = view_fill;
            view_fill_transparent.A = 0;
        }

#region Drawing
     
        public abstract void DrawHeaderSeparator(Cairo.Context cr, Gdk.Rectangle alloc, int x, int bottom_offset);
        
        public abstract void DrawHeaderBackground (Cairo.Context cr, Gdk.Rectangle alloc, int bottom_offset, bool fill);
        
        public void DrawFrame (Cairo.Context cr, Gdk.Rectangle alloc, bool baseColor)
        {
            DrawFrame (cr, alloc,  baseColor 
                ? colors.GetWidgetColor (GtkColorClass.Base, StateType.Normal)
                : colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal));
        }
        
        public abstract void DrawFrame (Cairo.Context cr, Gdk.Rectangle alloc, Cairo.Color color);
        
        public void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc, int bottom_offset)
        {
            DrawColumnHighlight (cr, alloc, bottom_offset, colors.GetWidgetColor(GtkColorClass.Background, StateType.Selected));
        }
        
        public abstract void DrawColumnHighlight (Cairo.Context cr, Gdk.Rectangle alloc, int bottom_offset, Cairo.Color color);
        
        public abstract void DrawFooter (Cairo.Context cr, Gdk.Rectangle alloc);
        
        public void DrawLeftBorder (Cairo.Context cr, Gdk.Rectangle alloc)
        {
            DrawLeftOrRightBorder (cr, alloc.X + 1, alloc);
        }
        
        public void DrawRightBorder (Cairo.Context cr, Gdk.Rectangle alloc)
        {
            DrawLeftOrRightBorder (cr, alloc.X + alloc.Width, alloc);   
        }
        
        protected abstract void DrawLeftOrRightBorder (Cairo.Context cr, int x, Gdk.Rectangle alloc);
        
        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height)
        {
            DrawRowSelection (cr, x, y, width, height, true);
        }
        
        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height, bool filled)
        {
            DrawRowSelection (cr, x, y, width, height, filled, true, 
                colors.GetWidgetColor (GtkColorClass.Background, StateType.Selected), CairoCorners.All);
        }
        
        public void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
            bool filled, bool stroked, Cairo.Color color)
        {
            DrawRowSelection (cr, x, y, width, height, filled, stroked, color, CairoCorners.All);
        }
        
        public abstract void DrawRowSelection (Cairo.Context cr, int x, int y, int width, int height,
            bool filled, bool stroked, Cairo.Color color, CairoCorners corners);
        
        public abstract void DrawRowRule (Cairo.Context cr, int x, int y, int width, int height);

        public Cairo.Color ViewFill {
            get { return view_fill; }
        }
        
        public Cairo.Color ViewFillTransparent {
            get { return view_fill_transparent; }
        }
        
        public Cairo.Color SelectionFill {
            get { return selection_fill; }
        }
        
        public Cairo.Color SelectionStroke {
            get { return selection_stroke; }
        }

#endregion

#region Contexts

        public void PushContext ()
        {
            PushContext (new ThemeContext ());
        }

        public void PushContext (ThemeContext context)
        {
            lock (this) {
                contexts.Push (context);
            }
        }
        
        public ThemeContext PopContext ()
        {
            lock (this) {
                return contexts.Pop ();
            }
        }

        public ThemeContext Context {
            get { lock (this) { return contexts.Peek (); } }
        }

#endregion

    }
}
