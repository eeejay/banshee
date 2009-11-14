//
// LastfmAlbumData.cs
//
// Author:
//   Peter de Kraker <peterdk.dev@umito.nl>
//
// Based on LastfmArtistData.cs
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
using Hyena;

using System.Collections.Generic;

namespace Lastfm.Data
{
    public class LastfmAlbumData
    {
        protected Dictionary <string, object> cache = new Dictionary <string, object> ();

        protected string artist;
        public string Artist {
            get { return artist; }
        }

        protected string album;
        public string Album {
            get { return album; }
        }

        public LastfmAlbumData (string artist, string album)
        {
            this.artist = artist;
            this.album = album;

            //Return Exception if the album is not found on Lastfm.
            try {
                if (AlbumData != null) {
                    return;
                }
                } catch { throw; }
        }


        protected LastfmData<T> Get<T> (string fragment) where T : DataEntry
        {
            return Get<T> (fragment, null);
        }

        protected LastfmData<T> Get<T> (string fragment, string xpath) where T : DataEntry
        {
            //using cacheKey because the public methods all use the same fragment but with a different xpath.
            string cacheKey = fragment + xpath;

            if (cache.ContainsKey (cacheKey)) {
                return (LastfmData<T>) cache [cacheKey];
            }

            LastfmData<T> obj = new LastfmData<T> (String.Format ("album/{0}/{1}/{2}", artist, album, fragment), xpath);
            cache [cacheKey] = obj;
            return obj;
        }


#region Public Properties
//      All these methods use the same fragment, but with a different xpath. This because the info.xml contains lots of different stuff.
//      Couldn't figure out how to process it otherwise.

        public AlbumData AlbumData {
            // We don't need the array, since there is only 1 set of albumdata for any album. Therefore "[0]".
            get { return (Get<AlbumData> ("info.xml", "/album"))[0]; }
        }

        public LastfmData<AlbumTrack> AlbumTracks {
            get { return Get<AlbumTrack> ("info.xml", "/album/tracks/track"); }
        }

        public AlbumCoverUrls AlbumCoverUrls {
            // We don't need the array, since there is only 1 set of covers for any album. Therefore "[0]".
            get { return (Get<AlbumCoverUrls> ("info.xml", "/album/coverart"))[0]; }
        }

#endregion

    }
}

