using RT.Internal;

// Possible future improvements:
// - monitor for changes / autoreload mode

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
    /// <summary>
    ///     Executable's directory. Forces portable mode without requiring a special marker file - however if present, the
    ///     marker file can still specify an alternative location.</summary>
    Portable,
}

/// <summary>
///     Loads and saves application settings with a number of convenience and reliability features. See Remarks.</summary>
/// <typeparam name="TSettings">
///     The type to be used to store application settings.</typeparam>
/// <remarks>
///     <para>
///         Compared to a simple approach of using a serializer directly to load and save from a hard-coded file path, this
///         class offers the following benefits for the application settings scenario:</para>
///     <list type="bullet">
///         <item>The settings file path is chosen automatically based on <see cref="SettingsLocation"/> and the
///         <c>appName</c>.</item>
///         <item>A portable mode marker file can be placed in the application directory to make the app portable.</item>
///         <item>File sharing violations are handled automatically; these are common with some backup software.</item>
///         <item>No need to special-case first run; a default instance is returned if the settings file does not exist.</item>
///         <item>A backup of the settings file is created if load fails and the application continues with default settings,
///         overwriting the faulty settings file.</item>
///         <item>When saving, the settings are first saved to a temporary file, which only overwrites the original settings
///         file after a successful serialization.</item>
///         <item>Unexpected I/O or parse errors can be toggled on/off, for applications wishing to trade between startup
///         reliability or preserving settings at the cost of a startup crash / additional error handling code.</item>
///         <item>Supports scheduling a background save on a background thread, with debouncing of frequent calls.</item></list>
///     <para>
///         <b>Typical usage</b></para>
///     <code>
///         private SettingsFile&lt;Settings&gt; AppSettingsFile;
///         ...
///         // Load / initialise settings
///         AppSettingsFile = new SettingsFileXml&lt;Settings&gt;("MyApp"); // defined in RT.Serialization.Xml
///         ...
///         // Use settings
///         Foobar = AppSettingsFile.Settings.Foobar;
///         AppSettingsFile.Settings.LastPos = MyWindow.Position;
///         ...
///         // Save settings
///         AppSettingsFile.Save();</code>
///     <para>
///         This minimal example will save the settings in User Profile under MyApp\MyApp.Settings.xml, and will support
///         portable mode automatically. It benefits from all the reliability features listed above. If the settings file is
///         corrupted this minimal example creates a backup of the corrupted file and resets the settings to defaults.</para>
///     <para>
///         <b>Settings-relative paths</b></para>
///     <para>
///         To construct paths relative to the settings location, use <see cref="PathCombine"/>. This ensures that such paths
///         remain valid if the user chooses to configure portable mode (see below).</para>
///     <para>
///         <b>Portable mode</b></para>
///     <para>
///         The goal of portable mode is to keep everything the application needs, including settings, in the application
///         directory. Conceptually this isn't actually a mode; it's simply a mechanism to select the appropriate path for the
///         settings file at settings load time (constructor call).</para>
///     <para>
///         When loading settings, this class will check whether a portable mode marker file with the name
///         <c>$"{appName}.IsPortable.txt"</c> is present in the application directory (but see <see
///         cref="GetPortableMarkerPath"/>). If present and empty, the settings file is stored in the application directory.
///         If not empty, the contents of the file specify the path to use. This path can be relative to the application
///         directory, or absolute, or use templates such as $(UserName), $(MachineName), $(UserDomainName), or $(member of
///         Environment.SpecialFolder).</para>
///     <para>
///         <b>Other notes</b></para>
///     <para>
///         The absolute path and filename of the settings file is fully determined at settings load time (constructor call)
///         and remains unchanged for the lifetime of the <see cref="SettingsFile{TSettings}"/> instance. It is available via
///         <see cref="FileName"/>.</para>
///     <para>
///         Where necessary, the application can check whether a new default settings instance was created by checking the
///         <see cref="DefaultsLoaded"/> property.</para></remarks>
public abstract class SettingsFile<TSettings> where TSettings : new()
{
    /// <summary>
    ///     Settings stored in this settings file. This property is automatically populated by the constructor, which loads
    ///     the settings or constructs a new instance where necessary. Users should update this object as appropriate and call
    ///     <see cref="Save(bool?)"/> to persist the settings.</summary>
    public TSettings Settings { get; set; }

