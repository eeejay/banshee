//
// FileNamePatternTests.cs
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

#if ENABLE_TESTS

using System;
using NUnit.Framework;

using Banshee.Base;
using Banshee.Collection;

namespace Banshee.Base.Tests
{
    [TestFixture]
    public class FileNamePatternTest
    {
        private static string ZeroPad(int num)
        {
            string str = Convert.ToString(num);
            return num < 10 ? "0" + str : str;
        }

        [Test]
        public void MakePathsRelative ()
        {
            Assert.AreEqual ("baz", Paths.MakePathRelative ("/foo/bar/baz", "/foo/bar"));
            Assert.AreEqual ("baz", Paths.MakePathRelative ("/foo/bar/baz", "/foo/bar/"));
            Assert.AreEqual ("",    Paths.MakePathRelative ("/foo/bar/baz", "/foo/bar/baz"));
            Assert.AreEqual (null,  Paths.MakePathRelative ("/foo/bar/baz", "foo"));
            Assert.AreEqual (null,  Paths.MakePathRelative ("/fo", "/foo"));
        }
    
        [Test]
        public void CreateFromTrackInfo()
        {
            SampleTrackInfo track = new SampleTrackInfo();
            string built = FileNamePattern.CreateFromTrackInfo(
                "%artist%:%album%:%title%:%track_number%:" + 
                "%track_count%:%track_number_nz%:%track_count_nz%",
                track);
    
            Assert.AreEqual(String.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}",
                track.ArtistName, track.AlbumTitle, track.TrackTitle, 
                ZeroPad(track.TrackNumber), ZeroPad(track.TrackCount),
                track.TrackNumber, track.TrackCount),
                built);
        }
    }
}

#endif
