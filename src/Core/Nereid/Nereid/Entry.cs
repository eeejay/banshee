//
// Entry.cs
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

namespace Nereid
{
    public static class NereidEntry
    {
        public static void Main ()
        {
            Hyena.Gui.CleanRoomStartup.Startup (Startup);
        }
        
        private static void Startup ()
        {   
            // Set the process name so system process listings and commands are pretty
            PlatformHacks.TrySetProcessName (Application.InternalName);
            
            // Initialize GTK
            Gtk.Application.Init ();
            Gtk.Window.DefaultIconName = "music-player-banshee";
            
            PlatformHacks.GdkSetProgramClass (Application.InternalName);
            
            // Create a GNOME program wrapper
            Gnome.Program program = new Gnome.Program ("Banshee", Application.Version, 
                Gnome.Modules.UI, Environment.GetCommandLineArgs ());
            
            // Register specific services this client will care about
            ServiceManager.RegisterService <Banshee.Gui.GtkElementsService> ();
            ServiceManager.RegisterService <Banshee.Gui.InterfaceActionService> ();
            ServiceManager.RegisterService <PlayerInterface> ();
            
            // Start the core boot process
            Application.ShutdownPromptHandler = OnShutdownPrompt;
            Application.TimeoutHandler = RunTimeout;
            Application.Run ();
            
            // Run the GTK main loop
            program.Run ();
        }
        
        private static bool OnShutdownPrompt ()
        {
            Banshee.Gui.Dialogs.ConfirmShutdownDialog dialog = new Banshee.Gui.Dialogs.ConfirmShutdownDialog();
            try {
                return dialog.Run () != Gtk.ResponseType.Cancel;
            } finally {
                dialog.Destroy();
            }
        }
        
        private static uint RunTimeout (uint milliseconds, TimeoutHandler handler)
        {
            return GLib.Timeout.Add (milliseconds, delegate { return handler (); });
        }
    }
}

