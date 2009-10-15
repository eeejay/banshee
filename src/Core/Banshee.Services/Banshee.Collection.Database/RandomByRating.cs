//
// RandomByRating.cs
//
// Authors:
//   Elena Grassi <grassi.e@gmail.com>
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Elena Grassi
// Copyright (C) 2009 Alexander Kojevnikov
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

using Banshee.PlaybackController;

namespace Banshee.Collection.Database
{
    public class RandomByRating : RandomBySlot
    {
        private static string track_condition = String.Format ("AND (CoreTracks.Rating = ? OR (? = 3 AND CoreTracks.Rating = 0)) {0} ORDER BY RANDOM()", RANDOM_CONDITION);

        public RandomByRating (Shuffler shuffler) : base (PlaybackShuffleMode.Rating, shuffler)
        {
            Condition = "(CoreTracks.Rating = ? OR (? = 3 AND CoreTracks.Rating = 0))";
            OrderBy = "RANDOM()";
        }

        public override TrackInfo GetPlaybackTrack (DateTime after)
        {
            var track = !IsReady ? null : Cache.GetSingle (track_condition, slot + 1, slot + 1, after, after);
            Reset ();
            return track;
        }

        public override DatabaseTrackInfo GetShufflerTrack (DateTime after)
        {
            if (!IsReady)
                return null;

            var track = GetTrack (ShufflerQuery, slot + 1, slot + 1, after);
            Reset ();
            return track;
        }

        protected override int Slots {
            get { return 5; }
        }

        protected override string PlaybackSlotQuerySql {
            get {
                return @"
                    SELECT
                        (CoreTracks.Rating - 1) AS Slot, COUNT(*)
                    FROM
                        CoreTracks, CoreCache {0}
                    WHERE
                        {1}
                        CoreCache.ModelID = {2} AND
                        CoreTracks.LastStreamError = 0 AND
                        (CoreTracks.LastPlayedStamp < ? OR CoreTracks.LastPlayedStamp IS NULL) AND
                        (CoreTracks.LastSkippedStamp < ? OR CoreTracks.LastSkippedStamp IS NULL)
                        {3}
                    GROUP BY Slot";
            }
        }

        protected override string ShufflerSlotQuerySql {
            get {
                return @"
                    SELECT
                        (CoreTracks.Rating - 1) AS Slot, COUNT(*)
                    FROM
                        CoreTracks LEFT OUTER JOIN CoreShuffles ON (CoreShuffles.ShufflerId = " + Shuffler.DbId.ToString () +
                    @" AND CoreShuffles.TrackID = CoreTracks.TrackID)
                        {0}
                    WHERE
                        CoreTracks.LastStreamError = 0 AND
                        (CoreShuffles.LastShuffledAt < ? OR CoreShuffles.LastShuffledAt IS NULL)
                        {3}
                    GROUP BY Slot";
            }
        }
    }
}
