//
// NotificationAreaService.cs
//
// Authors:
//   Sebastian Dröge <slomo@circular-chaos.org>
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
// Copyright (C) 2006-2007 Sebastian Dröge
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
using Mono.Unix;

//using Notifications;

using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.MediaEngine;

namespace Banshee.NotificationArea 
{
    public class NotificationAreaService : IExtensionService, IDisposable
    {
        private INotificationAreaBox notif_area;
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
    
        private Menu menu;
		//private RatingMenuItem rating_menu_item;
        private BansheeActionGroup actions;
        private uint ui_manager_id;
        
        private bool show_notifications = true;
    
        public NotificationAreaService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();
        
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
            }
                    
            ServiceStartup ();
        }
        
        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null) {
                return false;
            }
            
            Initialize ();
            
            ServiceManager.ServiceStarted -= OnServiceStarted;
            BuildNotificationArea ();
            
            return true;
        }
        
        private void Initialize ()
        {
            interface_action_service.GlobalActions.Add (new ActionEntry [] {
                new ActionEntry ("CloseAction", Stock.Close,
                    Catalog.GetString ("_Close"), "<Control>W",
                    Catalog.GetString ("Close"), CloseWindow)
            });
            
            actions = new BansheeActionGroup ("NotificationArea");
            actions.Add (new ToggleActionEntry [] {
                new ToggleActionEntry ("ToggleNotificationsAction", null,
                    Catalog.GetString ("_Show Notifications"), null,
                    Catalog.GetString ("Show notifications when song changes"), ToggleNotifications, ShowNotifications)
            });
            
            interface_action_service.AddActionGroup (actions);
            
            ui_manager_id = interface_action_service.UIManager.AddUiFromResource ("NotificationAreaMenu.xml");      
            menu = (Menu)interface_action_service.UIManager.GetWidget("/NotificationAreaIconMenu");
            menu.Show ();
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
        }
        
        public void Dispose ()
        {
            if (notif_area != null) {
                notif_area.Dispose ();
                notif_area = null;
            }
            
            ServiceManager.PlayerEngine.EventChanged -= OnPlayerEngineEventChanged;
            
            elements_service.PrimaryWindowClose = null;
            
            interface_action_service.UIManager.RemoveUi (ui_manager_id);
            interface_action_service.UIManager.RemoveActionGroup (actions);
            
            elements_service = null;
            interface_action_service = null;
        }
        
        private void BuildNotificationArea () 
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) {
                try {
                    notif_area = new X11NotificationAreaBox ();
                } catch {
                }
            }
            
            if (notif_area == null) {
                notif_area = new GtkNotificationAreaBox ();
            }
            
            notif_area.Disconnected += OnNotificationAreaDisconnected;
            notif_area.Activated += OnNotificationAreaActivated;
            notif_area.PopupMenuEvent += OnNotificationAreaPopupMenuEvent;
            
            if (!QuitOnCloseSchema.Get ()) {
                RegisterCloseHandler ();
            }
        }
        
        private void RegisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose == null) {
                elements_service.PrimaryWindowClose = OnPrimaryWindowClose;
            }
        }
        
        private void UnregisterCloseHandler ()
        {
            if (elements_service.PrimaryWindowClose != null) {
                elements_service.PrimaryWindowClose = null;
            }
        }
        
        private bool OnPrimaryWindowClose ()
        {
            CloseWindow (null, null);
            return true;
        }
        
        private void OnNotificationAreaDisconnected (object o, EventArgs args)
        {
            // Ensure we don't keep the destroyed reference around
            if (notif_area != null) {
                notif_area.Disconnected -= OnNotificationAreaDisconnected;
                notif_area.Activated -= OnNotificationAreaActivated;
                notif_area.PopupMenuEvent -= OnNotificationAreaPopupMenuEvent;
            }
            
            BuildNotificationArea ();
        }
        
        private void OnNotificationAreaActivated (object o, EventArgs args)
        {
            elements_service.PrimaryWindow.ToggleVisibility ();
        }
        
        private void OnNotificationAreaPopupMenuEvent (object o, PopupMenuArgs args)
        {
            menu.Popup (null, null, notif_area.PositionMenu, 3, Gtk.Global.CurrentEventTime);
        }
        
        private void CloseWindow (object o, EventArgs args)
        {
            try {
                if (NotifyOnCloseSchema.Get ()) {
                    /*Gdk.Pixbuf image = Branding.ApplicationLogo.ScaleSimple(42, 42, Gdk.InterpType.Bilinear);
                    Notification nf = new Notification(
                        Catalog.GetString("Still Running"), 
                        Catalog.GetString("Banshee was closed to the notification area. " + 
                            "Use the <i>Quit</i> option to end your session."),
                        image, event_box);
                    nf.Urgency = Urgency.Low;
                    nf.Timeout = 4500;
                    nf.Show();*/
                    
                    NotifyOnCloseSchema.Set (false);
                }
            } catch {
            }

            elements_service.PrimaryWindow.ToggleVisibility ();
        }

        private void ToggleNotifications (object o, EventArgs args)
        {
            ShowNotifications = ((ToggleAction)actions["ToggleNotificationsAction"]).Active;
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args) 
        {
            /*switch (args.Event) {
                case PlayerEngineEvent.Iterate:
                    if(PlayerEngineCore.CurrentTrack != null) {
                        popup.Duration = (uint)PlayerEngineCore.CurrentTrack.Duration.TotalSeconds;
                        popup.Position = PlayerEngineCore.Position;

                        if (current_track != PlayerEngineCore.CurrentTrack) {
                            current_track = PlayerEngineCore.CurrentTrack;
                            ShowNotification();
                        }
                    } else {
                        popup.Duration = 0;
                        popup.Position = 0;
                    }
                    break;
                case PlayerEngineEvent.StartOfStream:
                case PlayerEngineEvent.TrackInfoUpdated:
                    ToggleRatingMenuSensitive();
                    FillPopup();
                    ShowNotification();
                    break;
                case PlayerEngineEvent.EndOfStream:
                    // only hide the popup when we don't play again after 250ms
                    GLib.Timeout.Add(250, delegate {
                        if (PlayerEngineCore.CurrentState != PlayerEngineState.Playing) {
                            ToggleRatingMenuSensitive();
                            popup.Duration = 0;
                            popup.Position = 0;
                            can_show_popup = false;
                            popup.Hide();
                         }
                         return false;
                    });
                    break;
            }*/
        }
        
        public bool ShowNotifications {
            get { 
                show_notifications = ShowNotificationsSchema.Get (); 
                return show_notifications;
            }
            
            set { 
                ShowNotificationsSchema.Set (value);
                show_notifications = value;
            }
        }
        
        public bool QuitOnClose {
            get {
                return QuitOnCloseSchema.Get ();
            }
            
            set {
                QuitOnCloseSchema.Set (value);
                if (value) {
                    UnregisterCloseHandler ();
                } else {
                    RegisterCloseHandler ();
                }
            }
        }
    
        string IService.ServiceName {
            get { return "NotificationAreaService"; }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.notification_area", "enabled",
            true,
            "Plugin enabled",
            "Notification area plugin enabled"
        );
                
        public static readonly SchemaEntry<bool> ShowNotificationsSchema = new SchemaEntry<bool>(
            "plugins.notification_area", "show_notifications",
            true,
            "Show notifications",
            "Show track information notifications when track starts playing"
        );
                
        public static readonly SchemaEntry<bool> NotifyOnCloseSchema = new SchemaEntry<bool>(
            "plugins.notification_area", "notify_on_close",
            true,
            "Show a notification when closing main window",
            "When the main window is closed, show a notification stating this has happened."
        );
                
        public static readonly SchemaEntry<bool> QuitOnCloseSchema = new SchemaEntry<bool>(
            "plugins.notification_area", "quit_on_close",
            false,
            "Quit on close",
            "Quit instead of hide to notification area on close"
        );
    }
}
