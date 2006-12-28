using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using NUnit.Framework;

using Banshee.Playlists.Formats.Xspf;

namespace Banshee.Playlists.Formats.Xspf.Tests
{
	[TestFixture]
	public class XspfTest
	{
		[Test]
		public void Load()
		{
			Playlist playlist = new Playlist();
			playlist.Load("Xspf/complete.xml");
			Helper.TestPlaylist(playlist);
		}

		[Test]
		public void Validate()
		{
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.ValidationType = ValidationType.Schema;
			XmlSchemaSet schema_set = new XmlSchemaSet();
			schema_set.Add("http://xspf.org/ns/0/", "Xspf/xspf-1.xsd");
			settings.Schemas.Add(schema_set);
			settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
			XmlReader reader = XmlReader.Create("Xspf/complete.xml", settings);
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
			foreach(Meta meta in playlist.Meta) {
				Assert.AreEqual(new Uri("http://abock.org/fruit"), meta.Rel);
				if(meta.Value != "Apples" && meta.Value != "Oranges") {
					Assert.Fail("Expected one of 'Apples' or 'Oranges'");
				}
			}

			Assert.AreEqual(2, playlist.Links.Count);
			foreach(Link link in playlist.Links) {
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