    /// <summary>
    ///     Full path to the file from which the settings were loaded and to which they will be saved. This property is
    ///     populated on load even if the settings file did not exist as this is the path to which settings will be saved by
    ///     <see cref="Save(bool?)"/>.</summary>
    public string FileName { get; private set; }

    /// <summary>
    ///     <c>true</c> if a new default instance of the settings had to be constructed, otherwise <c>false</c>. When
    ///     <c>false</c> it's guaranteed that the settings were successfully loaded from the settings file. When <c>true</c>,
    ///     if <c>throwOnError</c> is true then it's guaranteed that the settings file was missing, but if <c>throwOnError</c>
    ///     is false then it's not possible to determine whether the file was present or a loading error has occurred.</summary>
    public bool DefaultsLoaded { get; private set; }

    /// <summary>
    ///     Deserializes settings from <paramref name="filename"/>. This method must not do any error handling of its own and
    ///     should propagate all exceptions, including File Not Found and any parse errors. This method must not return
    ///     default or null settings on failure; the <see cref="SettingsFile{TSettings}"/> class is responsible for that.</summary>
    /// <param name="filename">
    ///     Full path to the settings file.</param>
    /// <returns>
    ///     The deserialized instance.</returns>
    protected abstract TSettings Deserialize(string filename);

    /// <summary>
    ///     Serializes <paramref name="settings"/> to <paramref name="filename"/>. This method must not do any error handling
    ///     of its own and should propagate all exceptions, including File Not Found and any parse errors.</summary>
    /// <param name="filename">
    ///     Full path to the settings file.</param>
    /// <param name="settings">
    ///     Instance of settings to serialize.</param>
    protected abstract void Serialize(string filename, TSettings settings);

