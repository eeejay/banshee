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
using Gnome;
using GConf;
using Hal;

namespace Banshee
{
	public class Core
	{
		private static Core appInstance = null;
		private static int uid;
		
		public static string [] Args = null;
		public System.Threading.Thread MainThread;
		
		public IPlayerEngine Player; 
		public Program Program;
		public PlayerUI PlayerInterface;
		public Random Random;
		
		public string UserRealName;
		public string UserFirstName;
		
		private Library library;
		private GConf.Client gconfClient;
		private Context halContext;
		
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
		
		public static Context HalContext
		{
			get {
				return Instance.halContext;
			}
		}
		
		public int NextUid
		{
			get {
				return uid++;
			}
		}

		private Core()
		{
			uid = 0;
		
			Gdk.Threads.Init();
			Gtk.Application.Init();

			if(!Directory.Exists(Paths.ApplicationData))
				Directory.CreateDirectory(Paths.ApplicationData);
			
			Random = new Random();
			gconfClient = new GConf.Client();
			library = new Library();
			
			Player = PlayerEngineLoader.SelectedEngine;
			Player.Initialize();

			if(Player == null) {
				Console.Error.WriteLine("Could not load A PlayerEngine Core!");
				System.Environment.Exit(1);
			}
			
			DebugLog.Add("Loaded PlayerEngine core: " + Player.EngineName);
			
			StockIcons.Initialize();
			MainThread = System.Threading.Thread.CurrentThread;
			
			FindUserRealName();
		}
		
		public void ReloadEngine(IPlayerEngine engine)
		{
			if(Player != null) {
				Player.Shutdown();
				Player = null;
			}
			
			Player = engine;
			Player.Initialize();
		}
		
		public void Shutdown()
		{
			library.TransactionManager.CancelAll();
			library.Db.Close();
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
		
		private void FindUserRealName()
		{
			try {
				FileStream file = File.OpenRead("/etc/passwd");
				StreamReader reader = new StreamReader(file);
				string line;
				while((line = reader.ReadLine()) != null) {
					if(!line.StartsWith(Environment.UserName + ":"))
						continue;
						
					string [] parts = line.Split(':');
					UserRealName = parts[4].Trim();
					
					parts = UserRealName.Split(' ');
					UserFirstName = parts[0].Replace(',', ' ').Trim();
					UserFirstName += UserFirstName.EndsWith("s") ? "'" : "'s"; 
				}
				reader.Close();
				file.Close();
			} catch(Exception) { }
		}
	}
}
