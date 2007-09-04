//
// AlbumListDatabaseModel.cs
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
    public class AlbumListDatabaseModel : AlbumListModel
    {
        private Dictionary<int, AlbumInfo> albums = new Dictionary<int, AlbumInfo>();
        private BansheeDbConnection connection;
        private int rows;
        private string artist_id_filter_query;
        
        private AlbumInfo select_all_album = new AlbumInfo(null);
        
        public AlbumListDatabaseModel(BansheeDbConnection connection)
        {
            this.connection = connection;
        }
    
        public override void Reload()
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(AlbumID) FROM CoreAlbums";
            if(artist_id_filter_query != null) {
                command.CommandText = String.Format("{0} INNER JOIN CoreArtists ON CoreArtists.ArtistID = CoreAlbums.ArtistID WHERE {1}", command.CommandText, artist_id_filter_query);
            }
            Console.WriteLine(StringExtensions.Flatten(command.CommandText));
            rows = Convert.ToInt32(command.ExecuteScalar()) + 1;
            Console.WriteLine(rows);
            select_all_album.Title = String.Format("All Albums ({0})", rows - 1);
            OnReloaded();
        }
        
        public override AlbumInfo GetValue(int index)
        {
            if(index == 0) {
                return select_all_album;
            }
            
            int new_index = index + 1;
            
            if(albums.ContainsKey(new_index)) {
                return albums[new_index];
            }
            
            int fetch_count = 20;
            
            string filter_query = null;
            
            if(artist_id_filter_query != null) {
                filter_query = String.Format(" WHERE {0} ", artist_id_filter_query);
            }
         
            IDbCommand command = connection.CreateCommand();
            command.CommandText = String.Format(@"
                SELECT AlbumID, Title, CoreArtists.Name
                    FROM CoreAlbums
                    INNER JOIN CoreArtists
                        ON CoreArtists.ArtistID = CoreAlbums.ArtistID
                    {0}
                    ORDER BY Title
                    LIMIT {1}, {2}", filter_query, index - 1, fetch_count);

            Console.WriteLine(StringExtensions.Flatten(command.CommandText));
            IDataReader reader = command.ExecuteReader();
			
			int i = new_index;
            
			while(reader.Read()) {
			    if(!albums.ContainsKey(i)) {
			        AlbumInfo album = new LibraryAlbumInfo(reader);
			        albums.Add(i++, album);
			    }
			}
            
            if(albums.ContainsKey(new_index)) {
                return albums[new_index];
            }
            
            return null;
        }
        
        public override IEnumerable<ArtistInfo> ArtistInfoFilter {
            set { 
                ModelHelper.BuildIdFilter<ArtistInfo>(value, "CoreAlbums.ArtistID", artist_id_filter_query,
                    delegate(ArtistInfo artist) {
                        if(!(artist is LibraryArtistInfo)) {
                            return null;
                        }
                        
                        return ((LibraryArtistInfo)artist).DbId.ToString();
                    },
                
                    delegate(string new_filter) {
                        artist_id_filter_query = new_filter;
                        albums.Clear();
                        Reload();
                    }
                );
            }
        }

        public override int Rows { 
            get { return rows; }
        }
    }
}
