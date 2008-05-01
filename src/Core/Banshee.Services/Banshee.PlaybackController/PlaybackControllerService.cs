//
// PlaybackControllerService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

using Hyena;
using Hyena.Collections;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.MediaEngine;

namespace Banshee.PlaybackController
{
    public class PlaybackControllerService : IRequiredService, ICanonicalPlaybackController, IPlaybackControllerExportable
    {
        private enum Direction
        {
            Next,
            Previous
        }
    
        private IStackProvider<TrackInfo> previous_stack;
        private IStackProvider<TrackInfo> next_stack;
    
        private TrackInfo current_track;
        private TrackInfo changing_to_track;
        private bool raise_started_after_transition = false;
        private bool transition_track_started = false;
        
        //private Random random = new Random ();
    
        private PlaybackShuffleMode shuffle_mode;
        private PlaybackRepeatMode repeat_mode;
        private bool stop_when_finished = false;
        
        private PlayerEngineService player_engine;
        private ITrackModelSource source;
        private ITrackModelSource next_source;
        
        private event PlaybackControllerStoppedHandler dbus_stopped;
        event PlaybackControllerStoppedHandler IPlaybackController.Stopped {
            add { dbus_stopped += value; }
            remove { dbus_stopped -= value; }
        }
        
        public event EventHandler Stopped;
        public event EventHandler SourceChanged;
        public event EventHandler NextSourceChanged;
        public event EventHandler TrackStarted;
        public event EventHandler Transition;
        
        public PlaybackControllerService ()
        {
            InstantiateStacks ();
            
            player_engine = ServiceManager.PlayerEngine;
            player_engine.PlayWhenIdleRequest += OnPlayerEnginePlayWhenIdleRequest;
            player_engine.EventChanged += OnPlayerEngineEventChanged;
        }
        
        protected virtual void InstantiateStacks ()
        {
            previous_stack = new PlaybackControllerDatabaseStack ();
            next_stack = new PlaybackControllerDatabaseStack ();
        }
        
        private void OnPlayerEnginePlayWhenIdleRequest (object o, EventArgs args)
        {
            Next ();
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            switch (args.Event) {
                case PlayerEngineEvent.EndOfStream:
                    if (!StopWhenFinished) {
                        if (RepeatMode == PlaybackRepeatMode.RepeatSingle) {
                            QueuePlayTrack ();
                        } else {
                            Next ();
                        }
                    } else {
                        OnStopped ();
                    }
                    
                    StopWhenFinished = false;
                    break;
                case PlayerEngineEvent.StartOfStream:
                    TrackInfo track = player_engine.CurrentTrack;
                    if (changing_to_track != track && track != null) {
                        CurrentTrack = track;
                    }
                    
                    changing_to_track = null;
                    
                    if (!raise_started_after_transition) {
                        transition_track_started = false;
                        OnTrackStarted ();
                    } else {
                        transition_track_started = true;
                    }
                    break;
            }       
        }
        
        public void First ()
        {
            Source = NextSource;
            // This and OnTransition() below commented out b/c of BGO #524556
            //raise_started_after_transition = true;
            
            if (Source is IBasicPlaybackController) {
                ((IBasicPlaybackController)Source).First ();
            } else {
                ((ICanonicalPlaybackController)this).First ();
            }
            
            //OnTransition ();
        }
        
        public void Next ()
        {
            Next (RepeatMode == PlaybackRepeatMode.RepeatAll);
        }
        
        public void Next (bool restart)
        {
            Source = NextSource;
            raise_started_after_transition = true;

            player_engine.IncrementLastPlayed ();
            
            if (Source is IBasicPlaybackController) {
                ((IBasicPlaybackController)Source).Next (restart);
            } else {
                ((ICanonicalPlaybackController)this).Next (restart);
            }
            
            OnTransition ();
        }

        public void Previous ()
        {
            Previous (RepeatMode == PlaybackRepeatMode.RepeatAll);
        }
        
        public void Previous (bool restart)
        {
            Source = NextSource;
            raise_started_after_transition = true;

            player_engine.IncrementLastPlayed ();
            
            if (Source is IBasicPlaybackController) {
                ((IBasicPlaybackController)Source).Previous (restart);
            } else {
                ((ICanonicalPlaybackController)this).Previous (restart);
            }
            
            OnTransition ();
        }
        
        void ICanonicalPlaybackController.First ()
        {
            if (Source.Count > 0) {
                player_engine.OpenPlay (Source.TrackModel[0]);
            }
        }
        
