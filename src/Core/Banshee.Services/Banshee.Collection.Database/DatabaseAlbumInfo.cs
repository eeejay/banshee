//
// DatabaseAlbumInfo.cs
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

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Collection.Database
{
    public class DatabaseAlbumInfo : AlbumInfo
    {
        private static BansheeModelProvider<DatabaseAlbumInfo> provider = new BansheeModelProvider<DatabaseAlbumInfo> (
            ServiceManager.DbConnection, "CoreAlbums"
        );

        public static BansheeModelProvider<DatabaseAlbumInfo> Provider {
            get { return provider; }
        }

        private static HyenaSqliteCommand default_select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreAlbums.ArtistID = ? AND CoreAlbums.Title = ?",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static HyenaSqliteCommand null_select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreAlbums.ArtistID = ? AND CoreAlbums.Title IS NULL",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static int last_artist_id;
        private static string last_title;
        private static DatabaseAlbumInfo last_album;
        
        public static void Reset ()
        {
            last_artist_id = -1;
            last_title = null;
            last_album = null;
        }

        public static DatabaseAlbumInfo FindOrCreate (DatabaseArtistInfo artist, string title, string title_sort, bool isCompilation)
        {
            DatabaseAlbumInfo album = new DatabaseAlbumInfo ();
            album.Title = title;
            album.TitleSort = title_sort;
            album.IsCompilation = isCompilation;
            return FindOrCreate (artist, album);
        }

        private static IDataReader FindExistingArtists (int artist_id, string title)
        {
            HyenaSqliteConnection db = ServiceManager.DbConnection;
            if (title == null) {
                return db.Query (null_select_command, artist_id);
            }
            return db.Query (default_select_command, artist_id, title);
        }
        
        public static DatabaseAlbumInfo FindOrCreate (DatabaseArtistInfo artist, DatabaseAlbumInfo album)
        {
            if (album.Title == last_title && artist.DbId == last_artist_id && last_album != null) {
                return last_album;
            }

            if (String.IsNullOrEmpty (album.Title) || album.Title.Trim () == String.Empty) {
                album.Title = null;
            }

            using (IDataReader reader = FindExistingArtists (artist.DbId, album.Title)) {
                if (reader.Read ()) {
                    bool save = false;
                    last_album = provider.Load (reader);

                    // If the artist name has changed since last time (but it's the same artist) then update our copy of the ArtistName
                    if (last_album.ArtistName != artist.Name) {
                        last_album.ArtistName = artist.Name;
                        save = true;
                    }
                    
                    // Ditto artist sort name
                    if (last_album.ArtistNameSort != artist.NameSort) {
                        last_album.ArtistNameSort = artist.NameSort;
                        save = true;
                    }

                    // And album sort name
                    if (last_album.TitleSort != album.TitleSort) {
                        last_album.TitleSort = album.TitleSort;
                        save = true;
                    }

                    // If the album IsCompilation status has changed, update the saved album info
                    if (last_album.IsCompilation != album.IsCompilation) {
                        last_album.IsCompilation = album.IsCompilation;
                        save = true;
                    }

                    if (save) {
                        last_album.Save ();
                    }
                } else {
                    album.ArtistId = artist.DbId;
                    album.ArtistName = artist.Name;
                    album.ArtistNameSort = artist.NameSort;
                    album.Save ();
                    last_album = album;
                }
            }
            
            last_title = album.Title;
            last_artist_id = artist.DbId;
            return last_album;
        }

        public static DatabaseAlbumInfo UpdateOrCreate (DatabaseArtistInfo artist, DatabaseAlbumInfo album)
        {
            DatabaseAlbumInfo found = FindOrCreate (artist, album);
            if (found != album) {
                // Overwrite the found album
                album.Title = found.Title;
                album.TitleSort = found.TitleSort;
                album.ArtistName = found.ArtistName;
                album.ArtistNameSort = found.ArtistNameSort;
                album.IsCompilation = found.IsCompilation;
                album.dbid = found.DbId;
                album.ArtistId = found.ArtistId;
                album.Save ();
            }
            return album;
        }

        public DatabaseAlbumInfo () : base (null)
        {
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

        [DatabaseColumn("ArtistID")]
        private int artist_id;
        public int ArtistId {
            get { return artist_id; }
            set { artist_id = value; }
        }
        
        [DatabaseColumn("MusicBrainzID")]
        public override string MusicBrainzId {
            get { return base.MusicBrainzId; }
            set { base.MusicBrainzId = value; }
        }
        
        [DatabaseColumn]
        public override DateTime ReleaseDate {
            get { return base.ReleaseDate; }
            set { base.ReleaseDate = value; }
        }
        
        [DatabaseColumn]
        public override bool IsCompilation {
            get { return base.IsCompilation; }
            set { base.IsCompilation = value; }
        }

        [DatabaseColumn]
        public override string Title {
            get { return base.Title; }
            set { base.Title = value; }
        }
        
        [DatabaseColumn]
        public override string TitleSort {
            get { return base.TitleSort; }
            set { base.TitleSort = value; }
        }
        
        [DatabaseColumn(Select = false)]
        internal byte[] TitleSortKey {
            get { return Hyena.StringUtil.SortKey (TitleSort ?? DisplayTitle); }
        }

        [DatabaseColumn(Select = false)]
        internal string TitleLowered {
            get { return Hyena.StringUtil.SearchKey (DisplayTitle); }
        }

        [DatabaseColumn]
        public override string ArtistName {
            get { return base.ArtistName; }
            set { base.ArtistName = value; }
        }

        [DatabaseColumn]
        public override string ArtistNameSort {
            get { return base.ArtistNameSort; }
            set { base.ArtistNameSort = value; }
        }

        [DatabaseColumn(Select = false)]
        internal byte[] ArtistNameSortKey {
            get { return Hyena.StringUtil.SortKey (ArtistNameSort ?? DisplayArtistName); }
        }
        
        [DatabaseColumn(Select = false)]
        internal string ArtistNameLowered {
            get { return Hyena.StringUtil.SearchKey (DisplayArtistName); }
        }

        public override string ToString ()
        {
            return String.Format ("<LibraryAlbumInfo Title={0} DbId={1}>", Title, DbId);
        }
    }
}
