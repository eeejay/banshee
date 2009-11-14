//
// NamingUtil.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;

using Banshee.Collection;

namespace Banshee.Base
{
    public static class NamingUtil
    {
        public delegate bool PostfixDuplicateIncrementHandler (string check);

        public static string GenerateTrackCollectionName (IEnumerable tracks, string fallback)
        {
            Dictionary<string, int> weight_map = new Dictionary<string, int> ();

            if (tracks == null) {
                return fallback;
            }

            foreach (TrackInfo track in tracks) {
                string artist = null;
                string album = null;

                if (track.ArtistName != null) {
                    artist = track.ArtistName.Trim ();
                    if (artist == String.Empty) {
                        artist = null;
                    }
                }

                if (track.AlbumTitle != null) {
                    album = track.AlbumTitle.Trim ();
                    if (album == String.Empty) {
                        album = null;
                    }
                }

                if (artist != null && album != null) {
                    IncrementCandidate (weight_map, "\0" + artist + " - " + album);
                    IncrementCandidate (weight_map, artist);
                    IncrementCandidate (weight_map, album);
                } else if (artist != null) {
                    IncrementCandidate (weight_map, artist);
                } else if (album != null) {
                    IncrementCandidate (weight_map, album);
                }
            }

            int max_hit_count = 0;
            string max_candidate = fallback;

            List<string> sorted_keys = new List<string> (weight_map.Keys);
            sorted_keys.Sort ();

            foreach (string candidate in sorted_keys) {
                int current_hit_count = weight_map[candidate];
                if (current_hit_count > max_hit_count) {
                    max_hit_count = current_hit_count;
                    max_candidate = candidate;
                }
            }

            if (max_candidate[0] == '\0') {
                return max_candidate.Substring (1);
            }

            return max_candidate;
        }

        private static void IncrementCandidate (Dictionary<string, int> map, string hit)
        {
            if (map.ContainsKey (hit)) {
                map[hit]++;
            } else {
                map.Add (hit, 1);
            }
        }

        public static string PostfixDuplicate (string prefix, PostfixDuplicateIncrementHandler duplicateHandler)
        {
            if (duplicateHandler == null) {
                throw new ArgumentNullException ("A PostfixDuplicateIncrementHandler delegate must be given");
            }

            string name = prefix;
            for (int i = 1; true; i++) {
                if (!duplicateHandler (name)) {
                    return name;
                }

                name = prefix + " " + i;
            }
        }
    }
}
