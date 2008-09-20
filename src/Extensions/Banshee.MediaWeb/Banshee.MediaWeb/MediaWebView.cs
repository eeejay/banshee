//
// MediaWebView.cs
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

using Hyena;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.MediaWeb
{
    public class MediaWebView : WebKit.WebView, ISourceContents
    {
        ISource Banshee.Sources.Gui.ISourceContents.Source {
            get {
                return null;
            }
        }

        Widget Banshee.Sources.Gui.ISourceContents.Widget {
            get {
                return this;
            }
        }

        public MediaWebView () : base ()
        {
            this.ConsoleMessage += delegate (object o, ConsoleMessageArgs args)
            {
                Log.Debug (args.Message);
            };

            this.LoadProgressChanged += delegate (object o, LoadProgressChangedArgs args)
            {
                Log.DebugFormat ("LoadProgress: {0}", args.Progress);
            };
            
            //this.LoadHtmlString ("<html><body><i>fooooo!</i></body></html>", "/");
            //this.MarkTextMatches ("fo", false, 999);
        }

        private void OpenUrl (string uri)
        {
            Hyena.Log.DebugFormat ("MediaWeb.View Opening {0}", uri);
            //Open (uri);
            ExecuteScript (String.Format ("document.location = \"{0}\";", uri));
            //LoadHtmlString ("<b>hi!!</b>", "");
        }

        bool Banshee.Sources.Gui.ISourceContents.SetSource (ISource source)
        {
            //OpenUrl ("http://www.miroguide.com/");
            OpenUrl ("http://www.hulu.com/watch/24057/the-daily-show-with-jon-stewart-tue-jun-24-2008#s-p1-so-i0");
            return true;
        }

        void Banshee.Sources.Gui.ISourceContents.ResetSource ()
        {
        }
    }
}
