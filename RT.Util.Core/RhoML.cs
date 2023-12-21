using System.Text;
using System.Text.RegularExpressions;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

namespace RT.Util;

/// <summary>
///     Exposes methods related to the RhoML language. See Remarks.</summary>
/// <remarks>
///     <para>
///         RhoML is a language which parses into a tree somewhat reminiscent of XML. The document is represented by a root
///         <see cref="RhoElement"/>, and consists of a tree of <see cref="RhoNode"/>s. Two types of nodes exist: elements,
///         which have a name and may have attributes, and text nodes, which simply contain raw text. Only elements may
///         contain sub-nodes.</para>
///     <para>
///         Except for the root element, all elements must have a name, which can be an arbitrary string. The root element's
///         name is null. An element may have a default attribute value, as well as any number of additional named
///         attribute/value pairs. The attribute names and values can also be arbitrary strings. A named attribute is not
///         required to have a value.</para>
///     <para>
///         Syntactically, an element is delimited by an opening and a closing tag. The closing tag is always <c>{}</c> for
///         all elements. The opening tag begins with a <c>{</c>, followed by the element name, the default attribute value
///         (<c>=</c> followed by the value), any number of named attributes (name, <c>=</c>, value, delimited by <c>,</c>),
///         and ends with a <c>}</c>.</para>
///     <para>
///         The following is an example of valid RhoML with an explanation of what is represented:</para>
///     <code>
///         Basic {font=Times New Roman}example{}: element named "font" with the default attribute set.
///          Another {blah,foo=bar,stuff}example{}: element named "blah", no default attribute, two named attributes
///          ("foo" and "stuff"), the first of which has the value "bar" while the second one has no value. Other than the
///          two elements, the rest of this RhoML is literal text.</code>
///     <para>
///         The element name and attribute names/values can all be specified using either quoted or unquoted syntax. Unquoted
///         syntax is limited in what strings can be expressed, while quoted syntax allows every possible string to be
///         represented:</para>
///     <list type="bullet">
///         <item><description>
///             Unquoted values always terminate at <c>{</c>, <c>}</c>, <c>=</c>, <c>,</c>, <c>`</c>, newlines and tabs, and
///             these cannot be escaped. All other characters are allowed and are interpreted literally. Spaces are allowed
///             but leading and trailing spaces will be ignored.</description></item>
///         <item><description>
///             Quoted values begin and end with a <c>`</c>. Actual backticks can be represented by <c>``</c>. All other
///             characters are interpreted literally inside a quoted value, and there are no special escape sequences for
///             newlines or other non-printing characters.</description></item></list>
///     <para>
///         Whitespace is significant in all contexts, with some exceptions inside the opening tag of an element.
///         Specifically, whitespace is ignored inside the opening tag between all syntactic elements (but not inside
///         names/values), with the sole exception of immediately after <c>{</c>: this character must be followed by a
///         <c>`</c> (beginning the element name in quoted syntax) or a Unicode letter or digit (beginning the tag name in
///         unquoted syntax). Otherwise the <c>{</c> character is interpreted as a literal opening curly bracket.</para>
///     <para>
///         Within a run of text, only the <c>{</c> character needs special attention; all other characters are interpreted
///         literally. The <c>{</c> character is also interpreted literally unless followed by a <c>{</c> (in which case the
///         two are interpreted as a single literal curly bracket), <c>}</c> (interpreted as the closing tag), or a
///         <c>`</c>/letter/digit (interpreted as the start of an opening tag).</para>
///     <para>
///         A more complex example (the entire example is valid RhoML):</para>
///     <code>
///         This curly bracket { is interpreted literally, as is this } one. This {{ is a single open curly.
///         Here {` is ``{ `}an element{} whose name is " is `{ ", containing a text node with the text "an element".
///         Here's an element with some generous {use = of spaces ,   you   =   see}.{}; this represents an element named
///         "use", with a default attribute value "of spaces", and an attribute named "you" with a value "see".</code></remarks>
public static class RhoML
{
    /// <summary>
    ///     Parses the specified string as RhoML.</summary>
    /// <param name="input">
    ///     The string to parse.</param>
    /// <returns>
    ///     The root element of the parse tree. The root element has a null name, and contains the RhoML content as child
    ///     nodes, even if the parsed content has a single top-level node.</returns>
    public static RhoElement Parse(string input)
    {
        return new RhoParserState(input).Parse();
    }

