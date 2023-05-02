namespace RT.Serialization.Settings;

public static class SettingsExtensions
{
    public static void ConfigureXml<TSettings>(this SettingsManager manager, string appName, SettingsLocation kind = SettingsLocation.User, string suffix = ".Settings.xml", bool throwOnError = false, ClassifyOptions options = null)
    {
        manager.Configure(appName, kind, suffix, throwOnError,
            load: (string filename) => ClassifyXml.DeserializeFile<TSettings>(filename, options),
            save: (string filename, TSettings settings) => ClassifyXml.SerializeToFile(settings, filename, options));
    }
}
