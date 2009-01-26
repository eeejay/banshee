//
// BpmDetectJob.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.IO;
using System.Threading;

using Mono.Unix;
using Mono.Addins;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.Kernel;
using Banshee.Metadata;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Library;

namespace Banshee.Bpm
{
    public class BpmDetectJob : UserJob, IJob
    {
        private PrimarySource music_library;
        private int current_count;
        private int current_track_id;
        private IBpmDetector detector;
        
        private static HyenaSqliteCommand count_query = new HyenaSqliteCommand (
            "SELECT COUNT(*) FROM CoreTracks WHERE PrimarySourceID = ? AND (BPM = 0 OR BPM IS NULL)");

        private static HyenaSqliteCommand select_query = new HyenaSqliteCommand (@"
            SELECT DISTINCT Uri, UriType, TrackID
            FROM CoreTracks
            WHERE PrimarySourceID = ? AND (BPM = 0 OR BPM IS NULL) LIMIT 1");

        private static HyenaSqliteCommand update_query = new HyenaSqliteCommand (
            "UPDATE CoreTracks SET BPM = ?, DateUpdatedStamp = ? WHERE TrackID = ?");
        
        public BpmDetectJob () : base (Catalog.GetString ("Detecting BPM"))
        {
            music_library = ServiceManager.SourceManager.MusicLibrary;
            IconNames = new string [] {"audio-x-generic"};
            IsBackground = true;
        }
        
        public void Start ()
        {
            Register ();
            Scheduler.Schedule (this, JobPriority.Lowest);
        }

        private HyenaDataReader RunQuery ()
        {
            return new HyenaDataReader (ServiceManager.DbConnection.Query (select_query,
                music_library.DbId
            ));
        }
        
        public void Run ()
        {
            int total = ServiceManager.DbConnection.Query<int> (count_query, music_library.DbId);
            if (total > 0) {
                detector = GetDetector ();
                detector.FileFinished += OnFileFinished;

                DetectNext ();
            }
        }

        private void DetectNext ()
        {
            if (IsCancelRequested) {
                Hyena.Log.Debug ("BPM detection cancelled");
                Finish ();
                detector.Dispose ();
                return;
            }

            int total = current_count + ServiceManager.DbConnection.Query<int> (count_query, music_library.DbId);
            try {
                using (HyenaDataReader reader = RunQuery ()) {
                    if (reader.Read ()) {
                        SafeUri uri = music_library.UriAndTypeToSafeUri (
                            reader.Get<TrackUriType> (1), reader.Get<string> (0)
                        );
                        current_track_id = reader.Get<int> (2);
                        detector.ProcessFile (uri);
                    } else {
                        Finish ();
                        detector.Dispose ();
                    }
                }
            } catch (Exception e) {
                Log.Exception (e);
            } finally {
                Progress = (double) current_count / (double) total;
                current_count++;
            }
        }

        private void OnFileFinished (SafeUri uri, int bpm)
        {
            if (bpm > 0) {
                Log.DebugFormat ("Saving BPM of {0} for {1}", bpm, uri);
                ServiceManager.DbConnection.Execute (update_query, bpm, DateTime.Now, current_track_id);
            } else {
                Log.DebugFormat ("Unable to detect BPM for {0}", uri);
            }
            DetectNext ();
        }

        internal static IBpmDetector GetDetector ()
        {
            IBpmDetector detector = null;
            foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes ("/Banshee/MediaEngine/BpmDetector")) {
                try {
                    detector = (IBpmDetector)node.CreateInstance (typeof (IBpmDetector));
                } catch (Exception e) {
                    Log.Exception (e);
                }

                if (detector != null) {
                    break;
                }
            }
            return detector;
        }
    }
}