    /// <summary>
    ///     Escapes the specified string so that when parsed as RhoML, the result is a single text node containing the string
    ///     passed in.</summary>
    public static string Escape(string str)
    {
        // Special things: {{, {}, {`, {letter/digit - need to escape the first { in each case
        var result = new StringBuilder(str.Length + str.Length / 8);
        int i = 0;
        int startIndex = 0;
        while (i < str.Length)
        {
            var c = str[i];
            if (c == '{' && i < str.Length - 1)
            {
                i++;
                var cn = str[i];
                if (cn == '{' || cn == '}' || cn == '`' || char.IsLetterOrDigit(cn)) // then need to escape
                {
                    result.Append(str.Substring(startIndex, i - startIndex)); // include the { that necessitated this
                    result.Append('{');
                    startIndex = i;
                }
            }
            i++;
        }
        result.Append(str.Substring(startIndex));
        return result.ToString();
    }
}

/// <summary>Encapsulates one of the two possible types of RhoML nodes.</summary>
public abstract class RhoNode
{
    /// <summary>
    ///     Appends this node and all children to the specified string builder, converting them to RhoML format that would
    ///     parse back into this tree.</summary>
    public abstract void AppendTo(StringBuilder builder);

    /// <summary>Converts this node to a RhoML string.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        AppendTo(sb);
        return sb.ToString();
    }
}

/// <summary>Encapsulates a text node in a RhoML tree.</summary>
public sealed class RhoText : RhoNode
{
    /// <summary>Gets or sets the text string represented by this instance. Not null.</summary>
    public string Text
    {
        get { return _text; }
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            _text = value;
        }
    }

    private string _text;

    /// <summary>Constructor.</summary>
    public RhoText(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));
        Text = text;
    }

    /// <summary>
    ///     Appends this node and all children to the specified string builder, converting them to RhoML format that would
    ///     parse back into this tree.</summary>
    public override void AppendTo(StringBuilder builder)
    {
        builder.Append(Regex.Replace(_text, @"{[`}a-zA-Z]", match => "{" + match.Value));
    }
}

/// <summary>Encapsulates an element node in a RhoML tree.</summary>
public sealed class RhoElement : RhoNode
{
    /// <summary>Gets or sets the name of the element. Null for the root element, otherwise non-null.</summary>
    public string Name { get; set; }
    /// <summary>Gets or sets the value of the default attribute. Null if the default attribute was omitted.</summary>
    public string Value { get; set; }
    /// <summary>
    ///     Gets or sets a dictionary of attributes. Not null. There is a key for each named attribute specified on the
    ///     element. Attributes whose value was omitted will have the value of null in this dictionary.</summary>
    public IDictionary<string, string> Attributes
    {
        get { return _attributes; }
        set { if (value == null) throw new ArgumentNullException(nameof(value)); _attributes = value; }
    }
    /// <summary>
    ///     Gets or sets a read-only list of child elements. Not null. May be empty, or contain any <see cref="RhoNode"/>
    ///     instance in any order.</summary>
    public IList<RhoNode> Children
    {
        get { return _children; }
        set { if (value == null) throw new ArgumentNullException(nameof(value)); _children = value; }
    }

    private IDictionary<string, string> _attributes;
    private IList<RhoNode> _children;

    /// <summary>Constructor.</summary>
    public RhoElement()
    {
        _attributes = new Dictionary<string, string>();
        _children = new List<RhoNode>();
    }

    /// <summary>Constructor.</summary>
    public RhoElement(string name, string value = null)
        : this()
    {
        Name = name;
        Value = value;
    }

    /// <summary>Constructor.</summary>
    public RhoElement(string name, string value, IDictionary<string, string> attributes, List<RhoNode> elements)
    {
        if (attributes == null)
            throw new ArgumentNullException(nameof(attributes));
        if (elements == null)
            throw new ArgumentNullException(nameof(elements));
        Name = name;
        Value = value;
        Attributes = attributes.AsReadOnly();
        Children = elements.AsReadOnly();
    }

