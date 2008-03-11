//
// ViewActions.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2007-2008 Novell, Inc.
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

using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Equalizer.Gui;

namespace Banshee.Gui
{
    public class ViewActions : BansheeActionGroup
    {
        private InterfaceActionService action_service;
    
        public ViewActions (InterfaceActionService actionService) : base ("View")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("ViewMenuAction", null,
                    Catalog.GetString ("_View"), null, null, null),
                    
                new ActionEntry ("ShowEqualizerAction", null,
                   Catalog.GetString ("_Equalizer"), "<control>E",
                   Catalog.GetString ("View the graphical equalizer"), OnShowEqualizer)
            });
            
            Add (new ToggleActionEntry [] {               
                new ToggleActionEntry ("FullScreenAction", null,
                    Catalog.GetString ("_Fullscreen"), "F11",
                    Catalog.GetString ("Toggle Fullscreen Mode"), null, false),
                
                new ToggleActionEntry ("ShowCoverArtAction", null,
                    Catalog.GetString ("Show Cover _Art"), null,
                    Catalog.GetString ("Toggle display of album cover art"), null, false),
            });
            
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChanged;
            action_service = actionService;
        }
        
        private void OnPlayerEngineStateChanged (object o, PlayerEngineStateArgs args)
        {            
            if (args.State == PlayerEngineState.Ready && !ServiceManager.PlayerEngine.SupportsEqualizer) {
                action_service["View.ShowEqualizerAction"].Sensitive = false;
            }
        }
                
        private void OnShowEqualizer (object o, EventArgs args)
        {
            EqualizerWindow eqwin = new EqualizerWindow ();
            eqwin.Window.Show ();
        }
    }
}
