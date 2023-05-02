using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Util;

// TODO: to fully supersede RT.Util.SettingsUtil, add SaveSettingsInBackground

namespace RT.Serialization.Settings;

/// <summary>Enumerates possible storage locations for the settings file.</summary>
public enum SettingsLocation
{
    /// <summary>User's roaming profile. Most application settings should go here.</summary>
    User = 1,
    /// <summary>User's local profile.</summary>
    UserLocal,
    /// <summary>Settings specific to a machine and shared by all users, locally.</summary>
    MachineLocal,
    /// <summary>Executable's directory. Forces portable mode without requiring a special marker file.</summary>
    Portable,
}

/// <summary>
///     Loads and saves application settings with a number of convenience and reliability features. See Remarks.</summary>
/// <remarks>
///     <para>
///         Compared to a simple approach of using a serializer directly to load and save from a hard-coded file path, this
///         class offers the following benefits for the application settings scenario:</para>
///     <list type="bullet">
///         <item>The settings file path is chosen automatically based on <see cref="SettingsLocation"/> and the
///         <c>appName</c>.</item>
///         <item>A portable mode marker file can be placed in the application directory to make the app portable.</item>
///         <item>File sharing violations are handled automatically; these are common with some backup software.</item>
///         <item>No need to special case first run; a default instance is returned if the settings file does not exist.</item>
///         <item>A backup of the settings file is created if load fails and the application continues with default settings,
///         overwriting the faulty settings file (configurable).</item>
///         <item>When saving, the settings are first saved to a temporary file, which only overwrites the original settings
///         file after a successful serialization.</item>
///         <item>Unexpected I/O or parse errors can be toggled on/off, for applications wishing to trade between startup
///         reliability or preserving settings at the cost of a startup crash.</item></list>
///     <para>
///         <b>Typical usage</b></para>
///     <code>
///         private Settings AppSettings;
///         ...
///         SettingsUtil.Manager.ConfigureXml&lt;Settings&gt;("MyApp"); // extension method in RT.Serialization.Xml
///         SettingsUtil.Manager.LoadSettings(out AppSettings);
///         ...
///         SettingsUtil.Manager.SaveSettings(AppSettings);</code>
///     <para>
///         This is the minimal example targeting a very simple application. Alternatively the <see cref="SettingsManager"/>
///         can be used directly as a singleton through dependency injection.</para>
///     <para>
///         <b>Portable mode</b></para>
///         <para>The goal of portable mode is to keep everything the application needs, including settings, in the application directory. Conceptually this isn't actually a mode; it's simply a mechanism to select a different path for the settings file during a call to Configure. TODO</para>
///     <para>
///         <b>Error handling</b></para>
///         <para>TODO</para>
///         <para><b>Other remarks</b></para><para>While the API of this class naturally supports the use of multiple different settings types simultaneously, the intended usage scenario is to configure and use a single settings type.</para></remarks>
public class SettingsManager
{
    private class Info
    {
        public string FileName; // always a full absolute path
        public bool ThrowOnError;
        public Func<string, object> Deserialize;
        public Action<string, object> Serialize;
    }

    private Dictionary<Type, Info> _info = new();

