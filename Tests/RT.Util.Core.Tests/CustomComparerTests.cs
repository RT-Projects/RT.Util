using NUnit.Framework;

#pragma warning disable CS8981    // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

namespace RT.Util;

[TestFixture]
public sealed class CustomComparerTests
{
    private class something { public int Thing; public string Stuff; }

    [Test]
    public void TestComparer()
    {
        var s1 = new something { Thing = 4, Stuff = "tests" };
        var s2 = new something { Thing = 7, Stuff = "testS" };
        Assert.AreEqual(-1, new CustomComparer<something>((a, b) => a.Thing.CompareTo(b.Thing)).Compare(s1, s2));
        Assert.AreEqual(-1, CustomComparer<something>.By(s => s.Thing).Compare(s1, s2));
        Assert.AreEqual(-1, CustomComparer<something>.By(s => s.Stuff).Compare(s1, s2));
        Assert.AreEqual(0, CustomComparer<something>.By(s => s.Stuff, StringComparer.OrdinalIgnoreCase).Compare(s1, s2));
        Assert.AreEqual(0, CustomComparer<something>.By(s => s.Stuff, ignoreCase: true).Compare(s1, s2));
        Assert.AreEqual(-1, CustomComparer<something>.By(s => s.Stuff, ignoreCase: false).Compare(s1, s2));
        Assert.AreEqual(-1, CustomComparer<something>.By(s => s.Stuff, (a, b) => string.Compare(a, b)).Compare(s1, s2));

        Assert.AreEqual(-1, CustomComparer<something>.By(s => s.Stuff, ignoreCase: true).ThenBy(s => s.Thing).Compare(s1, s2));
        Assert.AreEqual(1, CustomComparer<something>.By(s => s.Stuff, ignoreCase: true).ThenBy(s => -s.Thing).Compare(s1, s2));
        Assert.AreEqual(0, CustomComparer<something>.By(s => s.Stuff, ignoreCase: true).ThenBy(s => 25).Compare(s1, s2));
        Assert.AreEqual(-1, CustomComparer<something>.By(s => s.Stuff, ignoreCase: false).ThenBy(s => -s.Thing).Compare(s1, s2));

        // a bit incomplete: not testing all the ThenBy overloads
        // a lot incomplete: CustomEqualityComparer tests
    }
}
