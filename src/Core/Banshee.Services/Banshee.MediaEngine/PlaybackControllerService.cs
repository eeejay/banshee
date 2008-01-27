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

using Hyena.Collections;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;

namespace Banshee.MediaEngine
{
    public class PlaybackControllerService : IService, IPlaybackController
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
        private Random random = new Random ();
    
        private PlaybackShuffleMode shuffle_mode;
        private PlaybackRepeatMode repeat_mode;
        private bool stop_when_finished = false;
        
        private PlayerEngineService player_engine;
        private ITrackModelSource source;
        
        public event EventHandler Stopped;
        
        private event PlaybackControllerStoppedHandler dbus_stopped;
        event PlaybackControllerStoppedHandler IPlaybackController.Stopped {
            add { dbus_stopped += value; }
            remove { dbus_stopped -= value; }
        }
        
        public event EventHandler SourceChanged;
        
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
                        Next ();
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
                    break;
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
                TrackInfo next_track = QueryTrack (Direction.Next);
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
        
        public void Previous ()
        {
            if (CurrentTrack != null && previous_stack.Count > 0) {
                next_stack.Push (current_track);
            }

            if (previous_stack.Count > 0) {
                CurrentTrack = previous_stack.Pop ();
            } else {
                CurrentTrack = QueryTrack (Direction.Previous);
            }
            
            QueuePlayTrack ();
        }
        
        private TrackInfo QueryTrack (Direction direction)
        {
            Log.DebugFormat ("Querying model for track to play in {0}:{1} mode", ShuffleMode, direction);
            return ShuffleMode == PlaybackShuffleMode.Linear
                ? QueryTrackLinear (direction)
                : QueryTrackRandom ();
        }
        
        private TrackInfo QueryTrackLinear (Direction direction)
        {
            int index = Source.TrackModel.IndexOf (CurrentTrack);
            return Source.TrackModel[index < 0 ? 0 : index + (direction == Direction.Next ? 1 : -1)];
        }
        
        private TrackInfo QueryTrackRandom ()
        {
            return Source.TrackModel[random.Next (0, Source.TrackModel.Count - 1)];
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
        
        protected virtual void OnSourceChanged ()
        {
            EventHandler handler = SourceChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public TrackInfo CurrentTrack {
            get { return current_track; }
            protected set { current_track = value; }
        }
        
        public ITrackModelSource Source {
            get { 
                if (source == null && ServiceManager.SourceManager.DefaultSource is ITrackModelSource) {
                    return (ITrackModelSource)ServiceManager.SourceManager.DefaultSource;
                }
                return source;
            }
            
            set {
                if (source != value) {
                    source = value;
                    OnSourceChanged ();
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
