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

using Hyena;
using Banshee.Base;
using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Gui.Dialogs;

namespace Banshee.Gui
{
    public abstract class GtkBaseClient : Client
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
        
        protected GtkBaseClient () : this (true, Application.IconName)
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

            ThreadAssist.InitializeMainThread ();
            
            Gdk.Global.ProgramClass = Application.InternalName;

            if (ApplicationContext.Debugging) {
                GLib.Log.SetLogHandler ("Gtk", GLib.LogLevelFlags.Critical, GLib.Log.PrintTraceLogFunction);
            }
            
            ServiceManager.ServiceStarted += OnServiceStarted;
            
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
            
            Application.PushClient (this);
            Application.Run ();
            
            Log.Notify += OnLogNotify;
        }
        
        public virtual void Run ()
        {
            RunIdle (delegate { OnStarted (); return false; });
            Gtk.Application.Run ();
        }
        
        protected virtual void OnRegisterServices ()
        {
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is BansheeDbConnection) {
                ServiceManager.ServiceStarted -= OnServiceStarted;
                BansheeDbFormatMigrator migrator = ((BansheeDbConnection)args.Service).Migrator;
                if (migrator != null) {
                    migrator.Started += OnMigratorStarted;
                    migrator.Finished += OnMigratorFinished;
                }
            }
        }
        
        private void OnMigratorStarted (object o, EventArgs args)
        {
            BansheeDbFormatMigrator migrator = (BansheeDbFormatMigrator)o;
            new BansheeDbFormatMigratorMonitor (migrator);
        }

        private void OnMigratorFinished (object o, EventArgs args)
        {
            BansheeDbFormatMigrator migrator = (BansheeDbFormatMigrator)o;
            migrator.Started -= OnMigratorStarted;
            migrator.Finished -= OnMigratorFinished;
        }

        private void OnLogNotify (LogNotifyArgs args)
        {
            RunIdle (delegate {
                ShowLogCoreEntry (args.Entry);
                return false;
            });
        }
                
        private void ShowLogCoreEntry (LogEntry entry)
        {
            Gtk.Window window = null;
            Gtk.MessageType mtype;
            
            if (ServiceManager.Contains<GtkElementsService> ()) {
                window = ServiceManager.Get<GtkElementsService> ().PrimaryWindow;
            }
            
            switch (entry.Type) {
                case LogEntryType.Warning:
                    mtype = Gtk.MessageType.Warning;
                    break;
                case LogEntryType.Information:
                    mtype = Gtk.MessageType.Info;
                    break;
                case LogEntryType.Error:
                default:
                    mtype = Gtk.MessageType.Error;
                    break;
            }
              
            Banshee.Widgets.HigMessageDialog dialog = new Banshee.Widgets.HigMessageDialog (
                window, Gtk.DialogFlags.Modal, mtype, Gtk.ButtonsType.Close, entry.Message, entry.Details);
            
            dialog.Title = String.Empty;
            dialog.Run ();
            dialog.Destroy ();
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
