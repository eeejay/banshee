//
// PlaylistSource.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Playlist
{
    public class PlaylistSource : AbstractPlaylistSource, IUnmapableSource
    {
        private static HyenaSqliteCommand add_track_range_command;
        private static HyenaSqliteCommand add_track_command;
        private static HyenaSqliteCommand remove_track_range_command;

        private static string add_track_range_from_joined_model_sql;

        private static string generic_name = Catalog.GetString ("Playlist");

        protected override string SourceTable {
            get { return "CorePlaylists"; }
        }

        protected override string SourcePrimaryKey {
            get { return "PlaylistID"; }
        }

        protected override string TrackJoinTable {
            get { return "CorePlaylistEntries"; }
        }

        static PlaylistSource () 
        {
            add_track_range_command = new HyenaSqliteCommand (@"
                INSERT INTO CorePlaylistEntries
                    SELECT null, ?, ItemID, 0
                        FROM CoreCache WHERE ModelID = ?
                        LIMIT ?, ?"
            );

            add_track_command = new HyenaSqliteCommand (@"
                INSERT INTO CorePlaylistEntries
                    VALUES (null, ?, ?, 0)"
            );

            add_track_range_from_joined_model_sql = @"
                INSERT INTO CorePlaylistEntries
                    SELECT null, ?, TrackID, 0
                        FROM CoreCache c INNER JOIN {0} e ON c.ItemID = e.{1}
                        WHERE ModelID = ?
                        LIMIT ?, ?";

            remove_track_range_command = new HyenaSqliteCommand (@"
                DELETE FROM CorePlaylistEntries WHERE PlaylistID = ? AND
                    EntryID IN (SELECT ItemID FROM CoreCache
                        WHERE ModelID = ? LIMIT ?, ?)"
            );
        }

#region Constructors

        public PlaylistSource (string name, int primarySourceId) : this (name, null, primarySourceId)
        {
        }

        protected PlaylistSource (string name, int? dbid, int primarySourceId) : this (name, dbid, -1, 0, primarySourceId, 0)
        {
        }

        protected PlaylistSource (string name, int? dbid, int sortColumn, int sortType, int primarySourceId, int count)
            : base (generic_name, name, dbid, sortColumn, sortType, primarySourceId)
        {
            Properties.SetString ("Icon.Name", "source-playlist");
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Playlist"));
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Delete Playlist"));
            DbId = dbid;
            SavedCount = count;
        }

#endregion

#region Source Overrides

        public override bool AcceptsInputFromSource (Source source)
        {
            return base.AcceptsInputFromSource (source) && (Parent == null || source == Parent || source.Parent == Parent);
        }
        
        public override SourceMergeType SupportedMergeTypes {
            get { return SourceMergeType.All; }
        }

#endregion

#region AbstractPlaylist overrides

        protected override void Update ()
        {
            ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (
                String.Format (
                    @"UPDATE {0}
                        SET Name = ?,
                            SortColumn = ?,
                            SortType = ?,
                            CachedCount = ?
                        WHERE PlaylistID = ?",
                    SourceTable
                ), Name, -1, 0, Count, dbid
            ));
        }

        protected override void Create ()
        {
            DbId = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (
                @"INSERT INTO CorePlaylists (PlaylistID, Name, SortColumn, SortType, PrimarySourceID)
                    VALUES (NULL, ?, ?, ?, ?)",
                Name, -1, 1, PrimarySourceId //SortColumn, SortType
            ));
        }

#endregion

#region DatabaseSource overrides

        // We can delete tracks only if our parent can
        public override bool CanDeleteTracks {
            get {
                DatabaseSource ds = Parent as DatabaseSource;
                return ds != null && ds.CanDeleteTracks;
            }
        }

        // We can add tracks only if our parent can
        public override bool CanAddTracks {
            get {
                DatabaseSource ds = Parent as DatabaseSource;
                return ds != null ? ds.CanAddTracks : base.CanAddTracks;
            }
        }

        // Have our parent handle deleting tracks
        public override void DeleteSelectedTracks ()
        {
            if (Parent is PrimarySource) {
                (Parent as PrimarySource).DeleteSelectedTracksFromChild (this);
            }
        }

#endregion

#region IUnmapableSource Implementation

        public virtual bool Unmap ()
        {
            if (DbId != null) {
                ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                    BEGIN TRANSACTION;
                        DELETE FROM CorePlaylists WHERE PlaylistID = ?;
                        DELETE FROM CorePlaylistEntries WHERE PlaylistID = ?;
                    COMMIT TRANSACTION",
                    DbId, DbId
                ));
            }

            Remove ();
            return true;
        }

        public virtual bool CanUnmap {
            get { return true; }
        }

        public virtual bool ConfirmBeforeUnmap {
            get { return true; }
        }

