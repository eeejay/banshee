//
// WikipediaView.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Collection;

using Banshee.Gui;

namespace Banshee.Wikipedia
{
    public class WikipediaView : Gtk.ScrolledWindow
    {
        // Translators: this is used for looking up artist pages on Wikipedia; change to your wikipedia language if you want
        private string url_format = Catalog.GetString ("http://en.wikipedia.org/wiki/{0}");

        internal WebKit.WebView view;

        public WikipediaView ()
        {
            view = new WebView ();

            view.ConsoleMessage += delegate (object o, ConsoleMessageArgs args) {
                Log.Debug (args.Message);
            };

            /*view.LoadProgressChanged += delegate (object o, LoadProgressChangedArgs args) {
                Log.DebugFormat ("LoadProgress: {0}", args.Progress);
            };*/

            Add (view);
        }

        private string last_artist;
        public bool SetArtist (string artist)
        {
            if (!String.IsNullOrEmpty (artist) && artist != last_artist) {
                last_artist = artist;
                OpenItem (artist);
                return true;
            }
            return false;
        }

        private void OpenItem (string item)
        {
            OpenUrl (String.Format (url_format, System.Web.HttpUtility.UrlEncode (item.Replace (' ', '_'))));
        }

        private void OpenUrl (string uri)
        {
            Hyena.Log.DebugFormat ("Opening {0}", uri);
            //Open (uri);
            view.ExecuteScript (String.Format ("document.location = \"{0}\";", uri));
        }
    }
}
