//
// Shuffler.cs
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
using System.Linq;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.ServiceStack;
using Banshee.PlaybackController;

namespace Banshee.Collection.Database
{
    public class Shuffler
    {
        public static readonly Shuffler Playback = new Shuffler () { Id = "playback", DbId = 0 };

        private DateTime random_began_at = DateTime.MinValue;
        private DateTime last_random = DateTime.MinValue;
        private RandomBy [] randoms;
        private DatabaseTrackListModel model;

        public string Id { get; private set; }
        public int DbId { get; private set; }

        private Shuffler ()
        {
            randoms = new RandomBy [] {
                new RandomByTrack (this), new RandomByArtist (this), new RandomByAlbum (this), new RandomByRating (this), new RandomByScore (this)
            };
        }

        public Shuffler (string id) : this ()
        {
            Id = id;
            LoadOrCreate ();
        }

        public void SetModelAndCache (DatabaseTrackListModel model, IDatabaseTrackModelCache cache)
        {
            this.model = model;

            foreach (var random in randoms) {
                random.SetModelAndCache (model, cache);
            }
        }

        public TrackInfo GetRandom (DateTime notPlayedSince, PlaybackShuffleMode mode, bool repeat, bool resetSinceTime)
        {
            lock (this) {
                if (this == Playback) {
                    if (model.Count == 0) {
                        return null;
                    }
                } else {
                    if (model.UnfilteredCount == 0) {
                        return null;
                    }
                }

                if (random_began_at < notPlayedSince) {
                    random_began_at = last_random = notPlayedSince;
                }

                TrackInfo track = GetRandomTrack (mode, repeat, resetSinceTime);
                if (track == null && (repeat || mode != PlaybackShuffleMode.Linear)) {
                    random_began_at = (random_began_at == last_random) ? DateTime.Now : last_random;
                    track = GetRandomTrack (mode, repeat, true);
                }

                last_random = DateTime.Now;
                return track;
            }
        }

        private TrackInfo GetRandomTrack (PlaybackShuffleMode mode, bool repeat, bool resetSinceTime)
        {
            foreach (var r in randoms) {
                if (resetSinceTime || r.Mode != mode) {
                    r.Reset ();
                }
            }
            
            var random = randoms.First (r => r.Mode == mode);
            if (random != null) {
                if (!random.IsReady) {
                    if (!random.Next (random_began_at) && repeat) {
                        random_began_at = last_random;
                        random.Next (random_began_at);
                    }
                }

                if (random.IsReady) {
                    return random.GetTrack (random_began_at);
                }
            }

            return null;
        }

        private void LoadOrCreate ()
        {
            var db = ServiceManager.DbConnection;

            int res = db.Query<int> ("SELECT ShufflerID FROM CoreShufflers WHERE ID = ?", Id);
            if (res > 0) {
                DbId = res;
            } else {
                DbId = db.Execute ("INSERT INTO CoreShufflers (ID) VALUES (?)", Id);
            }
        }
    }
}
