//
// DapPropertiesDisplay.cs
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
using Cairo;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Sources.Gui;
using Banshee.MediaProfiles.Gui;
using Banshee.Widgets;

using Hyena;
using Hyena.Widgets;

namespace Banshee.Dap.Gui
{
    public class DapPropertiesDisplay : RoundedFrame, ISourceContents
    {
        private DapSource source;
        
        private Gdk.Pixbuf large_icon;
        public Gdk.Pixbuf LargeIcon {
            get {
                if (large_icon == null) {
                    large_icon = SourceIconResolver.ResolveIcon (source, 128);
                }
                return large_icon;
            }
        }
        
        public DapPropertiesDisplay (DapSource source)
        {
            this.source = source;
        }
        
        /*private void BuildPropertyTable ()
        {
            MessagePane pane = new MessagePane ();
            pane.HeaderIcon = 
            pane.HeaderMarkup = String.Format ("<big><b>{0}</b></big>", GLib.Markup.EscapeText (source.Name));
            
            Button properties_button = new Button (String.Format (Catalog.GetString ("{0} Properties"), source.GenericName));
            pane.Append (properties_button);
            
            Add (pane);
        }*/
        
#region ISourceContents
        
        public bool SetSource (ISource src)
        {
            this.source = source as DapSource;
            return this.source != null;
        }

        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Widget Widget {
            get { return this; }
        }
        
#endregion

    }
}
