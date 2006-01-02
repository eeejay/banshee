/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  MetadataSearchPlugin.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Threading;
using Mono.Unix;

using MusicBrainz;
using Banshee.Base;

namespace Banshee.Plugins.MetadataSearch 
{
    public class MetadataSearchPlugin : Banshee.Plugins.Plugin
    {
        public override string DisplayName { get { return "Metadata Searcher"; } }
        
        public override string Description {
            get {
                return Catalog.GetString(
                    "Automatically search for missing and supplementary " + 
                    "metadata and cover art for songs in your library."
                );
            }
        }

        public override string [] Authors {
            get {
                return new string [] {
                    "Aaron Bockover"
                };
            }
        }
        
        private bool processing_queue;
        private Queue scan_queue;
        private Client mb_client;

        protected override void PluginInitialize()
        {
            scan_queue = new Queue();
            processing_queue = false;
            
            if(Globals.Library.IsLoaded) {
                ScanLibrary();
            }
            
            Globals.Library.Reloaded += OnLibraryReloaded;
            Globals.Library.TrackAdded += OnLibraryTrackAdded;
        }
        
        protected override void PluginDispose()
        {
            Globals.Library.Reloaded -= OnLibraryReloaded;
            Globals.Library.TrackAdded -= OnLibraryTrackAdded;
            while(processing_queue);
            scan_queue.Clear();
            scan_queue = null;
        }
        
        // ----------------------------------------------------
        
        private void OnLibraryReloaded(object o, EventArgs args)
        {
            ScanLibrary();
        }

        private void OnLibraryTrackAdded(object o, LibraryTrackAddedArgs args)
        {
            if(DisposeRequested) {
                return;
            }
            
            scan_queue.Enqueue(args.Track);
            if(!processing_queue) {
                ThreadAssist.Spawn(ProcessQueue);
            }
        }
                
        private void ScanLibrary()
        {
            ThreadAssist.Spawn(ScanLibraryThread);
        }
        
        private void ScanLibraryThread()
        {
            Console.WriteLine("Scanning library for tracks to update");
            
            IDataReader reader = Globals.Library.Db.Query(
                @"SELECT TrackID 
                    FROM Tracks 
                    WHERE RemoteLookupStatus IS NULL
                        OR RemoteLookupStatus = 0"
            );
            
            while(reader.Read() && !DisposeRequested) {
                scan_queue.Enqueue(Convert.ToInt32(reader["TrackID"]));
            }
            
            reader.Dispose();
            
            Console.WriteLine("Done scanning library");
            
            if(!DisposeRequested) {
                ProcessQueue();
            }
        }
        
        private void ProcessQueue()
        {
            if(processing_queue) {
                return;
            }
            
            Console.WriteLine("Processing track queue for pending queries");
            
            processing_queue = true;
            mb_client = new Client();
            
            while(scan_queue.Count > 0 && !DisposeRequested) {
                object o = scan_queue.Dequeue();
                if(o is int) {
                    ProcessTrack(Globals.Library.GetTrack((int)o));
                } else if(o is LibraryTrackInfo) {
                    ProcessTrack(o as LibraryTrackInfo);
                }
            }
            
            Console.WriteLine("Done processing track queue");
            
            mb_client.Dispose();
            processing_queue = false;
        }
        
        private void ProcessTrack(LibraryTrackInfo track)
        {   
            if(track == null) {
                return;
            }
            
            try {
                Console.Write("Querying MusicBrainz for {0} / {1}... ", track.Artist, track.Title);
            
                SimpleTrack mb_track = SimpleQuery.FileLookup(mb_client, 
                    track.Artist, track.Album, track.Title,
                    (int)track.TrackNumber, 
                    (int)track.Duration.TotalMilliseconds);
             
                if(mb_track == null) {
                    return;
                }
                
                if(mb_track.Artist != null) {
                    track.Artist = mb_track.Artist;
                }
                
                if(mb_track.Album != null) {
                    track.Album = mb_track.Album;
                }
                
                if(mb_track.Title != null) {
                    track.Title = mb_track.Title;
                }
                
                if(mb_track.TrackNumber > 0) {
                    track.TrackNumber = (uint)mb_track.TrackNumber;
                }
                
                if(mb_track.TrackCount > 0) {
                    track.TrackCount = (uint)mb_track.TrackCount;
                }
                
                if(mb_track.Asin != null) {
                    track.Asin = mb_track.Asin;
                    AmazonCoverFetcher.Fetch(mb_track.Asin, Paths.CoverArtDirectory);
                }
                
                track.RemoteLookupStatus = RemoteLookupStatus.Success;
                Console.WriteLine("OK");
            } catch(Exception e) {
                track.RemoteLookupStatus = RemoteLookupStatus.Failure;
                Console.WriteLine("FAILED ({0})", e.Message);
            }
            
            track.Save();
        }
    }
}
