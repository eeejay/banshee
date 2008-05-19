//
// QueryTests.cs
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

using Hyena.Query;

namespace Hyena.Query.Tests
{
    [TestFixture]
    public class QueryTests : Hyena.Tests.TestBase
    {
        [Test]
        public void QueryValueSql ()
        {
            QueryValue qv;
            
            qv = new DateQueryValue (); qv.ParseUserQuery ("2007-03-9");
            Assert.AreEqual (new DateTime (2007, 3, 9), qv.Value);
            Assert.AreEqual ("2007-03-09", qv.ToUserQuery ());
            Assert.AreEqual ("1173420000", qv.ToSql ());
    
            qv = new StringQueryValue (); qv.ParseUserQuery ("foo 'bar'");
            Assert.AreEqual ("foo 'bar'", qv.Value);
            Assert.AreEqual ("foo 'bar'", qv.ToUserQuery ());
            Assert.AreEqual ("foo ''bar''", qv.ToSql ());
    
            qv = new IntegerQueryValue (); qv.ParseUserQuery ("22");
            Assert.AreEqual (22, qv.Value);
            Assert.AreEqual ("22", qv.ToUserQuery ());
            Assert.AreEqual ("22", qv.ToSql ());
    
            qv = new FileSizeQueryValue (); qv.ParseUserQuery ("2048 KB");
            Assert.AreEqual (2097152, qv.Value);
            Assert.AreEqual ("2.048 KB", qv.ToUserQuery ());
            Assert.AreEqual ("2097152", qv.ToSql ());
    
            // TODO this will break once an it_IT translation for "days ago" etc is committed
            qv = new RelativeTimeSpanQueryValue (); qv.ParseUserQuery ("2 days ago");
            Assert.AreEqual (-172800, qv.Value);
            Assert.AreEqual ("2 days ago", qv.ToUserQuery ());
    
            // TODO this will break once an it_IT translation for "minutes" etc is committed
            qv = new TimeSpanQueryValue (); qv.ParseUserQuery ("4 minutes");
            Assert.AreEqual (240, qv.Value);
            Assert.AreEqual ("4 minutes", qv.ToUserQuery ());
            Assert.AreEqual ("240000", qv.ToSql ());
        }
    
        [Test]
        public void QueryParsing ()
        {
            string [] tests = new string [] {
                "foo",
                "foo bar",
                "foo -bar",
                "-foo -bar",
                "-(foo bar)",
                "-(foo or bar)",
                "-(foo (-bar or baz))",
                "-(foo (-bar or -baz))",
                "artist:foo",
                "-artist:foo",
                "-artist!=foo",
                "duration>\"2 minutes\"",
                "rating>3",
                "-rating>3",
                "artist:baz -album:bar",
                "artist:baz -album:bar",
                "artist:baz (rating>3 or rating<2)",
            };
    
            AssertForEach<string> (tests, UserQueryParsesAndGenerates);
        }
    
        [Test]
        public void CustomFormatParenthesisBugFixed ()
        {
            Assert.Fail ("gabe is lame and should fix this. kthx, --aaron");
            /*QueryValue val = new StringQueryValue ();
            val.ParseUserQuery ("mp3");
    
            Assert.AreEqual (
                "(CoreTracks.MimeType LIKE '%mp3%' OR CoreTracks.Uri LIKE '%mp3%')",
                BansheeQuery.MimeTypeField.ToSql (StringQueryValue.Contains, val)
            );*/
        }
    
        private static void UserQueryParsesAndGenerates (string query)
        {
            Assert.Fail ("gabe is lame and should fix this. kthx, --aaron");
            /*QueryNode node = UserQueryParser.Parse (query, BansheeQuery.FieldSet);
            if (query == null || query.Trim () == String.Empty) {
                Assert.AreEqual (node, null); 
                return;
            }
    
            Assert.AreEqual (query, node.ToUserQuery ());*/
        }
    }
}

#endif
