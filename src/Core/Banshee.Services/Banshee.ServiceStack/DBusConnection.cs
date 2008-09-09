//
// DBusConnection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using NDesk.DBus;
using org.freedesktop.DBus;

using Hyena;
using Banshee.Base;

namespace Banshee.ServiceStack
{
    public static class DBusConnection
    {
        public const string BusName = "org.bansheeproject.Banshee";
        
        private static bool enabled;
        public static bool Enabled {
            get { return enabled; }
        }
        
        private static bool instance_already_running;
        public static bool InstanceAlreadyRunning {
            get { return instance_already_running; }
        }
        
        private static bool connect_tried;
        public static bool ConnectTried {
            get { return connect_tried; }
        }
        
        public static void Connect ()
        {
            connect_tried = true;
            
            enabled = !ApplicationContext.CommandLine.Contains ("disable-dbus");
            if (!enabled) {
                return;
            }
            
            try {
                instance_already_running = Connect (true) != RequestNameReply.PrimaryOwner;
            } catch {
                Log.Warning ("DBus support could not be started. Disabling for this session.");
                enabled = false;
            }
        }
        
        public static RequestNameReply Connect (bool init)
        {
            connect_tried = true;
            
            if (init) {
                BusG.Init ();
            }
            
            RequestNameReply name_reply = Bus.Session.RequestName (BusName);
            Log.DebugFormat ("NDesk.DBus.Bus.Session.RequestName ('{0}') => {1}", BusName, name_reply);
            return name_reply;
        }
        
        private static GLib.MainLoop mainloop;
        
        public static void RunMainLoop ()
        {
            if (mainloop == null) {
                mainloop = new GLib.MainLoop ();
            }
            
            if (!mainloop.IsRunning) {
                mainloop.Run ();
            }
        }
        
        public static void QuitMainLoop ()
        {
            if (mainloop != null && mainloop.IsRunning) {
                mainloop.Quit ();
            }
        }
    }
}
