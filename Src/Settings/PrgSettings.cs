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
        /// Settings stored in a file named [ExeFile].Settings.txt
        /// </summary>
        FileInAppDir,
    }

    public enum SettingsState
    {
        OpenedForRead,
        OpenedForWrite,
        Closed
    }

    public static class PrgSettings
    {
        /// <summary>
        /// This defines the settings model for the application, i.e. how and where
        /// the settings are stored. Ideally this should be set before Application.Run
        /// and never changed afterwards.
        /// </summary>
        public static SettingsMode Mode = SettingsMode.FileInAppDir;

        /// <summary>
        /// Gets the current state of the settings object - opened (read/write) or closed.
        /// </summary>
        public static SettingsState State { get { return FState; } }
        private static SettingsState FState = SettingsState.Closed;

        /// <summary>
        /// The settings store through which the settings are accessed. This object is managed
        /// by the PrgSettings class (created when necessary etc)
        /// </summary>
        public static SettingsStore Store = null;

        /// <summary>
        /// Opens the settings store for reading settings from it. Should be done before accessing
        /// settings via the store. If the open fails Store will point to a blank Store, thus
        /// enabling the user to obtain defaults through the normal mechanism provided by the store.
        /// </summary>
        /// <returns>True if open succeeded.</returns>
        public static bool OpenForRead()
        {
            if (FState != SettingsState.Closed)
                throw new Exception("Cannot OpenForRead because PrgSettings is already open.");

            FState = SettingsState.OpenedForRead;

            if (Mode == SettingsMode.FileInAppDir)
            {
                SettingsFile sf = new SettingsFile();
                Store = sf;
                try
                {
                    sf.LoadFromFile(Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Opens the store for writing settings to it.
        /// </summary>
        /// <param name="StartFromClean">If true, the store will be clean after calling this
        ///        method. Otherwise it will contain any settings previously obtained in the
        ///        last OpenForRead operation, if applicable to the store in use (e.g. not
        ///        applicable to SettingsRegistry).</param>
        public static void OpenForWrite(bool StartFromClean)
        {
            if (FState != SettingsState.Closed)
                throw new Exception("Cannot OpenForWrite because PrgSettings is already open.");

            FState = SettingsState.OpenedForWrite;

            if (Mode == SettingsMode.FileInAppDir)
            {
                if (Store == null || !(Store is SettingsFile) || StartFromClean)
                {
                    Store = new SettingsFile();
                }
            }
        }

        /// <summary>
        /// Closes the store. For some stores this means flushing all settings written onto the
        /// non-volatile medium. For others, the physical writing could have happened by this
        /// time. Calling Close ensures equivalent behaviour for any store type.
        /// </summary>
        public static void Close()
        {
            if (FState == SettingsState.Closed)
                throw new Exception("Cannot Close because PrgSettings is already closed.");

            if (Mode == SettingsMode.FileInAppDir)
            {
                if (FState == SettingsState.OpenedForRead)
                {
                    // Nothing to be done for this Mode
                }
                else
                {
                    // Save the settings
                    SettingsFile sf = (SettingsFile)Store;
                    sf.SaveToFile(Ut.AppDir + Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".Settings.txt");
                }
            }

            FState = SettingsState.Closed;
        }
    }
}
