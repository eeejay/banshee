/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SpecialKeys.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Collections;
using System.Runtime.InteropServices;

namespace Banshee.Base
{
    public delegate void SpecialKeyPressedHandler(object o, SpecialKey key);

    public enum SpecialKey {
        None = 0,
        AudioLowerVolume = 0x1008FF11,
        AudioMute = 0x1008FF12,
        AudioRaiseVolume = 0x1008FF13,
        AudioPlay = 0x1008FF14,
        AudioStop = 0x1008FF15,
        AudioPrev = 0x1008FF16,
        AudioNext = 0x1008FF17
    };

    public class SpecialKeys
    {
        private Hashtable key_map = new Hashtable();
        private Hashtable key_registrations = new Hashtable();
        
        public SpecialKeys()
        {
            InitializeKeys();
        }
        
        public void RegisterHandler(SpecialKeyPressedHandler handler, params SpecialKey [] specialKeys)
        {
            foreach(SpecialKey specialKey in specialKeys) {
                int key = (int)key_map[specialKey];
                key_registrations[key] = Delegate.Combine(key_registrations[key] as Delegate, handler);
            }
        }
        
        public void UnregisterHandler(SpecialKeyPressedHandler handler, params SpecialKey [] specialKeys)
        {
            foreach(SpecialKey specialKey in specialKeys) {
                int key = (int)key_map[specialKey];
                key_registrations[key] = Delegate.Remove(key_registrations[key] as Delegate, handler); 
            }
        }
        
        private void InitializeKeys()
        {
            ArrayList kc_list = new ArrayList();
            
            foreach(SpecialKey key in Enum.GetValues(typeof(SpecialKey))) {
                int keycode = XKeysymToKeycode(gdk_x11_get_default_xdisplay(), key);
                key_map[keycode] = key;
                key_map[key] = keycode;
                kc_list.Add(keycode);
            }
            
            for(int i = 0; i < Gdk.Display.Default.NScreens; i++) {
                Gdk.Screen screen = Gdk.Display.Default.GetScreen(i);
                foreach(int keycode in kc_list) {
                    GrabKey(screen.RootWindow, keycode);
                }
                
                screen.RootWindow.AddFilter(FilterKey);
            }
        }
        
        private void GrabKey(Gdk.Window root, int keycode)
        {
            IntPtr xid = gdk_x11_drawable_get_xid(root.Handle);
            IntPtr xdisplay = gdk_x11_get_default_xdisplay();
            
            gdk_error_trap_push();
            
            XGrabKey(xdisplay, keycode, XModMask.None, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Mod2, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Mod5, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Lock, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Mod2 | XModMask.Mod5, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Mod2 | XModMask.Lock, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Mod5 | XModMask.Lock, xid, true, XGrabMode.Async, XGrabMode.Async);
            XGrabKey(xdisplay, keycode, XModMask.Mod2 | XModMask.Mod5 | XModMask.Lock, xid, true, 
                XGrabMode.Async, XGrabMode.Async);
        
            if(gdk_error_trap_pop() != 0) {
                Console.Error.WriteLine("Could not grab key {0}", keycode);
            }
        }
        
        private Gdk.FilterReturn FilterKey(IntPtr xeventPtr, Gdk.Event gdkEvent)
        {
            XKeyEvent xevent = (XKeyEvent)Marshal.PtrToStructure(xeventPtr, typeof(XKeyEvent));
            
            if(xevent.type != XEventName.KeyPress) {
                return Gdk.FilterReturn.Continue;
            }

            try {
                int keycode = (int)xevent.keycode;
                SpecialKey key = (SpecialKey)key_map[keycode];
                
                if(key_registrations[keycode] != null) {
                    (key_registrations[keycode] as SpecialKeyPressedHandler)(this, key);
                    return Gdk.FilterReturn.Remove;
                }
            } catch(Exception) {
                Console.Error.WriteLine("Could not invoke key callback");
            } 
            
            return Gdk.FilterReturn.Continue;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XKeyEvent
        {
            public XEventName type;
            public IntPtr serial;
            public bool send_event;
            public IntPtr display;
            public IntPtr window;
            public IntPtr root;
            public IntPtr subwindow;
            public IntPtr time;
            public int x;
            public int y;
            public int x_root;
            public int x_y;
            public uint state;
            public uint keycode;
            public bool same_screen;
        }

        [DllImport("libX11")]
        extern static int XKeysymToKeycode(IntPtr display, SpecialKey keysym);

        [DllImport("libX11")]
        extern static void XGrabKey(IntPtr display, int keycode, XModMask modifiers, 
            IntPtr window, bool owner_events, XGrabMode pointer_mode, XGrabMode keyboard_mode);

        [DllImport("gdk-x11-2.0")]
        static extern IntPtr gdk_x11_drawable_get_xid(IntPtr window);
        
        [DllImport("gdk-x11-2.0")]
        static extern IntPtr gdk_x11_get_default_xdisplay();
        
        [DllImport("gdk-x11-2.0")]
        static extern void gdk_error_trap_push();
        
        [DllImport("gdk-x11-2.0")]
        static extern int gdk_error_trap_pop();
        
        [Flags]
        private enum XModMask {
            None    = 0,
            Shift   = 1 << 0,
            Lock	    = 1 << 1,
            Control = 1 << 2,
            Mod1	    = 1 << 3,
            Mod2	    = 1 << 4,
            Mod3    = 1 << 5,
            Mod4    = 1 << 6,
            Mod5	    = 1 << 7
        }
        
        private enum XGrabMode {
            Sync  = 0,
            Async = 1
        };
        
        private enum XEventName {
            KeyPress   = 2,
            KeyRelease = 3,
        }
    }
}
