﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using RT.PostBuild;
using RT.Serialization;
using RT.Util.ExtensionMethods;
using RT.Util.Forms;

namespace RT.Util
{
    /// <summary>Provides convenience methods for loading and saving application settings.</summary>
    public static class SettingsUtil
    {
        /// <summary>
        ///     Retrieves the mandatory <see cref="SettingsAttribute"/> for the specified settings class type. Throws if the
        ///     type doesn't have it specified.</summary>
        /// <param name="settingsType">
        ///     Type of the settings class whose attribute is to be retrieved</param>
        public static SettingsAttribute GetAttribute(Type settingsType)
        {
            var attr = settingsType.GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault();
            if (attr == null)
                throw new ArgumentException("The type {0} must have a {1} on it to be used with SettingsUtil.".Fmt(settingsType.FullName, typeof(SettingsAttribute).FullName), "settingsType");
            return attr;
        }

        /// <summary>
        ///     Retrieves the mandatory <see cref="SettingsAttribute"/> for the type <typeparamref name="TSettings"/>. Throws
        ///     if the type doesn't have it specified.</summary>
        /// <typeparam name="TSettings">
        ///     Type of the settings class whose attribute is to be retrieved</typeparam>
        public static SettingsAttribute GetAttribute<TSettings>() where TSettings : SettingsBase, new()
        {
            return GetAttribute(typeof(TSettings));
        }

        /// <summary>
        ///     Performs safety checks to ensure that a settings object conforms to various requirements imposed by
        ///     SettingsUtil methods. Run this method as a post-build step to ensure reliability of execution. For an example
        ///     of use, see <see cref="PostBuildChecker.RunPostBuildChecks"/>. This method is available only in DEBUG mode.</summary>
        /// <typeparam name="TSettings">
        ///     The type of the settings object, derived from <see cref="SettingsBase"/>, which would be passed to
        ///     SettingsUtil methods at normal run-time.</typeparam>
        /// <param name="rep">
        ///     Object to report post-build errors to.</param>
        public static void PostBuildStep<TSettings>(IPostBuildReporter rep) where TSettings : SettingsBase
        {
            SettingsAttribute attr;
            try
            {
                attr = GetAttribute(typeof(TSettings));
            }
            catch (Exception e)
            {
                rep.Error(e.Message, "class", typeof(TSettings).Name);
                return;
            }

            switch (attr.Serializer)
            {
                case SettingsSerializer.ClassifyXml:
                case SettingsSerializer.ClassifyJson:
                case SettingsSerializer.ClassifyBinary:
                    Classify.PostBuildStep<TSettings>(rep);
                    break;
            }
        }

        /// <summary>
        ///     Loads settings into the specified class, or, if not available, creates a new instance of the class. See
        ///     Remarks.</summary>
        /// <remarks>
        ///     If the settings file exists but can't be loaded, this function will automatically create a backup of the
        ///     settings file. If the file is opened exclusively by other code, will retry reading from it for up to 1.5
        ///     seconds.</remarks>
        /// <param name="settings">
        ///     Destination - the settings class will be placed here</param>
        /// <param name="filename">
        ///     If specified, overrides the filename that is normally derived from the values specified in the <see
        ///     cref="SettingsAttribute"/> on the settings class.</param>
        /// <param name="serializer">
        ///     If specified, overrides the serializer specified in the <see cref="SettingsAttribute"/> on the settings class.</param>
        /// <returns>
        ///     true if loaded an existing file, false if created a new one.</returns>
        public static bool LoadSettings<TSettings>(out TSettings settings, string filename = null, SettingsSerializer? serializer = null) where TSettings : SettingsBase, new()
        {
            var attr = GetAttribute<TSettings>();
            if (filename == null)
                filename = attr.GetFileName();
            if (serializer == null)
                serializer = attr.Serializer;

            bool success = false, tryLoad = true;
            if (!File.Exists(filename))
            {
                tryLoad = false;
                foreach (var field in typeof(SettingsSerializer).GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    var fa = field.GetCustomAttributes<SerializerInfoAttribute>().First();
                    if (fa == null || !fa.AutoConvertFrom)
                        continue;
                    filename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "." + fa.DefaultFileExtension);
                    if (File.Exists(filename))
                    {
                        serializer = (SettingsSerializer) field.GetValue(null);
                        tryLoad = true;
                        break;
                    }
                }
            }

