using RT.Util.Json;

namespace RT.Util.Serialization
{
    /// <summary>Provides some predefined implementations of <see cref="IClassifyFormat{TElement}"/> for use in <see cref="Classify"/>.</summary>
    public static partial class ClassifyFormats
    {
        /// <summary>Provides a format to serialize/deserialize objects as XML.</summary>
        public static XmlClassifyFormat Xml { get { return _xml; } }
        private static XmlClassifyFormat _xml = new XmlClassifyFormat();

        /// <summary>Provides a format to serialize/deserialize objects as JSON.</summary>
        public static IClassifyFormat<JsonValue> Json { get { return _json; } }
        private static IClassifyFormat<JsonValue> _json = new jsonClassifyFormat();
    }
}
