//
// LibraryArtistInfo.cs
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
    [DatabaseTable("CoreArtists", 1)]
    public class LibraryArtistInfo : ArtistInfo, IDatabaseItem
    {
        private static BansheeModelProvider<LibraryArtistInfo> provider = new BansheeModelProvider<LibraryArtistInfo> (
            ServiceManager.DbConnection
        );

        public static BansheeModelProvider<LibraryArtistInfo> Provider {
            get { return provider; }
        }

        private static BansheeDbCommand select_command = new BansheeDbCommand (
            "SELECT ArtistID, Name FROM CoreArtists WHERE Name = ?", 1
        );

        private int dbid;

        private enum Column : int {
            ArtistID,
            Name
        }
        
        public LibraryArtistInfo () : base (null)
        {
        }

        public LibraryArtistInfo (string artistName) : base (null)
        {
            if (artistName == null || artistName.Trim () == String.Empty)
                artistName = Catalog.GetString ("Unknown Artist");

            IDataReader reader = ServiceManager.DbConnection.ExecuteReader (select_command.ApplyValues (artistName));

            if (reader.Read ()) {
                LoadFromReader (reader);
            } else {
                dbid = -1;
                Name = artistName;
                Save ();
            }

            reader.Dispose ();
        }

        public LibraryArtistInfo(IDataReader reader) : base(null)
        {
            LoadFromReader(reader);
        }

        public void Save ()
        {
            if (DbId < 0) {
                dbid = Provider.Insert (this);
            } else {
                Provider.Update (this);
            }
        }

        private void LoadFromReader(IDataReader reader)
        {
            dbid = Convert.ToInt32(reader[(int)Column.ArtistID]);
            Name = reader[(int)Column.Name] as string;
        }

        [DatabaseColumn("ArtistID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        public int DbId {
            get { return dbid; }
            internal set { dbid = value; }
        }

        [DatabaseColumn]
        public override string Name {
            get { return base.Name; }
            set { base.Name = value; }
        }
        
        private int db_index;
        public int DbIndex {
            get { return db_index; }
            set { db_index = value; }
        }
    }
}
