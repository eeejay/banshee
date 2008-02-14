//
// ServiceManager.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.IO;
using System.Collections.Generic;

using Mono.Addins;

using Hyena;
using Banshee.Base;
using Banshee.AudioProfiles;
using Banshee.Sources;
using Banshee.SmartPlaylist;
using Banshee.Database;
using Banshee.MediaEngine;
using Banshee.PlaybackController;
using Banshee.Library;

namespace Banshee.ServiceStack
{
    public static class ServiceManager
    {
        private static Dictionary<string, IService> services = new Dictionary<string, IService> ();
        private static Stack<IService> dispose_services = new Stack<IService> ();
        private static List<Type> service_types = new List<Type> ();
        private static ExtensionNodeList extension_nodes;
        
        private static bool is_initialized = false;
        private static readonly object self_mutex = new object ();
        
        public static event EventHandler StartupBegin;
        public static event EventHandler StartupFinished;
        public static event ServiceStartedHandler ServiceStarted;
        
        static ServiceManager ()
        {
            RegisterService<DBusServiceManager> ();
            RegisterService<BansheeDbConnection> ();
            RegisterService<SourceManager> ();
            RegisterService<SmartPlaylistCore> ();
            RegisterService<ProfileManager> ();
            RegisterService<PlayerEngineService> ();
            RegisterService<PlaybackControllerService> ();
            RegisterService<ImportSourceManager> ();
            RegisterService<LibraryImportManager> ();
            RegisterService<UserJobManager> ();
            
            AddinManager.Initialize (ApplicationContext.CommandLine.Contains ("uninstalled") 
                ? "." : UserAddinCachePath);
            
            Banshee.Configuration.ConfigurationClient.Initialize ();
        
            if (ApplicationContext.Debugging) {
                AddinManager.Registry.Rebuild (null /*new ConsoleProgressStatus (true)*/);
            }
            
            extension_nodes = AddinManager.GetExtensionNodes ("/Banshee/ServiceManager/Service");
        }
        
        public static void Run()
        {
            lock (self_mutex) {          
                OnStartupBegin ();
                
                uint cumulative_timer_id = Log.DebugTimerStart ();
                
                foreach (Type type in service_types) {
                    uint timer_id = Log.DebugTimerStart (); 
                    IService service = (IService)Activator.CreateInstance (type);
                    RegisterService (service);
                    
                    Log.DebugTimerPrint (timer_id, String.Format (
                        "Core service started ({0}, {{0}})", service.ServiceName));
                    
                    OnServiceStarted (service);
                    
                    if (service is IDisposable) {
                        dispose_services.Push (service);
                    }
                }
                
                foreach (TypeExtensionNode node in extension_nodes) {
                    IExtensionService service = null;
                    
                    try {
                        uint timer_id = Log.DebugTimerStart ();
                        
                        service = (IExtensionService)node.CreateInstance (typeof (IExtensionService));
                        service.Initialize ();
                        RegisterService (service);
                    
                        Log.DebugTimerPrint (timer_id, String.Format (
                            "Extension service started ({0}, {{0}})", service.ServiceName));
                    
                        OnServiceStarted (service);
                    
                        if (service is IDisposable) {
                            dispose_services.Push (service);
                        }
                    } catch (Exception e) {
                        Log.Warning (String.Format ("Extension `{0}' not started: {1}", 
                            service == null ? node.Path : service.GetType ().FullName, e.Message));
                    }
                }
                
                is_initialized = true;
                
                Log.DebugTimerPrint (cumulative_timer_id, "All services are started {0}");
                
                OnStartupFinished ();
            }
        }
        
        public static void Shutdown ()
        {
            lock (self_mutex) {
                while (dispose_services.Count > 0) {
                    IService service = dispose_services.Pop ();
                    ((IDisposable)service).Dispose ();
                    Log.DebugFormat ("Service disposed ({0})", service.ServiceName);
                }
                
                services.Clear ();
            }
        }
        
        public static void RegisterService (IService service)
        {
            lock (self_mutex) {
                services.Add (service.ServiceName, service);
                
                if(service is IDBusExportable) {
                    DBusServiceManager.RegisterObject ((IDBusExportable)service);
                }
            }
        }
        
        public static void RegisterService<T> () where T : IService
        {
            lock (self_mutex) {
                if (is_initialized) {
                    RegisterService (Activator.CreateInstance <T> ());
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
        
        public static T Get<T> () where T : class, IService
        {
            return Get (typeof (T).Name) as T;
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
                handler (new ServiceStartedArgs (service));
            }
        }
        
        public static int StartupServiceCount {
            get { return service_types.Count + (extension_nodes == null ? 0 : extension_nodes.Count) + 1; }
        }
        
        public static int ServiceCount {
            get { return services.Count; }
        }
        
        public static bool IsInitialized {
            get { return is_initialized; }
        }
        
        public static string UserAddinCachePath {
            get { return Path.Combine (Paths.ApplicationData, "addins"); }
        }
        
        public static DBusServiceManager DBusServiceManager {
            get { return (DBusServiceManager)Get ("DBusServiceManager"); }
        }
                
        public static BansheeDbConnection DbConnection {
            get { return (BansheeDbConnection)Get ("DbConnection"); }
        }

        public static ProfileManager ProfileManager {
            get { return (ProfileManager)Get ("ProfileManager"); }
        }
        
        public static SourceManager SourceManager {
            get { return (SourceManager)Get ("SourceManager"); }
        }
        
        public static PlayerEngineService PlayerEngine {
            get { return (PlayerEngineService)Get ("PlayerEngine"); }
        }
        
        public static PlaybackControllerService PlaybackController {
            get { return (PlaybackControllerService)Get ("PlaybackController"); }
        }
    }
}
