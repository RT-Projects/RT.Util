using System.Collections.Generic;
using System.IO;
using RT.Util.Collections;
using RT.Util.ExtensionMethods;
using RT.KitchenSink.Collections;

namespace RT.KitchenSink
{
    /// <summary>
    /// Holds a table of RVariant values, which can be saved to a file in CSV format.
    /// </summary>
    public sealed class CsvTable
    {
        private List<List<RVariant>> _data = new List<List<RVariant>>();

        /// <summary>"Selected" row index - affects some operations of the class.</summary>
        public int CurRow = 0;
        /// <summary>"Selected" column index - affects some operations of the class.</summary>
        public int CurCol = 0;

        /// <summary>
        /// Determines whether adding a cell moves the cursor to the right or down.
        /// </summary>
        public bool AdvanceRight = true;

        /// <summary>
        /// Adds a value at the current location and advances the cursor.
        /// </summary>
        public void Add(RVariant value)
        {
            this[CurRow, CurCol] = value;
            AdvanceCursor();
        }

        /// <summary>
        /// Advances the cursor down or to the right, depending on the value of <see cref="AdvanceRight"/>.
        /// </summary>
        public void AdvanceCursor()
        {
            if (AdvanceRight)
                CurCol++;
            else
                CurRow++;
        }

        /// <summary>
        /// Moves cursor to the specified position.
        /// </summary>
        public void SetCursor(int row, int col)
        {
            CurRow = row;
            CurCol = col;
        }

        /// <summary>
        /// Gets/sets the value at the specified position. Returns a "blank" (stub) <see cref="RVariant"/>
        /// if the value hasn't been set yet.
        /// </summary>
        public RVariant this[int row, int col]
        {
            get
            {
                if (row < _data.Count && col < _data[row].Count)
                    return _data[row][col];
                else
                    return new RVariant();
            }
            set
            {
                while (row >= _data.Count)
                    _data.Add(new List<RVariant>());
                while (col >= _data[row].Count)
                    _data[row].Add(new RVariant());

                _data[row][col] = value;
            }
        }

        /// <summary>
        /// Saves the table to a file in Excel-compatible CSV format.
        /// </summary>
        public void SaveToFile(string name)
        {
            StreamWriter wr = new StreamWriter(name);
            foreach (var row in _data)
            {
                foreach (var cell in row)
                {
                    if (cell.Kind == RVariantKind.Stub)
                        wr.Write(",");
                    else
                        wr.Write("\"" + cell.ToString().Replace("\"", "\"\"") + "\",");
                }
                wr.WriteLine();
            }
            wr.Close();
        }

        /// <summary>
        /// Saves the table to a file in Excel-compatible CSV format.
        /// </summary>
        public void SaveToFileXls(string name)
        {
            StreamWriter wr = new StreamWriter(name);
            wr.WriteLine(@"<html><meta http-equiv=""Content-Type"" content=""text/html"" charset=""utf-8"" /><table>");
            foreach (var row in _data)
            {
                wr.Write(@"<tr>");
                foreach (var cell in row)
                {
                    if (cell.Kind == RVariantKind.Stub)
                        wr.Write(@"<td></td>");
                    else
                        wr.Write(@"<td>" + cell.ToString().HtmlEscape() + @"</td>");
                }
                wr.WriteLine(@"</tr>");
            }
            wr.WriteLine(@"</table></html>");
            wr.Close();
        }
    }
}
