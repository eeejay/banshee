//
// AlbumListDatabaseModel.cs
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
using System.Text;
using System.Collections.Generic;

using Hyena.Data.Sqlite;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class AlbumListDatabaseModel : AlbumListModel, ICacheableDatabaseModel
    {
        private readonly BansheeModelProvider<LibraryAlbumInfo> provider;
        private readonly BansheeModelCache<LibraryAlbumInfo> cache;
        private readonly TrackListDatabaseModel track_model;
        private int count;
        private string artist_id_filter_query;
        private string reload_fragment;
        
        private readonly AlbumInfo select_all_album = new AlbumInfo(null);
        
        public AlbumListDatabaseModel(BansheeDbConnection connection, string uuid)
        {
            provider = LibraryAlbumInfo.Provider;
            cache = new BansheeModelCache <LibraryAlbumInfo> (connection, uuid, this, provider);
        }

        public AlbumListDatabaseModel(TrackListDatabaseModel trackModel, 
                BansheeDbConnection connection, string uuid) : this (connection, uuid)
        {
            this.track_model = trackModel;
        }

        private bool first_reload = true;
        public override void Reload()
        {
            if (!first_reload || !cache.Warm) {
                bool either = (artist_id_filter_query != null) || (track_model != null);
                bool both = (artist_id_filter_query != null) && (track_model != null);

                reload_fragment = String.Format (@"
                    FROM CoreAlbums INNER JOIN CoreArtists ON CoreAlbums.ArtistID = CoreArtists.ArtistID
                        {0} {1} {2} {3} ORDER BY CoreAlbums.Title, CoreArtists.Name",
                    either ? "WHERE" : null,
                    track_model == null ? null :
                        String.Format (@"
                            CoreAlbums.AlbumID IN
                                (SELECT CoreTracks.AlbumID FROM CoreTracks, CoreCache{1}
                                    WHERE CoreCache.ModelID = {0} AND
                                          CoreCache.ItemId = {2})",
                            track_model.CacheId,
                            track_model.JoinFragment,
                            track_model.JoinTable == null
                                ? "CoreTracks.TrackID"
                                : String.Format ("{0}.{1} AND CoreTracks.TrackID = {0}.{2}", track_model.JoinTable, track_model.JoinPrimaryKey, track_model.JoinColumn)
                            ),
                    both ? "AND" : null,
                    artist_id_filter_query
                );
                //Console.WriteLine ("reload fragment for albums is {0}", reload_fragment);

                cache.Reload ();
            }

            first_reload = false;
            count = cache.Count + 1;
            select_all_album.Title = String.Format("All Albums ({0})", count - 1);
            OnReloaded();
        }
        
        public override AlbumInfo this[int index] {
            get {
                if (index == 0)
                    return select_all_album;

                return cache.GetValue (index - 1);
            }
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
                        Reload();
                    }
                );
            }
        }

        public override int Count { 
            get { return count; }
        }

        // Implement ICacheableModel
        public int FetchCount {
            get { return 20; }
        }

        public string SelectAggregates { get { return null; } }

        //private const string primary_key = "CoreAlbums.AlbumID";

        public string ReloadFragment {
            get { return reload_fragment; }
        }

        public string JoinTable { get { return null; } }
        public string JoinFragment { get { return null; } }
        public string JoinPrimaryKey { get { return null; } }
        public string JoinColumn { get { return null; } }
    }
}
