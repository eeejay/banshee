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

namespace Sonance
{
	public class PlayerEngineLoader
	{
		private const string RootEngineDir = ConfigureDefines.InstallDir +
			"mediaengines/";	
			
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
		
		public static Type [] FindTypes()
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
		
		public static IPlayerEngine LoadPreferred()
		{
			Type [] types = FindTypes();
			
			if(types == null)
				return null;
				
			return Activator.CreateInstance(types[0]) as IPlayerEngine;
		}
	}
}
