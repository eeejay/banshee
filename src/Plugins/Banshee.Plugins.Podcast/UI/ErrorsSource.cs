/***************************************************************************
 *  ErrorsSource.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using Gtk;

using Banshee.Base;

namespace Banshee.Sources
{
    public class ErrorsSource : ChildSource
    {
        private Source source;         
        
        private TreeView view;
        private ListStore store;
        private ScrolledWindow scrolled_window;
        
        public ErrorsSource(string name, Source source) : base(name, 50)
        {
            this.source = source;
            this.source.AddChildSource (this);
            
            scrolled_window = new ScrolledWindow();
            scrolled_window.ShadowType = ShadowType.In;
            scrolled_window.VscrollbarPolicy = PolicyType.Automatic;
            scrolled_window.HscrollbarPolicy = PolicyType.Automatic;
            
            view = new TreeView();
            
            scrolled_window.Add(view);
            scrolled_window.ShowAll();
            
            TreeViewColumn message_col = view.AppendColumn(Catalog.GetString("Message"), 
                new CellRendererText(), "text", 0);
            TreeViewColumn file_col = view.AppendColumn(Catalog.GetString("File Name"), 
                new CellRendererText(), "text", 1);
                
            message_col.Resizable = true;
            file_col.Resizable = true;
            
            store = new ListStore(typeof(string), typeof(string), typeof(Exception));
            view.Model = store;
        }
      
        public override int Count {
            get { return store.IterNChildren(); }
        }

        public void AddError(string path, string message, Exception e)
        {
            ThreadAssist.ProxyToMain(delegate {
                store.AppendValues(message, path, e);
                OnUpdated();
            });
        }
        
        public override bool Unmap()
        {
            source.RemoveChildSource(this);
            return true;
        }
        
        public Source Source {
        	get { return source; }
        }        
        
        public override string UnmapLabel {
            get { return Catalog.GetString("Close Error Report"); }
        }

        public override string UnmapIcon {
            get { return Stock.Close; }
        }

        public override bool SearchEnabled {
            get { return false; }
        }
        
        public override Gtk.Widget ViewWidget {
            get { return scrolled_window; }
        }

        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, Gtk.Stock.DialogError);
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
    }
}
