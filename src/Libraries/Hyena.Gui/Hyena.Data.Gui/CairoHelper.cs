//
// CairoHelper.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using Gdk;
using Cairo;

namespace Hyena.Data.Gui
{
    public static class CairoHelper
    {
        // [System.Runtime.InteropServices.DllImport ("libgdk-x11-2.0.so")]
        // private static extern IntPtr gdk_cairo_create (IntPtr raw);

        public static Cairo.Context CreateCairoDrawable (Gdk.Drawable drawable)
        {
            if (drawable == null) {
                return null;
            }
            
            return Gdk.CairoHelper.Create (drawable);
            
            /*Cairo.Context context = new Cairo.Context(gdk_cairo_create(drawable.Handle));
            if(context == null) {
                throw new ApplicationException("Could not create Cairo.Context");
            }

            return context;*/
        }
    }
}
