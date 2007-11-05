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
using Banshee.Sources;

namespace Banshee.ServiceStack
{    
    public delegate bool ShutdownRequestHandler ();
    public delegate bool TimeoutHandler ();
    public delegate uint TimeoutImplementationHandler (uint milliseconds, TimeoutHandler handler); 
    
    public static class Application
    {   
        public static event ShutdownRequestHandler ShutdownRequested;
        
        public static void Run ()
        {
            Banshee.Base.PlatformHacks.TrapMonoJitSegv ();

            ServiceManager.Run ();
            ServiceManager.SourceManager.AddSource (new LibrarySource (), true);
            
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
        
        public static uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            if (timeout_handler == null) {
                throw new NotImplementedException ("The application client must provide a TimeoutImplementationHandler");
            }
            
            return timeout_handler (milliseconds, handler);
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
        
        public static string InternalName {
            get { return "banshee"; }
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
    }
}
