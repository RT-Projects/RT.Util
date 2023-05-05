using System;
using System.Threading;
using RT.Serialization.Settings;

namespace RT.Serialization.Tests;

internal class SettingsTests
{
    // not marked as a test case: this is not a fully implemented automated test; it can only be executed manually as it has no automated assertions
    public void TestBackgroundNonCloneable()
    {
        var cfg = new SettingsFileXml<FoobarNCl>("Foobar");

        // basic test 1: same for cloneable and non-cloneable
        cfg.Settings.Foo = "ABC1";
        cfg.SaveInBackground(); // this doesn't have enough time to trigger
        Thread.Sleep(2000);
        cfg.Settings.Foo = "ABC2";
        cfg.Save(); // this cancels the earlier background save
        cfg.Settings.Foo = "ABC3"; // this never gets saved
        Thread.Sleep(10000);

        // basic test 2
        cfg.Save();
        cfg.Settings.Foo = "ABC1";
        cfg.SaveInBackground(); // this doesn't have enough time to trigger
        Thread.Sleep(1000);
        cfg.Settings.Foo = "ABC2"; // this is the first change that gets persisted
        cfg.SaveInBackground();
        Thread.Sleep(20000);
        cfg.Settings.Foo = "ABC3"; // this also gets persisted
        cfg.Save();
    }

    public void TestBackgroundCloneable()
    {
        var cfg = new SettingsFileXml<FoobarCl>("Foobar");

        // basic test 1: same for cloneable and non-cloneable
        cfg.Settings.Foo = "ABC1";
        cfg.SaveInBackground(); // this doesn't have enough time to trigger
        Thread.Sleep(2000);
        cfg.Settings.Foo = "ABC2";
        cfg.Save(); // this cancels the earlier background save
        cfg.Settings.Foo = "ABC3"; // this never gets saved
        Thread.Sleep(10000);

        // basic test 2
        cfg.Settings.Foo = "ABC1";
        cfg.SaveInBackground();
        cfg.Settings.Foo = "ABC2"; // this must not be persisted when the background save triggers
        Thread.Sleep(7000);

        // basic test 3
        cfg.Settings.Foo = "ABC1";
        cfg.SaveInBackground();
        Thread.Sleep(1000);
        cfg.Settings.Foo = "ABC2";
        cfg.SaveInBackground(); // now it gets persisted, and we never see the ABC1 version
        Thread.Sleep(7000);
    }

    class FoobarNCl
    {
        public string Foo = "abc";
        public string Bar = "def";
    }

    class FoobarCl : ICloneable
    {
        public string Foo = "abc";
        public string Bar = "def";

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
