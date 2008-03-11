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

using Hyena.Data.Sqlite;

using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{
    public class LibraryAlbumInfo : AlbumInfo, ICacheableItem
    {
        private static BansheeModelProvider<LibraryAlbumInfo> provider = new BansheeModelProvider<LibraryAlbumInfo> (
            ServiceManager.DbConnection, "CoreAlbums"
        );

        public static BansheeModelProvider<LibraryAlbumInfo> Provider {
            get { return provider; }
        }

        private static HyenaSqliteCommand select_command = new HyenaSqliteCommand (
            "SELECT AlbumID, Title FROM CoreAlbums WHERE ArtistID = ? AND Title = ?"
        );

        private enum Column : int {
            AlbumID,
            Title,
            ArtistName
        }

        public static LibraryAlbumInfo FindOrCreate (LibraryArtistInfo artist, string title)
        {
            LibraryAlbumInfo album;

            if (title == null || title.Trim () == String.Empty)
                title = Catalog.GetString ("Unknown Album");

            using (IDataReader reader = ServiceManager.DbConnection.Query (select_command, artist.DbId, title)) {
                if (reader.Read ()) {
                    album = new LibraryAlbumInfo (reader);
                    album.ArtistName = artist.Name;
                } else {
                    album = new LibraryAlbumInfo ();
                    album.Title = title;
                    album.ArtistId = artist.DbId;
                    album.ArtistName = artist.Name;
                    album.Save ();
                }
            }
            return album;
        }

        public LibraryAlbumInfo () : base (null)
        {
        }

        protected LibraryAlbumInfo (IDataReader reader) : base (null)
        {
            LoadFromReader (reader);
        }

        private void LoadFromReader (IDataReader reader)
        {
            dbid = Convert.ToInt32 (reader[(int) Column.AlbumID]);
            Title = reader[(int) Column.Title] as string;
        }

        public void Save ()
        {
            Provider.Save (this);
        }

        [DatabaseColumn("AlbumID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int dbid;
        public int DbId {
            get { return dbid; }
        }

        private long cache_id;
        public long CacheId {
            get { return cache_id; }
            set { cache_id = value; }
        }

        [DatabaseColumn("ArtistID")]
        private int artist_id;
        public int ArtistId {
            get { return artist_id; }
            set { artist_id = value; }
        }

        [DatabaseColumn]
        public override string Title {
            get { return base.Title; }
            set { base.Title = value; }
        }

        [VirtualDatabaseColumn("Name", "CoreArtists", "ArtistID", "ArtistID")]
        public override string ArtistName {
            get { return base.ArtistName; }
            set { base.ArtistName = value; }
        }

        public override string ToString ()
        {
            return String.Format ("<LibraryAlbumInfo Title={0} DbId={1}>", Title, DbId);
        }
    }
}
