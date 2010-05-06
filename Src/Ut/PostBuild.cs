
namespace RT.Util
{
#if DEBUG
    /// <summary>Provides the ability to output post-build messages (with filename and line number) to Console.Error. This interface is used by <see cref="Ut.RunPostBuildChecks"/>.</summary>
    public interface IPostBuildReporter
    {
        /// <summary>When implemented in a class, searches the source directory for the first occurrence of the first token in <paramref name="tokens"/>,
        /// and then starts searching there to find the first occurrence of each of the subsequent <paramref name="tokens"/> within the same file. When found,
        /// outputs the error <paramref name="message"/> including the filename and line number where the last token was found.</summary>
        void Error(string message, params string[] tokens);

        /// <summary>When implemented in a class, searches the source directory for the first occurrence of the first token in <paramref name="tokens"/>,
        /// and then starts searching there to find the first occurrence of each of the subsequent <paramref name="tokens"/> within the same file. When found,
        /// outputs the warning <paramref name="message"/> including the filename and line number where the last token was found.</summary>
        void Warning(string message, params string[] tokens);
    }
#endif
}
