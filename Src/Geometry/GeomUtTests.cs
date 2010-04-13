using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RT.Util.Geometry
{
    [TestFixture]
    public sealed class GeomUtTests
    {
        [Test]
        public void TestNormalizedAngle()
        {
            double pi = Math.PI;

            assertNormalizedAngle(0, 0);
            assertNormalizedAngle(3, 3);
            assertNormalizedAngle(-3, -3);

            assertNormalizedAngle(6, 6 - 2*pi);
            assertNormalizedAngle(-6, -6 + 2*pi);

            assertNormalizedAngle(pi, pi);
            assertNormalizedAngle(-pi, pi);

            assertNormalizedAngle(7*pi + 1.2345, -pi + 1.2345);
            assertNormalizedAngle(8*pi + 1.2345, 1.2345);
        }

        private void assertNormalizedAngle(double input, double expected)
        {
            double pi = Math.PI;
            double epsilon = 1e-10;
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input), epsilon);
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input + 2*pi), epsilon);
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input - 2*pi), epsilon);
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input + 4*pi), epsilon);
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input - 4*pi), epsilon);
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input + 6*pi), epsilon);
            Assert.AreEqual(expected, GeomUt.NormalizedAngle(input - 6*pi), epsilon);
        }
    }
}
