//
// TaglibReadWriteTests.cs
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

using System;
using NUnit.Framework;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Configuration.Schema;

[TestFixture]
public class TaglibReadWriteTests : BansheeTests
{
    static string [] files;

    static TaglibReadWriteTests () {
        files = new string [] {
            Pwd + "/../tests/data/test.mp3",
        };
    }

    [Test]
    public void TestSystemIO ()
    {
        Banshee.IO.Provider.SetProvider (new Banshee.IO.SystemIO.Provider ());
        WriteMetadata (files);
    }

    [Test]
    public void TestUnixIO ()
    {
        Banshee.IO.Provider.SetProvider (new Banshee.IO.Unix.Provider ());
        WriteMetadata (files);
    }

    private static void WriteMetadata (string [] files)
    {
        SafeUri newuri = null;
        bool write_metadata = LibrarySchema.WriteMetadata.Get();
        LibrarySchema.WriteMetadata.Set (true);
        try {
            AssertForEach<string> (files, delegate (string uri) {
                string extension = System.IO.Path.GetExtension (uri);
                newuri = new SafeUri (Pwd + "/../tests/data/test_write." + extension);

                Banshee.IO.File.Copy (new SafeUri (uri), newuri, true);

                ChangeAndVerify (newuri);
            });
        } finally {
            LibrarySchema.WriteMetadata.Set (write_metadata);
            if (newuri != null)
                Banshee.IO.File.Delete (newuri);
        }
    }

#region Utility methods

    private static void ChangeAndVerify (SafeUri uri)
    {
        TagLib.File file = StreamTagger.ProcessUri (uri);
        TrackInfo track = new TrackInfo ();
        StreamTagger.TrackInfoMerge (track, file);

        // Make changes
        track.TrackTitle = "My Title";
        track.ArtistName = "My Artist";
        track.AlbumTitle = "My Album";
        track.Genre = "My Genre";
        track.TrackNumber = 4;
        track.Disc = 4;
        track.Year = 1999;

        // Save changes
        new SaveTrackMetadataJob (track).Run ();

        // Read changes
        file = StreamTagger.ProcessUri (uri);
        track = new TrackInfo ();
        StreamTagger.TrackInfoMerge (track, file);

        // Verify changes
        Assert.AreEqual ("My Title", track.TrackTitle);
        Assert.AreEqual ("My Artist", track.ArtistName);
        Assert.AreEqual ("My Album", track.AlbumTitle);
        Assert.AreEqual ("My Genre", track.Genre);
        Assert.AreEqual (4, track.TrackNumber);
        Assert.AreEqual (4, track.Disc);
        Assert.AreEqual (1999, track.Year);
    }

#endregion

}