    /// <summary>
    ///     Specifies how long to keep retrying when settings cannot be loaded due to a file sharing violation. Defaults to 5
    ///     seconds.</summary>
    public TimeSpan WaitSharingViolationOnLoad { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Specifies how long to keep retrying when settings cannot be saved due to a file sharing violation. Defaults to 5
    ///     seconds.</summary>
    public TimeSpan WaitSharingViolationOnSave { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Specifies whether to create a backup copy of the settings file when it cannot be loaded and a default instance of
    ///     settings is "loaded" instead.</summary>
    public bool UseLoadFailedBackup { get; set; } = true;

    /// <summary>
    ///     Configures a type for loading and saving application settings. Consider using an extension method such as
    ///     <c>ConfigureXml</c> or <c>ConfigureJson</c> instead - defined in the format-specific RT.Serialization packages.</summary>
    /// <typeparam name="TSettings">
    ///     The type to be used to store application settings.</typeparam>
    /// <param name="appName">
    ///     Application name; used as the settings folder name and to construct the settings file name and the portable marker
    ///     file name.</param>
    /// <param name="location">
    ///     Determines where to store the settings file.</param>
    /// <param name="suffix">
    ///     Appended to the settings file name; at a minimum this should include the extension dot and the file extension.</param>
    /// <param name="throwOnError">
    ///     If true, any errors while loading the file are silently suppressed and a default instance is loaded instead. If
    ///     false, such errors are propagated to the caller as exceptions. See "Error handling" in "Remarks" for <see
    ///     cref="SettingsManager"/>.</param>
    /// <param name="load">
    ///     A function that accepts a filename and returns a loaded instance. This function may assume that the file exists
    ///     and that the filename is always a full path. This function is not expected to do any error handling and is
    ///     expected to throw if it cannot read or parse the file.</param>
    /// <param name="save">
    ///     An action that accepts a filename and a settings instance to save. This function may assume that the parent
    ///     directory exists. The file must be overwritten if it already exists. The path is a full absolute path. This
    ///     function is not expected to do any error handling and is expected to throw if it cannot write the file.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if this type is already configured.</exception>
    public void Configure<TSettings>(string appName, SettingsLocation location, string suffix, bool throwOnError, Func<string, TSettings> load, Action<string, TSettings> save)
    {
        if (_info.ContainsKey(typeof(TSettings)))
            throw new InvalidOperationException($"This type is already configured for use as a settings type: {typeof(TSettings).FullName}");
        var info = new Info();
        info.ThrowOnError = throwOnError;

        string filename;
        var portablePath = GetPortablePath(appName, suffix, throwOnError);
        if (portablePath != null)
            filename = portablePath;
        else if (location == SettingsLocation.Portable)
            filename = PathUtil.AppPathCombine(appName + suffix);
        else if (location == SettingsLocation.User)
            filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName, appName + suffix);
        else if (location == SettingsLocation.UserLocal)
            filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName, appName + suffix);
        else if (location == SettingsLocation.MachineLocal)
            filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), appName, appName + suffix);
        else
            throw new Exception();
        info.FileName = filename;

        info.Deserialize = filename => load(filename);
        info.Serialize = (filename, settings) => save(filename, (TSettings) settings);

        _info[typeof(TSettings)] = info;
    }

    private Info getInfo<TSettings>()
    {
        if (_info.TryGetValue(typeof(TSettings), out var info))
            return info;
        throw new InvalidOperationException($"You must call {nameof(SettingsManager)}.{nameof(Configure)} on settings type {typeof(TSettings).FullName} before loading or saving settings.");
    }

    /// <summary>
    ///     Loads settings into <typeparamref name="TSettings"/> as previously configured via <see cref="Configure"/>, or
    ///     constructs a new instance using the default constructor.</summary>
    /// <typeparam name="TSettings">
    ///     Settings type.</typeparam>
    /// <param name="settings">
    ///     Loaded settings.</param>
    /// <returns>
    ///     <c>true</c> if the settings were loaded from file, or <c>false</c> if a default instance was constructed. If
    ///     errors are suppressed (see <see cref="Configure"/>) there is no way to determine whether <c>false</c> is due to an
    ///     error or simply because there was no settings file to load. If not suppressed, <c>false</c> can only be returned
    ///     if the settings file does not exist.</returns>
    public bool LoadSettings<TSettings>(out TSettings settings) where TSettings : new()
    {
        var info = getInfo<TSettings>();

        if (!File.Exists(info.FileName)) // can't throw
        {
            settings = new();
            return false;
        }

        object result;
        if (info.ThrowOnError)
        {
            // we're not allowed to silently pretend the file does not exist in case of read or parse failure. If it can't be read or parsed propagate that exception to the caller
            result = Ut.WaitSharingVio(() => info.Deserialize(info.FileName), maximum: WaitSharingViolationOnLoad);
        }
        else
        {
            // if we can't read or parse this file just reset the settings
            try
            {
                result = Ut.WaitSharingVio(() => info.Deserialize(info.FileName), maximum: WaitSharingViolationOnLoad);
            }
            catch
            {
                // as the old settings are likely to be overwritten by the next save, try preserving a backup of it - but this can also fail, and such failures are ignored
                if (UseLoadFailedBackup)
                    try { File.Copy(info.FileName, PathUtil.AppendBeforeExtension(info.FileName, ".LoadFailedBackup"), overwrite: true); }
                    catch { }

                settings = new();
                return false;
            }
        }

        settings = (TSettings) result; // Configure<> only accepts functions that return TSettings so this cast should be safe
        return true;
    }

    private void doSave(Info info, object settings)
    {
        PathUtil.CreatePathToFile(info.FileName);
        var tempName = info.FileName + ".~tmp";
        info.Serialize(tempName, settings);
        File.Delete(info.FileName);
        File.Move(tempName, info.FileName);
    }

    /// <summary>
    ///     Saves settings as previously configured via <see cref="Configure"/>.</summary>
    /// <typeparam name="TSettings">
    ///     Settings type.</typeparam>
    /// <param name="settings">
    ///     Settings to save.</param>
    /// <param name="throwOnError">
    ///     Optionally overrides whether a failure to save settings should be suppressed or propagated. If <c>null</c> then
    ///     the value specified in <see cref="Configure"/> is used. See "Error handling" in "Remarks" for <see
    ///     cref="SettingsManager"/>.</param>
    /// <returns>
    ///     <c>true</c> if saved successfully. <c>false</c> if save failed (for example, due to an I/O error). The latter is
    ///     only possible if errors are suppressed.</returns>
    public bool SaveSettings<TSettings>(TSettings settings, bool? throwOnError = null)
    {
        var info = getInfo<TSettings>();
        bool canThrow = throwOnError ?? info.ThrowOnError;

        if (canThrow)
        {
            Ut.WaitSharingVio(() => doSave(info, settings), maximum: WaitSharingViolationOnSave);
            return true;
        }
        else
        {
            try
            {
                Ut.WaitSharingVio(() => doSave(info, settings), maximum: WaitSharingViolationOnSave);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Combines the directory containing the settings file for <typeparamref name="TSettings"/> with the specified path segments. Use this to reference additional files relative to the settings directory while supporting portable mode marker file.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     Settings type.</typeparam>
    /// <param name="paths">Path segments.</param>
    public string PathCombine<TSettings>(params string[] paths)
    {
        var info = getInfo<TSettings>();
        return Path.Combine(new[] { Path.GetDirectoryName(info.FileName) }.Concat(paths).ToArray());
    }

    /// <summary>
    ///     Determines whether the user has configured a portable mode override. Returns null if not, or the full absolute
    ///     path to the settings file as overridden by the portable mode. The default implementation checks for the existence
    ///     of a file named <c>{appName}.IsPortable.txt</c> in the application directory, and optionally reads a template
    ///     filename from this file. See "Portable mode" in "Remarks" for <see cref="SettingsManager"/>.</summary>
    /// <param name="appName">
    ///     The <c>appName</c> parameter that was passed to <see cref="Configure"/>.</param>
    /// <param name="suffix">
    ///     The <c>suffix</c> parameter that was passed to <see cref="Configure"/>.</param>
    /// <param name="throwOnError">
    ///     The <c>throwOnError</c> parameter that was passed to <see cref="Configure"/>. If overridden, this method must not
    ///     throw when this parameter is <c>false</c>, and must return null instead.</param>
    protected virtual string GetPortablePath(string appName, string suffix, bool throwOnError)
    {
        var portablePath = Path.Combine(AppContext.BaseDirectory, $"{appName}.IsPortable.txt");
        if (!File.Exists(portablePath)) // can't throw
            return null;

        string portableTemplate;
        if (throwOnError)
        {
            // we're not allowed to silently pretend the file does not exist in case of read failure; IO exceptions here get propagated to the caller
            portableTemplate = Ut.WaitSharingVio(() => File.ReadAllText(portablePath), maximum: TimeSpan.FromSeconds(5));
        }
        else
        {
            try { portableTemplate = Ut.WaitSharingVio(() => File.ReadAllText(portablePath), maximum: TimeSpan.FromSeconds(5)); }
            catch { return null; }
        }
        portableTemplate = portableTemplate.Trim();
        if (portableTemplate.Length == 0)
            portableTemplate = appName + suffix;
        return PathUtil.AppPathCombine(PathUtil.ExpandPath(portableTemplate));
    }
}

/// <summary>
///     Exposes a singleton instance of <see cref="SettingsManager"/> via the static property <see cref="Manager"/> for
///     convenience.</summary>
public static class SettingsUtil
{
    private static SettingsManager _manager;

    /// <summary>
    ///     Loads and saves application settings with a number of convenience and reliability features. See <see
    ///     cref="SettingsManager"/> for details. Initialised on first use.</summary>
    public static SettingsManager Manager
    {
        get
        {
            if (_manager == null)
                _manager = new();
            return _manager;
        }
    }
}
