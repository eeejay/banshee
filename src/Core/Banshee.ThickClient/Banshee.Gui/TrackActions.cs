//
// TrackActions.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
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
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class TrackActions : ActionGroup
    {
        private InterfaceActionService action_service;
        private IHasTrackSelection selection_provider;

        private static readonly string [] require_selection_actions = new string [] {
            "TrackPropertiesAction"
        };
        
        public TrackActions (InterfaceActionService actionService, IHasTrackSelection selectionProvider) : base ("Track")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("TrackPropertiesAction", Stock.Edit,
                    Catalog.GetString ("_Track Properties"), null,
                    Catalog.GetString ("Edit metadata on selected songs"), OnTrackProperties)
            });

            action_service = actionService;
            selection_provider = selectionProvider;
            selection_provider.TrackSelection.Changed += HandleSelectionChanged;

            Sensitize ();
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            Sensitize ();
        }

        private void Sensitize ()
        {
            bool has_selection = selection_provider.TrackSelection.Count > 0;
            foreach (string action in require_selection_actions)
                this [action].Sensitive = has_selection;
        }
            
        private void OnTrackProperties (object o, EventArgs args)
        {
            Console.WriteLine ("In OnTrackPropertiesAction");
            foreach (TrackInfo track in selection_provider.GetSelectedTracks ()) {
                Console.WriteLine ("Have selected track: {0}", track.TrackTitle);
            }

            TrackEditor propEdit = new TrackEditor (selection_provider.GetSelectedTracks ());
            propEdit.Saved += delegate {
                //ui.playlistView.QueueDraw();
            };
        }
    }
}
