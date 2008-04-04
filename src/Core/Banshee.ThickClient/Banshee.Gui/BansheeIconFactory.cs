//
// BansheeIconFactory.cs
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
using System.IO;
using System.Reflection;

using Gtk;

namespace Banshee.Gui
{
    public class BansheeIconFactory : IconFactory
    {
        private IconTheme theme;
        public IconTheme Theme {
            get { return theme; }
        }
    
        public BansheeIconFactory ()
        {
            theme = IconTheme.Default;
        
            string icon_theme_path = Banshee.Base.Paths.GetInstalledDataDirectory ("icons");
            if (Directory.Exists (icon_theme_path)) {
                Hyena.Log.DebugFormat ("Adding icon theme search path: {0}", icon_theme_path);
                Theme.AppendSearchPath (icon_theme_path);
            }
               
            AddDefault ();
        }
        
        public void Add (string name)
        {
            IconSet icon_set = new IconSet ();
            IconSource source = new IconSource ();
            source.IconName = name;
            icon_set.AddSource (source);
            
            Add (name, icon_set);
        }
    }
}
