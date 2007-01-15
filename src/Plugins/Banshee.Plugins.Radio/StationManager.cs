/***************************************************************************
 *  StationManager.cs
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
using System.IO;
using System.Net;
using System.Collections.Generic;

using Mono.Unix;
using Gtk;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Configuration;
using Banshee.Playlists.Formats.Xspf;
 
namespace Banshee.Plugins.Radio
{
    public class StationManager
    {
        public class StationsLoadFailedArgs : EventArgs
        {
            private string message;
            
            public StationsLoadFailedArgs(string message)
            {
                this.message = message;
            }
            
            public string Message {
                get { return message; }
            }
        }
    
        public delegate void StationsLoadFailedHandler(object o, StationsLoadFailedArgs args);
    
        private static readonly Uri master_xspf_uri = new Uri("http://radio.banshee-project.org/"); 
        private static readonly TimeSpan check_timeout = TimeSpan.FromDays(1);
        private static readonly string stations_path = Path.Combine(Paths.UserPluginDirectory, "stations");
        
        private List<Playlist> station_groups = new List<Playlist>();
        private int total_stations = 0;
        
        public event EventHandler StationsLoaded;
        public event EventHandler StationsRefreshing;
        public event StationsLoadFailedHandler StationsLoadFailed;
        
        public StationManager()
        {
        }
        
        public void ReloadStations(bool forceUpdate)
        {
            if(forceUpdate) {
                ThreadAssist.Spawn(ForceReloadStations);
            } else {
                ThreadAssist.Spawn(ReloadStationsIfNeeded);
            }
        }
        
        private void ForceReloadStations()
        {
            ThreadedLoad(true);
        }
        
        private void ReloadStationsIfNeeded()
        {
            ThreadedLoad(false);
        }
        
        private void ThreadedLoad(bool forceUpdates)
        {
            ThreadAssist.ProxyToMain(delegate {
                OnStationsRefreshing();
            });
            
            try {
                CheckForUpdates(forceUpdates);
                LoadStations();
            } catch(Exception e) {
                LogCore.Instance.PushWarning("Could not update stations cache", e.Message, false);
                ThreadAssist.ProxyToMain(delegate {
                    OnStationsLoadFailed(e);
                });
            }
        }
        
        private void LoadStations()
        {
            total_stations = 0;
            station_groups.Clear();
        
            try {
                Directory.CreateDirectory(stations_path);
                
                foreach(string xspf_file in Directory.GetFiles(stations_path, "*.xspf")) {
                    LoadStation(xspf_file);
                }
                
                foreach(string xspf_file in Directory.GetFiles(Path.Combine(stations_path, "user"), "*.xspf")) {
                    LoadStation(xspf_file);
                }
            } catch {
            }
            
            ThreadAssist.ProxyToMain(delegate {
                OnStationsLoaded();
            });
        }
        
        private void LoadStation(string file)
        {
            Playlist playlist = new Playlist();
            playlist.Load(file);
            
            MetaEntry helix_meta = playlist.FindMetaEntry("/BansheeXSPF:helix_required");
            if(!helix_meta.Equals(MetaEntry.Zero) && !AlwaysShowHelixStationsSchema.Get()) {
                bool have_helix = false;
                foreach(Banshee.MediaEngine.PlayerEngine engine in PlayerEngineCore.Engines) {
                    if(engine.Id == "helix-remote") {
                        have_helix = true;
                        break;
                    }
                }
                
                if(!have_helix) {
                    return;
                }
            }
            
            total_stations += playlist.Tracks.Count;
            station_groups.Add(playlist);
        }
        
        private void OnStationsLoaded()
        {
            EventHandler handler = StationsLoaded;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        private void OnStationsRefreshing()
        {
            EventHandler handler = StationsRefreshing;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }

        private void OnStationsLoadFailed(Exception e)
        {
            StationsLoadFailedHandler handler = StationsLoadFailed;
            if(handler != null) {
                handler(this, new StationsLoadFailedArgs(e.Message));
            }
        }
        
        private void CheckForUpdates(bool force)
        {
            if(!force && LastUpdateCheck - (DateTime.Now - check_timeout) > TimeSpan.Zero) {
                return;
            }
            
            Playlist playlist = new Playlist();
            playlist.Load(CreateXspfWebStream(master_xspf_uri));
            
            DateTime master_last_modified = DateTime.MinValue;
            
            MetaEntry meta = playlist.FindMetaEntry("/BansheeXSPF:last_modified");
            if(!meta.Equals(MetaEntry.Zero)) {
                master_last_modified = W3CDateTime.Parse(meta.Value).LocalTime;
            }
            
            if(!force && master_last_modified <= LastUpdated) {
                return;
            }
            
            try {
                Directory.CreateDirectory(stations_path);
                
                foreach(string xspf_file in Directory.GetFiles(stations_path, "*.xspf")) {
                    File.Delete(xspf_file);
                }
            } catch {
            }
            
            foreach(Track station in playlist.Tracks) {
                DownloadStation(station.Locations[0]);
            }
            
            LastUpdated = master_last_modified;
            LastUpdateCheck = DateTime.Now;
        }
        
        private void DownloadStation(Uri uri)
        {
            using(Stream from_stream = CreateXspfWebStream(uri)) {
                long bytes_read = 0;

                using(FileStream to_stream = new FileStream(Path.Combine(stations_path, 
                    Path.GetFileName(uri.LocalPath)), FileMode.Create, FileAccess.ReadWrite)) {
                    byte [] buffer = new byte[8192];
                    int chunk_bytes_read = 0;

                    while((chunk_bytes_read = from_stream.Read(buffer, 0, buffer.Length)) > 0) {
                        to_stream.Write(buffer, 0, chunk_bytes_read);
                        bytes_read += chunk_bytes_read;
                    }
                }
            }
        }
        
        private static Stream CreateXspfWebStream(Uri uri)
        {
            if(!Globals.Network.Connected) {
                throw new NetworkUnavailableException();
            }
        
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri.AbsoluteUri);
            request.Accept = "application/xspf+xml";
            request.UserAgent = Banshee.Web.Browser.UserAgent;
            request.Timeout = 60 * 1000;
            request.KeepAlive = false;
            request.AllowAutoRedirect = true;
            
            return ((HttpWebResponse)request.GetResponse()).GetResponseStream();
        }
        
        public List<Playlist> StationGroups {
            get { return station_groups; }
        }
        
        public int TotalStations {
            get { return total_stations; }
        }
        
        public DateTime LastUpdated {
            get {
                try {
                    return DateTime.Parse(LastUpdatedSchema.Get());
                } catch {
                    return DateTime.MinValue;
                }
            }
            
            set { LastUpdatedSchema.Set(value.ToString()); }
        }
        
        public DateTime LastUpdateCheck {
            get {
                try {
                    return DateTime.Parse(LastUpdateCheckSchema.Get());
                } catch {
                    return DateTime.MinValue;
                }
            }
            
            set { LastUpdateCheckSchema.Set(value.ToString()); }
        }

        public static readonly SchemaEntry<string> LastUpdatedSchema = new SchemaEntry<string>(
            "plugins.radio", "last_updated",
            "",
            "Time of the last radio update",
            "Last time XSPF stations were updated from radio.banshee-project.org"
        );

        public static readonly SchemaEntry<string> LastUpdateCheckSchema = new SchemaEntry<string>(
            "plugins.radio", "last_update_check",
            "",
            "Time of the last radio update check",
            "Last time the master station list was checked for updates"
        );

        public static readonly SchemaEntry<bool> AlwaysShowHelixStationsSchema = new SchemaEntry<bool>(
            "plugins.radio", "always_show_helix_stations",
            false,
            "Show stations requiring Helix/RealPlayer",
            "Always show stations that require the Helix/RealPlayer engine, even if the engine is not loaded."
        );
    }
}
