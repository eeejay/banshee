//
// DatabaseArtistInfoTests.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
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

using NUnit.Framework;
using Banshee.Collection.Database;
using Banshee.Collection;

namespace Banshee.Collection.Database.Tests
{
    [TestFixture]
    public class DatabaseArtistInfoTests
    {
        static DatabaseArtistInfoTests () {
            Banshee.Database.BansheeDatabaseSettings.CheckTables = false;
        }

        protected void AssertNameSort (string name, string name_sort, byte[] expected)
        {
            DatabaseArtistInfo info = new DatabaseArtistInfo ();
            info.Name = name;
            info.NameSort = name_sort;
            Assert.AreEqual (expected, info.NameSortKey);
        }

        protected void AssertNameLowered (string name, string expected)
        {
            DatabaseArtistInfo info = new DatabaseArtistInfo ();
            info.Name = name;
            Assert.AreEqual (expected, info.NameLowered);
        }

        [Test]
        public void TestWithoutNameSortKey ()
        {
            AssertNameSort ("", null, Hyena.StringUtil.SortKey (ArtistInfo.UnknownArtistName));
            AssertNameSort ("a", null, new byte[] {14, 2, 1, 1, 1, 1, 0});
            AssertNameSort ("A", null, new byte[] {14, 2, 1, 1, 1, 1, 0});

            AssertNameSort ("a", "", new byte[] {14, 2, 1, 1, 1, 1, 0});
        }

        [Test]
        public void TestNameSortKey ()
        {
            AssertNameSort ("Title", "a", new byte[] {14, 2, 1, 1, 1, 1, 0});
            AssertNameSort ("Title", "A", new byte[] {14, 2, 1, 1, 1, 1, 0});
        }

        [Test]
        public void TestNameLowered ()
        {
            AssertNameLowered ("", ArtistInfo.UnknownArtistName.ToLower ());
            AssertNameLowered ("A", "a");
            AssertNameLowered ("\u0104", "a");
        }
    }
}

#endif

