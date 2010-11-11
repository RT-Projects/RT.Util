using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>Implements a parser for the minimalist text mark-up language EggsML.</summary>
    /// <remarks>
    ///     <para>The “rules” of EggsML are, in summary:</para>
    ///     <list type="bullet">
    ///         <item><description>In EggsML, the following non-alphanumeric characters are “special” (have meaning): <c>~ @ # $ % ^ &amp; * _ = + / \ | [ ] { } &lt; &gt; ` "</c></description></item>
    ///         <item><description>All other characters are always literal.</description></item>
    ///         <item><description>All the special characters can be escaped by doubling them.</description></item>
    ///         <item><description>The characters <c>~ @ # $ % ^ &amp; * _ = + / \ | [ { &lt;</c> can be used to open a “tag”.</description></item>
    ///         <item><description>Tags that start with <c>[ { &lt;</c> are closed with <c>] } &gt;</c>. All other tags are closed with the same character.</description></item>
    ///         <item><description>Tags can be nested arbitrarily. In order to start a nested tag of the same character as its immediate parent, triple the tag character. For example, <c>*one ***two* three*</c> contains an asterisk tag nested inside another asterisk tag, while <c>*one *two* three*</c> would be parsed as two asterisk tags, one containing “one ” and the other containing “ three”.</description></item>
    ///         <item><description>The backtick character (<c>`</c>) can be used to “unjoin” multiple copies of the same character. For example, <c>**</c> is a literal asterisk, but <c>*`*</c> is an empty tag containing no text.</description></item>
    ///         <item><description>The double-quote character (<c>"</c>) can be used to escape long strings of special characters, e.g. URLs.</description></item>
    ///     </list>
    ///</remarks>
    public static class EggsML
    {
        /// <summary>Returns an array containing all characters that have a special meaning in EggsML.</summary>
        public static char[] SpecialCharacters
        {
            get
            {
                if (_specialCharacters == null)
                    _specialCharacters = "~@#$%^&*_=+/\\[]{}<>|`\"".ToCharArray();
                return _specialCharacters;
            }
        }
        private static char[] _specialCharacters = null;

        /// <summary>Parses the specified EggsML input.</summary>
        /// <param name="input">The EggsML text to parse.</param>
        /// <returns>The resulting parse-tree.</returns>
        /// <remarks>
        ///     <list type="bullet">
        ///         <description><item>Tags are parsed into instances of <see cref="EggsTag"/>.</item></description>
        ///         <description><item>The top-level nodes are contained in an instance of <see cref="EggsTag"/> whose <see cref="EggsTag.Tag"/> property is set to null.</item></description>
        ///         <description><item>All the literal text is parsed into instances of <see cref="EggsText"/>. All continuous text is consolidated, so there are no two consecutive EggsText instances in any list of children.</item></description>
        ///     </list>
        /// </remarks>
        /// <exception cref="EggsMLParseException">Invalid syntax was encountered. The exception object contains the string index at which the error was detected.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> was null.</exception>
        public static EggsNode Parse(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            var curTag = new EggsTag(null, 0);
            if (input.Length == 0)
                return curTag;

            var curText = "";
            var curTextIndex = 0;
            var index = 0;
            var stack = new Stack<EggsTag>();

            while (input.Length > 0)
            {
                var pos = input.IndexOf(ch => SpecialCharacters.Contains(ch));

                // If no more special characters, we are done
                if (pos == -1)
                {
                    curText += input;
                    break;
                }

                // Find out the length of a run of special characters
                var idx = pos + 1;
                while (idx < input.Length && input[idx] == input[pos])
                    idx++;
                int runLength = idx - pos;

                if (runLength % 2 == 0)
                {
                    curText += input.Substring(0, pos) + new string(input[pos], runLength / 2);
                    input = input.Substring(idx);
                    index += idx;
                    continue;
                }

                index += pos;
                if (runLength > 3 && input[pos] != '"')
                    throw new EggsMLParseException("Five or more consecutive same characters not allowed unless number is even.", index);
                if (runLength == 3 && (alwaysCloses(input[pos]) || input[pos] == '`'))
                    throw new EggsMLParseException("Three consecutive same closing-tag characters or backticks not allowed.", index);

                if (pos > 0)
                {
                    curText += input.Substring(0, pos);
                    input = input.Substring(pos);
                    continue;
                }

                switch (input[0])
                {
                    case '`':
                        input = input.Substring(1);
                        index++;
                        continue;

                    case '"':
                        pos = input.IndexOf('"', 1);
                        if (pos == -1)
                            throw new EggsMLParseException(@"Closing '""' missing", index + 1);
                        while (pos < input.Length - 1 && input[pos + 1] == '"')
                        {
                            pos = input.IndexOf('"', pos + 2);
                            if (pos == -1)
                                throw new EggsMLParseException(@"Closing '""' missing", index + 1);
                        }
                        curText += input.Substring(1, pos - 1).Replace("\"\"", "\"");
                        input = input.Substring(pos + 1);
                        index += pos + 1;
                        continue;

                    default:
                        // Are we opening a new tag?
                        if ((runLength == 3 || alwaysOpens(input[0]) || input[0] != opposite(curTag.Tag)) && !alwaysCloses(input[0]))
                        {
                            if (!string.IsNullOrEmpty(curText))
                                curTag.Add(new EggsText(curText, curTextIndex));
                            stack.Push(curTag);
                            curTag = new EggsTag(input[0], index);
                            index += runLength;
                            curText = "";
                            curTextIndex = index;
                            input = input.Substring(runLength);
                            continue;
                        }
                        // Are we closing a tag?
                        else if (input[0] == opposite(curTag.Tag))
                        {
                            if (!string.IsNullOrEmpty(curText))
                                curTag.Add(new EggsText(curText, curTextIndex));
                            var prevTag = stack.Pop();
                            prevTag.Add(curTag);
                            curTag = prevTag;
                            index++;
                            curText = "";
                            curTextIndex = index;
                            input = input.Substring(1);
                            continue;
                        }
                        else if (alwaysCloses(input[0]))
                            throw new EggsMLParseException(@"Tag '{0}' unexpected".Fmt(input[0]), index);
                        throw new EggsMLParseException(@"Character '{0}' unexpected".Fmt(input[0]), index);
                }
            }

            if (stack.Count > 0)
                throw new EggsMLParseException(@"Closing '{0}' missing".Fmt(opposite(curTag.Tag)), curTag.Index);

            if (!string.IsNullOrEmpty(curText))
                curTag.Add(new EggsText(curText, curTextIndex));

            return curTag;
        }

        /// <summary>Escapes the input string such that it can be used in EggsML syntax. The result will either have no special characters in it or be entirely enclosed in double-quotes.</summary>
        public static string Escape(string input)
        {
            if (!input.Any(ch => SpecialCharacters.Contains(ch)))
                return input;
            return @"""" + input.Replace(@"""", @"""""") + @"""";
        }

        internal static char? opposite(char? p)
        {
            if (p == '[') return ']';
            if (p == '<') return '>';
            if (p == '{') return '}';
            return p;
        }

        internal static bool alwaysOpens(char? p) { return p == '[' || p == '<' || p == '{'; }
        private static bool alwaysCloses(char? p) { return p == ']' || p == '>' || p == '}'; }

        /// <summary>Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which renders a piece of text.</summary>
        /// <typeparam name="TState">The type of the text state, e.g. font or color.</typeparam>
        /// <param name="state">The state (font, color, etc.) the string is in.</param>
        /// <param name="text">The string to render.</param>
        /// <param name="width">The measured width of the string.</param>
        public delegate void EggRender<TState>(TState state, string text, int width);

        /// <summary>Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which measures the width of a string.</summary>
        /// <typeparam name="TState">The type of the text state, e.g. font or color.</typeparam>
        /// <param name="state">The state (font, color etc.) of the text.</param>
        /// <param name="text">The text whose width to measure.</param>
        /// <returns>The width of the text in any arbitrary unit, as long as the “width” parameter in the call to
        /// <see cref="WordWrap&lt;TState&gt;"/> is in the same unit.</returns>
        public delegate int EggMeasure<TState>(TState state, string text);

        /// <summary>Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which advances to the next line.</summary>
        /// <typeparam name="TState">The type of the text state, e.g. font or color.</typeparam>
        /// <param name="state">The state (font, color etc.) of the text.</param>
        /// <param name="newParagraph">‘true’ if a new paragraph begins, ‘false’ if a word is being wrapped within a paragraph.</param>
        /// <param name="indent">If <paramref name="newParagraph"/> is false, the indentation of the current paragraph as measured only by its leading spaces; otherwise, zero.</param>
        /// <returns>The indentation for the next line. Use this to implement, for example, hanging indents.</returns>
        public delegate int EggNextLine<TState>(TState state, bool newParagraph, int indent);

        /// <summary>Provides a delegate for <see cref="WordWrap&lt;TState&gt;"/> which determines how the text state (font, color etc.)
        /// changes for a given EggsML tag character.</summary>
        /// <typeparam name="TState">The type of the text state, e.g. font or color.</typeparam>
        /// <param name="oldState">The previous state (for the parent tag).</param>
        /// <param name="eggTag">The EggsML tag character.</param>
        /// <returns>The next state (return the old state for all tags that should not have a meaning) and an integer indicating the amount by which opening this tag has advanced the text position.</returns>
        public delegate Tuple<TState, int> EggNextState<TState>(TState oldState, char eggTag);

        private sealed class eggWalkData<TState>
        {
            public bool AtStartOfLine;
            public List<string> WordPieces;
            public List<TState> WordPiecesState;
            public List<int> WordPiecesWidths;
            public int WordPiecesWidthsSum;
            public EggMeasure<TState> Measure;
            public EggRender<TState> Render;
            public EggNextLine<TState> AdvanceToNextLine;
            public EggNextState<TState> NextState;
            public int X, WrapWidth, ActualWidth, CurParagraphIndent;

            public void EggWalkWordWrap(EggsNode node, TState initialState)
            {
                if (node == null)
                    throw new ArgumentNullException("node");

                eggWalkWordWrapRecursive(node, initialState, false);

                if (WordPieces.Count > 0)
                    renderPieces(initialState);
            }

            private void eggWalkWordWrapRecursive(EggsNode node, TState state, bool curNowrap)
            {
                var tag = node as EggsTag;
                if (tag != null)
                {
                    var newState = state;
                    if (tag.Tag != null)
                    {
                        var tup = NextState(state, tag.Tag.Value);
                        newState = tup.Item1;
                        X += tup.Item2;
                        if (tag.Tag == '+')
                            curNowrap = true;
                    }
                    foreach (var child in tag.Children)
                        eggWalkWordWrapRecursive(child, newState, curNowrap);
                }
                else if (node is EggsText)
                {
                    var txt = ((EggsText) node).Text;
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
                    throw new InvalidOperationException("An EggsNode is expected to be either EggsTag or EggsText, not {0}.".Fmt(node.GetType().FullName));
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
                    renderSpace(state);
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

        /// <summary>Word-wraps a given piece of EggsML, assuming that it is linearly flowing text. Newline (\n) characters can be used to split the text into multiple paragraphs.</summary>
        /// <typeparam name="TState">The type of the text state that an EggsML can change, e.g. font or color.</typeparam>
        /// <param name="node">The root node of the EggsML tree to word-wrap.</param>
        /// <param name="initialState">The initial text state.</param>
        /// <param name="wrapWidth">The maximum width at which to word-wrap. This width can be measured in any unit,
        /// as long as <paramref name="measure"/> uses the same unit.</param>
        /// <param name="measure">A delegate that measures the width of any piece of text.</param>
        /// <param name="render">A delegate that is called whenever a piece of text is ready to be rendered.</param>
        /// <param name="advanceToNextLine">A delegate that is called to advance to the next line.</param>
        /// <param name="nextState">A delegate that determines how each EggsML tag character modifies the state (font, color etc.).</param>
        /// <returns>The maximum width of the text.</returns>
        public static int WordWrap<TState>(EggsNode node, TState initialState, int wrapWidth,
            EggMeasure<TState> measure, EggRender<TState> render, EggNextLine<TState> advanceToNextLine, EggNextState<TState> nextState)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            if (wrapWidth <= 0)
                throw new ArgumentException("Wrap width must be greater than zero.");
            var data = new eggWalkData<TState>
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
            data.EggWalkWordWrap(node, initialState);
            return data.ActualWidth;
        }
    }

    /// <summary>Contains a node in the EggsML parse tree.</summary>
    public abstract class EggsNode
    {
        /// <summary>Returns the EggsML parse tree as XML.</summary>
        public abstract object ToXml();

        /// <summary>The index in the original string where this node starts.</summary>
        public int Index { get; protected set; }

        /// <summary>Determines whether this node contains any textual content.</summary>
        public abstract bool HasText { get; }

        /// <summary>Gets a reference to the parent node of this node. The root node is the only one for which this property is null.</summary>
        public EggsTag Parent { get; internal set; }

        /// <summary>Constructor.</summary>
        /// <param name="index">The index within the original string where this node starts.</param>
        public EggsNode(int index) { Index = index; }

        /// <summary>Turns a list of child nodes into EggsML mark-up.</summary>
        /// <param name="children">List of children to turn into mark-up.</param>
        /// <param name="tag">If non-null, assumes we are directly inside a tag with the specified character, causing necessary escaping to be performed.</param>
        /// <returns>EggsML mark-up representing the same tree structure as this node.</returns>
        protected static string stringify(List<EggsNode> children, char? tag)
        {
            if (children == null || children.Count == 0)
                return "";

            var sb = new StringBuilder();

            for (int i = 0; i < children.Count; i++)
            {
                var childStr = children[i].ToString();
                if (string.IsNullOrEmpty(childStr))
                    continue;

                // If the item is a tag, and it is the same tag character as the current one, we need to escape it by tripling it
                if (tag != null && children[i] is EggsTag && ((EggsTag) children[i]).Tag == tag && !EggsML.alwaysOpens(tag))
                    sb.Append(new string(tag.Value, 2));
                sb.Append(childStr);
            }
            return sb.ToString();
        }

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
    }

    /// <summary>Represents a node in the EggsML parse tree that corresponds to an EggsML tag or the top-level node.</summary>
    public sealed class EggsTag : EggsNode
    {
        /// <summary>Adds a new child node to this tag’s children.</summary>
        /// <param name="child">The child node to add.</param>
        internal void Add(EggsNode child) { child.Parent = this; _children.Add(child); }

        /// <summary>The children of this node.</summary>
        public ReadOnlyCollection<EggsNode> Children { get { return _children.AsReadOnly(ref _childrenCache); } }
        private ReadOnlyCollection<EggsNode> _childrenCache;

        /// <summary>The underlying collection containing the children of this node.</summary>
        private List<EggsNode> _children;

        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return _children.Any(child => child.HasText); } }

        /// <summary>The character used to open the tag (e.g. “[”), or null if this is the top-level node.</summary>
        public char? Tag { get; private set; }

        /// <summary>Constructs a new EggsML parse-tree node that represents an EggsML tag.</summary>
        /// <param name="tag">The character used to open the tag (e.g. '[').</param>
        /// <param name="index">The index in the original string where this tag was opened.</param>
        public EggsTag(char? tag, int index) : base(index) { Tag = tag; _children = new List<EggsNode>(); }

        /// <summary>Reconstructs the original EggsML that is represented by this node.</summary>
        /// <remarks>This does not necessarily return the same EggsML that was originally parsed. For example, redundant uses of the "`" character are removed.</remarks>
        public override string ToString()
        {
            if (_children.Count == 0)
                return Tag == null ? "" : EggsML.alwaysOpens(Tag) ? Tag.ToString() + EggsML.opposite(Tag) : Tag + "`" + Tag;

            var childrenStr = stringify(_children, Tag);
            if (Tag == null)
                return childrenStr;

            return childrenStr.StartsWith(Tag)
                ? childrenStr.EndsWith(EggsML.opposite(Tag))
                    ? Tag + "`" + childrenStr + "`" + EggsML.opposite(Tag)
                    : Tag + "`" + childrenStr + EggsML.opposite(Tag)
                : childrenStr.EndsWith(EggsML.opposite(Tag))
                    ? Tag + childrenStr + "`" + EggsML.opposite(Tag)
                    : Tag + childrenStr + EggsML.opposite(Tag);
        }

        /// <summary>Returns an XML representation of this EggsML node.</summary>
        public override object ToXml()
        {
            string tagName;
            switch (Tag)
            {
                case null: tagName = "root"; break;
                case '~': tagName = "tilde"; break;
                case '@': tagName = "at"; break;
                case '#': tagName = "hash"; break;
                case '$': tagName = "dollar"; break;
                case '%': tagName = "percent"; break;
                case '^': tagName = "hat"; break;
                case '&': tagName = "and"; break;
                case '*': tagName = "star"; break;
                case '_': tagName = "underscore"; break;
                case '=': tagName = "equals"; break;
                case '+': tagName = "plus"; break;
                case '/': tagName = "slash"; break;
                case '\\': tagName = "backslash"; break;
                case '[': tagName = "square"; break;
                case '{': tagName = "curly"; break;
                case '<': tagName = "angle"; break;
                case '|': tagName = "pipe"; break;
                default:
                    throw new ArgumentException("Unexpected tag character ‘{0}’.".Fmt(Tag), "Tag");
            }
            return new XElement(tagName, _children.Select(child => child.ToXml()));
        }

        internal override void textify(StringBuilder builder)
        {
            foreach (var child in _children)
                child.textify(builder);
        }
    }

    /// <summary>Represents a node in the EggsML parse tree that corresponds to a piece of text.</summary>
    public sealed class EggsText : EggsNode
    {
        /// <summary>The text contained in this node.</summary>
        public string Text { get; private set; }

        /// <summary>Constructs a new EggsML text node.</summary>
        /// <param name="text">The text for this node to contain.</param>
        /// <param name="index">The index in the original string where this text starts.</param>
        public EggsText(string text, int index)
            : base(index)
        {
            if (text == null)
                throw new ArgumentNullException("text", "The 'text' for an EggsText node cannot be null.");
            Text = text;
        }

        /// <summary>Reconstructs the original EggsML that is represented by this node.</summary>
        /// <remarks>This does not necessarily return the same EggsML that was originally parsed. For example, redundant uses of the "`" character are removed.</remarks>
        public override string ToString()
        {
            if (Text.Where(ch => EggsML.SpecialCharacters.Contains(ch) && ch != '"').Take(3).Count() >= 3)
                return string.Concat("\"", Text.Replace("\"", "\"\""), "\"");
            return new string(Text.SelectMany(ch => EggsML.SpecialCharacters.Contains(ch) ? new char[] { ch, ch } : new char[] { ch }).ToArray());
        }

        /// <summary>Returns an XML representation of this EggsML node.</summary>
        public override object ToXml() { return Text; }

        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return Text != null && Text.Length > 0; } }

        internal override void textify(StringBuilder builder) { builder.Append(Text); }
    }

    /// <summary>Represents a parse error encountered by the <see cref="EggsML"/> parser.</summary>
    [Serializable]
    public sealed class EggsMLParseException : Exception
    {
        /// <summary>The character index into the original string where the error occurred.</summary>
        public int Index { get; private set; }

        /// <summary>Constructor.</summary>
        /// <param name="message">Message.</param>
        /// <param name="index">The character index into the original string where the error occurred.</param>
        public EggsMLParseException(string message, int index) : base(message) { Index = index; }

        /// <summary>Constructor.</summary>
        /// <param name="message">Message.</param>
        /// <param name="index">The character index into the original string where the error occurred.</param>
        /// <param name="inner">An inner exception to pass to the base Exception class.</param>
        public EggsMLParseException(string message, int index, Exception inner) : base(message, inner) { Index = index; }
    }
}
