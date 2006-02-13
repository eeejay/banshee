
/***************************************************************************
 *  PlayerEngineLoader.cs
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
using System.Reflection;
using System.Collections;
using System.IO;

using Banshee.Base;

namespace Banshee.MediaEngine
{
	public class PlayerEngineSlot
	{
		public IPlayerEngine Engine;
		public bool Disabled;
	}

	public class PlayerEngineLoader
	{
		private const string RootEngineDir = ConfigureDefines.InstallDir +
			"Banshee.MediaEngine/";
			
		private static PlayerEngineSlot [] engineSlots;
			
		private static string [] FindEngineDirs()
		{
			DirectoryInfo info = new DirectoryInfo(RootEngineDir);
			if(!info.Exists)
				return null;
			
			ArrayList dirs = new ArrayList();
			
			foreach(DirectoryInfo sinfo in info.GetDirectories()) 
				dirs.Add(sinfo.FullName + "/"); 
				
			return dirs.ToArray(typeof(string)) as string [];
		}
		
		private static Assembly [] FindEngineAssemblies()
		{
			string [] engineDirs = FindEngineDirs();
			
			if(engineDirs == null)
				return null;
		
			ArrayList assemblies = new ArrayList();
		
			foreach(string dir in engineDirs) {
				DirectoryInfo di = new DirectoryInfo(dir);
				if(!di.Exists)
					continue;
					
				foreach(FileInfo file in di.GetFiles()) {
					if(file.Extension != ".dll")
						continue;
						
					try {
						Assembly asm = Assembly.LoadFrom(file.FullName);
						assemblies.Add(asm);
					} catch(Exception) { }
				}
			}
			
			if(assemblies.Count == 0)
				return null;
				
			return assemblies.ToArray(typeof(Assembly)) as Assembly [];
		}
		
		private static Type FindPlayerEngineType(Assembly asm)
		{
			foreach(Type t in asm.GetTypes()) {
				foreach(Type it in t.GetInterfaces()) {
					if(it.Name.Equals("IPlayerEngine")) {
						return t;
					}
				}
			}
			
			return null;
		}
		
		private static Type [] FindTypes()
		{
			Assembly [] assemblies = FindEngineAssemblies();
			
			if(assemblies == null)
				return null;
				
			ArrayList engines = new ArrayList();
				
			foreach(Assembly asm in assemblies) {
				Type playerEngineType = FindPlayerEngineType(asm);
				if(playerEngineType != null)
					engines.Add(playerEngineType);
			}
			
			if(engines.Count == 0)
				return null;
			
			return engines.ToArray(typeof(Type)) as Type [];
		}
		
		public static PlayerEngineSlot [] LoadEngines()
		{
			Type [] types = FindTypes();
			
			if(engineSlots != null)
				return engineSlots;
			
			engineSlots = new PlayerEngineSlot[types.Length];
			
			for(int i = 0; i < types.Length; i++) {
				engineSlots[i] = new PlayerEngineSlot();
				engineSlots[i].Engine = Activator.CreateInstance(types[i]) 
					as IPlayerEngine;
				try {
					engineSlots[i].Engine.TestInitialize();
				} catch(Exception e) {
					engineSlots[i].Disabled = true;
					Console.WriteLine(
						"Player Engine `{0}' failed init tests... disabling ({1})",
						engineSlots[i].Engine.EngineLongName, e.Message);
				}
			}
			
			return engineSlots;
		}
		
		public static IPlayerEngine SelectedEngine
		{
			get {
				LoadEngines();
				
				if(engineSlots.Length == 0)
					return null;
			
				GConf.Client gconf = new GConf.Client();
				
				try {
					string selected = gconf.Get(GConfKeys.PlayerEngine) 
						as string;
					for(int i = 0; i < engineSlots.Length; i++) {
						if(engineSlots[i].Engine.ConfigName.Equals(selected) 
							&& !engineSlots[i].Disabled)
							return engineSlots[i].Engine;
					}
				} catch(Exception) {}
				
				gconf.Set(GConfKeys.PlayerEngine, 
					engineSlots[0].Engine.ConfigName);
				return engineSlots[0].Disabled ? null : engineSlots[0].Engine;
			}
		}
		
		public static IPlayerEngine [] Engines
		{
			get {
				ArrayList engines = new ArrayList();
				foreach(PlayerEngineSlot slot in engineSlots)
					if(!slot.Disabled)
						engines.Add(slot.Engine);
				return engines.ToArray(typeof(IPlayerEngine)) 
					as IPlayerEngine [];
			}
		}
	}
}
