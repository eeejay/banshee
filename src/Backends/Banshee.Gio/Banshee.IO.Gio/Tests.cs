//
// Tests.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using NUnit.Framework;
using GLib;

using Banshee.Base;

namespace Banshee.IO.Gio
{
    [TestFixture]
    public class GioTests
    {
        private File file = new File ();
        private Directory dir = new Directory ();
        private string tmp_dir = System.IO.Path.Combine (System.Environment.CurrentDirectory, "tmp-gio");
        private SafeUri foo, baz;
        private string woo;

        static GioTests ()
        {
            GLib.GType.Init ();
        }

        [SetUp]
        public void Setup ()
        {
            foo = Uri  ("foo");
            baz = Uri  ("baz");
            woo = Path ("woo");

            System.IO.Directory.CreateDirectory (tmp_dir);
            System.IO.File.WriteAllText (Path ("foo"), "bar");
            System.IO.File.WriteAllText (Path ("baz"), "oof");
            System.IO.Directory.CreateDirectory (Path ("woo"));
        }

        [TearDown]
        public void Teardown ()
        {
            try { System.IO.File.Delete (Path ("foo")); } catch {}
            try { System.IO.File.Delete (Path ("baz")); } catch {}
            try { System.IO.Directory.Delete (woo); } catch {}
            try { System.IO.Directory.Delete (tmp_dir, true); } catch {}
        }

        [Test]
        public void Exists ()
        {
            Assert.IsTrue (file.Exists (foo));
            Assert.IsTrue (file.Exists (baz));
            Assert.IsTrue ( dir.Exists (woo));
        }

        [Test]
        public void DoesntExist ()
        {
            Assert.IsFalse ( dir.Exists (Path ("foo")));
            Assert.IsFalse (file.Exists (Uri ("woo")));
            Assert.IsFalse (file.Exists (Uri ("asd")));
        }

        [Test]
        public void Move ()
        {
            file.Move (foo, Uri ("fooz"));
            Assert.IsTrue  (file.Exists (Uri ("fooz")));
            Assert.IsFalse (file.Exists (foo));

            dir.Move (new SafeUri (woo), Uri ("wooz"));
            Assert.IsTrue  (dir.Exists (Path ("wooz")));
            Assert.IsFalse (dir.Exists (woo));
        }

        [Test]
        public void Create ()
        {
            var newf = Uri ("newfile");
            Assert.IsFalse (file.Exists (newf));
            file.OpenWrite (newf, false).Close ();
            Assert.IsTrue (file.Exists (newf));

            try {
                file.OpenWrite (newf, false).Close ();
                Assert.Fail ("Should have thrown an exception creating already-exists file w/o overwrite");
            } catch {}

            try {
                file.OpenWrite (newf, true).Close ();
            } catch {
                Assert.Fail ("Should not have thrown an exception creating already-exists file w/ overwrite");
            }

            var newd = Path ("newdir");
            Assert.IsFalse (dir.Exists (newd));
            dir.Create (newd);
            Assert.IsTrue (dir.Exists (newd));
        }

        [Test]
        public void DemuxCreateFile ()
        {
            var newf = Uri ("newfile");
            var newp = Path ("newfile");
            Assert.IsFalse (file.Exists (newf));

            var demux = new DemuxVfs (newp);
            Assert.IsTrue (demux.IsWritable);
            Assert.IsTrue (demux.IsReadable);

            var stream = demux.WriteStream;
            Assert.IsTrue (stream.CanWrite);
            stream.WriteByte (0xAB);
            demux.CloseStream (stream);

            Assert.IsTrue (file.Exists (newf));
        }

        [Test]
        public void DemuxOverwriteFile ()
        {
            Assert.IsTrue (file.Exists (foo));
            Assert.AreEqual (3, file.GetSize (foo));

            var demux = new DemuxVfs (foo.AbsoluteUri);
            Assert.IsTrue (demux.IsWritable);
            Assert.IsTrue (demux.IsReadable);
            var stream = demux.WriteStream;
            Assert.IsTrue (stream.CanWrite);

            // Make sure can read from WriteStream - required by TagLib#
            // FIXME - depends on glib 2.22 and new gio# - see gio DemuxVfs.cs
            Assert.AreEqual ((byte)'b', stream.ReadByte (), "Known failure, bug in Gio backend, depends on glib 2.22 for fix");
            stream.Position = 0;

            stream.WriteByte (0xAB);

            demux.CloseStream (stream);
            Assert.IsTrue (file.Exists (foo));
            Assert.AreEqual (1, file.GetSize (foo));
        }

        [Test]
        public void DemuxReadFile ()
        {
            Assert.IsTrue (file.Exists (foo));

            var demux = new DemuxVfs (foo.AbsoluteUri);
            var stream = demux.ReadStream;

            // foo contains 'bar'
            Assert.AreEqual ((byte)'b', stream.ReadByte ());
            Assert.AreEqual ((byte)'a', stream.ReadByte ());
            Assert.AreEqual ((byte)'r', stream.ReadByte ());

            demux.CloseStream (stream);
        }

        [Test]
        public void GetFileProperties ()
        {
            Assert.AreEqual (3, file.GetSize (foo));
            Assert.IsTrue (file.GetModifiedTime (foo) > 0);
        }

        [Test]
        public void Delete ()
        {
            Assert.IsTrue (file.Exists (foo));
            file.Delete (foo);
            Assert.IsFalse (file.Exists (foo));

            Assert.IsTrue (dir.Exists (woo));
            dir.Delete (woo);
            Assert.IsFalse (dir.Exists (woo));
        }

        [Test]
        public void DeleteRecursive ()
        {
            dir.Delete (tmp_dir, true);
        }

        [Test]
        public void DeleteRecursiveWithoutNativeOptimization ()
        {
            Directory.DisableNativeOptimizations = true;
            dir.Delete (tmp_dir, true);
            Directory.DisableNativeOptimizations = false;
        }

        [Test]
        public void GetChildFiles ()
        {
            var files = dir.GetFiles (tmp_dir).ToArray ();
            Assert.AreEqual (files, new string [] { foo.AbsoluteUri, baz.AbsoluteUri });
        }

        [Test]
        public void GetChildDirs ()
        {
            var dirs = dir.GetDirectories (tmp_dir).ToArray ();
            Assert.AreEqual (dirs, new string [] { new SafeUri (woo).AbsoluteUri });
        }

        private SafeUri Uri (string filename)
        {
            return new SafeUri (Path (filename));
        }

        private string Path (string filename)
        {
            return System.IO.Path.Combine (tmp_dir, filename);
        }
    }
}

#endif
