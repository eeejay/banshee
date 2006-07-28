/***************************************************************************
 *  DaapSource.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
using System.Collections.Generic;
using System.Collections;
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Plugins.Daap
{
    public class DaapSource : Source
    {
        private Service service;
        private Client client;
        private DAAP.Database database;
        private DatabaseProxy database_proxy;
        private bool is_activating;
        
        public DaapSource(Service service) : base(service.Name, 300)
        {
            this.service = service;
            is_activating = false;
            database_proxy = new DatabaseProxy();
        }
        
        public override void Activate()
        {
            if(client == null && !is_activating) {
                is_activating = true;
                Console.WriteLine("Connecting to DAAP share: " + service);
                ThreadAssist.Spawn(delegate {
                    client = new Client(service);
                    client.Updated += OnClientUpdated;
                    try {
                        if(client.AuthenticationMethod == AuthenticationMethod.None) {
                            client.Login();
                        } else {
                            ThreadAssist.ProxyToMain(PromptLogin);
                        }
                    } catch(Exception e) {
                        LogCore.Instance.PushError(Catalog.GetString("Cannot login to DAAP share"),
                            e.Message);
                    }
                    
                    is_activating = false;
                });
            }
        }

        private void AuthenticatedLogin(string username, string password)
        {
            ThreadAssist.Spawn(delegate {
                try {
                    client.Login(username, password);
                } catch(AuthenticationException) {
                    ThreadAssist.ProxyToMain(PromptLogin);
                }
            });
        }
        
        private void PromptLogin(object o, EventArgs args)
        {
            DaapLoginDialog dialog = new DaapLoginDialog(client.Name, 
            client.AuthenticationMethod == AuthenticationMethod.UserAndPassword);
            if(dialog.Run() == (int)Gtk.ResponseType.Ok) {
                AuthenticatedLogin(dialog.Username, dialog.Password);
            } else {
                Dispose();
            }

            dialog.Destroy();
        }
        
        protected override void OnDispose()
        {
            Unmap();
        }
        
        public override bool Unmap()
        {
            if(client != null) {
                client.Logout();
                client.Dispose();
                client = null;
                database = null;
            }
            
            if(database != null) {
                database.SongAdded -= OnDatabaseSongAdded;
                database.SongRemoved -= OnDatabaseSongRemoved;
                DaapCore.ProxyServer.UnregisterDatabase(database);
                database = null;
            }
            
            return true;
        }
        
        public override string UnmapIcon {
            get { return Gtk.Stock.Disconnect; }
        }

        public override string UnmapLabel {
            get { return Catalog.GetString("Disconnect"); }
        }

        private void OnClientUpdated(object o, EventArgs args)
        {
            if(database == null && client.Databases.Length > 0) {
                database = client.Databases[0];
                database.SongAdded += OnDatabaseSongAdded;
                database.SongRemoved += OnDatabaseSongRemoved;
                database_proxy.Database = database;
                DaapCore.ProxyServer.RegisterDatabase(database);
            }
            
            Name = client.Name;
            
            ThreadAssist.ProxyToMain(delegate {
                OnUpdated();
            });
        }
        
        private void OnDatabaseSongAdded(object o, Song song)
        {
            OnTrackAdded(new DaapTrackInfo(song, database));
        }
        
        private void OnDatabaseSongRemoved(object o, Song song)
        {
            OnTrackRemoved(new DaapTrackInfo(song, database, false));
        }
        
        public override IEnumerable<TrackInfo> Tracks {
            get { return database_proxy; }
        }
        
        public override int Count {
            get { return database == null ? -1 : database.SongCount; }
        }
        
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, "network-server", Gtk.Stock.Network); 
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
    }
}
