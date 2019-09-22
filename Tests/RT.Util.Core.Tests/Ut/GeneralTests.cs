using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    [TestFixture]
    public sealed class UtTests
    {
        [Test]
        public void TestGetLongestCommonSubstring()
        {
            Assert.Throws<ArgumentNullException>(() => { Ut.GetLongestCommonSubstring(null); });
            Assert.Throws<ArgumentException>(() => { Ut.GetLongestCommonSubstring(); });

            Assert.AreEqual("Single", Ut.GetLongestCommonSubstring("Single"));
            Assert.AreEqual("le", Ut.GetLongestCommonSubstring("Single", "Double"));
            Assert.AreEqual("", Ut.GetLongestCommonSubstring("Winter", "Spring", "Summer", "Autumn"));
            Assert.AreEqual("all", Ut.GetLongestCommonSubstring("all", "ball", "call"));
            Assert.AreEqual("all", Ut.GetLongestCommonSubstring("ball", "call", "all"));
        }
    }
}
