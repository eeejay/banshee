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

using Banshee.Logging;

namespace Banshee
{
    public class Core
    {
        private static Core appInstance = null;

        public static string [] Args = null;
        public static ArgumentQueue ArgumentQueue = null;
        public System.Threading.Thread MainThread;
        
        public IPlayerEngine activePlayer;
        public IPlayerEngine PreferredPlayer;
        public IPlayerEngine AudioCdPlayer;
        
        public Program Program;
        public PlayerUI PlayerInterface;
        public Random Random;
        public DBusServer dbusServer;
        
        public AudioCdCore AudioCdCore;
        public IpodCore IpodCore;
        
        public string UserRealName;
        public string UserFirstName;
        
        private Library library;
        private GConf.Client gconfClient;
        
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
                return Instance.gconfClient;
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
            Gdk.Threads.Init();
            Gtk.Application.Init();

            Gstreamer.Initialize();

            dbusServer = new DBusServer();

            if(!Directory.Exists(Paths.ApplicationData))
                Directory.CreateDirectory(Paths.ApplicationData);

            Random = new Random();
            gconfClient = new GConf.Client();
            library = new Library();

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
            
            AudioCdCore = new AudioCdCore();
            IpodCore = new IpodCore();

            StockIcons.Initialize();
            MainThread = System.Threading.Thread.CurrentThread;

            FindUserRealName();
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
            library.TransactionManager.CancelAll();
            library.Db.Close();
            AudioCdCore.Dispose();
            IpodCore.Dispose();
        }
        
        public static bool InMainThread
        {
            get {
                return Core.Instance.MainThread.Equals(
                    System.Threading.Thread.CurrentThread);
            }
        }
        
        public static void ProxyToMainThread(EventHandler handler)
        {
            if(!InMainThread) {
                Gtk.Application.Invoke(handler);
            } else {
                handler(null, new EventArgs());
            }
        }
        
        [DllImport("libglib-2.0.so")]
        static extern IntPtr g_get_real_name();

        private void FindUserRealName()
        {
            try {
                UserRealName = GLib.Marshaller.Utf8PtrToString(g_get_real_name());
                string[] parts = UserRealName.Split(' ');
                UserFirstName = parts[0].Replace(',', ' ').Trim();
            } catch(Exception) { }
        }
    }
    
    public class UidGenerator
    {
        private static int uid = 0;
        
        public static int Next
        {
            get {
                return ++uid;
            }
        }
    
    }

    public class ArgumentLayout
    {
        public string Name, ValueKind, Description;
        
        public ArgumentLayout(string name, string description) : this(name, 
            null, description)
        {
    
        }
        
        public ArgumentLayout(string name, string valueKind, string description)
        {
            Name = name;
            ValueKind = valueKind;
            Description = description;
        }
    }

    public class ArgumentQueue : IEnumerable
    {
        private ArgumentLayout [] availableArgs; 
        private Hashtable args = new Hashtable();

        public ArgumentQueue(ArgumentLayout [] availableArgs, string [] args)
        {
            this.availableArgs = availableArgs;
        
            for(int i = 0; i < args.Length; i++) {
                string arg = null, val = String.Empty;
                
                if(args[i].StartsWith("--")) {
                    arg = args[i].Substring(2);
                }

                if(i < args.Length - 1) {
                    if(!args[i + 1].StartsWith("--")) {
                        val = args[i + 1];
                        i++;
                    }
                }

                if(arg != null) {
                    Enqueue(arg, val);
                }
            }
        }

        public void Enqueue(string arg)
        {
            Enqueue(arg, String.Empty);
        }

        public void Enqueue(string arg, string val)
        {
            args.Add(arg, val);
        }

        public void Dequeue(string arg)
        {
            args.Remove(arg);
        }

        public bool Contains(string arg)
        {
            return args[arg] != null;
        }

        public string this [string arg]
        {
            get {
                return args[arg] as string;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return args.GetEnumerator();
        }

        public string [] Arguments 
        {
            get {
                ArrayList list = new ArrayList(args.Keys);
                return list.ToArray(typeof(string)) as string [];
            }
        }

        public ArgumentLayout [] AvailableArguments
        {
            get {
                return availableArgs;
            }
        }
    }
}
