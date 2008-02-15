//
// UserTopTracks.cs
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
    // <toptracks user="RJ" type="overall">
    // <track>
    // <artist mbid="bc641be9-ca36-4c61-9394-5230433f6646">Liquid Tension Experiment</artist>
    // <name>Three Minute Warning</name>
    // <mbid/>
    // <playcount>40</playcount>
    // <rank>1</rank>
    // <url>http://www.last.fm/music/Liquid+Tension+Experiment/_/Three+Minute+Warning</url>
    // </track>
    public class UserTopTracks : UserTopData<TopTrack>
    {
        public UserTopTracks (string username, TopType type) : base (username, "toptracks.xml", type)
        {
        }
    }

    public class TopTrack : UserTopEntry
    {
        public string Artist            { get { return Get<string>   ("artist"); } }
    }
}
