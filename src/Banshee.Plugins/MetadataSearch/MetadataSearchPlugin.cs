/***************************************************************************
 *  MetadataSearchPlugin.cs
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
using System.Data;
using System.Collections;
using Mono.Unix;

using MusicBrainz;
using Banshee.Base;
using Banshee.Kernel;
using Banshee.Database;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Plugins.MetadataSearch.MetadataSearchPlugin)
        };
    }
}

namespace Banshee.Plugins.MetadataSearch 
{
    internal enum FetchMethod 
    {
        CoversOnly,
        FillBlank,
        Overwrite
    }

    public class MetadataSearchPlugin : Banshee.Plugins.Plugin
    {
        private const string NotFoundAsin = "NOTFOUND";
        
        protected override string ConfigurationName { get { return "MetadataSearch"; } }
        public override string DisplayName { get { return Catalog.GetString("Metadata Searcher"); } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Automatically search for missing and supplementary " + 
                    "metadata and cover art for songs in your library."
                    );
            }
        }

        public override string [] Authors {
            get { return new string [] { "Aaron Bockover" }; }
        }

        public event EventHandler ScanStarted;
        public event EventHandler ScanEnded;
        
        private int generation;
        private int scan_ref_count;
        
        private object mb_client_mutex = new object();
        private Client mb_client;
        
        protected override void PluginInitialize()
        {
            System.Threading.Interlocked.Increment(ref generation);
            System.Threading.Interlocked.Exchange(ref scan_ref_count, 0);
            
            mb_client = new Client();
            
            RegisterConfigurationKey("FetchMethod");
            
            if(Globals.Library.IsLoaded) {
                ScanLibrary();
            } else {
                Globals.Library.Reloaded += OnLibraryReloaded;
            }
            
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
        }
        
        protected override void PluginDispose()
        {
            System.Threading.Interlocked.Exchange(ref scan_ref_count, 0);
            
            lock(mb_client_mutex) {
                if(mb_client != null) {
                    mb_client.Dispose();
                    mb_client = null;
                }
            }
            
            Globals.Library.Reloaded -= OnLibraryReloaded;
            Globals.Library.TrackAdded -= OnLibraryTrackAdded;
        }
        
        public override Gtk.Widget GetConfigurationWidget()
        {            
            return new MetadataSearchConfigPage(this);
        }
        
        // ----------------------------------------------------

        protected virtual void OnScanStarted()
        {
            System.Threading.Interlocked.Increment(ref scan_ref_count);
        
            EventHandler handler = ScanStarted;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        protected virtual void OnScanEnded()
        {
            System.Threading.Interlocked.Decrement(ref scan_ref_count);
        
            EventHandler handler = ScanEnded;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }

        private void OnLibraryReloaded(object o, EventArgs args)
        {
            ScanLibrary();
        }

        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            Scheduler.Schedule(new ProcessTrackJob(this, args.Track));
        }

        internal void RescanLibrary()
        {
            Scheduler.Unschedule(typeof(ProcessTrackJob));
            Globals.Library.Db.Query("UPDATE Tracks SET RemoteLookupStatus = 0");
            ScanLibrary();
        }
                
        private void ScanLibrary()
        {
            Scheduler.Schedule(new ScanLibraryJob(this));
        }
        
        internal FetchMethod FetchMethod {
            get {
                try {
                    return (FetchMethod)Globals.Configuration.Get(ConfigurationKeys["FetchMethod"]);
                } catch {
                    return FetchMethod.CoversOnly;
                }
            }
            
            set {
                Globals.Configuration.Set(ConfigurationKeys["FetchMethod"], (int)value);
            }
        }

        internal bool IsScanning {
            get { return scan_ref_count > 0; }
        }

        private class ScanLibraryJob : IJob
        {
            private MetadataSearchPlugin plugin;
            private int generation;
            
            public ScanLibraryJob(MetadataSearchPlugin plugin)
            {
                this.plugin = plugin;
                this.generation = plugin.generation;
            }
        
            public void Run()
            {
                if(generation != plugin.generation) {
                    return;
                }
                
                plugin.OnScanStarted();
                //Console.WriteLine("Scanning library for tracks to update");
                
                IDataReader reader = Globals.Library.Db.Query(
                    @"SELECT TrackID 
                        FROM Tracks 
                        WHERE RemoteLookupStatus IS NULL
                            OR RemoteLookupStatus = 0"
                );
                
                while(reader.Read()) {
                    Scheduler.Schedule(new ProcessTrackJob(plugin, Convert.ToInt32(reader["TrackID"])));
                }
                
                reader.Dispose();
                
                //Console.WriteLine("Done scanning library");
                plugin.OnScanEnded();
            }
        }
        
        private class ProcessTrackJob : IJob
        {
            private LibraryTrackInfo track;
            private int track_id;
            private MetadataSearchPlugin plugin;
            private int generation;
            
            public ProcessTrackJob(MetadataSearchPlugin plugin, LibraryTrackInfo track)
            {
                this.plugin = plugin;
                this.track = track;
                this.generation = plugin.generation;
            }
            
            public ProcessTrackJob(MetadataSearchPlugin plugin, int trackId)
            {
                this.plugin = plugin;
                this.track_id = trackId;
                this.generation = plugin.generation;
            }
        
            public void Run()
            {
                if(plugin.generation != generation) {
                    return;
                }
                
                lock(plugin.mb_client_mutex) {
                    ProcessTrack(track != null ? track : Globals.Library.GetTrack(track_id));
                }
            }

            private void ProcessTrack(LibraryTrackInfo track)
            {   
                if(track == null) {
                    return;
                }
                
                try {
                    FetchMethod fetch = plugin.FetchMethod;
                    
                    if(fetch == FetchMethod.CoversOnly) {
                        string asin = (string)Globals.Library.Db.QuerySingle(new DbCommand(
                            @"SELECT ASIN 
                                FROM Tracks
                                WHERE Artist = :artist
                                    AND AlbumTitle = :album",
                                    "artist", track.Artist,
                                    "album", track.Album)
                        );

                        if(asin == NotFoundAsin) {
                            track.Asin = NotFoundAsin;
                            track.RemoteLookupStatus = RemoteLookupStatus.Success;
                            track.Save();
                            return;
                        } else if(asin != null && asin != String.Empty) {
                            //Console.WriteLine("Setting ASIN from previous lookup ({0} / {1})", track.Artist, track.Title);
                            track.Asin = asin;
                            track.RemoteLookupStatus = RemoteLookupStatus.Success;
                            AmazonCoverFetcher.Fetch(asin, Paths.CoverArtDirectory);
                            track.Save();
                            return;
                        }
                    }
                    
                    //Console.Write("Querying MusicBrainz for {0} / {1}... ", track.Artist, track.Title);
                    
                    if(plugin.mb_client == null) {
                        return;
                    }
                    
                    SimpleTrack mb_track = SimpleQuery.FileLookup(plugin.mb_client, 
                        track.Artist, track.Album, track.Title,
                        (int)track.TrackNumber, 
                        (int)track.Duration.TotalMilliseconds);
                 
                    if(mb_track == null) {
                        return;
                    }
                    
                    if(fetch != FetchMethod.CoversOnly) {
                        if(mb_track.Artist != null && (fetch == FetchMethod.Overwrite 
                            || (track.Artist == null || track.Artist == String.Empty))) {
                            track.Artist = mb_track.Artist;
                        }
                        
                        if(mb_track.Album != null && (fetch == FetchMethod.Overwrite 
                            || (track.Album == null || track.Album == String.Empty))) {
                            track.Album = mb_track.Album;
                        }
                        
                        if(mb_track.Title != null && (fetch == FetchMethod.Overwrite 
                            || (track.Title == null || track.Title == String.Empty))) {
                            track.Title = mb_track.Title;
                        }
                        
                        if(mb_track.TrackNumber > 0 && (fetch == FetchMethod.Overwrite || track.TrackNumber == 0)) {
                            track.TrackNumber = (uint)mb_track.TrackNumber;
                        }
                        
                        if(mb_track.TrackCount > 0 && (fetch == FetchMethod.Overwrite || track.TrackCount == 0)) {
                            track.TrackCount = (uint)mb_track.TrackCount;
                        }
                    }
                    
                    if(mb_track.Asin != null && mb_track.Asin != String.Empty) {
                        track.Asin = mb_track.Asin;
                        AmazonCoverFetcher.Fetch(mb_track.Asin, Paths.CoverArtDirectory);
                    } else {
                        track.Asin = NotFoundAsin;
                    }
                    
                    track.RemoteLookupStatus = RemoteLookupStatus.Success;
                    //Console.WriteLine("OK");
                } catch(Exception e) {
                    track.RemoteLookupStatus = RemoteLookupStatus.Failure;
                    //Console.WriteLine("FAILED ({0})", e.Message);
                }
                
                track.Save();
            }
        }
    }
}
