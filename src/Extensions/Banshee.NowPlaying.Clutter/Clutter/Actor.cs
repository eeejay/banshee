//
// Actor.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Runtime.InteropServices;

namespace Clutter
{
    public class Actor : GLib.InitiallyUnowned
    {
        public Actor (IntPtr raw) : base (raw) 
        {
        }

        public Actor () : base (IntPtr.Zero)
        {
            CreateNativeObject (new string[0], new GLib.Value[0]);
        }
        
        [DllImport ("clutter")]
        private static extern void clutter_actor_set_position (IntPtr handle, int x, int y);
        
        public void SetPosition (int x, int y)
        {
            clutter_actor_set_position (Handle, x, y);
        }
        
        [DllImport ("clutter")]
        private static extern void clutter_actor_get_position (IntPtr handle, out int x, out int y);
        
        public void GetPosition (out int x, out int y)
        {
            clutter_actor_get_position (Handle, out x, out y);
        }
        
        [DllImport ("clutter")]
        private static extern void clutter_actor_set_size (IntPtr handle, int width, int height);
        
        public void SetSize (int width, int height)
        {
            clutter_actor_set_size (Handle, width, height);
        }
        
        [DllImport ("clutter")]
        private static extern void clutter_actor_get_size (IntPtr handle, out int width, out int height);
        
        public void GetSize (out int width, out int height)
        {
            clutter_actor_get_size (Handle, out width, out height);
        }
        
        [GLib.Property ("rotation-angle-x")]
        public double RotationAngleX {
            get { using (GLib.Value val = GetProperty ("rotation-angle-x")) return (double)val; }
            set { using (GLib.Value val = new GLib.Value (value)) SetProperty ("rotation-angle-x", val); }
        }
        
        [GLib.Property ("rotation-angle-y")]
        public double RotationAngleY {
            get { using (GLib.Value val = GetProperty ("rotation-angle-y")) return (double)val; }
            set { using (GLib.Value val = new GLib.Value (value)) SetProperty ("rotation-angle-y", val); }
        }
        
        [GLib.Property ("rotation-angle-z")]
        public double RotationAngleZ {
            get { using (GLib.Value val = GetProperty ("rotation-angle-z")) return (double)val; }
            set { using (GLib.Value val = new GLib.Value (value)) SetProperty ("rotation-angle-z", val); }
        }
        
        [DllImport ("clutter")]
        private static extern IntPtr clutter_actor_get_type ();
        
        public static new GLib.GType GType {
            get { return new GLib.GType (clutter_actor_get_type ()); }
        }
    }
}
