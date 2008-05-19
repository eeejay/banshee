//
// XspfTests.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006 Novell, Inc.
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
using System.IO;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;

using Media.Playlists.Xspf;

namespace Media.Playlists.Xspf.Tests
{
    [TestFixture]
    public class XspfTest
    {
        private const string complete_path = "../tests/Mono.Media/Xspf/complete.xml";
        private const string xsd_path = "../tests/Mono.Media/Xspf/xspf-1.xsd";

        [Test]
        public void Load()
        {
            Playlist playlist = new Playlist();
            playlist.Load(complete_path);
            Helper.TestPlaylist(playlist);
        }

        [Test]
        public void Validate()
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            XmlSchemaSet schema_set = new XmlSchemaSet();
            schema_set.Add("http://xspf.org/ns/0/", xsd_path);
            settings.Schemas.Add(schema_set);
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            XmlReader reader = XmlReader.Create(complete_path, settings);
            while(reader.Read());
        }
    }

    public static class Helper
    {
        public static void TestPlaylist(Playlist playlist)
        {
            Assert.AreEqual("Playlist Title", playlist.Title);
            Assert.AreEqual("Aaron Bockover", playlist.Creator);
            Assert.AreEqual("Comment", playlist.Annotation);

            Uri uri = new Uri("http://abock.org/");

            Assert.AreEqual(uri, playlist.Location);
            Assert.AreEqual(uri, playlist.Identifier);
            Assert.AreEqual(uri, playlist.Image);
            Assert.AreEqual(uri, playlist.License);

            Assert.AreEqual(W3CDateTime.Parse("2005-01-08T17:10:47-05:00").LocalTime, playlist.Date);

            Assert.AreEqual(2, playlist.Meta.Count);
            foreach(MetaEntry meta in playlist.Meta) {
                Assert.AreEqual(new Uri("http://abock.org/fruit"), meta.Rel);
                if(meta.Value != "Apples" && meta.Value != "Oranges") {
                    Assert.Fail("Expected one of 'Apples' or 'Oranges'");
                }
            }

            Assert.AreEqual(2, playlist.Links.Count);
            foreach(LinkEntry link in playlist.Links) {
                if(!link.Rel.AbsoluteUri.StartsWith("http://abock.org")) {
                    Assert.Fail("Incorrect rel, expected it to start with http://abock.org");
                }

                if(!link.Value.AbsoluteUri.StartsWith("http://abock.org")) {
                    Assert.Fail("Incorrect content, expected it to start with http://abock.org");
                }
            }

            Assert.AreEqual(1, playlist.Tracks.Count);

            Track track = playlist.Tracks[0];
            Assert.AreEqual("Track 1", track.Title);
            Assert.AreEqual("Aaron Bockover", track.Creator);
            Assert.AreEqual("Comment", track.Annotation);
            Assert.AreEqual("Album", track.Album);

            Assert.AreEqual(uri, track.Info);
            Assert.AreEqual(uri, track.Image);
            
            Assert.AreEqual(11, track.TrackNumber);
            Assert.AreEqual(TimeSpan.FromMilliseconds(5159), track.Duration);

            Assert.AreEqual(2, track.Locations.Count);
        }
    }
}

#endif
