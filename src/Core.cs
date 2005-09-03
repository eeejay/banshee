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
using Mono.Posix;


namespace Banshee
{
	public class Core
	{
		private static Core appInstance = null;

		public static string [] Args = null;
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
                DebugLog.Add("Active Player Engine is now '" + 
                    activePlayer.EngineName + "'");
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
            DebugLog.Add("Loaded PlayerEngine core: " + Player.EngineName);

            if(AudioCdPlayer != null) {
             AudioCdPlayer.Initialize();
             DebugLog.Add("Loaded AudioCdPlayerEngine core: " + 
                 AudioCdPlayer.EngineName);
            }
			
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
		
		public static void ThreadEnter()
		{
			if(!InMainThread)
				Gdk.Threads.Enter();
		}
		
		public static void ThreadLeave()
		{
			if(!InMainThread)
				Gdk.Threads.Leave();
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
}
