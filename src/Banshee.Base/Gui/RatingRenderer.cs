/***************************************************************************
 *  CellRendererRating.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Runtime.InteropServices;

using Gtk;
using Gdk;

namespace Banshee.Gui
{
    public static class CellRendererActivatable
    {
        public delegate bool ActivateHandler(IntPtr raw, IntPtr evnt, IntPtr widget, IntPtr path, 
            ref Gdk.Rectangle background_area, ref Gdk.Rectangle cell_area, int flags);
        
        [DllImport("libbanshee")]
        private static extern void gtksharp_cell_renderer_activatable_configure(IntPtr renderer, 
            ActivateHandler handler);

        public static void Configure(CellRenderer renderer, ActivateHandler handler)
        {
            gtksharp_cell_renderer_activatable_configure(renderer.Handle, handler);
        }
    }
    
    public delegate void CellRatingChangedHandler(object o, CellRatingChangedArgs args);

    public class CellRatingChangedArgs : EventArgs
    {
        private TreePath path;
        private uint rating;

        public CellRatingChangedArgs(TreePath path, uint rating)
        {
            this.path = path;
            this.rating = rating;
        }

        public TreePath Path {
            get { return path; }
        }

        public uint Rating {    
            get { return rating; }
        }
    }
    
    public class CellRendererRating : CellRendererText
    {
        private static Pixbuf rated_pixbuf = Gdk.Pixbuf.LoadFromResource("rating-rated.png");
        private static Pixbuf unrated_pixbuf = Gdk.Pixbuf.LoadFromResource("rating-unrated.png");
                
        public static Pixbuf RatedPixbuf {
            get { return rated_pixbuf; }
        }

        public static Pixbuf UnratedPixbuf {
            get { return unrated_pixbuf; }
        }

        public static uint MaxRating {
            get { return 5; }
        }

        private uint rating;
        private bool text_mode = false;

        private CellRendererActivatable.ActivateHandler activate_handler;
    
        public event CellRatingChangedHandler RatingChanged;
    
        public CellRendererRating()
        {
            try {
                activate_handler = new CellRendererActivatable.ActivateHandler(OnActivate);
                CellRendererActivatable.Configure(this, activate_handler);
            } catch {
                activate_handler = null;
            }
        }
        
        private bool OnActivate(IntPtr raw, IntPtr evnt_ptr, IntPtr widget, IntPtr path_ptr, 
            ref Gdk.Rectangle background_area, ref Gdk.Rectangle cell_area, int flags)
        {
            Gdk.EventButton evnt = Gdk.Event.GetEvent(evnt_ptr) as Gdk.EventButton;
            if(evnt == null) {
                return false;
            }
            
            TreePath path = null;
            
            try {
                TreeView view = new TreeView(widget);
                view.GetPathAtPos((int)evnt.X, (int)evnt.Y, out path);
            } catch {
            }
                        
            int cell_offset = (int)evnt.X - cell_area.X;
            int zero_offset = 4;
            uint rating = 0;
            
            if(cell_offset >= zero_offset) {
                rating = (uint)(Math.Min((cell_offset / RatedPixbuf.Width) + 1, MaxRating));
            }

            OnRatingChanged(path, rating);
        
            return true;
        }

        protected virtual void OnRatingChanged(TreePath path, uint rating)
        {
            CellRatingChangedHandler handler = RatingChanged;
            if(handler != null) {
                handler(this, new CellRatingChangedArgs(path, rating));
            }
        }

        private StateType RendererStateToWidgetState(CellRendererState flags)
        {
            StateType state = StateType.Normal;
            
            if((CellRendererState.Insensitive & flags).Equals(CellRendererState.Insensitive)) {
                state = StateType.Insensitive;
            } else if((CellRendererState.Selected & flags).Equals(CellRendererState.Selected)) {
                state = StateType.Selected;
            }
            
            return state;
        }
        
        protected override void Render(Gdk.Drawable drawable, Widget widget, Gdk.Rectangle background_area, 
            Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
        {
            Gdk.Window window = drawable as Gdk.Window;
            StateType state = RendererStateToWidgetState(flags);
            DrawRating(window, widget, cell_area, state, flags);
        }
        
        public override void GetSize(Widget widget, ref Gdk.Rectangle cell_area, 
            out int x_offset, out int y_offset, out int width, out int height)
        {
            height = RatedPixbuf.Height + 2;
            width = (RatedPixbuf.Width * (int)MaxRating) + 4;
            x_offset = 0;
            y_offset = 0;
        }

        private void DrawRating(Gdk.Window canvas, Gtk.Widget widget,
            Gdk.Rectangle area, StateType state, CellRendererState flags)
        {
            uint rating = !text_mode ? this.rating : 0; 
            
            if(text_mode) {
                try {
                    rating = Convert.ToUInt32(Text);
                } catch {
                }
            }
            
            if(RatedPixbuf == null || UnratedPixbuf == null) {
                return;
            }
            
            for(int i = 0; i < MaxRating; i++) {
                if(i < rating) {
                    canvas.DrawPixbuf(widget.Style.TextGC(state), RatedPixbuf, 0, 0,
                        area.X + (i * RatedPixbuf.Width) + 1, area.Y + 1, 
                        RatedPixbuf.Width, RatedPixbuf.Height, RgbDither.None, 0, 0);
                } else if((flags & CellRendererState.Prelit) > 0 && activate_handler != null) {
                    /*
                    This is nice in theory, but as prelight only happens on the row, not
                    the cell renderer or column+row, it is buggy (left<->right hover does
                    not work if cursor was already in the row) 
                    
                    int px = 0, py = 0;
                    
                    if(widget != null) {
                        widget.GetPointer(out px, out py);
                    }*/
                    
                    //if(px != 0 && py != 0 && px >= area.X && px <= area.X + area.Width) {
                    
                    canvas.DrawPixbuf(widget.Style.TextGC(state), UnratedPixbuf, 0, 0,
                        area.X + (i * UnratedPixbuf.Width) + 1, area.Y + 1,
                        UnratedPixbuf.Width, UnratedPixbuf.Height, RgbDither.None, 0, 0);
                        
                    //}
                }
            } 
        }
        
        public uint Rating {
            get { return rating; }
            set { rating = value; }
        }
        
        public bool TextMode {
            get { return text_mode; }
            set { text_mode = value; }
        }
    }
}
