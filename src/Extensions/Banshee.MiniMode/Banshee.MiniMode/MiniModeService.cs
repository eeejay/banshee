using System;
using Gtk;
using Mono.Unix;

using Banshee.Base;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.Sources;

namespace Banshee.MiniMode
{
    public class MiniModeService : IExtensionService, IDisposable
    {
        private MiniMode mini_mode = null;
        private Menu viewMenu;
        private MenuItem menuItem;
        private InterfaceActionService action_service;
        
        void IExtensionService.Initialize ()
        {
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            
            viewMenu = (action_service.UIManager.GetWidget ("/MainMenu/ViewMenu") as MenuItem).Submenu as Menu;
            menuItem = new MenuItem (Catalog.GetString ("_Mini Mode"));
            menuItem.Activated += delegate {
                if (mini_mode == null) {
                    mini_mode = new MiniMode (ServiceManager.Get<GtkElementsService> ().PrimaryWindow);
                }

                ServiceManager.Get<GtkElementsService> ().PrimaryWindow = mini_mode;
                mini_mode.Enable ();
            };
            viewMenu.Insert (menuItem, 2);
            menuItem.Show ();
        }
        
        public void Dispose ()
        {
            if (viewMenu != null && menuItem != null) {
                viewMenu.Remove (menuItem);
            }
        
            if (mini_mode != null) {
                // We'll do our visual cleaning in a timeout to avoid
                // glitches when Banshee quits. Besides, the plugin window is
                // accessible only on the full mode, so this won't cause any
                // trouble.
                GLib.Timeout.Add (1000, delegate {
                    try {
                        mini_mode.Hide ();
                    } catch { 
                    }
                    return false;
                });
            }
        }
        
        string IService.ServiceName {
            get { return "MiniModeService"; }
        }
    }
}
