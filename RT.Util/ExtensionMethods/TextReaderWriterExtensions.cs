using System.Collections.Generic;
using System.IO;

namespace RT.Util.ExtensionMethods
{
    /// <summary>
    ///     Provides extension methods on the <see cref="TextReader"/>, <see cref="TextWriter"/>
    ///     and any related types.</summary>
    public static class TextReaderWriterExtensions
    {
        /// <summary>
        ///     Enumerates all (remaining) lines from this text reader, reading lines only when needed, and
        ///     hence compatible with potentially blocking or infinite streams.</summary>
        public static IEnumerable<string> ReadLines(this TextReader reader)
        {
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    yield break;
                yield return line;
            }
        }
    }
}
