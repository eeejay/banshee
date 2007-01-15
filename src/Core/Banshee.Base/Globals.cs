/***************************************************************************
 *  Globals.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.IO;
using Mono.Unix;

using Banshee.AudioProfiles;

namespace Banshee.Base
{
    public delegate bool ShutdownRequestHandler();

    public static class Globals
    {
        // For the SEGV trap hack (see below)
        [System.Runtime.InteropServices.DllImport("libc")]
        private static extern int sigaction(Mono.Unix.Native.Signum sig, IntPtr act, IntPtr oact);

        private static NetworkDetect network_detect;
        private static ActionManager action_manager;
        private static Library library;
        private static ArgumentQueue argument_queue;
        private static AudioCdCore audio_cd_core;
        private static Random random;
        private static DBusRemote dbus_remote;
        private static DBusPlayer dbus_player;
        private static Banshee.Gui.UIManager ui_manager;
        private static ProfileManager audio_profile_manager;
        private static ComponentInitializer startup = new ComponentInitializer();
        
        public static event ShutdownRequestHandler ShutdownRequested;
        
        public static void Initialize()
        {
            Initialize(null);
        }
        
        public static void Initialize(ComponentInitializerHandler interfaceStartupHandler)
        {
            Mono.Unix.Catalog.Init(ConfigureDefines.GETTEXT_PACKAGE, ConfigureDefines.LOCALE_DIR);
        
            ui_manager = new Banshee.Gui.UIManager();
            
            if(!Branding.Initialize()) {
                System.Environment.Exit(1);
                return;
            }
            
            random = new Random();
            
            if(!Directory.Exists(Paths.ApplicationData)) {
                Directory.CreateDirectory(Paths.ApplicationData);
            }
            
            // override the browser URI launch hook in Last.FM and Banshee.Widgets.LinkLabel
            Last.FM.Browser.Open = new Last.FM.UriOpenHandler(Banshee.Web.Browser.Open);
            Banshee.Widgets.LinkLabel.DefaultOpen = 
                new Banshee.Widgets.LinkLabel.UriOpenHandler(Banshee.Web.Browser.Open);
                        
            startup.Register(Catalog.GetString("Starting background tasks"), delegate {
                try {
                    dbus_remote = new DBusRemote();
                    dbus_player = new DBusPlayer();
                    dbus_remote.RegisterObject(dbus_player, "Player");
                } catch {
                }
            });
            
            startup.Register(Catalog.GetString("Starting background tasks"), true,
                Catalog.GetString("Device support will be disabled for this instance (no HAL)"),
                HalCore.Initialize);
                
            startup.Register(Catalog.GetString("Initializing audio engine"), Banshee.Gstreamer.Utilities.Initialize);
            startup.Register(Catalog.GetString("Detecting network settings"), delegate { network_detect = NetworkDetect.Instance; });
            startup.Register(Catalog.GetString("Creating action manager"), delegate { action_manager = new ActionManager(); });
            startup.Register(Catalog.GetString("Loading music library"), delegate { 
                library = new Library(); 
                library.ReloadLibrary();
            });
            
            startup.Register(Catalog.GetString("Initializing audio profiles"), delegate {
                string system_path = Path.Combine(Banshee.Base.Paths.SystemApplicationData, "audio-profiles");
                string user_path = Path.Combine(Banshee.Base.Paths.ApplicationData, "audio-profiles");
                string env_path = Environment.GetEnvironmentVariable("BANSHEE_PROFILES_PATH");
                string load_path = null;
                
                if(Directory.Exists(env_path)) {
                    load_path = env_path;
                } else if(Directory.Exists(user_path)) {
                    load_path = user_path;
                } else {
                    /*try {
                        Directory.CreateDirectory(user_path);
                        foreach(string file in Directory.GetFiles(system_path, "*.xml")) {
                            File.Copy(file, Path.Combine(user_path, Path.GetFileName(file)));
                        }
                        
                        load_path = user_path;
                    } catch {*/
                        load_path = system_path;
                    //}
                }
                    
                LogCore.Instance.PushDebug("Loading audio profiles", load_path);
                
                Pipeline.AddSExprFunction("gst-element-is-available", Banshee.Gstreamer.Utilities.SExprTestElement);
                
                audio_profile_manager = new ProfileManager(load_path);
                audio_profile_manager.TestProfile += OnTestAudioProfile;
                audio_profile_manager.TestAll();
            });
            
            startup.Register(Catalog.GetString("Initializing audio engine"), PlayerEngineCore.Initialize);
            
            startup.Register(Catalog.GetString("Initializing audio CD support"), true, 
                Catalog.GetString("Audio CD support will be disabled for this instance"), delegate { audio_cd_core = new AudioCdCore(); });
                
            startup.Register(Catalog.GetString("Initializing digital audio player support"), true, 
                Catalog.GetString("DAP support will be disabled for this instance"), Banshee.Dap.DapCore.Initialize);
            
            startup.Register(Catalog.GetString("Initializing CD writing support"), true, 
                Catalog.GetString("CD burning support will be disabled for this instance"), Banshee.Burner.BurnerCore.Initialize);
                
            startup.Register(Catalog.GetString("Initializing plugins"), Banshee.SmartPlaylist.SmartPlaylistCore.Instance.Initialize);
            startup.Register(Catalog.GetString("Initializing plugins"), Banshee.Plugins.PluginCore.Initialize);
            startup.Register(Catalog.GetString("Initializing scripts"), Banshee.Plugins.ScriptCore.Initialize);
            startup.Register(Catalog.GetString("Starting background tasks"), PowerManagement.Initialize);
            
            if(interfaceStartupHandler != null) {
                startup.Register(Catalog.GetString("Loading user interface"), interfaceStartupHandler);
            }
            
            // We must get a reference to the JIT's SEGV handler because 
            // GStreamer will set its own and not restore the previous, which
            // will cause what should be NullReferenceExceptions to be unhandled
            // segfaults for the duration of the instance, as the JIT is powerless!
            // FIXME: http://bugzilla.gnome.org/show_bug.cgi?id=391777
            IntPtr mono_jit_segv_handler = System.Runtime.InteropServices.Marshal.AllocHGlobal(512);
            sigaction(Mono.Unix.Native.Signum.SIGSEGV, IntPtr.Zero, mono_jit_segv_handler);

            // Begin the Banshee boot process
            startup.Run();
            
            // Reset the SEGV handle to that of the JIT again (SIGH!)
            sigaction(Mono.Unix.Native.Signum.SIGSEGV, mono_jit_segv_handler, IntPtr.Zero);
            System.Runtime.InteropServices.Marshal.FreeHGlobal(mono_jit_segv_handler);
        }
        
        public static void Shutdown()
        {
            if(Banshee.Kernel.Scheduler.IsScheduled(typeof(Banshee.Kernel.IInstanceCriticalJob)) ||
                Banshee.Kernel.Scheduler.CurrentJob is Banshee.Kernel.IInstanceCriticalJob) {
                Banshee.Gui.Dialogs.ConfirmShutdownDialog dialog = new Banshee.Gui.Dialogs.ConfirmShutdownDialog();
                try {
                    if(dialog.Run() == Gtk.ResponseType.Cancel) {
                        return;
                    }
                } finally {
                    dialog.Destroy();
                }
            }
            
            if(OnShutdownRequested()) {
                Dispose();
            }
        }
        
        private static bool OnShutdownRequested()
        {
            ShutdownRequestHandler handler = ShutdownRequested;
            if(handler != null) {
                foreach(Delegate d in handler.GetInvocationList()) {
                    if(!(bool)d.DynamicInvoke(null)) {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        private static void OnTestAudioProfile(object o, TestProfileArgs args)
        {
            if(EnvironmentIsSet("BANSHEE_PROFILES_NO_TEST")) {
                foreach(Pipeline.Process process in args.Profile.Pipeline.GetPendingProcessesById("gstreamer")) {
                    args.Profile.Pipeline.AddProcess(process);
                    args.ProfileAvailable = true;
                    return;
                }
            }
            
            bool available = false;
            
            foreach(Pipeline.Process process in args.Profile.Pipeline.GetPendingProcessesById("gstreamer")) {
                string pipeline = args.Profile.Pipeline.CompileProcess(process);
                if(Banshee.Gstreamer.Utilities.TestPipeline(pipeline)) {
                    args.Profile.Pipeline.AddProcess(process);
                    available = true;
                    break;
                } else if(Debugging) {
                    LogCore.Instance.PushDebug("GStreamer pipeline does not run", pipeline);
                }
            }
            
            args.ProfileAvailable = available;
        }
        
        private static void Dispose()
        {
            dbus_remote.UnregisterObject(dbus_player);
            Banshee.Kernel.Scheduler.Dispose();
            Banshee.SmartPlaylist.SmartPlaylistCore.Instance.Dispose();
            Banshee.Plugins.PluginCore.Dispose();
            library.Db.Dispose();
            Banshee.Dap.DapCore.Dispose();
            HalCore.Dispose();
            PowerManagement.Dispose();
        }
        
        public static ComponentInitializer StartupInitializer {
            get { return startup; }
        }

        public static NetworkDetect Network {
            get { return network_detect; }
        }
        
        public static ActionManager ActionManager {
            get { return action_manager; }
        }
        
        public static Library Library {
            get { return library; }
        }
        
        public static ArgumentQueue ArgumentQueue {
            set { argument_queue = value; }
            get { return argument_queue; }
        }
        
        public static AudioCdCore AudioCdCore {
            get { return audio_cd_core; }
        }
        
        public static Random Random {
            get { return random; }
        }
        
        public static DBusRemote DBusRemote {
            get { return dbus_remote; }
        }
        
        public static DBusPlayer DBusPlayer {
            get { return dbus_player; }
        }
        
        public static Banshee.Gui.UIManager UIManager {
            get { return ui_manager; }
        }
        
        public static ProfileManager AudioProfileManager {
            get { return audio_profile_manager; }
        }
        
        private static bool? debugging = null;
        public static bool Debugging {
            get {
                if(debugging == null) {
                    debugging = ArgumentQueue.Contains("debug");
                    string debug_env = Environment.GetEnvironmentVariable("BANSHEE_DEBUG");
                    debugging |= debug_env != null && debug_env != String.Empty;
                }
                
                return debugging.Value;
            }
        }
        
        public static bool EnvironmentIsSet(string env)
        {
            string env_val = Environment.GetEnvironmentVariable(env);
            return env_val != null && env_val != String.Empty;
        }
    }
    
    public static class InterfaceElements
    {
        public delegate bool PrimaryWindowCloseHandler();
        
        public static PrimaryWindowCloseHandler PrimaryWindowClose;
    
        private static Gtk.Window main_window;
        public static Gtk.Window MainWindow {
            get { return main_window; }
            set {
                main_window = value;
            }
        }

        private static Gtk.Container playlist_container;
        public static Gtk.Container PlaylistContainer {
            get { return playlist_container; }
            set {
                if(playlist_container == null) {
                    playlist_container = value;
                }
            }
        }

        private static Gtk.Box main_container;
        public static Gtk.Box MainContainer {
            get { return main_container; }
            set {
                if(main_container == null) {
                    main_container = value;
                }
            }
        }
	
        private static Gtk.Widget playlist_view;
        public static Gtk.Widget PlaylistView {
            get { return playlist_view; }
            set {
                if(playlist_view == null) {
                    playlist_view = value;
                }
            }
        }
        
        private static Banshee.Widgets.SearchEntry search_entry;
        public static Banshee.Widgets.SearchEntry SearchEntry {
            get { return search_entry; }
            set {
                if(search_entry == null) {
                    search_entry = value;
                }
            }
        }
        
        private static Gtk.Box action_button_box;
        public static Gtk.Box ActionButtonBox {
            get { return action_button_box; }
            set {
                if(action_button_box == null) {
                    action_button_box = value;
                }
            }
        }        
        
        public static void DetachPlaylistContainer()
        {
            if(PlaylistContainer != null && PlaylistContainer.Parent != null) {
                (PlaylistContainer.Parent as Gtk.Container).Remove(PlaylistContainer);
            }
        }
    }
}
