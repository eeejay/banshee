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
        //private static HyenaSqliteCommand add_track_command;
        private static HyenaSqliteCommand remove_track_command;
        private static HyenaSqliteCommand add_track_range_command;
        private static HyenaSqliteCommand remove_track_range_command;

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
            //add_track_command = new HyenaSqliteCommand (
            //    "INSERT INTO CorePlaylistEntries (PlaylistID, TrackID) VALUES (?, ?)"
            //);

            remove_track_command = new HyenaSqliteCommand (
                "DELETE FROM CorePlaylistEntries WHERE PlaylistID = ? AND TrackID = ?"
            );

            add_track_range_command = new HyenaSqliteCommand (@"
                INSERT INTO CorePlaylistEntries
                    SELECT null, ?, ItemID, 0
                        FROM CoreCache WHERE ModelID = ?
                        LIMIT ?, ?"
            );

            remove_track_range_command = new HyenaSqliteCommand (@"
                DELETE FROM CorePlaylistEntries WHERE PlaylistID = ? AND
                    TrackID IN (SELECT ItemID FROM CoreCache
                        WHERE ModelID = ? LIMIT ?, ?)"
            );
        }

#region Constructors

        public PlaylistSource (string name) : this (name, null)
        {
        }

        public PlaylistSource (string name, int? dbid) : this (name, dbid, -1, 0)
        {
        }

        public PlaylistSource (string name, int? dbid, int sortColumn, int sortType) 
            : base (generic_name, name, dbid, sortColumn, sortType)
        {
            Properties.SetString ("Icon.Name", "source-playlist");
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Playlist"));
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Delete Playlist"));
            DbId = dbid;
        }

#endregion

#region Source Overrides

        public override bool AcceptsInputFromSource (Source source)
        {
            // TODO: Probably should be more restrictive than this
            return source is DatabaseSource;
        }
        
        public override void MergeSourceInput (Source from, SourceMergeType mergeType)
        {
            DatabaseSource source = from as DatabaseSource;
            if (source == null || !(source.TrackModel is TrackListDatabaseModel)) {
                return;
            }
            
            TrackListDatabaseModel model = (TrackListDatabaseModel)source.TrackModel;
            
            switch (mergeType) {
                case SourceMergeType.ModelSelection:
                    AddSelectedTracks (model);
                    break;
                case SourceMergeType.Source:
                    AddTrackRange (model, new RangeCollection.Range (0, model.Count));
                    Reload ();
                    break;
            }
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
                            SortType = ?
                        WHERE PlaylistID = ?",
                    SourceTable
                ), Name, -1, 0, dbid
            ));
        }

        protected override void Create ()
        {
            DbId = ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (
                @"INSERT INTO CorePlaylists (PlaylistID, Name, SortColumn, SortType)
                    VALUES (NULL, ?, ?, ?)",
                Name, -1, 1 //SortColumn, SortType
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

        // Have our parent handle deleting tracks
        public override void DeleteSelectedTracks (TrackListDatabaseModel model)
        {
            (Parent as DatabaseSource).DeleteSelectedTracks (model);
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

        /*public override void AddTrack (LibraryTrackInfo track)
        {
            add_track_command.ApplyValues (DbId, track.DbId);
            ServiceManager.DbConnection.Execute (add_track_command);
            Reload ();
        }*/
        
        public virtual void AddSelectedTracks (TrackListDatabaseModel from)
        {
            if (from == track_model)
                return;

            WithTrackSelection (from, AddTrackRange);
            OnUserNotifyUpdated ();
        }

        protected virtual void AddTrackRange (TrackListDatabaseModel from, RangeCollection.Range range)
        {
            add_track_range_command.ApplyValues (DbId, from.CacheId, range.Start, range.End - range.Start + 1);
            ServiceManager.DbConnection.Execute (add_track_range_command);
        }

        public override void RemoveTrack (LibraryTrackInfo track)
        {
            remove_track_command.ApplyValues (DbId, track.DbId);
            ServiceManager.DbConnection.Execute (remove_track_command);
            Reload ();
        }

        protected override void RemoveTrackRange (TrackListDatabaseModel from, RangeCollection.Range range)
        {
            remove_track_range_command.ApplyValues (DbId, from.CacheId, range.Start, range.End - range.Start + 1);
            ServiceManager.DbConnection.Execute (remove_track_range_command);
        }

        public static IEnumerable<PlaylistSource> LoadAll ()
        {
            using (IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT PlaylistID, Name, SortColumn, SortType FROM CorePlaylists WHERE Special = 0")) {
                while (reader.Read ()) {
                    yield return new PlaylistSource (
                        reader[1] as string, Convert.ToInt32 (reader[0]),
                        Convert.ToInt32 (reader[2]), Convert.ToInt32 (reader[3])
                    );
                }
            }
        }
        
        public static int GetPlaylistId (string name)
        {
            object result = ServiceManager.DbConnection.ExecuteScalar (new HyenaSqliteCommand (
                "SELECT PlaylistID FROM Playlists WHERE Name = ? LIMIT 1", name));
            
            if (result != null) {
                return Convert.ToInt32 (result);
            }
            
            return 0;
        }
        
        public static bool PlaylistExists (string name)
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
