using System;
using System.Reflection;
using NUnit.Framework;

using Hyena.Query;
using Banshee.Query;

[TestFixture]
public class QueryTests
{
    [Test]
    public void TestQueryValueSql ()
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
    public void TestQueryParsing ()
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

    public delegate void TestRunner<T> (T item);
    public static void AssertForEach<T> (System.Collections.Generic.IEnumerable<T> objects, TestRunner<T> runner)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder ();
        foreach (T o in objects) {
            try { runner (o); }
            catch (AssertionException e) { sb.AppendFormat ("Failed processing {0}: {1}\n", o, e.Message); }
            catch (Exception e) { sb.AppendFormat ("Failed processing {0}: {1}\n", o, e.ToString ()); }
        }

        if (sb.Length > 0)
            Assert.Fail ("\n" + sb.ToString ());
    }

    private static void UserQueryParsesAndGenerates (string query)
    {
        QueryNode node = UserQueryParser.Parse (query, BansheeQuery.FieldSet);
        if (query == null || query.Trim () == String.Empty) {
            Assert.AreEqual (node, null); 
            return;
        }

        Assert.AreEqual (query, node.ToUserQuery ());
    }
}
