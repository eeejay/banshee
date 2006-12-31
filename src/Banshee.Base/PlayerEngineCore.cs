/***************************************************************************
 *  PlayerEngineCore.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Mono.Unix;

using Banshee.MediaEngine;
using Banshee.Plugins;
using Banshee.Configuration;
using Banshee.Metadata;

namespace Banshee.Base
{
    public static class PlayerEngineCore
    {   
        private static PluginFactory<PlayerEngine> factory = new PluginFactory<PlayerEngine>();
        
        private static List<PlayerEngine> engines = new List<PlayerEngine>();
        private static PlayerEngine active_engine;
        private static PlayerEngine default_engine;
        private static PlayerEngine pending_engine;

        private static string preferred_engine_id = null;

        public static event PlayerEngineEventHandler EventChanged;
        public static event PlayerEngineStateHandler StateChanged;

        private static void InstantiateEngines()
        {
            preferred_engine_id = EngineSchema.Get();
            
            factory.PluginLoaded += OnPluginLoaded;
            
            if(Environment.GetEnvironmentVariable("BANSHEE_ENGINES_PATH") != null) {
                factory.AddScanDirectoryFromEnvironmentVariable("BANSHEE_ENGINES_PATH");
            } else {
                factory.AddScanDirectory(Path.Combine(ConfigureDefines.InstallDir, "Banshee.MediaEngine"));
            }
            
            factory.LoadPlugins();
            factory.PluginLoaded -= OnPluginLoaded;
            
            if(default_engine == null && engines.Count > 0) {
                default_engine = engines[0];
            }
            
            if(default_engine != null) {
                active_engine = default_engine;
                LogCore.Instance.PushDebug(Catalog.GetString("Default player engine"), active_engine.Name);
            }
        }
        
        private static void OnPluginLoaded(object o, PluginFactoryEventArgs<PlayerEngine> args)
        {
            PlayerEngine engine = args.Plugin;
            engine.StateChanged += OnEngineStateChanged;
            engine.EventChanged += OnEngineEventChanged;

            if(engine.Id == preferred_engine_id) {
                DefaultEngine = engine;
            } else {
                engines.Add(engine);
            }
        }
        
        public static void Initialize()
        {
            InstantiateEngines();
            if(default_engine == null || active_engine == null || engines == null || engines.Count == 0) {
                throw new ApplicationException(Catalog.GetString(
                    "No player engines were found. Please ensure Banshee has been cleanly installed."));
            }
            
            MultipleMetadataProvider.Instance.HaveResult += OnMetadataProviderHaveResult;
        }

        public static void Dispose()
        {
            /*foreach(PlayerEngine engine in engines) {
                engine.Dispose();
            }*/
            
            factory.Dispose();
        }
        
        private static void OnMetadataProviderHaveResult(object o, MetadataLookupResultArgs args)
        {
            if(CurrentTrack != null && args.Track == CurrentTrack) {
                foreach(StreamTag tag in args.ResultTags) {
                    StreamTagger.TrackInfoMerge(CurrentTrack, tag);
                }
                
                PlayerEngineEventArgs eventargs = new PlayerEngineEventArgs();
                eventargs.Event = PlayerEngineEvent.TrackInfoUpdated;
                OnEngineEventChanged(active_engine, eventargs);
            }
        }
        
        private static void OnEngineStateChanged(object o, PlayerEngineStateArgs args)
        {
            if(o != active_engine) {
                return;
            }
            
            if(args.State == PlayerEngineState.Loaded && CurrentTrack != null) {
                MultipleMetadataProvider.Instance.Lookup(CurrentTrack);
            }
            
            PlayerEngineStateHandler handler = StateChanged;
            if(handler != null) {
                handler(o, args);
            }
        }

        private static void OnEngineEventChanged(object o, PlayerEngineEventArgs args)
        {
            if(o != active_engine) {
                return;
            }
            
            if(args.Event == PlayerEngineEvent.Error 
                && CurrentTrack.PlaybackError == TrackPlaybackError.None) {
                CurrentTrack.PlaybackError = TrackPlaybackError.Unknown;
            } else if(args.Event == PlayerEngineEvent.Iterate 
                && CurrentTrack.PlaybackError != TrackPlaybackError.None) {
                CurrentTrack.PlaybackError = TrackPlaybackError.None;
            }
            
            PlayerEngineEventHandler handler = EventChanged;
            if(handler != null) {
                handler(o, args);
            }
        }
        
        public static void Open(TrackInfo track)
        {
            if(!track.CanPlay) {
                return;
            }
               
            OpenCheck(track);
        }
        
        public static void Open(SafeUri uri)
        {
            OpenCheck(uri);
        }
        
        public static void OpenPlay(TrackInfo track)
        {
            if(!track.CanPlay) {
                return;
            }
        
            try {
                OpenCheck(track);
                active_engine.Play();
            } catch(Exception e) {
                LogCore.Instance.PushError(Catalog.GetString("Problem with Player Engine"), e.Message);
                Close();
                ActiveEngine = default_engine;
            }
        }
        
        private static void OpenCheck(object o)
        {
            SafeUri uri = null;
            TrackInfo track = null;
        
            if(o is SafeUri) {
                uri = o as SafeUri;
            } else if(o is TrackInfo) {
                track = o as TrackInfo;
                uri = track.Uri;
            } else {
                return;
            }
            
            FindSupportingEngine(uri);
            CheckPending();
            
            if(track != null) {
                active_engine.Open(track);
            } else if(uri != null) {
                active_engine.Open(uri);
            }
        }
        
        private static void FindSupportingEngine(SafeUri uri)
        {
            foreach(PlayerEngine engine in engines) {
                foreach(string extension in engine.ExplicitDecoderCapabilities) {
                    if(!uri.AbsoluteUri.EndsWith(extension)) {
                        continue;
                    } else if(active_engine != engine) {
                        Close();
                        pending_engine = engine;
                        Console.WriteLine("Switching engine to: " + engine.GetType());
                    }
                    return;
                }
            }
        
            foreach(PlayerEngine engine in engines) {
                foreach(string scheme in engine.SourceCapabilities) {
                    bool supported = scheme == uri.Scheme;
                    if(supported && active_engine != engine) {
                        Close();
                        pending_engine = engine;
                        Console.WriteLine("Switching engine to: " + engine.GetType());
                        return;
                    } else if(supported) {
                        return;
                    }
                }
            }
        }
        
        public static void Close()
        {
            active_engine.Reset();
            active_engine.Close();
        }
        
        public static void Play()
        {
            active_engine.Play();
        }
        
        public static void Pause()
        {
            active_engine.Pause();
        }
        
        public static void TrackInfoUpdated()
        {
            active_engine.TrackInfoUpdated();
        }

        private static void CheckPending()
        {
            if(pending_engine != null && pending_engine != active_engine) {
                if(active_engine.CurrentState == PlayerEngineState.Idle) {
                    Close();
                }
                
                active_engine = pending_engine;
                pending_engine = null;
            } 
        }
    
        public static TrackInfo CurrentTrack {
            get { return active_engine.CurrentTrack; }
        }
        
        public static SafeUri CurrentSafeUri {
            get { return active_engine.CurrentUri; }
        }
        
        public static PlayerEngineState CurrentState {
            get { return active_engine.CurrentState; }
        }
        
        public static PlayerEngineState LastState {
            get { return active_engine.LastState; }
        }
        
        public static ushort Volume {
            get { return active_engine.Volume; }
            set { 
                foreach(PlayerEngine engine in engines) {
                    engine.Volume = value;
                }
            }
        }
        
        public static uint Position {
            get { return active_engine.Position; }
            set { active_engine.Position = value; }
        }
        
        public static bool CanSeek {
            get { return active_engine.CanSeek; }
        }
        
        public static uint Length {
            get { 
                uint length = active_engine.Length;
                if(length > 0) {
                    return length;
                } else if(active_engine.CurrentTrack == null) {
                    return 0;
                }
                
                return (uint)active_engine.CurrentTrack.Duration.TotalSeconds;
            }
        }
    
        public static PlayerEngine ActiveEngine {
            get { return active_engine; }
            set { pending_engine = value; }
        }
        
        public static PlayerEngine DefaultEngine {
            get { return default_engine; }
            set { 
                if(engines.Contains(value)) {
                    engines.Remove(value);
                }
                
                engines.Insert(0, value);
            
                default_engine = value;
                EngineSchema.Set(value.Id);
            }
        }
        
        public static IEnumerable<PlayerEngine> Engines {
            get { return engines; }
        }
        
        public static readonly SchemaEntry<int> VolumeSchema = new SchemaEntry<int>(
            "player_engine", "volume",
            80,
            "Volume",
            "Volume of playback relative to mixer output"
        );

        public static readonly SchemaEntry<string> EngineSchema = new SchemaEntry<string>(
            "player_engine", "backend",
            "helix-remote",
            "Backend",
            "Name of media playback engine backend"
        );
    }
}
