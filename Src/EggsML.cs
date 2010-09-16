﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;
using System.Xml.Linq;

namespace RT.Util
{
    /// <summary>Implements a parser for the minimalist text mark-up language EggsML.</summary>
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
        public static EggsNode Parse(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            EggsContainer curTag = new EggsGroup(0);
            if (input.Length == 0)
                return curTag;

            var index = 0;
            var stack = new Stack<EggsContainer>();

            while (input.Length > 0)
            {
                var pos = input.IndexOf(ch => SpecialCharacters.Contains(ch));

                if (pos == -1)
                {
                    curTag.Add(input);
                    break;
                }

                if (pos < input.Length - 1 && input[pos + 1] == input[pos])
                {
                    curTag.Add(input.Substring(0, pos + 1));
                    input = input.Substring(pos + 2);
                    index += pos + 2;
                    continue;
                }

                if (pos > 0)
                {
                    curTag.Add(input.Substring(0, pos));
                    input = input.Substring(pos);
                    index += pos;
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
                        curTag.Add(input.Substring(1, pos - 1).Replace("\"\"", "\""));
                        input = input.Substring(pos + 1);
                        index += pos + 1;
                        continue;

                    case '|':
                        if (!(curTag is EggsTag))
                            throw new EggsMLParseException("Cannot use '|' at the top level (only allowed inside tags).", index);
                        ((EggsTag) curTag).Children.Add(new List<EggsNode>());
                        input = input.Substring(1);
                        index++;
                        continue;

                    default:
                        bool last = input.Length == 1;
                        // Are we opening a new tag?
                        if ((alwaysOpens(input[0]) || !(curTag is EggsTag) || input[0] != opposite(((EggsTag) curTag).Tag) || (!last && input[1] == '|')) && !alwaysCloses(input[0]))
                        {
                            stack.Push(curTag);
                            var i = (!last && input[1] == '|') ? 2 : 1;
                            index += i;
                            curTag = new EggsTag(input[0], index);
                            input = input.Substring(i);
                            continue;
                        }
                        // Are we closing a tag?
                        else if (curTag is EggsTag && input[0] == opposite(((EggsTag) curTag).Tag))
                        {
                            var prevTag = stack.Pop();
                            prevTag.Add(curTag);
                            curTag = prevTag;
                            input = input.Substring(1);
                            index++;
                            continue;
                        }
                        else if (alwaysCloses(input[0]))
                            throw new EggsMLParseException(@"Tag '{0}' unexpected".Fmt(input[0]), index + 1);
                        throw new EggsMLParseException(@"Character '{0}' unexpected".Fmt(input[0]), index + 1);
                }
            }

            if (stack.Count > 0)
                throw new EggsMLParseException(@"Closing '{0}' missing".Fmt(opposite(((EggsTag) curTag).Tag)), curTag.Index);

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
        /// <summary>Allows implicit conversion from a string to an <see cref="EggsText"/> node.</summary>
        public static implicit operator EggsNode(string text) { return text == null ? null : new EggsText(text); }
        /// <summary>Returns the EggsML parse tree as XML.</summary>
        public abstract object ToXml();
        /// <summary>The index in the original string where this tag was opened.</summary>
        public int Index;

        /// <summary>Turns a list of child nodes into EggsML mark-up.</summary>
        /// <param name="childList">List of children to turn into mark-up.</param>
        /// <param name="tag">If null, assumes we are not at the beginning of a tag. Otherwise, assumes we are at the beginning of a tag with the specified character, causing necessary escaping to be performed.</param>
        /// <returns>EggsML mark-up representing the same tree structure as this node.</returns>
        protected string stringifyList(List<EggsNode> childList, char? tag)
        {
            if (childList == null || childList.Count == 0)
                return string.Empty;
            var sb = new StringBuilder();
            var prev = childList[0].ToString();
            if (tag != null && childList[0] is EggsTag && ((EggsTag) childList[0]).Tag == tag.Value && !EggsML.alwaysOpens(tag.Value))
            {
                sb.Append(tag.Value);
                sb.Append('|');
                sb.Append(prev.Substring(1));
            }
            else
                sb.Append(prev);
            foreach (var child in childList.Skip(1))
            {
                var next = child.ToString();
                if (tag != null && child is EggsTag && ((EggsTag) child).Tag == tag.Value && !EggsML.alwaysOpens(tag.Value))
                    next = tag.Value.ToString() + "|" + next.Substring(1);
                if (next.Length > 0 && next[0] == prev[prev.Length - 1])
                    sb.Append('`');
                sb.Append(next);
                prev = next;
            }
            return sb.ToString();
        }
        /// <summary>Determines whether this node contains any textual content.</summary>
        public abstract bool HasText { get; }
    }