        void ICanonicalPlaybackController.Next (bool restart)
        {
            TrackInfo tmp_track = CurrentTrack;

            if (next_stack.Count > 0) {
                CurrentTrack = next_stack.Pop ();
                if (tmp_track != null) {
                    previous_stack.Push (tmp_track);
                }
            } else {
                TrackInfo next_track = QueryTrack (Direction.Next, restart);
                if (next_track != null) {
                    if (tmp_track != null) {
                        previous_stack.Push (tmp_track);
                    }
                } else {
                    return;
                }
                
                CurrentTrack = next_track;
            }
            
            QueuePlayTrack ();
        }
        
        void ICanonicalPlaybackController.Previous (bool restart)
        {
            if (CurrentTrack != null && previous_stack.Count > 0) {
                next_stack.Push (current_track);
            }

            if (previous_stack.Count > 0) {
                CurrentTrack = previous_stack.Pop ();
            } else {
                TrackInfo track = CurrentTrack = QueryTrack (Direction.Previous, restart);
                if (track != null) {
                    CurrentTrack = track;
                } else {
                    return;
                }
            }
            
            QueuePlayTrack ();
        }
        
        private TrackInfo QueryTrack (Direction direction, bool restart)
        {
            Log.DebugFormat ("Querying model for track to play in {0}:{1} mode", ShuffleMode, direction);
            return ShuffleMode == PlaybackShuffleMode.Linear
                ? QueryTrackLinear (direction, restart)
                : QueryTrackRandom (restart);
        }
        
        private TrackInfo QueryTrackLinear (Direction direction, bool restart)
        {
            if (Source.TrackModel.Count == 0)
                return null;

            int index = Source.TrackModel.IndexOf (CurrentTrack);
            if (index == -1) {
                return Source.TrackModel[0];
            } else {
                index += (direction == Direction.Next ? 1 : -1);
                if (index >= 0 && index < Source.TrackModel.Count) {
                    return Source.TrackModel[index];
                } else if (!restart) {
                    return null;
                } else if (index < 0) {
                    return Source.TrackModel[Source.TrackModel.Count - 1];
                } else {
                    return Source.TrackModel[0];
                }
            }
        }
        
        private TrackInfo QueryTrackRandom (bool restart)
        {
            return Source.TrackModel.GetRandom (source_set_at, restart);
            //return Source.TrackModel[random.Next (0, Source.TrackModel.Count - 1)];
        }
        
        private void QueuePlayTrack ()
        {
            changing_to_track = CurrentTrack;
            player_engine.OpenPlay (CurrentTrack);
        }

        protected virtual void OnStopped ()
        {
            EventHandler handler = Stopped;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
            
            PlaybackControllerStoppedHandler dbus_handler = dbus_stopped;
            if (dbus_handler != null) {
                dbus_handler ();
            }
        }
        
        protected virtual void OnTransition ()
        {
            EventHandler handler = Transition;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
            
            if (raise_started_after_transition && transition_track_started) {
                OnTrackStarted ();
            }
            
            raise_started_after_transition = false;
            transition_track_started = false;
        }
        
        protected virtual void OnSourceChanged ()
        {
            EventHandler handler = SourceChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected void OnNextSourceChanged ()
        {
            EventHandler handler = NextSourceChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnTrackStarted ()
        {
            EventHandler handler = TrackStarted;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public TrackInfo CurrentTrack {
            get { return current_track; }
            protected set { current_track = value; }
        }
        
        protected DateTime source_set_at;
        public ITrackModelSource Source {
            get { 
                if (source == null && ServiceManager.SourceManager.DefaultSource is ITrackModelSource) {
                    return (ITrackModelSource)ServiceManager.SourceManager.DefaultSource;
                }
                return source;
            }
            
            set {
                if (source != value) {
                    NextSource = value;
                    source = value;
                    source_set_at = DateTime.Now;
                    OnSourceChanged ();
                }
            }
        }
        
        public ITrackModelSource NextSource {
            get { return next_source ?? Source; }
            set {
                if (next_source != value) {
                    next_source = value;
                    OnNextSourceChanged ();
                    
                    if (!player_engine.IsPlaying ()) {
                        Source = next_source;
                    }
                }
            }
        }
        
        public PlaybackShuffleMode ShuffleMode {
            get { return shuffle_mode; }
            set { shuffle_mode = value; }
        }
        
        public PlaybackRepeatMode RepeatMode {
            get { return repeat_mode; }
            set { repeat_mode = value; }
        }
        
        public bool StopWhenFinished {
            get { return stop_when_finished; }
            set { stop_when_finished = value; }
        }
        
        string IService.ServiceName {
            get { return "PlaybackController"; }
        }
        
        IDBusExportable IDBusExportable.Parent {
            get { return null; }
        }
    }
}
