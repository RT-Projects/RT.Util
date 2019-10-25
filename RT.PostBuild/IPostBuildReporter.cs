namespace RT.PostBuild
{
    /// <summary>Provides the ability to output post-build messages (with filename and line number) to Console.Error. This interface is used by <see cref="PostBuildChecker.RunPostBuildChecks"/>.</summary>
    public interface IPostBuildReporter
    {
        /// <summary>When implemented in a class, searches the source directory for the first occurrence of the first token in <paramref name="tokens"/>,
        /// and then starts searching there to find the first occurrence of each of the subsequent <paramref name="tokens"/> within the same file. When found,
        /// outputs the error <paramref name="message"/> including the filename and line number where the last token was found.</summary>
        void Error(string message, params string[] tokens);

        /// <summary>When implemented in a class, outputs the error <paramref name="message"/> including the specified <paramref name="filename"/>, <paramref name="lineNumber"/> and optional <paramref name="columnNumber"/>.</summary>
        void Error(string message, string filename, int lineNumber, int? columnNumber = null);

        /// <summary>When implemented in a class, searches the source directory for the first occurrence of the first token in <paramref name="tokens"/>,
        /// and then starts searching there to find the first occurrence of each of the subsequent <paramref name="tokens"/> within the same file. When found,
        /// outputs the warning <paramref name="message"/> including the filename and line number where the last token was found.</summary>
        void Warning(string message, params string[] tokens);

        /// <summary>When implemented in a class, outputs the warning <paramref name="message"/> including the specified <paramref name="filename"/>, <paramref name="lineNumber"/> and optional <paramref name="columnNumber"/>.</summary>
        void Warning(string message, string filename, int lineNumber, int? columnNumber = null);
    }
}
