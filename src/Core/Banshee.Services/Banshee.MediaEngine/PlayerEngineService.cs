//
// PlayerEngineService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.IO;
using System.Collections.Generic;
using System.Reflection;

using Mono.Unix;
using Mono.Addins;

using Hyena;
using Banshee.Base;
using Banshee.Streaming;
using Banshee.ServiceStack;
using Banshee.Metadata;
using Banshee.Configuration;
using Banshee.Collection;
using Banshee.Equalizer;

namespace Banshee.MediaEngine
{
    public class PlayerEngineService : IService, IPlayerEngineService
    {   
        private List<PlayerEngine> engines = new List<PlayerEngine> ();
        private PlayerEngine active_engine;
        private PlayerEngine default_engine;
        private PlayerEngine pending_engine;

        private string preferred_engine_id = null;

        public event PlayerEngineEventHandler EventChanged;
        public event PlayerEngineStateHandler StateChanged;
        public event EventHandler PlayWhenIdleRequest;
        
        private event DBusPlayerEngineEventHandler dbus_event_changed;
        event DBusPlayerEngineEventHandler IPlayerEngineService.EventChanged {
            add { dbus_event_changed += value; }
            remove { dbus_event_changed -= value; }
        }

        private event DBusPlayerEngineStateHandler dbus_state_changed;
        event DBusPlayerEngineStateHandler IPlayerEngineService.StateChanged {
            add { dbus_state_changed += value; }
            remove { dbus_state_changed -= value; }
        }
        
        public PlayerEngineService ()
        {
            preferred_engine_id = EngineSchema.Get();
            
            if (default_engine == null && engines.Count > 0) {
                default_engine = engines[0];
            }
            
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Banshee/PlayerEngines/PlayerEngine")) {
                LoadEngine (node);
            }
            
            if (default_engine != null) {
                active_engine = default_engine;
                Log.Debug (Catalog.GetString ("Default player engine"), active_engine.Name);
            } else {
                default_engine = active_engine;
            }
            
            if (default_engine == null || active_engine == null || engines == null || engines.Count == 0) {
                Log.Warning (Catalog.GetString (
                    "No player engines were found. Please ensure Banshee has been cleanly installed."),
                    "Using the featureless NullPlayerEngine.");
                PlayerEngine null_engine = new NullPlayerEngine ();
                LoadEngine (null_engine);
                active_engine = null_engine;
                default_engine = null_engine;
            }
            
            MetadataService.Instance.HaveResult += OnMetadataServiceHaveResult;
        }
        
        private void LoadEngine (TypeExtensionNode node)
        {
            LoadEngine ((PlayerEngine) node.CreateInstance (typeof (PlayerEngine)));
        }
        
        private void LoadEngine (PlayerEngine engine)
        {
            engine.StateChanged += OnEngineStateChanged;
            engine.EventChanged += OnEngineEventChanged;

            if(engine.Id == preferred_engine_id) {
                DefaultEngine = engine;
            } else {
                if (active_engine == null) {
                    active_engine = engine;
                }
                engines.Add (engine);
            }
        }

        public void Dispose ()
        {
        }
        
        private void OnMetadataServiceHaveResult (object o, MetadataLookupResultArgs args)
        {
            if (CurrentTrack != null && args.Track == CurrentTrack) {
                foreach (StreamTag tag in args.ResultTags) {
                    StreamTagger.TrackInfoMerge(CurrentTrack, tag);
                }
                
                PlayerEngineEventArgs eventargs = new PlayerEngineEventArgs ();
                eventargs.Event = PlayerEngineEvent.TrackInfoUpdated;
                OnEngineEventChanged (active_engine, eventargs);
            }
        }
        