            if (tryLoad)
            {
                try
                {
                    settings = deserialize<TSettings>(filename, serializer.Value);
                    success = true;
                }
                catch (XmlException) { settings = new TSettings(); }
                catch (IOException) { settings = new TSettings(); }
                catch (SerializationException) { settings = new TSettings(); }

                if (!success)
                {
                    try { File.Copy(filename, PathUtil.AppendBeforeExtension(filename, ".LoadFailedBackup"), overwrite: true); }
                    catch { }
                }
            }
            else
                settings = new TSettings();

            settings.AfterLoad();
            return success;
        }

        internal static void save(SettingsBase settings, string filename, SettingsSerializer? serializer, SettingsOnFailure onFailure)
        {
            var settingsType = settings.GetType();
            var attr = GetAttribute(settingsType);
            if (filename == null)
                filename = attr.GetFileName();
            if (serializer == null)
                serializer = attr.Serializer;

            settings.BeforeSave();

            if (onFailure == SettingsOnFailure.Throw)
            {
                serialize(settings, settingsType, filename, serializer.Value);
            }
            else if (onFailure == SettingsOnFailure.DoNothing)
            {
                try { serialize(settings, settingsType, filename, serializer.Value); }
                catch { }
            }
            else
            {
                while (true)
                {
                    try
                    {
                        serialize(settings, settingsType, filename, serializer.Value);
                        break;
                    }
                    catch (Exception e)
                    {
                        var choices = new List<string>() {
                            "Try &again",
                            "&Don't save settings",
                        };
                        int cancelIndex = -1;
                        if (onFailure == SettingsOnFailure.ShowRetryWithCancel)
                        {
                            cancelIndex = choices.Count;
                            choices.Add("&Cancel");
                        }
#if DEBUG
                        int breakIndex = choices.Count;
                        choices.Add("&Break debugger");
#endif
                        int choice = DlgMessage.ShowWarning("Program settings could not be saved.\n({0})\n\nWould you like to try again?".Fmt(e.Message), choices.ToArray());
                        if (choice == 1)
                            return;
                        if (choice == cancelIndex)
                            throw new SettingsCancelException();
#if DEBUG
                        if (choice == breakIndex)
                            System.Diagnostics.Debugger.Break();
#endif
                    }
                };
            }
        }

        internal static void serialize(object settings, Type settingsType, string filename, SettingsSerializer serializer)
        {
            var tempname = filename + ".~tmp";
            Ut.WaitSharingVio(() =>
            {
                switch (serializer)
                {
                    case SettingsSerializer.ClassifyXml:
                        // SerializeToFile automatically creates the folder if necessary
                        ClassifyXml.SerializeToFile(settingsType, settings, tempname, format: ClassifyXmlFormat.Create("Settings"));
                        break;

                    case SettingsSerializer.ClassifyJson:
                        // SerializeToFile automatically creates the folder if necessary
                        ClassifyJson.SerializeToFile(settingsType, settings, tempname);
                        break;

                    case SettingsSerializer.ClassifyBinary:
                        // SerializeToFile automatically creates the folder if necessary
                        ClassifyBinary.SerializeToFile(settingsType, settings, tempname);
                        break;

                    default:
                        throw new InternalErrorException("4968453");
                }
                File.Delete(filename);
                File.Move(tempname, filename);
            }, TimeSpan.FromSeconds(5));
        }

