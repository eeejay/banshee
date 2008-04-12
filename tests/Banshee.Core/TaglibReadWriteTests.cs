using System;
using NUnit.Framework;

using Mono.Addins;
using Banshee.Base;
using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Configuration.Schema;

[TestFixture]
public class TaglibReadWriteTests
{
    static string [] files;
    static string pwd;

    static TaglibReadWriteTests () {
        Hyena.Log.Debugging = true;

        pwd = Mono.Unix.UnixDirectoryInfo.GetCurrentDirectory ();
        AddinManager.Initialize (pwd + "/../bin/");
        Banshee.Configuration.ConfigurationClient.Initialize ();

        files = new string [] {
            pwd + "/data/test.mp3",
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
            QueryTests.AssertForEach<string> (files, delegate (string uri) {
                string extension = System.IO.Path.GetExtension (uri);
                newuri = new SafeUri (pwd + "/data/test_write." + extension);

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
