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

using Hyena.Data.Gui;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Sources;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public class PlaybackActions : BansheeActionGroup
    {
        private InterfaceActionService action_service;
        private Gtk.Action play_pause_action;
        private PlaybackRepeatActions repeat_actions;

        public PlaybackRepeatActions RepeatActions {
            get { return repeat_actions; }
        }
        
        public PlaybackActions (InterfaceActionService actionService) : base ("Playback")
        {
            repeat_actions = new PlaybackRepeatActions (actionService);

            Add (new ActionEntry [] {
                new ActionEntry ("PlayPauseAction", null,
                    Catalog.GetString ("_Play"), "space",
                    Catalog.GetString ("Play or pause the current item"), OnPlayPauseAction),
                    
                new ActionEntry ("NextAction", null,
                    Catalog.GetString ("_Next"), "N",
                    Catalog.GetString ("Play the next item"), OnNextAction),
                    
                new ActionEntry ("PreviousAction", null,
                    Catalog.GetString ("Pre_vious"), "B",
                    Catalog.GetString ("Play the previous item"), OnPreviousAction),

                new ActionEntry ("SeekToAction", null,
                    Catalog.GetString ("Seek _to..."), "T",
                    Catalog.GetString ("Seek to a specific location in current item"), OnSeekToAction),

                new ActionEntry ("JumpToPlayingTrackAction", null,
                    Catalog.GetString("_Jump to Playing Song"), "<control>J",
                    Catalog.GetString ("Jump to the currently playing item"), OnJumpToPlayingTrack),
                
                new ActionEntry ("RestartSongAction", null,
                    Catalog.GetString ("_Restart Song"), "R",
                    Catalog.GetString ("Restart the current item"), OnRestartSongAction)
            });
            
            Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ShuffleAction", null,
                    Catalog.GetString ("Shu_ffle"), null,
                    Catalog.GetString ("Toggle between shuffle or continuous playback modes"), 
                    OnShuffleAction, ShuffleEnabled.Get ()),
                    
                new ToggleActionEntry ("StopWhenFinishedAction", null,
                    Catalog.GetString ("_Stop When Finished"), "<Shift>space",
                    Catalog.GetString ("Stop playback after the current item finishes playing"), 
                    OnStopWhenFinishedAction, false)
            });
            
            actionService.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("PlaybackMenuAction", null,
                    Catalog.GetString ("_Playback"), null, null, null),
            });
            
            ServiceManager.PlaybackController.ShuffleMode = ShuffleEnabled.Get () 
                ? PlaybackShuffleMode.Shuffle
                : PlaybackShuffleMode.Linear;

            this["JumpToPlayingTrackAction"].Sensitive = false;
            this["RestartSongAction"].Sensitive = false;
            this["SeekToAction"].Sensitive = false;
            
            this["PlayPauseAction"].IconName = "media-playback-start";
            this["NextAction"].IconName = "media-skip-forward";
            this["PreviousAction"].IconName = "media-skip-backward";
            this["ShuffleAction"].IconName = "media-playlist-shuffle";
            
            action_service = actionService;
            ServiceManager.PlayerEngine.StateChanged += OnPlayerEngineStateChanged;
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
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
            
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            switch (args.Event) {
                case PlayerEngineEvent.StartOfStream:
                    TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack; 
                    this["SeekToAction"].Sensitive = !track.IsLive;
                    this["RestartSongAction"].Sensitive = !track.IsLive;
                    this["RestartSongAction"].Label = (track.MediaAttributes & TrackMediaAttributes.VideoStream) == 0
                        ? Catalog.GetString ("_Restart Song")
                        : Catalog.GetString ("_Restart Video");

                    this["JumpToPlayingTrackAction"].Sensitive = true;
                    this["JumpToPlayingTrackAction"].Label = (track.MediaAttributes & TrackMediaAttributes.VideoStream) == 0
                        ? Catalog.GetString ("_Jump to Playing Song")
                        : Catalog.GetString ("_Jump to Playing Video");
                    break;

                case PlayerEngineEvent.Error:
                case PlayerEngineEvent.EndOfStream:
                    this["JumpToPlayingTrackAction"].Sensitive = false;
                    this["RestartSongAction"].Sensitive = false;
                    this["SeekToAction"].Sensitive = false;

                    ToggleAction stop_action = (ToggleAction) this["StopWhenFinishedAction"];
                    // Kinda lame, but we don't want to actually reset StopWhenFinished inside the controller
                    // since it is also listening to EOS and needs to actually stop playback; we listen here
                    // just to keep the UI in sync.
                    stop_action.Activated -= OnStopWhenFinishedAction;
                    stop_action.Active = false;
                    stop_action.Activated += OnStopWhenFinishedAction;
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
            
        private void OnRestartSongAction (object o, EventArgs args)
        {
            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
            if (track != null) {
                ServiceManager.PlayerEngine.Close ();
                ServiceManager.PlayerEngine.OpenPlay (track);
            }
        }
        
        private void OnStopWhenFinishedAction (object o, EventArgs args)
        {
            ServiceManager.PlaybackController.StopWhenFinished = ((ToggleAction)o).Active;
        }

        private void OnJumpToPlayingTrack (object o, EventArgs args)
        {
            Source playback_src = ServiceManager.PlaybackController.Source as Source;

            if (playback_src != null && playback_src is ITrackModelSource) {
                ITrackModelSource track_src = (playback_src as ITrackModelSource);
                int i = track_src.TrackModel.IndexOf (ServiceManager.PlaybackController.CurrentTrack);
                if (i != -1) {
                    // TODO clear the search/filters if there are any, since they might be hiding the currently playing item?
                    // and/or switch to the track's primary source?  what if it's been removed from the library all together?
                    IListView<TrackInfo> track_list = (track_src as Source).Properties.Get<IListView<TrackInfo>> ("Track.IListView");
                    if (track_list != null) {
                        ServiceManager.SourceManager.SetActiveSource (playback_src);
                        track_src.TrackModel.Selection.Clear (false);
                        track_src.TrackModel.Selection.Select (i);
                        track_list.CenterOn (i);
                    }
                }
            }
        }
        
        private void OnShuffleAction (object o, EventArgs args)
        {
            ServiceManager.PlaybackController.ShuffleMode = ((ToggleAction)o).Active 
                ? PlaybackShuffleMode.Shuffle
                : PlaybackShuffleMode.Linear;
            
            ShuffleEnabled.Set ((o as ToggleAction).Active);
        }
        
        public static readonly SchemaEntry<bool> ShuffleEnabled = new SchemaEntry<bool> (
            "playback", "shuffle",
            false,
            "Shuffle playback",
            "Enable shuffle mode"
        );
    }
}
