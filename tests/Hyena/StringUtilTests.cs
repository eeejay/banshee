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
}