    /// <summary>Enumerates all descendants of this node, in an unspecified order.</summary>
    public IEnumerable<RhoNode> Descendants
    {
        get
        {
            // If a specific order is needed, this can be implemented backwards-compatibly at a later stage.
            // For now it's a breadth-first enumeration of the nested nodes.
            var queue = new Queue<RhoNode>();
            queue.EnqueueRange(this.Children);

            while (queue.Any())
            {
                var cur = queue.Dequeue();
                yield return cur;
                var tag = cur as RhoElement;
                if (tag != null)
                    queue.EnqueueRange(tag.Children);
            }
        }
    }

    /// <summary>
    ///     Appends this node and all children to the specified string builder, converting them to RhoML format that would
    ///     parse back into this tree.</summary>
    public override void AppendTo(StringBuilder builder)
    {
        if (Name != null)
        {
            builder.Append('{');
            builder.Append(escapedAttrString(Name));
            if (Value != null)
            {
                builder.Append('=');
                builder.Append(escapedAttrString(Value));
            }
            foreach (var attr in Attributes.OrderBy(a => a.Key))
            {
                builder.Append(',');
                builder.Append(escapedAttrString(attr.Key));
                if (attr.Value != null)
                {
                    builder.Append('=');
                    builder.Append(escapedAttrString(attr.Value));
                }
            }
            builder.Append('}');
        }

        foreach (var element in Children)
            element.AppendTo(builder);

        if (Name != null)
            builder.Append("{}");
    }

    private string escapedAttrString(string str)
    {
        if (str.Length == 0)
            return "";
        if (str[0] == ' ' || str[str.Length - 1] == ' ' || str.Any(c => c == '}' || c == '=' || c == ',' || c == '`' || c == '\r' || c == '\n' || c == '\t'))
            return "`" + str.Replace("`", "``") + "`";
        else
            return str;
    }
}

internal sealed class RhoParserState
{
    public string Input;
    public int Pos;

    private OffsetToLineCol _offsetConverter;
    public OffsetToLineCol OffsetConverter { get { if (_offsetConverter == null) _offsetConverter = new OffsetToLineCol(Input); return _offsetConverter; } }

    private RhoParserState() { }

    public RhoParserState(string input)
    {
        Input = input;
        Pos = 0;
    }

    public RhoParserState Clone()
    {
        var result = new RhoParserState();
        result.Input = Input;
        result.Pos = Pos;
        result._offsetConverter = _offsetConverter;
        return result;
    }

    public char? Cur { get { return Pos >= Input.Length ? null : (char?) Input[Pos]; } }
    public char? Next { get { return Pos + 1 >= Input.Length ? null : (char?) Input[Pos + 1]; } }

    public string Snippet
    {
        get
        {
            int line, col;
            OffsetConverter.GetLineAndColumn(Pos, out line, out col);
            return "Before: {2}   After: {3}   At: {0},{1}".Fmt(line, col, Input.SubstringSafe(Pos - 15, 15), Input.SubstringSafe(Pos, 15));
        }
    }

    public override string ToString()
    {
        return Snippet;
    }

    public RhoElement Parse()
    {
        var elems = parseElements();
        if (Cur != null)
            throw new RhoParseException(this, "Expected end of input");
        return new RhoElement(null, null, new Dictionary<string, string>(), elems);
    }

    private List<RhoNode> parseElements()
    {
        var result = new List<RhoNode>();
        while (true)
        {
            string consumed = consumeUntilNonText();
            if (consumed.Length > 0)
                result.Add(new RhoText(consumed));
            if (Pos >= Input.Length)
                return result;
            if (Cur == '{' && Next == '}')
                return result;
            result.Add(parseElement());
        }
    }

    private void consumeWhitespace()
    {
        while (Pos < Input.Length && " \t\r\n".Contains(Input[Pos]))
            Pos++;
    }

