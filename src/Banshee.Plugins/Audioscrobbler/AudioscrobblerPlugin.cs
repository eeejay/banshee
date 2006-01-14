/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AudioscrobblerPlugin.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Chris Toshok (toshok@ximian.com)
 *             Aaron Bockover (aaron@aaronbock.net)
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
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Gtk;
using Mono.Unix;

using GConf;

using Banshee.MediaEngine;
using Banshee.Base;

namespace Banshee.Plugins.Audioscrobbler 
{
    public class AudioscrobblerPlugin : Banshee.Plugins.Plugin
    {
        private Engine protocol_engine;
        private GConf.Client gconf;
        private ActionGroup actions;
        private uint ui_manager_id;

        protected override string ConfigurationName { get { return "Audioscrobbler"; } }
        public override string DisplayName { get { return "Audioscrobbler"; } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Your profile page on Last.fm is automatically updated " +
                    "whenever you listen to music. It lets others see what " +
                    "you're listening to right now, and shows charts of " + 
                    "your listening history."
                );
            }
        }
        
        public override string [] Authors {
            get {
                return new string [] { 
                    "Chris Toshok",
                    "Aaron Bockover"
                };
            }
        }

        protected override void PluginInitialize()
        {
            RegisterConfigurationKey("EngineEnabled");
            RegisterConfigurationKey("Username");
            RegisterConfigurationKey("Password");
            
            gconf = Globals.Configuration;
            gconf.AddNotify(ConfigurationBase, GConfNotifier);

            InstallInterfaceActions();

            if(Enabled) {
                StartEngine();
            }
        }
        
        protected override void PluginDispose()
        {
            StopEngine();
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup(actions);
            gconf.RemoveNotify(ConfigurationBase, GConfNotifier);
            actions = null;
        }
        
        public override Gtk.Widget GetConfigurationWidget()
        {            
            return new AudioscrobblerConfigPage(this, true);
        }

        private void InstallInterfaceActions()
        {
            actions = new ActionGroup("Audioscrobbler");
            
            actions.Add(new ActionEntry [] {
                new ActionEntry("AudioscrobblerAction", null,
                    Catalog.GetString("Audioscrobbler"), null,
                    Catalog.GetString("Configure the Audioscrobbler plugin"), null),
                    
                new ActionEntry("AudioscrobblerVisitAction", null,
                    Catalog.GetString("Visit user profile page"), null,
                    Catalog.GetString("Visit your Audioscrobbler profile page"), OnVisitHomePage),
                
                new ActionEntry("AudioscrobblerGroupAction", null,
                    Catalog.GetString("Visit group page"), null,
                    Catalog.GetString("Visit the Banshee last.fm group page"), OnVisitGroupPage),
                
                new ActionEntry("AudioscrobblerConfigureAction", null,
                    Catalog.GetString("Configure..."), null,
                    Catalog.GetString("Configure the Audioscrobbler plugin"), OnConfigurePlugin)
            });
            
            actions.Add(new ToggleActionEntry [] { 
                new ToggleActionEntry("AudioscrobblerEnableAction", null,
                    Catalog.GetString("Enable song reporting"), "<control>L",
                    Catalog.GetString("Enable song reporting"), OnToggleEnabled, Enabled)
            });
            
            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("AudioscrobblerMenu.xml");
            
            Globals.ActionManager["AudioscrobblerVisitAction"].Sensitive = Username != null 
                && Username != String.Empty;
        }
        
        private void OnConfigurePlugin(object o, EventArgs args)
        {
            AudioscrobblerConfigPage page = new AudioscrobblerConfigPage(this, false);
            Dialog dialog = new Dialog();
            dialog.Title = Catalog.GetString("Audioscrobbler Reporting");
            dialog.BorderWidth = 12;
            dialog.VBox.Spacing = 10;
            dialog.HasSeparator = false;
            IconThemeUtils.SetWindowIcon(dialog);
            dialog.VBox.Add(page);
            dialog.VBox.Remove(dialog.ActionArea);
            dialog.AddButton(Stock.Close, ResponseType.Close);
                
            HBox bottom_box = new HBox();
            bottom_box.PackStart(page.Logo, true, true, 5);
            bottom_box.PackStart(dialog.ActionArea, false, false, 0);
            bottom_box.ShowAll();
            dialog.VBox.PackEnd(bottom_box, false, false, 0);
            
            dialog.Run();
            dialog.Destroy();
        }
        
        private void OnVisitHomePage(object o, EventArgs args)
        {
            Gnome.Url.Show("http://last.fm/user/" + Username);
        }        
        
        private void OnVisitGroupPage(object o, EventArgs args)
        {
            JoinGroup();
        }
        
        private void OnToggleEnabled(object o, EventArgs args)
        {
            Enabled = (o as ToggleAction).Active;
        }

        private void StartEngine()
        {
            Console.WriteLine("Audioscrobbler starting protocol engine");
            protocol_engine = new Engine();
            protocol_engine.SetUserPassword(Username, Password);
			protocol_engine.Start();
        }
        
        private void StopEngine()
        {
            if(protocol_engine != null) {
                Console.WriteLine("Audioscrobbler stopping protocol engine");
                protocol_engine.Stop();
                protocol_engine = null;
            }
        }

        private void GConfNotifier(object sender, NotifyEventArgs args)
        {
            //Console.WriteLine ("key that changed: {0}", args.Key);
            if(args.Key == ConfigurationKeys["EngineEnabled"]) {
                if((bool)args.Value == false) {
                    StopEngine();
                } else {
                    StartEngine();
                }
                
                (Globals.ActionManager["AudioscrobblerEnableAction"] as ToggleAction).Active = (bool)args.Value;
            } else if(args.Key == ConfigurationKeys["Username"] || args.Key == ConfigurationKeys["Password"]) {
                if(protocol_engine != null) {
                    protocol_engine.SetUserPassword(Username, Password);
                }
            }
        }

        internal void CreateAccount()
        {
            Gnome.Url.Show("http://www.last.fm/signup.php");
        }
        
        internal void JoinGroup()
        {
            Gnome.Url.Show("http://www.last.fm/group/Banshee");
        }

        internal string Username {
            get {
                return GetStringPref(ConfigurationKeys["Username"], String.Empty);
            }
            
            set {
                gconf.Set(ConfigurationKeys["Username"], value);
                Globals.ActionManager["AudioscrobblerVisitAction"].Sensitive = Username != null 
                    && Username != String.Empty;
            }
        }
        
        internal string Password {
            get {
                return GetStringPref(ConfigurationKeys["Password"], String.Empty);
            }
            
            set {
                gconf.Set(ConfigurationKeys["Password"], value);
            }
        }
        
        internal bool Enabled {
            get {
                return GetBoolPref(ConfigurationKeys["EngineEnabled"], false);
            }
            
            set {
                gconf.Set(ConfigurationKeys["EngineEnabled"], value);
            }
        }

        private string GetStringPref(string key, string def)
        {
            try {
                return (string)gconf.Get(key);
            } catch {
                return def;
            }
        }

        private bool GetBoolPref(string key, bool def)
        {
            try {
                return (bool)gconf.Get(key);
            } catch {
                return def;
            }
        }
    }
}
