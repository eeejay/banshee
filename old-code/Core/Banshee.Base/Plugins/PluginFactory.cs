/***************************************************************************
 *  PluginFactory.cs
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

using Banshee.Base;

namespace Banshee.Plugins
{
    public enum PluginFactoryType
    {
        Type,
        Instance
    }
    
    public delegate void GenericEventHandler<U, V>(U o, V args);
    
    public class PluginFactoryEventArgs<T> where T : IPlugin
    {
        private T plugin;
        private Type type;
        
        public PluginFactoryEventArgs(T plugin)
        {
            this.plugin = plugin;
            this.type = plugin.GetType();
        }
        
        public PluginFactoryEventArgs(Type type)
        {
            this.type = type;
        }
        
        public T Plugin {
            get { return plugin; }
        }
        
        public Type Type {
            get { return type; }
        }
    }

    public class PluginFactory<T> : IDisposable, IEnumerable<T>, IEnumerable where T : IPlugin
    {
        private List<T> plugin_instances = new List<T>();
        private List<Type> plugin_types = new List<Type>();
        private List<DirectoryInfo> scan_directories = new List<DirectoryInfo>();
        private List<string> exclude_masks = new List<string>();
        private string include_mask = "*.dll";
        private PluginFactoryType factory_type;

        public event GenericEventHandler<PluginFactory<T>, PluginFactoryEventArgs<T>> PluginLoaded;
        
        public PluginFactory() : this(PluginFactoryType.Instance)
        {
        }
        
        public PluginFactory(PluginFactoryType factoryType)
        {
            factory_type = factoryType;
        }
        
        public void Dispose()
        {
            foreach(T t in this) {
                t.Dispose();
            }
        }
        
        public void RemovePlugin(T plugin)
        {
            plugin_instances.Remove(plugin);
            RemovePlugin(plugin.GetType());
        }
        
        public void RemovePlugin(Type type)
        {
            plugin_types.Remove(type);
        }

        public void AddScanDirectory(DirectoryInfo directory)
        {
            AddScanDirectory(directory, false);
        }
        
        public void AddScanDirectory(DirectoryInfo directory, bool recurse)
        {        
            if(directory == null || !directory.Exists) {
                return;
            }
            
            scan_directories.Add(directory);
            
            if(!recurse) {
                return;
            }
            
            foreach(DirectoryInfo sub_directory in directory.GetDirectories()) {
                AddScanDirectory(sub_directory, recurse);
            }
        }
        
        public void AddScanDirectory(string directory)
        {
            AddScanDirectory(directory, false);
        }
        
        public void AddScanDirectory(string directory, bool recurse)
        {
            AddScanDirectory(new DirectoryInfo(directory), recurse);
        }
        
        public void AddScanDirectoryFromEnvironmentVariable(string env)
        {
            string env_path = Environment.GetEnvironmentVariable(env);
            if(env_path == null || env_path == String.Empty) {
                return;
            }
            
            try {
                AddScanDirectory(new DirectoryInfo(env_path), true);
            } catch {
            }
        }
        
        public void LoadPlugins()
        {
            foreach(DirectoryInfo directory in scan_directories) {
                LoadPluginsFromDirectory(directory);
            }
        }
        
        public void LoadPluginsFromDirectory(DirectoryInfo directory)
        {
            try {
                foreach(FileInfo file in directory.GetFiles(include_mask)) {
                    LoadPluginsFromFile(file);
                }
            } catch(DirectoryNotFoundException) {
                try {
                    directory.Create();
                } catch { 
                }
            }
        }
        
        public void LoadPluginsFromFile(FileInfo file)
        {
            foreach(string exclude in exclude_masks) {
                if(file.FullName.ToLower().Contains(exclude)) {
                    return;
                }
            }
            
            LoadPluginsFromAssembly(Assembly.LoadFrom(file.FullName));
        }
        
        public void LoadPluginsFromAssembly(Assembly assembly)
        {
            Type [] check_types = null;
            List<Type> non_entry_types = null;
            
            try {
                check_types = ReflectionUtil.ModuleGetTypes(assembly, "PluginModuleEntry");
            } catch {
            }
            
            if(check_types == null) {
                check_types = assembly.GetTypes();
                non_entry_types = new List<Type>();
            }
        
            foreach(Type type in check_types) {
                if(!type.IsSubclassOf(typeof(T)) || type.IsAbstract) {
                    continue;
                }
                
                bool already_loaded = false;
                
                foreach(Type loaded_type in plugin_types) {
                    if(loaded_type == type) {
                        already_loaded = true;
                        break;
                    }
                }
                
                if(already_loaded) {
                    continue;
                }
                
                if(non_entry_types != null) {
                    non_entry_types.Add(type);
                }
                
                LoadPluginFromType(type);
            }
            
            if(non_entry_types != null && non_entry_types.Count > 0) {
                Console.WriteLine(
                    "Plugin module: {0}\n" +
                    "Does not implement PluginModuleEntry.GetTypes. For faster startup performance\n" +
                    "and to lower memory consumption, it is recommended that the following code\n" +
                    "be added to the plugin module:\n",
                    assembly.Location);
                
                Console.WriteLine("public static class PluginModuleEntry");
                Console.WriteLine("{");
                Console.WriteLine("    public static Type [] GetTypes()");
                Console.WriteLine("    {");
                Console.WriteLine("        return new Type [] {");
                
                for(int i = 0; i < non_entry_types.Count; i++) {
                    Console.WriteLine("            typeof({0}){1}", non_entry_types[i].FullName,
                        i < non_entry_types.Count -1 ? "," : String.Empty);
                }
                
                Console.WriteLine("        };");
                Console.WriteLine("    }");
                Console.WriteLine("}\n");
            } else if(non_entry_types != null) {
                try {
                    Console.WriteLine(
                        "Assembly.GetTypes() was called on assembly:\n" +
                        "{0}\n\n" +
                        "This assembly does not include any {1} types\n" + 
                        "and should probably be filtered from being passed to\n" +
                        "PluginFactory.LoadPluginsFromAssembly to prevent memory\n" + 
                        "loss and performance issues.\n\n",
                        assembly.Location, typeof(T).FullName);
                } catch {
                }
            }
        }
        
        public void LoadPluginFromType(Type type)
        {
            if(factory_type == PluginFactoryType.Instance) {
                try {
                    T plugin = (T)Activator.CreateInstance(type);
                    plugin_instances.Add(plugin);
                    plugin_types.Add(type);
                    OnPluginLoaded(plugin);
                } catch {
                }
            } else {
                plugin_types.Add(type);
                OnPluginLoaded(type);
            }
        }
        
        protected void OnPluginLoaded(T plugin)
        {
            GenericEventHandler<PluginFactory<T>, PluginFactoryEventArgs<T>> handler = PluginLoaded;
            if(handler != null) {
                handler(this, new PluginFactoryEventArgs<T>(plugin));
            }
        }
        
        protected void OnPluginLoaded(Type type)
        {
            GenericEventHandler<PluginFactory<T>, PluginFactoryEventArgs<T>> handler = PluginLoaded;
            if(handler != null) {
                handler(this, new PluginFactoryEventArgs<T>(type));
            }
        }
        
        public void AddExcludeMask(string exclude_mask)
        {
            exclude_masks.Add(exclude_mask.ToLower());
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return plugin_instances.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return plugin_instances.GetEnumerator();
        }
        
        public IEnumerable<Type> PluginTypes {
            get { return plugin_types; }    
        }
        
        public string IncludeMask {
            get { return include_mask; }
            set { include_mask = value; }
        }
    }
}
