using NUnit.Framework;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RT.Util
{
    [TestFixture]
    public sealed class EggsMLTests
    {
        [Test]
        public void TestEggsML()
        {
            var expectations = new List<Tuple<string, string, string>>
            {
                Tuple.Create("", "(0:)", ""),
                Tuple.Create("*bold*", "(1:*(1:))", "*bold*"),
                Tuple.Create("non-bold *bold* non-bold",  "(3:,*(1:),)", "non-bold *bold* non-bold"),
                Tuple.Create("more ***bold*", "(2:,*(1:))", "more *bold*"),
                Tuple.Create("Nested |*tags*|", "(2:,|(1:*(1:)))", "Nested |*tags*|"),
                Tuple.Create("Nested |`|||tags|`| yeah!", "(3:,|(1:|(1:)),)", "Nested |`|||tags|`| yeah!"),
                Tuple.Create("Empty |`| tag!", "(3:,|(0:),)", "Empty |`| tag!"),
                Tuple.Create(@"Escaped ""http://www.google.com/"" URL", "(1:)", @"""Escaped http://www.google.com/ URL"""),
                Tuple.Create(@"""String """"string"""" string""", "(1:)", @"String """"string"""" string"),
                Tuple.Create("[[square]] [<[tag]>]", "(2:,[(1:<(1:[(1:))))", "[[square]] [<[tag]>]"),
            };

            foreach (var tuple in expectations)
            {
                Func<EggsNode, string> recurse = null;
                recurse = node => (node is EggsTag ? ((EggsTag) node).Tag.ToString() : "") + (node is EggsContainer ? "(" + ((EggsContainer) node).Children.Count.ToString() + ":" + ((EggsContainer) node).Children.Select(n => recurse(n)).JoinString(",") + ")" : "");
                var eggs = EggsML.Parse(tuple.Item1);
                Assert.AreEqual(tuple.Item2, recurse(eggs));
                Assert.AreEqual(tuple.Item3, eggs.ToString());
            }

            Assert.Throws<ArgumentNullException>(() => EggsML.Parse(null));
            Assert.Throws<EggsMLParseException>(() => EggsML.Parse("xyz] xyz"));
            Assert.Throws<EggsMLParseException>(() => EggsML.Parse("xyz [[[xyz"));
            Assert.Throws<EggsMLParseException>(() => EggsML.Parse("xyz```xyz"));
            Assert.Throws<EggsMLParseException>(() => EggsML.Parse("xyz*****xyz"));
            Assert.Throws<EggsMLParseException>(() => EggsML.Parse("xyz]]] xyz"));
            Assert.Throws<EggsMLParseException>(() => EggsML.Parse("xyz *xyz"));
        }
    }
}
