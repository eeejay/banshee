//
// ArtistListDatabaseModel.cs
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
using System.Collections.Generic;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class ArtistListDatabaseModel : ArtistListModel
    {
        private Dictionary<int, ArtistInfo> artists = new Dictionary<int, ArtistInfo>();
        private BansheeDbConnection connection;
        private int rows;
        
        private ArtistInfo select_all_artist = new ArtistInfo(null);
        
        public ArtistListDatabaseModel(BansheeDbConnection connection)
        {
            this.connection = connection;
        }
    
        public override void Reload()
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM CoreArtists";
            rows = Convert.ToInt32(command.ExecuteScalar()) + 1;
            select_all_artist.Name = String.Format("All Artists ({0})", rows - 1);
            OnReloaded();
        }
        
        public override ArtistInfo GetValue(int index)
        {
            if(index == 0) {
                return select_all_artist;
            }
            
            int new_index = index + 1;
            
            if(artists.ContainsKey(new_index)) {
                return artists[new_index];
            }
            
            int fetch_count = 20;
            
            IDbCommand command = connection.CreateCommand();
            command.CommandText = String.Format(@"
                SELECT ArtistID, Name 
                    FROM CoreArtists
                    ORDER BY Name
                    LIMIT {0}, {1}", index - 1, fetch_count);
                
            IDataReader reader = command.ExecuteReader();
			
			int i = new_index;
            
			while(reader.Read()) {
			    if(!artists.ContainsKey(i)) {
			        ArtistInfo artist = new LibraryArtistInfo(reader);
			        artists.Add(i++, artist);
			    }
			}
            
            if(artists.ContainsKey(new_index)) {
                return artists[new_index];
            }
            
            return null;
        }

        public override int Rows { 
            get { return rows; }
        }
    }
}
