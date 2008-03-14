//
// Application.cs
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
using System.Reflection;
using Mono.Unix;

using Banshee.Library;
using Banshee.Playlist;
using Banshee.SmartPlaylist;
using Banshee.Sources;

namespace Banshee.ServiceStack
{    
    public delegate bool ShutdownRequestHandler ();
    public delegate bool TimeoutHandler ();
    public delegate bool IdleHandler ();
    public delegate void InvokeHandler ();
    public delegate bool IdleTimeoutRemoveHandler (uint id);
    public delegate uint TimeoutImplementationHandler (uint milliseconds, TimeoutHandler handler); 
    public delegate uint IdleImplementationHandler (IdleHandler handler);
    public delegate bool IdleTimeoutRemoveImplementationHandler (uint id);
    
    public static class Application
    {   
        public static event ShutdownRequestHandler ShutdownRequested;
        
        public static void Run ()
        {
            Banshee.Base.PlatformHacks.TrapMonoJitSegv ();

            ServiceManager.Run ();
            
            if (ServiceManager.SourceManager != null) {
                ServiceManager.SourceManager.AddSource (new LibrarySource (), true);

                foreach (PlaylistSource pl in PlaylistSource.LoadAll ()) {
                    ServiceManager.SourceManager.Library.AddChildSource (pl);
                }

                ServiceManager.SourceManager.LoadExtensionSources ();
            }
            
            Banshee.Base.PlatformHacks.RestoreMonoJitSegv ();
        }
     
        public static void Shutdown ()
        {
            if (Banshee.Kernel.Scheduler.IsScheduled (typeof (Banshee.Kernel.IInstanceCriticalJob)) ||
                Banshee.Kernel.Scheduler.CurrentJob is Banshee.Kernel.IInstanceCriticalJob) {
                if (shutdown_prompt_handler != null && !shutdown_prompt_handler ()) {
                    return;
                }
            }
            
            if (OnShutdownRequested ()) {
                Dispose ();
            }
        }
        
        private static bool OnShutdownRequested()
        {
            ShutdownRequestHandler handler = ShutdownRequested;
            if (handler != null) {
                foreach (Delegate d in handler.GetInvocationList ()) {
                    if(!(bool)d.DynamicInvoke (null)) {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public static void Invoke (InvokeHandler handler)
        {
            RunIdle (delegate { handler (); return false; });
        }
        
        public static void Invoke (EventHandler handler)
        {
            RunIdle (delegate { handler (null, EventArgs.Empty); return false; });
        }
        
        public static uint RunIdle (IdleHandler handler)
        {
            if (idle_handler == null) {
                throw new NotImplementedException ("The application client must provide an IdleImplementationHandler");
            }
            
            return idle_handler (handler);
        }
        
        public static uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            if (timeout_handler == null) {
                throw new NotImplementedException ("The application client must provide a TimeoutImplementationHandler");
            }
            
            return timeout_handler (milliseconds, handler);
        }
        
        public static bool IdleTimeoutRemove (uint id)
        {
            if (idle_timeout_remove_handler == null) {
                throw new NotImplementedException ("The application client must provide a IdleTimeoutRemoveImplementationHandler");
            }
            
            return idle_timeout_remove_handler (id);
        }
        
        private static void Dispose ()
        {
            ServiceManager.Shutdown ();
        }
        
        private static ShutdownRequestHandler shutdown_prompt_handler = null;
        public static ShutdownRequestHandler ShutdownPromptHandler {
            get { return shutdown_prompt_handler; }
            set { shutdown_prompt_handler = value; }
        }
        
        private static TimeoutImplementationHandler timeout_handler = null;
        public static TimeoutImplementationHandler TimeoutHandler {
            get { return timeout_handler; }
            set { timeout_handler = value; }
        }
        
        private static IdleImplementationHandler idle_handler = null;
        public static IdleImplementationHandler IdleHandler {
            get { return idle_handler; }
            set { idle_handler = value; }
        }
        
        private static IdleTimeoutRemoveImplementationHandler idle_timeout_remove_handler = null;
        public static IdleTimeoutRemoveImplementationHandler IdleTimeoutRemoveHandler {
            get { return idle_timeout_remove_handler; }
            set { idle_timeout_remove_handler = value; }
        }
        
        public static string InternalName {
            get { return "banshee"; }
        }
        
        public static string IconName {
            get { return "media-player-banshee"; }
        }
        
        private static string version;
        public static string Version {
            get { 
                if (version != null) {
                    return version;
                }
                
                try {
                    AssemblyName name = Assembly.GetEntryAssembly ().GetName ();
                    version = String.Format ("{0}.{1}.{2}", name.Version.Major, 
                        name.Version.Minor, name.Version.Build);
                } catch {
                    version = Catalog.GetString ("Unknown");
                }
                
                return version;
            }
        }
        
        private static string display_version;
        public static string DisplayVersion {
            get { 
                if (display_version != null) {
                    return display_version;
                }
                
                foreach (Attribute attribute in Assembly.GetEntryAssembly ().GetCustomAttributes (false)) {
                    Type type = attribute.GetType ();
                    PropertyInfo property = type.GetProperty ("Version");
                    if (type.Name == "AssemblyDisplayVersionAttribute" && property != null && 
                        property.PropertyType == typeof (string)) {
                        display_version = (string)property.GetValue (attribute, null); 
                    }
                }
                
                return display_version;
            }
        }
    }
}