        private void OnEngineStateChanged (object o, PlayerEngineStateArgs args)
        {
            if (o != active_engine) {
                return;
            }
            
            if (args.State == PlayerEngineState.Loaded && CurrentTrack != null) {
                active_engine.Volume = (ushort) VolumeSchema.Get ();
                MetadataService.Instance.Lookup (CurrentTrack);
            } else if (args.State == PlayerEngineState.Initalized) {
                // Enable our preferred equalizer if it exists and was enabled last time.
                if (this.SupportsEqualizer && EqualizerSetting.EnabledSchema.Get ()) {
                    string name = EqualizerSetting.PresetSchema.Get();
                    
                    if (name != "") {
                        // Don't use EqualizerManager.Instance - used by the eq dialog window.
                        EqualizerManager manager = new EqualizerManager (EqualizerManager.Instance.Path);
                        manager.Load ();
                        EqualizerSetting equalizer = null;
                        foreach (EqualizerSetting eq in manager) {
                            if (eq.Name == name) {
                                equalizer = eq;
                                break;
                            }
                        }
                        
                        if (equalizer != null) {
                            Log.DebugFormat ("Enabling equalizer preset: {0}", equalizer.Name);
                            manager.Enable (equalizer);
                        }
                    }
                }
            }
            
            PlayerEngineStateHandler handler = StateChanged;
            if (handler != null) {
                handler (o, args);
            }
            
            DBusPlayerEngineStateHandler dbus_handler = dbus_state_changed;
            if (dbus_handler != null) {
                dbus_handler (args.State.ToString ().ToLower ());
            }
        }

        private void OnEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            if (o != active_engine) {
                return;
            }
            
            if (CurrentTrack != null) {
                if (args.Event == PlayerEngineEvent.Error 
                    && CurrentTrack.PlaybackError == StreamPlaybackError.None) {
                    CurrentTrack.PlaybackError = StreamPlaybackError.Unknown;
                } else if (args.Event == PlayerEngineEvent.Iterate 
                    && CurrentTrack.PlaybackError != StreamPlaybackError.None) {
                    CurrentTrack.PlaybackError = StreamPlaybackError.None;
                }
            }
            
            PlayerEngineEventHandler handler = EventChanged;
            if (handler != null) {
                handler(o, args);
            }
            
            // Do not raise iterate across DBus to avoid so many calls;
            // DBus clients should do their own iterating and 
            // event/state checking locally
            if (args.Event == PlayerEngineEvent.Iterate) {
                return;
            }
            
