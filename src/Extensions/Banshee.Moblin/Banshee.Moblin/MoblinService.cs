//
// MoblinService.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Novell, Inc.
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
using Gtk;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Gui;

using Mutter;

namespace Banshee.Moblin
{
    public class MoblinService : IExtensionService
    {
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private SourceManager source_manager;
        private PlayerEngineService player;
        private Banshee.Sources.Source now_playing;
        
        public MoblinService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();
            source_manager = ServiceManager.SourceManager;
            player = ServiceManager.PlayerEngine;
        
            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }
        
        private void OnServiceStarted (ServiceStartedArgs args) 
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            } else if (args.Service is SourceManager) {
                source_manager = ServiceManager.SourceManager;
            } else if (args.Service is PlayerEngineService) {
                player = ServiceManager.PlayerEngine;
            }
                    
            ServiceStartup ();
        }
        
        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null || source_manager == null || player == null) {
                return false;
            }
            
            Initialize ();
            
            ServiceManager.ServiceStarted -= OnServiceStarted;
            
            return true;
        }
        
        private void Initialize ()
        {
            ReflectionHackeryUpTheNereid ();
            
            if (MoblinPanel.Instance == null) {
                return;
            }
            
            var container = MoblinPanel.Instance.ParentContainer;
            foreach (var child in container.Children) {
                container.Remove (child);
            }
            container.Add (new MediaPanelContents ());
            container.ShowAll ();
            
            if (MoblinPanel.Instance.ToolbarPanel != null) {
                container.SetSizeRequest (
                    (int)MoblinPanel.Instance.ToolbarPanelWidth,
                    (int)MoblinPanel.Instance.ToolbarPanelHeight);
            }
            
            // Since the Panel is running, we don't actually ever want to quit!
            Banshee.ServiceStack.Application.ShutdownRequested += () => {
                elements_service.PrimaryWindow.Hide ();
                return false;
            };

            FindNowPlaying ();
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerStateChanged, PlayerEvent.StateChange | PlayerEvent.StartOfStream);
        }
        
        private void ReflectionHackeryUpTheNereid ()
        {
            // This is a horribly abusive method, but hey, this kind
            // of stuff is what Firefox extensions are made of!
            
            // First grab the type and instance of the primary window
            // and make sure we're only hacking the Nereid UI
            var pwin = elements_service.PrimaryWindow;
            var pwin_type = pwin.GetType ();
            if (pwin_type.FullName != "Nereid.PlayerInterface") {
                return;
            }
            
            // regular metacity does not seem to like this at all, crashing
            // and complaining "Window manager warning: Buggy client sent a 
            // _NET_ACTIVE_WINDOW message with a timestamp of 0 for 0x2e00020"
            if (MoblinPanel.Instance != null) {
                pwin.Decorated = false;
                pwin.Maximize ();
            }
            
            // Now we want to make the Toolbar work in the Moblin GTK theme
            var pwin_toolbar = (Toolbar)pwin_type.GetProperty ("HeaderToolbar").GetValue (pwin, null);
            var pwin_toolbar_align = (Alignment)pwin_toolbar.Parent;
            pwin_toolbar_align.TopPadding = 0;
            pwin_toolbar_align.BottomPadding = 6;
            pwin_type.GetMethod ("DisableHeaderToolbarExposeEvent").Invoke (pwin, null);
                        
            // Remove the volume button since Moblin enforces the global volume
            foreach (var child in pwin_toolbar.Children) {
                if (child.GetType ().FullName.StartsWith ("Banshee.Widgets.GenericToolItem")) {
                    var c = child as Container;
                    if (c != null && c.Children[0] is Banshee.Gui.Widgets.ConnectedVolumeButton) {
                        pwin_toolbar.Remove (child);
                        break;
                    }
                }
            }
            
            // Incredibly ugly hack to pack in a close button in a separate
            // toolbar so that it may be aligned at the top right of the
            // window (appears to float in the menubar)
            var pwin_header_table = (Table)pwin_type.GetProperty ("HeaderTable").GetValue (pwin, null);

            var close_button = new Banshee.Widgets.HoverImageButton (IconSize.Menu, "window-close") { DrawFocus = false };
            close_button.Image.Xpad = 1;
            close_button.Image.Ypad = 1;
            close_button.Clicked += (o, e) => Banshee.ServiceStack.Application.Shutdown ();
            
            var close_toolbar = new Toolbar () {
                ShowArrow = false,
                ToolbarStyle = ToolbarStyle.Icons
            };
            
            close_toolbar.Add (close_button);
            close_toolbar.ShowAll ();

            pwin_header_table.Attach (close_toolbar, 1, 2, 0, 1, 
                AttachOptions.Shrink, AttachOptions.Fill | AttachOptions.Expand, 0, 0);
            
            // Set the internal engine volume to 100%
            // FIXME: We should have something like PlayerEngine.InternalVolumeEnabled = false
            ServiceManager.PlayerEngine.Volume = 100;
        }

        private void OnPlayerStateChanged (PlayerEventArgs args)
        {
            var player = ServiceManager.PlayerEngine;
            if (player.CurrentState == PlayerState.Playing && player.CurrentTrack.HasAttribute (TrackMediaAttributes.VideoStream)) {
                if (now_playing != null) {
                    ServiceManager.SourceManager.SetActiveSource (now_playing);
                }

                PresentPrimaryInterface ();
            }
        }

        private void FindNowPlaying ()
        {
            foreach (var src in ServiceManager.SourceManager.Sources) {
                if (src.UniqueId.Contains ("now-playing")) {
                    now_playing = src;
                    break;
                }
            }

            if (now_playing != null)
                return;

            Banshee.ServiceStack.ServiceManager.SourceManager.SourceAdded += (args) => {
                if (now_playing == null && args.Source.UniqueId.Contains ("now-playing")) {
                    now_playing = args.Source;
                }
            };
        }
        
        public void PresentPrimaryInterface ()
        {
            elements_service.PrimaryWindow.Maximize ();
            elements_service.PrimaryWindow.Present ();
            if (MoblinPanel.Instance != null && MoblinPanel.Instance.ToolbarPanel != null) {
                MoblinPanel.Instance.ToolbarPanel.RequestHide ();
            }
        }
        
        public void Dispose ()
        {
            if (MoblinPanel.Instance != null) {
                MoblinPanel.Instance.Dispose ();
            }
            
            interface_action_service = null;
            elements_service = null;
        }

        string IService.ServiceName {
            get { return "MoblinService"; }
        }
    }
}
