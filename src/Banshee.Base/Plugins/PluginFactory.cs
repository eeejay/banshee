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

    public class PluginFactory<T> : IDisposable, IEnumerable<T>, IEnumerable where T : IPlugin
    {
        private List<T> plugin_instances = new List<T>();
        private List<Type> plugin_types = new List<Type>();
        private List<DirectoryInfo> scan_directories = new List<DirectoryInfo>();
        private PluginFactoryType factory_type;
        
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
        
        public void AddScanDirectory(DirectoryInfo directory)
        {
            scan_directories.Add(directory);
        }
        
        public void AddScanDirectory(string directory)
        {
            scan_directories.Add(new DirectoryInfo(directory));
        }
        
        public void LoadPlugins()
        {
            foreach(DirectoryInfo directory in scan_directories) {
                LoadPluginsFromDirectory(directory);
            }
        }
        
        public void LoadPluginsFromDirectory(DirectoryInfo directory)
        {
            foreach(FileInfo file in directory.GetFiles("*.dll")) {
                LoadPluginsFromFile(file);
            }
        }
        
        public void LoadPluginsFromFile(FileInfo file)
        {
            LoadPluginsFromAssembly(Assembly.LoadFrom(file.FullName));
        }
        
        public void LoadPluginsFromAssembly(Assembly assembly)
        {
            foreach(Type type in assembly.GetTypes()) {
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
                
                if(factory_type == PluginFactoryType.Instance) {
                    try {
                        T plugin = (T)Activator.CreateInstance(type);
                        plugin_instances.Add(plugin);
                        plugin_types.Add(type);
                    } catch {
                    }
                } else {
                    plugin_types.Add(type);
                }
            }
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
    }
}
