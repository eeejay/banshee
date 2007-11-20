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

using Banshee.Base;
using Banshee.Streaming;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class GlobalActions : ActionGroup
    {
        public GlobalActions (InterfaceActionService actionService) : base ("Global")
        {
            Add (new ActionEntry [] {
                // Music Menu
                new ActionEntry ("MusicMenuAction", null, 
                    Catalog.GetString ("_Music"), null, null, null),
                
                new ActionEntry ("ImportMusicAction", Stock.Open,
                    Catalog.GetString ("Import _Music..."), "<control>I",
                    Catalog.GetString ("Import music from a variety of sources"), OnImportMusic),
                    
                new ActionEntry ("OpenLocationAction", null, 
                    Catalog.GetString ("Open _Location..."), "<control>L",
                    Catalog.GetString ("Open a remote location for playback"), OnOpenLocationAction),
                    
                new ActionEntry ("QuitAction", Stock.Quit,
                    Catalog.GetString ("_Quit"), "<control>Q",
                    Catalog.GetString ("Quit Banshee"), OnQuit),

                // Edit Menu
                new ActionEntry("EditMenuAction", null, 
                    Catalog.GetString("_Edit"), null, null, null),
                
                // Help Menu
                new ActionEntry ("HelpMenuAction", null, 
                    Catalog.GetString ("_Help"), null, null, null),
                
                new ActionEntry ("WebMenuAction", null,
                    Catalog.GetString ("_Web Resources"), null, null, null),
                    
                new ActionEntry ("WikiGuideAction", Stock.Help,
                    Catalog.GetString ("Banshee _User Guide (Wiki)"), null,
                    Catalog.GetString ("Learn about how to use Banshee"), delegate {
                        Banshee.Web.Browser.Open ("http://banshee-project.org/Guide");
                    }),
                    
                new ActionEntry ("WikiAction", null,
                    Catalog.GetString ("Banshee _Home Page"), null,
                    Catalog.GetString ("Visit the Banshee Home Page"), delegate {
                        Banshee.Web.Browser.Open ("http://banshee-project.org/");
                    }),
                    
                new ActionEntry ("WikiDeveloperAction", null,
                    Catalog.GetString ("_Get Involved"), null,
                    Catalog.GetString ("Become a contributor to Banshee"), delegate {
                        Banshee.Web.Browser.Open ("http://banshee-project.org/Developers");
                    }),
                 
                new ActionEntry ("VersionInformationAction", null,
                    Catalog.GetString ("_Version Information..."), null,
                    Catalog.GetString ("View detailed version and configuration information"), OnVersionInformation),
                    
                new ActionEntry("AboutAction", "gtk-about", OnAbout)
            });
        }
            
#region Music Menu Actions
        
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
        
        private void OnOpenLocationAction (object o, EventArgs args)
        {
            OpenLocationDialog dialog = new OpenLocationDialog ();
            ResponseType response = dialog.Run ();
            string address = dialog.Address;
            dialog.Destroy ();
            
            if(response != ResponseType.Ok) {
                return;
            }
            
            try {
                RadioTrackInfo radio_track = new RadioTrackInfo (new SafeUri (address));
                radio_track.ParsingPlaylistEvent += delegate {
                    if (radio_track.PlaybackError != StreamPlaybackError.None) {
                        Log.Error (Catalog.GetString ("Error opening stream"), 
                            Catalog.GetString ("Could not open stream or playlist"));
                        radio_track = null;
                    }
                };
                radio_track.Play ();
            } catch {
                Log.Error (Catalog.GetString ("Error opening stream"), 
                    Catalog.GetString("Problem parsing playlist"));
            }
        }
        
        private void OnQuit (object o, EventArgs args)
        {
            Banshee.ServiceStack.Application.Shutdown ();
        }
        
#endregion
        
#region Help Menu Actions
        
        private void OnVersionInformation (object o, EventArgs args)
        {
            Hyena.Gui.Dialogs.VersionInformationDialog dialog = new Hyena.Gui.Dialogs.VersionInformationDialog ();
            dialog.Run ();
            dialog.Destroy ();
        }
        
        private void OnAbout (object o, EventArgs args)
        {
            Banshee.Gui.Dialogs.AboutDialog dialog = new Banshee.Gui.Dialogs.AboutDialog ();
            dialog.Run ();
            dialog.Destroy ();
        }

#endregion
            
    }
}
