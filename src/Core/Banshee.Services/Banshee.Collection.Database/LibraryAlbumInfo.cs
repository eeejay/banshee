//
// LibraryAlbumInfo.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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

using Mono.Unix;

using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{    
    public class LibraryAlbumInfo : AlbumInfo
    {
        private static BansheeDbCommand select_command = new BansheeDbCommand (
            "SELECT AlbumID, Title FROM CoreAlbums WHERE ArtistID = ? AND Title = ?", 2
        );

        private int dbid;
        private int artist_id;

        private enum Column : int {
            AlbumID,
            Title,
            ArtistName
        }

        public LibraryAlbumInfo (LibraryArtistInfo artist, string title) : base (null)
        {
            if (title == null || title.Trim () == String.Empty)
                title = Catalog.GetString ("Unknown Album");

            IDataReader reader = ServiceManager.DbConnection.ExecuteReader (select_command.ApplyValues (artist.DbId, title));

            if (reader.Read ()) {
                dbid = Convert.ToInt32 (reader[(int) Column.AlbumID]);
                Title = reader[(int) Column.Title] as string;
                ArtistName = artist.Name;
            } else {
                dbid = -1;
                artist_id = artist.DbId;
                Title = title;
                Save ();
            }

            reader.Dispose ();
        }

        public LibraryAlbumInfo(IDataReader reader) : base(null)
        {
            LoadFromReader(reader);
        }

        public void Save ()
        {
            if (DbId < 0) {
                InsertCommit ();
            } else {
                UpdateCommit ();
            }
        }

        private void InsertCommit ()
        {
            TableSchema.CoreAlbums.InsertCommand.ApplyValues (
                null, // AlbumID
                ArtistId,
                -1, // TagSetID
                null, // MusicBrainzID
                Title,
                0,
                0,
                0,
                0
            );
            
            dbid = ServiceManager.DbConnection.Execute (TableSchema.CoreAlbums.InsertCommand);
        }
        
        private void UpdateCommit ()
        {
            TableSchema.CoreAlbums.UpdateCommand.ApplyValues (
                DbId,
                ArtistId,
                -1, // TagSetID
                null, // MusicBrainzID
                Title,
                0,
                0,
                0,
                0,
                DbId // AlbumID (again, for WHERE clause)
            );

            ServiceManager.DbConnection.Execute (TableSchema.CoreAlbums.UpdateCommand);
        }

        private void LoadFromReader(IDataReader reader)
        {
            dbid = Convert.ToInt32(reader[(int)Column.AlbumID]);
            Title = reader[(int)Column.Title] as string;
            ArtistName = reader[(int)Column.ArtistName] as string;
        }

        public int DbId {
            get { return dbid; }
        }

        public int ArtistId {
            get { return artist_id; }
        }
    }
}
