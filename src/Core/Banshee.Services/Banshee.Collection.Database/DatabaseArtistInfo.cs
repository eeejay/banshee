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

        private static HyenaSqliteCommand select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreArtists.Name = ?",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private enum Column : int {
            ArtistID,
            Name
        }

        private static string last_artist_name = null;
        private static DatabaseArtistInfo last_artist = null;

        public static DatabaseArtistInfo FindOrCreate (string artistName)
        {
            DatabaseArtistInfo artist = new DatabaseArtistInfo ();
            artist.Name = artistName;
            return FindOrCreate (artist);
        }

        public static DatabaseArtistInfo FindOrCreate (DatabaseArtistInfo artist)
        {
            if (artist.Name == last_artist_name && last_artist != null) {
                return last_artist;
            }

            if (String.IsNullOrEmpty (artist.Name) || artist.Name.Trim () == String.Empty) {
                artist.Name = Catalog.GetString ("Unknown Artist");
            }

            using (IDataReader reader = ServiceManager.DbConnection.Query (select_command, artist.Name)) {
                if (reader.Read ()) {
                    last_artist = provider.Load (reader);
                } else {
                    last_artist = artist;
                    last_artist.Save ();
                }
            }
            
            last_artist_name = artist.Name;
            return last_artist;
        }

        public static DatabaseArtistInfo UpdateOrCreate (DatabaseArtistInfo artist)
        {
            DatabaseArtistInfo found = FindOrCreate (artist);
            if (found != artist) {
                // Overwrite the found artist
                artist.Name = found.Name;
                artist.dbid = found.DbId;
                artist.Save ();
            }
            return artist;
        }
        
        public DatabaseArtistInfo () : base (null)
        {
        }

        public void Save ()
        {
            Provider.Save (this);
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
               
        [DatabaseColumn("MusicBrainzID")]
        public override string MusicBrainzId {
            get { return base.MusicBrainzId; }
            set { base.MusicBrainzId = value; }
        }
        
        [DatabaseColumn(Select = false)]
        protected string NameLowered {
            get { return Name == null ? null : Name.ToLower (); }
        }

        public override string ToString ()
        {
            return String.Format ("DatabaseArtistInfo<DbId: {0}, Name: {1}>", DbId, Name);
        }
    }
}
