using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RT.Util.Serialization
{
    public interface IClassifyFormat<TElement>
    {
        TElement ReadFromStream(Stream stream);
        void WriteToStream(TElement element, Stream stream);

        bool IsNull(TElement element);
        object GetSimpleValue(TElement element);
        TElement GetSelfValue(TElement element);
        IEnumerable<TElement> GetList(TElement element, int? tupleSize);
        bool GetKeyValuePair(TElement element, out TElement key, out TElement value);
        IEnumerable<KeyValuePair<object, TElement>> GetDictionary(TElement element);
        bool HasField(TElement element, string fieldName);
        TElement GetField(TElement element, string fieldName);
        string GetType(TElement element);
        bool IsReference(TElement element);
        bool IsReferable(TElement element);
        bool IsFollowID(TElement element);
        string GetReferenceID(TElement element);
        bool IsEmpty(TElement element);

        TElement FormatNullValue(string name);
        TElement FormatSimpleValue(string name, object value);
        TElement FormatSelfValue(string name, TElement value);
        TElement FormatList(string name, bool isTuple, IEnumerable<TElement> values);
        TElement FormatKeyValuePair(string name, TElement key, TElement value);
        TElement FormatDictionary(string name, IEnumerable<KeyValuePair<object, TElement>> values);
        TElement FormatObject(string name, IEnumerable<KeyValuePair<string, TElement>> fields);
        TElement FormatFollowID(string name, string id);
        TElement FormatReference(string name, string refId);
        TElement FormatReferable(TElement element, string refId);
        TElement FormatWithType(TElement element, string type);
    }
}