            DBusPlayerEngineEventHandler dbus_handler = dbus_event_changed;
            if (dbus_handler != null) {
                dbus_handler (args.Event.ToString ().ToLower (), args.Message ?? String.Empty, args.BufferingPercent);
            }
        }
        
        private void OnPlayWhenIdleRequest ()
        {
            EventHandler handler = PlayWhenIdleRequest;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public void Open (TrackInfo track)
        {
            if (!track.CanPlay) {
                return;
            }
               
            OpenCheck (track);
        }
        
        public void Open (SafeUri uri)
        {
            OpenCheck (uri);
        }
        
        void IPlayerEngineService.Open (string uri)
        {
            OpenCheck (new SafeUri (uri));
        }
        
        public void OpenPlay (TrackInfo track)
        {
            if (!track.CanPlay) {
                return;
            }
        
            try {
                OpenCheck (track);
                active_engine.Play ();
            } catch (Exception e) {
                Log.Error (Catalog.GetString ("Problem with Player Engine"), e.Message);
                Close ();
                ActiveEngine = default_engine;
            }
        }
        
        private void OpenCheck (object o)
        {
            SafeUri uri = null;
            TrackInfo track = null;
        
            if (o is SafeUri) {
                uri = o as SafeUri;
            } else if (o is TrackInfo) {
                track = o as TrackInfo;
                uri = track.Uri;
            } else {
                return;
            }
            
            FindSupportingEngine (uri);
            CheckPending ();
            
            if (track != null) {
                active_engine.Open (track);
            } else if (uri != null) {
                active_engine.Open (uri);
            }
        }
        
        private void FindSupportingEngine (SafeUri uri)
        {
            foreach (PlayerEngine engine in engines) {
                foreach (string extension in engine.ExplicitDecoderCapabilities) {
                    if (!uri.AbsoluteUri.EndsWith (extension)) {
                        continue;
                    } else if (active_engine != engine) {
                        Close ();
                        pending_engine = engine;
                        Log.DebugFormat ("Switching engine to: {0}", engine.GetType ());
                    }
                    return;
                }
            }
        
            foreach (PlayerEngine engine in engines) {
                foreach (string scheme in engine.SourceCapabilities) {
                    bool supported = scheme == uri.Scheme;
                    if (supported && active_engine != engine) {
                        Close ();
                        pending_engine = engine;
                        Log.DebugFormat ("Switching engine to: {0}", engine.GetType ());
                        return;
                    } else if (supported) {
                        return;
                    }
                }
            }
        }
        
        public void Close ()
        {
            active_engine.Reset ();
            active_engine.Close ();
        }
        
        public void Play ()
        {
            active_engine.Play ();
        }
        
        public void Pause ()
        {
            if (!CanPause) {
                Close ();
            } else {
                active_engine.Pause ();
            }
        }
        
        public void TogglePlaying ()
        {
            switch (CurrentState) {
                case PlayerEngineState.Idle:
                    OnPlayWhenIdleRequest ();
                    break;
                case PlayerEngineState.Playing:
                    Pause ();
                    break;
                default:
                    Play ();
                    break;
            }
        }
        
        public void TrackInfoUpdated ()
        {
            active_engine.TrackInfoUpdated ();
        }
        
        public bool IsPlaying (TrackInfo track)
        {
            return CurrentState != PlayerEngineState.Idle && track.AudiblyEqual (CurrentTrack);
        }

        private void CheckPending ()
        {
            if(pending_engine != null && pending_engine != active_engine) {
                if(active_engine.CurrentState == PlayerEngineState.Idle) {
                    Close ();
                }
                
                active_engine = pending_engine;
                pending_engine = null;
            } 
        }
    
        public TrackInfo CurrentTrack {
            get { return active_engine.CurrentTrack; }
        }
        
        IDictionary<string, object> IPlayerEngineService.CurrentTrack {
            get { return CurrentTrack == null ? null : CurrentTrack.GenerateExportable (); }
        }
        
        public SafeUri CurrentSafeUri {
            get { return active_engine.CurrentUri; }
        }
        
        string IPlayerEngineService.CurrentUri {
            get { return CurrentSafeUri == null ? String.Empty : CurrentSafeUri.AbsoluteUri; }
        }
        
        public PlayerEngineState CurrentState {
            get { return active_engine.CurrentState; }
        }
        
        string IPlayerEngineService.CurrentState {
            get { return CurrentState.ToString ().ToLower (); }
        }
        
        public PlayerEngineState LastState {
            get { return active_engine.LastState; }
        }
        
        string IPlayerEngineService.LastState {
            get { return LastState.ToString ().ToLower (); }
        }
        
        public ushort Volume {
            get { return active_engine.Volume; }
            set { 
                foreach (PlayerEngine engine in engines) {
                    engine.Volume = value;
                }
            }
        }
        
        public uint Position {
            get { return active_engine.Position; }
            set { active_engine.Position = value; }
        }
        
        public bool CanSeek {
            get { return active_engine.CanSeek; }
        }
        
        public bool CanPause {
            get { return CurrentTrack != null && !CurrentTrack.IsLive; }
        }
        
        public bool SupportsEqualizer {
            get { return ((active_engine is IEqualizer) && active_engine.SupportsEqualizer); }
        }
        
        public uint Length {
            get { 
                uint length = active_engine.Length;
                if (length > 0) {
                    return length;
                } else if (active_engine.CurrentTrack == null) {
                    return 0;
                }
                
                return (uint) active_engine.CurrentTrack.Duration.TotalSeconds;
            }
        }
    
        public PlayerEngine ActiveEngine {
            get { return active_engine; }
            set { pending_engine = value; }
        }
        
        public PlayerEngine DefaultEngine {
            get { return default_engine; }
            set { 
                if (engines.Contains (value)) {
                    engines.Remove (value);
                }
                
                engines.Insert (0, value);
            
                default_engine = value;
                EngineSchema.Set (value.Id);
            }
        }
        
        public IEnumerable<PlayerEngine> Engines {
            get { return engines; }
        }
        
        string IService.ServiceName {
            get { return "PlayerEngine"; }
        }
        
        IDBusExportable IDBusExportable.Parent { 
            get { return null; }
        }
        
        public static readonly SchemaEntry<int> VolumeSchema = new SchemaEntry<int> (
            "player_engine", "volume",
            80,
            "Volume",
            "Volume of playback relative to mixer output"
        );

        public static readonly SchemaEntry<string> EngineSchema = new SchemaEntry<string> (
            "player_engine", "backend",
            "helix-remote",
            "Backend",
            "Name of media playback engine backend"
        );
    }
}
