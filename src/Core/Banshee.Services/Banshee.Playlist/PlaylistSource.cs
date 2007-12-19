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
    public class PlaylistSource : DatabaseSource, IUnmapableSource
    {
        private const string TRACK_JOIN = ", CorePlaylistEntries";
        private const string TRACK_CONDITION = " CorePlaylistEntries.TrackID = CoreTracks.TrackID AND CorePlaylistEntries.PlaylistID = {0}";

        private int? dbid;

        private static string generic_name = Catalog.GetString ("Playlist");
        private static BansheeDbCommand add_tracks_command;
        private static BansheeDbCommand remove_tracks_command;
        private static BansheeDbCommand delete_command;

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

        protected int? DbId {
            get { return dbid; }
            set {
                if (value == null)
                    return;
                dbid = value;
                track_model.JoinFragment = TRACK_JOIN;
                track_model.Condition = String.Format (TRACK_CONDITION, dbid);
                AfterInitialized ();
            }
        }

        public PlaylistSource (int dbid, string name, int sortColumn, int sortType) : this (name, dbid)
        {
        }

        public PlaylistSource (string name) : this (name, CreateNewPlaylist (name))
        {
        }

        public PlaylistSource (string name, int? dbid) : base (generic_name, name, 500)
        {
            Properties.SetString ("IconName", "source-playlist");
            Properties.SetString ("RemoveTracksActionLabel", Catalog.GetString ("Remove From Playlist"));
            Properties.SetString ("UnmapSourceActionLabel", Catalog.GetString ("Delete Playlist"));
            DbId = dbid;
        }

#region Source overrides

        public override void Rename (string newName)
        {
            base.Rename (newName);
            Commit ();
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

        public void AddTracks (TrackListDatabaseModel from, Selection selection)
        {
            if (from == track_model || selection.Count == 0)
                return;

            lock (from) {
                using (new Timer ("Adding tracks to playlist")) {
                    foreach (RangeCollection.Range range in selection.Ranges) {
                        add_tracks_command.ApplyValues (DbId, from.CacheId, range.Start, range.End - range.Start + 1);
                        Console.WriteLine ("Adding selection with: {0}\n{1}", add_tracks_command, add_tracks_command.CommandText);
                        ServiceManager.DbConnection.Execute (add_tracks_command);
                    }
                }
                Reload ();
            }
        }

        protected override void RemoveTrackRange (TrackListDatabaseModel model, RangeCollection.Range range)
        {
            remove_tracks_command.ApplyValues (DbId, model.CacheId, range.Start, range.End - range.Start + 1);
            Console.WriteLine ("Removing selection with: {0}\n{1}", remove_tracks_command, remove_tracks_command.CommandText);
            ServiceManager.DbConnection.Execute (remove_tracks_command);
        }

        protected void Commit ()
        {
            if (dbid == null)
                return;

            ServiceManager.DbConnection.Execute (new BansheeDbCommand (
                @"UPDATE CorePlaylists
                    SET Name = ?,
                        SortColumn = ?,
                        SortType = ?
                    WHERE PlaylistID = ?",
                Name, -1, 0, dbid
            ));
        }

        private static int CreateNewPlaylist (string name)
        {
            return ServiceManager.DbConnection.Execute (new BansheeDbCommand (
                @"INSERT INTO CorePlaylists
                    VALUES (NULL, ?, -1, 0)",
                name
            ));
        }

        public static List<PlaylistSource> LoadAll ()
        {
            List<PlaylistSource> sources = new List<PlaylistSource> ();

            IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT PlaylistID, Name, SortColumn, SortType FROM CorePlaylists"
            );
            
            while (reader.Read ()) {
                PlaylistSource playlist = new PlaylistSource (
                    Convert.ToInt32 (reader[0]), (string) reader[1], 
                    Convert.ToInt32 (reader[2]), Convert.ToInt32 (reader[3])
                );
                sources.Add (playlist);
            }
            
            reader.Dispose();
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
