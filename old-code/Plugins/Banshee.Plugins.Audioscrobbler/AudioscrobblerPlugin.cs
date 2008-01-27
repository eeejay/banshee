/***************************************************************************
 *  AudioscrobblerPlugin.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Chris Toshok <toshok@ximian.com>
 *             Aaron Bockover <aaron@abock.org>
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

using Banshee.MediaEngine;
using Banshee.Base;
using Banshee.Configuration;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.Audioscrobbler.AudioscrobblerPlugin)
        };
    }
}

namespace Banshee.Plugins.Audioscrobbler 
{
    public class AudioscrobblerPlugin : Banshee.Plugins.Plugin
    {
        private Engine protocol_engine;
        private ActionGroup actions;
        private uint ui_manager_id;
        
        protected override string ConfigurationName { get { return "audioscrobbler"; } }
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
            if(Enabled) {
                StartEngine();
            }
        }
        
        protected override void InterfaceInitialize()
        {
            actions = new ActionGroup("Audioscrobbler");
            
            actions.Add(new ActionEntry [] {
                new ActionEntry("AudioscrobblerAction", null,
                    Catalog.GetString("_Audioscrobbler"), null,
                    Catalog.GetString("Configure the Audioscrobbler plugin"), null),
                    
                new ActionEntry("AudioscrobblerVisitAction", null,
                    Catalog.GetString("Visit _user profile page"), null,
                    Catalog.GetString("Visit your Audioscrobbler profile page"), OnVisitHomePage),
                
                new ActionEntry("AudioscrobblerGroupAction", null,
                    Catalog.GetString("Visit _group page"), null,
                    Catalog.GetString("Visit the Banshee last.fm group page"), OnVisitGroupPage),
                
                new ActionEntry("AudioscrobblerConfigureAction", null,
                    Catalog.GetString("_Configure..."), null,
                    Catalog.GetString("Configure the Audioscrobbler plugin"), OnConfigurePlugin)
            });
            
            actions.Add(new ToggleActionEntry [] { 
                new ToggleActionEntry("AudioscrobblerEnableAction", null,
                    Catalog.GetString("_Enable song reporting"), "<control>U",
                    Catalog.GetString("Enable song reporting"), OnToggleEnabled, Enabled)
            });
            
            Globals.ActionManager.UI.InsertActionGroup(actions, 0);
            ui_manager_id = Globals.ActionManager.UI.AddUiFromResource("AudioscrobblerMenu.xml");
            
            Globals.ActionManager["AudioscrobblerVisitAction"].Sensitive = Username != null 
                && Username != String.Empty;
        }
        
        protected override void PluginDispose()
        {
            StopEngine();
            Globals.ActionManager.UI.RemoveUi(ui_manager_id);
            Globals.ActionManager.UI.RemoveActionGroup(actions);
            actions = null;
        }
        
        public override Gtk.Widget GetConfigurationWidget()
        {            
            return new AudioscrobblerConfigPage(this, true);
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
            Banshee.Web.Browser.Open("http://last.fm/user/" + Username);
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
            if(protocol_engine == null) {
                LogCore.Instance.PushDebug("Audioscrobbler starting protocol engine", "");
                protocol_engine = new Engine();
                protocol_engine.SetUserPassword(Username, Password);
                protocol_engine.Start();
            }
        }
        
        private void StopEngine()
        {
            if(protocol_engine != null) {
                LogCore.Instance.PushDebug("Audioscrobbler stopping protocol engine", "");
                protocol_engine.Stop();
                protocol_engine = null;
            }
        }

        internal void CreateAccount()
        {
            Banshee.Web.Browser.Open("http://www.last.fm/signup.php");
        }
        
        internal void JoinGroup()
        {
            Banshee.Web.Browser.Open("http://www.last.fm/group/Banshee");
        }

        internal string Username {
            get { return UsernameSchema.Get(); }
            set { 
                UsernameSchema.Set(value);
                Globals.ActionManager["AudioscrobblerVisitAction"].Sensitive = Username != null 
                    && Username != String.Empty;
                if(protocol_engine != null) {
                    protocol_engine.SetUserPassword(Username, Password);
                }
            }
        }
        
        internal string Password {
            get { return PasswordSchema.Get(); }
            set { 
                PasswordSchema.Set(value); 
                if(protocol_engine != null) {
                    protocol_engine.SetUserPassword(Username, Password);
                }
            }
        }
        
        internal bool Enabled {
            get { return EngineEnabledSchema.Get(); }
            set { 
                EngineEnabledSchema.Set(value);
            
                if(!value) {
                    StopEngine();
                } else {
                    StartEngine();
                }
                
                (Globals.ActionManager["AudioscrobblerEnableAction"] as ToggleAction).Active = value;
            }
        }
           
        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool>(
            "plugins.audioscrobbler", "enabled",
            true,
            "Plugin enabled",
            "Audioscrobbler reporting plugin enabled"
        );
        
        public static readonly SchemaEntry<string> UsernameSchema = new SchemaEntry<string>(
            "plugins.audioscrobbler", "username",
            String.Empty,
            "Username",
            "last.fm Username"
        );
   
        public static readonly SchemaEntry<string> PasswordSchema = new SchemaEntry<string>(
            "plugins.audioscrobbler", "password",
            String.Empty,
            "Password",
            "last.fm Password"
        );
   
        public static readonly SchemaEntry<bool> EngineEnabledSchema = new SchemaEntry<bool>(
            "plugins.audioscrobbler", "engine_enabled",
            false,
            "Engine enabled",
            "Audioscrobbler reporting engine enabled"
        );
    }
}
