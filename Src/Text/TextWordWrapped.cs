using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.Util.Text
{
    /// <summary>
    /// Holds an instance of a text which has been word-wrapped to a
    /// specified width. Supports unix-style new lines and indented
    /// paragraphs.
    /// 
    /// The results can either be accessed through the <see>Lines</see>
    /// list or via the [] operator. The latter will return blank lines
    /// if trying to access a line beyond the last one.
    /// </summary>
    public class TextWordWrapped
    {
        /// <summary>
        /// Holds the wrapped text, line by line.
        /// </summary>
        public readonly List<string> Lines = new List<string>();

        private static Regex regexSplitOnWindowsNewline = new Regex(@"\r\n", RegexOptions.Compiled);
        private static Regex regexSplitOnUnixMacNewline = new Regex(@"[\r\n]", RegexOptions.Compiled);
        private static Regex regexKillDoubleSpaces = new Regex(@"  +", RegexOptions.Compiled);

        /// <summary>
        /// Creates an instance of the class, generating the word-wrapped
        /// version of the supplied <see>text</see>.
        /// 
        /// The supplied text will be split into "paragraphs" on the newline
        /// characters. Every paragraph will begin on a new line in the word-
        /// wrapped output, indented by the same number of spaces as in the
        /// input. All subsequent lines belonging to that paragraph will also
        /// be indented by the same amount.
        /// 
        /// All multiple contiguous spaces will be replaced with a single
        /// space (except for the indentation).
        /// </summary>
        /// <param name="maxWidth">The maximum number of characters permitted
        /// on a single line, not counting the end-of-line terminator.</param>
        public TextWordWrapped(string text, int maxWidth)
        {
            if (text == null)
                text = "";
            if (maxWidth < 1)
                throw new ArgumentOutOfRangeException("maxWidth cannot be less than 1");

            // Split into "paragraphs"
            string[] paragraphs = regexSplitOnWindowsNewline.Split(text);
            foreach (string paragraph in paragraphs)
                foreach (string para in regexSplitOnUnixMacNewline.Split(paragraph))
                    AddParagraph(para, maxWidth);
        }

        /// <summary>
        /// Word-wraps the specified paragraph and adds the resulting
        /// lines to the Lines list. Does not expect to find any line
        /// breaks in the input.
        /// </summary>
        private void AddParagraph(string paragraph, int maxWidth)
        {
            // Count the number of spaces at the start of the paragraph
            int indentLen = 0;
            while (indentLen < paragraph.Length && paragraph[indentLen] == ' ')
                indentLen++;

            // Get a list of words
            string[] words = regexKillDoubleSpaces.Replace(paragraph.Substring(indentLen), " ").Split(' ');

            StringBuilder curLine = new StringBuilder();
            string indent = new string(' ', indentLen);
            string space = indent;

            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];

                if (curLine.Length + space.Length + word.Length > maxWidth)
                {
                    // Need to wrap
                    if (word.Length > maxWidth)
                    {
                        // This is a very long word
                        // Leave part of the word on the current line but only if at least 2 chars fit
                        if (curLine.Length + space.Length + 2 <= maxWidth)
                        {
                            int length = maxWidth - curLine.Length - space.Length;
                            curLine.Append(space);
                            curLine.Append(word.Substring(0, length));
                            word = word.Substring(length);
                        }
                        // Commit the current line
                        Lines.Add(curLine.ToString());

                        // Now append full lines' worth of text until we're left with less than a full line
                        while (indent.Length + word.Length > maxWidth)
                        {
                            Lines.Add(indent + word.Substring(0, maxWidth - indent.Length));
                            word = word.Substring(maxWidth - indent.Length);
                        }

                        // Start a new line with whatever is left
                        curLine = new StringBuilder();
                        curLine.Append(indent);
                        curLine.Append(word);
                    }
                    else
                    {
                        // This word is not very long and it doesn't fit so just wrap it to the next line
                        Lines.Add(curLine.ToString());

                        // Start a new line
                        curLine = new StringBuilder();
                        curLine.Append(indent);
                        curLine.Append(word);
                    }
                }
                else
                {
                    // No need to wrap yet
                    curLine.Append(space);
                    curLine.Append(word);
                }

                space = " ";
            }

            Lines.Add(curLine.ToString());
        }

        /// <summary>
        /// Accesses the nth line of the resulting word-wrapped text.
        /// Returns "" if the line is out of range.
        /// </summary>
        public string this[int lineIndex]
        {
            get
            {
                if (lineIndex >= Lines.Count)
                    return "";
                else
                    return Lines[lineIndex];
            }
        }
    }

}
