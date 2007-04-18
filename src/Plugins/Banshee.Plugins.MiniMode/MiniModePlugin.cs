using System;
using Gtk;

using Mono.Gettext;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Configuration;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.MiniMode.MiniModePlugin)
        };
    }
}

namespace Banshee.Plugins.MiniMode
{
    public class MiniModePlugin : Banshee.Plugins.Plugin
    {
        private MiniMode mini_mode = null;
        private Menu viewMenu;
        private MenuItem menuItem;
        
        protected override string ConfigurationName { 
            get { return "minimode"; } 
        }
        
        public override string DisplayName { 
            get { return "Mini Mode"; } 
        }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Mini Mode allows controlling Banshee through a small " +
                    "window with only playback controls and track information."
                );
            }
        }
        
        public override string [] Authors {
            get { 
                return new string [] {
                    "Felipe Almeida Lessa",
                    "Aaron Bockover"
                };
            }
        }
 
        // --------------------------------------------------------------- //

        protected override void PluginInitialize()
        {
        }
        
        protected override void InterfaceInitialize()
        {
            viewMenu = (Globals.ActionManager.GetWidget("/MainMenu/ViewMenu") as MenuItem).Submenu as Menu;
            menuItem = new MenuItem(Catalog.GetString("_Mini Mode"));
            menuItem.Activated += delegate {
                if (mini_mode == null) {
                    mini_mode =  new MiniMode();
                }

                mini_mode.Show();
            };
            viewMenu.Insert(menuItem, 2);
            menuItem.Show();
        }
        
        protected override void PluginDispose()
        {
            if(viewMenu != null && menuItem != null) {
                viewMenu.Remove(menuItem);
            }
        
            if(mini_mode != null) {
                // We'll do our visual cleaning in a timeout to avoid
                // glitches when Banshee quits. Besides, the plugin window is
                // accessible only on the full mode, so this won't cause any
                // trouble.
                GLib.Timeout.Add(1000, delegate {
                    try {
                        mini_mode.Hide();
                    } catch { 
                    }
                    return false;
                });
            }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.minimode", "enabled",
            true,
            "Plugin enabled",
            "MiniMode plugin enabled"
        );
    }
}
