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
        /// It is a known design "feature" that BinaryWriter cannot serialize "null"
        /// so instead we serialise a special object that represents null.
        /// </summary>
        [Serializable]
        private struct NullObject { }

        /// <summary>
        /// Loads the settings from a file in the application directory
        /// </summary>
        public override void LoadSettings()
        {
            string fname = Ut.AppPath + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.dat";
            if (File.Exists(fname))
                LoadFromFile(fname);
        }

        /// <summary>
        /// Saves the settings to a file in the application directory
        /// </summary>
        public override void SaveSettings()
        {
            SaveToFile(Ut.AppPath + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.dat");
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
            int nvals;
            Dir dir = new Dir();
            // Load values
            nvals = (int)br.ReadUInt32Optim();
            for (int i=0; i<nvals; i++)
            {
                string name = br.ReadString();
                object value = BinFmt.Deserialize(br.BaseStream);
                if (value is NullObject) value = null;
                dir.Vals.Add(name, value);
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
            // Save values
            bw.WriteUInt32Optim((uint)dir.Vals.Count);
            foreach (KeyValuePair<string, object> kvp in dir.Vals)
            {
                bw.Write(kvp.Key);
                bw.Flush();
                BinFmt.Serialize(bw.BaseStream, kvp.Value == null ? new NullObject() : kvp.Value);
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
}
