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
using GConf;

namespace Banshee.Base
{
    public delegate bool ShutdownRequestHandler();

    public static class Globals
    {
        private static GConf.Client gconf_client;
        private static NetworkDetect network_detect;
        private static ActionManager action_manager;
        private static Library library;
        private static ArgumentQueue argument_queue;
        private static AudioCdCore audio_cd_core;
        private static Random random;
        private static DBusRemote dbus_remote;
        private static DBusPlayer dbus_player;
        private static Banshee.Gui.UIManager ui_manager;
        private static ComponentInitializer startup = new ComponentInitializer();
        
        public static event ShutdownRequestHandler ShutdownRequested;
        
        public static void Initialize()
        {
            if(!Directory.Exists(Paths.ApplicationData)) {
                Directory.CreateDirectory(Paths.ApplicationData);
            }
            
            Mono.Unix.Catalog.Init(ConfigureDefines.GETTEXT_PACKAGE, ConfigureDefines.LOCALE_DIR);
        
            ui_manager = new Banshee.Gui.UIManager();
            random = new Random();
            
            if(!Branding.Initialize()) {
                System.Environment.Exit(1);
                return;
            }
                        
            startup.Register("Starting DBus server", delegate {
                try {
                    dbus_remote = new DBusRemote();
                    dbus_player = new DBusPlayer();
                    dbus_remote.RegisterObject(dbus_player, "Player");
                } catch {
                }
            });
            
            startup.Register("Connecting to GConf", delegate { gconf_client = new GConf.Client(); });
            startup.Register("Detecting network settings", delegate { network_detect = NetworkDetect.Instance; });
            startup.Register("Creating action manager", delegate { action_manager = new ActionManager(); });
            startup.Register("Loading music library", delegate { library = new Library(); });
            
            startup.Register("Initializing GStreamer", Banshee.Gstreamer.Utilities.Initialize);
            startup.Register("Initializing player engine", PlayerEngineCore.Initialize);
            
            startup.Register("Initializing Audio CD support", true, 
                "Audio CD support will be disabled for this instance", delegate { audio_cd_core = new AudioCdCore(); });
                
            startup.Register("Initializing Digital Audio Player support", true, 
                "DAP support will be disabled for this instance", Banshee.Dap.DapCore.Initialize);
            
            startup.Register("Initializing CD Burning support", true, 
                "CD Burning support will be disabled for this instance", Banshee.Burner.BurnerCore.Initialize);
                
            startup.Register("Initializing plugins", Banshee.Plugins.PluginCore.Initialize);
            startup.Register("Initializing power management", PowerManagement.Initialize);
            
            startup.Run();
            
            action_manager.LoadInterface();
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
        
        private static void Dispose()
        {
            dbus_remote.UnregisterObject(dbus_player);
            Banshee.Kernel.Scheduler.Dispose();
            Banshee.Plugins.PluginCore.Dispose();
            network_detect.Dispose();
            library.Db.Dispose();
            Banshee.Dap.DapCore.Dispose();
            HalCore.Dispose();
            PowerManagement.Dispose();
        }
        
        public static ComponentInitializer StartupInitializer {
            get { return startup; }
        }
        
        public static GConf.Client Configuration {
            get { return gconf_client; }
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
    }
    
    public static class InterfaceElements
    {
        private static Gtk.Window main_window;
        public static Gtk.Window MainWindow {
            get { return main_window; }
            set {
                if(main_window == null) {
                    main_window = value;
                }
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
	
        private static Gtk.TreeView playlist_view;
        public static Gtk.TreeView PlaylistView {
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
            if(PlaylistContainer.Parent != null) {
                (PlaylistContainer.Parent as Gtk.Container).Remove(PlaylistContainer);
            }
        }
    }
}