#endregion

        protected void AddTrack (int track_id)
        {
            add_track_command.ApplyValues (DbId, track_id);
            ServiceManager.DbConnection.Execute (add_track_command);
            OnTracksAdded ();
        }
        
        protected override void AddTrack (DatabaseTrackInfo track)
        {
            AddTrack (track.TrackId);
        }
        
        public override bool AddSelectedTracks (Source source)
        {
            if (Parent == null || source == Parent || source.Parent == Parent) {
                return base.AddSelectedTracks (source);
            } else {
                // Adding from a different primary source, so add to our primary source first
                //PrimarySource primary = Parent as PrimarySource;
                //primary.AddSelectedTracks (model);
                // then add to us
                Log.Information ("Note: Feature Not Implemented", String.Format ("In this alpha release, you can only add tracks to {0} from {1} or its playlists.", Name, Parent.Name), true);
            }
            return false;
        }

        DatabaseTrackListModel last_add_range_from_model;
        HyenaSqliteCommand last_add_range_command = null;
        protected override void AddTrackRange (DatabaseTrackListModel from, RangeCollection.Range range)
        {
            last_add_range_command = (!from.CachesJoinTableEntries)
                ? add_track_range_command
                : from == last_add_range_from_model
                    ? last_add_range_command
                    : new HyenaSqliteCommand (String.Format (add_track_range_from_joined_model_sql, from.JoinTable, from.JoinPrimaryKey));

            last_add_range_command.ApplyValues (DbId, from.CacheId, range.Start, range.Count);
            ServiceManager.DbConnection.Execute (last_add_range_command);

            last_add_range_from_model = from;
        }

        protected override void RemoveTrackRange (DatabaseTrackListModel from, RangeCollection.Range range)
        {
            remove_track_range_command.ApplyValues (DbId, from.CacheId, range.Start, range.Count);
            ServiceManager.DbConnection.Execute (remove_track_range_command);
        }

        protected override void HandleTracksChanged (Source sender, TrackEventArgs args)
        {
            if (args.When > last_updated) {
                last_updated = args.When;
                // Optimization: playlists only need to reload for updates if they are sorted by the column updated
                //if (args.ChangedField == null || args.ChangedChanged == [current sort field]) {
                    //if (ServiceManager.DbConnection.Query<int> (count_updated_command, last_updated) > 0) {
                        Reload ();
                    //}
                //}
            }
        }

        protected override void HandleTracksDeleted (Source sender, TrackEventArgs args)
        {
            if (args.When > last_removed) {
                last_removed = args.When;
                Reload ();
                /*if (ServiceManager.DbConnection.Query<int> (count_removed_command, last_removed) > 0) {
                    //ServiceManager.DbConnection.Execute ("DELETE FROM CoreCache WHERE ModelID = ? AND ItemID IN (SELECT EntryID FROM CorePlaylistEntries WHERE PlaylistID = ? AND TrackID IN (TrackID FROM CoreRemovedTracks))");
                    ServiceManager.DbConnection.Execute ("DELETE FROM CorePlaylistEntries WHERE TrackID IN (SELECT TrackID FROM CoreRemovedTracks)");
                    //track_model.UpdateAggregates ();
                    //OnUpdated ();
                }*/
            }
        }

        public static IEnumerable<PlaylistSource> LoadAll (int primary_id)
        {
            using (IDataReader reader = ServiceManager.DbConnection.Query (
                @"SELECT PlaylistID, Name, SortColumn, SortType, PrimarySourceID, CachedCount FROM CorePlaylists 
                    WHERE Special = 0 AND PrimarySourceID = ?", primary_id)) {
                while (reader.Read ()) {
                    yield return new PlaylistSource (
                        reader[1] as string, Convert.ToInt32 (reader[0]),
                        Convert.ToInt32 (reader[2]), Convert.ToInt32 (reader[3]), Convert.ToInt32 (reader[4]),
                        Convert.ToInt32 (reader[5])
                    );
                }
            }
        }

        public override void SetParentSource (Source parent)
        {
            base.SetParentSource (parent);

            PrimarySource primary = parent as PrimarySource;
            if (primary != null) {
                primary.TracksChanged += HandleTracksChanged;
                primary.TracksDeleted += HandleTracksDeleted;
            }
        }
        
        private static int GetPlaylistId (string name)
        {
            return ServiceManager.DbConnection.Query<int> (
                "SELECT PlaylistID FROM Playlists WHERE Name = ? LIMIT 1", name
            );
        }
        
        private static bool PlaylistExists (string name)
        {
            return GetPlaylistId (name) > 0;
        }
        
        public static string CreateUniqueName () 
        {
            return NamingUtil.PostfixDuplicate (Catalog.GetString ("New Playlist"), PlaylistExists);
        }
        
        public static string CreateUniqueName (IEnumerable tracks)
        {
            return NamingUtil.PostfixDuplicate (NamingUtil.GenerateTrackCollectionName (
                tracks, Catalog.GetString ("New Playlist")), PlaylistExists);
        }
    }
}
