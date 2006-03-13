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
using System.Collections;
using System.Reflection;
using Mono.Unix;

using Banshee.MediaEngine;

namespace Banshee.Base
{
    public static class PlayerEngineCore
    {
        private const string RootEngineDir = ConfigureDefines.InstallDir + "Banshee.MediaEngine/";   
        
        private static ArrayList engines = new ArrayList();
        private static PlayerEngine active_engine;
        private static PlayerEngine default_engine;
        private static PlayerEngine pending_engine;

        public static event PlayerEngineEventHandler EventChanged;
        public static event PlayerEngineStateHandler StateChanged;

        private static void InstantiateEngines()
        {
            DirectoryInfo info = new DirectoryInfo(RootEngineDir);
            
            if(!info.Exists) {
                throw new IOException("Directory " + RootEngineDir + " does not exist");
            }
            
            string preferred_id = null;
            try {
                preferred_id = (string)Globals.Configuration.Get(GConfKeys.PlayerEngine);
            } catch {
            }
            
            foreach(DirectoryInfo sub_info in info.GetDirectories()) {
                DirectoryInfo directory_info = new DirectoryInfo(sub_info.FullName + "/");
                if(!directory_info.Exists) {
                    continue;
                }
                
                foreach(FileInfo file_info in directory_info.GetFiles("*.dll")) {
                    Assembly assembly = Assembly.LoadFrom(file_info.FullName);
                    foreach(Type type in assembly.GetTypes()) {
                        if(!type.IsSubclassOf(typeof(PlayerEngine))) {
                            continue;
                        }

                        PlayerEngine engine = (PlayerEngine)Activator.CreateInstance(type);
                        engine.StateChanged += OnEngineStateChanged;
                        engine.EventChanged += OnEngineEventChanged;

                        if(engine.Id == preferred_id) {
                            default_engine = engine;
                        }

                        engines.Add(engine);
                    }
                }
            }
            
            if(default_engine == null && engines.Count > 0) {
                default_engine = engines[0] as PlayerEngine;
            }
            
            active_engine = default_engine;
            
            LogCore.Instance.PushDebug(Catalog.GetString("Default player engine"), active_engine.Name);
        }
        
        public static void Initialize()
        {
            try {
                InstantiateEngines();
                if(default_engine == null || active_engine == null || engines == null || engines.Count == 0) {
                    throw new ApplicationException("No player engines found");
                }
            } catch(Exception e) {
                Console.Error.WriteLine("Cannot load any PlayerEngine:\n\n{0}\n", e);
                System.Environment.Exit(1);
            }   
        }

        public static void Dispose()
        {
            foreach(PlayerEngine engine in engines) {
                engine.Dispose();
            }
        }

        private static void OnEngineStateChanged(object o, PlayerEngineStateArgs args)
        {
            if(o != active_engine) {
                return;
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
            
            PlayerEngineEventHandler handler = EventChanged;
            if(handler != null) {
                handler(o, args);
            }
        }
        
        public static void Open(TrackInfo track)
        {
            CheckPending();   
            active_engine.Open(track);
        }
        
        public static void Open(Uri uri)
        {
            CheckPending();
            active_engine.Open(uri);
        }
        
        public static void OpenPlay(TrackInfo track)
        {
            CheckPending();
            active_engine.Open(track);
            active_engine.Play();
        }
        
        public static void Close()
        {
            active_engine.Close();
            active_engine.Reset();
        }
        
        public static void Play()
        {
            active_engine.Play();
        }
        
        public static void Pause()
        {
            active_engine.Pause();
        }

        private static void CheckPending()
        {
            if(pending_engine != null && pending_engine != active_engine) {
                if(active_engine.CurrentState == PlayerEngineState.Idle) {
                    active_engine.Close();
                }
                
                active_engine = pending_engine;
                pending_engine = null;
            } 
        }
    
        public static TrackInfo CurrentTrack {
            get { return active_engine.CurrentTrack; }
        }
        
        public static Uri CurrentUri {
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
            set { active_engine.Volume = value; }
        }
        
        public static uint Position {
            get { return active_engine.Position; }
            set { active_engine.Position = value; }
        }
        
        public static uint Length {
            get { return active_engine.Length; }
        }
    
        public static PlayerEngine ActiveEngine {
            get { return active_engine; }
            set { pending_engine = value; }
        }
        
        public static PlayerEngine DefaultEngine {
            get { return default_engine; }
            set { 
                default_engine = value;
                Globals.Configuration.Set(GConfKeys.PlayerEngine, value.Id);
            }
        }
        
        public static IEnumerable Engines {
            get { return engines; }
        }
    }
}
