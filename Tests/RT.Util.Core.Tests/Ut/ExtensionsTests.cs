using NUnit.Framework;

namespace RT.Util;

[TestFixture]
public sealed class UtExtensionsTests
{
    private void Check<T>(Type expected, T actual)
    {
        Assert.AreEqual(expected, typeof(T));
    }

    [Test]
    public void TestNullOr()
    {
        // STRUCT? → STRUCT
        {
            int? value = 47;
            var result = value.NullOr(j => { Check(typeof(int), j); return j + 1; });
            Check(typeof(int?), result);
            Assert.AreEqual(result, 48);

            value = null;
            result = value.NullOr(j => { Assert.Fail(); return j + 1; });
            Assert.AreEqual(result, null);
        }

        // STRUCT? → STRUCT?
        {
            int? value = 47;
            var result = value.NullOr(j => { Check(typeof(int), j); return (j + 1).Nullable(); });
            Check(typeof(int?), result);
            Assert.AreEqual(result, 48);

            value = null;
            result = value.NullOr(j => { Assert.Fail(); return (j + 1).Nullable(); });
            Assert.AreEqual(result, null);
        }

        // STRUCT? → CLASS
        {
            DateTime? value = DateTime.UtcNow;
            var result = value.NullOr(d => { Check(typeof(DateTime), d); return d.GetType(); });
            Check(typeof(Type), result);
            Assert.AreEqual(typeof(DateTime), result);

            value = null;
            result = value.NullOr(d => { Assert.Fail(); return d.GetType(); });
            Assert.AreEqual(result, null);
        }

        // CLASS → STRUCT
        {
            string value = "47";
            var result = value.NullOr(str => { Check(typeof(string), str); return str.Length; });
            Check(typeof(int?), result);
            Assert.AreEqual(result, 2);

            value = null;
            result = value.NullOr(str => { Assert.Fail(); return str.Length; });
            Assert.AreEqual(result, null);
        }

        // CLASS → STRUCT?
        {
            string value = "47";
            var result = value.NullOr(str => { Check(typeof(string), str); return str.Length.Nullable(); });
            Check(typeof(int?), result);
            Assert.AreEqual(result, 2);

            value = null;
            result = value.NullOr(str => { Assert.Fail(); return str.Length.Nullable(); });
            Assert.AreEqual(result, null);
        }

        // CLASS → CLASS
        {
            string value = "xyz";
            var result = value.NullOr(str => { Check(typeof(string), str); return str.ToUpperInvariant(); });
            Check(typeof(string), result);
            Assert.AreEqual(result, "XYZ");

            value = null;
            result = value.NullOr(str => { Assert.Fail(); return str.ToUpperInvariant(); });
            Assert.AreEqual(result, null);
        }
    }
}