        private static TSettings deserialize<TSettings>(string filename, SettingsSerializer serializer) where TSettings : SettingsBase, new()
        {
            return Ut.WaitSharingVio(maximum: TimeSpan.FromSeconds(5), func: () =>
            {
                switch (serializer)
                {
                    case SettingsSerializer.ClassifyXml:
                        return ClassifyXml.DeserializeFile<TSettings>(filename);

                    case SettingsSerializer.ClassifyJson:
                        return ClassifyJson.DeserializeFile<TSettings>(filename);

                    case SettingsSerializer.ClassifyBinary:
                        return ClassifyBinary.DeserializeFile<TSettings>(filename);

                    default:
                        throw new InternalErrorException("6843184");
                }
            });
        }
    }

    /// <summary>
    ///     Provides a base class for settings classes, implementing certain common usage patterns. See remarks for detailed
    ///     usage instructions.</summary>
    /// <remarks>
    ///     <para>
    ///         Derive a class from this one and add the fields you wish to persist. Mark those you don't want stored with the
    ///         <see cref="ClassifyIgnoreAttribute"/>. You must mark the derived class with <see cref="SettingsAttribute"/>.</para>
    ///     <para>
    ///         Once the above is done, the settings can be saved by calling <see cref="Save"/>/<see cref="SaveQuiet"/>,
    ///         depending on intent. To load, call <see cref="SettingsUtil.LoadSettings&lt;T&gt;"/>, making sure that the
    ///         generic type is the type of your descendant class. For example:</para>
    ///     <code>
    ///         static class Program
    ///         {
    ///             public static MySettings Settings;
    ///
    ///             static void Main(string[] args)
    ///             {
    ///                 SettingsUtil.LoadSettings(out Settings);
    ///                 DoWork();
    ///                 Settings.Save();
    ///             }
    ///         }
    ///
    ///         [Settings("MyApplicationName", SettingsKind.UserSpecific)]
    ///         class MySettings : SettingsBase
    ///         {
    ///             public string SomeSetting = "initial/default value";
    ///         }</code>
    ///     <para>
    ///         <see cref="SettingsThreadedBase"/> implements an extra method to enable settings to be queued for a save on a
    ///         separate thread, to reduce the performance impact of less important saves.</para></remarks>
    [Serializable]
    public abstract class SettingsBase
    {
        /// <summary>Lock object used to protect concurrent access.</summary>
        [ClassifyIgnore, NonSerialized]
        protected internal object _lock = new object();
        /// <summary>The thread on which the background saving is performed.</summary>
        [ClassifyIgnore, NonSerialized]
        protected internal Thread _saveThread;

        /// <summary>
        ///     This method is called just before the settings class is written out to disk, allowing any required changes to
        ///     be made to the fields. The base implementation does nothing. Note that this may be called on a different
        ///     thread than the one invoking a Save* operation (but the same as the thread performing the save immediately
        ///     after this method returns).</summary>
        protected internal virtual void BeforeSave()
        {
        }

        /// <summary>
        ///     This method is called just before the settings class is restored from disk, allowing any required changes to
        ///     be made to the fields. The base implementation does nothing.</summary>
        protected internal virtual void AfterLoad()
        {
        }

        /// <summary>
        ///     <para>
        ///         Saves the settings.</para>
        ///     <para>
        ///         This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>, and will cancel any
        ///         pending earlier (older) saves.</para></summary>
        public virtual void Save(string filename = null, SettingsSerializer? serializer = null, SettingsOnFailure onFailure = SettingsOnFailure.Throw)
        {
            // Save and delete must not be interrupted or superseded by a SaveThreaded
            lock (_lock)
            {
                if (_saveThread != null) // this can only ever occur in the Sleep/lock wait phase of the quick save thread
                {
                    _saveThread.Abort();
                    _saveThread = null;
                }
                SettingsUtil.save(this, filename, serializer, onFailure);
            }
        }

        /// <summary>
        ///     <para>
        ///         Saves the settings. Intended to be used whenever the settings are important enough to bug the user if this
        ///         fails.</para>
        ///     <para>
        ///         This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>, and will cancel any
        ///         pending earlier (older) saves.</para></summary>
        public virtual void SaveLoud(string filename = null, SettingsSerializer? serializer = null)
        {
            Save(filename, serializer, onFailure: SettingsOnFailure.ShowRetryOnly);
        }


        /// <summary>
        ///     <para>
        ///         Saves the settings. Intended to be used whenever the settings are not important enough to bug the user if
        ///         this fails.</para>
        ///     <para>
        ///         This method is fully compatible with <see cref="SettingsThreadedBase.SaveThreaded"/>, and will cancel any
        ///         pending earlier (older) saves.</para></summary>
        public virtual void SaveQuiet(string filename = null, SettingsSerializer? serializer = null)
        {
            Save(filename, serializer, onFailure: SettingsOnFailure.DoNothing);
        }

        /// <summary>Deletes the settings file.</summary>
        public virtual void Delete(string filename = null, SettingsOnFailure onFailure = SettingsOnFailure.Throw)
        {
            // Save and delete must not be interrupted or superseded by a SaveThreaded
            lock (_lock)
            {
                if (_saveThread != null) // this can only ever occur in the Sleep/lock wait phase of the quick save thread
                {
                    _saveThread.Abort();
                    _saveThread = null;
                }
                var attr = SettingsUtil.GetAttribute(GetType());
                if (filename == null)
                    filename = attr.GetFileName();

                if (onFailure == SettingsOnFailure.Throw)
                {
                    File.Delete(filename);
                }
                else if (onFailure == SettingsOnFailure.DoNothing)
                {
                    try { File.Delete(filename); }
                    catch { }
                }
                else
                {
                    while (true)
                    {
                        try
                        {
                            File.Delete(filename);
                            break;
                        }
                        catch (Exception e)
                        {
                            var choices = new List<string>() { "Try &again", "&Don't delete settings" };
                            if (onFailure == SettingsOnFailure.ShowRetryWithCancel)
                                choices.Add("&Cancel");
                            int choice = DlgMessage.ShowWarning("Program settings could not be deleted.\n({0})\n\nWould you like to try again?".Fmt(e.Message), choices.ToArray());
                            if (choice == 1)
                                return;
                            if (choice == 2)
                                throw new SettingsCancelException();
                        }
                    };
                }
            }
        }

        /// <summary>
        ///     Gets the <see cref="SettingsAttribute"/> instance specified on this settings class, or null if none are
        ///     specified.</summary>
        public SettingsAttribute Attribute
        {
            get { return this.GetType().GetCustomAttributes<SettingsAttribute>(false).FirstOrDefault(); }
        }
    }

    /// <summary>Like <see cref="SettingsBase"/>, but implements an additional save method.</summary>
    public abstract class SettingsThreadedBase : SettingsBase
    {
        [ClassifyIgnore, NonSerialized]
        private SettingsBase _saveObj;
        [ClassifyIgnore, NonSerialized]
        private string _saveFilename;
        [ClassifyIgnore, NonSerialized]
        private SettingsSerializer? _saveSerializer;

        /// <summary>
        ///     Must return a deep clone of this class. This will be used to create a snapshot of the settings at the time
        ///     when <see cref="SaveThreaded"/> is called.</summary>
        protected abstract SettingsThreadedBase CloneForSaveThreaded();

        /// <summary>
        ///     <para>
        ///         Saves the settings. Intended for frequent use at any point where it would make sense to commit settings,
        ///         but would not make sense to bug the user about any failures. This method is like <see
        ///         cref="SettingsBase.SaveQuiet"/>, except that the actual save occurs slightly later on a separate thread.
        ///         The method returns as soon as <see cref="CloneForSaveThreaded"/> returns.</para>
        ///     <para>
        ///         Note that this method is NOT guaranteed to save settings, but it usually will. Make sure you call <see
        ///         cref="SettingsBase.Save"/> when you want to guarantee a save, especially just before the program
        ///         terminates.</para></summary>
        public virtual void SaveThreaded(string filename = null, SettingsSerializer? serializer = null)
        {
            lock (_lock)
            {
                _saveObj = CloneForSaveThreaded();
                _saveFilename = filename;
                _saveSerializer = serializer;
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
                SettingsUtil.save(_saveObj, _saveFilename, _saveSerializer, SettingsOnFailure.DoNothing);
                _saveThread = null;
            }
        }

    }

    /// <summary>Determines what the settings in the settings file are logically "attached" to.</summary>
    public enum SettingsKind
    {
        /// <summary>
        ///     These settings are specific to a particular computer. In normal mode: shared among all user accounts, and do
        ///     not roam. In portable mode: separate settings stored for every machine name; user account doesn't matter.</summary>
        MachineSpecific,

        /// <summary>
        ///     These settings are specific to a particular user. In normal mode: separate settings for each user account;
        ///     will roam to other machines if roaming is configured. In portable mode: always shared; user account and
        ///     machine name do not matter.</summary>
        UserSpecific,

        /// <summary>
        ///     These settings are specific to a particular combination of user and machine. In normal mode: separate settings
        ///     for each user account on each machine; will not roam. In portable mode: separate settings stored for every
        ///     machine name; user account doesn't matter.</summary>
        UserAndMachineSpecific,

        /// <summary>
        ///     These settings are intended to be global, with constraints imposed by reality. In normal mode: shared among
        ///     all user accounts, and do not roam. In portable mode: always shared; user account and machine name do not
        ///     matter.</summary>
        Global,
    }

    /// <summary>Specifies what to do in case of operation failing.</summary>
    public enum SettingsOnFailure
    {
        /// <summary>Just ignore the failure: no exceptions thrown, no dialogs shown</summary>
        DoNothing,
        /// <summary>Throw an exception in case of failure</summary>
        Throw,
        /// <summary>Ask the user to retry or to skip operation. No exceptions thrown.</summary>
        ShowRetryOnly,
        /// <summary>Ask the user to retry, skip operation or cancel. <see cref="SettingsCancelException"/> thrown on cancel.</summary>
        ShowRetryWithCancel,
    }

    /// <summary>Determines which serializer the settings are read/written by.</summary>
    public enum SettingsSerializer
    {
        /// <summary>Use the Classify serializer with the XML format.</summary>
        [SerializerInfo(defaultFileExtension: "xml", AutoConvertFrom = true)]
        ClassifyXml,
        /// <summary>Use the Classify serializer with the binary format.</summary>
        [SerializerInfo(defaultFileExtension: "dat", AutoConvertFrom = true)]
        ClassifyBinary,
        /// <summary>Use the Classify serializer with the JSON format.</summary>
        [SerializerInfo(defaultFileExtension: "json", AutoConvertFrom = true)]
        ClassifyJson,
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class SerializerInfoAttribute : Attribute
    {
        public string DefaultFileExtension { get; private set; }
        public bool AutoConvertFrom { get; set; }

        public SerializerInfoAttribute(string defaultFileExtension)
        {
            if (defaultFileExtension == null)
                throw new ArgumentNullException(nameof(defaultFileExtension));
            DefaultFileExtension = defaultFileExtension;
            AutoConvertFrom = false;
        }
    }

    /// <summary>Indicates that the user chose to cancel the current operation.</summary>
    public sealed class SettingsCancelException : Exception
    {
        /// <summary>Creates an exception instance with the specified message.</summary>
        public SettingsCancelException()
            : base("User chose to cancel the operation")
        { }
    }

    /// <summary>Describes the intended usage of a "settings" class to <see cref="SettingsUtil"/> methods.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe]
    public sealed class SettingsAttribute : Attribute
    {
        /// <summary>
        ///     Creates an instance of this attribute.</summary>
        /// <param name="appName">
        ///     The name of the settings file is formed from this <paramref name="appName"/> according to certain rules. This
        ///     should be a string equal to the name of the application. Paths and extensions should be omitted. It is
        ///     important to specify the same name for settings of different <paramref name="kind"/>, because this allows
        ///     their portability to be controlled with the same {name}.IsPortable.txt file.</param>
        /// <param name="kind">
        ///     Specifies what the settings in this settings class are logically "attached" to.</param>
        /// <param name="serializer">
        ///     Specifies which serializer to use.</param>
        public SettingsAttribute(string appName, SettingsKind kind, SettingsSerializer serializer = SettingsSerializer.ClassifyXml)
        {
            AppName = appName;
            Kind = kind;
            Serializer = serializer;
        }

        /// <summary>
        ///     The name of the settings file is formed from this <see cref="AppName"/> according to certain rules. This
        ///     should normally be a string equal to the name of the application. Paths and extensions should be omitted.</summary>
        public string AppName { get; private set; }

        /// <summary>Specifies what the settings in this settings class are logically "attached" to.</summary>
        public SettingsKind Kind { get; private set; }

        /// <summary>Specifies which serializer is used to read/write the settings file.</summary>
        public SettingsSerializer Serializer { get; private set; }

        /// <summary>
        ///     Returns the full path and file name that should be used to store the settings class marked with this
        ///     attribute. Note that the return value may change depending on external factors (see remarks). The recommended
        ///     approach is to load settings once, and then save them whenever necessary to whichever path is returned by this
        ///     function.</summary>
        /// <remarks>
        ///     This method checks if a file by the name <c>(AppName).IsPortable.txt</c> exists in the application path. If
        ///     that file exists, the settings are always stored in that application path. This check is skipped if there is
        ///     no entry assembly (<see cref="Assembly.GetEntryAssembly"/> is null).</remarks>
        public string GetFileName()
        {
            string filename = AppName;
            switch (Kind)
            {
                case SettingsKind.UserSpecific:
                    // AppName.settings.xml is the user-specific machine-independent settings file; also ensures backwards compatibility
                    break;
                case SettingsKind.UserAndMachineSpecific:
                    // AppName.SIRIUS.settings.xml is the user-specific machine-specific settings file
                    filename += "." + Environment.MachineName;
                    break;
                case SettingsKind.MachineSpecific:
                    // AppName.AllUsers.SIRIUS.settings.xml is a rare special case for a portable app
                    filename += ".AllUsers." + Environment.MachineName;
                    break;
                case SettingsKind.Global:
                    // AppName.Global.settings.xml - need a separate name to user-specific, so add the suffix.
                    filename += ".Global";
                    break;
                default:
                    throw new InternalErrorException("unreachable (97628)");
            }

            var fileExtension = typeof(SettingsSerializer).GetFields(BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(f => f.GetValue(null).Equals(Serializer))
                .NullOr(field => field.GetCustomAttributes<SerializerInfoAttribute>().FirstOrDefault().NullOr(inf => inf.DefaultFileExtension))
                ?? "bin";

            filename = filename.FilenameCharactersEscape() + ".Settings." + fileExtension;

            if (PathUtil.AppPath != null && File.Exists(PathUtil.AppPathCombine(AppName + ".IsPortable.txt")))
                return PathUtil.AppPathCombine(filename);

            switch (Kind)
            {
                case SettingsKind.Global:
                case SettingsKind.MachineSpecific:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName, filename);
                case SettingsKind.UserSpecific:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName, filename);
                case SettingsKind.UserAndMachineSpecific:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName, filename);
                default:
                    throw new InternalErrorException("unreachable (97629)");
            }
        }
    }
}
