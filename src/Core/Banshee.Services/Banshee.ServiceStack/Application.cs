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
    
    public static class Application
    {
        // For the SEGV trap hack (see below)
        [System.Runtime.InteropServices.DllImport ("libc")]
        private static extern int sigaction (Mono.Unix.Native.Signum sig, IntPtr act, IntPtr oact);
        
        public static event ShutdownRequestHandler ShutdownRequested;
        
        public static void Run ()
        {
            // We must get a reference to the JIT's SEGV handler because 
            // GStreamer will set its own and not restore the previous, which
            // will cause what should be NullReferenceExceptions to be unhandled
            // segfaults for the duration of the instance, as the JIT is powerless!
            // FIXME: http://bugzilla.gnome.org/show_bug.cgi?id=391777
            IntPtr mono_jit_segv_handler = System.Runtime.InteropServices.Marshal.AllocHGlobal (512);
            sigaction (Mono.Unix.Native.Signum.SIGSEGV, IntPtr.Zero, mono_jit_segv_handler);
            
            // Begin the Banshee boot process
            ServiceManager.Instance.Run ();
            ServiceManager.SourceManager.AddSource (new LibrarySource ());

            // Reset the SEGV handle to that of the JIT again (SIGH!)
            sigaction (Mono.Unix.Native.Signum.SIGSEGV, mono_jit_segv_handler, IntPtr.Zero);
            System.Runtime.InteropServices.Marshal.FreeHGlobal (mono_jit_segv_handler);
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
        
        private static void Dispose ()
        {
            ServiceManager.Instance.Shutdown ();
        }
        
        private static ShutdownRequestHandler shutdown_prompt_handler = null;
        public static ShutdownRequestHandler ShutdownPromptHandler {
            get { return shutdown_prompt_handler; }
            set { shutdown_prompt_handler = value; }
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
