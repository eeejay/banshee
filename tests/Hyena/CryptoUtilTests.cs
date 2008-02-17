using System;
using NUnit.Framework;
using Hyena;

[TestFixture]
public class CryptoUtilTests
{
    [Test]
    public void Md5Encode ()
    {
        Assert.AreEqual ("ae2b1fca515949e5d54fb22b8ed95575", CryptoUtil.Md5Encode ("testing"));
    }

    [Test]
    public void IsMd5Encoded ()
    {
        Assert.IsTrue (CryptoUtil.IsMd5Encoded ("ae2b1fca515949e5d54fb22b8ed95575"));
        Assert.IsFalse (CryptoUtil.IsMd5Encoded ("abc233"));
        Assert.IsFalse (CryptoUtil.IsMd5Encoded ("lebowski"));
        Assert.IsFalse (CryptoUtil.IsMd5Encoded ("ae2b1fca515949e5g54fb22b8ed95575"));
    }
}