    /// <summary>
    ///     Specifies how long to keep retrying when settings cannot be saved due to a file sharing violation. Defaults to 5
    ///     seconds.</summary>
    public TimeSpan WaitSharingViolationOnSave { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Specifies how long to keep retrying when settings cannot be loaded due to a file sharing violation. Defaults to 5
    ///     seconds. See Remarks.</summary>
    /// <remarks>
    ///     This is a static property because loading is performed in the constructor. It's a bit of a last resort hack for
    ///     potential use cases where it's really not appropriate to wait the default 5 seconds - though no such use cases
    ///     have come up in practice yet.</remarks>
    public static TimeSpan WaitSharingViolationOnLoad { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The value of <c>throwOnError</c> passed into the constructor, which enables <see cref="Save(bool?)"/> to default
    ///     to the same value. It is private to ensure that descendants do not erroneously attempt to make use of this value.</summary>
    private bool _throwOnError;

    /// <summary>
    ///     Configures the location of the settings file and loads or initialises the settings. See Remarks on <see
    ///     cref="SettingsFile{TSettings}"/>.</summary>
    /// <param name="appName">
    ///     Application name; used as the settings folder name and to construct the settings file name and the portable marker
    ///     file name.</param>
    /// <param name="location">
    ///     Determines where to store the settings file.</param>
    /// <param name="throwOnError">
    ///     If false, any errors while loading the file are silently suppressed and a default instance is loaded instead. If
    ///     true, such errors are propagated to the caller as exceptions. This value is also used as the default value for
    ///     <c>throwOnError</c> when calling <see cref="Save(bool?)"/>.</param>
    /// <param name="suffix">
    ///     Appended to the settings file name; at a minimum this should include the extension dot and the file extension.</param>
    public SettingsFile(string appName, SettingsLocation location, bool throwOnError, string suffix)
    {
        string filename;
        var portablePath = getPortablePath(appName, throwOnError, suffix);
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
        filename = DetectLegacyFileNaming(filename, appName, suffix);
        FileName = filename;

        _throwOnError = throwOnError;
        Settings = load();
    }

    /// <summary>
    ///     Configures <paramref name="filename"/> as the location of the settings file, bypassing all logic related to
    ///     portable mode, and loads the settings. Avoid where possible and use the other constructor.</summary>
    /// <param name="filename">
    ///     Full path and filename of the settings file.</param>
    /// <param name="throwOnError">
    ///     If false, any errors while loading the file are silently suppressed and a default instance is loaded instead. If
    ///     true, such errors are propagated to the caller as exceptions. This value is also used as the default value for
    ///     <c>throwOnError</c> when calling <see cref="Save(bool?)"/>.</param>
    public SettingsFile(bool throwOnError, string filename)
    {
        FileName = PathUtil.AppPathCombine(filename);
        _throwOnError = throwOnError;
        Settings = load();
    }

    private TSettings load()
    {
        if (!File.Exists(FileName)) // can't throw
        {
            DefaultsLoaded = true;
            return new();
        }

        DefaultsLoaded = false;
        if (_throwOnError)
        {
            // we're not allowed to silently pretend the file does not exist in case of read or parse failure. If it can't be read or parsed propagate that exception to the caller
            return Ut.WaitSharingVio(() => Deserialize(FileName), maximum: WaitSharingViolationOnLoad);
        }
        else
        {
            // if we can't read or parse this file just reset the settings
            try
            {
                return Ut.WaitSharingVio(() => Deserialize(FileName), maximum: WaitSharingViolationOnLoad);
            }
            catch
            {
                // as the old settings are likely to be overwritten by the next save, try preserving a backup of it - but this can also fail, and such failures are ignored
                try { File.Copy(FileName, PathUtil.AppendBeforeExtension(FileName, ".LoadFailedBackup"), overwrite: true); }
                catch { }

                DefaultsLoaded = true;
                return new();
            }
        }
    }

    private void doSave(string filename, TSettings settings)
    {
        PathUtil.CreatePathToFile(filename);
        var tempName = filename + ".~tmp";
        Serialize(tempName, settings);
        File.Delete(filename);
        File.Move(tempName, filename);
    }

    /// <summary>
    ///     Saves the settings object <see cref="Settings"/> to the settings file.</summary>
    /// <param name="throwOnError">
    ///     Specifies whether a failure to save settings should be propagated as an exception or silently suppressed
    ///     (returning <c>false</c> from this method). If <c>null</c> (the default) then the same value is used as was passed
    ///     into the <see cref="SettingsFile{TSettings}"/> constructor.</param>
    /// <returns>
    ///     <c>true</c> if saved successfully. <c>false</c> if the save failed (for example, due to an I/O error). The latter
    ///     is only possible if errors are suppressed via <paramref name="throwOnError"/>.</returns>
    /// <remarks>
    ///     This method interacts with <see cref="SaveInBackground"/> by cancelling any pending background saves and making
    ///     sure that the current Settings object is the one that gets persisted last, overwriting any previous changes. Thus
    ///     it is safe to call this to reliably save settings on program exit regardless of any calls to <see
    ///     cref="SaveInBackground"/> with older versions of the <see cref="Settings"/> object.</remarks>
    public bool Save(bool? throwOnError = null)
    {
        lock (_backgroundLock)
        {
            _backgroundCancel?.Cancel();
            _backgroundCancel = null;
            // This guarantees that there is no pending save with a potentially older version of the settings object. It remains possible for a new
            // background save to be scheduled, but it would be saving a newer version of the settings object. This Save can be stuck on waiting
            // for a sharing violation long enough for the background save timeout to expire, but because we are holding the lock while we wait
            // the background save will execute after this save completes, preserving the expected order: first this Save with the older Settings,
            // then immediately the background save with the newer Settings.
            return Save(FileName, throwOnError);
        }
    }

    /// <summary>
    ///     Saves the settings object <see cref="Settings"/> to the settings file.</summary>
    /// <param name="filename">
    ///     Custom filename to save the settings to, ignoring the value of <see cref="FileName"/>.</param>
    /// <param name="throwOnError">
    ///     Specifies whether a failure to save settings should be propagated as an exception or silently suppressed
    ///     (returning <c>false</c> from this method). If <c>null</c> (the default) then the same value is used as was passed
    ///     into the <see cref="SettingsFile{TSettings}"/> constructor.</param>
    /// <returns>
    ///     <c>true</c> if saved successfully. <c>false</c> if the save failed (for example, due to an I/O error). The latter
    ///     is only possible if errors are suppressed via <paramref name="throwOnError"/>.</returns>
    /// <remarks>
    ///     This method completely ignores the existence of background saves initiated by <see cref="SaveInBackground"/>.
    ///     Saving to the same filename as <see cref="FileName"/> will result in undefined behaviour.</remarks>
    public bool Save(string filename, bool? throwOnError = null)
    {
        bool canThrow = throwOnError ?? _throwOnError;

        if (canThrow)
        {
            Ut.WaitSharingVio(() => doSave(filename, Settings), maximum: WaitSharingViolationOnSave);
            return true;
        }
        else
        {
            try
            {
                Ut.WaitSharingVio(() => doSave(filename, Settings), maximum: WaitSharingViolationOnSave);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private object _backgroundLock = new();
    private CancellationTokenSource _backgroundCancel = null; // not null when we have a pending background save task; null when it's cancelled or completed
    private TSettings _backgroundSettings = default;

    /// <summary>
    ///     Determines the maximum frequency at which calls to <see cref="SaveInBackground"/> actually perform the
    ///     (potentially expensive) save. The first background save is delayed by the amount specified here; subsequent
    ///     background saves only update the object to be saved but do not postpone the save.</summary>
    public TimeSpan BackgroundSaveDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Take this lock before making changes to the <see cref="Settings"/> object if you use <see
    ///     cref="SaveInBackground"/>. See Remarks on <see cref="SaveInBackground"/> for specifics.</summary>
    public object BackgroundLock => _backgroundLock;

    /// <summary>
    ///     Saves settings on a background thread after a short delay. See Remarks.</summary>
    /// <remarks>
    ///     <para>
    ///         This method can be called as often as necessary. It is cheap to call frequently, and will only save at most
    ///         once every <see cref="BackgroundSaveDelay"/> seconds.</para>
    ///     <para>
    ///         This method may be safely interleaved with calls to <see cref="Save(bool?)"/>. The saves are ordered as
    ///         called; it is not possible for an older background save to overwrite a newer save from either method. The <see
    ///         cref="Save(bool?)"/> method attempts to cancel any pending background saves. Note that <see cref="Save(string,
    ///         bool?)"/> is completely exempt from this logic; it does not co-operate with background saving in any way and
    ///         just writes out the settings to the specified file.</para>
    ///     <para>
    ///         Save errors are ignored; it is not possible to detect a failed background save. However, on file sharing
    ///         violation a new background save is scheduled automatically.</para>
    ///     <para>
    ///         If the process exits with a background save pending, the background save is lost. Make sure to call <see
    ///         cref="Save(bool?)"/> prior to exiting the process.</para>
    ///     <para>
    ///         If <typeparamref name="TSettings"/> implements <see cref="ICloneable"/> the object is cloned on every call and
    ///         it is the clone that is scheduled for saving. Otherwise the <see cref="Settings"/> object is saved as-is,
    ///         meaning that any changes made after the call to <see cref="SaveInBackground"/> may end up getting saved.</para>
    ///     <para>
    ///         You must consider thread safety when modifying the <see cref="Settings"/> object if you use this method. The
    ///         background save thread expects to be able to read it consistently: in the <see cref="ICloneable"/> case it's
    ///         at the time of this call on the calling thread; otherwise it's at an unspecified time on a thread pool thread.
    ///         In particular, basic .NET collections such as Dictionary will break if read and modified simultaneously from
    ///         multiple threads. You should either implement <see cref="ICloneable"/> and synchronize the call to this method
    ///         with all changes to <see cref="Settings"/> yourself, or you must wrap all changes to <see cref="Settings"/> in
    ///         a lock of <see cref="BackgroundLock"/>. Alternatively you can make sure that all updates to <see
    ///         cref="Settings"/> are atomic (which non-concurrent .NET collections are not!)</para></remarks>
    public void SaveInBackground()
    {
        if (Settings is ICloneable cloneable)
            _backgroundSettings = (TSettings) cloneable.Clone();
        else
            _backgroundSettings = Settings;

        scheduleBackgroundSave();
    }

    private void scheduleBackgroundSave()
    {
        lock (_backgroundLock)
        {
            if (_backgroundCancel != null)
                return; // we already have a pending save and don't need to do anything

            _backgroundCancel = new();
            var token = _backgroundCancel.Token;

            Task.Delay(BackgroundSaveDelay, token).ContinueWith(_ =>
            {
                lock (_backgroundLock)
                {
                    if (token.IsCancellationRequested) // while _backgroundCancel itself shouldn't be null here (it's set to null inside the lock by Save), to make extra sure we use the locally captured token instead
                        return;
                    _backgroundCancel = null;
                    try
                    {
                        doSave(FileName, _backgroundSettings);
                    }
                    catch (IOException ex) when (ex.HResult == -2147024864) // 0x80070020 ERROR_SHARING_VIOLATION
                    {
                        scheduleBackgroundSave(); // on sharing violation schedule another background save as a retry
                    }
                    catch
                    {
                        // all other problems with saving are ignored by background saves
                    }
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }

    /// <summary>
    ///     Combines the directory containing this settings file with the specified path segments. Use this method to
    ///     reference additional files relative to the settings directory with correct support for portable mode marker file.</summary>
    /// <param name="paths">
    ///     Path segments.</param>
    public string PathCombine(params string[] paths)
    {
        return Path.Combine(new[] { Path.GetDirectoryName(FileName) }.Concat(paths).ToArray());
    }

    /// <summary>
    ///     Gets the full path to where the portable marker file is expected to be found. The default implementation returns
    ///     the path for <c>$"{appName}.IsPortable.txt"</c> in the application directory.</summary>
    protected virtual string GetPortableMarkerPath(string appName)
    {
        return PathUtil.AppPathCombine($"{appName}.IsPortable.txt");
    }

    /// <summary>
    ///     Determines whether the user has configured portable mode. Returns null if not, or the full absolute path to the
    ///     settings file as overridden by the portable mode marker file. See "Portable mode" in "Remarks" for <see
    ///     cref="SettingsFile{TSettings}"/>.</summary>
    private string getPortablePath(string appName, bool throwOnError, string suffix)
    {
        var portablePath = GetPortableMarkerPath(appName);
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

    /// <summary>
    ///     Detects legacy settings filenames produced by <c>RT.Util.SettingsUtil</c>. If the file at <paramref
    ///     name="filename"/> does not exist, and there is a matching legacy-named file at the same path, returns the full
    ///     path to that legacy file. Otherwise returns <paramref name="filename"/> unchanged.</summary>
    /// <param name="filename">
    ///     Non-legacy full path and filename for this settings file.</param>
    /// <param name="appName">
    ///     Application name as passed into the constructor.</param>
    /// <param name="suffix">
    ///     Filename suffix as passed into the constructor.</param>
    protected virtual string DetectLegacyFileNaming(string filename, string appName, string suffix)
    {
        if (File.Exists(filename))
            return filename;
        var dir = Path.GetDirectoryName(filename);
        string legacyName;
        if (File.Exists(legacyName = Path.Combine(dir, $"{appName}.Global{suffix}")))
            return legacyName;
        if (File.Exists(legacyName = Path.Combine(dir, $"{appName}.{Environment.MachineName}{suffix}")))
            return legacyName;
        if (File.Exists(legacyName = Path.Combine(dir, $"{appName}.AllUsers.{Environment.MachineName}{suffix}")))
            return legacyName;
        return filename;
    }
}
