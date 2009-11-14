//
// GtkElementsService.cs
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

using Hyena.Data;

using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Gui
{
    public class GtkElementsService : IService, IPropertyStoreExpose
    {
        public delegate bool PrimaryWindowCloseHandler ();

        private PropertyStore property_store = new PropertyStore ();
        private BansheeIconFactory icon_factory = new BansheeIconFactory ();

        private BaseClientWindow primary_window;
        private PrimaryWindowCloseHandler primary_window_close_handler;

        public event EventHandler ThemeChanged;

        public GtkElementsService ()
        {
        }

        private void OnStyleSet (object o, StyleSetArgs args)
        {
            SourceInvalidateIconPixbuf (ServiceManager.SourceManager.Sources);

            // Ugly hack to avoid stupid themes that set this to 0, causing a huge
            // bug when constructing the "add to playlist" popup menu (BGO #524706)
            Gtk.Rc.ParseString ("gtk-menu-popup-delay = 225");

            OnThemeChanged ();
        }

        private void OnPrimaryWindowRealized (object o, EventArgs args)
        {
            if (primary_window != null && primary_window.GdkWindow != null) {
                property_store.Set<IntPtr> ("PrimaryWindow.RawHandle", primary_window.GdkWindow.Handle);
            } else {
                property_store.Remove ("PrimaryWindow.RawHandle");
            }
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

        public BaseClientWindow PrimaryWindow {
            get { return primary_window; }
            set {
                if (primary_window != null) {
                    primary_window.StyleSet -= OnStyleSet;
                    primary_window.Realized -= OnPrimaryWindowRealized;
                }

                primary_window = value;

                if (primary_window != null) {
                    property_store.Set<BaseClientWindow> ("PrimaryWindow", primary_window);

                    primary_window.StyleSet += OnStyleSet;
                    primary_window.Realized += OnPrimaryWindowRealized;
                } else {
                    property_store.Remove ("PrimaryWindow");
                    property_store.Remove ("PrimaryWindow.RawHandle");
                }
            }
        }

        private List<Window> content_windows;
        public IEnumerable<Window> ContentWindows {
            get {
                if (PrimaryWindow != null) {
                    yield return PrimaryWindow;
                }

                if (content_windows != null) {
                    foreach (var window in content_windows) {
                        yield return window;
                    }
                }
            }
        }

        public void RegisterContentWindow (Window window)
        {
            if (content_windows == null) {
                content_windows = new List<Window> ();
            }

            content_windows.Add (window);
        }

        public void UnregisterContentWindow (Window window)
        {
            if (content_windows != null) {
                content_windows.Remove (window);
                if (content_windows.Count == 0) {
                    content_windows = null;
                }
            }
        }

        public PrimaryWindowCloseHandler PrimaryWindowClose {
            get { return primary_window_close_handler; }
            set { primary_window_close_handler = value; }
        }

        public BansheeIconFactory IconFactory {
            get { return icon_factory; }
        }

        PropertyStore IPropertyStoreExpose.PropertyStore {
            get { return property_store; }
        }

        string IService.ServiceName {
            get { return "GtkElementsService"; }
        }
    }
}
