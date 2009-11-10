using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
                    settings = XmlClassify.LoadObjectFromXmlFile<TSettings>(filename);
                }
                catch
                {
                    settings = new TSettings();
                }
            }
        }

        /// <summary>
        /// Loads settings into the specified class, or, if not available, creates
        /// a new instance of the class.
        /// </summary>
        /// <remarks>
        /// The type <typeparamref name="TSettings"/> must have the <see cref="SettingsAttribute"/>specified,
        /// otherwise an exception will be thrown.
        /// </remarks>
        /// <param name="settings">Destination - the settings class will be placed here.</param>
        /// <typeparam name="TSettings">The type of the settings class.</typeparam>
        public static void LoadSettings<TSettings>(out TSettings settings) where TSettings : new()
        {
            var type = typeof(TSettings);
            var attr = type.GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            if (attr == null)
                throw new ArgumentException("In order to use this overload of LoadSettings on type {0}, the type must have a {1} on it".Fmt(type.FullName, typeof(SettingsAttribute).FullName), "TSettings");
            LoadSettings(out settings, attr.AppName);
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="settingsType">The type of the settings object.</param>
        /// <param name="appName">Application or module name - used to determine the file path</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        public static void SaveSettings(object settings, Type settingsType, string appName, OnFailure onFailure)
        {
            string filename = PathUtil.AppPathCombine(appName + ".settings.xml");

            if (onFailure == OnFailure.Throw)
            {
                XmlClassify.SaveObjectToXmlFile(settings, settingsType, filename);
            }
            else if (onFailure == OnFailure.DoNothing)
            {
                try { XmlClassify.SaveObjectToXmlFile(settings, settingsType, filename); }
                catch { }
            }
            else
            {
                while (true)
                {
                    try
                    {
                        XmlClassify.SaveObjectToXmlFile(settings, settingsType, filename);
                        break;
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
                };
            }
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <remarks>
        /// The type <typeparamref name="TSettings"/> must have the <see cref="SettingsAttribute"/>specified,
        /// otherwise an exception will be thrown.
        /// </remarks>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="settingsType">The type of the settings object.</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        public static void SaveSettings(object settings, Type settingsType, OnFailure onFailure)
        {
            var attr = settingsType.GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            if (attr == null)
                throw new ArgumentException("In order to use this overload of SaveSettings on type {0}, the type must have a {1} on it".Fmt(settingsType.FullName, typeof(SettingsAttribute).FullName), "TSettings");
            SaveSettings(settings, settingsType, attr.AppName, onFailure);
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <typeparam name="TSettings">The type of the settings object.</typeparam>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="appName">Application or module name - used to determine the file path</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        public static void SaveSettings<TSettings>(TSettings settings, string appName, OnFailure onFailure)
        {
            SaveSettings(settings, typeof(TSettings), appName, onFailure);
        }

        /// <summary>
        /// Saves the specified settings class into the appropriate location.
        /// </summary>
        /// <remarks>
        /// The type <typeparamref name="TSettings"/> must have the <see cref="SettingsAttribute"/>specified,
        /// otherwise an exception will be thrown.
        /// </remarks>
        /// <typeparam name="TSettings">The type of the settings object.</typeparam>
        /// <param name="settings">The settings class to be saved</param>
        /// <param name="onFailure">Specifies how failures should be handled</param>
        public static void SaveSettings<TSettings>(TSettings settings, OnFailure onFailure)
        {
            SaveSettings(settings, typeof(TSettings), onFailure);
        }
    }

    /// <summary>
    /// Provides a base class for settings classes, implementing certain common usage patterns.
    /// See remarks for detailed usage instructions.
    /// </summary>
    /// <remarks>
    /// <para>Derive a class from this one and add the fields you wish to persist. Mark those you don't want stored
    /// with the <see cref="XmlIgnoreAttribute"/>. You must mark the derived class with <see cref="SettingsAttribute"/>
    /// to specify the name for the settings file.</para>
    /// <para>Once the above is done, the settings can be saved by calling <see cref="Save"/>/<see cref="SaveQuiet"/>,
    /// depending on intent. To load, call <see cref="SettingsUtil.LoadSettings&lt;T&gt;(out T)"/>, making sure that the
    /// generic type is the type of your descendant class. For example:
    /// </para>
    /// <code>
    /// static class Program
    /// {
    ///     public static MySettings Settings;
    ///     
    ///     static void Main(string[] args)
    ///     {
    ///         SettingsUtil.LoadSettings(out Settings);
    ///         DoWork();
    ///         Settings.Save();
    ///     }
    /// }
    /// 
    /// [Settings("MyApplicationName")]
    /// class MySettings : SettingsBase
    /// {
    ///     public string SomeSetting = "initial/default value";
    /// }
    /// </code>
    /// 
    /// <para><see cref="SettingsThreadedBase"/> implements an extra method to enable settings to be
    /// queued for a save on a separate thread, to reduce the performance impact of less important saves.</para>
    /// </remarks>
    public abstract class SettingsBase
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
        [XmlIgnore]
        protected internal object _lock = new object();
        [XmlIgnore]
        protected internal Thread _saveThread;
        [XmlIgnore]
        protected internal SettingsBase _saveObj;
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        /// <summary>
        /// <para>Saves the settings. Intended to be used whenever it is absolutely vital to save the settings and
        /// bug the user if this fails.</para>
        /// <para>This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>,
        /// and will cancel any pending earlier (older) saves.</para>
        /// </summary>
        public void Save()
        {
            // Save must not be interrupted or superseded by a SaveThreaded
            lock (_lock)
            {
                if (_saveThread != null) // this can only ever occur in the Sleep/lock wait phase of the quick save thread
                {
                    _saveThread.Abort();
                    _saveThread = null;
                }
                SettingsUtil.SaveSettings(this, this.GetType(), SettingsUtil.OnFailure.ShowRetryOnly);
            }
        }

        /// <summary>
        /// <para>Saves the settings. Intended to be used whenever it is important to make sure the settings
        /// hit the disk, but the settings are not important enough to bug the user if this fails.</para>
        /// <para>This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>,
        /// and will cancel any pending earlier (older) saves.</para>
        /// </summary>
        public void SaveQuiet()
        {
            // SaveQuiet must not be interrupted or superseded by a SaveThreaded
            lock (_lock)
            {
                if (_saveThread != null) // this can only ever occur in the Sleep/lock wait phase of the quick save thread
                {
                    _saveThread.Abort();
                    _saveThread = null;
                }
                SettingsUtil.SaveSettings(this, this.GetType(), SettingsUtil.OnFailure.DoNothing);
            }
        }
    }

    /// <summary>
    /// Like <see cref="SettingsBase"/>, but implements and additional save method.
    /// </summary>
    public abstract class SettingsThreadedBase : SettingsBase
    {
        /// <summary>
        /// Must return a deep clone of this class. This will be used to create a snapshot of the settings
        /// at the time when <see cref="SaveThreaded"/> is called.
        /// </summary>
        protected abstract SettingsThreadedBase CloneForSaveThreaded();

        /// <summary>
        /// <para>Saves the settings. Intended for frequent use at any point where it would make sense to
        /// commit settings, but would not make sense to bug the user about any failures. This method
        /// is like <see cref="SettingsBase.SaveQuiet"/>, except that the actual save occurs slightly later on a separate
        /// thread. The method returns as soon as <see cref="CloneForSaveThreaded"/> returns.</para>
        /// <para>Note that this method is NOT guaranteed to save settings, but it usually will. Make sure
        /// you call <see cref="SettingsBase.Save"/> when you want to guarantee a save, especially just before the
        /// program terminates.</para>
        /// </summary>
        public void SaveThreaded()
        {
            lock (_lock)
            {
                _saveObj = CloneForSaveThreaded();
                if (_saveObj == null)
                    throw new InvalidOperationException("CloneForSaveThreaded returned null.");
                if (_saveThread == null)
                {
                    _saveThread = new Thread(saveThreadFunc);
                    _saveThread.IsBackground = true;
                    _saveThread.Start();
                }
            }
        }

        private void saveThreadFunc()
        {
            Thread.Sleep(2000);
            lock (_lock)
            {
                SettingsUtil.SaveSettings(_saveObj, _saveObj.GetType(), SettingsUtil.OnFailure.DoNothing);
                _saveThread = null;
            }
        }

    }

    /// <summary>
    /// Describes the intended usage of a "settings" class to <see cref="SettingsUtil"/> methods.
    /// </summary>
    [global::System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SettingsAttribute : Attribute
    {
        private string _appName;

        /// <summary>
        /// Creates an instance of this attribute.
        /// </summary>
        /// <param name="appName">The name of the settings file is formed from this <paramref name="appName"/>
        /// according to certain rules. This should normally be a string equal to the name of the application. Paths and
        /// extensions should be omitted.
        /// </param>
        public SettingsAttribute(string appName)
        {
            _appName = appName;
        }

        /// <summary>
        /// The name of the settings file is formed from this <see cref="AppName"/> according to certain rules.
        /// This should normally be a string equal to the name of the application. Paths and
        /// extensions should be omitted.
        /// </summary>
        public string AppName { get { return _appName; } }
    }
}
