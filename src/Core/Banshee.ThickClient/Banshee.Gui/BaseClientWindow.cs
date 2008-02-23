//
// BaseClientWindow.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.Configuration;

namespace Banshee.Gui
{
    public abstract class BaseClientWindow : Window
    {
        private GtkElementsService elements_service;
        protected GtkElementsService ElementsService {
            get { return elements_service; }
        }
        
        private InterfaceActionService action_service;
        protected InterfaceActionService ActionService {
            get { return action_service; }
        }
        
        public event EventHandler TitleChanged;
    
        public BaseClientWindow (string title) : base (title)
        {
            elements_service = ServiceManager.Get<GtkElementsService> ("GtkElementsService");
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            
            ConfigureWindow ();
            ResizeMoveWindow ();
            
            elements_service.PrimaryWindow = this;
            
            AddAccelGroup (action_service.UIManager.AccelGroup);
            
            Initialize ();
        }
        
        public void ToggleVisibility ()
        {
            if (Visible) {
                SaveWindowSizePosition ();
                Visible = false;
            } else {
                RestoreWindowSizePosition ();
                Present ();
            }
        }
        
        private int x, y, w, h;
        private bool maximized;
        
        private void SaveWindowSizePosition ()
        {
            maximized = ((GdkWindow.State & Gdk.WindowState.Maximized) > 0);

            if (!maximized) {
                GetPosition (out x, out y);
                GetSize (out w, out h);
            }
        }

        private void RestoreWindowSizePosition () 
        {
            if (maximized) {
                Maximize ();
            } else {
                Resize (w, h);
                Move (x, y);
            }
        }
        
        protected abstract void Initialize ();
    
        protected virtual void ConfigureWindow ()
        {
            WindowPosition = WindowPosition.Center;
        }
    
        protected virtual void ResizeMoveWindow ()
        {
            int x = XPosSchema.Get ();
            int y = YPosSchema.Get (); 
            int width = WidthSchema.Get ();
            int height = HeightSchema.Get ();
           
            if(width != 0 && height != 0) {
                Resize (width, height);
            }

            if (x == 0 && y == 0) {
                SetPosition (WindowPosition.Center);
            } else {
                Move (x, y);
            }
            
            if (MaximizedSchema.Get ()) {
                Maximize ();
            } else {
                Unmaximize ();
            }
        }
        
        protected override bool OnDeleteEvent (Gdk.Event evnt)
        {
             if (ElementsService.PrimaryWindowClose != null) {
                if (ElementsService.PrimaryWindowClose ()) {
                    return true;
                }
            }
            
            Banshee.ServiceStack.Application.Shutdown ();
            return base.OnDeleteEvent (evnt);
        }

        protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
        {
            int x, y, width, height;

            if ((GdkWindow.State & Gdk.WindowState.Maximized) != 0) {
                return base.OnConfigureEvent (evnt);
            }
            
            GetPosition (out x, out y);
            GetSize (out width, out height);
           
            XPosSchema.Set (x);
            YPosSchema.Set (y);
            WidthSchema.Set (width);
            HeightSchema.Set (height);
            
            return base.OnConfigureEvent (evnt);
        }
        
        protected override bool OnWindowStateEvent (Gdk.EventWindowState evnt)
        {
            if ((evnt.NewWindowState & Gdk.WindowState.Withdrawn) == 0) {
                MaximizedSchema.Set ((evnt.NewWindowState & Gdk.WindowState.Maximized) != 0);
            }
            
            return base.OnWindowStateEvent (evnt);
        }
        
        protected virtual void OnTitleChanged ()
        {
            EventHandler handler = TitleChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected abstract void UpdateTitle ();
        
        public static readonly SchemaEntry<int> WidthSchema = new SchemaEntry<int>(
            "player_window", "width",
            1024,
            "Window Width",
            "Width of the main interface window."
        );

        public static readonly SchemaEntry<int> HeightSchema = new SchemaEntry<int>(
            "player_window", "height",
            700,
            "Window Height",
            "Height of the main interface window."
        );

        public static readonly SchemaEntry<int> XPosSchema = new SchemaEntry<int>(
            "player_window", "x_pos",
            0,
            "Window Position X",
            "Pixel position of Main Player Window on the X Axis"
        );

        public static readonly SchemaEntry<int> YPosSchema = new SchemaEntry<int>(
            "player_window", "y_pos",
            0,
            "Window Position Y",
            "Pixel position of Main Player Window on the Y Axis"
        );

        public static readonly SchemaEntry<bool> MaximizedSchema = new SchemaEntry<bool>(
            "player_window", "maximized",
            false,
            "Window Maximized",
            "True if main window is to be maximized, false if it is not."
        );
    }
}
