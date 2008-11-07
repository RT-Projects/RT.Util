using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RT.Util.Settings
{
    /// <summary>
    /// A text file settings store.
    /// </summary>
    public class SettingsTextFile : SettingsStore
    {
        /// <summary>
        /// Loads the settings from a file in the application directory
        /// </summary>
        public override void LoadSettings()
        {
            string fname = PathUtil.AppPath + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt";
            if (File.Exists(fname))
                LoadFromFile(fname);
        }

        /// <summary>
        /// Saves the settings to a file in the application directory
        /// </summary>
        public override void SaveSettings()
        {
            SaveToFile(PathUtil.AppPath + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt");
        }

        public void LoadFromFile(string name)
        {
            string[] lines = File.ReadAllLines(name, Encoding.UTF8);
            List<string> path = new List<string>();
            Regex rgSection = new Regex(@"^\s*(\[+)\s*(.*?)\s*(\]+)\s*$");
            Regex rgValue = new Regex(@"^\s*([^=]*?)\s*=\s*(.*?)\s*$");

            Data = new Dir();
            foreach (string ln in lines)
            {
                if (ln.Trim().Length == 0)
                    continue;

                Match ms = rgSection.Match(ln);
                Match mv = rgValue.Match(ln);

                //if (ms.Success)
                //{
                //    // Section matches
                //    if (ms.Groups[1].Length != ms.Groups[3].Length)
                //        throw new Exception("Cannot parse line: " + ln);
                //    cursec = UnmakeSingleString(ms.Groups[2].Value);
                //    if (!Data.ContainsKey(cursec))
                //        Data.Add(cursec, new Dictionary<string, string>());
                //}
                //else if (mv.Success)
                //{
                //    // Data matches
                //    Data[cursec][UnmakeSingleString(mv.Groups[1].Value)] =
                //        UnmakeSingleString(mv.Groups[2].Value);
                //}
                //else
                //    throw new Exception("Cannot parse line: " + ln);
            }
        }

        public void SaveToFile(string name)
        {
            List<string> lines = new List<string>();
            SaveDir("", 0, Data, lines);
            File.WriteAllLines(name, lines.ToArray(), Encoding.UTF8);
        }

        private Dir LoadDir()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        private void SaveDir(string dirname, int dirdepth, Dir dir, List<string> lines)
        {
            // Section name
            string sn = dirname;
            string sp = "";
            for (int i=0; i<dirdepth; i++)
            {
                sn = "[" + sn + "]";
                sp = sp + " ";
            }
            if (dirdepth != 0)
            {
                if ((dirdepth == 1) && (lines.Count != 0))
                    lines.Add("");
                lines.Add(sn);
            }
            // Values
            foreach (KeyValuePair<string, object> kvp in dir.Vals)
                lines.Add(sp + MakeSingleString(kvp.Key) + " = " + Stringify(kvp.Value));
            // Subdirs
            foreach (KeyValuePair<string, Dir> kvp in dir.Dirs)
                SaveDir(kvp.Key, dirdepth+1, kvp.Value, lines);
        }

        private string Stringify(object p)
        {
            IFormatProvider fmt = CultureInfo.InvariantCulture.NumberFormat;
            if (p is int)
                return "i:"+((int)p).ToString(fmt);
            else if (p is long)
                return "l:"+((long)p).ToString(fmt);
            else if (p is double)
                return "d:"+((double)p).ToString(fmt);
            else if (p is decimal)
                return "m:"+((decimal)p).ToString(fmt);
            else if (p is bool)
                return "b:"+((bool)p).ToString(fmt);
            else if (p is string)
                return "s:"+MakeSingleString(p as string);
            else
                return "<TYPE STRINGIFICATION HAS NOT BEEN IMPLEMENTED>";
        }

        public static string MakeSingleString(string val)
        {
            val = val.Replace(@"\", @"\\");
            val = val.Replace("\r", @"\R");
            val = val.Replace("\n", @"\N");
            return val;
        }

        public static string UnmakeSingleString(string val)
        {
            val = Regex.Replace(val, @"(^|[^\\])(\\\\)*\\R", "$1\r");
            val = Regex.Replace(val, @"(^|[^\\])(\\\\)*\\N", "$1\n");
            val = val.Replace(@"\\", @"\");
            return val;
        }

    }
}
