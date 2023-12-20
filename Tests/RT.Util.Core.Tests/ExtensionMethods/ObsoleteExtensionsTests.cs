using NUnit.Framework;

namespace RT.Util.ExtensionMethods.Obsolete;

#if !NET5_0_OR_GREATER
[TestFixture]
public sealed class ObsoleteExtensionsTests
{
    [Test]
    public void TestTakeLast()
    {
        Assert.Throws<ArgumentNullException>(() => { ObsoleteExtensions.TakeLast<string>(null, 5); });

        var input = new List<string> { "one", "two", "three", "four" };
        var takeLast2 = input.TakeLast(2);
        Assert.IsTrue(input.TakeLast(0).SequenceEqual(new string[0]));
        Assert.IsTrue(takeLast2.SequenceEqual(new[] { "three", "four" }));
        Assert.IsTrue(input.TakeLast(20).SequenceEqual(input));

        // Test that a change to an underlying enumerable that implements ICollection<T> still returns the correct result
        input.Add("five");
        Assert.IsTrue(takeLast2.SequenceEqual(new[] { "four", "five" }));

        // Test TakeLast on something that isn’t IList<T>
        var input2 = new List<string> { "one", "two", "three", "four" }.Skip(0);
        Assert.IsTrue(input2.TakeLast(0).SequenceEqual(new string[0]));
        Assert.IsTrue(input2.TakeLast(2).SequenceEqual(new[] { "three", "four" }));
        Assert.IsTrue(input2.TakeLast(20).SequenceEqual(input2));
    }
}
#endif
