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

#if ENABLE_TESTS

using System;
using System.Reflection;
using NUnit.Framework;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Streaming;
using Banshee.Configuration.Schema;

using Hyena.Tests;

namespace Banshee.Base.Tests
{
    // FIXME: These tests don't really belong here
    
    [TestFixture]
    public class TaglibReadWriteTests : TestBase
    {
        private string [] files;
        
        [TestFixtureSetUp]
        public void Setup ()
        {
            files = new string [] {
                BinDir + "/../tests/data/test.mp3",
            };
        }
    
        [Test]
        public void TestSystemIO ()
        {
            Banshee.IO.Provider.SetProvider (new Banshee.IO.SystemIO.Provider ());
            WriteMetadata (files, "My Genre");
        }
    
        [Test]
        public void TestUnixIO ()
        {
            Banshee.IO.Provider.SetProvider (CreateUnixIOProvider ());
            WriteMetadata (files, "My Genre");
        }
    
        [Test]
        public void TestNullGenreBug ()
        {
            // Bug in taglib-sharp-2.0.3.0: Crash if you send it a genre of "{ null }" on
            // a song with both ID3v1 and ID3v2 metadata. It's happy with "{}", though.
            // (see http://forum.taglib-sharp.com/viewtopic.php?f=5&t=239 )
            // This tests our workaround.
            Banshee.IO.Provider.SetProvider (CreateUnixIOProvider ());
            WriteMetadata (files, null);
        }
    
        private void WriteMetadata (string [] files, string genre)
        {
            SafeUri newuri = null;
            bool write_metadata = LibrarySchema.WriteMetadata.Get();
            LibrarySchema.WriteMetadata.Set (true);
            try {
                AssertForEach<string> (files, delegate (string uri) {
                    string extension = System.IO.Path.GetExtension (uri);
                    newuri = new SafeUri (BinDir + "/../tests/data/test_write." + extension);
    
                    Banshee.IO.File.Copy (new SafeUri (uri), newuri, true);
    
                    ChangeAndVerify (newuri, genre);
                });
            } finally {
                LibrarySchema.WriteMetadata.Set (write_metadata);
                if (newuri != null)
                    Banshee.IO.File.Delete (newuri);
            }
        }
    
        private void ChangeAndVerify (SafeUri uri, string genre)
        {
            TagLib.File file = StreamTagger.ProcessUri (uri);
            TrackInfo track = new TrackInfo ();
            StreamTagger.TrackInfoMerge (track, file);
    
            // Make changes
            track.TrackTitle = "My Title";
            track.ArtistName = "My Artist";
            track.AlbumTitle = "My Album";
            track.Genre = genre;
            track.TrackNumber = 4;
            track.DiscNumber = 4;
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
            Assert.AreEqual (genre, track.Genre);
            Assert.AreEqual (4, track.TrackNumber);
            Assert.AreEqual (4, track.DiscNumber);
            Assert.AreEqual (1999, track.Year);
        }
        
        private Type unix_io_type;
    
        private Banshee.IO.IProvider CreateUnixIOProvider ()
        {
            if (unix_io_type == null) {
                Assembly asm = Assembly.LoadFrom (BinDir + "/Banshee.Unix.dll");
                unix_io_type = asm.GetType ("Banshee.IO.Unix.Provider");
            }
            
            return (Banshee.IO.IProvider)Activator.CreateInstance (unix_io_type);
        }
    }
}

#endif
