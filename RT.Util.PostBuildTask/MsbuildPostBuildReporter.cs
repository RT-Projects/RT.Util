using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Utilities;
using RT.Util.ExtensionMethods;

namespace RT.Util.PostBuildTask
{
    internal class MsbuildPostBuildReporter : IPostBuildReporter
    {
        public bool AnyErrors { get; set; }

        private readonly string _path;
        private readonly TaskLoggingHelper _logger;
        public MsbuildPostBuildReporter(string path, TaskLoggingHelper logger)
        {
            _path = path;
            _logger = logger;
            AnyErrors = false;

        }
        public void Error(string message, params string[] tokens)
        {
            AnyErrors = true;
            output(true, message, tokens);
        }

        public void Warning(string message, params string[] tokens)
        {
            output(false, message, tokens);
        }

        public void Error(string message, string filename, int lineNumber, int? columnNumber = null)
        {
            AnyErrors = true;
            Log(true, message, filename, lineNumber, columnNumber ?? 0);
        }

        public void Warning(string message, string filename, int lineNumber, int? columnNumber = null)
        {
            Log(false, message, filename, lineNumber, columnNumber ?? 0);
        }

        private void Log(bool isError, string message, string filename, int lineNumber, int columnNumber)
        {
            var endColumnNumber = columnNumber + 5;
            if (columnNumber > 0)
                endColumnNumber = File.ReadLines(filename).Skip(lineNumber - 1).First().Length;

            if (isError)
                _logger.LogError("PostBuildCheck", "CS9999", "", filename, lineNumber, columnNumber, lineNumber, endColumnNumber, message);
            else
                _logger.LogWarning("PostBuildCheck", "CS9999", "", filename, lineNumber, columnNumber, lineNumber, endColumnNumber, message);
        }

        private void output(bool isError, string message, params string[] tokens)
        {
            if (tokens == null || tokens.Length == 0 || tokens.All(t => t == null))
            {
                var frame = new StackFrame(2, true);
                Log(isError, message, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetFileColumnNumber());
                return;
            }

            try
            {
                var tokenRegexes = tokens.Select(tok => tok == null ? null : new Regex(@"\b" + Regex.Escape(tok) + @"\b")).ToArray();
                foreach (var f in new DirectoryInfo(_path).GetFiles("*.cs", SearchOption.AllDirectories))
                {
                    var lines = File.ReadAllLines(f.FullName);
                    var tokenIndex = tokens.IndexOf(t => t != null);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        Match match;
                        var charIndex = 0;
                        while ((match = tokenRegexes[tokenIndex].Match(lines[i], charIndex)).Success)
                        {
                            do { tokenIndex++; } while (tokenIndex < tokens.Length && tokens[tokenIndex] == null);
                            if (tokenIndex == tokens.Length)
                            {
                                if (isError)
                                    _logger.LogError("PostBuildCheck", "CS9999", "", f.FullName, i + 1, match.Index + 1, i + 1, match.Index + match.Length + 1, message);
                                else
                                    _logger.LogWarning("PostBuildCheck", "CS9999", "", f.FullName, i + 1, match.Index + 1, i + 1, match.Index + match.Length + 1, message);
                                return;
                            }
                            charIndex = match.Index + match.Length;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogErrorFromException(e);
            }

            {
                var frame = new StackFrame(2, true);
                Log(isError, message, frame.GetFileName(), frame.GetFileLineNumber(), frame.GetFileColumnNumber());
            }
        }
    }

}
