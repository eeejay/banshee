/***************************************************************************
 *  RadioPlugin.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using Gtk;
using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Configuration;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Radio.RadioPlugin)
        };
    }
}

namespace Banshee.Plugins.Radio
{
    public class RadioPlugin : Banshee.Plugins.Plugin
    {
        protected override string ConfigurationName { get { return "radio"; } }
        public override string DisplayName { get { return Catalog.GetString("Radio"); } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Provides Internet radio/streaming audio station support"
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] {
                    "Aaron Bockover"
                };
            }
        }
        
        private RadioSource source;
        private StationManager manager;
        
        private ActionGroup actions;
        private uint ui_manager_id;
        
        protected override void PluginInitialize()
        {
            if(manager == null) {
                manager = new StationManager();
                manager.StationsRefreshing += delegate { DisableRefresh(); };
                manager.StationsLoadFailed += delegate { EnableRefresh(); };
                manager.StationsLoaded += delegate { EnableRefresh(); };
            }
        }

        protected override void InterfaceInitialize()
        {
            InstallInterfaceActions();
            
            source = new RadioSource(this);
            SourceManager.AddSource(source);
            
            manager.ReloadStations(false);
        }
        
        protected override void PluginDispose()
        {
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup(actions);

            actions = null;
            SourceManager.RemoveSource(source);
        }
        
        private void InstallInterfaceActions()
        {
            actions = new ActionGroup("Radio");
            
            actions.Add(new ActionEntry [] {
                new ActionEntry("RefreshRadioAction", Stock.Refresh,
                    Catalog.GetString("Refresh Stations"), null,
                    Catalog.GetString("Refresh stations from the Banshee Radio Web Service"), OnRefreshStations)
            });
            
            actions.Add(new ActionEntry [] {
                new ActionEntry("CopyUriAction", Stock.Copy,
                    Catalog.GetString("Copy URI"), null,
                    Catalog.GetString("Copy stream URI to clipboard"), null)
            });

            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("RadioActions.xml");
        }
        
        private void EnableRefresh()
        {
            actions.GetAction("RefreshRadioAction").Sensitive = true;
        }
        
        private void DisableRefresh()
        {
            actions.GetAction("RefreshRadioAction").Sensitive = false;
        }
        
        private void OnRefreshStations(object o, EventArgs args)
        {
            manager.ReloadStations(true);
        }
        
        public StationManager StationManager {
            get { return manager; }
        }
        
        public ActionGroup Actions {
            get { return actions; }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.radio", "enabled",
            true,
            "Plugin enabled",
            "Radio plugin enabled"
        );
    }
}
