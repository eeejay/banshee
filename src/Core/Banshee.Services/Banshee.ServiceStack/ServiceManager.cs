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

//using Banshee.Sources;
using Banshee.Database;

namespace Banshee.ServiceStack
{
    public class ServiceManager : IService
    {
        private Dictionary<string, IService> services = new Dictionary<string, IService>();

        private static ServiceManager instance;
        public static ServiceManager Instance {
            get { 
                if(instance == null) {
                    instance = new ServiceManager();
                }
                
                return instance; 
            }
        }
        
        public static void Initialize()
        {
        }
        
        public void Run()
        {
            IService [] _services = new IService [] {
                new DBusServiceManager(),
                new BansheeDbConnection(),
                //new SourceManager(),
                this
            };
            
            foreach(IService service in _services) {
                services.Add(service.ServiceName, service);
                
                if(service is IDBusExportable) {
                    DBusServiceManager.RegisterObject((IDBusExportable)service);
                }
            }
        }
        
        public IService this[string serviceName] {
            get { try { return services[serviceName]; } catch { throw new Exception(serviceName); } }
        }
        
        string IService.ServiceName {
            get { return "ServicesManager"; }
        }
        
        public static DBusServiceManager DBusServiceManager {
            get { return (DBusServiceManager)Instance["DBusServiceManager"]; }
        }
                
        public static BansheeDbConnection DbConnection {
            get { return (BansheeDbConnection)Instance["DbConnection"]; }
        }
        
        /*public static SourceManager SourceManager {
            get { return (SourceManager)Instance["SourceManager"]; }
        }*/
    }
}
