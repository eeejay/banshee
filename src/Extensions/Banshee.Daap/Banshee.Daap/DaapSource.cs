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
    public class DaapSource : PrimarySource, IDurationAggregator, IDisposable, IUnmapableSource
    {
        private Service service;
        private DAAP.Client client;
        private DAAP.Database database;
        
        public DAAP.Database Database {
            get { return database; }
        }
        
        private bool is_activating;
        private SourceMessage status_message;
        
        public DaapSource (Service service) : base (Catalog.GetString ("Music Share"), service.Name, (service.Address.ToString () + service.Port).Replace (":", "").Replace (".", ""), 300)
        {
            this.service = service;
            Properties.SetString ("Icon.Name", "computer");
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Disconnect"));
            
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
                    client = new DAAP.Client (service);
                    client.Updated += OnClientUpdated;
                    
                    if (client.AuthenticationMethod == AuthenticationMethod.None) {
                        client.Login ();
                    } else {
                        ThreadAssist.ProxyToMain (PromptLogin);
                    }
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
            // Stop currently playing track if its from us.
            try {
                if (ServiceManager.PlayerEngine.CurrentState == Banshee.MediaEngine.PlayerEngineState.Playing) {
                    DatabaseTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
                    if (track != null && track.PrimarySource == this) {
                        ServiceManager.PlayerEngine.Close ();
                    }
                }
            } catch { }
            
            // Remove tracks associated with this source, since we don't want
            // them after we unmap - we'll refetch.
            if (Count > 0) {
                RemoveTrackRange ((TrackListDatabaseModel)TrackModel, new Hyena.Collections.RangeCollection.Range (0, Count));
            }
            
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
            if (status_message != null) {
                DestroyStatusMessage ();
            }
            
            status_message = new SourceMessage (this);
            status_message.FreezeNotify ();
            status_message.CanClose = false;
            status_message.IsSpinning = spinner;
            status_message.Text = message;
            status_message.ThawNotify ();
            
            PushMessage (status_message);
        }
        
        private void DestroyStatusMessage ()
        {
            if (status_message != null) {
                RemoveMessage (status_message);
                status_message = null;
            }
        }
        
        private void HideMessage ()
        {
            if (status_message != null) {
                RemoveMessage (status_message);
                status_message = null;
            }
        }
        
        private void PromptLogin (object o, EventArgs args)
        {
            SetMessage (String.Format (Catalog.GetString ("Logging in to {0}"), Name), true);
            
            DaapLoginDialog dialog = new DaapLoginDialog (client.Name, 
            client.AuthenticationMethod == AuthenticationMethod.UserAndPassword);
            if (dialog.Run () == (int) Gtk.ResponseType.Ok) {
                AuthenticatedLogin (dialog.Username, dialog.Password);
            } else {
                Unmap ();
            }

            dialog.Destroy ();
        }
        
        private void AuthenticatedLogin (string username, string password)
        {
            ThreadAssist.Spawn (delegate {
                try {
                    client.Login (username, password);
                } catch (AuthenticationException) {
                    ThreadAssist.ProxyToMain (PromptLogin);
                }
            });
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
                    DaapTrackInfo daaptrack = new DaapTrackInfo (track, this);
                    daaptrack.Save ();
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
        
        public bool Unmap ()
        {
            // TODO: Maybe keep track of where we came from, or pick the next source up on the list?
            ServiceManager.SourceManager.SetActiveSource (ServiceManager.SourceManager.MusicLibrary);
            
            // Disconnect and clear out our tracks and such.
            Disconnect (true);
            
            Reload ();
            
            return true;
        }
        
        public bool CanUnmap {
            get { return true; }
        }
        
        public bool ConfirmBeforeUnmap {
            get { return false; }
        }
    }
}
