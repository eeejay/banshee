//
// MediaWebSource.cs
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
using Mono.Unix;
using Gtk;

using WebKit;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.MediaWeb
{
    public class MediaWebSource : Source, IDisposable
    {
        protected override string TypeUniqueId {
            get { return "media-web-source"; }
        }

        private MediaWebView view;
        
        // FIXME avoiding new translated strings, for the moment
        //public MediaWebSource () : base (Catalog.GetString ("Media Web"), Catalog.GetString ("Media Web"), 30)
        public MediaWebSource () : base ("Media Web", "Media Web", 30)
        {
            view = new MediaWebView ();
        
            Properties.SetString ("Icon.Name", "applications-internet");
            Properties.Set<ISourceContents> ("Nereid.SourceContents", view);
            Properties.Set<bool> ("Nereid.SourceContents.HeaderVisible", false);
            //Properties.SetString ("ActiveSourceUIResource", "ActiveSourceUI.xml");
            
            ServiceManager.SourceManager.AddSource (this);
        }
        
        public void Dispose ()
        {
            if (view != null) {
                view.Destroy ();
                view.Dispose ();
                view = null;
            }
        }
        
        public override void Activate ()
        {
            /*if (now_playing_interface != null) {
                now_playing_interface.OverrideFullscreen ();
            }*/
        }

        public override void Deactivate ()
        {
            /*if (now_playing_interface != null) {
                now_playing_interface.RelinquishFullscreen ();
            }*/
        }
    }
}
