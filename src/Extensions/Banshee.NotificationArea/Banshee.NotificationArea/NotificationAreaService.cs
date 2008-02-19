//
// NotificationAreaService.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Sebastian Dröge <slomo@circular-chaos.org>
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

using Notifications;

using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;
using Banshee.MediaEngine;
using Banshee.Widgets;

namespace Banshee.NotificationArea 
{
    public class NotificationAreaService : IExtensionService, IDisposable
    {
        private INotificationAreaBox notif_area;
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private ArtworkManager artwork_manager_service;
    
        private Menu menu;
		private RatingMenuItem rating_menu_item;
        private BansheeActionGroup actions;
        private uint ui_manager_id;
        
        private bool show_notifications;
        private string notify_last_title;
        private string notify_last_artist;
        private TrackInfo current_track;
        private Notification current_nf;
    
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
            
            for (int i = 0; i < menu.Children.Length; i++) {
                if (menu.Children[i].Name == "Next") {
                    rating_menu_item = new RatingMenuItem ();
                    rating_menu_item.Activated += OnItemRatingActivated;
                    ToggleRatingMenuSensitive ();
                    menu.Insert (rating_menu_item, i + 2);
                    break;
                }
            }

            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
            
            // Forcefully load this
            show_notifications = ShowNotifications;
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
                    Gdk.Pixbuf image = IconThemeUtils.LoadIcon (48, "music-player-banshee");
                    if (image != null) {
                        image = image.ScaleSimple (42, 42, Gdk.InterpType.Bilinear);
                    }
                    
                    Notification nf = new Notification (
                        Catalog.GetString ("Still Running"), 
                        Catalog.GetString ("Banshee was closed to the notification area. " + 
                            "Use the <i>Quit</i> option to end your session."),
                        image, notif_area.Widget);
                    nf.Urgency = Urgency.Low;
                    nf.Timeout = 4500;
                    nf.Show ();
                    
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
            switch (args.Event) {
                case PlayerEngineEvent.Iterate:
                    if (current_track != ServiceManager.PlayerEngine.CurrentTrack) {
                        current_track = ServiceManager.PlayerEngine.CurrentTrack;
                        ShowTrackNotification ();
                    }
                    break;
                case PlayerEngineEvent.StartOfStream:
                case PlayerEngineEvent.TrackInfoUpdated:
                    ToggleRatingMenuSensitive ();
                    ShowTrackNotification ();
                    break;
                case PlayerEngineEvent.EndOfStream:
                    current_track = null;
                    ToggleRatingMenuSensitive ();
                    break;
            }
        }
        
        private void OnItemRatingActivated (object o, EventArgs args)
        {
            if (ServiceManager.PlayerEngine.CurrentTrack != null) {
                ServiceManager.PlayerEngine.CurrentTrack.Rating = rating_menu_item.Value;
                ServiceManager.PlayerEngine.CurrentTrack.Save ();
                ServiceManager.PlayerEngine.TrackInfoUpdated ();
            }
        }
        
        private void ToggleRatingMenuSensitive () 
        {
            if (ServiceManager.PlayerEngine.CurrentTrack is LibraryTrackInfo) {
                rating_menu_item.Reset ((int)ServiceManager.PlayerEngine.CurrentTrack.Rating);
                rating_menu_item.Show ();
            } else {
                rating_menu_item.Hide ();
            }
        }
        
        private void ShowTrackNotification ()
        {
            // This has to happen before the next if, otherwise the last_* members aren't set correctly.
            if (current_track == null || (notify_last_title == current_track.DisplayTrackTitle 
                && notify_last_artist == current_track.DisplayArtistName)) {
                return;
            }
            
            notify_last_title = current_track.DisplayTrackTitle;
            notify_last_artist = current_track.DisplayArtistName;

            if (!show_notifications || elements_service.PrimaryWindow.HasToplevelFocus) {
                return;
            }
            
            string message = String.Format ("{0}\n<i>{1}</i>", 
                GLib.Markup.EscapeText (current_track.DisplayTrackTitle),
                GLib.Markup.EscapeText (current_track.DisplayArtistName));
            
            if (artwork_manager_service == null) {
                artwork_manager_service = ServiceManager.Get<ArtworkManager> ();
            }
            
            Gdk.Pixbuf image = null;
            
            if (artwork_manager_service != null) {
                image = artwork_manager_service.LookupScale (current_track.ArtistAlbumId, 42);
            }
            
            if (image == null) {
                image = IconThemeUtils.LoadIcon (48, "audio-x-generic");
                if (image != null) {
                    image.ScaleSimple (42, 42, Gdk.InterpType.Bilinear);
                }
            }
            
            try {
                if (current_nf != null) {
                    current_nf.Close ();
                }
                
                Notification nf = new Notification (Catalog.GetString ("Now Playing"), 
                    message, image, notif_area.Widget);
                nf.Urgency = Urgency.Low;
                nf.Timeout = 4500;
                nf.Show ();
                
                current_nf = nf;
            } catch (Exception e) {
                Hyena.Log.Warning (Catalog.GetString ("Cannot show notification"), e.Message);
            }
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
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.notification_area", "enabled",
            true,
            "Plugin enabled",
            "Notification area plugin enabled"
        );
                
        public static readonly SchemaEntry<bool> ShowNotificationsSchema = new SchemaEntry<bool> (
            "plugins.notification_area", "show_notifications",
            true,
            "Show notifications",
            "Show track information notifications when track starts playing"
        );
                
        public static readonly SchemaEntry<bool> NotifyOnCloseSchema = new SchemaEntry<bool> (
            "plugins.notification_area", "notify_on_close",
            true,
            "Show a notification when closing main window",
            "When the main window is closed, show a notification stating this has happened."
        );
                
        public static readonly SchemaEntry<bool> QuitOnCloseSchema = new SchemaEntry<bool> (
            "plugins.notification_area", "quit_on_close",
            false,
            "Quit on close",
            "Quit instead of hide to notification area on close"
        );
    }
}
