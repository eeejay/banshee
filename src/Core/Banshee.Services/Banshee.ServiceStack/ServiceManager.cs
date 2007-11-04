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
    public class ServiceManager : IService
    {
        private static ServiceManager instance;
        public static ServiceManager Instance {
            get { 
                if(instance == null) {
                    instance = new ServiceManager ();
                }
                
                return instance; 
            }
        }
        
        private Dictionary<string, IService> services = new Dictionary<string, IService> ();
        private List<Type> service_types = new List<Type> ();
        private bool has_run = false;
        
        public event EventHandler StartupBegin;
        public event EventHandler StartupFinished;
        public event ServiceStartedHandler ServiceStarted;
        
        public ServiceManager ()
        {
            RegisterService<DBusServiceManager> ();
            RegisterService<BansheeDbConnection> ();
            RegisterService<SourceManager> ();
        }
        
        public void Run()
        {
            lock (this) {          
                OnStartupBegin ();
                
                foreach (Type type in service_types) {
                    IService service = (IService)Activator.CreateInstance (type);
                    RegisterServiceNoLock (service);
                    OnServiceStarted (service);
                }
                
                RegisterServiceNoLock (this);
                OnServiceStarted (this);
                
                has_run = true;
                
                OnStartupFinished ();
            }
        }
        
        public void Shutdown ()
        {
            lock (this) {
                foreach (IService service in services.Values) {
                    if (service is IDisposable) {
                        ((IDisposable)service).Dispose ();
                    }
                }
                
                services.Clear ();
            }
        }
        
        private void RegisterServiceNoLock (IService service)
        {
            services.Add (service.ServiceName, service);
            
            if(service is IDBusExportable) {
                DBusServiceManager.RegisterObject ((IDBusExportable)service);
            }
        }
                    
        public void RegisterService (IService service)
        {
            lock (this) {
                RegisterServiceNoLock (service);
            }
        }
                    
        public void RegisterService<T> () where T : IService
        {
            lock (this) {
                if (has_run) {
                    RegisterServiceNoLock (Activator.CreateInstance <T> ());
                } else {
                    service_types.Add (typeof (T));
                }
            }
        }
        
        public bool Contains (string serviceName)
        {
            lock (this) {
                return services.ContainsKey (serviceName);
            }
        }
        
        protected virtual void OnStartupBegin ()
        {
            EventHandler handler = StartupBegin;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnStartupFinished ()
        {
            EventHandler handler = StartupFinished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        protected virtual void OnServiceStarted (IService service)
        {
            ServiceStartedHandler handler = ServiceStarted;
            if (handler != null) {
                handler (this, new ServiceStartedArgs (service));
            }
        }
        
        public int StartupServiceCount {
            get { return service_types.Count + 1; }
        }
        
        public int ServiceCount {
            get { return services.Count; }
        }
        
        public IService this[string serviceName] {
            get { try { return services[serviceName]; } catch { throw new Exception(serviceName); } }
        }
        
        string IService.ServiceName {
            get { return "ServiceManager"; }
        }
        
        public static DBusServiceManager DBusServiceManager {
            get { return (DBusServiceManager)Instance["DBusServiceManager"]; }
        }
                
        public static BansheeDbConnection DbConnection {
            get { return (BansheeDbConnection)Instance["DbConnection"]; }
        }
        
        public static SourceManager SourceManager {
            get { return (SourceManager)Instance["SourceManager"]; }
        }
    }
}
