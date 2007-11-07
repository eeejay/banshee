//
// ServiceManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Banshee.Sources;
using Banshee.Database;

namespace Banshee.ServiceStack
{
    public static class ServiceManager
    {
        private static Dictionary<string, IService> services = new Dictionary<string, IService> ();
        private static List<Type> service_types = new List<Type> ();
        
        private static bool has_run = false;
        private static readonly object self_mutex = new object ();
        
        public static event EventHandler StartupBegin;
        public static event EventHandler StartupFinished;
        public static event ServiceStartedHandler ServiceStarted;
        
        static ServiceManager ()
        {
            //RegisterService<DBusServiceManager> ();
            //RegisterService<BansheeDbConnection> ();
            //RegisterService<SourceManager> ();
            RegisterService<AddinCoreService> ();
        }
        
        public static void Run()
        {
            lock (self_mutex) {          
                OnStartupBegin ();
                
                foreach (Type type in service_types) {
                    IService service = (IService)Activator.CreateInstance (type);
                    RegisterServiceNoLock (service);
                    OnServiceStarted (service);
                }
                
                has_run = true;
                
                OnStartupFinished ();
            }
        }
        
        public static void Shutdown ()
        {
            lock (self_mutex) {
                foreach (IService service in services.Values) {
                    if (service is IDisposable) {
                        ((IDisposable)service).Dispose ();
                    }
                }
                
                services.Clear ();
            }
        }
        
        private static void RegisterServiceNoLock (IService service)
        {
            services.Add (service.ServiceName, service);
            
            if(service is IDBusExportable) {
                DBusServiceManager.RegisterObject ((IDBusExportable)service);
            }
        }
                    
        public static void RegisterService (IService service)
        {
            lock (self_mutex) {
                RegisterServiceNoLock (service);
            }
        }
                    
        public static void RegisterService<T> () where T : IService
        {
            lock (self_mutex) {
                if (has_run) {
                    RegisterServiceNoLock (Activator.CreateInstance <T> ());
                } else {
                    service_types.Add (typeof (T));
                }
            }
        }
        
        public static bool Contains (string serviceName)
        {
            lock (self_mutex) {
                return services.ContainsKey (serviceName);
            }
        }
        
        public static IService Get (string serviceName)
        {
            if (services.ContainsKey (serviceName)) {
                return services[serviceName]; 
            }
            
            return null;
        }
        
        public static T Get<T> (string serviceName) where T : class, IService
        {
            return Get (serviceName) as T;
        }
        
        private static void OnStartupBegin ()
        {
            EventHandler handler = StartupBegin;
            if (handler != null) {
                handler (null, EventArgs.Empty);
            }
        }
        
        private static void OnStartupFinished ()
        {
            EventHandler handler = StartupFinished;
            if (handler != null) {
                handler (null, EventArgs.Empty);
            }
        }
        
        private static void OnServiceStarted (IService service)
        {
            ServiceStartedHandler handler = ServiceStarted;
            if (handler != null) {
                handler (null, new ServiceStartedArgs (service));
            }
        }
        
        public static int StartupServiceCount {
            get { return service_types.Count + 1; }
        }
        
        public static int ServiceCount {
            get { return services.Count; }
        }
        
        public static DBusServiceManager DBusServiceManager {
            get { return (DBusServiceManager)Get ("DBusServiceManager"); }
        }
                
        public static BansheeDbConnection DbConnection {
            get { return (BansheeDbConnection)Get ("DbConnection"); }
        }
        
        public static SourceManager SourceManager {
            get { return (SourceManager)Get ("SourceManager"); }
        }
    }
}
