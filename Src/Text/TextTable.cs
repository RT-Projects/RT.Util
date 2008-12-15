using System;
using System.Collections.Generic;
using System.Text;
using RT.Util.ExtensionMethods;
using System.Linq;

namespace RT.Util.Text
{
    /// <summary>
    /// Holds a table of strings. Provides methods to print the table to text
    /// assuming a fixed-width font. Will wrap the cell contents as necessary.
    /// Supports automatic resizing of columns to fit the required width.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Will not necessarily lay out the table optimally in cases where
    /// there are multiple auto-size columns and one of the following
    /// is true:</para>
    /// <list type="bullet">
    /// <item>there are multiple paragraphs in some cells</item>
    /// <item>length of the contents of the cells in the same column
    ///     vary a lot, e.g. most cells are tiny but one is really
    ///     really long.</item>
    /// </list>
    /// </remarks>
    public class TextTable
    {
        private class TextColumn
        {
            private readonly List<string> _rows = new List<string>();
            private readonly List<int> _maxParaLength = new List<int>();

            /// <summary>
            /// Gets/sets the text associated with the specified row index.
            /// The behaviour is as if the column had an infinite number of
            /// rows all initially set to "". Storing null results in the
            /// empty string being stored.
            /// </summary>
            public string this[int row]
            {
                get
                {
                    if (row < _rows.Count)
                        return _rows[row];
                    else
                        return "";
                }

                set
                {
                    if (row < _rows.Count && _maxParaLength[row] == _maxWidth)
                        _maxWidthNeedsRecalc = true; // removing a longest element

                    // Grow the list as necessary
                    while (row >= _rows.Count)
                    {
                        _rows.Add("");
                        _maxParaLength.Add(0);
                    }

                    _rows[row] = value == null ? "" : value;
                    _maxParaLength[row] = getMaxParaLength(_rows[row]);

                    if (_maxWidth < _maxParaLength[row])
                    {
                        _maxWidth = _maxParaLength[row];
                        _maxWidthNeedsRecalc = false; // adding THE longest element
                    }
                }
            }

            /// <summary>
            /// Recalculates max width by going through every value.
            /// </summary>
            private void UpdateMaxWidth()
            {
                _maxWidth = 0;
                for (int row = 0; row < _rows.Count; row++)
                {
                    _maxParaLength[row] = getMaxParaLength(_rows[row]);
                    if (_maxWidth < _maxParaLength[row])
                        _maxWidth = _maxParaLength[row];
                }

                _maxWidthNeedsRecalc = false;
            }

            /// <summary>
            /// Returns the length of the longest paragraph in the string.
            /// Paragraphs are separated by \n.
            /// </summary>
            private int getMaxParaLength(string str)
            {
                string[] para = str.Split('\n');
                int max = 0;

                foreach (string p in para)
                    if (max < p.Length)
                        max = p.Length;

                return max;
            }

            /// <summary>
            /// Keeps track of the longest string found so far.
            /// </summary>
            private int _maxWidth = 0;

            /// <summary>
            /// This is set to true whenever we're unsure if maxWidth is
            /// correct, and reset to false whenever that's certain.
            /// </summary>
            private bool _maxWidthNeedsRecalc = false;

            /// <summary>
            /// Returns the width of the widest row currently in this column.
            /// 
            /// The max width is evaluated in a cool semi-lazy fashion, keeping
            /// the max width up-to-date only when it's cheap or when it's
            /// truly necessary.
            /// </summary>
            public int MaxWidth
            {
                get
                {
                    if (_maxWidthNeedsRecalc)
                        UpdateMaxWidth();
                    return _maxWidth;
                }
            }

            /// <summary>
            /// The desired width for the column is stored here by the outer class.
            /// </summary>
            public double CalculatedWidth;

            /// <summary>
            /// Used by the outer class to keep track of whether this column is
            /// automatically sized or not.
            /// </summary>
            public bool AutoSize;

            public TextColumn(bool defaultAutoSize)
            {
                AutoSize = defaultAutoSize;
            }
        }

        /// <summary>
        /// Constructs a TextTable in which all columns initially have AutoSize set to false.
        /// Use <see cref="SetAutoSize"/> to set AutoSize for individual columns to true.
        /// </summary>
        public TextTable()
        {
            _defaultAutoSize = false;
        }

        /// <summary>
        /// Constructs a TextTable in which all columns initially have the specified AutoSize setting.
        /// Use <see cref="SetAutoSize"/> to change the AutoSize setting for individual columns.
        /// </summary>
        public TextTable(bool defaultAutoSize)
        {
            _defaultAutoSize = defaultAutoSize;
        }

        /// <summary>Remembers the default AutoSize setting for new columns.</summary>
        private readonly bool _defaultAutoSize;

        /// <summary>Holds a list of every column in the table.</summary>
        private readonly List<TextColumn> _cols = new List<TextColumn>();

        /// <summary>
        /// This is updated to hold the number of rows deduced from the
        /// furthest row to ever be assigned a value.
        /// </summary>
        private int _numRows = 0;

        /// <summary>
        /// Makes the <see cref="_cols"/> member long enough to be able to
        /// access the specified column.
        /// </summary>
        private void growAsNecessary(int columnIndex)
        {
            while (columnIndex >= _cols.Count)
                _cols.Add(new TextColumn(_defaultAutoSize));
        }

