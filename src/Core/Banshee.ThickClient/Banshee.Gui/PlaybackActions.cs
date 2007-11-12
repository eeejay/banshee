//
// PlaybackActions.cs
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

using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class PlaybackActions : ActionGroup
    {
        private InterfaceActionService action_service;
        private Action play_pause_action;
        
        public PlaybackActions (InterfaceActionService actionService) : base ("Playback")
        {
            Add (new ActionEntry [] {
                new ActionEntry ("PlayPauseAction", "media-playback-start",
                    Catalog.GetString ("_Play"), "space",
                    Catalog.GetString ("Play or pause the current song"), OnPlayPauseAction),
                    
                new ActionEntry ("NextAction", "media-skip-forward",
                    Catalog.GetString ("_Next"), "N",
                    Catalog.GetString ("Play the next song"), OnNextAction),
                    
                new ActionEntry ("PreviousAction", "media-skip-backward",
                    Catalog.GetString ("Pre_vious"), "B",
                    Catalog.GetString ("Play the previous song"), OnPreviousAction),

                new ActionEntry ("SeekToAction", null,
                    Catalog.GetString ("Seek _to..."), "T",
                    Catalog.GetString ("Seek to a specific location in current song"), OnSeekToAction)
            });
                
            actionService.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("PlaybackMenuAction", null,
                    Catalog.GetString ("_Playback"), null, null, null)
            });
            
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChanged;
            action_service = actionService;
        }
        
        private void OnPlayerEngineStateChanged (object o, PlayerEngineStateArgs args)
        {
            if (play_pause_action == null) {
                play_pause_action = action_service["Playback.PlayPauseAction"];
            }
            
            switch (args.State) {
                case PlayerEngineState.Contacting:
                case PlayerEngineState.Playing:
                    ShowPlayAction ();
                    break;
                case PlayerEngineState.Paused:
                    ShowPlay ();
                    break;
                case PlayerEngineState.Idle:
                    ShowPlay ();
                    break;
                default:
                    break;
            }
        }
        
        private void ShowPlayAction ()
        { 
            if (ServiceManager.PlayerEngine.CanPause) {
                ShowPause ();
            } else {
                ShowPlay ();
            }
        }
        
        private void ShowPause ()
        {
            play_pause_action.Label = Catalog.GetString ("_Pause");
            play_pause_action.StockId = "media-playback-pause";
        }
        
        private void ShowPlay ()
        {
            play_pause_action.Label = Catalog.GetString ("_Play");
            play_pause_action.StockId = "media-playback-start";
        }
                
        private void OnPlayPauseAction (object o, EventArgs args)
        {
            ServiceManager.PlayerEngine.TogglePlaying ();
        }
        
        private void OnNextAction (object o, EventArgs args)
        {
            ServiceManager.PlaybackController.Next ();
        }
        
        private void OnPreviousAction (object o, EventArgs args)
        {
            ServiceManager.PlaybackController.Previous ();
        }

        private void OnSeekToAction (object o, EventArgs args)
        {
            GladeDialog dialog = new SeekDialog ();
            dialog.Run ();
            dialog.Destroy ();
        }
    }
}
