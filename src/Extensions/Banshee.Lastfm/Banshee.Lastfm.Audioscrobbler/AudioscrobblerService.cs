//
// AudioscrobblerService.cs
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
//   Chris Toshok <toshok@ximian.com>
//   Ruben Vermeersch <ruben@savanne.be>
//   Aaron Bockover <aaron@abock.org>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Gtk;
using Mono.Unix;

using Hyena;

using Lastfm;
using Lastfm.Gui;

using Banshee.MediaEngine;
using Banshee.Base;
using Banshee.Configuration;
using Banshee.ServiceStack;
using Banshee.Gui;

using Banshee.Collection;

namespace Banshee.Lastfm.Audioscrobbler
{
    public class AudioscrobblerService : IExtensionService, IDisposable
    {
        private AudioscrobblerConnection connection;
        private ActionGroup actions;
        private uint ui_manager_id;
        private InterfaceActionService action_service;
        private Queue queue;        
        private Account account;
        
        private bool song_started = false; /* if we were watching the current song from the beginning */
        private bool queued; /* if current_track has been queued */
        private bool sought; /* if the user has sought in the current playing song */
    
        public AudioscrobblerService ()
        {
        }
        
        void IExtensionService.Initialize ()
        {
            account = new Account ();
            account.UserName = LastUserSchema.Get ();
            account.CryptedPassword = LastPassSchema.Get ();
            
            queue = new Queue ();
            connection = new AudioscrobblerConnection (account, queue);
            
            ServiceManager.PlayerEngine.EventChanged += OnPlayerEngineEventChanged;
            
            action_service = ServiceManager.Get<InterfaceActionService> ("InterfaceActionService");
            InterfaceInitialize ();
        
            if (!connection.Started) {
                connection.Connect ();
            }
        }
        
        public void InterfaceInitialize ()
        {
            actions = new ActionGroup ("Audioscrobbler");
            
            actions.Add (new ActionEntry [] {
                new ActionEntry ("AudioscrobblerAction", null,
                    Catalog.GetString ("_Audioscrobbler"), null,
                    Catalog.GetString ("Configure the Audioscrobbler plugin"), null),
                    
                new ActionEntry ("AudioscrobblerVisitAction", null,
                    Catalog.GetString ("Visit _user profile page"), null,
                    Catalog.GetString ("Visit your Audioscrobbler profile page"), OnVisitOwnProfile),
                
                new ActionEntry ("AudioscrobblerConfigureAction", null,
                    Catalog.GetString ("_Configure..."), null,
                    Catalog.GetString ("Configure the Audioscrobbler plugin"), OnConfigurePlugin)
            });
            
            actions.Add (new ToggleActionEntry [] { 
                new ToggleActionEntry ("AudioscrobblerEnableAction", null,
                    Catalog.GetString ("_Enable song reporting"), "<control>U",
                    Catalog.GetString ("Enable song reporting"), OnToggleEnabled, Enabled)
            });
            
            action_service.UIManager.InsertActionGroup (actions, 0);
            ui_manager_id = action_service.UIManager.AddUiFromResource ("AudioscrobblerMenu.xml");
            
            actions["AudioscrobblerVisitAction"].Sensitive = account.UserName != null && account.UserName != String.Empty;
        }
        
  
        public void Dispose ()
        {
            ServiceManager.PlayerEngine.EventChanged -= OnPlayerEngineEventChanged;
            
            connection.Stop ();
        
            action_service.UIManager.RemoveUi (ui_manager_id);
            action_service.UIManager.RemoveActionGroup (actions);
            actions = null;
        }
        
        private void OnPlayerEngineEventChanged (object o, PlayerEngineEventArgs args)
        {
            switch (args.Event) {
                /* Queue if we're watching this song from the beginning,
                 * it isn't queued yet and the user didn't seek until now,
                 * we're actually playing, song position and length are greater than 0
                 * and we already played half of the song or 240 seconds */
                 
                case PlayerEngineEvent.Iterate:
                    if (song_started && !queued && !sought &&
                        ServiceManager.PlayerEngine.CurrentState == PlayerEngineState.Playing &&
                        ServiceManager.PlayerEngine.Length > 0 &&
                        ServiceManager.PlayerEngine.Position > 0 &&
                        (ServiceManager.PlayerEngine.Position > ServiceManager.PlayerEngine.Length / 2 || ServiceManager.PlayerEngine.Position > (240 * 1000))) {
                            TrackInfo track = ServiceManager.PlayerEngine.CurrentTrack;
                            if (track == null) {
                                queued = sought = false;
                            } else {
                                if ((actions["AudioscrobblerEnableAction"] as ToggleAction).Active) {
                                    queue.Add (track, DateTime.Now - TimeSpan.FromSeconds (ServiceManager.PlayerEngine.Position));
                                }
                                queued = true;
                            }
                    }
                    
                    break;
                 
                /* Start of Stream: new song started */
                case PlayerEngineEvent.StartOfStream:
                    queued = sought = false;
                    song_started = true;
                    break;
                
                /* End of Stream: song finished */
                case PlayerEngineEvent.EndOfStream:
                    song_started = queued = sought = false;
                    break;
                
                /* Did the user seek? */
                case PlayerEngineEvent.Seek:
                    sought = true;
                    break;
            }
        }
        
        private void OnConfigurePlugin (object o, EventArgs args)
        {
            AccountLoginDialog dialog = new AccountLoginDialog (account, true);
            dialog.SaveOnEdit = true;
            if (account.UserName == null) {
                dialog.AddSignUpButton ();
            }
            dialog.Run ();
            dialog.Destroy ();
        }
        
        private void OnVisitOwnProfile (object o, EventArgs args)
        {
            account.VisitUserProfile (account.UserName);
        }
        
        private void OnToggleEnabled (object o, EventArgs args)
        {
            Enabled = (o as ToggleAction).Active;
        }
        
        internal bool Enabled {
            get { return EngineEnabledSchema.Get (); }
            set { 
                EngineEnabledSchema.Set (value);
                if (!connection.Started) {
                    connection.Connect ();
                }
                
                (actions["AudioscrobblerEnableAction"] as ToggleAction).Active = value;
            }
        }
           
        public static readonly SchemaEntry<string> LastUserSchema = new SchemaEntry<string> (
            "plugins.lastfm", "username", "", "Last.fm user", "Last.fm username"
        );

        public static readonly SchemaEntry<string> LastPassSchema = new SchemaEntry<string> (
            "plugins.lastfm", "password_hash", "", "Last.fm password", "Last.fm password (hashed)"
        );
   
        public static readonly SchemaEntry<bool> EngineEnabledSchema = new SchemaEntry<bool> (
            "plugins.audioscrobbler", "engine_enabled",
            false,
            "Engine enabled",
            "Audioscrobbler reporting engine enabled"
        );
        
        string IService.ServiceName {
            get { return "AudioscrobblerService"; }
        }
    }
}
