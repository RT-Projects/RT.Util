using NUnit.Framework;
using System.Xml.Linq;

namespace RT.Util.ExtensionMethods
{
    [TestFixture]
    public sealed class XmlLinqExtensionsTests
    {
        string xml = @"<job unique-id='2987CF7B-A18D7EA3-A019820B-D42BC32F'>
                                <plan id='PRJ-GROUP-PLANNAME'/>
                                <security pwd='dontlookitsasecret'/>
                                <callback url='http://rebuilderserver/callback/3469872bcb9876a98a76f98f76de9876da'/>
                                <actions timeout='1800'>
                                    <action plugin='CuteBits/perforce' on-failure='abort' timeout='600.5'>
                                        <plugin-settings/>
                                    </action>
                                </actions>
                            </job>
                            ".Replace("'", "\"");
        XDocument doc;
        XElement root;

        [TestFixtureSetUp]
        public void Setup()
        {
            doc = XDocument.Parse(xml);
            root = doc.Root;
        }

        [Test]
        public void TestElementPath()
        {
            Assert.AreEqual("job", root.Path());
            Assert.AreEqual("job/callback", root.Element("callback").Path());
            Assert.AreEqual("job/actions/action/plugin-settings", root.Element("actions").Element("action").Element("plugin-settings").Path());
        }

        [Test]
        public void TestAttributePath()
        {
            Assert.AreEqual("job[unique-id]", root.Attribute("unique-id").Path());
            Assert.AreEqual("job/callback[url]", root.Element("callback").Attribute("url").Path());
            Assert.AreEqual("job/actions/action[timeout]", root.Element("actions").Element("action").Attribute("timeout").Path());
        }

        [Test]
        public void TestChkElement()
        {
            Assert.AreSame(root.Element("callback"),
                root.ChkElement("callback"));
            Assert.AreSame(root.Element("actions").Element("action").Element("plugin-settings"),
                root.ChkElement("actions").ChkElement("action").ChkElement("plugin-settings"));

            try
            {
                root.ChkElement("actions").ChkElement("not-here");
                Assert.Fail("Exception expected");
            }
            catch (RTException E)
            {
                Assert.IsTrue(E.Message.Contains(root.ChkElement("actions").Path()));
                Assert.IsTrue(E.Message.Contains("not-here"));
            }
        }

        [Test]
        public void TestChkAttribute()
        {
            Assert.AreSame(root.Attribute("unique-id"),
                root.ChkAttribute("unique-id"));
            Assert.AreSame(root.Element("actions").Element("action").Attribute("timeout"),
                root.ChkElement("actions").ChkElement("action").ChkAttribute("timeout"));

            try
            {
                root.ChkElement("actions").ChkAttribute("not-here");
                Assert.Fail("Exception expected");
            }
            catch (RTException E)
            {
                Assert.IsTrue(E.Message.Contains(root.ChkElement("actions").Path()));
                Assert.IsTrue(E.Message.Contains("not-here"));
            }
        }

        [Test]
        public void TestAsDouble()
        {
            Assert.AreEqual(double.Parse("600.5"), root.Element("actions").Element("action").Attribute("timeout").AsDouble());
            try
            {
                double dummy = root.Attribute("unique-id").AsDouble();
                Assert.Fail("Expected exception");
            }
            catch (RTException E)
            {
                Assert.IsTrue(E.Message.Contains("double"));
            }
        }
    }
}
