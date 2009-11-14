//
// MtpDapTests.cs
//
// Author:
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

#if ENABLE_TESTS

using System;
using NUnit.Framework;

using Hyena;
using Mtp;

using Banshee.Collection;
using Banshee.Dap.Mtp;

namespace Banshee.Dap.Mtp
{
    [TestFixture]
    public class MtpDapTests
    {
        [Test]
        public void MtpToBansheeTrack ()
        {
            Track track = new Track ("foo.mp3", 1000);
            track.Album = "Mtp Album";
            track.Artist = "Mtp Artist";
            track.Title = "Mtp Title";
            track.Duration = (uint) (1000 * 132.2);
            track.Rating = 80;
            track.TrackNumber = 3;

            TrackInfo track_info = new MtpTrackInfo (null, track);
            Assert.AreEqual ("Mtp Artist", track_info.ArtistName);
            Assert.AreEqual ("Mtp Album", track_info.AlbumTitle);
            Assert.AreEqual ("Mtp Title", track_info.TrackTitle);
            Assert.AreEqual (132.2, track_info.Duration.TotalSeconds);
            Assert.AreEqual (4, track_info.Rating);
            Assert.AreEqual (3, track_info.TrackNumber);
            Assert.AreEqual (0, track_info.Year);

            track.Year = 1983;
            track_info = new MtpTrackInfo (null, track);
            Assert.AreEqual (1983, track_info.Year);
        }

        [Test]
        public void BansheeToMtpTrack ()
        {
            TrackInfo track_info = new TrackInfo ();
            track_info.ArtistName = "Banshee Artist";
            track_info.AlbumTitle = "Banshee Album";
            track_info.TrackTitle = "Banshee Title";
            track_info.Year = 2003;
            track_info.Duration = TimeSpan.FromSeconds (3600 * 1.32);
            track_info.Rating = 2;
            track_info.TrackNumber = 13;

            Track track = new Track ("foo.mp3", 1000);
            MtpTrackInfo.ToMtpTrack (track_info, track);

            Assert.AreEqual ("Banshee Artist", track.Artist);
            Assert.AreEqual ("Banshee Album", track.Album);
            Assert.AreEqual ("Banshee Title", track.Title);
            Assert.AreEqual (1000 * 3600 * 1.32, track.Duration);
            Assert.AreEqual (40, track.Rating);
            Assert.AreEqual (13, track.TrackNumber);
            Assert.AreEqual (2003, track.Year);

            //track.ReleaseDate = "00000101T0000.00";
            //track_info = new MtpTrackInfo (track);
            //Assert.AreEqual (0, track_info.Year);
        }
    }
}

#endif
