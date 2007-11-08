//
// PlaylistSource.cs
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
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Database;
using Banshee.Sources;
using Banshee.Collection;

namespace Banshee.Playlist
{
    public class PlaylistSource : DatabaseSource
    {
        private const string JOIN = ", CorePlaylistEntries";
        private const string CONDITION = " CorePlaylistEntries.TrackID = CoreTracks.TrackID AND CorePlaylistEntries.PlaylistID = {0}";

        // For existing playlists
        public PlaylistSource (int dbid, string name, int sortColumn, int sortType) : this (name, 500)
        {
            track_model.JoinFragment = JOIN;
            track_model.Condition = String.Format (CONDITION, dbid);
        }

        public PlaylistSource (string name, int order) : base (name, order)
        {
            Properties.SetStringList ("IconName", "source-playlist");
            AfterInitialized ();
        }

        public PlaylistSource (int order) : this ("new playlist", order)
        {
        }

        public static List<PlaylistSource> LoadAll ()
        {
            List<PlaylistSource> sources = new List<PlaylistSource> ();

            IDbCommand command = ServiceManager.DbConnection.CreateCommand ();
            command.CommandText = "SELECT PlaylistID, Name, SortColumn, SortType FROM CorePlaylists";
            IDataReader reader = command.ExecuteReader ();
            
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
        }
        
        public static string UniqueName {
            get { return NamingUtil.PostfixDuplicate(Catalog.GetString("New Playlist"), PlaylistExists); }
        }
        
        public static string GoodUniqueName(IEnumerable tracks)
        {
            return NamingUtil.PostfixDuplicate(NamingUtil.GenerateTrackCollectionName(
                tracks, Catalog.GetString("New Playlist")), PlaylistExists);
        }*/
    }
}
