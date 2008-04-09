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
using System.Collections.Generic;
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Daap
{
    public class DaapSource : PrimarySource, IDurationAggregator, IUnmapableSource, IImportSource
    {
        private Service service;
        private DAAP.Client client;
        private DAAP.Database database;
        
        public DAAP.Database Database {
            get { return database; }
        }
        
        private Dictionary <int, DaapTrackInfo> daap_track_map;
        public Dictionary <int, DaapTrackInfo> TrackMap {
            get { return daap_track_map; }
        }
        
        private bool is_activating;
        
        public DaapSource (Service service) : base (Catalog.GetString ("Music Share"), service.Name, 
                                                    (service.Address.ToString () + service.Port).Replace (":", "").Replace (".", ""), 300)
        {
            this.service = service;
            daap_track_map = new Dictionary <int, DaapTrackInfo> ();
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Disconnect"));
            Properties.SetString ("UnmapSourceActionIconName", "gtk-disconnect");
            
            UpdateIcon ();
            
            AfterInitialized ();
        }
        
        private void UpdateIcon ()
        {
            if (service != null && !service.IsProtected) {
                Properties.SetStringList ("Icon.Name", "computer", "network-server");
            } else {
                Properties.SetStringList ("Icon.Name", "system-lock-screen", "computer", "network-server");
            }
        }
        
        public override void Activate ()
        {
            if (client != null || is_activating) {
            	return;
            }
            
            is_activating = true;
            base.Activate ();
            
            SetStatus (String.Format (Catalog.GetString ("Connecting to {0}"), service.Name), false);
            
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
                    
                    string details = String.Format ("Couldn't connect to service {0} on {1}:{2} - {3}",
                                                      service.Name,
                                                      service.Address,
                                                      service.Port, e.ToString ().Replace ("&", "&amp;")
                                                    .Replace ("<", "&lt;").Replace (">", "&gt;"));
                    Hyena.Log.Warning ("Failed to connect", details, true);
                    HideStatus ();
                }
               
                is_activating = false;
            });
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
            
            daap_track_map.Clear ();
            
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
                database.TrackRemoved -= OnDatabaseTrackRemoved;
                database = null;
            }

            ClearChildSources();
            
            return true;
        }
        
        public override void Dispose ()
        {
            Disconnect (true);
            base.Dispose ();
        }
        
        private void PromptLogin (object o, EventArgs args)
        {
            SetStatus (String.Format (Catalog.GetString ("Logging in to {0}"), Name), false);
            
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
                database.TrackRemoved += OnDatabaseTrackRemoved;
                
                foreach (Track track in database.Tracks) {
                    DaapTrackInfo daaptrack = new DaapTrackInfo (track, this);
                    daaptrack.Save ();
                    
                    daap_track_map.Add (track.Id, daaptrack);
                }
                
                AddPlaylistSources ();
                
                Reload ();
                
                ThreadAssist.ProxyToMain(delegate {
                    HideStatus ();
                });
            }
            
            Name = client.Name;
            
            UpdateIcon ();
            OnUpdated ();
        }
        
        private void AddPlaylistSources ()
        {
            foreach (DAAP.Playlist pl in database.Playlists) {
                Console.WriteLine ("Has playlist: {0}", pl.Name);
                DaapPlaylistSource source = new DaapPlaylistSource (pl, this);
                AddChildSource (source);
            }
        }
        
        public void OnDatabaseTrackAdded (object o, TrackArgs args)
        {
            DaapTrackInfo track = new DaapTrackInfo (args.Track, this);
            track.Save ();
            
            if (!daap_track_map.ContainsKey (args.Track.Id)) {
                daap_track_map.Add (args.Track.Id, track);
            }
        }
        
        public void OnDatabaseTrackRemoved (object o, TrackArgs args)
        {
            if (daap_track_map.ContainsKey (args.Track.Id)) {
                DaapTrackInfo track = daap_track_map [args.Track.Id];
                RemoveTrack (track);
            }
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
        
        public void Import ()
        {
            Console.WriteLine ("Import called.");
            foreach (TrackInfo track in TrackModel.SelectedItems) {
                Console.WriteLine ("Selected: {0}", track);
            }
            
            Console.WriteLine ("Selection count: {0}", TrackModel.Selection.Count);
        }
        
        public bool CanImport {
            get { return true; }
        }
        
        public string [] IconNames {
            get { return Properties.GetStringList ("Icon.Name"); }
        }
    }
}
