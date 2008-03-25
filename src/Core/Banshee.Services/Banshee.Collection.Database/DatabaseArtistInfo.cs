//
// DatabaseArtistInfo.cs
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
    public class DatabaseArtistInfo : ArtistInfo, ICacheableItem
    {
        private static BansheeModelProvider<DatabaseArtistInfo> provider = new BansheeModelProvider<DatabaseArtistInfo> (
            ServiceManager.DbConnection, "CoreArtists"
        );

        public static BansheeModelProvider<DatabaseArtistInfo> Provider {
            get { return provider; }
        }

        private static HyenaSqliteCommand select_command = new HyenaSqliteCommand (
            "SELECT ArtistID, Name FROM CoreArtists WHERE Name = ?"
        );

        private enum Column : int {
            ArtistID,
            Name
        }

        private static string last_artist_name = null;
        private static DatabaseArtistInfo last_artist = null;
        public static DatabaseArtistInfo FindOrCreate (string artistName)
        {
            if (artistName == last_artist_name) {
                return last_artist;
            }

            if (artistName == null || artistName.Trim () == String.Empty)
                artistName = Catalog.GetString ("Unknown Artist");

            using (IDataReader reader = ServiceManager.DbConnection.Query (select_command, artistName)) {
                if (reader.Read ()) {
                    last_artist = new DatabaseArtistInfo (reader);
                } else {
                    last_artist = new DatabaseArtistInfo ();
                    last_artist.Name = artistName;
                    last_artist.Save ();
                }
            }
            
            last_artist_name = artistName;
            return last_artist;
        }
        
        public DatabaseArtistInfo () : base (null)
        {
        }

        protected DatabaseArtistInfo (IDataReader reader) : base (null)
        {
            LoadFromReader (reader);
        }

        public void Save ()
        {
            Provider.Save (this);
        }

        private void LoadFromReader (IDataReader reader)
        {
            dbid = Convert.ToInt32 (reader[(int)Column.ArtistID]);
            Name = reader[(int)Column.Name] as string;
        }

        [DatabaseColumn("ArtistID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int dbid;
        public int DbId {
            get { return dbid; }
        }

        private long cache_entry_id;
        public long CacheEntryId {
            get { return cache_entry_id; }
            set { cache_entry_id = value; }
        }

        private long cache_model_id;
        public long CacheModelId {
            get { return cache_model_id; }
            set { cache_model_id = value; }
        }

        [DatabaseColumn]
        public override string Name {
            get { return base.Name; }
            set { base.Name = value; }
        }
    }
}