    private string consumeUntilNonText()
    {
        int pos = Pos;

        var sb = new StringBuilder();
        while (pos < Input.Length)
        {
            // Last char before EOF can only be text
            if (pos + 1 >= Input.Length)
            {
                pos++;
                break;
            }

            if (Input[pos] == '{')
            {
                char next = Input[pos + 1];
                if (next == '`' || next == '}' || char.IsLetterOrDigit(next))
                    break;
                // Otherwise the next character cannot possibly start a tag
                if (next == '{')
                {
                    sb.Append(Input.Substring(Pos, pos - Pos));
                    Pos = pos + 1;
                }
                pos++;
            }

            pos++;
        }
        sb.Append(Input.Substring(Pos, pos - Pos));
        Pos = pos;
        return sb.ToString();
    }

    private RhoElement parseElement()
    {
        // Opening tag
        if (Cur != '{')
            throw new RhoParseException(this, "Expected '{'");
        Pos++;
        string name = parseAttrString();

        string value = null;
        if (Cur == '=')
        {
            Pos++;
            consumeWhitespace();
            value = parseAttrString();
        }

        var attrs = new Dictionary<string, string>();
        while (Cur == ',')
        {
            Pos++;
            consumeWhitespace();
            string attrName = parseAttrString();
            string attrValue = null;
            if (Cur == '=')
            {
                Pos++;
                consumeWhitespace();
                attrValue = parseAttrString();
            }
            if (attrs.ContainsKey(attrName))
                throw new RhoParseException(this, "Duplicate attribute name");
            attrs[attrName] = attrValue;
        }

        if (Cur != '}')
            throw new RhoParseException(this, "Expected '}' (or ',' or possibly '=')");
        Pos++;

        // Content
        var elems = parseElements();

        // Closing tag
        if (Cur != '{' || Next != '}')
            throw new RhoParseException(this, "Expected '{}' - the closing tag");
        Pos += 2;

        // Done!
        return new RhoElement(name, value, attrs, elems);
    }

    private string parseAttrString()
    {
        if (Cur == '`')
            return parseAttrStringQuoted();
        else
            return parseAttrStringRaw();
    }

    private string parseAttrStringQuoted()
    {
        if (Cur != '`')
            throw new RhoParseException(this, "Expected '`'");
        Pos++;
        var result = new StringBuilder();
        int pos = Pos;
        while (pos < Input.Length)
        {
            char c = Input[pos];
            if (c == '`')
            {
                if (pos + 1 < Input.Length && Input[pos + 1] == '`')
                {
                    // this ` is escaped
                    result.Append(Input.Substring(Pos, pos - Pos + 1));
                    pos++;
                    Pos = pos + 1;
                }
                else
                    break; // not escaped: definitely a string-closing `
            }
            pos++;
        }
        result.Append(Input.Substring(Pos, pos - Pos));
        Pos = pos;
        if (Cur != '`')
            throw new RhoParseException(this, "Expected '`'");
        Pos++;
        consumeWhitespace();
        return result.ToString(); ;
    }

    private string parseAttrStringRaw()
    {
        int pos = Pos;
        while (pos < Input.Length)
        {
            char c = Input[pos];
            if (c == '{' || c == '}' || c == '=' || c == ',' || c == '`' || c == '\r' || c == '\n' || c == '\t')
                break;
            pos++;
        }
        string result = Input.Substring(Pos, pos - Pos);
        Pos = pos;
        consumeWhitespace();
        return result.Trim(' ');
    }
}

/// <summary>Represents a RhoML parsing exception.</summary>
public class RhoParseException : Exception
{
    private RhoParserState _state;

    internal RhoParseException(RhoParserState ps, string message)
        : base(message)
    {
        _state = ps.Clone();
    }

    /// <summary>Gets the line number at which the parse error occurred.</summary>
    public int Line { get { return _state.OffsetConverter.GetLine(_state.Pos); } }
    /// <summary>Gets the column number at which the parse error occurred.</summary>
    public int Column { get { return _state.OffsetConverter.GetColumn(_state.Pos); } }
    /// <summary>A snippet of the JSON string at which the parse error occurred.</summary>
    public string Snippet { get { return _state.Snippet; } }
}
