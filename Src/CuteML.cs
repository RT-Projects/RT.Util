using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Util.Collections;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>
    ///     Implements a parser for the minimalist text mark-up language CuteML.</summary>
    /// <remarks>
    ///     <para>
    ///         The “rules” of CuteML are, in summary:</para>
    ///     <list type="bullet">
    ///         <item><description>
    ///             A CuteML tag starts with <c>[</c> and ends with <c>]</c>.</description></item>
    ///         <item><description>
    ///             The character following the <c>[</c> is the tag name. This can be any character except for <c>&lt;</c>. For
    ///             example, the following is legal CuteML: <c>this is [*bold] text.</c></description></item>
    ///         <item><description>
    ///             Optional attribute data may be inserted between the <c>[</c> and the tag name enclosed in <c>&lt;...&gt;</c>.
    ///             For example, the following is legal CuteML: <c>this is [&lt;red&gt;:red] text.</c> This attribute data may not
    ///             contain the characters <c>[</c>, <c>]</c> or <c>&lt;</c>.</description></item>
    ///         <item><description>
    ///             The special tags <c>[(]</c> and <c>[)]</c> can be used to insert an opening and closing literal square
    ///             bracket, respectively.</description></item>
    ///         <item><description>
    ///             The special tag <c>[ ...]</c> (i.e., the tag name is the space character) can be used to insert a matching
    ///             literal square bracket. In other words, <c>[ xyz]</c> is equivalent to
    ///             <c>[(]xyz[)]</c>.</description></item></list></remarks>
    public static class CuteML
    {
        /// <summary>
        ///     Parses the specified CuteML input.</summary>
        /// <param name="input">
        ///     The CuteML text to parse.</param>
        /// <returns>
        ///     The resulting parse-tree.</returns>
        /// <remarks>
        ///     <list type="bullet">
        ///         <item><description>
        ///             Tags are parsed into instances of <see cref="CuteTag"/>.</description></item>
        ///         <item><description>
        ///             The top-level nodes are contained in an instance of <see cref="CuteTag"/> whose <see cref="CuteTag.Tag"/>
        ///             property is set to null.</description></item>
        ///         <item><description>
        ///             All the literal text is parsed into instances of <see cref="CuteText"/>. All continuous text is
        ///             consolidated, so there are no two consecutive <see cref="CuteText"/> instances in any list of
        ///             children.</description></item></list></remarks>
        /// <exception cref="CuteMLParseException">
        ///     Invalid syntax was encountered. The exception object contains the string index at which the error was
        ///     detected.</exception>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="input"/> was null.</exception>
        public static CuteNode ParseCuteML(this string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var curTag = new CuteTag(null, null, 0);
            if (input.Length == 0)
                return curTag;

            var curText = "";
            var curTextIndex = 0;
            var index = 0;
            var stack = new Stack<CuteTag>();

            while (input.Length > 0)
            {
                var pos1 = input.IndexOf('[');
                var pos2 = input.IndexOf(']');

                // If no more tags, we are done
                if (pos1 == -1 && pos2 == -1)
                {
                    curText += input;
                    break;
                }

                var pos = pos1 == -1 ? pos2 : pos2 == -1 ? pos1 : Math.Min(pos1, pos2);

                if (pos > 0)
                {
                    curText += input.Substring(0, pos);
                    index += pos;
                    input = input.Substring(pos);
                }

                switch (input[0])
                {
                    case '[':
                        string attribute = null;
                        int i = 1;
                        if (input[1] == '<')
                        {
                            var close = input.IndexOf('>');
                            if (close == -1)
                                throw new CuteMLParseException("Closing ‘>’ missing.", index + 1, input.Length - 1);
                            attribute = input.Substring(2, close - 2);
                            int j = attribute.IndexOf(ch => ch == '<' || ch == '[' || ch == ']');
                            if (j != -1)
                                throw new CuteMLParseException("‘<’, ‘[’ and ‘]’ not permitted in attribute values.", index + j + 2, 1);
                            i = close + 1;
                        }

                        if (!string.IsNullOrEmpty(curText))
                        {
                            curTag.Add(new CuteText(curText, curTextIndex));
                            curText = "";
                        }
                        stack.Push(curTag);
                        curTag = new CuteTag(input[i], attribute, index);
                        index += i + 1;
                        curTextIndex = index;
                        input = input.Substring(i + 1);
                        continue;

                    case ']':
                        var prevTag = stack.Pop();
                        CuteText text;
                        switch (curTag.Tag)
                        {
                            case ' ':
                                string newText = "";
                                int newTextIndex = curTag.Index;
                                if (prevTag.Children.Count > 0 && (text = prevTag.Children.Last() as CuteText) != null)
                                {
                                    newText = text.Text;
                                    newTextIndex = text.Index;
                                    prevTag.RemoveLast();
                                }
                                newText += "[";
                                if (curTag.Children.Count > 0 && (text = curTag.Children.First() as CuteText) != null)
                                {
                                    newText += text.Text;
                                    curTag.RemoveFirst();
                                }
                                if (curTag.Children.Count > 0)
                                {
                                    prevTag.Add(new CuteText(newText, newTextIndex));
                                    prevTag.AddRange(curTag.Children);
                                }
                                else
                                {
                                    curText = newText + curText;
                                    curTextIndex = newTextIndex;
                                }
                                curText += "]";
                                break;

                            case '(':
                            case ')':
                                curText = "";
                                curTextIndex = curTag.Index;
                                if (prevTag.Children.Count > 0 && (text = prevTag.Children.Last() as CuteText) != null)
                                {
                                    curText = text.Text;
                                    curTextIndex = text.Index;
                                    prevTag.RemoveLast();
                                }
                                curText += curTag.Tag == '(' ? "[" : "]";
                                break;

                            default:
                                if (!string.IsNullOrEmpty(curText))
                                {
                                    curTag.Add(new CuteText(curText, curTextIndex));
                                    curText = "";
                                }
                                prevTag.Add(curTag);
                                curTextIndex = index + 1;
                                break;
                        }
                        curTag = prevTag;
                        index++;
                        input = input.Substring(1);
                        continue;

                    default:
                        throw new CuteMLParseException(@"Character ‘{0}’ unexpected.".Fmt(input[0]), index, 1);
                }
            }

            if (stack.Count > 0)
                throw new CuteMLParseException(@"Closing ‘]’ missing", index, 0, curTag.Index);

            if (!string.IsNullOrEmpty(curText))
                curTag.Add(new CuteText(curText, curTextIndex));

            return curTag;
        }

        /// <summary>
        ///     Escapes the input string such that it can be used in CuteML syntax. The result will have its <c>[</c> and <c>]</c>
        ///     replaced with <c>[(]</c> and <c>[)]</c>.</summary>
        public static string EscapeCuteML(this string input)
        {
            var builder = new StringBuilder();
            while (true)
            {
                var pos1 = input.IndexOf('[');
                var pos2 = input.IndexOf(']');
                if (pos1 == -1 && pos2 == -1)
                    break;
                var pos = pos1 == -1 ? pos2 : pos2 == -1 ? pos1 : Math.Min(pos1, pos2);
                builder.Append(input.Substring(0, pos));
                builder.Append(input[pos] == '[' ? "[(]" : "[)]");
                input = input.Substring(pos + 1);
            }
            builder.Append(input);
            return builder.ToString();
        }

        /// <summary>
        ///     Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which renders a piece of text.</summary>
        /// <typeparam name="TState">
        ///     The type of the text state, e.g. font or color.</typeparam>
        /// <param name="state">
        ///     The state (font, color, etc.) the string is in.</param>
        /// <param name="text">
        ///     The string to render.</param>
        /// <param name="width">
        ///     The measured width of the string.</param>
        public delegate void CuteRender<TState>(TState state, string text, int width);

        /// <summary>
        ///     Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which measures the width of a string.</summary>
        /// <typeparam name="TState">
        ///     The type of the text state, e.g. font or color.</typeparam>
        /// <param name="state">
        ///     The state (font, color etc.) of the text.</param>
        /// <param name="text">
        ///     The text whose width to measure.</param>
        /// <returns>
        ///     The width of the text in any arbitrary unit, as long as the “width” parameter in the call to <see
        ///     cref="WordWrap&lt;TState&gt;"/> is in the same unit.</returns>
        public delegate int CuteMeasure<TState>(TState state, string text);

        /// <summary>
        ///     Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which advances to the next line.</summary>
        /// <typeparam name="TState">
        ///     The type of the text state, e.g. font or color.</typeparam>
        /// <param name="state">
        ///     The state (font, color etc.) of the text.</param>
        /// <param name="newParagraph">
        ///     ‘true’ if a new paragraph begins, ‘false’ if a word is being wrapped within a paragraph.</param>
        /// <param name="indent">
        ///     If <paramref name="newParagraph"/> is false, the indentation of the current paragraph as measured only by its
        ///     leading spaces; otherwise, zero.</param>
        /// <returns>
        ///     The indentation for the next line. Use this to implement, for example, hanging indents.</returns>
        public delegate int CuteNextLine<TState>(TState state, bool newParagraph, int indent);

        /// <summary>
        ///     Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which determines how the text state (font, color
        ///     etc.) changes for a given CuteML tag character. This delegate is called for all tags except for <c>[+...]</c>,
        ///     which is automatically processed to mean “nowrap”.</summary>
        /// <typeparam name="TState">
        ///     The type of the text state, e.g. font or color.</typeparam>
        /// <param name="oldState">
        ///     The previous state (for the parent tag).</param>
        /// <param name="cuteTag">
        ///     The CuteML tag character.</param>
        /// <param name="parameter">
        ///     The contents of the tag attribute, which can be used to parameterize tags.</param>
        /// <returns>
        ///     The next state (return the old state for all tags that should not have a meaning) and an integer indicating the
        ///     amount by which opening this tag has advanced the text position.</returns>
        public delegate (TState newState, int advance) CuteNextState<TState>(TState oldState, char cuteTag, string parameter);

        private sealed class CuteWalkData<TState>
        {
            public bool AtStartOfLine;
            public List<string> WordPieces;
            public List<TState> WordPiecesState;
            public List<int> WordPiecesWidths;
            public int WordPiecesWidthsSum;
            public TState SpaceState;
            public CuteMeasure<TState> Measure;
            public CuteRender<TState> Render;
            public CuteNextLine<TState> AdvanceToNextLine;
            public CuteNextState<TState> NextState;
            public int X, WrapWidth, ActualWidth, CurParagraphIndent;

            public void CuteWalkWordWrap(CuteNode node, TState initialState)
            {
                if (node == null)
                    throw new ArgumentNullException(nameof(node));

                cuteWalkWordWrapRecursive(node, initialState, false);

                if (WordPieces.Count > 0)
                    renderPieces(initialState);
            }

            private void cuteWalkWordWrapRecursive(CuteNode node, TState state, bool curNowrap)
            {
                if (node is CuteTag tag)
                {
                    var newState = state;
                    if (tag.Tag == '+')
                        curNowrap = true;
                    else if (tag.Tag != null)
                    {
                        var tup = NextState(state, tag.Tag.Value, tag.Attribute);
                        newState = tup.newState;
                        X += tup.advance;
                    }
                    foreach (var child in tag.Children)
                        cuteWalkWordWrapRecursive(child, newState, curNowrap);
                }
                else if (node is CuteText text)
                {
                    var txt = text.Text;
                    for (int i = 0; i < txt.Length; i++)
                    {
                        // Check whether we are looking at a whitespace character or not, and if not, find the end of the word.
                        int lengthOfWord = 0;
                        while (lengthOfWord + i < txt.Length && (curNowrap || !char.IsWhiteSpace(txt, lengthOfWord + i)) && txt[lengthOfWord + i] != '\n')
                            lengthOfWord++;

                        if (lengthOfWord > 0)
                        {
                            // We are looking at a word. (It doesn’t matter whether we’re at the beginning of the word or in the middle of one.)
                            retry1:
                            string fragment = txt.Substring(i, lengthOfWord);
                            var fragmentWidth = Measure(state, fragment);
                            retry2:

                            // If we are at the start of a line, and the word itself doesn’t fit on a line by itself, we need to break the word up.
                            if (AtStartOfLine && X + WordPiecesWidthsSum + fragmentWidth > WrapWidth)
                            {
                                // We don’t know exactly where to break the word, so use binary search to discover where that is.
                                if (lengthOfWord > 1)
                                {
                                    lengthOfWord /= 2;
                                    goto retry1;
                                }

                                // If we get to here, ‘WordPieces’ contains as much of the word as fits into one line, and the next letter makes it too long.
                                // If ‘WordPieces’ is empty, we are at the beginning of a paragraph and the first letter already doesn’t fit.
                                if (WordPieces.Count > 0)
                                {
                                    // Render the part of the word that fits on the line and then move to the next line.
                                    renderPieces(state);
                                    advanceToNextLine(state, false);
                                }
                            }
                            else if (!AtStartOfLine && X + Measure(state, " ") + WordPiecesWidthsSum + fragmentWidth > WrapWidth)
                            {
                                // We have already rendered some text on this line, but the word we’re looking at right now doesn’t
                                // fit into the rest of the line, so leave the rest of this line blank and advance to the next line.
                                advanceToNextLine(state, false);

                                // In case the word also doesn’t fit on a line all by itself, go back to top (now that ‘AtStartOfLine’ is true)
                                // where it will check whether we need to break the word apart.
                                goto retry2;
                            }

                            // If we get to here, the current fragment fits on the current line (or it is a single character that overflows
                            // the line all by itself).
                            WordPieces.Add(fragment);
                            WordPiecesState.Add(state);
                            WordPiecesWidths.Add(fragmentWidth);
                            WordPiecesWidthsSum += fragmentWidth;
                            i += lengthOfWord - 1;
                            continue;
                        }

                        // We encounter a whitespace character. All the word pieces fit on the current line, so render them.
                        if (WordPieces.Count > 0)
                        {
                            renderPieces(state);
                            AtStartOfLine = false;
                        }

                        SpaceState = state;

                        if (txt[i] == '\n')
                        {
                            // If the whitespace character is actually a newline, start a new paragraph.
                            advanceToNextLine(state, true);
                        }
                        else if (AtStartOfLine)
                        {
                            // Otherwise, if we are at the beginning of the line, treat this space as the paragraph’s indentation.
                            CurParagraphIndent += renderSpace(state);
                        }
                    }
                }
                else
                    throw new InvalidOperationException("A CuteNode is expected to be either CuteTag or CuteText, not {0}.".Fmt(node.GetType().FullName));
            }

            private void advanceToNextLine(TState state, bool newParagraph)
            {
                if (newParagraph)
                    CurParagraphIndent = 0;
                X = AdvanceToNextLine(state, newParagraph, CurParagraphIndent);
                AtStartOfLine = true;
            }

            private int renderSpace(TState state)
            {
                var w = Measure(state, " ");
                Render(state, " ", w);
                X += w;
                ActualWidth = Math.Max(ActualWidth, X);
                return w;
            }

            private void renderPieces(TState state)
            {
                // Add a space if we are not at the beginning of the line.
                if (!AtStartOfLine)
                    renderSpace(SpaceState);
                for (int j = 0; j < WordPieces.Count; j++)
                    Render(WordPiecesState[j], WordPieces[j], WordPiecesWidths[j]);
                X += WordPiecesWidthsSum;
                ActualWidth = Math.Max(ActualWidth, X);
                WordPieces.Clear();
                WordPiecesState.Clear();
                WordPiecesWidths.Clear();
                WordPiecesWidthsSum = 0;
            }
        }

        /// <summary>
        ///     Word-wraps a given piece of CuteML, assuming that it is linearly flowing text. Newline (<c>\n</c>) characters can
        ///     be used to split the text into multiple paragraphs. The special <c>[+...]</c> tag marks text that may not be
        ///     broken by wrapping (effectively turning all spaces into non-breaking spaces).</summary>
        /// <typeparam name="TState">
        ///     The type of the text state that a tag can change, e.g. font or color.</typeparam>
        /// <param name="node">
        ///     The root node of the CuteML tree to word-wrap.</param>
        /// <param name="initialState">
        ///     The initial text state.</param>
        /// <param name="wrapWidth">
        ///     The maximum width at which to word-wrap. This width can be measured in any unit, as long as <paramref
        ///     name="measure"/> uses the same unit.</param>
        /// <param name="measure">
        ///     A delegate that measures the width of any piece of text.</param>
        /// <param name="render">
        ///     A delegate that is called whenever a piece of text is ready to be rendered.</param>
        /// <param name="advanceToNextLine">
        ///     A delegate that is called to advance to the next line.</param>
        /// <param name="nextState">
        ///     A delegate that determines how each CuteML tag character modifies the state (font, color etc.).</param>
        /// <returns>
        ///     The maximum width of the text.</returns>
        public static int WordWrap<TState>(CuteNode node, TState initialState, int wrapWidth,
            CuteMeasure<TState> measure, CuteRender<TState> render, CuteNextLine<TState> advanceToNextLine, CuteNextState<TState> nextState)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (wrapWidth <= 0)
                throw new ArgumentException("Wrap width must be greater than zero.", nameof(wrapWidth));
            var data = new CuteWalkData<TState>
            {
                AtStartOfLine = true,
                WordPieces = new List<string>(),
                WordPiecesState = new List<TState>(),
                WordPiecesWidths = new List<int>(),
                WordPiecesWidthsSum = 0,
                Measure = measure,
                Render = render,
                AdvanceToNextLine = advanceToNextLine,
                NextState = nextState,
                X = 0,
                WrapWidth = wrapWidth,
                ActualWidth = 0
            };
            data.CuteWalkWordWrap(node, initialState);
            return data.ActualWidth;
        }
    }

    /// <summary>Contains a node in the CuteML parse tree.</summary>
    public abstract class CuteNode
    {
        /// <summary>Returns the CuteML parse tree as XML.</summary>
        public abstract object ToXml();

        /// <summary>The index in the original string where this node starts.</summary>
        public int Index { get; protected set; }

        /// <summary>Determines whether this node contains any textual content.</summary>
        public abstract bool HasText { get; }

        /// <summary>Gets a reference to the parent node of this node. The root node is the only one for which this property is null.</summary>
        public CuteTag Parent { get; internal set; }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="index">
        ///     The index within the original string where this node starts.</param>
        public CuteNode(int index) { Index = index; }

        /// <summary>Gets the text of this node and/or sub-nodes concatenated into one string.</summary>
        public string ToString(bool excludeSyntax)
        {
            if (excludeSyntax)
            {
                var builder = new StringBuilder();
                textify(builder);
                return builder.ToString();
            }
            else
                return ToString();
        }

        internal abstract void textify(StringBuilder builder);

        /// <summary>
        ///     Generates a sequence of <see cref="ConsoleColoredString"/>s from a CuteML parse tree by word-wrapping the output
        ///     at a specified character width.</summary>
        /// <param name="wrapWidth">
        ///     The number of characters at which to word-wrap the output.</param>
        /// <param name="hangingIndent">
        ///     The number of spaces to add to each line except the first of each paragraph, thus creating a hanging
        ///     indentation.</param>
        /// <returns>
        ///     The sequence of <see cref="ConsoleColoredString"/>s generated from the CuteML parse tree.</returns>
        /// <remarks>
        ///     <para>
        ///         The following CuteML tags are processed:</para>
        ///     <list type="bullet">
        ///         <item><description>
        ///             <c>[&lt;color&gt;:...]</c> = use the specified console color, for example
        ///             <c>[&lt;darkred&gt;:...]</c>.</description></item>
        ///         <item><description>
        ///             <c>[*blah]</c> = Brightens the current color, for example turning dark-red into red or light-gray into
        ///             white.</description></item>
        ///         <item><description>
        ///             <c>[-blah]</c> = Darkens the current color, for example turning red into dark-red or white into
        ///             light-gray.</description></item>
        ///         <item><description>
        ///             <c>[.blah]</c> = Creates a bullet point. Surround a whole paragraph with this to add the bullet point and
        ///             indent the paragraph. Use this to create bulleted lists. The default bullet point character is <c>*</c>;
        ///             you can use an attribute to specify another one, for example <c>[&lt;-&gt;.blah]</c>.</description></item>
        ///         <item><description>
        ///             <c>[+blah]</c> = Suppresses word-wrapping within a certain stretch of text. In other words, the contents
        ///             of a <c>[+...]</c> tag are treated as if they were a single word. Use this in preference to U+00A0
        ///             (no-break space) as it is more explicit and more future-compatible in case hyphenation is ever implemented
        ///             here.</description></item></list>
        ///     <para>
        ///         Text which is not inside a color tag defaults to light gray.</para></remarks>
        public IEnumerable<ConsoleColoredString> ToConsoleColoredStrings(int wrapWidth = int.MaxValue, int hangingIndent = 0)
        {
            var results = new List<ConsoleColoredString> { ConsoleColoredString.Empty };
            CuteML.WordWrap(this, new CuteWordWrapState(ConsoleColor.Gray, 0), wrapWidth,
                (state, text) => text.Length,
                (state, text, width) => { results[results.Count - 1] += new ConsoleColoredString(text, state.Color); },
                (state, newParagraph, indent) =>
                {
                    var s = (newParagraph ? 0 : indent + hangingIndent) + state.Indent;
                    results.Add(new ConsoleColoredString(new string(' ', s), state.Color));
                    return s;
                },
                (state, tag, parameter) =>
                {
                    bool curLight = state.Color >= ConsoleColor.DarkGray;
                    switch (tag)
                    {
                        case ':':
                            ConsoleColor color;
                            if (!Enum.TryParse(parameter, true, out color))
                                throw new InvalidOperationException("“{0}” is not a valid ConsoleColor value.".Fmt(parameter));
                            return (state.SetColor(color), 0);
                        case '*':
                            return (curLight ? state : state.SetColor((ConsoleColor) ((int) state.Color + 8)), 0);
                        case '-':
                            return (curLight ? state.SetColor((ConsoleColor) ((int) state.Color - 8)) : state, 0);
                        case '.':
                            var bullet = (parameter ?? "*") + " ";
                            results[results.Count - 1] += new ConsoleColoredString(bullet, state.Color);
                            return (state.SetIndent(state.Indent + bullet.Length), 2);
                    }
                    return (state, 0);
                });
            if (results.Last().Length == 0)
                results.RemoveAt(results.Count - 1);
            return results;
        }

        private class CuteWordWrapState
        {
            public ConsoleColor Color { get; private set; }
            public int Indent { get; private set; }
            public CuteWordWrapState(ConsoleColor color, int indent) { Color = color; Indent = indent; }
            public CuteWordWrapState SetColor(ConsoleColor color) { return new CuteWordWrapState(color, Indent); }
            public CuteWordWrapState SetIndent(int indent) { return new CuteWordWrapState(Color, indent); }
            public override string ToString() { return "Color={0}, Indent={1}".Fmt(Color, Indent); }
        }
    }

    /// <summary>Represents a node in the CuteML parse tree that corresponds to a CuteML tag or the top-level node.</summary>
    public sealed class CuteTag : CuteNode
    {
        /// <summary>
        ///     Adds a new child node to this tag’s children.</summary>
        /// <param name="child">
        ///     The child node to add.</param>
        internal void Add(CuteNode child) { child.Parent = this; _children.Add(child); }

        /// <summary>
        ///     Adds the specified child nodes to this tag’s children.</summary>
        /// <param name="children">
        ///     The child nodes to add.</param>
        internal void AddRange(IEnumerable<CuteNode> children)
        {
            foreach (var child in children)
            {
                child.Parent = this;
                _children.Add(child);
            }
        }

        /// <summary>Removes the first child node.</summary>
        internal void RemoveFirst() { _children.RemoveAt(0); }

        /// <summary>Removes the last child node.</summary>
        internal void RemoveLast() { _children.RemoveAt(_children.Count - 1); }

        /// <summary>The children of this node.</summary>
        public ReadOnlyCollection<CuteNode> Children { get { return _children.AsReadOnly(ref _childrenCache); } }
        private ReadOnlyCollection<CuteNode> _childrenCache;

        /// <summary>The underlying collection containing the children of this node.</summary>
        private List<CuteNode> _children;

        /// <summary>The attribute data associated with the tag, or null if no attribute data was specified.</summary>
        public string Attribute { get; private set; }

        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return _children.Any(child => child.HasText); } }

        /// <summary>The character that constitutes the tag name (e.g. <c>*</c>), or null if this is the top-level node.</summary>
        public char? Tag { get; private set; }

        /// <summary>
        ///     Constructs a new CuteML parse-tree node that represents a CuteML tag.</summary>
        /// <param name="tag">
        ///     The character that constitutes the tag name (e.g. <c>*</c>).</param>
        /// <param name="attribute">
        ///     The attribute string provided with the tag, or null if none.</param>
        /// <param name="index">
        ///     The index in the original string where this tag was opened.</param>
        public CuteTag(char? tag, string attribute, int index) : base(index) { Tag = tag; Attribute = attribute; _children = new List<CuteNode>(); }

        /// <summary>Returns an XML representation of this CuteML node.</summary>
        public override object ToXml()
        {
            var elem = Tag == null
                ? new XElement("root", _children.Select(child => child.ToXml()))
                : new XElement("tag", new XAttribute("character", Tag.Value), _children.Select(child => child.ToXml()));
            if (Attribute != null)
                elem.Add(new XAttribute("attribute", Attribute));
            return elem;
        }

        /// <summary>Converts the CuteML parse tree back into CuteML mark-up.</summary>
        public override string ToString()
        {
            var builder = new StringBuilder();
            if (Tag != null)
                builder.Append("[" + Tag.Value);
            foreach (var child in _children)
                builder.Append(child.ToString());
            if (Tag != null)
                builder.Append("]");
            return builder.ToString();
        }

        internal override void textify(StringBuilder builder)
        {
            foreach (var child in _children)
                child.textify(builder);
        }
    }

    /// <summary>Represents a node in the CuteML parse tree that corresponds to a piece of text.</summary>
    public sealed class CuteText : CuteNode
    {
        /// <summary>The text contained in this node.</summary>
        public string Text { get; private set; }

        /// <summary>
        ///     Constructs a new CuteML text node.</summary>
        /// <param name="text">
        ///     The text for this node to contain.</param>
        /// <param name="index">
        ///     The index in the original string where this text starts.</param>
        public CuteText(string text, int index = 0)
            : base(index)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text), "The 'text' for a CuteText node cannot be null.");
        }

        /// <summary>Returns an XML representation of this EggsML node.</summary>
        public override object ToXml() => Text;

        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText => Text != null && Text.Length > 0;

        /// <summary>Returns the contained text in CuteML-escaped form.</summary>
        public override string ToString() => Text.EscapeCuteML();

        internal override void textify(StringBuilder builder) { builder.Append(Text); }
    }

    /// <summary>Represents a parse error encountered by the <see cref="CuteML"/> parser.</summary>
    [Serializable]
    public sealed class CuteMLParseException : Exception
    {
        /// <summary>The character index into the original string where the error occurred.</summary>
        public int Index { get; private set; }

        /// <summary>The length of the text in the original string where the error occurred.</summary>
        public int Length { get; private set; }

        /// <summary>
        ///     The character index of an earlier position in the original string where the error started (e.g. the start of a tag
        ///     that is missing its end tag).</summary>
        public int? FirstIndex { get; private set; }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="message">
        ///     Message.</param>
        /// <param name="index">
        ///     The character index into the original string where the error occurred.</param>
        /// <param name="length">
        ///     The length of the text in the original string where the error occurred.</param>
        /// <param name="firstIndex">
        ///     The character index of an earlier position in the original string where the error started (e.g. the start of a tag
        ///     that is missing its end tag).</param>
        /// <param name="inner">
        ///     An inner exception to pass to the base Exception class.</param>
        public CuteMLParseException(string message, int index, int length, int? firstIndex = null, Exception inner = null) : base(message, inner) { Index = index; Length = length; FirstIndex = firstIndex; }
    }
}
