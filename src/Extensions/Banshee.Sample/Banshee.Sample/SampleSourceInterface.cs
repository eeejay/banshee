//
// SampleSourceInterface.cs
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

using Mono.Unix;
using Gtk;

using Banshee.Sources;
using Banshee.Collection;
using Banshee.ServiceStack;

using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.Sample
{
    public class SampleSourceInterface : VBox, ISourceContents
    {
        private SampleSource source;

        public SampleSourceInterface (SampleSource source)
        {
            this.source = source;

            Button button = new Button ("Waiting...");
            button.Show ();

            PackStart (button, true, true, 0);

            ServiceManager.PlayerEngine.TrackIntercept += delegate (TrackInfo track) {
                if (System.IO.Path.GetExtension (track.Uri.LocalPath) != ".wmv") {
                    // We don't care about non wmv URIs, so let the engine take care of it
                    return false;
                }

                // Stop the engine if playing
                ServiceManager.PlayerEngine.Close ();

                // Update our UI and switch to our source
                button.Label = track.ToString ();
                ServiceManager.SourceManager.SetActiveSource (source);

                // Tell the engine that we've handled this track
                return true;
            };
        }

#region ISourceContents

        public bool SetSource (ISource src)
        {
            return source is SampleSource;
        }

        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
        }

        public Widget Widget {
            get { return this; }
        }

#endregion

    }
}
