using System;
using NUnit.Framework;
using Hyena;

[TestFixture]
public class StringUtilTests
{
    private class Map
    {
        public Map (string camel, string under)
        {
            Camel = camel;
            Under = under;
        }

        public string Camel;
        public string Under;
    }

    private Map [] u_to_c_maps = new Map [] {
        new Map ("Hello", "hello"),
        new Map ("HelloWorld", "hello_world"),
        new Map ("HelloWorld", "hello__world"),
        new Map ("HelloWorld", "hello___world"),
        new Map ("HelloWorld", "hello____world"),
        new Map ("HelloWorld", "_hello_world"),
        new Map ("HelloWorld", "__hello__world"),
        new Map ("HelloWorld", "___hello_world_"),
        new Map ("HelloWorldHowAreYou", "_hello_World_HOW_ARE__YOU__"),
        new Map (null, ""),
        new Map ("H", "h")
    };

    [Test]
    public void TestUnderCaseToCamelCase ()
    {
        foreach (Map map in u_to_c_maps) {
            Assert.AreEqual (map.Camel, StringUtil.UnderCaseToCamelCase (map.Under));
        }
    }

    private Map [] c_to_u_maps = new Map [] {
        new Map ("Hello", "hello"),
        new Map ("HelloWorld", "hello_world"),
        new Map ("HiWorldHowAreYouDoingToday", "hi_world_how_are_you_doing_today"),
        new Map ("SRSLYHowAreYou", "srsly_how_are_you"),
        new Map ("OMGThisShitIsBananas", "omg_this_shit_is_bananas"),
        new Map ("KTHXBAI", "kthxbai"),
        new Map ("nereid.track_view_columns.MusicLibrarySource-Library/composer", "nereid.track_view_columns._music_library_source_-_library/composer"),
        new Map ("", null),
        new Map ("H", "h")
    };

    [Test]
    public void TestCamelCaseToUnderCase ()
    {
        foreach (Map map in c_to_u_maps) {
            Assert.AreEqual (map.Under, StringUtil.CamelCaseToUnderCase (map.Camel));
        }
    }

    [Test]
    public void TestDoubleToTenthsPrecision ()
    {
        // Note we are testing with locale = it_IT, hence the commas
        Assert.AreEqual ("15",      StringUtil.DoubleToTenthsPrecision (15.0));
        Assert.AreEqual ("15",      StringUtil.DoubleToTenthsPrecision (15.0334));
        Assert.AreEqual ("15,1",    StringUtil.DoubleToTenthsPrecision (15.052));
        Assert.AreEqual ("15,5",    StringUtil.DoubleToTenthsPrecision (15.5234));
        Assert.AreEqual ("15",      StringUtil.DoubleToTenthsPrecision (14.9734));
        Assert.AreEqual ("14,9",    StringUtil.DoubleToTenthsPrecision (14.92));
        Assert.AreEqual ("0,4",     StringUtil.DoubleToTenthsPrecision (0.421));
        Assert.AreEqual ("0",       StringUtil.DoubleToTenthsPrecision (0.01));
        Assert.AreEqual ("1.000,3", StringUtil.DoubleToTenthsPrecision (1000.32));
        Assert.AreEqual ("9.233",   StringUtil.DoubleToTenthsPrecision (9233));
    }

    [Test]
    public void TestDoubleToPluralInt ()
    {
        // This method helps us pluralize doubles. Probably a horrible i18n idea.
        Assert.AreEqual (0,     StringUtil.DoubleToPluralInt (0));
        Assert.AreEqual (1,     StringUtil.DoubleToPluralInt (1));
        Assert.AreEqual (2,     StringUtil.DoubleToPluralInt (2));
        Assert.AreEqual (1,     StringUtil.DoubleToPluralInt (0.5));
        Assert.AreEqual (2,     StringUtil.DoubleToPluralInt (1.8));
        Assert.AreEqual (22,    StringUtil.DoubleToPluralInt (21.3));
    }
}
