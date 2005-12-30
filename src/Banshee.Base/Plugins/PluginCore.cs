/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PluginCore.cs
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
using Mono.Unix;
using Hal;

using Banshee.Base;

namespace Banshee.Plugins
{
    public static class PluginCore 
    {
        private static ArrayList plugins = new ArrayList();
    
        public static void Initialize()
        {
            string [] directories = {
                Paths.UserPluginDirectory,
                Paths.SystemPluginDirectory
            };
            
            foreach(string directory in directories) {
                LoadPluginsFromDirectory(directory, typeof(Plugin));
            }
        }
        
        private static void LoadPluginsFromDirectory(string directory, Type type)
        {
            try {
                FileInfo [] files = (new DirectoryInfo(directory)).GetFiles("*.dll");
                if(files == null || files.Length == 0) {
                    return;
                }

                foreach(FileInfo file in files) {
                    try {
                        LoadPluginsFromAssembly(Assembly.LoadFrom(file.FullName), type);
                    } catch {
                    }
                }
            } catch {
            }
        }
        
        private static void LoadPluginsFromAssembly(Assembly assembly, Type pluginType)
        {
            foreach(Type type in assembly.GetTypes()) {
                if(!type.IsSubclassOf(pluginType)) {
                    continue;
                }
                
                bool already_loaded = false;
                
                foreach(Plugin plugin in Plugins) {
                    if(plugin.GetType() == type) {
                        already_loaded = true;
                        break;
                    }
                }
                
                if(already_loaded) {
                    LogCore.Instance.PushWarning("Plugin with same type already loaded", 
                        type.FullName, false);
                    continue;
                }
                
                try {
                    Plugin plugin = (Plugin)Activator.CreateInstance(type);
                    plugin.Initialize();
                    plugins.Add(plugin);
                } catch(TargetInvocationException te) {
                    try {
                        throw te.InnerException;
                    } catch(InvalidPluginException e) {
                        // An InvalidPluginException will only be thrown if the *design* of the plugin
                        // is improper, thus only the plugin developer will see this exception.
                        // We force an exit to make the design error more obvious.
                        Console.WriteLine("** Invalid Plugin Design **");
                        Console.WriteLine(e.Message);
                        System.Environment.Exit(1);
                    } catch {
                    }
                } catch {
                }
            }
        }
        
        public static void Dispose()
        {
            foreach(Plugin plugin in Plugins) {
                plugin.Dispose();
            }
        }
        
        public static ICollection Plugins {
            get {
                return plugins;
            }
        }
    }
}
