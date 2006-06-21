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
using Banshee.Base;

namespace Banshee.Gui.Dialogs
{
    internal class SplashScreen : Gtk.Window, IDisposable
    {
        private static Gdk.Color fg_color = new Gdk.Color(0xc3, 0xd3, 0xe7);
            
        private Gdk.Pixbuf pixbuf;
        private string message;
        private double progress;
        private Pango.Layout layout;
        
        static SplashScreen()
        {
            Gdk.Colormap.System.AllocColor(ref fg_color, true, true);
        }
        
        public SplashScreen(string title, Gdk.Pixbuf pixbuf) : base(WindowType.Toplevel)
        {
            this.pixbuf = pixbuf;
            
            SetSizeRequest(pixbuf.Width, pixbuf.Height);
            
            Title = title;
            Decorated = false;
            KeepAbove = true;
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
        
        private void DrawSplash()
        {
            int bar_height = 6;
            
            GdkWindow.DrawPixbuf(Style.LightGC(StateType.Normal), pixbuf, 0, 0, 
                Allocation.X, Allocation.Y, pixbuf.Width, pixbuf.Height,
                Gdk.RgbDither.None, 0, 0);
                
            if(message != null) {
                int width, height;
                layout.SetMarkup(String.Format("<small>{0}</small>", GLib.Markup.EscapeText(message)));
                layout.GetPixelSize(out width, out height);
                
                Style.PaintLayout(Style, GdkWindow, StateType.Normal, true, Allocation, 
                    this, null, 20, Allocation.Height - height - bar_height - 24, layout);
            }
            
            GdkWindow.DrawRectangle(this.Style.ForegroundGC(StateType.Normal), false,
                Allocation.X + 20, Allocation.Height - bar_height - 20, 
                Allocation.Width - 40, bar_height);
                
            GdkWindow.DrawRectangle(this.Style.ForegroundGC(StateType.Normal), true,
                Allocation.X + 20 + 2, Allocation.Height - bar_height - 18, 
                (int)((double)(Allocation.Width - 43) * progress), bar_height - 3);
        }
        
        protected override void OnRealized()
        {
            base.OnRealized();
            
            GdkWindow.SetBackPixmap(null, false);
            
            ModifyText(StateType.Normal, Style.White);
            ModifyFg(StateType.Normal, fg_color);
            
            DrawSplash();
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            DrawSplash();
            return base.OnExposeEvent(evnt);
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
    }
}
