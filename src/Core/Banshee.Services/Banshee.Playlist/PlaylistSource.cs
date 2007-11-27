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
    public class PlaylistSource : DatabaseSource
    {
        private const string TRACK_JOIN = ", CorePlaylistEntries";
        private const string TRACK_CONDITION = " CorePlaylistEntries.TrackID = CoreTracks.TrackID AND CorePlaylistEntries.PlaylistID = {0}";
        private int? dbid;

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

        public PlaylistSource (string name, int? dbid) : base (name, 500)
        {
            Properties.SetString ("IconName", "source-playlist");
            DbId = dbid;
        }

        public override void Rename (string newName)
        {
            base.Rename (newName);
            Commit ();
        }

        public void AddTracks (TrackListDatabaseModel from, Selection selection)
        {
            if (from == track_model || selection.Count == 0)
                return;

            lock (from) {
                using (new Timer ("Adding tracks to playlist")) {
                foreach (RangeCollection.Range range in selection.Ranges) {
                    DbCommand command = new DbCommand (String.Format (@"
                        INSERT INTO CorePlaylistEntries
                            SELECT null, {0}, ItemID, 0
                                FROM CoreCache WHERE ModelID = {1}
                                LIMIT {2}, {3}",
                        DbId, from.CacheId, range.Start, range.End - range.Start + 1
                    ));
                    Console.WriteLine ("Adding selection with: {0}\n{1}", command, command.CommandText);
                    ServiceManager.DbConnection.Execute (command);
                }
                }
                Reload ();
            }
        }

        public void RemoveTracks (Selection selection)
        {
            if (selection.Count == 0)
                return;

            lock (track_model) {
                using (new Timer ("Removing tracks from playlist")) {
                foreach (RangeCollection.Range range in selection.Ranges) {
                    DbCommand command = new DbCommand (String.Format (@"
                        DELETE FROM CorePlaylistEntries WHERE TrackID IN
                            (SELECT ItemID FROM CoreCache
                                WHERE ModelID = {0} LIMIT {1}, {2})",
                        track_model.CacheId, range.Start, range.End - range.Start + 1
                    ));
                    Console.WriteLine ("Removing selection with: {0}\n{1}", command, command.CommandText);
                    ServiceManager.DbConnection.Execute (command);
                }
                }
                Reload ();
            }
        }

        private void Reload ()
        {
            track_model.Reload ();
            artist_model.Reload ();
            album_model.Reload ();
        }

        protected void Commit ()
        {
            if (dbid == null)
                return;

            ServiceManager.DbConnection.Execute (
                @"UPDATE CorePlaylists
                    SET Name = :playlist_name,
                        SortColumn = :sort_column,
                        SortType = :sort_type
                    WHERE PlaylistID = :playlist_id",
                "playlist_name", Name,
                "sort_column", -1,
                "sort_type", 0,
                "playlist_id", dbid
            );
        }

        private static int CreateNewPlaylist (string name)
        {
            return ServiceManager.DbConnection.Execute (
                @"INSERT INTO CorePlaylists
                    VALUES (NULL, :playlist_name, -1, 0)",
                    "playlist_name", name
            );
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
