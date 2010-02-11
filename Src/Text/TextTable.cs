using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RT.Util.ExtensionMethods;

namespace RT.Util.Text
{
    /// <summary>
    /// Produces a table in a fixed-width character environment.
    /// </summary>
    public class TextTable
    {
        /// <summary>Provides values to change the horizontal alignment of text within cells.</summary>
        public enum Alignment
        {
            /// <summary>Specifies alignment to the left edge of each cell.</summary>
            Left,
            /// <summary>Specifies horizontally centered alignment.</summary>
            Center,
            /// <summary>Specifies alignment to the right edge of each cell.</summary>
            Right
        };

        /// <summary>Gets or sets the number of characters to space each column apart from the next.</summary>
        public int ColumnSpacing { get; set; }
        /// <summary>Gets or sets the number of characters to space each row apart from the next.</summary>
        public int RowSpacing { get; set; }
        /// <summary>Gets or sets the maximum width of the table, including all column spacing. If <see cref="UseFullWidth"/> is false, the table may be narrower. If this is null, the table will be as wide as all the text in it, unwrapped.</summary>
        public int? MaxWidth { get; set; }
        /// <summary>Gets or sets a value indicating whether horizontal rules are rendered between rows. The horizontal rules are rendered only if <see cref="RowSpacing"/> is greater than zero.</summary>
        public bool HorizontalRules { get; set; }
        /// <summary>Gets or sets a value indicating whether vertical rules are rendered between columns. The vertical rules are rendered only if <see cref="ColumnSpacing"/> is greater than zero.</summary>
        public bool VerticalRules { get; set; }
        /// <summary>Gets or sets a value indicating the number of rows from the top that are considered table headers. The only effect of this is that the horizontal rule (if any) after the header rows is rendered using '=' characters instead of '-'.</summary>
        public int HeaderRows { get; set; }
        /// <summary>If true, the table will be expanded to fill the <see cref="MaxWidth"/>. If false, the table will fill the whole width only if any cells need to be word-wrapped.</summary>
        public bool UseFullWidth { get; set; }
        /// <summary>Specifies the default alignment to use for cells where the alignment is not explicitly set. Default is <see cref="Alignment.Left"/>.</summary>
        public Alignment DefaultAlignment { get; set; }
        /// <summary>Gets or sets a value indicating the number of spaces to add left of the table. This does not count towards the <see cref="MaxWidth"/>.</summary>
        public int LeftMargin { get; set; }

