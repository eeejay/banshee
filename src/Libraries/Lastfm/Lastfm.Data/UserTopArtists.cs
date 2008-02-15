//
// UserTopArtists.cs
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

namespace Lastfm.Data
{
    // <topartists user="RJ" type="overall">
    // <artist>
    // <name>Dream Theater</name>
    // <mbid>28503ab7-8bf2-4666-a7bd-2644bfc7cb1d</mbid>
    // <playcount>1211</playcount>
    // <rank>1</rank>
    // <url>http://www.last.fm/music/Dream+Theater</url>
    // <thumbnail>http://userserve-ak.last.fm/serve/50/235557.jpg</thumbnail>
    // <image>http://userserve-ak.last.fm/serve/160/235557.jpg</image>
    // </artist>
    // </topartists>
    public class UserTopArtists : UserTopData<TopArtist>
    {
        public UserTopArtists (string username, TopType type) : base (username, "topartists.xml", type)
        {
        }
    }

    public class TopArtist : UserTopEntry
    {
        public string ThumbnailUrl      { get { return Get<string>   ("thumbnail"); } }
        public string ImageUrl          { get { return Get<string>   ("image"); } }
    }
}
