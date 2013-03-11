using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RT.Util.Json;

namespace RT.Util.Serialization
{
    public static partial class ClassifyFormats
    {
        private static IClassifyFormat<XElement> _xml = new xmlClassifyFormat();
        public static IClassifyFormat<XElement> Xml { get { return _xml; } }

        private static IClassifyFormat<JsonValue> _json = new jsonClassifyFormat();
        public static IClassifyFormat<JsonValue> Json { get { return _json; } }
    }
}
