using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using RT.Util.ExtensionMethods;

namespace RT.Util.Streams
{
    [TestFixture]
    public sealed class DynamicContentStreamTests
    {
        [Test]
        public void TestDynamicContentStream([Values(true, false)] bool buffered)
        {
            for (int chunkSize = 1; chunkSize <= 1024; chunkSize++)
            {
                using (var dynamicContentStream = new DynamicContentStream(dynamicContent(), buffered))
                using (var writeToMemory = new MemoryStream())
                {
                    var buf = new byte[chunkSize];
                    while (true)
                    {
                        var bytesRead = dynamicContentStream.Read(buf, 0, chunkSize);
                        if (bytesRead == 0)
                            break;
                        writeToMemory.Write(buf, 0, bytesRead);
                    }
                    writeToMemory.Close();
                    var bytes = writeToMemory.ToArray();
                    Assert.AreEqual(dynamicContent().JoinString(), bytes.FromUtf8());
                }
            }
        }

        private IEnumerable<string> dynamicContent()
        {
            yield return "// Let P be the parent entity, C be the child.\n";
            yield return "// Let An(P) be the ancestors of P, and De(C) the descendants of C.\n";
            yield return "\n";
            yield return "// In order to update the transitive closure after a parent-child link has been deleted,\n";
            yield return "// we observe the following invariants:\n";
            yield return "\n";
            yield return "// ① The only TC entries X→Y that could potentially be affected are those where X ∈ An(P) and Y ∈ De(C).\n";
            yield return "// ② An(P) and De(C) are necessarily disjoint, so Y ∉ An(P).\n";
            yield return "// ③ Therefore, we can trust that all other links in the TC table (and their distance values) are fine. In particular, any link V→W is fine if (V ∈ An(P) && W ∈ An(P)) || (V ∉ An(P) && W ∉ An(P)).\n";
            yield return "// ④ For any affected TC entry X→Y, it needs to be retained if there is some other path from X to Y that doesn't go through the (P, C) relationship.\n";
            yield return "// ⑤ Any such other link must cross the boundary between An(P) (where X is) and ¬An(P) (where Y is) somewhere else. (Equivalently, one could consider the boundary between ¬De(C) and De(C).)\n";
            yield return "// ⑥ Therefore, a link X→Y must be retained if and only if there exists a parent-child pair (G, H) such that G ∈ An(P), H ∉ An(P), and X→G and H→Y are existing TC entries.\n";
            yield return "//     (Notice that G=P is possible, and H=C is also possible, but not both simultaneously because the (P, C) relationship has already been deleted.)\n";
            yield return "\n";
            yield return "// Thus, delete any link that does not fulfill criterion ⑥.\n";
        }
    }
}
