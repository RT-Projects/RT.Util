using NUnit.Framework;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public sealed class NumericExtensionsTests
    {
        [Test]
        public void TestToWords()
        {
            Assert.AreEqual("zero", 0.ToWords());
            Assert.AreEqual("one", 1.ToWords());
            Assert.AreEqual("sixteen", 16.ToWords());
            Assert.AreEqual("forty-two", 42.ToWords());
            Assert.AreEqual("ninety", 90.ToWords());
            Assert.AreEqual("one hundred", 100.ToWords());
            Assert.AreEqual("one hundred and forty-two", 142.ToWords());
            Assert.AreEqual("two billion one hundred and ten million", 2110000000.ToWords());
            Assert.AreEqual("eighty-one million four hundred and thirty-eight thousand five hundred and ten", 81438510.ToWords());
            Assert.AreEqual("minus eighty-one million four hundred and thirty-eight thousand five hundred and ten", (-81438510).ToWords());
        }
    }
}
