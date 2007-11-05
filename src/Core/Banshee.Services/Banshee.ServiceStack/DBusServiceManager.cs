//
// DBusServiceManager.cs
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
using System.Text;
using System.Collections.Generic;
    
using NDesk.DBus;
using org.freedesktop.DBus;

namespace Banshee.ServiceStack
{
    public class DBusServiceManager : IService
    {
        public const string BusName = "org.bansheeproject.Banshee";
        public const string ObjectRoot = "/org/bansheeproject/Banshee";

        private static bool dbus_enabled;
        
        public DBusServiceManager()
        {
            dbus_enabled = !Banshee.Base.ApplicationContext.CommandLine.Contains ("disable-dbus");
            if (!dbus_enabled) {
                return;
            }
            
            BusG.Init();
            
            try {
                RequestNameReply name_reply = Bus.Session.RequestName(BusName);
                Console.WriteLine ("NDesk.DBus.Bus.Session.RequestName ('{0}') => {1}", BusName, name_reply);
                // TODO: error handling based on nameReply. should probably throw if 
                // nameReply is anything other than NameReply.PrimaryOwner
            } catch(Exception e) {
                throw e;
            }
        }

        public static string MakeDBusSafeString(string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(str, @"[^A-Za-z0-9]*", String.Empty);
        }
        
        public static string MakeObjectPath(IDBusExportable o)
        {
            StringBuilder object_path = new StringBuilder();
            
            object_path.Append(ObjectRoot);
            object_path.Append('/');
            
            Stack<string> paths = new Stack<string>();
            
            IDBusExportable p = o.Parent;
            
            while(p != null) {
                paths.Push(String.Format("{0}/", p.ServiceName));
                p = p.Parent;
            }
            
            while(paths.Count > 0) {
                object_path.Append(paths.Pop());
            }
            
            object_path.Append(o.ServiceName);
            
            return object_path.ToString();
        }
        
        public static string [] MakeObjectPathArray<T>(IEnumerable<T> collection) where T : IDBusExportable
        {
            List<string> paths = new List<string>();
            
            foreach(IDBusExportable item in collection) {
                paths.Add(MakeObjectPath(item));
            }
            
            return paths.ToArray();
        }
        
        public void RegisterObject(IDBusExportable o)
        {
            RegisterObject(o, MakeObjectPath(o));
        }
        
        public void RegisterObject(object o, string objectName)
        {
            if(dbus_enabled && Bus.Session != null) {
                Bus.Session.Register(BusName, new ObjectPath(objectName), o);
                Console.WriteLine("Registered {0} on {1}", objectName, BusName);
            }
        }

        public void UnregisterObject(object o)
        {
            //TODO: unregistering objects with managed dbus
        }
        
        public static T FindInstance<T>(string objectPath) where T : class
        {
            if(!dbus_enabled || !Bus.Session.NameHasOwner(BusName)) {
                return null;
            }
            
            string full_object_path = objectPath;
            if(!objectPath.StartsWith(ObjectRoot)) {
                full_object_path = ObjectRoot + objectPath;
            }

            return Bus.Session.GetObject<T>(BusName, new ObjectPath(full_object_path));
        }
        
        string IService.ServiceName {
            get { return "DBusServiceManager"; }
        }
    }
}
