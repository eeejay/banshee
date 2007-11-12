// 
// GtkBaseClient.cs
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

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public abstract class GtkBaseClient
    {
        private static Type client_type; 
        
        public static void Entry<T> () where T : GtkBaseClient
        {
            if (client_type != null) {
                throw new ApplicationException ("Only a single GtkBaseClient can be initialized through Entry<T>");
            }
            
            client_type = typeof (T);            
            Hyena.Gui.CleanRoomStartup.Startup (Startup);
        }
        
        private static void Startup ()
        {
            ((GtkBaseClient)Activator.CreateInstance (client_type)).Run ();
        }
        
        private string default_icon_name;
        
        protected GtkBaseClient () : this (true, "music-player-banshee")
        {
        }
        
        protected GtkBaseClient (bool initializeDefault, string defaultIconName)
        {
            this.default_icon_name = defaultIconName;
            if (initializeDefault) {
                Initialize (true);
            }
        }
        
        protected void Initialize (bool registerCommonServices)
        {
            // Set the process name so system process listings and commands are pretty
            PlatformHacks.TrySetProcessName (Application.InternalName);
            
            // Initialize GTK
            Gtk.Application.Init ();
            Gtk.Window.DefaultIconName = default_icon_name;
            
            PlatformHacks.GdkSetProgramClass (Application.InternalName);
            
            // Register specific services this client will care about
            if (registerCommonServices) {
                Banshee.Gui.CommonServices.Register ();
            }
            
            OnRegisterServices ();
            
            Application.ShutdownPromptHandler = OnShutdownPrompt;
            Application.TimeoutHandler = RunTimeout;
            Application.IdleHandler = RunIdle;
            Application.IdleTimeoutRemoveHandler = IdleTimeoutRemove;
            
            // Start the core boot process
            Application.Run ();
        }
        
        public virtual void Run ()
        {
            Gtk.Application.Run ();
        }
        
        protected virtual void OnRegisterServices ()
        {
        }
        
        private bool OnShutdownPrompt ()
        {
            ConfirmShutdownDialog dialog = new ConfirmShutdownDialog ();
            try {
                return dialog.Run () != Gtk.ResponseType.Cancel;
            } finally {
                dialog.Destroy ();
            }
        }
        
        protected uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            return GLib.Timeout.Add (milliseconds, delegate { return handler (); });
        }
        
        protected uint RunIdle (IdleHandler handler)
        {
            return GLib.Idle.Add (delegate { return handler (); });
        }
        
        protected bool IdleTimeoutRemove (uint id)
        {
            return GLib.Source.Remove (id);
        }
    }
}