namespace RT.Serialization.Settings;

/// <inheritdoc/>
public class SettingsFileJson<TSettings> : SettingsFile<TSettings> where TSettings : new()
{
    private ClassifyOptions _options;

    /// <inheritdoc/>
    public SettingsFileJson(string appName, SettingsLocation location = SettingsLocation.User, bool throwOnError = false, string suffix = ".Settings.json", ClassifyOptions options = null)
        : base(appName, location, throwOnError, suffix)
    {
        _options = options;
    }

    /// <inheritdoc/>
    public SettingsFileJson(bool throwOnError, string filename, ClassifyOptions options = null)
        : base(throwOnError, filename)
    {
        _options = options;
    }

    /// <inheritdoc/>
    protected override TSettings Deserialize(string filename)
    {
        return ClassifyJson.DeserializeFile<TSettings>(filename, _options);
    }

    /// <inheritdoc/>
    protected override void Serialize(string filename, TSettings settings)
    {
        ClassifyJson.SerializeToFile(settings, filename, _options);
    }
}
