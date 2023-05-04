namespace RT.Serialization.Settings;

/// <inheritdoc/>
public class SettingsFileXml<TSettings> : SettingsFile<TSettings> where TSettings : new()
{
    private ClassifyOptions _options;

    /// <inheritdoc/>
    public SettingsFileXml(string appName, SettingsLocation location = SettingsLocation.User, bool throwOnError = false, string suffix = ".Settings.xml", ClassifyOptions options = null)
        : base(appName, location, throwOnError, suffix)
    {
        _options = options;
    }

    /// <inheritdoc/>
    protected override TSettings Deserialize(string filename)
    {
        return ClassifyXml.DeserializeFile<TSettings>(filename, _options);
    }

    /// <inheritdoc/>
    protected override void Serialize(string filename, TSettings settings)
    {
        ClassifyXml.SerializeToFile(settings, filename, _options, format: ClassifyXmlFormat.Create("Settings"));
    }
}
