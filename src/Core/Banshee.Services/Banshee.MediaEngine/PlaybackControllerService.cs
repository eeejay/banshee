//
// PlaybackControllerService.cs
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

using Hyena;

using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;

namespace Banshee.MediaEngine
{
    public class PlaybackControllerService : IService
    {
        private IStackProvider<TrackInfo> previous_stack;
        private IStackProvider<TrackInfo> next_stack;
    
        private int index = 0;
        private TrackInfo current_track;
        private Random random = new Random ();
    
        private PlaybackShuffleMode shuffle_mode;
        private PlaybackRepeatMode repeat_mode;
        
        private PlayerEngineService player_engine;
        private SourceManager source_manager;
        private ITrackModelSource source;
        
        public PlaybackControllerService ()
        {
            InstantiateStacks ();
        
            source_manager = ServiceManager.SourceManager;
            source_manager.ActiveSourceChanged += OnActiveSourceChanged;
            
            player_engine = ServiceManager.PlayerEngine;
            player_engine.PlayWhenIdleRequest += OnPlayerEnginePlayWhenIdleRequest;
            player_engine.EventChanged += OnPlayerEngineEventChanged;
        }
        
        protected virtual void InstantiateStacks ()
        {
            previous_stack = new PlaybackControllerDatabaseStack ();
            next_stack = new PlaybackControllerDatabaseStack ();
        }
        
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            if (args.Source is ITrackModelSource) {
                Source = (ITrackModelSource)args.Source;
            }
        }
        
        private void OnPlayerEnginePlayWhenIdleRequest (object o, EventArgs args)
        {
            Next ();
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            switch (args.Event) {
                case PlayerEngineEvent.EndOfStream:
                    //ToggleAction action = Globals.ActionManager["StopWhenFinishedAction"] as ToggleAction;
                    
                    //if(!action.Active) {
                        Next ();
                    //} else {
                        //OnStopped ();
                    //}
                    
                    break;
                    //action.Active = false;
            }       
        }
        
        public void Next ()
        {
            TrackInfo tmp_track = CurrentTrack;

            if (next_stack.Count > 0) {
                CurrentTrack = next_stack.Pop ();
                if (tmp_track != null) {
                    previous_stack.Push (tmp_track);
                }
            } else {
                TrackInfo next_track = QueryTrack ();
                if (next_track != null) {
                    if (tmp_track != null) {
                        previous_stack.Push (tmp_track);
                    }
                }
                
                CurrentTrack = next_track;
            }
            
            QueuePlayTrack ();
        }
        
        public void Previous ()
        {
            if (CurrentTrack != null && previous_stack.Count > 0) {
                next_stack.Push (current_track);
            }

            if (previous_stack.Count > 0) {
                CurrentTrack = previous_stack.Pop ();
            }
            
            QueuePlayTrack ();
        }
        
        private TrackInfo QueryTrack ()
        {
            if (ShuffleMode == PlaybackShuffleMode.Linear) {
                return source.TrackModel.GetValue (index++);
            } else {
                return source.TrackModel.GetValue (random.Next (0, source.TrackModel.Rows - 1));
            }
        }
        
        private void QueuePlayTrack ()
        {
            player_engine.OpenPlay (CurrentTrack);
        }
        
        public TrackInfo CurrentTrack {
            get { return current_track; }
            protected set { current_track = value; }
        }
        
        public ITrackModelSource Source {
            get { return source; }
            set { source = value; }
        }
        
        public PlaybackShuffleMode ShuffleMode {
            get { return shuffle_mode; }
            set { shuffle_mode = value; }
        }
        
        public PlaybackRepeatMode RepeatMode {
            get { return repeat_mode; }
            set { repeat_mode = value; }
        }
        
        string IService.ServiceName {
            get { return "PlaybackControllerService"; }
        }
    }
}
