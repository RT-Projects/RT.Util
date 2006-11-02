using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO.Compression;
using System.Globalization;
using RT.Util.Streams;
using RT.Util.Controls;
using System.Windows.Forms;

namespace RT.Util.Settings
{
    /// <summary>
    /// Binary file settings store. The settings are serialized into a
    /// gzipped binary stream.
    /// </summary>
    public class SettingsBinaryFile : SettingsStore
    {
        /// <summary>
        /// I don't even understand why BinaryFormatter doesn't have static methods
        /// for Serialize/Deserialize.
        /// </summary>
        private BinaryFormatter BinFmt = new BinaryFormatter();

        /// <summary>
        /// Please forgive this pointless struct with the stupid name, but it is a known
        /// design "feature" that BinaryWriter cannot serialize "null" unless it's
        /// the value of a field in a non-null object.
        /// </summary>
        [Serializable]
        private struct DT
        {
            public string NM;
            public object OB;
        }

        /// <summary>
        /// Loads the settings from a file in the application directory
        /// </summary>
        public override void LoadSettings()
        {
            string fname = Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.dat";
            if (File.Exists(fname))
                LoadFromFile(fname);
        }

        /// <summary>
        /// Saves the settings to a file in the application directory
        /// </summary>
        public override void SaveSettings()
        {
            SaveToFile(Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.dat");
        }

        /// <summary>
        /// Loads the settings from the specified file
        /// </summary>
        public void LoadFromFile(string filename)
        {
            FileStream fs = null;
            GZipStream gz = null;
            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                gz = new GZipStream(fs, CompressionMode.Decompress);
                // This serializes the entire data dictionary somewhat inefficiently
                // Data = (Dir)BinFmt.Deserialize(gz);
                // So instead we use our custom tree writing function
                Data = LoadDir(new BinaryReaderPlus(gz));
            }
            finally
            {
                if (gz != null) gz.Close();
                if (fs != null) fs.Close();
            }
        }

        /// <summary>
        /// Saves the settings to the specified file
        /// </summary>
        public void SaveToFile(string filename)
        {
            FileStream fs = null;
            GZipStream gz = null;
            try
            {
                fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
                gz = new GZipStream(fs, CompressionMode.Compress);
                // This serializes the entire data dictionary somewhat inefficiently
                // BinFmt.Serialize(gz, Data);
                // So instead we use our custom tree writing function
                SaveDir(Data, new BinaryWriterPlus(gz));
            }
            finally
            {
                if (gz != null) gz.Close();
                if (fs != null) fs.Close();
            }
        }

        /// <summary>
        /// Helper function to load dirs recursively
        /// </summary>
        private Dir LoadDir(BinaryReaderPlus br)
        {
            DT sillything = new DT();
            int nvals;
            Dir dir = new Dir();
            // Load values
            nvals = (int)br.ReadUInt32Optim();
            for (int i=0; i<nvals; i++)
            {
                sillything = (DT)BinFmt.Deserialize(br.BaseStream);
                dir.Vals.Add(sillything.NM, sillything.OB);
            }
            // Load dirs
            nvals = (int)br.ReadUInt32Optim();
            for (int i=0; i<nvals; i++)
            {
                string name = br.ReadString();
                dir.Dirs.Add(name, LoadDir(br));
            }
            return dir;
        }

        /// <summary>
        /// Helper function to save dirs recursively
        /// </summary>
        private void SaveDir(Dir dir, BinaryWriterPlus bw)
        {
            DT sillything = new DT();
            // Save values
            bw.WriteUInt32Optim((uint)dir.Vals.Count);
            foreach (KeyValuePair<string, object> kvp in dir.Vals)
            {
                sillything.NM = kvp.Key;
                sillything.OB = kvp.Value;
                bw.Flush();
                BinFmt.Serialize(bw.BaseStream, sillything);
            }
            // Save dirs
            bw.WriteUInt32Optim((uint)dir.Dirs.Count);
            foreach (KeyValuePair<string, Dir> kvp in dir.Dirs)
            {
                bw.Write(kvp.Key);
                SaveDir(kvp.Value, bw);
            }
        }

    }

    /// <summary>
    /// A text file settings store.
    /// TBD: This is quite incomplete.
    /// </summary>
    public class SettingsTextFile : SettingsStore
    {
        /// <summary>
        /// Loads the settings from a file in the application directory
        /// </summary>
        public override void LoadSettings()
        {
            string fname = Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt";
            if (File.Exists(fname))
                LoadFromFile(fname);
        }

        /// <summary>
        /// Saves the settings to a file in the application directory
        /// </summary>
        public override void SaveSettings()
        {
            SaveToFile(Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt");
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

            // Save
            File.WriteAllLines(name, lines.ToArray(), Encoding.UTF8);
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
