//
// DatabaseArtistListModel.cs
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

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Database;

namespace Banshee.Collection.Database
{
    public class DatabaseArtistListModel : ArtistListModel, ICacheableDatabaseModel
    {
        private readonly BansheeModelProvider<DatabaseArtistInfo> provider;
        private readonly BansheeModelCache<DatabaseArtistInfo> cache;
        private readonly DatabaseTrackListModel track_model;
        private string reload_fragment;
        private long count;
        
        private readonly ArtistInfo select_all_artist = new ArtistInfo(null);
        
        public DatabaseArtistListModel (BansheeDbConnection connection, string uuid)
        {
            provider = DatabaseArtistInfo.Provider;
            cache = new BansheeModelCache <DatabaseArtistInfo> (connection, uuid, this, provider);
            cache.HasSelectAllItem = true;

            Selection.Changed += HandleSelectionChanged;
        }

        public DatabaseArtistListModel(DatabaseTrackListModel trackModel, BansheeDbConnection connection, string uuid) : this (connection, uuid)
        {
            this.track_model = trackModel;
        }

        private void HandleSelectionChanged (object sender, EventArgs args)
        {
            track_model.Reload (ReloadTrigger.ArtistFilter);
        }

        public override void Reload ()
        {
            Reload (true);
        }
    
        internal void Reload (bool notify)
        {
            reload_fragment = String.Format (
                "FROM CoreArtists {0} ORDER BY NameLowered",
                track_model == null ? null : String.Format (@"
                    WHERE CoreArtists.ArtistID IN
                        (SELECT CoreTracks.ArtistID FROM CoreTracks, CoreCache{1}
                            WHERE CoreCache.ModelID = {0} AND
                                  CoreCache.ItemID = {2})",
                    track_model.CacheId,
                    track_model.CachesJoinTableEntries ? track_model.JoinFragment : null,
                    (!track_model.CachesJoinTableEntries)
                        ? "CoreTracks.TrackID"
                        : String.Format ("{0}.{1} AND CoreTracks.TrackID = {0}.{2}", track_model.JoinTable, track_model.JoinPrimaryKey, track_model.JoinColumn)
                )
            );

            cache.SaveSelection ();
            cache.Reload ();
            cache.UpdateAggregates ();
            cache.RestoreSelection ();

            count = cache.Count + 1;
            select_all_artist.Name = String.Format("All Artists ({0})", count - 1);

            if (notify)
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
            get { return (int) count; }
        }

        public int CacheId {
            get { return (int) cache.CacheId; }
        }

        public void ClearCache ()
        {
            cache.Clear ();
        }

        // Implement ICacheableModel
        public int FetchCount {
            get { return 20; }
        }

        public string SelectAggregates { get { return null; } }
        
        public string ReloadFragment {
            get { return reload_fragment; }
        }

        public string JoinTable { get { return null; } }
        public string JoinFragment { get { return null; } }
        public string JoinPrimaryKey { get { return null; } }
        public string JoinColumn { get { return null; } }
        public bool CachesJoinTableEntries { get { return false; } }
    }
}
