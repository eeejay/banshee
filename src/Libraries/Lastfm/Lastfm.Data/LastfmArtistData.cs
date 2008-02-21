//
// LastfmArtistData.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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

using System.Collections.Generic;

namespace Lastfm.Data
{
    public class LastfmArtistData
    {
        protected Dictionary <string, object> cache = new Dictionary <string, object> ();

        protected string name;
        public string Name {
            get { return name; }
        }

        public LastfmArtistData (string name)
        {
            this.name = name;
        }

        protected LastfmData<T> Get<T> (string fragment) where T : DataEntry
        {
            return Get<T> (fragment, null);
        }

        protected LastfmData<T> Get<T> (string fragment, string xpath) where T : DataEntry
        {
            if (cache.ContainsKey (fragment)) {
                return (LastfmData<T>) cache [fragment];
            }

            LastfmData<T> obj = new LastfmData<T> (String.Format ("artist/{0}/{1}", name, fragment), xpath);
            cache [fragment] = obj;
            return obj;
        }

#region Public Properties

        public LastfmData<SimilarArtist> SimilarArtists {
            get { return Get<SimilarArtist> ("similar.xml"); }
        }

        // ?showtracks=1
        public LastfmData<ArtistFan> Fans {
            get { return Get<ArtistFan> ("fans.xml"); }
        }

        public LastfmData<ArtistTopTrack> TopTracks {
            get { return Get<ArtistTopTrack> ("toptracks.xml"); }
        }

        public LastfmData<ArtistTopAlbum> TopAlbums {
            get { return Get<ArtistTopAlbum> ("topalbums.xml"); }
        }

        public LastfmData<TopTag> TopTags {
            get { return Get<TopTag> ("toptags.xml"); }
        }

        public LastfmData<EventEntry> CurrentEvents {
            get { return Get<EventEntry> ("events.rss"); }
        }

#endregion

    }
}

