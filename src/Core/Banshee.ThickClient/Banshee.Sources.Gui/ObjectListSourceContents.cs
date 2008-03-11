//
// ObjectListSourceContents.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Gui;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;

namespace Banshee.Sources.Gui
{
    public class ObjectListSourceContents : ScrolledWindow, ISourceContents
    {
        private ObjectListView object_view;
        private ISource source;

        public ObjectListSourceContents () : base ()
        {
            object_view = new ObjectListView ();
            Add (object_view);
            object_view.Show ();
        }

        public void ResetSource ()
        {
            source = null;
            object_view.SetModel (null);
        }

        public bool SetSource (ISource source)
        {
            object_view.SetModel((Hyena.Data.IObjectListModel)source);
            this.source = source;
            return true;
        }

        public ISource Source {
            get { return source; }
        }

        public Widget Widget {
            get { return this; }
        }
    }
}