    /// <summary>Represents either an <see cref="EggsGroup"/> or an <see cref="EggsTag"/>.</summary>
    public abstract class EggsContainer : EggsNode
    {
        /// <summary>Adds a new child node to this container.</summary>
        /// <param name="child">The child node to add.</param>
        public abstract void Add(EggsNode child);
    }

    /// <summary>Represents a node in the EggsML parse tree that simply contains child nodes, but has no semantic meaning of its own.</summary>
    public sealed class EggsGroup : EggsContainer
    {
        /// <summary>The children of this node.</summary>
        public List<EggsNode> Children = new List<EggsNode>();
        /// <summary>Constructs a new, empty EggsML parse-tree node.</summary>
        /// <param name="index">The index in the original string where this tag was opened.</param>
        public EggsGroup(int index) { Index = index; }
        /// <summary>Adds a new child node to this group.</summary>
        /// <param name="child">The child node to add.</param>
        public override void Add(EggsNode child) { Children.Add(child); }
        /// <summary>Reconstructs the original EggsML that is represented by this node.</summary>
        /// <remarks>This does not necessarily return the same EggsML that was originally parsed. For example, redundant uses of the "`" character are removed.</remarks>
        public override string ToString() { return stringifyList(Children, null); }
        /// <summary>Returns an XML representation of this EggsML node.</summary>
        public override object ToXml() { return new XElement("root", Children.Select(c => c.ToXml()).ToArray()); }
        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return Children.Any(c => c.HasText); } }
    }

    /// <summary>Represents a node in the EggsML parse tree that corresponds to an EggsML tag.</summary>
    public sealed class EggsTag : EggsContainer
    {
        /// <summary>The character used to open the tag (e.g. '[').</summary>
        public char Tag;
        /// <summary>The children of this node. The first level corresponds to the usage of the '|' character in EggsML.</summary>
        public List<List<EggsNode>> Children = new List<List<EggsNode>> { new List<EggsNode>() };
        /// <summary>Constructs a new EggsML parse-tree node that represents an EggsML tag.</summary>
        /// <param name="tag">The character used to open the tag (e.g. '[').</param>
        /// <param name="index">The index in the original string where this tag was opened.</param>
        public EggsTag(char tag, int index) { Tag = tag; Index = index; }
        /// <summary>Adds a new child node to the last set of children of this tag.</summary>
        /// <param name="child">The child node to add.</param>
        public override void Add(EggsNode child) { Children[Children.Count - 1].Add(child); }
        /// <summary>Reconstructs the original EggsML that is represented by this node.</summary>
        /// <remarks>This does not necessarily return the same EggsML that was originally parsed. For example, redundant uses of the "`" character are removed.</remarks>
        public override string ToString()
        {
            if (Children == null || Children.Count == 0)
                throw new ArgumentException("Children must be present.", "Chidren");

            if (Children.Count == 1 && Children[0].Count == 0)
                return EggsML.alwaysOpens(Tag) ? Tag.ToString() + EggsML.opposite(Tag) : Tag.ToString() + '`' + Tag;

            var sb = new StringBuilder();
            sb.Append(Tag);
            var firstChild = stringifyList(Children[0], Tag);
            if (firstChild.Length == 0 || firstChild[0] == Tag || firstChild[0] == '|')
                sb.Append('`');
            sb.Append(firstChild);
            foreach (var childList in Children.Skip(1))
            {
                sb.Append('|');
                var nextChild = stringifyList(childList, Tag);
                if (nextChild.StartsWith("|"))
                    sb.Append('`');
                sb.Append(nextChild);
            }
            if (sb[sb.Length - 1] == Tag && sb[sb.Length - 2] != Tag)
                sb.Append('`');
            sb.Append(EggsML.opposite(Tag));
            return sb.ToString();
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
                default:
                    throw new ArgumentException("Unexpected tag character.", "Tag");
            }
            return new XElement(tagName, Children.Count == 1 ? (object) Children[0].Select(ch => ch.ToXml()) : Children.Select(chlist => new XElement("item", chlist.Select(ch => ch.ToXml()))));
        }
        /// <summary>Determines whether this node contains any textual content.</summary>
        public override bool HasText { get { return Children.Any(c1 => c1.Any(c2 => c2.HasText)); } }
    }
    /// <summary>Represents a node in the EggsML parse tree that corresponds to a piece of text.</summary>
    public sealed class EggsText : EggsNode
    {
        /// <summary>The text contained in this node.</summary>
        public string Text = "";
        /// <summary>Constructs a new EggsML text node.</summary>
        /// <param name="text">The text for this node to contain.</param>
        public EggsText(string text)
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