        /// <summary>Places the specified content into the cell at the specified co-ordinates.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        public void SetCell(int col, int row, string content)
        {
            setCell(col, row, content, 1, 1, false, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell will be word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width.</param>
        public void SetCell(int col, int row, string content, bool noWrap)
        {
            setCell(col, row, content, 1, 1, noWrap, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        public void SetCell(int col, int row, string content, int colSpan, int rowSpan)
        {
            setCell(col, row, content, colSpan, rowSpan, false, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell is word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width. If false, the cell is automatically word-wrapped to optimise the table's layout.</param>
        public void SetCell(int col, int row, string content, int colSpan, int rowSpan, bool noWrap)
        {
            setCell(col, row, content, colSpan, rowSpan, noWrap, null);
        }

        /// <summary>Places the specified content into the cell at the specified co-ordinates.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, string content, Alignment alignment)
        {
            setCell(col, row, content, 1, 1, false, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell will be word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, string content, bool noWrap, Alignment alignment)
        {
            setCell(col, row, content, 1, 1, noWrap, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, string content, int colSpan, int rowSpan, Alignment alignment)
        {
            setCell(col, row, content, colSpan, rowSpan, false, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell is word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width. If false, the cell is automatically word-wrapped to optimise the table's layout.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, string content, int colSpan, int rowSpan, bool noWrap, Alignment alignment)
        {
            setCell(col, row, content, colSpan, rowSpan, noWrap, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>

        public void SetCell(int col, int row, ConsoleColoredString content)
        {
            setCell(col, row, content, 1, 1, false, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell will be word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, bool noWrap)
        {
            setCell(col, row, content, 1, 1, noWrap, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, int colSpan, int rowSpan)
        {
            setCell(col, row, content, colSpan, rowSpan, false, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell is word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width. If false, the cell is automatically word-wrapped to optimise the table's layout.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, int colSpan, int rowSpan, bool noWrap)
        {
            setCell(col, row, content, colSpan, rowSpan, noWrap, null);
        }

        /// <summary>Places the specified content into the cell at the specified co-ordinates.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, Alignment alignment)
        {
            setCell(col, row, content, 1, 1, false, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell will be word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, bool noWrap, Alignment alignment)
        {
            setCell(col, row, content, 1, 1, noWrap, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, int colSpan, int rowSpan, Alignment alignment)
        {
            setCell(col, row, content, colSpan, rowSpan, false, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell is word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width. If false, the cell is automatically word-wrapped to optimise the table's layout.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, ConsoleColoredString content, int colSpan, int rowSpan, bool noWrap, Alignment alignment)
        {
            setCell(col, row, content, colSpan, rowSpan, noWrap, alignment);
        }

        /// <summary>Places the specified content into the cell at the specified co-ordinates.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        public void SetCell(int col, int row, EggsNode content)
        {
            setCell(col, row, content, 1, 1, false, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell will be word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width.</param>
        public void SetCell(int col, int row, EggsNode content, bool noWrap)
        {
            setCell(col, row, content, 1, 1, noWrap, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        public void SetCell(int col, int row, EggsNode content, int colSpan, int rowSpan)
        {
            setCell(col, row, content, colSpan, rowSpan, false, null);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell is word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width. If false, the cell is automatically word-wrapped to optimise the table's layout.</param>
        public void SetCell(int col, int row, EggsNode content, int colSpan, int rowSpan, bool noWrap)
        {
            setCell(col, row, content, colSpan, rowSpan, noWrap, null);
        }

        /// <summary>Places the specified content into the cell at the specified co-ordinates.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, EggsNode content, Alignment alignment)
        {
            setCell(col, row, content, 1, 1, false, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell will be word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, EggsNode content, bool noWrap, Alignment alignment)
        {
            setCell(col, row, content, 1, 1, noWrap, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, EggsNode content, int colSpan, int rowSpan, Alignment alignment)
        {
            setCell(col, row, content, colSpan, rowSpan, false, alignment);
        }
        /// <summary>Places the specified content into the cell at the specified co-ordinates with the supplied options.</summary>
        /// <param name="col">Column where to place the content.</param>
        /// <param name="row">Row where to place the content.</param>
        /// <param name="content">The content to place.</param>
        /// <param name="colSpan">The number of columns to span. The default is 1.</param>
        /// <param name="rowSpan">The number of rows to span. The default is 1.</param>
        /// <param name="noWrap">If true, indicates that this cell should not be automatically word-wrapped except at explicit newlines in <paramref name="content"/>. 
        /// The cell is word-wrapped only if doing so is necessary to fit all no-wrap cells into the table's total width. If false, the cell is automatically word-wrapped to optimise the table's layout.</param>
        /// <param name="alignment">How to align the contents within the cell.</param>
        public void SetCell(int col, int row, EggsNode content, int colSpan, int rowSpan, bool noWrap, Alignment alignment)
        {
            setCell(col, row, content, colSpan, rowSpan, noWrap, alignment);
        }

        private void setCell(int col, int row, object content, int colSpan, int rowSpan, bool noWrap, Alignment? alignment)
        {
            if (col < 0)
                throw new ArgumentOutOfRangeException("col", col, @"""col"" cannot be negative.");
            if (row < 0)
                throw new ArgumentOutOfRangeException("row", row, @"""row"" cannot be negative.");
            if (colSpan < 1)
                throw new ArgumentOutOfRangeException("colSpan", colSpan, @"""colSpan"" cannot be less than 1.");
            if (rowSpan < 1)
                throw new ArgumentOutOfRangeException("rowSpan", rowSpan, @"""rowSpan"" cannot be less than 1.");
            if (content == null)
                throw new ArgumentNullException("content");

            // Complain if setting this cell would overlap with another cell due to its colspan or rowspan
            if (row >= _cells.Count || col >= _cells[row].Count || _cells[row][col] == null || _cells[row][col] is surrogateCell)
            {
                for (int x = 0; x < colSpan; x++)
                    for (int y = 0; y < rowSpan; y++)
                        if (row + y < _cells.Count && col + x < _cells[row + y].Count && _cells[row + y][col + x] is surrogateCell)
                        {
                            var sur = (surrogateCell) _cells[row][col];
                            var real = (trueCell) _cells[sur.RealRow][sur.RealCol];
                            throw new InvalidOperationException(@"The cell at row {0}, column {1} is already occupied because the cell at row {2}, column {3} has rowspan {4} and colspan {5}.".Fmt(row, col, sur.RealRow, sur.RealCol, real.RowSpan, real.ColSpan));
                        }
            }

            ensureCell(col, row);

            // If the cell contains a true cell, remove it with all its surrogates
            if (_cells[row][col] is trueCell)
            {
                var tr = (trueCell) _cells[row][col];
                for (int x = 0; x < tr.ColSpan; x++)
                    for (int y = 0; y < tr.RowSpan; y++)
                        _cells[row + y][col + x] = null;
            }

            // Insert the cell in the right place
            var tru = new trueCell
            {
                ColSpan = colSpan,
                RowSpan = rowSpan,
                Value = content,
                NoWrap = noWrap,
                Alignment = alignment
            };
            _cells[row][col] = tru;

            // For cells with span, insert the appropriate surrogate cells.
            for (int x = 0; x < colSpan; x++)
                for (int y = x == 0 ? 1 : 0; y < rowSpan; y++)
                {
                    ensureCell(col + x, row + y);
                    _cells[row + y][col + x] = new surrogateCell { RealCol = col, RealRow = row };
                }
        }

        /// <summary>Generates the table.</summary>
        /// <returns>The complete rendered table as a single string.</returns>
        public override string ToString()
        {
            var result = new StringBuilder();
            toString(s => result.Append(s), s => result.Append(s.ToString()));
            return result.ToString();
        }

        /// <summary>Generates the table.</summary>
        /// <returns>The complete rendered table as a single <see cref="ConsoleColoredString"/>.</returns>
        public ConsoleColoredString ToColoredString()
        {
            var result = new List<ConsoleColoredString>();
            toString(s => result.Add(s), s => result.Add(s));
            return new ConsoleColoredString(result.ToArray());
        }

        /// <summary>Outputs the entire table to the console. If <see cref="MaxWidth"/> is null, it will be set to the width of the console window.</summary>
        public void WriteToConsole()
        {
            if (MaxWidth == null)
            {
                MaxWidth = ConsoleUtil.WrapWidth() - 1;
                if (MaxWidth == int.MaxValue - 1)
                    MaxWidth = 120;
            }
            toString(s => Console.Write(s), s => ConsoleUtil.Write(s));
        }

        private void toString(Action<string> outputString, Action<ConsoleColoredString> outputColoredString)
        {
            int rows = _cells.Count;
            if (rows == 0)
                return;
            int cols = _cells.Max(row => row.Count);

            // Create a lookup array which, for each column, and for each possible value of colspan, tells you which cells in that column have this colspan and end in this column
            var cellsByColspan = new SortedDictionary<int, List<int>>[cols];
            for (var col = 0; col < cols; col++)
            {
                var cellsInThisColumn = new SortedDictionary<int, List<int>>();
                for (int row = 0; row < rows; row++)
                {
                    if (col >= _cells[row].Count)
                        continue;
                    var cel = _cells[row][col];
                    if (cel == null)
                        continue;
                    if (cel is surrogateCell && ((surrogateCell) cel).RealRow != row)
                        continue;
                    int realCol = cel is surrogateCell ? ((surrogateCell) cel).RealCol : col;
                    var realCell = (trueCell) _cells[row][realCol];
                    if (realCol + realCell.ColSpan - 1 != col)
                        continue;
                    cellsInThisColumn.AddSafe(realCell.ColSpan, row);
                }
                cellsByColspan[col] = cellsInThisColumn;
            }

            // Find out the width that each column would have if the text wasn't wrapped.
            // If this fits into the total width, then we want each column to be at least this wide.
            var columnWidths = generateColumnWidths(cols, cellsByColspan, c => Math.Max(1, c.LongestParagraph()));
            var unwrapped = true;

            // If the table is now too wide, use the length of the longest word, or longest paragraph if nowrap
            if (MaxWidth != null && columnWidths.Sum() > MaxWidth.Value - (cols - 1) * ColumnSpacing)
            {
                columnWidths = generateColumnWidths(cols, cellsByColspan, c => Math.Max(1, c.MinWidth()));
                unwrapped = false;
            }

            // If the table is still too wide, use the length of the longest paragraph if nowrap, otherwise 0
            if (MaxWidth != null && columnWidths.Sum() > MaxWidth.Value - (cols - 1) * ColumnSpacing)
                columnWidths = generateColumnWidths(cols, cellsByColspan, c => c.NoWrap ? Math.Max(1, c.LongestParagraph()) : 1);

            // If the table is still too wide, we will have to wrap like crazy.
            if (MaxWidth != null && columnWidths.Sum() > MaxWidth.Value - (cols - 1) * ColumnSpacing)
            {
                columnWidths = new int[cols];
                for (int i = 0; i < cols; i++) columnWidths[i] = 1;
            }

            // If the table is STILL too wide, all bets are off.
            if (MaxWidth != null && columnWidths.Sum() > MaxWidth.Value - (cols - 1) * ColumnSpacing)
                throw new InvalidOperationException(@"The specified table width is too narrow. It is not possible to fit the {0} columns and the column spacing of {1} per column into a total width of {2} characters.".Fmt(cols, ColumnSpacing, MaxWidth));

            // If we have any extra width to spare...
            var missingTotalWidth = MaxWidth == null ? 0 : MaxWidth.Value - columnWidths.Sum() - (cols - 1) * ColumnSpacing;
            if (missingTotalWidth > 0 && (UseFullWidth || !unwrapped))
            {
                // Use the length of the longest paragraph in each column to calculate a proportion by which to enlarge each column
                var widthProportionByCol = new int[cols];
                for (var col = 0; col < cols; col++)
                    foreach (var kvp in cellsByColspan[col])
                        distributeEvenly(
                            widthProportionByCol,
                            col,
                            kvp.Key,
                            kvp.Value.Max(row => ((trueCell) _cells[row][col - kvp.Key + 1]).LongestParagraph()) - widthProportionByCol.Skip(col - kvp.Key + 1).Take(kvp.Key).Sum() - (unwrapped ? 0 : columnWidths.Skip(col - kvp.Key + 1).Take(kvp.Key).Sum())
                        );
                var widthProportionTotal = widthProportionByCol.Sum();

                // Adjust the width of the columns according to the calculated proportions so that they fill the missing width.
                // We do this in two steps. Step one: enlarge the column widths by the integer part of the calculated portion (round down).
                // After this the width remaining will be smaller than the number of column, so each column is missing at most 1 character.
                var widthRemaining = missingTotalWidth;
                var fractionalParts = new double[cols];
                for (int col = 0; col < cols; col++)
                {
                    var widthToAdd = (float) (widthProportionByCol[col] * missingTotalWidth) / widthProportionTotal;
                    var integerPart = (int) widthToAdd;
                    columnWidths[col] += integerPart;
                    fractionalParts[col] = widthToAdd - integerPart;
                    widthRemaining -= integerPart;
                }

                // Step two: enlarge a few more columns by 1 character so that we reach the desired width.
                // The columns with the largest fractional parts here are the furthest from what we ideally want, so we favour those.
                foreach (var elem in fractionalParts.Select((frac, col) => new { Value = frac, Col = col }).OrderByDescending(e => e.Value))
                {
                    if (widthRemaining < 1) break;
                    columnWidths[elem.Col]++;
                    widthRemaining--;
                }
            }

            // Word-wrap all the contents of all the cells
            foreach (var row in _cells)
                for (int col = 0; col < row.Count; col++)
                    if (row[col] is trueCell)
                    {
                        var cel = (trueCell) row[col];
                        cel.Wordwrap(columnWidths.Skip(col).Take(cel.ColSpan).Sum() + (cel.ColSpan - 1) * ColumnSpacing);
                    }

            // Calculate the string index for each column
            var strIndexByCol = new int[cols + 1];
            for (var i = 0; i < cols; i++)
                strIndexByCol[i + 1] = strIndexByCol[i] + columnWidths[i] + ColumnSpacing;
            var realWidth = strIndexByCol[cols] - ColumnSpacing;

            // Make sure we don't render rules if we can't
            bool verticalRules = VerticalRules && ColumnSpacing > 0;
            bool horizontalRules = HorizontalRules && RowSpacing > 0;

            // If we do render vertical rules, where (at which string offset) within the column spacing should it be
            var vertRuleOffset = 1 + (ColumnSpacing - 1) / 2;

            // Finally, render the entire output
            List<ConsoleColoredString> currentLine = null;
            for (int row = 0; row < _cells.Count; row++)
            {
                var rowList = _cells[row];
                var extraRows = RowSpacing + 1;
                var isFirstIteration = true;
                bool anyMoreContentInThisRow;
                do
                {
                    ConsoleColoredString previousLine = currentLine == null ? null : new ConsoleColoredString(currentLine.ToArray());
                    currentLine = new List<ConsoleColoredString>();
                    anyMoreContentInThisRow = false;
                    for (int col = 0; col < rowList.Count; col++)
                    {
                        var cel = rowList[col];

                        // For cells with colspan, consider only the first cell they're spanning and skip the rest
                        if (cel is surrogateCell && ((surrogateCell) cel).RealCol != col)
                            continue;

                        // If the cell has rowspan, what row did this cell start in?
                        var valueRow = cel is surrogateCell ? ((surrogateCell) cel).RealRow : row;

                        // Retrieve the data for the cell
                        var realCell = (trueCell) _cells[valueRow][col];
                        var colspan = realCell == null ? 1 : realCell.ColSpan;
                        var rowspan = realCell == null ? 1 : realCell.RowSpan;

                        // Does this cell end in this row?
                        var isLastRow = valueRow + rowspan == row + 1;

                        // If we are going to render a horizontal rule, where does it start and end?
                        var horizRuleStart = col > 0 ? strIndexByCol[col] - vertRuleOffset + 1 : 0;
                        var horizRuleEnd = (col + colspan < cols) ? strIndexByCol[col + colspan] - vertRuleOffset + (verticalRules ? 0 : 1) : realWidth;

                        // If we are inside the cell, render one line of the contents of the cell
                        if (realCell != null && realCell.WordwrappedValue.Length > realCell.WordwrappedIndex)
                        {
                            var align = realCell.Alignment ?? DefaultAlignment;
                            int spaces = strIndexByCol[col] - currentLine.Sum(c => c.Length);
                            object textRaw = realCell.WordwrappedValue[realCell.WordwrappedIndex];
                            ConsoleColoredString text = textRaw is ConsoleColoredString ? (ConsoleColoredString) textRaw : (string) textRaw;  // implicit conversion to ConsoleColoredString
                            if (align == Alignment.Center)
                                spaces += (strIndexByCol[col + colspan] - strIndexByCol[col] - ColumnSpacing - text.Length) / 2;
                            else if (align == Alignment.Right)
                                spaces += strIndexByCol[col + colspan] - strIndexByCol[col] - ColumnSpacing - text.Length;
                            currentLine.Add(new string(' ', spaces));
                            currentLine.Add(text);
                            realCell.WordwrappedIndex++;
                        }

                        // If we are at the end of a row, render horizontal rules
                        if (horizontalRules && isLastRow && extraRows == 1)
                        {
                            currentLine.Add(new string(' ', horizRuleStart - currentLine.Sum(c => c.Length)));
                            currentLine.Add(new string((row == HeaderRows - 1) ? '=' : '-', horizRuleEnd - horizRuleStart));
                        }

                        // If we are at the beginning of a row, render the horizontal rules for the row above by modifying the previous line.
                        // We want to do this because it may have an unwanted vertical rule if this is a cell with colspan and there are
                        // multiple cells with smaller colspans above it.
                        if (isFirstIteration && horizontalRules && row > 0 && cel is trueCell)
                            previousLine = new ConsoleColoredString(previousLine.Substring(0, horizRuleStart), new string((row == HeaderRows) ? '=' : '-', horizRuleEnd - horizRuleStart), previousLine.Substring(horizRuleEnd));

                        // Render vertical rules
                        if (verticalRules && (col + colspan < cols))
                        {
                            currentLine.Add(new string(' ', strIndexByCol[col + colspan] - vertRuleOffset - currentLine.Sum(c => c.Length)));
                            currentLine.Add("|");
                        }

                        // Does this cell still contain any more content that needs to be output before this row can be finished?
                        anyMoreContentInThisRow = anyMoreContentInThisRow || (realCell != null && isLastRow && realCell.WordwrappedValue.Length > realCell.WordwrappedIndex);
                    }

                    if (previousLine != null)
                    {
                        if (LeftMargin > 0)
                            outputString(new string(' ', LeftMargin));
                        outputColoredString(previousLine);
                        outputString(Environment.NewLine);
                    }

                    isFirstIteration = false;

                    // If none of the cells in this row contain any more content, start counting down the row spacing
                    if (!anyMoreContentInThisRow)
                        extraRows--;
                }
                while (anyMoreContentInThisRow || (extraRows > 0 && row < rows - 1));
            }

            // Output the last line
            if (LeftMargin > 0)
                outputString(new string(' ', LeftMargin));
            outputColoredString(new ConsoleColoredString(currentLine.ToArray()));
            outputString(Environment.NewLine);
        }

        private abstract class cell { }
        private class surrogateCell : cell
        {
            public int RealRow, RealCol;
            public override string ToString() { return "{" + RealCol + ", " + RealRow + "}"; }
        }
        private class trueCell : cell
        {
            public object Value;                               // either string, ConsoleColoredString, or EggsNode
            public object[] WordwrappedValue;    // either string[] or ConsoleColoredString[]
            public int WordwrappedIndex, ColSpan, RowSpan;
            public bool NoWrap;
            public Alignment? Alignment; // if null, use TextTable.DefaultAlignment
            public override string ToString() { return Value.ToString(); }

            private int? _cachedLongestWord = null;
            private int? _cachedLongestPara = null;
            public int LongestWord()
            {
                return getLongest(' ', false, ref _cachedLongestWord);
            }
            public int LongestParagraph()
            {
                return getLongest('\n', true, ref _cachedLongestPara);
            }
            public int MinWidth()
            {
                return NoWrap ? LongestParagraph() : LongestWord();
            }
            private int getLongest(char sep, bool para, ref int? cache)
            {
                if (cache == null)
                {
                    if (Value is string || Value is ConsoleColoredString)
                        cache = Value.ToString().Split(sep).Max(s => s.Length);
                    else
                    {
                        int longestSoFar = 0;
                        int curLength = 0;
                        longestEggWalk((EggsNode) Value, ref longestSoFar, ref curLength, false, para);
                        cache = longestSoFar;
                    }
                }
                return cache.Value;
            }
            private void longestEggWalk(EggsNode node, ref int longestSoFar, ref int curLength, bool curNowrap, bool para)
            {
                if (node is EggsText && curNowrap && !para)
                {
                    curLength += ((EggsText) node).Text.Length;
                    if (curLength > longestSoFar)
                        longestSoFar = curLength;
                    return;
                }
                else if (node is EggsText)
                {
                    var i = 0;
                    var txt = ((EggsText) node).Text;
                    while (i < txt.Length)
                    {
                        if (!para && char.IsWhiteSpace(txt, i))
                            curLength = 0;
                        else if (para && txt[i] == '\n')
                            curLength = 0;
                        else
                        {
                            curLength++;
                            if (curLength > longestSoFar)
                                longestSoFar = curLength;
                        }
                        i += char.IsSurrogate(txt, i) ? 2 : 1;
                    }
                    return;
                }
                else if (node is EggsGroup)
                {
                    foreach (var child in ((EggsGroup) node).Children)
                        longestEggWalk(child, ref longestSoFar, ref curLength, curNowrap, para);
                    return;
                }
                else
                {
                    var tag = (EggsTag) node;
                    foreach (var childList in tag.Children)
                        foreach (var child in childList)
                            longestEggWalk(child, ref longestSoFar, ref curLength, curNowrap || tag.Tag == '+', para);
                }
            }

            public void Wordwrap(int wrapWidth)
            {
                if (Value is string)
                    WordwrappedValue = ((string) Value).WordWrap(wrapWidth).Cast<object>().ToArray();
                else if (Value is ConsoleColoredString)
                    WordwrappedValue = ((ConsoleColoredString) Value).WordWrap(wrapWidth).Cast<object>().ToArray();
                else     // assume EggsNode
                    WordwrappedValue = ConsoleColoredString.FromEggsNodeWordWrap((EggsNode) Value, wrapWidth).Cast<object>().ToArray();
                WordwrappedIndex = 0;
            }
        }
        private List<List<cell>> _cells = new List<List<cell>>();

        private void ensureCell(int col, int row)
        {
            while (row >= _cells.Count)
                _cells.Add(new List<cell>());
            while (col >= _cells[row].Count)
                _cells[row].Add(null);
        }

        // Distributes 'width' evenly over the columns from 'col' - 'colspan' + 1 to 'col'.
        private void distributeEvenly(int[] colWidths, int col, int colSpan, int width)
        {
            if (width <= 0) return;
            var each = width / colSpan;
            for (var i = 0; i < colSpan; i++)
                colWidths[col - i] += each;
            var gap = width - (colSpan * each);
            for (var i = 0; i < gap; i++)
                colWidths[col - (i * colSpan / gap)]++;
        }

        private int[] generateColumnWidths(int cols, SortedDictionary<int, List<int>>[] cellsByColspan, Func<trueCell, int> getMinWidth)
        {
            var columnWidths = new int[cols];
            for (int col = 0; col < cols; col++)
                foreach (var kvp in cellsByColspan[col])
                    distributeEvenly(
                        columnWidths,
                        col,
                        kvp.Key,
                        kvp.Value.Select(row => (trueCell) _cells[row][col - kvp.Key + 1]).Max(getMinWidth) - columnWidths.Skip(col - kvp.Key + 1).Take(kvp.Key).Sum() - (kvp.Key - 1) * ColumnSpacing
                    );
            return columnWidths;
        }
    }
}
