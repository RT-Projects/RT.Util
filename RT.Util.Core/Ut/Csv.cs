using System.Text;

namespace RT.Util;

public static partial class Ut
{
    /// <summary>
    ///     Parses text from the <paramref name="reader"/> as CSV. See Remarks.</summary>
    /// <remarks>
    ///     <para>
    ///         Does not attempt to handle header columns in any way whatsoever.</para>
    ///     <para>
    ///         Empty rows (rows with zero columns) are skipped. Rows with only empty columns (<c>""</c>) are enumerated
    ///         normally. This behaviour is unaffected by the <paramref name="minColumns"/> setting.</para>
    ///     <para>
    ///         As suggested by RFC 4180, spaces around commas are significant: a leading space is interpreted as the
    ///         beginning of an un-quoted value, and a trailing space after a quoted value is an error.</para>
    ///     <para>
    ///         This method enumerates rows as they are parsed, meaning that a parse exception may occur after a number of
    ///         rows have already been yielded.</para></remarks>
    /// <param name="reader">
    ///     Text reader containing the text to parse.</param>
    /// <param name="minColumns">
    ///     The minimum number of columns to return for each row. Any row with fewer columns than this is padded with empty
    ///     string values to meet this minimum.</param>
    /// <returns>
    ///     An enumeration of rows, with each row represented by an array of column values.</returns>
    public static IEnumerable<string[]> ParseCsv(TextReader reader, int minColumns = 0)
    {
        var curRow = new List<string>(minColumns);
        var curField = new StringBuilder();
        const int EOF = -1;
        const int state_start_of_row = 1, state_start_of_field = 2, state_in_unquoted_field = 3, state_in_quoted_field = 4, state_in_quote_after_dblq = 5;
        int state = state_start_of_row;
        while (true)
        {
            int c = reader.Read();

            switch (state)
            {
                // State: start of a row
                case state_start_of_row:
                    if (c == ',') // start and end of an empty field, and start of another field
                    {
                        curRow.Add("");
                        state = state_start_of_field;
                    }
                    else if (c == '\r' || c == '\n' || c == EOF) // end of a row, and we're also not currently processing a field
                    {
                        if (curRow.Count > 0) // start_of_row state is entered only after clearing the current row so there's never anything to yield
                            throw new InternalErrorException("f2daj81");
                    }
                    else if (c == '\"') // start of a quoted field
                        state = state_in_quoted_field;
                    else // start of an unquoted field; current character is part of the value
                    {
                        curField.Append((char) c);
                        state = state_in_unquoted_field;
                    }
                    break;

                // State: start of a field; don't know yet if quoted or unquoted but there's definitely a field here (this happens after a comma)
                case state_start_of_field:
                    if (c == ',')
                    {
                        // it was an empty unquoted field, and start of another field
                        curRow.Add("");
                        state = state_start_of_field;
                    }
                    else if (c == '\r' || c == '\n' || c == EOF) // this field ended so it was an empty unquoted field
                    {
                        curRow.Add("");
                        while (curRow.Count < minColumns)
                            curRow.Add("");
                        yield return curRow.ToArray();
                        curRow.Clear();
                        state = state_start_of_row;
                    }
                    else if (c == '"') // it's a quoted field
                        state = state_in_quoted_field;
                    else // it's an unquoted field; current character is part of the value
                    {
                        curField.Append((char) c);
                        state = state_in_unquoted_field;
                    }
                    break;

                // State: inside an unquoted field
                case state_in_unquoted_field:
                    if (c == ',') // end of this field, start of another one
                    {
                        curRow.Add(curField.ToString());
                        curField.Clear();
                        state = state_start_of_field;
                    }
                    else if (c == '\r' || c == '\n' || c == EOF)
                    {
                        curRow.Add(curField.ToString());
                        curField.Clear();
                        while (curRow.Count < minColumns)
                            curRow.Add("");
                        yield return curRow.ToArray();
                        curRow.Clear();
                        state = state_start_of_row;
                    }
                    else
                        curField.Append((char) c);
                    break;

                // State: inside a quoted field, and outside a potential double-quote escape
                case state_in_quoted_field:
                    if (c == '"')
                        state = state_in_quote_after_dblq;
                    else
                        curField.Append((char) c);
                    break;

                // State 3: inside a quoted field, after a double-quote: could be a double-quote-escape, or end of field
                case state_in_quote_after_dblq:
                    if (c == '"')
                    {
                        curField.Append('"');
                        state = state_in_quoted_field;
                    }
                    else if (c == ',') // end of this field, start of another one
                    {
                        curRow.Add(curField.ToString());
                        curField.Clear();
                        state = state_start_of_field;
                    }
                    else if (c == '\r' || c == '\n' || c == EOF)
                    {
                        curRow.Add(curField.ToString());
                        curField.Clear();
                        while (curRow.Count < minColumns)
                            curRow.Add("");
                        yield return curRow.ToArray();
                        curRow.Clear();
                        state = state_start_of_row;
                    }
                    else
                        throw new InvalidOperationException("CSV format error: a quoted field may only be followed by comma, end of line, or end of stream ");
                    break;

                default:
                    throw new InternalErrorException("ea2g7t");
            }

            if (c == EOF)
                break;
        }
    }

    /// <summary>Parses <paramref name="content"/> as CSV. For details see <see cref="ParseCsv(TextReader, int)"/>.</summary>
    public static IEnumerable<string[]> ParseCsv(string content, int minColumns = 0)
    {
        return ParseCsv(new StringReader(content), minColumns);
    }

    /// <summary>Parses the file at <paramref name="path"/> as CSV. For details see <see cref="ParseCsv(TextReader, int)"/>.</summary>
    public static IEnumerable<string[]> ParseCsvFile(string path, int minColumns = 0)
    {
        using var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(file);
        foreach (var row in ParseCsv(reader, minColumns))
            yield return row;
    }

    /// <summary>
    ///     Formats an entire CSV row, escaping cell values where required. See Remarks.</summary>
    /// <remarks>
    ///     The value returned may contain newlines inside cell values. It does not contain the terminating newline that
    ///     separates CSV rows from each other. Zero cells are formatted to a blank line, which gets skipped entirely by <see
    ///     cref="ParseCsv(string,int)"/>. All other inputs round-trip exactly.</remarks>
    public static string FormatCsvRow(IEnumerable<object> cells)
    {
        var sb = new StringBuilder();
        bool first = true;
        bool singleBlank = false;
        foreach (var cc in cells)
        {
            if (!first)
                sb.Append(',');
            var cell = cc.ToString();
            // we only have to quote: anything with a double-quote, comma, /r, /n
            if (cell.Any(c => c == '"' || c == ',' || c == '\r' || c == '\n'))
            {
                sb.Append('"');
                sb.Append(cell.Replace("\"", "\"\""));
                sb.Append('"');
            }
            else
                sb.Append(cell);
            singleBlank = first && cell == "";
            first = false;
        }
        if (singleBlank)
            sb.Append("\"\"");
        return sb.ToString();
    }

    /// <summary>
    ///     Formats an entire CSV row, escaping cell values where required. See Remarks.</summary>
    /// <remarks>
    ///     The value returned may contain newlines inside cell values. It does not contain the terminating newline that
    ///     separates CSV rows from each other. Zero cells are formatted to a blank line, which gets skipped entirely by <see
    ///     cref="ParseCsv(string,int)"/>. All other inputs round-trip exactly.</remarks>
    public static string FormatCsvRow(params object[] cells)
    {
        return FormatCsvRow(cells.AsEnumerable());
    }
}