        /// <summary>
        /// Gets/sets the value of the specified cell. Behaves as
        /// if the table was infinite in both rows and columns. Will
        /// allocate storage as required upon a "set". Will return ""
        /// when accessing anything that hasn't been set before.
        /// </summary>
        public string this[int rowIndex, int columnIndex]
        {
            get
            {
                if (columnIndex < _cols.Count)
                    return _cols[columnIndex][rowIndex];
                else
                    return "";
            }

            set
            {
                growAsNecessary(columnIndex);

                _cols[columnIndex][rowIndex] = value;

                if (_numRows <= rowIndex)
                    _numRows = rowIndex + 1;
            }
        }

        /// <summary>
        /// Configures the auto-sizing of the specified column.
        /// </summary>
        public void SetAutoSize(int columnIndex, bool autoSize)
        {
            if (columnIndex < 0) throw new ArgumentException("Column index must not be negative");

            growAsNecessary(columnIndex);
            _cols[columnIndex].AutoSize = autoSize;

        }

        /// <summary>
        /// Generates text for the table using the specified settings.
        /// </summary>
        /// <param name="leftIndent">The amount of indent to add on the left.
        /// This is NOT counted as part of the maxTableWidth value.</param>
        /// <param name="maxTableWidth">The maximum width the entire table
        /// is allowed to be. Note that it may not actually be possible to
        /// accommodate this width because non-auto-size columns and the
        /// intra-column indent cannot be resized.</param>
        /// <param name="intraColumnIndent">The number of spaces to place
        /// between every column.</param>
        /// <param name="nullIfTooWide">If true, the function will return null
        /// whenever the table could not be fitted into the specified width.
        /// Otherwise returns the wider table in such cases.</param>
        public string GetText(int leftIndent, int maxTableWidth, int intraColumnIndent, bool nullIfTooWide)
        {
            StringBuilder sb = new StringBuilder();

            bool fits = autoSizeColumns(intraColumnIndent, maxTableWidth);

            if (nullIfTooWide && !fits)
                return null;

            for (int r = 0; r < _numRows; r++)
            {
                string[][] wwrap = new string[_cols.Count][];
                int maxSubrowCount = 0;

                // Iterate over columns to determine number of sub-rows
                for (int c = 0; c < _cols.Count; c++)
                {
                    wwrap[c] = this[r, c].WordWrap((int) _cols[c].CalculatedWidth).ToArray();
                    if (maxSubrowCount < wwrap[c].Length)
                        maxSubrowCount = wwrap[c].Length;
                }

                // Iterate over the sub-rows
                for (int subRow = 0; subRow < maxSubrowCount; subRow++)
                {
                    sb.Append(' ', leftIndent);

                    // Render this sub-row in each column
                    for (int c = 0; c < _cols.Count; c++)
                    {
                        string s = wwrap[c].Length <= subRow ? "" : wwrap[c][subRow];
                        sb.Append(s);
                        if (c < _cols.Count - 1)
                            sb.Append(' ', (int) _cols[c].CalculatedWidth - s.Length + intraColumnIndent);
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calculates the actual sizes of the auto-size columns based on the
        /// requested settings. Returns true if the table fits into the specified
        /// maximum width.
        /// </summary>
        private bool autoSizeColumns(int intraColumnIndent, int maxTableWidth)
        {
            int[] min = new int[_cols.Count];
            int[] cur = new int[_cols.Count];
            bool[] auto = new bool[_cols.Count];

            int mintot_auto = 0, curtot_auto = 0, curtot_fix = 0;

            // Pass 1: fill in the arrays and calculate the totals
            for (int i = 0; i < _cols.Count; i++)
            {
                cur[i] = _cols[i].MaxWidth;
                auto[i] = _cols[i].AutoSize;

                if (i != 0)
                    curtot_fix += intraColumnIndent;

                if (auto[i])
                {
                    min[i] = 2;
                    curtot_auto += cur[i];
                    mintot_auto += min[i];
                }
                else
                {
                    min[i] = _cols[i].MaxWidth;
                    curtot_fix += cur[i];
                }
            }

            // Pass 2 (and more): adjust the width of the columns which are still sizable.
            // Keep marking those that have reached their minimum width as "fixed" and trying
            // again until either the table is narrow enough or all columns are "fixed".
            while (curtot_auto > mintot_auto && curtot_auto + curtot_fix > maxTableWidth)
            {
                double ratio = (double) (maxTableWidth - curtot_fix) / curtot_auto;
                for (int i = 0; i < _cols.Count; i++)
                {
                    if (!auto[i])
                        continue;

                    curtot_auto -= cur[i];

                    cur[i] = (int) (_cols[i].MaxWidth * ratio);
                    if (cur[i] > _cols[i].MaxWidth)
                        cur[i] = _cols[i].MaxWidth;

                    if (cur[i] < min[i])
                    {
                        cur[i] = min[i];
                        curtot_fix += cur[i];
                    }
                    else
                    {
                        curtot_auto += cur[i];
                    }
                }
            }

            // Store the results
            for (int i = 0; i < _cols.Count; i++)
                _cols[i].CalculatedWidth = cur[i];

            // Return true if the table fits in the specified limits
            return curtot_auto + curtot_fix <= maxTableWidth;
        }
    }
}
