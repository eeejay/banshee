/***************************************************************************
 *  IconThemeUtils.cs
 *
 *  Copyright (C) 2005-2007 Novell, Inc.
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

namespace Banshee.Gui
{
    public static class IconThemeUtils
    {
        public static bool HasIcon(string name)
        {
            return IconTheme.Default.HasIcon(name);
        }

        public static Gdk.Pixbuf LoadIcon(int size, params string [] names)
        {
            for(int i = 0; i < names.Length; i++) {
                Gdk.Pixbuf pixbuf = LoadIcon(names[i], size, i == names.Length - 1);
                if(pixbuf != null) {
                    return pixbuf;
                }
            }
            
            return null;
        }

        public static Gdk.Pixbuf LoadIcon(string name, int size)
        {
            return LoadIcon(name, size, true);
        }

        public static Gdk.Pixbuf LoadIcon(string name, int size, bool fallBackOnResource)
        {
            try {
                Gdk.Pixbuf pixbuf = IconTheme.Default.LoadIcon(name, size, (IconLookupFlags)0);
                if(pixbuf != null) {
                    return pixbuf;
                }
            } catch {
            }
            
            if(!fallBackOnResource) {
                return null;
            }
            
            try {
                return new Gdk.Pixbuf(System.Reflection.Assembly.GetEntryAssembly(), name + ".png");
            } catch(Exception) {
                return null;
            }
        }

        public static void SetWindowIcon(Gtk.Window window)
        {
           // SetWindowIcon(window, Branding.ApplicationIconName);
        }

        public static void SetWindowIcon(Gtk.Window window, string iconName)
        {
            if(window != null && iconName != null) {
                window.IconName = iconName;
            }
        }
    }
}
