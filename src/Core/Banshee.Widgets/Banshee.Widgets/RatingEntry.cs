// 
// RatingEntry.cs
//
// Author:
//   Gabriel Burt <gabriel.burt@gmail.com>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Gabriel Burt
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
// NONINFRINGEMENT. IN NO  SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Gtk;
using Gdk;
using System;

namespace Banshee.Widgets
{
    public class RatingEntry : Gtk.EventBox
    {
        private static Pixbuf default_icon_rated;
        private static Pixbuf default_icon_not_rated;
        
        private int max_rating = 5;
        private int min_rating = 1;
        private Pixbuf icon_rated;
        private Pixbuf icon_blank;
        
        public object RatedObject;
        
        private int rating;
        private bool embedded;
        private int y_offset = 4, x_offset = 4;
        private bool preview_on_hover = false;
        private Gdk.Pixbuf display_pixbuf;
        
        public event EventHandler Changing;
        public event EventHandler Changed;
        
#region Constructors
        
        public RatingEntry () : this (1) 
        {
        }
        
        public RatingEntry (int rating) : this (rating, false)
        {
        }

        public RatingEntry (int rating, bool embedded)
        {
            if (IconRated.Height != IconNotRated.Height || IconRated.Width != IconNotRated.Width) {
                throw new ArgumentException ("Rating widget requires that rated and blank icons have the same height and width");
            }
            
            this.rating = rating;
            this.embedded = embedded;
            
            if (embedded) {
                y_offset = 0;
                x_offset = 0;
            }
            
            //PreviewOnHover = true;
            //Events |= Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
            
            CanFocus = true;
            
            display_pixbuf = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, Width, Height);
            display_pixbuf.Fill (0xffffff00);
            DrawRating (Value);
            
            EnsureStyle ();
            ShowAll ();
        }
        
#endregion

        ~RatingEntry ()
        {
            display_pixbuf.Dispose ();
            display_pixbuf = null;
            
            icon_rated = null;
            icon_blank = null;
        }
        
#region Public API
        
        /*public Pixbuf DrawRating (int val)
        {
            Pixbuf buf = new Pixbuf (Gdk.Colorspace.Rgb, true, 8, Width, Height);
            DrawRating (buf, val);
            return buf;
        }*/
        
        internal void SetValueFromPosition (int x)
        {
            Value = RatingFromPosition (x);
        }
        
#endregion

#region Public Properties

        public int Value {
            get { return rating; }
            set {
                if (rating != value && value >= min_rating - 1 && value <= max_rating) {
                    rating = value;
                    OnChanging ();
                    OnChanged ();
                }
            }
        }
        
        public int XOffset {
            get { return x_offset; }
        }
        
        public int YOffset {
            get { return y_offset; }
        }
        
        public Pixbuf DisplayPixbuf {
            get { return display_pixbuf; }
        }
        
        public int MaxRating {
            get { return max_rating; }
            set { max_rating = value; }
        }
        
        public int MinRating {
            get { return min_rating; }
            set { min_rating = value; }
        }
        
        public int NumLevels {
            get { return max_rating - min_rating + 1; }
        }
        
        public static Pixbuf DefaultIconRated {
            get { return default_icon_rated ?? default_icon_rated = Gdk.Pixbuf.LoadFromResource ("rating-rated.png"); }
            set { default_icon_rated = value; }
        }
        
        public static Pixbuf DefaultIconNotRated {
            get { return default_icon_not_rated ?? default_icon_not_rated = Gdk.Pixbuf.LoadFromResource ("rating-unrated.png"); }
            set { default_icon_not_rated = value; }
        }
        
        public Pixbuf IconRated {
            get { return icon_rated ?? icon_rated = DefaultIconRated; }
            set { icon_rated = value; }
        }
        
        public Pixbuf IconNotRated {
            get { return icon_blank ?? icon_blank = DefaultIconNotRated; }
            set { icon_blank = value; }
        }
        
        public bool PreviewOnHover {
            get { return preview_on_hover; }
            set {
                if (preview_on_hover == value)
                    return;
                    
                preview_on_hover = value;
                
                /*if (value)
                    Events |= Gdk.EventMask.PointerMotionMask;
                else
                    Events = Events & ~Gdk.EventMask.PointerMotionMask;*/
            }
        }
        
