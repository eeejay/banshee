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
using Banshee.Metadata;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Library;

namespace Banshee.Bpm
{
    public class BpmDetectJob : DbIteratorJob
    {
        private int current_track_id;
        private IBpmDetector detector;
        private PrimarySource music_library;
        
        private static HyenaSqliteCommand update_query = new HyenaSqliteCommand (
            "UPDATE CoreTracks SET BPM = ?, DateUpdatedStamp = ? WHERE TrackID = ?");
        
        public BpmDetectJob () : base (Catalog.GetString ("Detecting BPM"))
        {
            IconNames = new string [] {"audio-x-generic"};
            IsBackground = true;

            music_library = ServiceManager.SourceManager.MusicLibrary;

            CountCommand = new HyenaSqliteCommand (String.Format (
                "SELECT COUNT(*) FROM CoreTracks WHERE PrimarySourceID = {0} AND (BPM = 0 OR BPM IS NULL)",
                music_library.DbId
            ));

            SelectCommand = new HyenaSqliteCommand (String.Format (@"
                SELECT DISTINCT Uri, TrackID
                FROM CoreTracks
                WHERE PrimarySourceID IN ({0}) AND (BPM = 0 OR BPM IS NULL) LIMIT 1",
                music_library.DbId
            ));

            Register ();
        }
        
        protected override void Init ()
        {
            detector = GetDetector ();
            detector.FileFinished += OnFileFinished;
        }

        protected override void Cleanup ()
        {
            Finish ();

            if (detector != null) {
                detector.Dispose ();
            }
        }

        protected override void IterateCore (HyenaDataReader reader)
        {
            SafeUri uri = new SafeUri (reader.Get<string> (0));
            current_track_id = reader.Get<int> (1);
            detector.ProcessFile (uri);
        }

        private void OnFileFinished (SafeUri uri, int bpm)
        {
            if (bpm > 0) {
                Log.DebugFormat ("Saving BPM of {0} for {1}", bpm, uri);
                ServiceManager.DbConnection.Execute (update_query, bpm, DateTime.Now, current_track_id);
            } else {
                ServiceManager.DbConnection.Execute (update_query, -1, DateTime.Now, current_track_id);
                Log.DebugFormat ("Unable to detect BPM for {0}", uri);
            }

            Iterate ();
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
