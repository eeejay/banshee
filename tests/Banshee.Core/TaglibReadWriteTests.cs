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
    /*
    static string [] files, blank_files;
    static string pwd;

    static TaglibReadWriteTests () {
        pwd = Mono.Unix.UnixDirectoryInfo.GetCurrentDirectory ();
        AddinManager.Initialize (pwd + "/../bin/");
        Banshee.Configuration.ConfigurationClient.Initialize ();

        files = new string [] {
            pwd + "/data/test1.ogg",
            // TODO this flac file doesn't have metadata yet
            //"data/test2.flac",
            pwd + "/data/test3.mp3",
        };

        blank_files = new string [] {
            pwd + "/data/no_metadata1.ogg",
            // TODO this flac file doesn't have metadata yet
            //"data/test2.flac",
            pwd + "/data/no_metadata3.mp3",
        };
    }

    [Test]
    public void ReadMetadata ()
    {
        QueryTests.AssertForEach<string> (files, delegate (string uri) {
            VerifyRead (new SafeUri (uri));
        });
    }

    [Test]
    public void UpdateMetadata ()
    {
        WriteMetadata (files, true);
    }

    [Test]
    public void CreateMetadata ()
    {
        WriteMetadata (blank_files, false);
    }

    private static void WriteMetadata (string [] files, bool verify_start_state)
    {
        SafeUri newuri = null;
        bool write_metadata = LibrarySchema.WriteMetadata.Get();
        LibrarySchema.WriteMetadata.Set (true);
        try {
            QueryTests.AssertForEach<string> (files, delegate (string uri) {
                string [] p = uri.Split ('.');
                string extension = p[p.Length - 1];
                newuri = new SafeUri (pwd + "/data/test_write." + extension);

                Banshee.IO.File.Copy (new SafeUri (uri), newuri, true);

                if (verify_start_state) {
                    VerifyRead (newuri);
                }

                ChangeAndVerify (newuri);
            });
        } finally {
            LibrarySchema.WriteMetadata.Set (write_metadata);
            if (newuri != null)
                Banshee.IO.File.Delete (newuri);
        }
    }

#region Utility methods

    private static void VerifyRead (SafeUri uri)
    {
        TagLib.File file = StreamTagger.ProcessUri (uri);
        TrackInfo track = new TrackInfo ();
        StreamTagger.TrackInfoMerge (track, file);

        Assert.AreEqual ("TestTitle", track.TrackTitle);
        Assert.AreEqual ("TestArtist", track.ArtistName);
        Assert.AreEqual ("TestAlbum", track.AlbumTitle);
        Assert.AreEqual ("TestGenre", track.Genre);
        Assert.AreEqual (2, track.TrackNumber);
        Assert.AreEqual (2, track.Disc);
        Assert.AreEqual (2001, track.Year);
    }

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
    */

}
