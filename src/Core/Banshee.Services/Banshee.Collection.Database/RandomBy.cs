//
// RandomBy.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.PlaybackController;

namespace Banshee.Collection.Database
{
    public abstract class RandomBy
    {
        protected const string RANDOM_CONDITION = "AND LastStreamError = 0 AND (LastPlayedStamp < ? OR LastPlayedStamp IS NULL) AND (LastSkippedStamp < ? OR LastSkippedStamp IS NULL)";

        protected DatabaseTrackListModel Model { get; private set; }
        protected IDatabaseTrackModelCache Cache { get; private set; }

        private HyenaSqliteCommand insert_shuffle;

        protected Shuffler Shuffler { get; private set; }

        public virtual bool IsReady { get { return true; } }
        public PlaybackShuffleMode Mode { get; private set; }

        protected string Condition { get; set; }
        protected string OrderBy { get; set; }

        public RandomBy (PlaybackShuffleMode mode, Shuffler shuffler)
        {
            Shuffler = shuffler;
            Mode = mode;
            insert_shuffle = new HyenaSqliteCommand ("INSERT OR REPLACE INTO CoreShuffles (ShufflerID, TrackID, LastShuffledAt) VALUES (?, ?, ?)");
        }

        private HyenaSqliteCommand shuffler_query;
        protected HyenaSqliteCommand ShufflerQuery {
            get {
                if (shuffler_query == null) {
                    var provider = DatabaseTrackInfo.Provider;
                    shuffler_query = new HyenaSqliteCommand (String.Format (@"
                        SELECT {0}
                            FROM {1} LEFT OUTER JOIN CoreShuffles ON (CoreShuffles.ShufflerId = {2} AND CoreShuffles.TrackID = CoreTracks.TrackID)
                            WHERE {3} {4} AND {5} AND
                                LastStreamError = 0 AND (CoreShuffles.LastShuffledAt < ? OR CoreShuffles.LastShuffledAt IS NULL)
                            ORDER BY {6}",
                        provider.Select,
                        Model.FromFragment, Shuffler.DbId,
                        String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where, Model.ConditionFragment ?? "1=1", Condition,
                        OrderBy
                    ));
                }

                return shuffler_query;
            }
        }

        public void SetModelAndCache (DatabaseTrackListModel model, IDatabaseTrackModelCache cache)
        {
            if (Model != model) {
                Model = model;
                Cache = cache;
                Reset ();

                OnModelAndCacheUpdated ();
            }

            shuffler_query = null;
        }

        protected virtual void OnModelAndCacheUpdated ()
        {
        }

        public virtual void Reset () {}

        public abstract bool Next (DateTime after);

        public TrackInfo GetTrack (DateTime after)
        {
            if (Shuffler == Shuffler.Playback) {
                return GetPlaybackTrack (after);
            } else {
                var track = GetShufflerTrack (after);

                // Record this shuffle
                if (track != null) {
                    ServiceManager.DbConnection.Execute (insert_shuffle, Shuffler.DbId, track.TrackId, DateTime.Now);
                }

                return track;
            }
        }

        public abstract TrackInfo GetPlaybackTrack (DateTime after);
        public abstract DatabaseTrackInfo GetShufflerTrack (DateTime after);

        protected DatabaseTrackInfo GetTrack (HyenaSqliteCommand cmd, params object [] args)
        {
            using (var reader = ServiceManager.DbConnection.Query (cmd, args)) {
                if (reader.Read ()) {
                    return DatabaseTrackInfo.Provider.Load (reader);
                }
            }

            return null;
        }
    }
}
