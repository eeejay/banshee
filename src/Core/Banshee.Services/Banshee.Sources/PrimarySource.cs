//
// PrimarySource.cs
//
// Author:
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

using Mono.Unix;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Sources
{
    public abstract class PrimarySource : DatabaseSource
    {
        protected ErrorSource error_source = new ErrorSource (Catalog.GetString ("Import Errors"));
        protected bool error_source_visible = false;

        protected HyenaSqliteCommand remove_range_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreTracks WHERE TrackID IN
                (SELECT ItemID FROM CoreCache
                    WHERE ModelID = ? LIMIT ?, ?);
            DELETE FROM CorePlaylistEntries WHERE TrackID IN
                (SELECT ItemID FROM CoreCache
                    WHERE ModelID = ? LIMIT ?, ?);
            DELETE FROM CoreSmartPlaylistEntries WHERE TrackID IN
                (SELECT ItemID FROM CoreCache
                    WHERE ModelID = ? LIMIT ?, ?)"
        );

        protected HyenaSqliteCommand remove_track_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreTracks WHERE TrackID = ?;
            DELETE FROM CorePlaylistEntries WHERE TrackID = ?;
            DELETE FROM CoreSmartPlaylistEntries WHERE TrackID = ?"
        );

        protected int source_id;
        public int SourceId {
            get { return source_id; }
        }

        protected PrimarySource (string generic_name, string name, string id, int order) : base (generic_name, name, id, order)
        {
            source_id = ServiceManager.DbConnection.Query<int> ("SELECT SourceID FROM CorePrimarySources WHERE StringID = ?", id);
            if (source_id == 0) {
                source_id = ServiceManager.DbConnection.Execute ("INSERT INTO CorePrimarySources (StringID) VALUES (?)", id);
            }

            track_model.Condition = String.Format ("CoreTracks.SourceID = {0}", source_id);;
            error_source.Updated += OnErrorSourceUpdated;
            OnErrorSourceUpdated (null, null);
        }

        public ErrorSource ErrorSource {
            get { return error_source; }
        }

        protected void OnErrorSourceUpdated (object o, EventArgs args)
        {
            if (error_source.Count > 0 && !error_source_visible) {
                AddChildSource (error_source);
                error_source_visible = true;
            } else if (error_source.Count <= 0 && error_source_visible) {
                RemoveChildSource (error_source);
                error_source_visible = false;
            }
        }

        public override void RemoveTrack (DatabaseTrackInfo track)
        {
            remove_track_command.ApplyValues (track.DbId);
            ServiceManager.DbConnection.Execute (remove_track_command);
            Reload ();
            ReloadChildren ();
        }

        /*public override void RemoveTracks (IEnumerable<TrackInfo> tracks)
        {

            // BEGIN transaction

            int i = 0;
            DatabaseTrackInfo ltrack;
            foreach (TrackInfo track in tracks) {
                ltrack = track as DatabaseTrackInfo;
                if (ltrack == null)
                    continue;

                command.ApplyValues (ltrack.DbId, ltrack.DbId, ltrack.DbId);
                ServiceManager.DbConnection.Execute (command);

                if (++i % 100 == 0) {
                    // COMMIT and BEGIN new transaction
                }
            }

            // COMMIT transaction

            // Reload the library, all playlists, etc
            Reload ();
            ReloadChildren ();
        }*/

        public override void RemoveSelectedTracks (TrackListDatabaseModel model)
        {
            base.RemoveSelectedTracks (model);
            ReloadChildren ();
        }

        protected override void RemoveTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            remove_range_command.ApplyValues (
                    model.CacheId, range.Start, range.End - range.Start + 1,
                    model.CacheId, range.Start, range.End - range.Start + 1,
                    model.CacheId, range.Start, range.End - range.Start + 1
            );
            ServiceManager.DbConnection.Execute (remove_range_command);
        }

        public override void DeleteSelectedTracks (TrackListDatabaseModel model)
        {
            base.DeleteSelectedTracks (model);
            ReloadChildren ();
        }

        private void ReloadChildren ()
        {
            foreach (Source child in Children) {
                if (child is ITrackModelSource) {
                    (child as ITrackModelSource).Reload ();
                }
            }
        }
    }
}
