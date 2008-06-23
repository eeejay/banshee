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

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Library;
using Banshee.Sources;
using Banshee.ServiceStack;

using DAAP = Daap;

namespace Banshee.Daap
{
    public class DaapSource : PrimarySource, IDurationAggregator, IUnmapableSource, IImportSource
    {
        private DAAP.Service service;
        private DAAP.Client client;
        private DAAP.Database database;
        
        public DAAP.Database Database {
            get { return database; }
        }
        
        private bool is_activating;
        private int playlistid;
        
        public DaapSource (DAAP.Service service) : base (Catalog.GetString ("Music Share"), service.Name, 
                                                    (service.Address.ToString () + service.Port).Replace (":", "").Replace (".", ""), 300)
        {
            this.service = service;
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Disconnect"));
            Properties.SetString ("UnmapSourceActionIconName", "gtk-disconnect");
            
            UpdateIcon ();
            
            AfterInitialized ();
            playlistid = this.DbId;
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
                    
                    if (client.AuthenticationMethod == DAAP.AuthenticationMethod.None) {
                        client.Login ();
                    } else {
                        ThreadAssist.ProxyToMain (PromptLogin);
                    }
                } catch(Exception e) {
                    SetStatus (String.Format (Catalog.GetString ("Failed to connect to {0}"), service.Name), true);
                    Hyena.Log.Exception (e);
                }
               
                is_activating = false;
            });
        }
        
        internal bool Disconnect (bool logout)
        {
            // Stop currently playing track if its from us.
            try {
                if (ServiceManager.PlayerEngine.CurrentState == Banshee.MediaEngine.PlayerState.Playing) {
                    DatabaseTrackInfo track = ServiceManager.PlayerEngine.CurrentTrack as DatabaseTrackInfo;
                    if (track != null && track.PrimarySource == this) {
                        ServiceManager.PlayerEngine.Close ();
                    }
                }
            } catch { }
            
            // Remove tracks associated with this source, since we don't want
            // them after we unmap - we'll refetch.
            PurgeTracks ();
            
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
            
            List<Source> children = new List<Source> (Children);
            foreach (Source child in children) {
                if (child is Banshee.Sources.IUnmapableSource) {
                    (child as Banshee.Sources.IUnmapableSource).Unmap ();
                }
            }
            
            ClearChildSources();
            
            return true;
        }
        
        public override void Dispose ()
        {
            Disconnect (true);
            base.Dispose ();
        }
        
        private void PromptLogin ()
        {
            SetStatus (String.Format (Catalog.GetString ("Logging in to {0}"), Name), false);
            
            DaapLoginDialog dialog = new DaapLoginDialog (client.Name, 
            client.AuthenticationMethod == DAAP.AuthenticationMethod.UserAndPassword);
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
                } catch (DAAP.AuthenticationException) {
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
                
                SetStatus (String.Format (Catalog.GetString ("Loading {0} tracks"), database.Tracks.Count), false, true, "gtk-refresh");
                
                int count = 0;
                DaapTrackInfo daap_track = null;
                foreach (DAAP.Track track in database.Tracks) {
                    daap_track = new DaapTrackInfo (track, this);
                    daap_track.Save (++count % 250 == 0);
                }
                
                // Save the last track once more to trigger the NotifyTrackAdded
                if (daap_track != null) {
                    daap_track.Save ();
                }
                
                SetStatus (Catalog.GetString ("Loading playlists"), false);
                AddPlaylistSources ();
                Reload ();
                HideStatus ();
            }
            
            Name = client.Name;
            
            UpdateIcon ();
            OnUpdated ();
        }
        
        private void AddPlaylistSources ()
        {
            foreach (DAAP.Playlist pl in database.Playlists) {
                DaapPlaylistSource source = new DaapPlaylistSource (pl, playlistid, this);
                AddChildSource (source);
                playlistid ++;
            }
        }
        
        public void OnDatabaseTrackAdded (object o, DAAP.TrackArgs args)
        {
            DaapTrackInfo track = new DaapTrackInfo (args.Track, this);
            track.Save ();
        }
        
        public void OnDatabaseTrackRemoved (object o, DAAP.TrackArgs args)
        {
            //RemoveTrack (
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
            // Disconnect and clear out our tracks and such.
            Disconnect (true);
            
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
            Log.Debug ("Starting import...");
            DateTime start = DateTime.Now;
            ServiceManager.SourceManager.MusicLibrary.AddAllTracks (this);
            DateTime finish = DateTime.Now;
            TimeSpan time = finish - start;
            Log.DebugFormat ("Import completed. Took {0} seconds.", time.TotalSeconds);
        }
        
        public bool CanImport {
            get { return true; }
        }
        
        public string [] IconNames {
            get { return Properties.GetStringList ("Icon.Name"); }
        }
    }
}
