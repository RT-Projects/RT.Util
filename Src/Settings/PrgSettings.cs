using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace RT.Util.Settings
{
    public enum SettingsMode
    {
        /// <summary>
        /// Settings stored in a text file named [ExeFile].Settings.txt
        /// </summary>
        TextFileInAppDir,

        /// <summary>
        /// Settings stored in a binary file named [ExeFile].Settings.dat
        /// </summary>
        BinaryFileInAppDir,

        /// <summary>
        /// Settings stored in the registry. If the root of the path is "User" or
        /// "Machine" then the setting is stored in HKCU or HKLM respectively, under
        /// Software/[CompanyName]/[AppName]/rest/of/the/path. Otherwise, HKCU is
        /// assumed.
        /// </summary>
        Registry,
    }

    public static class PrgSettings
    {
        /// <summary>
        /// This defines the settings model for the application, i.e. how and where
        /// the settings are stored. Ideally this should be set before Application.Run
        /// and never changed afterwards.
        /// </summary>
        public static SettingsMode Mode = SettingsMode.BinaryFileInAppDir;

        /// <summary>
        /// The settings store through which the settings are accessed. This object is managed
        /// by the PrgSettings class (created when necessary etc)
        /// </summary>
        public static SettingsStore Store = null;

        /// <summary>
        /// Clears all the settings. Not sure why one would use this, except perhaps for
        /// some sort of Reset All Settings button. Make sure the Mode is configured
        /// correctly when doing this.
        /// </summary>
        public static void Clear()
        {
            if (Mode == SettingsMode.BinaryFileInAppDir)
                Store = new SettingsBinaryFile();
            else if (Mode == SettingsMode.TextFileInAppDir)
                Store = new SettingsTextFile();
            else
                throw new NotImplementedException("This Mode is not implemented.");
        }

        /// <summary>
        /// Loads all settings using the specified mode. Call this in Program.cs just before
        /// any user code is executed.
        /// </summary>
        public static void Load(SettingsMode mode)
        {
            Mode = mode;
            Load();
        }

        /// <summary>
        /// Loads all the settings using the mode specified in the Mode variable. Call this in
        /// Program.cs just before any user code is executed.
        /// </summary>
        public static void Load()
        {
            if (Mode == SettingsMode.BinaryFileInAppDir)
            {
                SettingsBinaryFile sf = new SettingsBinaryFile();
                Store = sf;
                string fname = Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.dat";
                if (File.Exists(fname))
                    sf.LoadFromFile(fname);
            }
            else if (Mode == SettingsMode.TextFileInAppDir)
            {
                SettingsTextFile sf = new SettingsTextFile();
                Store = sf;
                string fname = Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt";
                if (File.Exists(fname))
                    sf.LoadFromFile(fname);
            }
            else
                throw new NotImplementedException("This Mode is not implemented.");
        }

        /// <summary>
        /// Saves all program settings. Call this in Program.cs after the Application.Run
        /// method returns.
        /// </summary>
        public static void Save()
        {
            // If the mode is not correct then the cast of the Store will throw an
            // exception, so no extra checks are needed.
            if (Mode == SettingsMode.BinaryFileInAppDir)
            {
                SettingsBinaryFile sf = (SettingsBinaryFile)Store;
                sf.SaveToFile(Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.dat");
            }
            else if (Mode == SettingsMode.TextFileInAppDir)
            {
                SettingsTextFile sf = (SettingsTextFile)Store;
                sf.SaveToFile(Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt");
            }
        }
    }
}
