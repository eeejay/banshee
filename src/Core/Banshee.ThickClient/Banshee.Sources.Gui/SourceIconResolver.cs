//
// SourceIconResolver.cs
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
using System.Reflection;

using Banshee.Gui;

namespace Banshee.Sources.Gui
{
    public static class SourceIconResolver
    {
        public static Gdk.Pixbuf ResolveIcon (Source source)
        {
            return ResolveIcon (source, 22, null);
        }

        public static Gdk.Pixbuf ResolveIcon (Source source, string @namespace)
        {
            return ResolveIcon (source, 22, @namespace);
        }

        public static Gdk.Pixbuf ResolveIcon (Source source, int size)
        {
            return ResolveIcon (source, size, null);
        }

        public static Gdk.Pixbuf ResolveIcon (Source source, int size, string @namespace)
        {
            string name_property = @namespace == null
                ? "Icon.Name"
                : String.Format ("{0}.Icon.Name", @namespace);

            string pixbuf_property = @namespace == null
                ? String.Format ("Icon.Pixbuf_{0}", size)
                : String.Format ("{0}.Icon.Pixbuf_{1}", @namespace, size);

            Gdk.Pixbuf icon = source.Properties.Get<Gdk.Pixbuf> (pixbuf_property);

            if (icon != null) {
                return icon;
            }

            Type icon_type = source.Properties.GetType (name_property);
            Assembly asm = source.Properties.Get<Assembly> ("ResourceAssembly");

            if (icon_type == typeof (string)) {
                icon = IconThemeUtils.LoadIcon (asm, size, source.Properties.Get<string> (name_property));
            } else if (icon_type == typeof (string [])) {
                icon = IconThemeUtils.LoadIcon (asm, size, source.Properties.Get<string[]> (name_property));
            }

            if (icon == null) {
                icon = Banshee.Gui.IconThemeUtils.LoadIcon (size, "image-missing");
            }

            if (icon != null) {
                source.Properties.Set<Gdk.Pixbuf> (pixbuf_property, icon);
            }

            return icon;
        }

        public static void InvalidatePixbufs (Source source)
        {
            InvalidatePixbufs (source, null);
        }

        public static void InvalidatePixbufs (Source source, string @namespace)
        {
            string property = @namespace == null ? "Icon.Pixbuf" : String.Format ("{0}.Icon.Pixbuf", @namespace);
            source.Properties.RemoveStartingWith (property);
        }
    }
}
