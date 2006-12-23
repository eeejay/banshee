/***************************************************************************
 *  PluginCore.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Collections.Generic;
using System.Reflection;
using Mono.Unix;
using Hal;

using Banshee.Base;
using Banshee.Configuration;

using Boo.Lang.Compiler;
using Boo.Lang.Compiler.IO;
using Boo.Lang.Compiler.Pipelines;
using Boo.Lang.Interpreter;

namespace Banshee.Plugins
{
    public static class PluginCore 
    {
        private static PluginFactory<Plugin> factory = new PluginFactory<Plugin>();
    
        public static void Initialize()
        {
            if(Environment.GetEnvironmentVariable("BANSHEE_PLUGINS_DISABLE") != null) {
                return;
            }
        
            if(Environment.GetEnvironmentVariable("BANSHEE_PLUGINS_PATH") != null) {
                factory.AddScanDirectoryFromEnvironmentVariable("BANSHEE_PLUGINS_PATH");
            } else {
                factory.AddScanDirectory(Paths.UserPluginDirectory);
                factory.AddScanDirectory(Paths.SystemPluginDirectory);
            }
            
            try {
                Directory.CreateDirectory(Paths.UserPluginDirectory);
            } catch(Exception e) {
                LogCore.Instance.PushError("Could not create plugins directory",
                    Paths.UserPluginDirectory + "\nPlugins will not be loaded\n" + e, false);
                return;
            }
            
            factory.LoadPluginFromType(typeof(Banshee.SmartPlaylist.SmartPlaylistCore));
            factory.LoadPlugins();
            
            InitializeScripts();
            InitializePlugins();
        }
        
        private static void InitializePlugins()
        {
            List<Plugin> to_remove = new List<Plugin>();
            
            foreach(Plugin plugin in factory) {
                try {
                    if(plugin.GetType().ToString() == "Banshee.Plugins.SmartPlaylists.Plugin") {
                        plugin.Dispose();
                        to_remove.Add(plugin);
                        continue;
                    }
                    
                    if(ConfigurationClient.Get<bool>(plugin.ConfigurationNamespace, "enabled", false)) {
                        plugin.Initialize();
                    }
                } catch(GConf.NoSuchKeyException) {
                }
            }
            
            foreach(Plugin plugin in to_remove) {
                factory.RemovePlugin(plugin);
            }
        }
        
        private static void InitializeScripts()
        {
            try {
                foreach(string file in Directory.GetFiles(Path.Combine(
                    Paths.ApplicationData, "scripts"), "*.boo")) {
                    RunBooScript(file);
                } 
            } catch {
            }
        }
        
        private static void RunBooScript(string file)
        {
            BooCompiler compiler = new BooCompiler();
            
            compiler.Parameters.Input.Add(new FileInput(file));
            compiler.Parameters.Pipeline = new CompileToMemory();
            compiler.Parameters.Ducky = true;

            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                compiler.Parameters.References.Add(assembly);
            }
            
            CompilerContext context = compiler.Run();
            
            if(context.GeneratedAssembly != null) {
                try {
                    Type script_module = context.GeneratedAssembly.GetTypes()[0];
                    if(script_module == null) {
                        BooScriptOutput(file, "Could not find module in script");
                    } else {
                        MethodInfo main_entry = script_module.GetMethod("Main");
                        if(main_entry == null) {
                            factory.LoadPluginsFromAssembly(context.GeneratedAssembly);
                        } else {
                            main_entry.Invoke(null, null);
                        }
                    }
                } catch(Exception e) {
                    BooScriptOutput(file, e.ToString());
                }
            } else {
                foreach(CompilerError error in context.Errors) {
                    BooScriptOutput(file, error.ToString());
                }
            }
        }
        
        private static void BooScriptOutput(string file, string output)
        {
            Console.WriteLine("BooCompiler: {0}: {1}", Path.GetFileName(file), output);
        }
        
        public static void Dispose()
        {
            factory.Dispose();
        }
        
        public static void ShowPluginDialog()
        {
            PluginDialog dialog = new PluginDialog();
            dialog.Run();
            dialog.Destroy();
        }
        
        public static PluginFactory<Plugin> Factory {
            get { return factory; }
        }
    }
}
