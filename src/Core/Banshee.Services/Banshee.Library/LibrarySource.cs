//
// LibrarySource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using Hyena.Collections;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Library
{
    public class LibrarySource : DatabaseSource
    {
        private ErrorSource error_source = new ErrorSource (Catalog.GetString ("Import Errors"));
        private bool error_source_visible = false;

        private HyenaSqliteCommand remove_range_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreTracks WHERE TrackID IN
                (SELECT ItemID FROM CoreCache
                    WHERE ModelID = ? LIMIT ?, ?);
            DELETE FROM CorePlaylistEntries WHERE TrackID IN
                (SELECT ItemID FROM CoreCache
                    WHERE ModelID = ? LIMIT ?, ?);
            DELETE FROM CoreSmartPlaylistEntries WHERE TrackID IN
                (SELECT ItemID FROM CoreCache
                    WHERE ModelID = ? LIMIT ?, ?)", 9
        );

        private HyenaSqliteCommand remove_track_command = new HyenaSqliteCommand (@"
            DELETE FROM CoreTracks WHERE TrackID = ?;
            DELETE FROM CorePlaylistEntries WHERE TrackID = ?;
            DELETE FROM CoreSmartPlaylistEntries WHERE TrackID = ?", 3
        );
    
        public LibrarySource () : base (Catalog.GetString("Library"), Catalog.GetString ("Library"), "Library", 1)
        {
            Properties.SetStringList ("IconName", "go-home", "user-home", "source-library");
            Properties.SetString ("GtkActionPath", "/LibraryContextMenu");
            AfterInitialized ();

            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Library"));
            
            error_source.Updated += OnErrorSourceUpdated;
            OnErrorSourceUpdated (null, null);
        }
        
        private void OnErrorSourceUpdated (object o, EventArgs args)
        {
            if (error_source.Count > 0 && !error_source_visible) {
                AddChildSource (error_source);
                error_source_visible = true;
            } else if (error_source.Count <= 0 && error_source_visible) {
                RemoveChildSource (error_source);
                error_source_visible = false;
            }
        }

        /*public override void RemoveTracks (IEnumerable<TrackInfo> tracks)
        {

            // BEGIN transaction

            int i = 0;
            LibraryTrackInfo ltrack;
            foreach (TrackInfo track in tracks) {
                ltrack = track as LibraryTrackInfo;
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

        protected override void DeleteTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            for (int i = range.Start; i <= range.End; i++) {
                LibraryTrackInfo track = model [i] as LibraryTrackInfo;
                if (track == null)
                    continue;

                try {
                    // Remove from file system
                    try {
                        Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
                    } catch (System.IO.FileNotFoundException e) {
                    } catch (System.IO.DirectoryNotFoundException e) {}

                    // Remove from database
                    remove_track_command.ApplyValues (track.DbId, track.DbId, track.DbId);
                    ServiceManager.DbConnection.Execute (remove_track_command);
                } catch (Exception e) {
                    ErrorSource.AddMessage (e.Message, track.Uri.ToString ());
                }
            }
        }

        private void ReloadChildren ()
        {
            foreach (Source child in Children) {
                if (child is ITrackModelSource) {
                    (child as ITrackModelSource).Reload ();
                }
            }
        }
        
        public ErrorSource ErrorSource {
            get { return error_source; }
        }

        public override bool CanRename {
            get { return false; }
        }
    }
}
