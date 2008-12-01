using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using RT.Util.Dialogs;

namespace RT.Util.Settings
{
    /// <summary>
    /// Maintains program settings. This class may not be the best thing for new projects.
    /// </summary>
    public static class PrgSettings
    {
        /// <summary>
        /// The settings store through which the settings are accessed. The main purpose of
        /// PrgSettings is to keep The One instance of the settings store accessible to all
        /// classes wanting to load/save settings.
        /// </summary>
        public static SettingsStore Store;

        /// <summary>
        /// Clears all the settings. Not sure why one would use this, except perhaps for
        /// some sort of Reset All Settings button.
        /// </summary>
        public static void Clear()
        {
            throw new NotImplementedException("Not implemented");
        }

        /// <summary>
        /// Loads all settings using the specified Store. Call this in Program.cs just before
        /// any user code is executed, passing it a newly constructed instance of a store.
        /// </summary>
        public static void LoadSettings(SettingsStore TheStore)
        {
            Store = TheStore;
            Store.LoadSettings();
        }

        /// <summary>
        /// Saves all program settings. Call this in Program.cs after the Application.Run
        /// method returns. This method will not throw an exception - it will instead display
        /// an error message in case there is a problem.
        /// </summary>
        public static void SaveSettings()
        {
            try
            {
                Store.SaveSettings();
            }
            catch (Exception E)
            {
                DlgMessage.ShowWarning("Failed to save settings:\n" + E.Message + "\n\nDisk full?");
            }
        }
    }
}
