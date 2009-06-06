using System;
using System.Collections.Generic;
using System.IO;
using RT.Util.Dialogs;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.Util
{
    /// <summary>
    /// Provides convenience methods for loading and saving application settings.
    /// </summary>
    public static class SettingsUtil
    {
        // For the future:
        //- allow xmlclassify to take a lambda which would check whether a certain field should be saved
        //- optionally mark fields in the settings class as per-pc or per-user using special attributes
        //- add SettingsUtil which provide two generic methods for loading and saving settings as follows:
        //  - determine whether to save into application directory by checking if a certain file exists in it
        //  - if so, save per-pc settings into "appname.pcname.settings.xml" and per-user settings into "appname.pcname.username.settings.xml"
        //  - otherwise use OS-provided directories for this purpose, i.e. C:\Users in Vista

        /// <summary>
        /// Specifies what to do in case of operation failing.
        /// </summary>
        public enum OnFailure
        {
            /// <summary>Just ignore the failure: no exceptions thrown, no dialogs shown</summary>
            DoNothing,
            /// <summary>Throw an exception in case of failure</summary>
            Throw,
            /// <summary>Ask the user to retry or to skip operation. No exceptions thrown.</summary>
            ShowRetryOnly,
            /// <summary>Ask the user to retry, skip operation or cancel. <see cref="CancelException"/> thrown on cancel.</summary>
            ShowRetryWithCancel,
        }

        /// <summary>
        /// Indicates that the user chose to cancel the current operation.
        /// </summary>
        public class CancelException : RTException
        {
             /// <summary>Creates an exception instance with the specified message.</summary>
             public CancelException()
                 : base("User chose to cancel the operation")
             { }
        }

        /// <summary>
        /// Loads settings into the specified class, or, if not available, creates
        /// a new instance of the class.
        /// </summary>
        /// <param name="settings">Destination - the settings class will be placed here</param>
        /// <param name="appName">Application or module name - used to determine the file path</param>
        public static void LoadSettings<TSettings>(out TSettings settings, string appName) where TSettings : new()
        {
            string filename = PathUtil.AppPathCombine(appName + ".settings.xml");
            if (!File.Exists(filename))
            {
                settings = new TSettings();
            }
            else
            {
                try
                {
                    settings = XmlClassify.LoadObjectFromXmlFile<TSettings>(appName);
                }
                catch
                {
                    settings = new TSettings();
                }
            }
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="appName">Application or module name - used to determine the file path</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        public static void SaveSettings<TSettings>(TSettings settings, string appName, OnFailure onFailure) where TSettings : new()
        {
            string filename = PathUtil.AppPathCombine(appName + ".settings.xml");

            if (onFailure == OnFailure.Throw)
            {
                XmlClassify.SaveObjectToXmlFile(settings, filename);
            }
            else if (onFailure == OnFailure.DoNothing)
            {
                try { XmlClassify.SaveObjectToXmlFile(settings, filename); }
                catch { }
            }
            else
            {
                do
                {
                    try
                    {
                        XmlClassify.SaveObjectToXmlFile(settings, filename);
                    }
                    catch (Exception e)
                    {
                        var choices = new List<string>() { "Try &again", "&Don't save settings" };
                        if (onFailure == OnFailure.ShowRetryWithCancel)
                            choices.Add("&Cancel");
                        int choice = DlgMessage.ShowWarning("Program settings could not be saved.\n({0})\n\nWould you like to try again?".Fmt(e.Message), choices.ToArray());
                        if (choice == 1)
                            return;
                        if (choice == 2)
                            throw new CancelException();
                    }
                } while (true);
            }
        }
    }
}
