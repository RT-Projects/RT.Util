using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Consoles
{
    /// <summary>
    ///     Encapsulates a string in which each character can have an associated <see cref="ConsoleColor"/>.</summary>
    /// <remarks>
    ///     Use <see cref="ConsoleUtil.Write(ConsoleColoredString,bool)"/> and <see
    ///     cref="ConsoleUtil.WriteLine(ConsoleColoredString,bool)"/> to output the string to the console.</remarks>
    public sealed class ConsoleColoredString
    {
        /// <summary>Represents an empty colored string. This field is read-only.</summary>
        public static ConsoleColoredString Empty { get { if (_empty == null) _empty = new ConsoleColoredString(); return _empty; } }
        private static ConsoleColoredString _empty = null;

        /// <summary>
        ///     Represents the environment's newline, colored in the default color (<see cref="ConsoleColor.Gray"/>). This field
        ///     is read-only.</summary>
        public static ConsoleColoredString NewLine { get { if (_newline == null) _newline = new ConsoleColoredString(Environment.NewLine, ConsoleColor.Gray); return _newline; } }
        private static ConsoleColoredString _newline = null;

        private string _text;
        private ConsoleColor[] _colors;

        /// <summary>
        ///     Provides implicit conversion from <see cref="string"/> to <see cref="ConsoleColoredString"/> by assuming a default
        ///     color of <see cref="ConsoleColor.Gray"/>.</summary>
        /// <param name="input">
        ///     The string to convert.</param>
        public static implicit operator ConsoleColoredString(string input)
        {
            if (input == null)
                return null;
            return new ConsoleColoredString(input, ConsoleColor.Gray);
        }

        /// <summary>
        ///     Provides explicit conversion from <see cref="ConsoleColoredString"/> to <see cref="string"/> by discarding all
        ///     color information.</summary>
        /// <param name="input">
        ///     The string to convert.</param>
        public static explicit operator string(ConsoleColoredString input)
        {
            if (input == null)
                return null;
            return input._text;
        }

        /// <summary>
        ///     Constructs a <see cref="ConsoleColoredString"/> with the specified text and the specified color.</summary>
        /// <param name="input">
        ///     The string containing the text to initialise this <see cref="ConsoleColoredString"/> to.</param>
        /// <param name="color">
        ///     The color to assign to the whole string.</param>
        public ConsoleColoredString(string input, ConsoleColor color)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            _text = input;
            _colors = new ConsoleColor[input.Length];
            if (color != default(ConsoleColor))
                for (int i = 0; i < _colors.Length; i++)
                    _colors[i] = color;
        }

        /// <summary>
        ///     Constructs a <see cref="ConsoleColoredString"/> with the specified text and the specified colors for each
        ///     character.</summary>
        /// <param name="input">
        ///     The string containing the text to initialise this <see cref="ConsoleColoredString"/> to. The length of this string
        ///     must match the number of elements in <paramref name="characterColors"/>.</param>
        /// <param name="characterColors">
        ///     The colors to assign to each character in the string. The length of this array must match the number of characters
        ///     in <paramref name="input"/>.</param>
        public ConsoleColoredString(string input, ConsoleColor[] characterColors)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (characterColors == null)
                throw new ArgumentNullException("characterColors");
            if (input.Length != characterColors.Length)
                throw new InvalidOperationException("The number of characters must match the number of colors.");
            _text = input;
            _colors = characterColors;
        }

        /// <summary>
        ///     Constructs a <see cref="ConsoleColoredString"/> by concatenating the specified <see
        ///     cref="ConsoleColoredString"/>s.</summary>
        /// <param name="strings">
        ///     Input strings to concatenate.</param>
        /// <remarks>
        ///     The color of each character in the input strings is preserved.</remarks>
        public ConsoleColoredString(params ConsoleColoredString[] strings)
            : this((ICollection<ConsoleColoredString>) strings)
        {
        }

        /// <summary>
        ///     Constructs a <see cref="ConsoleColoredString"/> by concatenating the specified <see
        ///     cref="ConsoleColoredString"/>s.</summary>
        /// <param name="strings">
        ///     Input strings to concatenate.</param>
        /// <remarks>
        ///     The color of each character in the input strings is preserved.</remarks>
        public ConsoleColoredString(ICollection<ConsoleColoredString> strings)
        {
            var builder = new StringBuilder();
            foreach (var str in strings)
                builder.Append(str._text);
            _text = builder.ToString();
            _colors = new ConsoleColor[_text.Length];
            var index = 0;
            foreach (var str in strings)
            {
                Array.Copy(str._colors, 0, _colors, index, str._colors.Length);
                index += str._colors.Length;
            }
        }

        /// <summary>Returns the number of characters in this <see cref="ConsoleColoredString"/></summary>
        public int Length { get { return _text.Length; } }
        /// <summary>Returns the raw text of this <see cref="ConsoleColoredString"/> by discarding all the color information.</summary>
        public override string ToString() { return _text; }

        /// <summary>
        ///     Concatenates two <see cref="ConsoleColoredString"/>s.</summary>
        /// <param name="string1">
        ///     First input string to concatenate.</param>
        /// <param name="string2">
        ///     Second input string to concatenate.</param>
        /// <remarks>
        ///     The color of each character in the input strings is preserved.</remarks>
        public static ConsoleColoredString operator +(ConsoleColoredString string1, ConsoleColoredString string2)
        {
            if (string1 == null || string1.Length == 0)
                return string2 ?? "";
            if (string2 == null || string2.Length == 0)
                return string1;
            return new ConsoleColoredString(string1, string2);
        }

        /// <summary>
        ///     Concatenates a string onto a <see cref="ConsoleColoredString"/>.</summary>
        /// <param name="string1">
        ///     First input string to concatenate.</param>
        /// <param name="string2">
        ///     Second input string to concatenate.</param>
        /// <remarks>
        ///     The color of each character in the first input string is preserved. The second input string is given the color
        ///     <see cref="ConsoleColor.Gray"/>.</remarks>
        public static ConsoleColoredString operator +(ConsoleColoredString string1, string string2)
        {
            if (string1 == null || string1.Length == 0)
                return string2 ?? "";    // implicit conversion
            if (string2 == null || string2.Length == 0)
                return string1;

            var colors = new ConsoleColor[string1._colors.Length + string2.Length];
            Array.Copy(string1._colors, colors, string1._colors.Length);
            for (int i = string1.Length; i < string1.Length + string2.Length; i++)
                colors[i] = ConsoleColor.Gray;
            return new ConsoleColoredString(string1._text + string2, colors);
        }

        /// <summary>
        ///     Concatenates a <see cref="ConsoleColoredString"/> onto a string.</summary>
        /// <param name="string1">
        ///     First input string to concatenate.</param>
        /// <param name="string2">
        ///     Second input string to concatenate.</param>
        /// <remarks>
        ///     The color of each character in the second input string is preserved. The first input string is given the color
        ///     <see cref="ConsoleColor.Gray"/>.</remarks>
        public static ConsoleColoredString operator +(string string1, ConsoleColoredString string2)
        {
            if (string2 == null || string2.Length == 0)
                return string1 ?? "";   // implicit conversion
            if (string1 == null || string1.Length == 0)
                return string2;

            var colors = new ConsoleColor[string1.Length + string2._colors.Length];
            for (int i = 0; i < string1.Length; i++)
                colors[i] = ConsoleColor.Gray;
            Array.Copy(string2._colors, 0, colors, string1.Length, string2._colors.Length);
            return new ConsoleColoredString(string1 + string2._text, colors);
        }

        /// <summary>
        ///     Constructs a <see cref="ConsoleColoredString"/> from an EggsML parse tree.</summary>
        /// <param name="node">
        ///     The root node of the EggsML parse tree.</param>
        /// <returns>
        ///     The <see cref="ConsoleColoredString"/> constructed from the EggsML parse tree.</returns>
        /// <remarks>
        ///     <para>
        ///         The following EggsML tags map to the following console colors:</para>
        ///     <list type="bullet">
        ///         <item><description>
        ///             <c>~</c> = black, or dark gray if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>/</c> = dark blue, or blue if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>$</c> = dark green, or green if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>&amp;</c> = dark cyan, or cyan if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>_</c> = dark red, or red if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>%</c> = dark magenta, or magenta if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>^</c> = dark yellow, or yellow if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>=</c> = dark gray (independent of <c>*</c> tag)</description></item></list>
        ///     <para>
        ///         Text which is not inside any of the above color tags defaults to light gray, or white if inside a <c>*</c>
        ///         tag.</para></remarks>
        public static ConsoleColoredString FromEggsNode(EggsNode node)
        {
            StringBuilder text = new StringBuilder();
            List<ConsoleColor> colors = new List<ConsoleColor>();
            List<int> colorLengths = new List<int>();

            eggWalk(node, text, colors, colorLengths, ConsoleColor.Gray);

            var colArr = new ConsoleColor[colorLengths.Sum()];
            var index = 0;
            for (int i = 0; i < colors.Count; i++)
            {
                var col = colors[i];
                for (int j = 0; j < colorLengths[i]; j++)
                {
                    colArr[index] = col;
                    index++;
                }
            }

            return new ConsoleColoredString(text.ToString(), colArr);
        }

        private static void eggWalk(EggsNode node, StringBuilder text, List<ConsoleColor> colors, List<int> colorLengths, ConsoleColor curColor)
        {
            var tag = node as EggsTag;
            if (tag != null)
            {
                bool curLight = curColor >= ConsoleColor.DarkGray;
                switch (tag.Tag)
                {
                    case '~': curColor = curLight ? ConsoleColor.DarkGray : ConsoleColor.Black; break;
                    case '/': curColor = curLight ? ConsoleColor.Blue : ConsoleColor.DarkBlue; break;
                    case '$': curColor = curLight ? ConsoleColor.Green : ConsoleColor.DarkGreen; break;
                    case '&': curColor = curLight ? ConsoleColor.Cyan : ConsoleColor.DarkCyan; break;
                    case '_': curColor = curLight ? ConsoleColor.Red : ConsoleColor.DarkRed; break;
                    case '%': curColor = curLight ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta; break;
                    case '^': curColor = curLight ? ConsoleColor.Yellow : ConsoleColor.DarkYellow; break;
                    case '=': curColor = ConsoleColor.DarkGray; curLight = true; break;
                    case '*': if (!curLight) curColor = (ConsoleColor) ((int) curColor + 8); curLight = true; break;
                }
                foreach (var child in tag.Children)
                    eggWalk(child, text, colors, colorLengths, curColor);
            }
            else if (node is EggsText)
            {
                var txt = (EggsText) node;
                text.Append(txt.Text);
                colors.Add(curColor);
                colorLengths.Add(txt.Text.Length);
            }
        }

        /// <summary>
        ///     Generates a sequence of <see cref="ConsoleColoredString"/>s from an EggsML parse tree by word-wrapping the output
        ///     at a specified character width.</summary>
        /// <param name="node">
        ///     The root node of the EggsML parse tree.</param>
        /// <param name="wrapWidth">
        ///     The number of characters at which to word-wrap the output.</param>
        /// <param name="hangingIndent">
        ///     The number of spaces to add to each line except the first of each paragraph, thus creating a hanging
        ///     indentation.</param>
        /// <returns>
        ///     The sequence of <see cref="ConsoleColoredString"/>s generated from the EggsML parse tree.</returns>
        /// <remarks>
        ///     <para>
        ///         The following EggsML tags map to the following console colors:</para>
        ///     <list type="bullet">
        ///         <item><description>
        ///             <c>~</c> = black, or dark gray if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>/</c> = dark blue, or blue if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>$</c> = dark green, or green if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>&amp;</c> = dark cyan, or cyan if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>_</c> = dark red, or red if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>%</c> = dark magenta, or magenta if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>^</c> = dark yellow, or yellow if inside a <c>*</c> tag</description></item>
        ///         <item><description>
        ///             <c>=</c> = dark gray (independent of <c>*</c> tag)</description></item></list>
        ///     <para>
        ///         Text which is not inside any of the above color tags defaults to light gray, or white if inside a <c>*</c>
        ///         tag.</para>
        ///     <para>
        ///         Additionally, the <c>+</c> tag can be used to suppress word-wrapping within a certain stretch of text. In
        ///         other words, the contents of a <c>+</c> tag are treated as if they were a single word. Use this in preference
        ///         to U+00A0 (no-break space) as it is more explicit and more future-compatible in case hyphenation is ever
        ///         implemented here.</para></remarks>
        public static IEnumerable<ConsoleColoredString> FromEggsNodeWordWrap(EggsNode node, int wrapWidth, int hangingIndent = 0)
        {
            var results = new List<ConsoleColoredString> { ConsoleColoredString.Empty };
            EggsML.WordWrap(node, ConsoleColor.Gray, wrapWidth,
                (color, text) => text.Length,
                (color, text, width) => { results[results.Count - 1] += new ConsoleColoredString(text, color); },
                (color, newParagraph, indent) =>
                {
                    var s = newParagraph ? 0 : indent + hangingIndent;
                    results.Add(new ConsoleColoredString(new string(' ', s), color));
                    return s;
                },
                (color, tag, parameter) =>
                {
                    bool curLight = color >= ConsoleColor.DarkGray;
                    switch (tag)
                    {
                        case '~': color = curLight ? ConsoleColor.DarkGray : ConsoleColor.Black; break;
                        case '/': color = curLight ? ConsoleColor.Blue : ConsoleColor.DarkBlue; break;
                        case '$': color = curLight ? ConsoleColor.Green : ConsoleColor.DarkGreen; break;
                        case '&': color = curLight ? ConsoleColor.Cyan : ConsoleColor.DarkCyan; break;
                        case '_': color = curLight ? ConsoleColor.Red : ConsoleColor.DarkRed; break;
                        case '%': color = curLight ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta; break;
                        case '^': color = curLight ? ConsoleColor.Yellow : ConsoleColor.DarkYellow; break;
                        case '=': color = ConsoleColor.DarkGray; break;
                        case '*': color = curLight ? color : (ConsoleColor) ((int) color + 8); break;
                    }
                    return Tuple.Create(color, 0);
                });
            if (results.Last().Length == 0)
                results.RemoveAt(results.Count - 1);
            return results;
        }

        /// <summary>
        ///     Returns the character at the specified index.</summary>
        /// <param name="index">
        ///     A character position in the current <see cref="ConsoleColoredString"/>.</param>
        /// <returns>
        ///     The character at the specified index.</returns>
        public char CharAt(int index)
        {
            if (index < 0 || index >= _text.Length)
                throw new ArgumentOutOfRangeException("index", "index must be greater or equal to 0 and smaller than the length of the ConsoleColoredString.");
            return _text[index];
        }

        /// <summary>
        ///     Word-wraps the current <see cref="ConsoleColoredString"/> to a specified width. Supports UNIX-style newlines and
        ///     indented paragraphs.</summary>
        /// <remarks>
        ///     <para>
        ///         The supplied text will be split into "paragraphs" at the newline characters. Every paragraph will begin on a
        ///         new line in the word-wrapped output, indented by the same number of spaces as in the input. All subsequent
        ///         lines belonging to that paragraph will also be indented by the same amount.</para>
        ///     <para>
        ///         All multiple contiguous spaces will be replaced with a single space (except for the
        ///         indentation).</para></remarks>
        /// <param name="maxWidth">
        ///     The maximum number of characters permitted on a single line, not counting the end-of-line terminator.</param>
        /// <param name="hangingIndent">
        ///     The number of spaces to add to each line except the first of each paragraph, thus creating a hanging
        ///     indentation.</param>
        public IEnumerable<ConsoleColoredString> WordWrap(int maxWidth, int hangingIndent = 0)
        {
            if (_text.Length == 0)
                yield break;
            if (maxWidth < 1)
                throw new ArgumentOutOfRangeException("maxWidth", maxWidth, "maxWidth cannot be less than 1");
            if (hangingIndent < 0)
                throw new ArgumentOutOfRangeException("hangingIndent", hangingIndent, "hangingIndent cannot be negative.");

            // Split into "paragraphs"
            foreach (ConsoleColoredString paragraph in Split(new string[] { "\r\n", "\r", "\n" }, null, StringSplitOptions.None))
            {
                // Count the number of spaces at the start of the paragraph
                int indentLen = 0;
                while (indentLen < paragraph.Length && paragraph.CharAt(indentLen) == ' ')
                    indentLen++;

                var curLine = new List<ConsoleColoredString>();
                var indent = new string(' ', indentLen + hangingIndent);
                var space = new string(' ', indentLen);

                // Get a list of words
                foreach (var wordForeach in paragraph.Substring(indentLen).Split(new string[] { " " }, null, StringSplitOptions.RemoveEmptyEntries))
                {
                    var word = wordForeach;
                    if (curLine.Sum(c => c.Length) + space.Length + word.Length > maxWidth)
                    {
                        // Need to wrap
                        if (word.Length > maxWidth)
                        {
                            // This is a very long word
                            // Leave part of the word on the current line but only if at least 2 chars fit
                            if (curLine.Sum(c => c.Length) + space.Length + 2 <= maxWidth)
                            {
                                int length = maxWidth - curLine.Sum(c => c.Length) - space.Length;
                                curLine.Add(space);
                                curLine.Add(word.Substring(0, length));
                                word = word.Substring(length);
                            }
                            // Commit the current line
                            yield return new ConsoleColoredString(curLine.ToArray());

                            // Now append full lines' worth of text until we're left with less than a full line
                            while (indent.Length + word.Length > maxWidth)
                            {
                                yield return new ConsoleColoredString(indent, word.Substring(0, maxWidth - indent.Length));
                                word = word.Substring(maxWidth - indent.Length);
                            }

                            // Start a new line with whatever is left
                            curLine = new List<ConsoleColoredString>();
                            curLine.Add(indent);
                            curLine.Add(word);
                        }
                        else
                        {
                            // This word is not very long and it doesn't fit so just wrap it to the next line
                            yield return new ConsoleColoredString(curLine.ToArray());

                            // Start a new line
                            curLine = new List<ConsoleColoredString>();
                            curLine.Add(indent);
                            curLine.Add(word);
                        }
                    }
                    else
                    {
                        // No need to wrap yet
                        curLine.Add(space);
                        curLine.Add(word);
                    }

                    space = " ";
                }

                yield return new ConsoleColoredString(curLine.ToArray());
            }
        }

        /// <summary>Equivalent to <see cref="System.String.IndexOf(char)"/>.</summary>
        public int IndexOf(char value) { return _text.IndexOf(value); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(string)"/>.</summary>
        public int IndexOf(string value) { return _text.IndexOf(value); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(char,int)"/>.</summary>
        public int IndexOf(char value, int startIndex) { return _text.IndexOf(value, startIndex); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(string,int)"/>.</summary>
        public int IndexOf(string value, int startIndex) { return _text.IndexOf(value, startIndex); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(string,StringComparison)"/>.</summary>
        public int IndexOf(string value, StringComparison comparisonType) { return _text.IndexOf(value, comparisonType); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(char,int,int)"/>.</summary>
        public int IndexOf(char value, int startIndex, int count) { return _text.IndexOf(value, startIndex, count); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(string,int,int)"/>.</summary>
        public int IndexOf(string value, int startIndex, int count) { return _text.IndexOf(value, startIndex, count); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(string,int,StringComparison)"/>.</summary>
        public int IndexOf(string value, int startIndex, StringComparison comparisonType) { return _text.IndexOf(value, startIndex, comparisonType); }
        /// <summary>Equivalent to <see cref="System.String.IndexOf(string,int,int,StringComparison)"/>.</summary>
        public int IndexOf(string value, int startIndex, int count, StringComparison comparisonType) { return _text.IndexOf(value, startIndex, count, comparisonType); }

        /// <summary>
        ///     Returns a new string in which a specified string is inserted at a specified index position in this
        ///     instance.</summary>
        /// <param name="startIndex">
        ///     The zero-based index position of the insertion.</param>
        /// <param name="value">
        ///     The string to insert.</param>
        /// <returns>
        ///     A new string that is equivalent to this instance, but with <paramref name="value"/> inserted at position <paramref
        ///     name="startIndex"/>.</returns>
        public ConsoleColoredString Insert(int startIndex, ConsoleColoredString value)
        {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException("startIndex", "startIndex cannot be negative or greater than the length of the string.");
            return Substring(0, startIndex) + value + Substring(startIndex);
        }

        /// <summary>
        ///     Returns a string array that contains the substrings in this <see cref="ConsoleColoredString"/> that are delimited
        ///     by elements of a specified string array. Parameters specify the maximum number of substrings to return and whether
        ///     to return empty array elements.</summary>
        /// <param name="separator">
        ///     An array of strings that delimit the substrings in this <see cref="ConsoleColoredString"/>, an empty array that
        ///     contains no delimiters, or null.</param>
        /// <param name="count">
        ///     The maximum number of substrings to return, or null to return all.</param>
        /// <param name="options">
        ///     Specify <see cref="System.StringSplitOptions.RemoveEmptyEntries"/> to omit empty array elements from the array
        ///     returned, or <see cref="System.StringSplitOptions.None"/> to include empty array elements in the array
        ///     returned.</param>
        /// <returns>
        ///     A collection whose elements contain the substrings in this <see cref="ConsoleColoredString"/> that are delimited
        ///     by one or more strings in <paramref name="separator"/>.</returns>
        public IEnumerable<ConsoleColoredString> Split(string[] separator, int? count = null, StringSplitOptions options = StringSplitOptions.None)
        {
            if (separator == null)
            {
                if (_text.Length > 0 || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
                    yield return this;
                yield break;
            }
            var index = 0;
            while (true)
            {
                var candidates = separator.Select(sep => new { Separator = sep, MatchIndex = _text.IndexOf(sep, index) }).Where(sep => sep.MatchIndex != -1).ToArray();
                if (!candidates.Any())
                {
                    if (index < _text.Length || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
                        yield return Substring(index);
                    yield break;
                }
                var min = candidates.MinElement(a => a.MatchIndex);
                if (min.MatchIndex != index || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
                    yield return Substring(index, min.MatchIndex - index);
                if (count != null)
                {
                    count = count.Value - 1;
                    if (count.Value == 0)
                        yield break;
                }
                index = min.MatchIndex + min.Separator.Length;
            }
        }

        /// <summary>
        ///     Retrieves a substring from this instance. The substring starts at a specified character position.</summary>
        /// <param name="startIndex">
        ///     The zero-based starting character position of a substring in this instance.</param>
        /// <returns>
        ///     A <see cref="ConsoleColoredString"/> object equivalent to the substring that begins at <paramref
        ///     name="startIndex"/> in this instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     <paramref name="startIndex"/> is less than zero or greater than the length of this instance.</exception>
        public ConsoleColoredString Substring(int startIndex)
        {
            return new ConsoleColoredString(_text.Substring(startIndex), _colors.Subarray(startIndex));
        }

        /// <summary>
        ///     Retrieves a substring from this instance. The substring starts at a specified character position and has a
        ///     specified length.</summary>
        /// <param name="startIndex">
        ///     The zero-based starting character position of a substring in this instance.</param>
        /// <param name="length">
        ///     The number of characters in the substring.</param>
        /// <returns>
        ///     A <see cref="ConsoleColoredString"/> equivalent to the substring of length length that begins at <paramref
        ///     name="startIndex"/> in this instance.</returns>
        public ConsoleColoredString Substring(int startIndex, int length)
        {
            return new ConsoleColoredString(_text.Substring(startIndex, length), _colors.Subarray(startIndex, length));
        }

        /// <summary>Outputs the current <see cref="ConsoleColoredString"/> to the console.</summary>
        internal void writeToConsole(bool stdErr = false)
        {
            int index = 0;
            var console = stdErr ? Console.Error : Console.Out;
            while (index < _text.Length)
            {
                ConsoleColor cc = _colors[index];
                Console.ForegroundColor = cc;
                var origIndex = index;
                do
                    index++;
                while (index < _text.Length && _colors[index] == cc);
                console.Write(_text.Substring(origIndex, index - origIndex));
            }
            Console.ResetColor();
        }

        /// <summary>
        ///     Replaces each format item in a specified string with the string representation of a corresponding object in a
        ///     specified array.</summary>
        /// <param name="format">
        ///     A composite format string.</param>
        /// <param name="args">
        ///     An object array that contains zero or more objects to format.</param>
        /// <returns>
        ///     A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of
        ///     the corresponding objects in <paramref name="args"/>.</returns>
        public static ConsoleColoredString Format(ConsoleColoredString format, params object[] args)
        {
            return ConsoleColoredString.Format(null, format, args);
        }

        /// <summary>
        ///     Replaces the format item in a specified string with the string representation of a corresponding object in a
        ///     specified array. A specified parameter supplies culture-specific formatting information.</summary>
        /// <param name="provider">
        ///     An object that supplies culture-specific formatting information.</param>
        /// <param name="format">
        ///     A composite format string.</param>
        /// <param name="args">
        ///     An object array that contains zero or more objects to format.</param>
        /// <returns>
        ///     A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of
        ///     the corresponding objects in <paramref name="args"/>.</returns>
        public static ConsoleColoredString Format(IFormatProvider provider, ConsoleColoredString format, params object[] args)
        {
            var index = 0;
            var oldIndex = 0;
            var fmt = format._text;
            var result = new List<ConsoleColoredString>();
            var customFormatter = provider == null ? null : provider.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;

            while (index < fmt.Length)
            {
                char ch = fmt[index];
                if (ch == '{')
                {
                    index++;
                    if (index == fmt.Length)
                        throw new FormatException("The specified format string is invalid.");
                    ch = fmt[index];
                    if (ch == '{')
                    {
                        if (index > oldIndex)
                            result.Add(format.Substring(oldIndex, index - oldIndex));
                    }
                    else
                    {
                        ConsoleColor color = format._colors[index - 1];
                        if (index - 1 > oldIndex)
                            result.Add(format.Substring(oldIndex, index - oldIndex - 1));
                        var num = 0;
                        var leftAlign = false;
                        var align = 0;
                        StringBuilder colorBuilder = null, formatBuilder = null;

                        // Syntax: {num[,alignment][/color][:format]}
                        // States: 0 = before first digit of num; 1 = during num; 2 = before align; 3 = during align; 4 = during color; 5 = during format
                        var state = 0;

                        while (true)
                        {
                            if (ch == '}')
                            {
                                if (index + 1 == fmt.Length || fmt[index + 1] != '}')
                                    break;
                                index++;
                            }

                            if ((state == 0 || state == 1) && ch >= '0' && ch <= '9')
                            {
                                num = (num * 10) + (ch - '0');
                                state = 1;
                            }
                            else if (state == 1 && ch == ',')
                                state = 2;
                            else if (state == 2 && ch == '-')
                            {
                                leftAlign = true;
                                state = 3;
                            }
                            else if ((state == 2 || state == 3) && ch >= '0' && ch <= '9')
                            {
                                align = (align * 10) + (ch - '0');
                                state = 3;
                            }
                            else if ((state == 1 || state == 3) && ch == '/')
                            {
                                colorBuilder = new StringBuilder();
                                state = 4;
                            }
                            else if ((state == 1 || state == 3 || state == 4) && ch == ':')
                            {
                                formatBuilder = new StringBuilder();
                                state = 5;
                            }
                            else if (state == 4)
                                colorBuilder.Append(ch);
                            else if (state == 5)
                                formatBuilder.Append(ch);
                            else
                                throw new FormatException("The specified format string is invalid.");

                            index++;
                            if (index == fmt.Length)
                                throw new FormatException("The specified format string is invalid.");
                            ch = fmt[index];
                        }

                        if (num >= args.Length)
                            throw new FormatException("The specified format string references an array index outside the bounds of the supplied arguments.");
                        string str;
                        // Side-effect: This sets “color”
                        if (colorBuilder != null && !Enum.TryParse<ConsoleColor>(str = colorBuilder.ToString(), true, out color))
                            throw new FormatException("The specified format string uses an invalid console color name ({0}).".Fmt(str));

                        object obj = args[num];
                        ConsoleColoredString ccstr;
                        IFormattable objFmt;

                        // Side-effect: This sets “ccstr” inside the if expression
                        if (colorBuilder != null || (ccstr = obj as ConsoleColoredString) == null)
                            ccstr = new ConsoleColoredString(
                                formatBuilder != null && (objFmt = obj as IFormattable) != null ? objFmt.ToString(formatBuilder.ToString(), provider) :
                                formatBuilder != null && customFormatter != null ? customFormatter.Format(formatBuilder.ToString(), obj, provider) :
                                obj.ToString(),
                                color);
                        if (ccstr.Length < align)
                            ccstr = leftAlign ? ccstr + new string(' ', align - ccstr.Length) : new string(' ', align - ccstr.Length) + ccstr;
                        result.Add(ccstr);
                    }
                    oldIndex = index + 1;
                }
                else if (ch == '}')
                {
                    index++;
                    if (index == fmt.Length || ch != '}')
                        throw new FormatException("The specified format string is invalid.");
                    result.Add(format.Substring(oldIndex, index - oldIndex));
                    oldIndex = index + 1;
                }
                index++;
            }
            if (index > oldIndex)
                result.Add(format.Substring(oldIndex, index - oldIndex));
            return new ConsoleColoredString(result);
        }

        /// <summary>Equivalent to <see cref="ConsoleColoredString.Format(ConsoleColoredString,object[])"/>.</summary>
        public ConsoleColoredString Fmt(params object[] args)
        {
            return ConsoleColoredString.Format(null, this, args);
        }

        /// <summary>
        ///     Returns an array describing the color of every character in the current string.</summary>
        /// <returns>
        ///     A copy of the internal color array. Modifying the returned array is safe.</returns>
        public ConsoleColor[] GetColors()
        {
            return _colors.ToArray();
        }

        /// <summary>
        ///     Gets the character and color at a specified character position in the current colored string.</summary>
        /// <param name="index">
        ///     A character position in the current colored string.</param>
        /// <returns>
        ///     A tuple containing a Unicode character and a console color.</returns>
        /// <exception cref="IndexOutOfRangeException">
        ///     <paramref name="index"/> is greater than or equal to the length of this object or less than zero.</exception>
        public ConsoleColoredChar this[int index]
        {
            get
            {
                if (index < 0 || index >= _text.Length)
                    throw new IndexOutOfRangeException("The index must be non-negative and smaller than the length of the string.");
                return new ConsoleColoredChar(_text[index], _colors[index]);
            }
        }
    }

    /// <summary>Contains a character and a console color.</summary>
    public sealed class ConsoleColoredChar
    {
        /// <summary>Gets the character.</summary>
        public char Character { get; private set; }
        /// <summary>Gets the console color.</summary>
        public ConsoleColor Color { get; private set; }
        /// <summary>
        ///     Constructor.</summary>
        /// <param name="character">
        ///     The character.</param>
        /// <param name="color">
        ///     The console color.</param>
        public ConsoleColoredChar(char character, ConsoleColor color)
        {
            Character = character;
            Color = color;
        }
    }
}
