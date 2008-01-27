/***************************************************************************
 *  SplashScreen.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using Gtk;
using Cairo;

using Banshee.Base;

namespace Banshee.Gui.Dialogs
{
    public class SplashScreenDrawArgs : EventArgs
    {
        private Gdk.Rectangle allocation;
        private Context context;
        private Gdk.Drawable drawable;
        private bool retval;
        
        public SplashScreenDrawArgs(Context context, Gdk.Drawable drawable, Gdk.Rectangle allocation)
        {
            this.context = context;
            this.drawable = drawable;
            this.allocation = allocation;
            this.retval = true;
        }
        
        public Gdk.Rectangle Allocation {
            get { return allocation; }
        }
        
        public Context Context {
            get { return context; }
        }
        
        public Gdk.Drawable Drawable {
            get { return drawable; }
        }
        
        public bool RetVal {
            get { return retval; }
            set { retval = value; }
        }
    }

    public delegate void SplashScreenDrawHandler(object o, SplashScreenDrawArgs args);

    public class SplashScreen : Gtk.Window, IDisposable
    {
        private static Color default_color = new Color(0xff, 0xff, 0xff, 0.65);
    
        private Color text_color = default_color;
        private Color bar_fill_color = default_color;
        private Color bar_outline_color = default_color;
        
        private Gdk.Pixbuf pixbuf;
        private string message;
        private double progress;
        private Pango.Layout layout;
        
        public event SplashScreenDrawHandler DrawScreen;

        public SplashScreen(string title, Gdk.Pixbuf pixbuf) : base(WindowType.Toplevel)
        {
            this.pixbuf = pixbuf;
            
            SetSizeRequest(pixbuf.Width, pixbuf.Height);
            
            Title = title;
            Decorated = false;
            TypeHint = Gdk.WindowTypeHint.Splashscreen;
            WindowPosition = WindowPosition.Center;
            AppPaintable = true;
            
            layout = new Pango.Layout(PangoContext);
            
            Globals.StartupInitializer.ComponentInitializing += delegate(object o, ComponentInitializingArgs args) {
                message = args.Name;
                progress = args.Current / (double)args.Total;
                QueueDraw();
                PumpEventLoop();
            };
        }
        
        private void PumpEventLoop()
        {
            while(Application.EventsPending()) {
                Application.RunIteration(false);
                System.Threading.Thread.Sleep(10);
            }
        }
        
        private bool DrawSplash(Cairo.Context cr)
        {
            SplashScreenDrawHandler handler = DrawScreen;
            if(handler != null) {
                SplashScreenDrawArgs args = new SplashScreenDrawArgs(cr, GdkWindow, Allocation);
                handler(this, args);
                return args.RetVal;
            }
        
            int bar_height = 6;
            
            GdkWindow.DrawPixbuf(Style.LightGC(StateType.Normal), pixbuf, 0, 0, 
                Allocation.X, Allocation.Y, pixbuf.Width, pixbuf.Height,
                Gdk.RgbDither.None, 0, 0);
                
            cr.Antialias = Antialias.Default;
            cr.Color = text_color;
            
            if(message != null) {
                cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Normal);
                cr.SetFontSize(12);
                cr.MoveTo(20, Allocation.Height - bar_height - 26);
                cr.ShowText(message);
            }
            
            cr.Antialias = Antialias.None;
            
            cr.Color = bar_outline_color;
            cr.LineWidth = 1.0;
            cr.Rectangle(Allocation.X + 20, Allocation.Height - bar_height - 20,
                Allocation.Width - 40, bar_height);
            cr.Stroke();
            
            cr.Color = bar_fill_color;
            cr.Rectangle(Allocation.X + 20 + 1, Allocation.Height - bar_height - 18,
                (int)((double)(Allocation.Width - 43) * progress), bar_height - 3);
            cr.FillPreserve();
            
            return true;
        }
        
        protected override void OnRealized()
        {
            base.OnRealized();
            GdkWindow.SetBackPixmap(null, false);
            QueueDraw();
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if(!IsRealized) {
                return false;
            }
            
            Cairo.Context cr = Gdk.CairoHelper.Create(GdkWindow);
            bool retval = true;
            
            foreach(Gdk.Rectangle rect in evnt.Region.GetRectangles()) {
                cr.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
                cr.Clip();
                retval |= DrawSplash(cr);
            }
            
            ((IDisposable)cr).Dispose();
            return retval;
        }
        
        public void Run()
        {   
            Show();
            PumpEventLoop();
        }
        
        public override void Dispose()
        {
            Hide();
            Destroy();
            
            base.Dispose();
        }
        
        public Color TextColor {
            get { return text_color; }
            set { text_color = value; QueueDraw(); }
        }
        
        public Color BarOutlineColor {
            get { return bar_outline_color; }
            set { bar_outline_color = value; QueueDraw(); }
        }
        
        public Color BarFillColor {
            get { return bar_fill_color; }
            set { bar_fill_color = value; QueueDraw(); }
        }
    }
}
