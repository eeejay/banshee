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
using Gnome;
using GConf;

namespace Sonance
{
	public class Core
	{
		private static Core appInstance = null;
		private static int uid;
		
		public static string [] Args = null;
		public System.Threading.Thread MainThread;
		
		public GstPlayer Player; 
		public Program Program;
		public PlayerUI PlayerInterface;
		public Random Random;
		public DecoderRegistry DecoderRegistry;
		
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
			
			if(!Engine.Initialize(Args)) {
				Gtk.Application.Quit();
				ErrorDialog.Run(
					"Sonance could not initialize GStreamer. Please ensure " + 
					"that GStreamer is properly installed and configured. " +
					"Try running gst-register."
				);
				return;
			}
			
			Random = new Random();
			gconfClient = new GConf.Client();
			library = new Library();
			DecoderRegistry = new DecoderRegistry();
			Player = new GstPlayer();
			
			StockIcons.Initialize();
			MainThread = System.Threading.Thread.CurrentThread;
			
			DebugLog.Add("Sonance.Backend.Core Initialized");
		}
		
		public void Shutdown()
		{
			library.TransactionManager.CancelAll();
			library.Db.Close();
			Player.Close();
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
	}
}
