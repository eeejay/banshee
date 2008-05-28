//
// Client.cs
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
using System.Diagnostics;
using System.Reflection;

using Banshee.Base;
using Banshee.ServiceStack;

namespace Nereid
{
    public class Client : Banshee.Gui.GtkBaseClient
    {
        public static void Main ()
        {
            // Check for single instance
            DBusConnection.Connect ();
            if (DBusConnection.InstanceAlreadyRunning) {
                // Try running our friend Halie, the DBus command line client
                AppDomain.CurrentDomain.ExecuteAssembly (Path.Combine (Path.GetDirectoryName (
                    Assembly.GetEntryAssembly ().Location), "Halie.exe"));
                return;
            }
                    
            Hyena.Log.InformationFormat ("Running Banshee {0}", Banshee.ServiceStack.Application.Version);
            
            // This could go into GtkBaseClient, but it's probably something we
            // should really only support at each client level
            string user_gtkrc = Path.Combine (Paths.ApplicationData, "gtkrc"); 
            if (File.Exists (user_gtkrc) && !ApplicationContext.CommandLine.Contains ("no-gtkrc")) {
                Gtk.Rc.AddDefaultFile (user_gtkrc);
            } 

            // Boot the client
            Banshee.Gui.GtkBaseClient.Entry<Client> ();
        }
        
        protected override void OnRegisterServices ()
        {
            // Register the main interface
            ServiceManager.RegisterService <PlayerInterface> ();
        }
        
        public override string ClientId {
            get { return "nereid"; }
        }
    }
}

