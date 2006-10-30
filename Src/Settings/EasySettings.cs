using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;

namespace RT.Util.Settings
{
    public static class EasySettings
    {
        /// <summary>
        /// The Settings object. You can store any settings for your application in here.
        /// You may wish to use EasySettings.Get() and/or EasySettings.Set() if you find
        /// Dictionary<>'s API inconvenient.
        /// </summary>
        public static Dictionary<string, object> Settings
        {
            get
            {
                if (!FEverRead)
                    ReadSettings();
                return FSettings;
            }
        }

        private static Dictionary<string, object> FSettings;
        private static bool FEverRead = false;

        private static void ReadSettings()
        {
            FEverRead = true;
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = null;
            GZipStream gz = null;
            try
            {
                fs = new FileStream(Filename(), FileMode.Open, FileAccess.Read, FileShare.Read);
                gz = new GZipStream(fs, CompressionMode.Decompress);
                Dictionary<string, object> TryToLoad = 
                    (Dictionary<string, object>)bf.Deserialize(gz);
                FSettings = TryToLoad;
            }
            catch (Exception)
            {
                FSettings = new Dictionary<string, object>();
            }
            finally
            {
                if (gz != null) gz.Close();
                if (fs != null) fs.Close();
            }
        }

        /// <summary>
        /// Saves all settings to the application's setting file.
        /// This function should be called in Program.cs after Application.Run().
        /// </summary>
        public static void WriteSettings()
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fs = new FileStream(Filename(), FileMode.Create, FileAccess.Write, FileShare.Read);
            GZipStream gz = new GZipStream(fs, CompressionMode.Compress);
            bf.Serialize(gz, FSettings);
            gz.Close();
            fs.Close();
        }

        private static string Filename()
        {
            string Ret = Application.StartupPath;
            if (Ret[Ret.Length-1] != '\\')
                Ret += "\\";
            return Ret + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".cfg";
        }

        /// <summary>
        /// Sets a setting with the given Key to the given Value,
        /// overwriting any existing value for the given Key.
        /// </summary>
        /// <param name="Key">The name of the setting.</param>
        /// <param name="Value">The value to set the setting to.</param>
        public static void Set(string Key, object Value)
        {
            // Use Settings rather than FSettings because we need to trigger the get method
            if (Settings.ContainsKey(Key))
                Settings.Remove(Key);
            Settings.Add(Key, Value);
        }

        /// <summary>
        /// Returns the setting with the given Key, or Default if the setting doesn't exist.
        /// </summary>
        /// <param name="Key">The name of the setting.</param>
        /// <param name="Default">The default value in case the setting doesn't exist.</param>
        /// <returns>The requested setting or the specified default value.</returns>
        public static object Get(string Key, object DefaultValue)
        {
            // Use Settings rather than FSettings because we need to trigger the get method
            return Settings.ContainsKey(Key) ? Settings[Key] : DefaultValue;
        }

        /// <summary>
        /// Returns the setting with the given Key, or null if the setting doesn't exist.
        /// </summary>
        /// <param name="Key">The name of the setting.</param>
        /// <returns>The requested setting or null.</returns>
        public static object Get(string Key)
        {
            // Use Settings rather than FSettings because we need to trigger the get method
            return Settings.ContainsKey(Key) ? Settings[Key] : null;
        }
    }
}
