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
using System.Collections;
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
            
            proxy_server = new DaapProxyWebServer(8089);
            proxy_server.Start();
            
            string share_name = null;
            
            try {
                share_name = Globals.Configuration.Get(plugin.ConfigurationKeys["ShareName"]) as string;
            } catch {
            }
            
            if(share_name == null || share_name == String.Empty) {
                share_name = Catalog.GetString("Banshee Music Share");
            }
            
            database = new DAAP.Database(share_name);
            server = new Server(share_name);
            server.Collision += delegate {
                server.Name = server.Name + " [2]"; // FIXME
            };
            
            server.AddDatabase(database);
            
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
        }
        
        private static void OnServiceFound(object o, ServiceArgs args)
        {
            if (args.Service.Name == ServerName)
                return;
            
            try {
                Source source = new DaapSource(args.Service);
                source_map.Add(args.Service.Name, source);
                SourceManager.AddSource(source);
            } catch(InvalidSourceException) {
            }
        }
        
        private static void OnServiceRemoved(object o, ServiceArgs args)
        {
            Source source = source_map[args.Service.Name] as DaapSource;
            if(source == null) {
                return;
            }
            
            source.Dispose();
            SourceManager.RemoveSource(source);
            source_map.Remove(args.Service.Name);
        }
        
        internal static void StartServer()
        {
            Console.WriteLine("Starting DAAP Server");
            server.Start();
            Globals.Configuration.Set(plugin.ConfigurationKeys["ServerEnabled"], true);
            
            if(!initial_db_committed) {
                server.Commit();
                initial_db_committed = true;
            }
        }
        
        internal static void StopServer()
        {
            Console.WriteLine("Stopping DAAP Server");
            server.Stop();
            Globals.Configuration.Set(plugin.ConfigurationKeys["ServerEnabled"], false);
        }
        
        private static DAAP.Song TrackInfoToSong(TrackInfo track)
        {
            if(track == null || track.Uri == null || track.Uri.Scheme != Uri.UriSchemeFile) {
                return null;
            }
            
            DAAP.Song song = new DAAP.Song();
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
            return song;
        }
        
        private static void LoadInitialServerDatabase()
        {
            Console.WriteLine("Building initial DAAP database from local library...");
            
            lock(Globals.Library.Tracks.Values) {
                foreach(TrackInfo track in Globals.Library.Tracks.Values) {
                    DAAP.Song song = TrackInfoToSong(track);
                    if(song != null) {
                        database.AddSong(song);
                    }
                }
            }
            
            try {
                if((bool)Globals.Configuration.Get(plugin.ConfigurationKeys["ServerEnabled"])) {
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
                    server.Name = value;
                    database.Name = value;
                    Globals.Configuration.Set(plugin.ConfigurationKeys["ShareName"], value);
                }
            }
        }
        
        internal static bool IsServerRunning {
            get {
                return server.IsRunning;
            }
        }
    }
}
