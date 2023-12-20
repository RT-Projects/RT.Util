using NUnit.Framework;
using RT.KitchenSink;

namespace KitchenSinkTests;

[TestFixture]
class ValueStatTests
{
    [Test]
    public void TestValueStatDouble()
    {
        var nums1 = new[] { 19.8, 15.5, 89.6, 38.6, 77.5, 11.3 };
        var nums2 = new[] { 94.0, 9.4, 12.2, 40.3, 71.1, 0.7, 23.1, 86.2, 55.5, 92.7, 29.1, 56.5 };
        var stat1 = new ValueStat();
        var stat2 = new ValueStat();
        var stat3 = new ValueStat();
        foreach (var num in nums1)
        {
            stat1.AddObservation(num);
            stat3.AddObservation(num);
        }
        foreach (var num in nums2)
        {
            stat2.AddObservation(num);
            stat3.AddObservation(num);
        }
        var stat4 = new ValueStat();
        stat4.AddObservations(stat1.ObservationCount, stat1.Mean, stat1.Min, stat1.Max, stat1.Variance);
        stat4.AddObservations(stat2.ObservationCount, stat2.Mean, stat2.Min, stat2.Max, stat2.Variance);

        Assert.AreEqual(6, stat1.ObservationCount);
        Assert.AreEqual(12, stat2.ObservationCount);
        Assert.AreEqual(6 + 12, stat3.ObservationCount);
        Assert.AreEqual(6 + 12, stat4.ObservationCount);

        Assert.AreEqual(42.05, stat1.Mean, 0.00001);
        Assert.AreEqual(47.56666667, stat2.Mean, 0.00001);
        Assert.AreEqual(45.72777778, stat3.Mean, 0.00001);
        Assert.AreEqual(45.72777778, stat4.Mean, 0.00001);

        Assert.AreEqual(945.8558333, stat1.Variance, 0.00001);
        Assert.AreEqual(1023.215556, stat2.Variance, 0.00001);
        Assert.AreEqual(1004.192006, stat3.Variance, 0.00001);
        Assert.AreEqual(1004.192006, stat4.Variance, 0.00001);
    }
}
