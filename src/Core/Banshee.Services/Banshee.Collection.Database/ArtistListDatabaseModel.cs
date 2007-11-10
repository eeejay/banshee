//
// ArtistListDatabaseModel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
        private string track_model_filter;
        private int rows;
        
        private ArtistInfo select_all_artist = new ArtistInfo(null);
        
        public ArtistListDatabaseModel(BansheeDbConnection connection)
        {
            this.connection = connection;
        }

        public ArtistListDatabaseModel(TrackListDatabaseModel trackModel, BansheeDbConnection connection) : this (connection)
        {
            track_model_filter = String.Format (@"
                CoreArtists.ArtistID IN (
                    SELECT DISTINCT(CoreTracks.ArtistID) FROM CoreTracksCache, CoreTracks
                    WHERE CoreTracksCache.TableID = {0} AND CoreTracksCache.ID = CoreTracks.TrackID)",
                trackModel.DbId
            );
        }
    
        public override void Reload()
        {
            lock (this) {
                InvalidateManagedCache();
                IDbCommand command = connection.CreateCommand();
                using (new Timer ("Getting artists count")) {
                command.CommandText = String.Format ("SELECT COUNT(*) FROM CoreArtists {0}", WhereFragment);
                rows = Convert.ToInt32(command.ExecuteScalar()) + 1;
                }
                select_all_artist.Name = String.Format("All Artists ({0})", rows - 1);
            }
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
            
            using (new Timer ("Getting artists")) {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = String.Format(@"
                SELECT CoreArtists.ArtistID, CoreArtists.Name 
                    FROM CoreArtists {0}
                    ORDER BY Name
                    LIMIT {1}, {2}",
                    WhereFragment, index - 1, fetch_count);
                
            IDataReader reader = command.ExecuteReader();
			
			int i = new_index;
            
			while(reader.Read()) {
			    if(!artists.ContainsKey(i)) {
			        ArtistInfo artist = new LibraryArtistInfo(reader);
			        artists.Add(i++, artist);
			    }
			}
            }
            
            if(artists.ContainsKey(new_index)) {
                return artists[new_index];
            }
            
            return null;
        }

        private string WhereFragment {
            get {
                if (track_model_filter == null)
                    return String.Empty;

                return String.Format ("WHERE {0}", track_model_filter);
            }
        }

        private void InvalidateManagedCache()
        {
            artists.Clear();
        }

        public override int Rows { 
            get { return rows; }
        }
    }
}
