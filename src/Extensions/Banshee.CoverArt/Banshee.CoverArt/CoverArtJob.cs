//
// CoverArtJob.cs
//
// Authors:
//   James Willcox <snorp@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Data;
using System.IO;
using System.Threading;
using Mono.Unix;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.Kernel;
using Banshee.Metadata;
using Banshee.ServiceStack;
using Banshee.Library;
using Hyena;
using Gtk;

namespace Banshee.CoverArt
{
    public class CoverArtJob : UserJob, IJob
    {
        private const int BatchSize = 10;
        
        private DateTime last_scan = DateTime.MinValue;
        
        public CoverArtJob (DateTime lastScan) : base (Catalog.GetString ("Downloading Cover Art"))
        {
            last_scan = lastScan;
            CanCancel = true;
        }
        
        public void Start ()
        {
            Register ();
            Scheduler.Schedule (this);
        }
        
        private IDataReader RunQuery (int iteration, bool count)
        {
            string query = String.Format (@"
                SELECT {0}, CoreAlbums.Title, CoreArtists.Name
                FROM CoreAlbums, CoreArtists, CoreTracks
                WHERE
                    CoreAlbums.ArtistID = CoreArtists.ArtistID AND
                    CoreTracks.AlbumID = CoreAlbums.AlbumID AND
                    CoreTracks.DateUpdatedStamp > ? AND
                    CoreTracks.PrimarySourceID = ?
                    ORDER BY CoreAlbums.Title ASC
                    LIMIT {1} OFFSET {2}
                ", count ? "count(DISTINCT CoreAlbums.AlbumID)" : "DISTINCT CoreAlbums.AlbumID",
                BatchSize, iteration * BatchSize);
            
            return ServiceManager.DbConnection.Query (query, last_scan,
                                                      ServiceManager.SourceManager.MusicLibrary.DbId);
        }
        
        private void FetchForTrack (TrackInfo track)
        {
            try {
                IMetadataLookupJob job = MetadataService.Instance.CreateJob (track);
                job.Run ();
            } catch (Exception e) {
                Log.Exception (e);
            }
        }
        
        public void Run ()
        {
            this.Status = Catalog.GetString ("Preparing...");
            this.IconNames = new string [] {Stock.Network};
            
            int current_track_count = 0;
            int total_track_count = 0;
            int offset = 0;
            using (IDataReader reader = RunQuery (offset, true)) {
                if (reader.Read ()) {
                    total_track_count = reader.GetInt32 (0);
                }
            }
            
            if (total_track_count == 0) {
                Finish ();
                return;
            }

            TrackInfo track = new TrackInfo ();

            while (true) {
                using (IDataReader reader = RunQuery (offset++, false)) {
                    int batch_count = 0;
                    while (reader.Read ()) {
                        if (IsCancelRequested) {
                            Finish ();
                            return;
                        }
                        
                        batch_count++;
                        if (!CoverArtSpec.CoverExists (reader.GetString (2),
                                                       reader.GetString (1))) {
                            track.AlbumTitle = reader.GetString (1);
                            track.ArtistName = reader.GetString (2);
                            
                            Log.DebugFormat ("Downloading cover art for {0} - {1}", track.ArtistName, track.AlbumTitle);
                            this.Progress = (double) current_track_count / (double) total_track_count;
                            this.Status = String.Format (Catalog.GetString ("{0} - {1}"), track.ArtistName, track.AlbumTitle);
                            FetchForTrack (track);
                        }
                        
                        current_track_count++;
                    }
                    
                    if (batch_count != BatchSize)
                        break;
                }
            }
            
            Finish ();
        }
    }
}
