/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Core.cs
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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Gnome;
using GConf;
using Mono.Unix;

using Banshee.FileSystemMonitor;
using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee
{
    public class Core
    {
        private static Core appInstance = null;

        public static string [] Args = null;
        public static ArgumentQueue ArgumentQueue = null;
        
        public IPlayerEngine activePlayer;
        public IPlayerEngine PreferredPlayer;
        public IPlayerEngine AudioCdPlayer;
        
        public Program Program;
        public PlayerUI PlayerInterface;
        public Random Random;
        public DBusServer dbusServer;
        
        public AudioCdCore AudioCdCore;
        
        public string UserRealName;
        public string UserFirstName;
        
        private Library library;
        private Watcher fs_watcher;
        
        public static Core Instance
        {
            get {
                if(appInstance == null)
                    appInstance = new Core();
                    
                return appInstance;
            }
        }
        
        public static bool IsInstantiated 
        {
            get {
                return appInstance != null;
            }
        }
        
        public static GConf.Client GconfClient
        {
            get {
                return Globals.Configuration;
            }
        }
        
        public static Library Library
        {
            get {
                return Instance.library;
            }
        }
        
        public DBusServer DBusServer
        {
            get { 
                return dbusServer;
            }
        }

        public IPlayerEngine Player
        {
            get {
                return activePlayer;
            }
            
            set {
                activePlayer = value;
                Core.Log.PushDebug("Changed active player engine", activePlayer.EngineName);
            }
        }
        
        public static LogCore Log
        {
            get {
                return LogCore.Instance;
            }
        }

        private Core()
        {
            Gtk.Application.Init();

            Gstreamer.Initialize();

            dbusServer = new DBusServer();

            if(!Directory.Exists(Paths.ApplicationData))
                Directory.CreateDirectory(Paths.ApplicationData);

            Random = new Random();
            library = new Library();

            Globals.LibraryLocation = library.Location;

            Player = PlayerEngineLoader.SelectedEngine;
            PreferredPlayer = Player;
            
            foreach(IPlayerEngine engine in PlayerEngineLoader.Engines) {
                if(engine.ConfigName == "gstreamer") {
                    AudioCdPlayer = engine;
                    break;
                }
            }

            if(Player == null) {
                Console.Error.WriteLine("Could not load A PlayerEngine Core!");
                System.Environment.Exit(1);
            }

            Player.Initialize();
            Core.Log.PushDebug("Loaded PlayerEngine core", Player.EngineName);

            if(AudioCdPlayer == null) {
                Console.Error.WriteLine("Could not load AudioCdPlayer!");
                System.Environment.Exit(1);
            }

            AudioCdPlayer.Initialize();
            
            Core.Log.PushDebug("Loaded AudioCdPlayerEngine core", AudioCdPlayer.EngineName);
            
            try {
                AudioCdCore = new AudioCdCore();
            } catch(ApplicationException e) {
                Core.Log.PushWarning("Audio CD support will be disabled for this instance", e.Message, false);
            }
            
            try {
                Banshee.Dap.DapCore.Initialize();
            } catch(ApplicationException e) {
                Core.Log.PushWarning("DAP support will be disabled for this instance", e.Message, false);
            }

            StockIcons.Initialize();
            
            try {
                if((bool)Globals.Configuration.Get(GConfKeys.EnableFileSystemMonitoring)) {
                    fs_watcher = new Watcher(Globals.Configuration.Get(GConfKeys.LibraryLocation) as string);
                } 
            } catch(Exception e) {
                Core.Log.PushWarning("File System Monitoring will be disabled for this instance", e.Message, false);
            }
        }
        
        public void ReloadEngine(IPlayerEngine engine)
        {
            if(Player != null) {
                Player.Dispose();
                Player = null;
            }
            
            Player = engine;
            Player.Initialize();
        }
        
        public void LoadCdPlayer()
        {
            if(Player == AudioCdPlayer)
                return;
                
            if(AudioCdPlayer == null)
                throw new ApplicationException(Catalog.GetString(
                    "CD Playback is not supported in your Banshee Setup"));
                
            Player = AudioCdPlayer;
        }            
        
        public void UnloadCdPlayer()
        {
            if(Player == PreferredPlayer)
                return;
            
            
            Player = PreferredPlayer;
        }
        
        public void Shutdown()
        {
            if(fs_watcher != null) {
                fs_watcher.Dispose();
            }
            
            library.TransactionManager.CancelAll();
            library.Db.Close();
            
            HalCore.Dispose();
            Banshee.Dap.DapCore.Dispose();
        }
    }
}
