//
// GlobalActions.cs
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
using Mono.Unix;
using Gtk;

namespace Banshee.Gui
{
    public class GlobalActions : ActionGroup
    {
        public GlobalActions (InterfaceActionService actionService) : base ("Global")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("MusicMenuAction", null, 
                    Catalog.GetString ("_Music"), null, null, null),
                
                new ActionEntry ("ImportMusicAction", Stock.Open,
                    Catalog.GetString ("Import _Music..."), "<control>I",
                    Catalog.GetString ("Import music from a variety of sources"), OnImportMusic),
                    
                new ActionEntry ("QuitAction", Stock.Quit,
                    Catalog.GetString ("_Quit"), "<control>Q",
                    Catalog.GetString ("Quit Banshee"), OnQuit)
            });
        }
            
        private void OnImportMusic (object o, EventArgs args)
        {
            Banshee.Library.Gui.ImportDialog dialog = new Banshee.Library.Gui.ImportDialog ();            
            try {
                if (dialog.Run () != Gtk.ResponseType.Ok) {
                    return;
                }
                    
                dialog.ActiveSource.Import ();
            } finally {
                dialog.Destroy ();
            }
        }
        
        private void OnQuit (object o, EventArgs args)
        {
            Banshee.ServiceStack.Application.Shutdown ();
        }
    }
}
