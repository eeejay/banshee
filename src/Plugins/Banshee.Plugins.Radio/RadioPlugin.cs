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
        
        private ActionGroup source_actions;
        private ActionGroup popup_actions;
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
            Globals.ActionManager.UI.RemoveActionGroup(source_actions);
            Globals.ActionManager.UI.RemoveActionGroup(popup_actions);

            source_actions = null;
            popup_actions = null;
            
            SourceManager.RemoveSource(source);
        }
        
        private void InstallInterfaceActions()
        {
            source_actions = new ActionGroup("Radio");
            popup_actions = new ActionGroup("Radio Popup");
            
            source_actions.Add(new ActionEntry [] {
                new ActionEntry("RefreshRadioAction", Stock.Refresh,
                    Catalog.GetString("Refresh Stations"), null,
                    Catalog.GetString("Refresh stations from the Banshee Radio Web Service"), OnRefreshStations),
            });
            
            popup_actions.Add(new ActionEntry [] {
                new ActionEntry("NewStationGroupAction", Stock.New,
                    Catalog.GetString("New Station Group"), null,
                    Catalog.GetString("Create a new local station group"), null),
                    
                new ActionEntry("CopyUriAction", Stock.Copy,
                    Catalog.GetString("Copy URI"), null,
                    Catalog.GetString("Copy stream URI to clipboard"), null),
                    
                new ActionEntry("EditAction", Stock.Edit,
                    Catalog.GetString("Edit"), null,
                    Catalog.GetString("Edit Radio Station"), null),
                    
                new ActionEntry("AddAction", Stock.Add,
                    Catalog.GetString("Add Station"), null,
                    Catalog.GetString("Add new Radio Station"), null),
                    
                new ActionEntry("RemoveAction", Stock.Remove,
                    Catalog.GetString("Remove"), null,
                    Catalog.GetString("Remove selected Radio Station"), null)
            });

            Globals.ActionManager.UI.InsertActionGroup(source_actions, 0);
            Globals.ActionManager.UI.InsertActionGroup(popup_actions, 0);
            
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("RadioActions.xml");
        }
        
        private void EnableRefresh()
        {
            source_actions.GetAction("RefreshRadioAction").Sensitive = true;
        }
        
        private void DisableRefresh()
        {
            source_actions.GetAction("RefreshRadioAction").Sensitive = false;
        }
        
        private void OnRefreshStations(object o, EventArgs args)
        {
            manager.ReloadStations(true);
        }
        
        public StationManager StationManager {
            get { return manager; }
        }
        
        public ActionGroup SourceActions {
            get { return source_actions; }
        }
        
        public ActionGroup PopupActions {
            get { return popup_actions; }
        }
        
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.radio", "enabled",
            true,
            "Plugin enabled",
            "Radio plugin enabled"
        );
    }
}
