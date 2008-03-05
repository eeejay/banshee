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
    public class LibraryArtistInfo : ArtistInfo
    {
        private static BansheeModelProvider<LibraryArtistInfo> provider = new BansheeModelProvider<LibraryArtistInfo> (
            ServiceManager.DbConnection, "CoreArtists"
        );

        public static BansheeModelProvider<LibraryArtistInfo> Provider {
            get { return provider; }
        }

        private static HyenaSqliteCommand select_command = new HyenaSqliteCommand (
            "SELECT ArtistID, Name FROM CoreArtists WHERE Name = ?"
        );

        private enum Column : int {
            ArtistID,
            Name
        }

        public static LibraryArtistInfo FindOrCreate (string artistName)
        {
            LibraryArtistInfo artist;

            if (artistName == null || artistName.Trim () == String.Empty)
                artistName = Catalog.GetString ("Unknown Artist");

            using (IDataReader reader = ServiceManager.DbConnection.Query (select_command, artistName)) {
                if (reader.Read ()) {
                    artist = new LibraryArtistInfo (reader);
                } else {
                    artist = new LibraryArtistInfo ();
                    artist.Name = artistName;
                    artist.Save ();
                }
            }
            return artist;
        }
        
        public LibraryArtistInfo () : base (null)
        {
        }

        protected LibraryArtistInfo (IDataReader reader) : base (null)
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

        [DatabaseColumn]
        public override string Name {
            get { return base.Name; }
            set { base.Name = value; }
        }
    }
}