        public int Width {
            get { return IconRated.Width * NumLevels; }
        }
        
        public int Height {
            get { return IconRated.Height; }
        }
        
#endregion

#region Protected Gtk.Widget Overrides
        
        protected override void OnSizeRequested (ref Gtk.Requisition requisition)
        {
            requisition.Width = Width + (2 * x_offset);
            requisition.Height = Height + (2 * y_offset);
            base.OnSizeRequested (ref requisition);
        }
        
        protected override bool OnExposeEvent (Gdk.EventExpose evnt)
        {
            if (evnt.Window != GdkWindow) {
                return true;
            }
            
            int y_mid = (Allocation.Height - Height) / 2;

            if (!embedded) {            
                Gtk.Style.PaintShadow (Style, GdkWindow, StateType.Normal, ShadowType.In,
                    evnt.Area, this, "entry", 0, y_mid - y_offset, Allocation.Width, 
                    Height + (y_offset * 2));
            }

            GdkWindow.DrawPixbuf (Style.BackgroundGC (StateType.Normal), 
                display_pixbuf, 0, 0, x_offset, y_mid, Width, Height, Gdk.RgbDither.None, 0, 0);

            return true;
        }

        protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
        {
            if (evnt.Button != 1) {
                return false;
            }
            
            HasFocus = true;
            Value = RatingFromPosition (evnt.X);
            
            return true;
        }
        
        protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing crossing)
        {
            DrawRating (Value);
            QueueDraw ();
            return true;
        }
        
        protected override bool OnMotionNotifyEvent (Gdk.EventMotion motion)
        {
            if ((motion.State & Gdk.ModifierType.Button1Mask) != 0) {
                Value = RatingFromPosition (motion.X);
                return true;
            } else if (preview_on_hover) {
                DrawRating (RatingFromPosition (motion.X));
                QueueDraw ();
                return true;
            }
            
            return false;
        }
        
        protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
        {
            switch (evnt.Key) {
                case Gdk.Key.Up:
                case Gdk.Key.Right:
                case Gdk.Key.plus:
                case Gdk.Key.equal:
                    Value++;
                    return true;
                
                case Gdk.Key.Down:
                case Gdk.Key.Left:
                case Gdk.Key.minus:
                    Value--;
                    return true;
            }
            
            if (evnt.KeyValue >= (48 + MinRating - 1) && evnt.KeyValue <= (48 + MaxRating) && evnt.KeyValue <= 59) {
                Value = (int)evnt.KeyValue - 48;
                return true;
            }
            
            return false;
        }
        
        protected override bool OnScrollEvent (EventScroll args)
        {
            return HandleScroll (args);
        }

#endregion

#region Internal API, primarily for RatingMenuItem
        
        internal bool HandleKeyPress (Gdk.EventKey evnt)
        {
            return this.OnKeyPressEvent (evnt);
        }

        internal bool HandleScroll (EventScroll args)
        {
            switch (args.Direction) {
                case Gdk.ScrollDirection.Up:
                case Gdk.ScrollDirection.Right:
                    Value++;
                    return true;
                
                case Gdk.ScrollDirection.Down:
                case Gdk.ScrollDirection.Left:
                    Value--;
                    return true;
            }
            
            return false;
        }
        
#endregion

#region Protected methods

        protected virtual void OnChanging ()
        {
            EventHandler handler = Changing;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

        protected virtual void OnChanged ()
        {
            DrawRating (Value);
            QueueDraw ();

            EventHandler handler = Changed;
            if (handler != null) {
                handler (this, new EventArgs ());
            }
        }

#endregion

#region Private methods

        private void DrawRating (int val)
        {
            for (int i = 0; i < MaxRating; i++) {
                if (i <= val - MinRating) {
                    IconRated.CopyArea (0, 0, IconRated.Width, IconRated.Height, 
                        DisplayPixbuf, i * IconRated.Width, 0);
                } else {
                    IconNotRated.CopyArea (0, 0, IconRated.Width, IconRated.Height,
                        DisplayPixbuf, i * IconRated.Width, 0);
                }
            }
        }
        
        private int RatingFromPosition (double x)
        {
            return x < x_offset + 1 ? 0 : (int) Math.Max ( 0, Math.Min (((x - x_offset) 
                / (double)icon_rated.Width) + 1, MaxRating));
        }

#endregion

    }
}
