//
// DaapSource.cs
//
// Authors:
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2008 Alexander Hixon
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
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Daap
{
    public class DaapSource : PrimarySource, IDurationAggregator, IDisposable
    {
        private Service service;
        private DAAP.Client client;
        private DAAP.Database database;
        
        private bool is_activating;
        private SourceMessage status_message;
        
        public DaapSource (Service service) : base (Catalog.GetString ("Music Share"), service.Name, (service.Address.ToString () + service.Port).Replace (":", "").Replace (".", ""), 300)
        {
            this.service = service;
            Properties.SetString ("Icon.Name", "computer");
            
            AfterInitialized ();
        }
        
        public override void Activate ()
        {
            if (client != null || is_activating) {
            	return;
            }
            
            is_activating = true;
            base.Activate ();
            
            SetMessage (String.Format (Catalog.GetString ("Connecting to {0}"), service.Name), true);
            
            Console.WriteLine ("Connecting to {0}:{1}", service.Address, service.Port);
            
            ThreadAssist.Spawn (delegate {
                try {
                    // XXX: We get connect failures if we try with IPv6 address - what's up with that?!
                    // Investigate.
                    
                    if (service.Address.ToString ().Contains (".")) {
                        client = new DAAP.Client (service);
                    } else {
                        Console.WriteLine ("Was IPv6 address - we're probably going to die... :(");
                    }
                    //client = new Client (System.Net.IPAddress.Parse ("127.0.0.1"), service.Port);
                    client.Updated += OnClientUpdated;
                    
                    if (client.AuthenticationMethod == AuthenticationMethod.None) {
                        client.Login ();
                    }/* else {
                        ThreadAssist.ProxyToMain (PromptLogin);
                    }*/
                } catch(Exception e) {
                    /*ThreadAssist.ProxyToMain(delegate {
                        DaapErrorView error_view = new DaapErrorView(this, DaapErrorType.BrokenAuthentication);
                        while(box.Children.Length > 0) {
                            box.Remove(box.Children[0]);
                        }
                        box.PackStart(error_view, true, true, 0);
                        error_view.Show();
                    });*/
                    Console.WriteLine ("Error while connecting to remote: {0}", e);
                }
               
                is_activating = false;
            });
            
            Reload ();
        }
        
        internal bool Disconnect (bool logout)
        {
            if (client != null) {
                if (logout) {
                    client.Logout ();
                }
                
                client.Dispose ();
                client = null;
                database = null;
            }
            
            if (database != null) {
                database.TrackAdded -= OnDatabaseTrackAdded;
                // TODO
                //database.TrackRemoved -= OnDatabaseTrackRemoved;
                database = null;
            }

            ClearChildSources();
            
            return true;
        }
        
        public void Dispose ()
        {
            Disconnect (true);
        }
        
        private void SetMessage (string message, bool spinner)
        {
            if (status_message == null) {
                status_message = new SourceMessage (this);
                PushMessage (status_message);
                
                // Nice hack here.
                status_message.FreezeNotify ();
                status_message.Text = message;
                status_message.CanClose = !spinner;
                status_message.IsSpinning = spinner;
                status_message.SetIconName (null);
                
                status_message.ThawNotify ();
            }
            
            //string status_name = String.Format ("<i>{0}</i>", GLib.Markup.EscapeText (Name));
            
            status_message.FreezeNotify ();
            status_message.Text = message;
            status_message.CanClose = !spinner;
            status_message.IsSpinning = spinner;
            status_message.SetIconName (null);
            
            status_message.ThawNotify ();
        }
        
        private void HideMessage ()
        {
            if (status_message != null) {
                RemoveMessage (status_message);
                status_message = null;
            }
        }
        
        private void OnClientUpdated (object o, EventArgs args)
        {
            if (database == null && client.Databases.Count > 0) {
                database = client.Databases[0];
                database.TrackAdded += OnDatabaseTrackAdded;
                //database.TrackRemoved += OnDatabaseTrackRemoved;
                //database_proxy.Database = database;
                //DaapCore.ProxyServer.RegisterDatabase (database);
                //AddPlaylistSources ();
                
                foreach (Track track in database.Tracks) {
                    //track_model.Add (track);
                    DatabaseTrackInfo r = new DatabaseTrackInfo ();
                    r.TrackTitle = track.Title;
                    r.AlbumTitle = track.Album;
                    r.ArtistName = track.Artist;
                    
                    r.TrackNumber = track.TrackNumber;
                    r.Year = track.Year;
                    r.Duration = track.Duration;
                    r.PrimarySource = this;
                    
                    r.Save ();
                }
                
                Reload ();
                
                ThreadAssist.ProxyToMain(delegate {
                    HideMessage ();
                });
            }
            
            Name = client.Name;
            
            OnUpdated ();
        }
        
        public void OnDatabaseTrackAdded (object o, TrackArgs args)
        {
            Console.WriteLine ("Added: {0}", args.Track);
        }
        
        public override bool CanRemoveTracks {
            get { return false; }
        }

        public override bool CanDeleteTracks {
            get { return false; }
        }

        public override bool ConfirmRemoveTracks {
            get { return false; }
        }

        public override void RemoveSelectedTracks ()
        {
        }

        public override void DeleteSelectedTracks ()
        {
            throw new Exception ("Should not call DeleteSelectedTracks on DaapSource");
        }
        
        public override bool HasDependencies {
            get { return false; }
        }
        
        protected override string TypeUniqueId {
            get { return "daap"; }
        }
    }
}
