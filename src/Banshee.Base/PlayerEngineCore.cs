/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlayerEngineCore.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Collections;
using Mono.Unix;

using Banshee.MediaEngine;

namespace Banshee.Base
{
    public static class PlayerEngineCore
    {
        private static IPlayerEngine active_player;
        private static IPlayerEngine preferred_player;
        private static IPlayerEngine audio_cd_player;
    
        public static void Initialize()
        {
            ActivePlayer = PlayerEngineLoader.SelectedEngine;
            preferred_player = ActivePlayer;
            
            foreach(IPlayerEngine engine in PlayerEngineLoader.Engines) {
                if(engine.ConfigName == "gstreamer") {
                    audio_cd_player = engine;
                    break;
                }
            }
            
            if(ActivePlayer == null) {
                Console.Error.WriteLine("Could not load A PlayerEngine Core!");
                System.Environment.Exit(1);
            }

            ActivePlayer.Initialize();
            LogCore.Instance.PushDebug("Loaded primary playback engine", ActivePlayer.EngineName);

            if(AudioCdPlayer == null) {
                Console.Error.WriteLine("Could not load AudioCdPlayer as the GStreamer backend could not be " +
                    "loaded. This is probably a problem with GStreamer. Try running gst-register-0.8");
                System.Environment.Exit(1);
            }

            AudioCdPlayer.Initialize();
            
            LogCore.Instance.PushDebug("Loaded Audio CD playback engine", AudioCdPlayer.EngineName);
        }
    
        public static void ReloadEngine(IPlayerEngine engine)
        {
            if(ActivePlayer == engine) {
                return;
            }
        
            if(ActivePlayer != null) {
                ActivePlayer.Dispose();
            }
            
            ActivePlayer = engine;
            ActivePlayer.Initialize();
        }
        
        public static void LoadEngineByExtension(string extension)
        {
            UnloadCdPlayer();
            
            foreach(IPlayerEngine engine in PlayerEngineLoader.Engines) {
                if(engine.SupportedExtensions == null) {
                    continue;
                }
                
                foreach(string engine_ext in engine.SupportedExtensions) {
                    if(engine_ext == extension) {
                        Console.WriteLine("Loading " + engine.EngineName);
                        ReloadEngine(engine);
                        return;
                    }
                }       
            }
            
            Console.WriteLine("Keeping existing engine " + ActivePlayer.EngineName);
        }
        
        public static void LoadCdPlayer()
        {
            if(ActivePlayer == AudioCdPlayer) {
                return;
            }
                
            if(AudioCdPlayer == null) {
                throw new ApplicationException(Catalog.GetString("CD playback is not supported for this instance"));
            }
            
            ActivePlayer = AudioCdPlayer;
        }            
        
        public static void UnloadCdPlayer()
        {
            if(ActivePlayer == PreferredPlayer) {
                return;
            }
            
            ActivePlayer = PreferredPlayer;
        }
    
        public static IPlayerEngine ActivePlayer {
            get {
                return active_player;
            }
            
            set {
                active_player = value;
                LogCore.Instance.PushDebug("Changed active playback engine", active_player.EngineName);
            }
        }
        
        public static IPlayerEngine PreferredPlayer {
            get {
                return preferred_player;
            } 
            
            set {
                preferred_player = value;
            }
        }
        
        public static IPlayerEngine AudioCdPlayer {
            get {
                return audio_cd_player;
            }
        }
    }
}
