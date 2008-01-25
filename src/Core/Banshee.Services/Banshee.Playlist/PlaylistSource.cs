//
// PlaylistSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;
using Hyena.Collections;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;

namespace Banshee.Playlist
{
    public class PlaylistSource : AbstractPlaylistSource
    {
        private static BansheeDbCommand add_tracks_command;
        private static BansheeDbCommand remove_tracks_command;
        private static BansheeDbCommand delete_command;

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

        protected override string IconName {
            get { return "source-playlist"; }
        }

        static PlaylistSource () {
            add_tracks_command = new BansheeDbCommand (@"
                INSERT INTO CorePlaylistEntries
                    SELECT null, ?, ItemID, 0
                        FROM CoreCache WHERE ModelID = ?
                        LIMIT ?, ?", 4
            );

            remove_tracks_command = new BansheeDbCommand (@"
                DELETE FROM CorePlaylistEntries WHERE PlaylistID = ? AND
                    TrackID IN (SELECT ItemID FROM CoreCache
                        WHERE ModelID = ? LIMIT ?, ?)", 4
            );
        }

#region Constructors

        public PlaylistSource (string name) : this (name, null)
        {
        }

        public PlaylistSource (string name, int? dbid) : this (name, dbid, -1, 0)
        {
        }

        public PlaylistSource (string name, int? dbid, int sortColumn, int sortType) : base (generic_name, name, dbid, sortColumn, sortType)
        {
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Playlist"));
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Delete Playlist"));
            DbId = dbid;
        }

#endregion

#region AbstractPlaylist overrides

        protected override void Update ()
        {
            ServiceManager.DbConnection.Execute (new BansheeDbCommand (
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
            DbId = ServiceManager.DbConnection.Execute (new BansheeDbCommand (
                String.Format (@"INSERT INTO {0}
                    VALUES (NULL, ?, ?, ?)",
                    SourceTable
                ), Name, -1, 0 //SortColumn, SortType
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

        public bool Unmap ()
        {
            if (DbId != null) {
                ServiceManager.DbConnection.Execute (new BansheeDbCommand (@"
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

        public bool CanUnmap {
            get { return true; }
        }

        public bool ConfirmBeforeUnmap {
            get { return true; }
        }

#endregion

        public virtual void AddSelectedTracks (TrackListDatabaseModel from)
        {
            if (from == track_model)
                return;

            WithTrackSelection (from, AddTrackRange);
        }

        protected virtual void AddTrackRange (TrackListDatabaseModel from, RangeCollection.Range range)
        {
            add_tracks_command.ApplyValues (DbId, from.CacheId, range.Start, range.End - range.Start + 1);
            Console.WriteLine ("adding tracks with {0}", add_tracks_command.CommandText);
            ServiceManager.DbConnection.Execute (add_tracks_command);
        }

        protected override void RemoveTrackRange (TrackListDatabaseModel from, RangeCollection.Range range)
        {
            remove_tracks_command.ApplyValues (DbId, from.CacheId, range.Start, range.End - range.Start + 1);
            ServiceManager.DbConnection.Execute (remove_tracks_command);
        }

        public static List<PlaylistSource> LoadAll ()
        {
            List<PlaylistSource> sources = new List<PlaylistSource> ();

            using (IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT PlaylistID, Name, SortColumn, SortType FROM CorePlaylists")) {
                while (reader.Read ()) {
                    PlaylistSource playlist = new PlaylistSource (
                        reader[1] as string, Convert.ToInt32 (reader[0]),
                        Convert.ToInt32 (reader[2]), Convert.ToInt32 (reader[3])
                    );
                    sources.Add (playlist);
                }
            }
            
            return sources;
        }
    }

    public static class PlaylistUtil
    {
        /*internal static int GetPlaylistID(string name)
        {
            try {
                return Convert.ToInt32(Globals.Library.Db.QuerySingle(new DbCommand(
                @"SELECT PlaylistID
                    FROM Playlists
                    WHERE Name = :name
                    LIMIT 1",
                    "name", name
                )));
            } catch(Exception) {
                return 0;
            }
        }*/
        
        /*internal static bool PlaylistExists(string name)
        {
            return GetPlaylistID(name) > 0;
        }*/
        
        /*public static string UniqueName {
            get { return NamingUtil.PostfixDuplicate(Catalog.GetString("New Playlist"), PlaylistExists); }
        }
        
        public static string GoodUniqueName(IEnumerable tracks)
        {
            return NamingUtil.PostfixDuplicate(NamingUtil.GenerateTrackCollectionName(
                tracks, Catalog.GetString("New Playlist")), PlaylistExists);
        }*/
    }
}
