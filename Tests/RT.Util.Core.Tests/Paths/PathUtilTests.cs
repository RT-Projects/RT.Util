using NUnit.Framework;

namespace RT.Util.Paths;

[TestFixture]
public sealed class PathUtilTests
{
    [Test]
    public void TestToggleRelative()
    {
        // Basic relative to absolute
        assertPathToggle(@"C:\blah\thing", @".", @"C:\blah\thing");
        assertPathToggle(@"C:\blah\thing", @"stuff", @"C:\blah\thing\stuff");
        assertPathToggle(@"C:\blah\thing", @"stuff\more", @"C:\blah\thing\stuff\more");
        assertPathToggle(@"C:\blah\thing", @"..\stuff\more", @"C:\blah\stuff\more");
        assertPathToggle(@"C:\blah\thing", @"..\..\stuff\more", @"C:\stuff\more");
        assertPathToggle(@"C:\blah\thing", @"..", @"C:\blah");
        assertPathToggle(@"C:\blah\thing", @"..\..\..", @"C:\"); // even though path is above root
        assertPathToggle(@"C:\", @".", @"C:\");
        assertPathToggle(@"C:\", @"stuff\more", @"C:\stuff\more");
        assertPathToggle(@"C:\", @"..", @"C:\"); // even though path is above root
        assertPathToggle(@"C:\", @"..\stuff\more", @"C:\stuff\more"); // even though path is above root

        // Basic absolute to relative
        assertPathToggle(@"C:\blah\thing", @"C:\", @"..\..");
        assertPathToggle(@"C:\blah\thing", @"C:\blah", @"..");
        assertPathToggle(@"C:\blah\thing", @"C:\blah\thing", @".");
        assertPathToggle(@"C:\blah\thing", @"C:\blah\thing\stuff", @"stuff");
        assertPathToggle(@"C:\blah\thing", @"C:\blah\thing\stuff\more", @"stuff\more");
        assertPathToggle(@"C:\blah\thing", @"C:\blah\stuff\more", @"..\stuff\more");
        assertPathToggle(@"C:\blah\thing", @"C:\stuff\more", @"..\..\stuff\more");
        assertPathToggle(@"C:\", @"C:\", @".");
        assertPathToggle(@"C:\", @"C:\thing", @"thing");
        assertPathToggle(@"C:\", @"C:\thing\stuff", @"thing\stuff");
        assertPathToggle(@"C:\", @"D:\", ToggleRelativeProblem.PathsOnDifferentDrives);
        assertPathToggle(@"C:\thing", @"D:\stuff", ToggleRelativeProblem.PathsOnDifferentDrives);

        // Base path is not absolute
        assertPathToggle(@"blah\thing", @"stuff", ToggleRelativeProblem.BasePathNotAbsolute);
        // C:blah is so rare that we'll just call this "undefined behaviour"...

        // Invalid paths
        assertPathToggle(@"C:\blah\thing", @"", ToggleRelativeProblem.InvalidToggledPath);
        assertPathToggle(@"", @"thing\blah", ToggleRelativeProblem.InvalidBasePath);
        //assertPathToggle(@"C:\blah\thing", @"thing\*\blah", ToggleRelativeProblem.InvalidToggledPath); - this is valid in Core
        //assertPathToggle(@"C:\blah\*\thing", @"thing\blah", ToggleRelativeProblem.InvalidBasePath); - this is valid in Core
        // UNC paths
        assertPathToggle(@"\\serv\share\blah\thing", @"..\stuff\more", @"\\serv\share\blah\stuff\more");
        assertPathToggle(@"\\serv\share\blah\thing", @"\\serv\share\blah\stuff\more", @"..\stuff\more");
        // Weird paths with extra . or .. or forward slashes or multiple slashes - test all at once since the normalization is provided by the framework; no need to thoroughly test that.
        assertPathToggle(@"C:\blah\thing\.", @"stuff\..\more", @"C:\blah\thing\more");
        assertPathToggle(@"C:\\\blah/./..\thing", @".\stuff/foo\..\\\more\.\bar", @"C:\thing\stuff\more\bar");
        assertPathToggle(@"C:\\\blah/./..\thing", @".\stuff/foo\..\/\../\/..\more\.\bar", @"C:\more\bar");
        assertPathToggle(@"C:\blah\thing\.", @"C:\stuff\..\more", @"..\..\more");
        assertPathToggle(@"C:\\\blah/./..\thing", @"C:\./stuff/../thing\/foo\..\\\more\.\bar", @"more\bar");
    }

    private void assertPathToggle(string basePath, string toggledPath, string expectedResult)
    {
        Assert.AreEqual(expectedResult, PathUtil.ToggleRelative(basePath, toggledPath));
        Assert.AreEqual(expectedResult, PathUtil.ToggleRelative(basePath + "\\", toggledPath));
        Assert.AreEqual(expectedResult, PathUtil.ToggleRelative(basePath, toggledPath + "\\"));
    }

    private void assertPathToggle(string basePath, string toggledPath, ToggleRelativeProblem expectedProblem)
    {
        try { PathUtil.ToggleRelative(basePath, toggledPath); }
        catch (ToggleRelativeException e) { Assert.AreEqual(expectedProblem, e.Problem, "ToggleRelativeException has the wrong Problem"); return; }
        Assert.Fail("ToggleRelativeException expected");
    }
}
