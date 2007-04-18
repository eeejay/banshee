/***************************************************************************
 *  DaapCore.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@aaronbock.net>
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
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;
using DAAP;

using Banshee.Base;
using Banshee.Sources;

namespace Banshee.Plugins.Daap
{
    internal static class DaapCore
    {
        private static DaapPlugin plugin;
        private static Server server;
        private static DAAP.Database database;
        private static ServiceLocator locator;
        private static DaapProxyWebServer proxy_server;
        private static Hashtable source_map;
        private static int collision_count;
        private static DaapContainerSource container_source;
        private static Dictionary<TrackInfo, DAAP.Track> track_map;
        
        private static bool initial_db_committed = false;
        
        internal static void Initialize(DaapPlugin plugin)
        {
            DaapCore.plugin = plugin;
            
            source_map = new Hashtable();
            locator = new ServiceLocator();
            locator.Found += OnServiceFound;
            locator.Removed += OnServiceRemoved;
            locator.ShowLocalServices = true;
            locator.Start();
            
            proxy_server = new DaapProxyWebServer();
            proxy_server.Start();
            
            string share_name = DaapPlugin.ShareNameSchema.Get();
            
            if(share_name == null || share_name == String.Empty) {
                share_name = Catalog.GetString("Banshee Music Share");
            }
            
            collision_count = 0;
            
            database = new DAAP.Database(share_name);
            server = new Server(share_name);
            server.Collision += delegate {
                server.Name = share_name + " [" + ++collision_count + "]";
            };
            
            server.AddDatabase(database);
            
            track_map = new Dictionary<TrackInfo, DAAP.Track>();
            
            if(Globals.Library.IsLoaded) {
                LoadInitialServerDatabase();
            } else {
                Globals.Library.Reloaded += OnLibraryReloaded;
            }
        }
        
        internal static void Dispose()
        {
            if(locator != null) {
                locator.Stop();
                locator.Found -= OnServiceFound;
                locator.Removed -= OnServiceRemoved;
                locator = null;
            }
            
            if(proxy_server != null) {
                proxy_server.Stop();
            }
            
            if(server != null) {
                server.Stop();
            }
            
            if(source_map != null) {
                foreach(DaapSource source in source_map.Values) {
                    SourceManager.RemoveSource(source);
                    source.Dispose();
                }
                
                source_map.Clear();
                source_map = null;
            }
            
            track_map.Clear();
            track_map = null;
        }
        
        private static void OnServiceFound(object o, ServiceArgs args)
        {
            if (args.Service.Name == ServerName)
                return;
            
            try {
                DaapSource source = new DaapSource(args.Service);
                int collision = 0;
                string service_name = args.Service.Name;
                
                while (source_map.Contains(service_name))
                    service_name = args.Service.Name + " [" + ++collision + "]";
                source_map.Add(service_name, source);
                if(source_map.Count == 1) {
                    container_source = new DaapContainerSource();
                    SourceManager.AddSource(container_source);
                }
                container_source.AddChildSource(source);
                
            } catch(InvalidSourceException) {
            }
        }
        
        private static void OnServiceRemoved(object o, ServiceArgs args)
        {
            DaapSource source = source_map[args.Service.Name] as DaapSource;
            if(source == null) {
                return;
            }
            
            source.Disconnect (false);
            source.Dispose();
            container_source.RemoveChildSource(source);
            source_map.Remove(args.Service.Name);
            if(source_map.Count == 0) {
                container_source.Dispose();
                SourceManager.RemoveSource(container_source);
                container_source = null;
            }
        }
        
        internal static void StartServer()
        {
            Console.WriteLine("Starting DAAP Server");
            try {
                server.Start();
            } catch (System.Net.Sockets.SocketException) {
                server.Port = 0;
                server.Start();
            }
            
            DaapPlugin.ServerEnabledSchema.Set(true);
            
            if(!initial_db_committed) {
                server.Commit();
                initial_db_committed = true;
            }
        }
        
        internal static void StopServer()
        {
            Console.WriteLine("Stopping DAAP Server");
            server.Stop();
            DaapPlugin.ServerEnabledSchema.Set(false);
        }
        
        private static DAAP.Track TrackInfoToTrack(TrackInfo track)
        {
            if(track == null || track.Uri == null || track.Uri.Scheme != Uri.UriSchemeFile) {
                return null;
            }
            
            if(track_map.ContainsKey(track)) {
                return track_map[track];
            }
            
            DAAP.Track song = new DAAP.Track();
            song.Album = track.Album;
            song.Artist = track.Artist;
            song.DateAdded = track.DateAdded;
            song.Duration = track.Duration;
            song.FileName = track.Uri.LocalPath;
            song.Genre = track.Genre;
            song.Title = track.Title;
            song.TrackCount = (int)track.TrackCount;
            song.TrackNumber = (int)track.TrackNumber;
            song.Year = track.Year;

            string ext = Path.GetExtension (song.FileName);
            if (ext != null && ext.Length > 0) {
                song.Format = Path.GetExtension (song.FileName).Substring (1);
            }
            
            track_map[track] = song;
            
            return song;
        }
        
        private static DAAP.Playlist ChildSourceToPlaylist(ChildSource playlist)
        {
            if(playlist == null) {
                return null;
            }
            
            DAAP.Playlist daap_playlist = new DAAP.Playlist();
            daap_playlist.Name = playlist.Name;
            foreach(TrackInfo track in playlist.Tracks) {
                DAAP.Track song = TrackInfoToTrack(track);
                if(null != song) {
                    daap_playlist.AddTrack(song);
                }
            }
            return daap_playlist;
        }
        
        private static void LoadInitialServerDatabase()
        {
            Console.WriteLine("Building initial DAAP database from local library...");
            
            lock(Globals.Library.Tracks.Values) {
                foreach(TrackInfo track in Globals.Library.Tracks.Values) {
                    DAAP.Track song = TrackInfoToTrack(track);
                    if(song != null) {
                        database.AddTrack(song);
                    }
                }
            }
            
            lock(SourceManager.DefaultSource.Children) {
                foreach(ChildSource child in SourceManager.DefaultSource.Children) {
                    DAAP.Playlist daap_playlist = ChildSourceToPlaylist(child);
                    if(null != daap_playlist) {
                        database.AddPlaylist(daap_playlist);
                    }
                }
            }
            
            try {
                if(DaapPlugin.ServerEnabledSchema.Get()) {
                    StartServer();
                }
            } catch {
            }
        }
        
        private static void OnLibraryReloaded(object o, EventArgs args)
        {
            Globals.Library.Reloaded -= OnLibraryReloaded;
            LoadInitialServerDatabase();
        }

        public static DaapProxyWebServer ProxyServer {
            get {
                return proxy_server;
            }
        }
        
        internal static string ServerName {
            get {
                return server != null ? server.Name : null;
            }
            
            set {
                if(value != null && value != String.Empty) {
                    collision_count = 0;
                    server.Name = value;
                    database.Name = value;
                    DaapPlugin.ShareNameSchema.Set(value);
                }
            }
        }
        
        internal static bool IsServerRunning {
            get { return server.IsRunning; }
        }
        
        internal static int ServerCount {
            get { return source_map.Count; }
        }
    }
}
