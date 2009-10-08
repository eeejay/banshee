// 
// PanelGtk.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;

using Gtk;

namespace Mutter
{
    public class PanelGtk : PanelClient
    {
        [DllImport ("libmoblin-panel-gtk")]
        private static extern IntPtr mpl_panel_gtk_get_type ();
    
        public static new GLib.GType GType {
            get { return new GLib.GType (mpl_panel_gtk_get_type ()); }
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern IntPtr mpl_panel_gtk_new (string name, string tooltip, string stylesheet,
            string button_style, bool with_toolbar_service);
        
        public PanelGtk (string name, string tooltip, string stylesheet,
            string button_style, bool with_toolbar_service) : base (IntPtr.Zero)
        {
            Raw = mpl_panel_gtk_new (name, tooltip, stylesheet, button_style, with_toolbar_service);
        }
    
        public PanelGtk (IntPtr raw) : base (raw)
        {
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern IntPtr mpl_panel_gtk_get_window (IntPtr panel);
    
        // FIXME: can probably cache this
        public Container Window {
            get { return new Container (mpl_panel_gtk_get_window (Handle)); }
        }
    }
}
