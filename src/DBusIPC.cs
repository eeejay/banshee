/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DBusIPC.cs
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
using DBus;

namespace Banshee
{
	public class DBusServer
	{
		private static DBusServer instance = null;
		
		private Service service;
			
		public static DBusServer Instance
		{
			get {
				if(instance == null)
					instance = new DBusServer();
				return instance;
			}
		}
		
		public DBusServer()
		{
            service = new Service(Bus.GetSessionBus(), "org.gnome.Banshee");
		}
		
		public void RegisterObject(object o, string path)
		{
			service.RegisterObject(o, path);
		}
		
		public void UnregisterObject(object o)
		{
			service.UnregisterObject(o);
		}
	}
	
	[Interface("org.gnome.Banshee.Core")]
	public class BansheeCore
	{
		private Gtk.Window mainWindow;
		private PlayerUI PlayerUI;
		
		public static BansheeCore FindInstance()
		{
			Connection connection = Bus.GetSessionBus();
			Service service = Service.Get(connection, "org.gnome.Banshee");		
			return (BansheeCore)service.GetObject(typeof(BansheeCore), 
				"/org/gnome/Banshee/Core");
		}
		
		public BansheeCore(Gtk.Window mainWindow, PlayerUI ui)
		{
			this.mainWindow = mainWindow;
			this.PlayerUI = ui;
		}
		
		[Method]
		public virtual void PresentWindow()
		{
			if(mainWindow != null)
				mainWindow.Present();
		}
		
		[Method]
		public virtual void TogglePlaying()
		{
			if(PlayerUI != null)
				PlayerUI.TogglePlaying();
		}
		
		[Method]
		public virtual void Next()
		{
			if(PlayerUI != null)
				PlayerUI.Next();
		}
		
		[Method]
		public virtual void Previous()
		{
			if(PlayerUI != null)
				PlayerUI.Previous();
		}
	}
}

