//
// UserTopAlbums.cs
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
    // <topalbums user="RJ" type="overall">
    // <album>
    // <artist mbid="28503ab7-8bf2-4666-a7bd-2644bfc7cb1d">Dream Theater</artist>
    // <name>Images and Words</name>
    // <mbid>f20971f2-c8ad-4d26-91ab-730f6dedafb2</mbid>
    // <playcount>154</playcount>
    // <rank>1</rank>
    // <url>http://www.last.fm/music/Dream+Theater/Images+and+Words</url>
    // </album>
    public class UserTopAlbums : UserTopData<TopAlbum>
    {
        public UserTopAlbums (string username, TopType type) : base (username, "topalbums.xml", type)
        {
        }
    }

    public class TopAlbum : UserTopEntry
    {
        public string Artist            { get { return Get<string>   ("artist"); } }
    }
}
