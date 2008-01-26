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

using Hyena.Data.Sqlite;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class ArtistListDatabaseModel : ArtistListModel, ICacheableDatabaseModel
    {
        private BansheeDbConnection connection;
        private BansheeModelProvider<LibraryArtistInfo> provider;
        private BansheeModelCache<LibraryArtistInfo> cache;
        private TrackListDatabaseModel track_model;
        private string reload_fragment;
        private int count;
        
        private ArtistInfo select_all_artist = new ArtistInfo(null);
        
        public ArtistListDatabaseModel(BansheeDbConnection connection, string uuid)
        {
            this.connection = connection;
            provider = LibraryArtistInfo.Provider;
            cache = new BansheeModelCache <LibraryArtistInfo> (connection, uuid, this, provider);
        }

        public ArtistListDatabaseModel(TrackListDatabaseModel trackModel, BansheeDbConnection connection, string uuid) : this (connection, uuid)
        {
            this.track_model = trackModel;
        }
    
        private bool first_reload = true;
        public override void Reload()
        {
            if (!first_reload || !cache.Warm) {
                reload_fragment = String.Format (
                    @"FROM CoreArtists {0} ORDER BY Name",
                    track_model != null ? String.Format(@"
                        WHERE CoreArtists.ArtistID IN
                            (SELECT CoreTracks.ArtistID FROM CoreTracks, CoreArtists, CoreCache
                                WHERE CoreCache.ModelID = {0} AND
                                      CoreCache.ItemId = CoreTracks.TrackID AND
                                      CoreArtists.ArtistId = CoreTracks.ArtistID)",
                        track_model.CacheId
                    ) : null
                );

                //Console.WriteLine ("reload fragment for artists is {0}", reload_fragment);

                cache.Reload ();
            }

            first_reload = false;
            count = cache.Count + 1;
            select_all_artist.Name = String.Format("All Artists ({0})", count - 1);
            OnReloaded();
        }
        
        public override ArtistInfo this[int index] {
            get {
                if (index == 0)
                    return select_all_artist;

                return cache.GetValue (index - 1);
            }
        }

        public override int Count { 
            get { return count; }
        }

        // Implement ICacheableModel
        public int FetchCount {
            get { return 20; }
        }
        
        public string ReloadFragment {
            get { return reload_fragment; }
        }
    }
}
