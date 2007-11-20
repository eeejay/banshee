using System;
using NUnit.Framework;

using Banshee.Base;
using Banshee.Collection;

[TestFixture]
public class FileNamePatternTest
{
    private static string ZeroPad(int num)
    {
        string str = Convert.ToString(num);
        return num < 10 ? "0" + str : str;
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

    [Test]
    public void Escape()
    {
        Assert.AreEqual("_ _ _ _ _ _ _", 
            FileNamePattern.Escape("/ \\ $ % ? * :"));
    }
}

