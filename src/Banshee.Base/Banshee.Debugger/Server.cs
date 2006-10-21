/***************************************************************************
 *  Server.cs
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
using System.Text;
using System.Reflection;
using System.Collections.Generic;

using NDesk.DBus;

namespace Banshee.Debugger
{
    public delegate void ShutdownHandler();
    
    [Interface("org.gnome.Banshee.Debugger")]
    public interface IDebuggerServer
    {
        event ShutdownHandler Shutdown; 
        string MethodCall(string method);
    }
    
    public class Server
    {
        private static Server server;
        
        public static Server Instance {
            get { return server; }
        }
    
        public static void Initialize()
        {
            if(server != null) {
                return;
            }
            
            server = new Server();
            Banshee.Base.Globals.DBusRemote.RegisterObject(server, "Debugger");
        }
        
        private Dictionary<string, MethodInfo> method_cache;
        public event ShutdownHandler Shutdown;
        
        public void Dispose()
        {
            server = null;
            if(Shutdown != null) {
                Shutdown();
            }
        }
        
        public string MethodCall(string method)
        {
            lock(this) {
                MethodInfo method_info = FindMethodInfo(method);
                if(method_info == null) {
                    return String.Empty;
                }

                WriteMessage("Invoking {0}", method_info.Name);
                
                try {
                    object result = method_info.Invoke(null, new object [] { });
                    if(result != null && result is string) {
                        return (string)result;
                    } else {
                        return "Success";
                    }
                } catch(Exception e) {
                    Console.WriteLine(e);
                    if(e.InnerException != null) {
                        Console.WriteLine(e.InnerException);
                    }
                    return "Error running method (see server console)";
                }
            }
        }
        
        [RemoteMethod("show-methods")]
        public static string ShowMethods()
        {
            if(Instance == null) {
                return "Server is null";
            }
            
            if(Instance.method_cache == null) {
                return "Method cache is null";
            }
        
            StringBuilder result = new StringBuilder();
            foreach(KeyValuePair<string, MethodInfo> method in Instance.method_cache) {
                result.Append(String.Format("{0}:\n\t{1}\n\t{2}.{3}\n\n", method.Key, 
                    method.Value.ReturnType.ToString(),
                    method.Value.DeclaringType.FullName,
                    method.Value.Name));
            }
            return result.ToString();
        }
        
        private void WriteMessage(string message, params object [] args)
        {
            Console.WriteLine("** IDebuggerServer [{0}]: {1}", DateTime.Now, String.Format(message, args));
        }
        
        private MethodInfo FindMethodInfo(string method)
        {
            if(method_cache == null) {
                WriteMessage("Building method cache");
                
                method_cache = new Dictionary<string, MethodInfo>();
                
                foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    foreach(Type type in assembly.GetTypes()) {
                        foreach(MethodInfo method_info in type.GetMethods()) {
                            if(!method_info.IsPublic || !method_info.IsStatic) {
                                continue;
                            }
                            
                            foreach(object attribute in method_info.GetCustomAttributes(false)) {
                                if(!(attribute is RemoteMethodAttribute)) {
                                    continue;
                                }
                                
                                RemoteMethodAttribute remote_attr = attribute as RemoteMethodAttribute;
                                
                                if(remote_attr.RemoteName == null) {
                                    method_cache.Add(String.Format("{0}.{1}", method_info.DeclaringType.FullName,
                                        method_info.Name), method_info);
                                } else {
                                    method_cache.Add(remote_attr.RemoteName, method_info);
                                }
                            }
                        }
                    }
                }
                
                WriteMessage("Method cache contains {0} methods", method_cache.Count);
            }
            
            if(method_cache.ContainsKey(method)) {
                return method_cache[method];
            }
            
            foreach(MethodInfo method_info in method_cache.Values) {
                if(String.Format("{0}.{1}", method_info.DeclaringType.FullName, method_info.Name) == method) {
                    return method_info;
                }
            }
            
            return null;
        }
    }
}
