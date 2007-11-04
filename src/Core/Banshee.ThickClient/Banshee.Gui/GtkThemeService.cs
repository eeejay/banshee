//
// GtkThemeService.cs
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
using System.Collections.Generic;
using Gtk;

using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Gui
{
    public class GtkThemeService : IService
    {
        private Widget listen_widget;
        
        public event EventHandler ThemeChanged;
        
        public GtkThemeService ()
        {
        }
        
        private void OnStyleSet (object o, StyleSetArgs args)
        {
            SourceInvalidateIconPixbuf (ServiceManager.SourceManager.Sources);
            OnThemeChanged ();
        }
        
        private void SourceInvalidateIconPixbuf (ICollection<Source> sources)
        {
            foreach (Source source in sources) {
                source.Properties.Remove ("IconPixbuf");
                SourceInvalidateIconPixbuf (source.Children);
            }
        }
        
        protected virtual void OnThemeChanged ()
        {
            EventHandler handler = ThemeChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public Widget ListenWidget {
            get { return listen_widget; }
            set {
                if (listen_widget != null) {
                    listen_widget.StyleSet -= OnStyleSet;
                }
                
                listen_widget = value;
                
                if (listen_widget != null) {
                    listen_widget.StyleSet += OnStyleSet;
                }
            }
        }
        
        string IService.ServiceName {
            get { return "GtkThemeService"; }
        }
    }
}