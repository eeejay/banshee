using System;
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;

using Banshee.Base;
using Banshee.Playlists.Formats;

namespace Banshee.Playlists.Formats.Tests
{
    [TestFixture]
    public class PlaylistFormatsTest
    {

#region Setup

        private static Uri BaseUri = new Uri("/iamyourbase/");
        private List<Dictionary<string, object>> elements = new List<Dictionary<string, object>>();

        [TestFixtureSetUp]
        public void Init()
        {
            IPlaylistFormat playlist = LoadPlaylist(new M3uPlaylistFormat(), "extended.m3u");            
            foreach(Dictionary<string, object> element in playlist.Elements) {
                elements.Add(element);
            }
        }

#endregion

#region Tests

        [Test]
        public void ReadAsxSimple()
        {
            LoadTest(new AsxPlaylistFormat(), "simple.asx");
        }

        [Test]
        public void ReadAsxExtended()
        {
            LoadTest(new AsxPlaylistFormat(), "extended.asx");
        }

        [Test]
        public void ReadM3uSimple()
        {
            LoadTest(new M3uPlaylistFormat(), "simple.m3u");
        }

        [Test]
        public void ReadM3uExtended()
        {
            LoadTest(new M3uPlaylistFormat(), "extended.m3u");
        }

        [Test]
        public void ReadPlsSimple()
        {
            LoadTest(new PlsPlaylistFormat(), "simple.pls");
        }

        [Test]
        public void ReadPlsExtended()
        {
            LoadTest(new PlsPlaylistFormat(), "extended.pls");
        }

        [Test]
        public void ReadDetectMagic()
        {
            PlaylistParser parser = new PlaylistParser();
            parser.BaseUri = BaseUri;

            foreach(string path in Directory.GetFiles("playlist-data")) {
                parser.Parse(new SafeUri(Path.Combine(Environment.CurrentDirectory, path)));
                AssertTest(parser.Elements);
            }

            parser.Parse(new SafeUri("http://banshee-project.org/files/tests/extended.pls"));
            AssertTest(parser.Elements);
        }

#endregion

#region Utilities

        private IPlaylistFormat LoadPlaylist(IPlaylistFormat playlist, string filename)
        {
            playlist.BaseUri = BaseUri;
            playlist.Load(File.OpenRead(Path.Combine("playlist-data", filename)), true);
            return playlist;
        }

        private void LoadTest(IPlaylistFormat playlist, string filename)
        {
            LoadPlaylist(playlist, filename);
            AssertTest(playlist.Elements);
        }

        private void AssertTest(List<Dictionary<string, object>> plelements)
        {
            int i = 0;
            foreach(Dictionary<string, object> element in plelements) {
                Assert.AreEqual((Uri)elements[i]["uri"], (Uri)element["uri"]);
                if(element.ContainsKey("title")) {
                    Assert.AreEqual((string)elements[i]["title"], (string)element["title"]);
                }

                if(element.ContainsKey("duration")) {
                    Assert.AreEqual((TimeSpan)elements[i]["duration"], (TimeSpan)element["duration"]);
                }

                i++;
            }
        }

#endregion

    }
}