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
        ///         <description><item>The top-level nodes are contained in an instance of <see cref="EggsGroup"/>.</item></description>
        ///         <description><item>Both of the above implement <see cref="EggsContainer"/> and thus have children.</item></description>
        ///         <description><item>All the literal text is parsed into instances of <see cref="EggsText"/>. All continuous text is consolidated, so there are no two consecutive EggsText instances in any list of children.</item></description>
        ///     </list>
        /// </remarks>
        /// <exception cref="EggsMLParseException">Invalid syntax was encountered. The exception object contains the string index at which the error was detected.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> was null.</exception>
        public static EggsNode Parse(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            EggsContainer curTag = new EggsGroup(0);
            if (input.Length == 0)
                return curTag;

            var curText = "";
            var curTextIndex = 0;
            var index = 0;
            var stack = new Stack<EggsContainer>();

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
                        if ((runLength == 3 || alwaysOpens(input[0]) || !(curTag is EggsTag) || input[0] != opposite(((EggsTag) curTag).Tag)) && !alwaysCloses(input[0]))
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
                        else if (curTag is EggsTag && input[0] == opposite(((EggsTag) curTag).Tag))
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
                throw new EggsMLParseException(@"Closing '{0}' missing".Fmt(opposite(((EggsTag) curTag).Tag)), curTag.Index);

            if (!string.IsNullOrEmpty(curText))
                curTag.Add(new EggsText(curText, curTextIndex));

            return curTag;
        }

        /// <summary>Escapes the input string such that it can be used in EggsML syntax.</summary>
        public static string Escape(string input)
        {
            return @"""" + input.Replace(@"""", @"""""") + @"""";
        }

        internal static char opposite(char p)
        {
            if (p == '[') return ']';
            if (p == '<') return '>';
            if (p == '{') return '}';
            return p;
        }

        internal static bool alwaysOpens(char p) { return p == '[' || p == '<' || p == '{'; }
        private static bool alwaysCloses(char p) { return p == ']' || p == '>' || p == '}'; }
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
                if (tag != null && children[i] is EggsTag && ((EggsTag) children[i]).Tag == tag.Value && !EggsML.alwaysOpens(tag.Value))
                    sb.Append(new string(tag.Value, 2));
                sb.Append(childStr);
            }
            return sb.ToString();
        }
    }

    /// <summary>Represents either an <see cref="EggsGroup"/> or an <see cref="EggsTag"/>.</summary>
    public abstract class EggsContainer : EggsNode
    {
        /// <summary>Constructor.</summary>
        /// <param name="index">The index within the original string where this node starts.</param>
        public EggsContainer(int index) : base(index) { _children = new List<EggsNode>(); }
        /// <summary>Adds a new child node to this container.</summary>
        /// <param name="child">The child node to add.</param>
        public void Add(EggsNode child) { _children.Add(child); }
        /// <summary>The children of this node.</summary>
        public ReadOnlyCollection<EggsNode> Children { get { return _children.AsReadOnly(ref _childrenCache); } }
        private ReadOnlyCollection<EggsNode> _childrenCache;
        /// <summary>The underlying collection containing the children of this node.</summary>
        protected List<EggsNode> _children;
    }

    /// <summary>Represents a node in the EggsML parse tree that simply contains child nodes, but has no semantic meaning of its own.</summary>
    public sealed class EggsGroup : EggsContainer
    {
        /// <summary>Constructs a new, empty EggsML parse-tree node.</summary>
        /// <param name="index">The index in the original string where this tag was opened.</param>
        public EggsGroup(int index) : base(index) { }
        /// <summary>Reconstructs the original EggsML that is represented by this node.</summary>
        /// <remarks>This does not necessarily return the same EggsML that was originally parsed. For example, redundant uses of the "`" character are removed.</remarks>
        public override string ToString() { return stringify(_children, null); }
        /// <summary>Returns an XML representation of this EggsML node.</summary>
        public override object ToXml() { return new XElement("root", _children.Select(child => child.ToXml()).ToArray()); }
        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return _children.Any(child => child.HasText); } }
    }

    /// <summary>Represents a node in the EggsML parse tree that corresponds to an EggsML tag.</summary>
    public sealed class EggsTag : EggsContainer
    {
        /// <summary>The character used to open the tag (e.g. '[').</summary>
        public char Tag { get; private set; }
        /// <summary>Constructs a new EggsML parse-tree node that represents an EggsML tag.</summary>
        /// <param name="tag">The character used to open the tag (e.g. '[').</param>
        /// <param name="index">The index in the original string where this tag was opened.</param>
        public EggsTag(char tag, int index) : base(index) { Tag = tag; }
        /// <summary>Reconstructs the original EggsML that is represented by this node.</summary>
        /// <remarks>This does not necessarily return the same EggsML that was originally parsed. For example, redundant uses of the "`" character are removed.</remarks>
        public override string ToString()
        {
            if (Children.Count == 0)
                return EggsML.alwaysOpens(Tag) ? Tag.ToString() + EggsML.opposite(Tag) : Tag + "`" + Tag;

            var childrenStr = stringify(_children, Tag);

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
                    throw new ArgumentException("Unexpected tag character.", "Tag");
            }
            return new XElement(tagName, _children.Select(child => child.ToXml()));
        }
        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return _children.Any(child => child.HasText); } }
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
