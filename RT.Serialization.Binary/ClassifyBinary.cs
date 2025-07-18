using System.Numerics;
using RT.Internal;

namespace RT.Serialization;

/// <summary>Offers a convenient way to use <see cref="Classify"/> to serialize objects using a compact binary format.</summary>
public static class ClassifyBinary
{
    private static readonly IClassifyFormat<node> _defaultFormat = classifyBinaryFormat.Default;

    /// <summary>
    ///     Reconstructs an object of the specified type from the specified file.</summary>
    /// <typeparam name="T">
    ///     Type of object to read.</typeparam>
    /// <param name="options">
    ///     Options.</param>
    /// <param name="filename">
    ///     Path and filename of the file to read from.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static T DeserializeFile<T>(string filename, ClassifyOptions options = null) => Classify.DeserializeFile<node, T>(filename, _defaultFormat, options);

    /// <summary>
    ///     Reconstructs an object of the specified type from the specified file.</summary>
    /// <param name="type">
    ///     Type of object to read.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <param name="filename">
    ///     Path and filename of the file to read from.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static object DeserializeFile(Type type, string filename, ClassifyOptions options = null) => Classify.DeserializeFile(type, filename, _defaultFormat, options);

    /// <summary>
    ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
    /// <typeparam name="T">
    ///     Type of object to reconstruct.</typeparam>
    /// <param name="binaryData">
    ///     Serialized form to reconstruct object from.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static T Deserialize<T>(byte[] binaryData, ClassifyOptions options = null)
    {
        using var mem = new MemoryStream(binaryData);
        return Classify.Deserialize<node, T>(_defaultFormat.ReadFromStream(mem), _defaultFormat, options);
    }

    /// <summary>
    ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
    /// <param name="type">
    ///     Type of object to reconstruct.</param>
    /// <param name="binaryData">
    ///     Serialized form to reconstruct object from.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static object Deserialize(Type type, byte[] binaryData, ClassifyOptions options = null)
    {
        using var mem = new MemoryStream(binaryData);
        return Classify.Deserialize(type, _defaultFormat.ReadFromStream(mem), _defaultFormat, options);
    }

    /// <summary>
    ///     Reconstructs an object of the specified type from the specified serialized form by applying the values to an
    ///     existing instance of the type.</summary>
    /// <typeparam name="T">
    ///     Type of object to reconstruct.</typeparam>
    /// <param name="binaryData">
    ///     Serialized form to reconstruct object from.</param>
    /// <param name="intoObject">
    ///     Object to assign values to in order to reconstruct the original object.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void DeserializeIntoObject<T>(byte[] binaryData, T intoObject, ClassifyOptions options = null)
    {
        using var mem = new MemoryStream(binaryData);
        Classify.DeserializeIntoObject(_defaultFormat.ReadFromStream(mem), intoObject, _defaultFormat, options);
    }

    /// <summary>
    ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired type.
    ///     The type of object is inferred from the object passed in.</summary>
    /// <param name="filename">
    ///     Path and filename of the file to read from.</param>
    /// <param name="intoObject">
    ///     Object to assign values to in order to reconstruct the original object. Also determines the type of object
    ///     expected.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void DeserializeFileIntoObject(string filename, object intoObject, ClassifyOptions options = null) => Classify.DeserializeFileIntoObject(filename, intoObject, _defaultFormat, options);

    /// <summary>
    ///     Stores the specified object in a file with the given path and filename.</summary>
    /// <typeparam name="T">
    ///     Type of the object to store.</typeparam>
    /// <param name="saveObject">
    ///     Object to store in a file.</param>
    /// <param name="filename">
    ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void SerializeToFile<T>(T saveObject, string filename, ClassifyOptions options = null) => Classify.SerializeToFile(saveObject, filename, _defaultFormat, options);

    /// <summary>
    ///     Stores the specified object in a file with the given path and filename.</summary>
    /// <param name="saveType">
    ///     Type of the object to store.</param>
    /// <param name="saveObject">
    ///     Object to store in a file.</param>
    /// <param name="filename">
    ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void SerializeToFile(Type saveType, object saveObject, string filename, ClassifyOptions options = null) => Classify.SerializeToFile(saveType, saveObject, filename, _defaultFormat, options);

    /// <summary>
    ///     Converts the specified object into a serialized form.</summary>
    /// <typeparam name="T">
    ///     Type of object to convert.</typeparam>
    /// <param name="saveObject">
    ///     Object to be serialized.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     The serialized form generated from the object.</returns>
    public static byte[] Serialize<T>(T saveObject, ClassifyOptions options = null)
    {
        var node = Classify.Serialize(saveObject, _defaultFormat, options);
        using var mem = new MemoryStream();
        node.WriteToStream(mem);
        return mem.ToArray();
    }

    /// <summary>
    ///     Converts the specified object into a serialized form.</summary>
    /// <param name="saveType">
    ///     Type of object to convert.</param>
    /// <param name="saveObject">
    ///     Object to be serialized.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     The serialized form generated from the object.</returns>
    public static byte[] Serialize(Type saveType, object saveObject, ClassifyOptions options = null)
    {
        var node = Classify.Serialize(saveType, saveObject, _defaultFormat, options);
        using var mem = new MemoryStream();
        node.WriteToStream(mem);
        return mem.ToArray();
    }

    [Flags]
    private enum dataType : byte
    {
        End = 0x00,
        Null = 0x01,

        // Simple types
        False = 0x02,
        True = 0x03,
        Byte = 0x04,
        SByte = 0x05,
        Int16 = 0x06,       // also used for chars
        UInt16 = 0x07,    // also used for chars
        Int32 = 0x08,
        UInt32 = 0x09,
        Int64 = 0x0a,
        UInt64 = 0x0b,
        Single = 0x0c,
        Double = 0x0d,
        DateTime = 0x0e,
        Decimal = 0x0f,

        // Complex types (part 1)
        RawDataWithRefId = 0x10,    // see 0x13
        KeyValuePair = 0x11,
        String = 0x12,
        RawData = 0x13,                      // also used for strings in UTF-16 format

        // Dictionaries (the types are the type of the key)
        DictionaryInt64 = 0x14,                                 // key stored as Int64Optim
        DictionaryInt64WithRefId = 0x15,
        DictionaryString = 0x16,                                 // key stored as UTF-8, terminated by 0xff
        DictionaryStringWithRefId = 0x17,
        DictionaryOther = 0x18,                                 // type of key encoded in another DataType byte following the DataType byte with this value
        DictionaryOtherWithRefId = 0x19,
        DictionaryTwoStrings = 0x1a,                        // key stored as two UTF-8 strings, each terminated by 0xff
        DictionaryTwoStringsWithRefId = 0x1b,

        // Complex types (part 2)
        List = 0x1c,
        ListWithRefId = 0x1d,
        Ref = 0x1e,
        BigInteger = 0x1f,

        Mask = 0x1f,

        NoTypeSpec = 0x00,
        SimpleTypeSpec = 0x40,
        FullTypeSpec = 0x80,

        TypeSpecMask = 0xc0
    }

    // These must correspond index-by-index with those in _withRefs
    private static readonly dataType[] _reffables = [
        dataType.DictionaryInt64,
        dataType.DictionaryString,
        dataType.DictionaryOther,
        dataType.DictionaryTwoStrings,
        dataType.List,
        dataType.RawData
    ];

    // These must correspond index-by-index with those in _reffables
    private static readonly dataType[] _withRefs = [
        dataType.DictionaryInt64WithRefId,
        dataType.DictionaryStringWithRefId,
        dataType.DictionaryOtherWithRefId,
        dataType.DictionaryTwoStringsWithRefId,
        dataType.ListWithRefId,
        dataType.RawDataWithRefId
    ];

    private abstract class node
    {
        public int? RefId;
        public dataType DataType;
        public string TypeSpec;
        public bool TypeSpecIsFull;

        public void WriteToStream(Stream stream)
        {
            var dt = DataType;
            var typeSpec =
                TypeSpec == null ? dataType.NoTypeSpec :
                TypeSpecIsFull ? dataType.FullTypeSpec : dataType.SimpleTypeSpec;

            if (RefId != null)
            {
                var ix = _reffables.IndexOf(dt);
                Ut.Assert(ix >= 0);
                dt = _withRefs[ix];
            }

            stream.WriteByte((byte) (dt | typeSpec));

            writeToStreamImpl(stream);

            if (typeSpec is dataType.SimpleTypeSpec or dataType.FullTypeSpec)
                WriteBuffer(stream, TypeSpec.ToUtf8(), true);

            if (RefId != null)
                stream.WriteInt32Optim(RefId.Value);
        }
        protected abstract void writeToStreamImpl(Stream stream);

        public void WriteBuffer(Stream stream, byte[] buffer, bool isUtf8String)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                stream.WriteByte(buffer[i]);
                if (buffer[i] == 0xff)
                    stream.WriteByte(0x01);
            }
            stream.WriteByte(0xff);
            if (!isUtf8String)
                stream.WriteByte(0x00);
        }
    }

    private sealed class valueNode : node
    {
        public object Value;
        protected override void writeToStreamImpl(Stream stream)
        {
            switch (DataType)
            {
                case dataType.Byte: stream.WriteByte((byte) Value); break;
                case dataType.SByte: stream.WriteByte((byte) (sbyte) Value); break;
                case dataType.Int16: stream.Write(BitConverter.GetBytes((short) Value)); break;
                case dataType.UInt16: stream.Write(BitConverter.GetBytes((ushort) Value)); break;
                case dataType.Int32: stream.Write(BitConverter.GetBytes((int) Value)); break;
                case dataType.UInt32: stream.Write(BitConverter.GetBytes((uint) Value)); break;
                case dataType.Single: stream.Write(BitConverter.GetBytes((float) Value)); break;
                case dataType.Int64: stream.Write(BitConverter.GetBytes((long) Value)); break;
                case dataType.UInt64: stream.Write(BitConverter.GetBytes((ulong) Value)); break;
                case dataType.BigInteger: writeBigInteger(stream, (BigInteger) Value); break;
                case dataType.Double: stream.Write(BitConverter.GetBytes((double) Value)); break;
                case dataType.DateTime: stream.Write(BitConverter.GetBytes(((DateTime) Value).ToBinary())); break;
                case dataType.Decimal: stream.WriteDecimalOptim((decimal) Value); break;
                case dataType.String: WriteBuffer(stream, ((string) Value).ToUtf8(), true); break;
                case dataType.RawData: WriteBuffer(stream, (byte[]) Value, false); break;
                case dataType.Null: break;
                case dataType.False: break;
                case dataType.True: break;
                case dataType.Ref: stream.WriteInt32Optim((int) Value); break;
            }
        }

        private void writeBigInteger(Stream stream, BigInteger value)
        {
            var encode = value < 0 ? (~value) : value;
            while (encode > 0)
            {
                stream.Write(BitConverter.GetBytes((ulong) (encode % (ulong.MaxValue - 1))));
                encode /= ulong.MaxValue - 1;
            }
            stream.Write(BitConverter.GetBytes(value < 0 ? ulong.MaxValue - 1 : ulong.MaxValue));
        }
    }

    private sealed class kvpNode : node
    {
        public node Key;
        public node Value;
        protected override void writeToStreamImpl(Stream stream)
        {
            Key.WriteToStream(stream);
            Value.WriteToStream(stream);
        }
    }

    private sealed class listNode : node
    {
        public List<node> List;
        protected override void writeToStreamImpl(Stream stream)
        {
            foreach (var node in List)
                node.WriteToStream(stream);
            stream.WriteByte((byte) dataType.End);
        }
    }

    private sealed class fieldNameWithType(string fieldName, string declaringType) : IEquatable<fieldNameWithType>
    {
        public string FieldName { get; private set; } = fieldName;
        public string DeclaringType { get; private set; } = declaringType;
        public bool Equals(fieldNameWithType other) => other != null && other.FieldName == FieldName && other.DeclaringType == DeclaringType;
        public override bool Equals(object obj) => Equals(obj as fieldNameWithType);
        public override int GetHashCode() => FieldName.GetHashCode() * 37 + DeclaringType.GetHashCode();
    }

    private sealed class dictNode : node
    {
        public Dictionary<object, node> Dictionary;
        public dataType KeyType;

        protected override void writeToStreamImpl(Stream stream)
        {
            if (DataType == dataType.DictionaryOther)
                stream.WriteByte((byte) KeyType);

            foreach (var kvp in Dictionary)
            {
                // Store value first
                kvp.Value.WriteToStream(stream);

                // Then store key
                switch (DataType)
                {
                    case dataType.DictionaryInt64:
                        stream.WriteInt64Optim(ExactConvert.ToLong(kvp.Key));
                        break;

                    case dataType.DictionaryString:
                        WriteBuffer(stream, ExactConvert.ToString(kvp.Key).ToUtf8(), true);
                        break;

                    case dataType.DictionaryTwoStrings:
                        if (kvp.Key is string key)
                        {
                            WriteBuffer(stream, key.ToUtf8(), true);
                            WriteBuffer(stream, [], true);
                        }
                        else
                        {
                            var fn = (fieldNameWithType) kvp.Key;
                            WriteBuffer(stream, fn.FieldName.ToUtf8(), true);
                            WriteBuffer(stream, fn.DeclaringType == null ? [] : fn.DeclaringType.ToUtf8(), true);
                        }
                        break;

                    case dataType.DictionaryOther:
                        switch (KeyType)
                        {
                            case dataType.UInt64:
                                stream.WriteUInt64Optim(ExactConvert.ToULong(kvp.Key));
                                break;

                            case dataType.Single:
                                stream.Write(BitConverter.GetBytes(ExactConvert.ToFloat(kvp.Key)));
                                break;

                            case dataType.Double:
                                stream.Write(BitConverter.GetBytes(ExactConvert.ToDouble(kvp.Key)));
                                break;

                            case dataType.DateTime:
                                stream.Write(BitConverter.GetBytes(((DateTime) kvp.Key).ToBinary()));
                                break;

                            case dataType.Decimal:
                                stream.WriteDecimalOptim(ExactConvert.ToDecimal(kvp.Key));
                                break;

                            case dataType.RawData:
                                WriteBuffer(stream, ExactConvert.ToString(kvp.Key).ToUtf16(), false);
                                break;

                            default:
                                throw new InvalidOperationException("Invalid dictionary key type.");
                        }
                        break;

                    default:
                        throw new InvalidOperationException("Invalid dictionary type.");
                }
            }
            stream.WriteByte((byte) dataType.End);
        }
    }

    private sealed class classifyBinaryFormat : IClassifyFormat<node>
    {
        public static IClassifyFormat<node> Default => _default ??= new classifyBinaryFormat();
        private static classifyBinaryFormat _default;

        private classifyBinaryFormat() { }

        private byte[] readBuffer(Stream stream, bool isUtf8String)
        {
            using var mem = new MemoryStream();
            while (true)
            {
                var b = stream.ReadByte();
                if (b == -1)
                    throw new InvalidOperationException("Unexpected end of data.");
                if (b == 0xff)
                {
                    if (isUtf8String)
                        return mem.ToArray();
                    var b2 = stream.ReadByte();
                    if (b2 == 0)
                        return mem.ToArray();
                    if (b2 != 1)
                        throw new InvalidOperationException("Invalid binary data format.");
                }
                mem.WriteByte((byte) b);
            }
        }

        public node ReadFromStream(Stream stream)
        {
            var dt = (dataType) stream.ReadByte();
            var ts = dt & dataType.TypeSpecMask;
            dt &= dataType.Mask;

            var hasRefId = false;
            var ix = Array.IndexOf(_withRefs, dt);
            if (ix >= 0)
            {
                hasRefId = true;
                dt = _reffables[ix];
            }

            node node;
            dataType keyType = 0;
            switch (dt)
            {
                case dataType.End: return null;
                case dataType.Byte: node = new valueNode { Value = (byte) stream.ReadByte() }; break;
                case dataType.SByte: node = new valueNode { Value = (sbyte) (byte) stream.ReadByte() }; break;
                case dataType.Int16: node = new valueNode { Value = BitConverter.ToInt16(stream.Read(2), 0) }; break;
                case dataType.UInt16: node = new valueNode { Value = BitConverter.ToUInt16(stream.Read(2), 0) }; break;
                case dataType.Int32: node = new valueNode { Value = BitConverter.ToInt32(stream.Read(4), 0) }; break;
                case dataType.UInt32: node = new valueNode { Value = BitConverter.ToUInt32(stream.Read(4), 0) }; break;
                case dataType.Single: node = new valueNode { Value = BitConverter.ToSingle(stream.Read(4), 0) }; break;
                case dataType.Int64: node = new valueNode { Value = BitConverter.ToInt64(stream.Read(8), 0) }; break;
                case dataType.UInt64: node = new valueNode { Value = BitConverter.ToUInt64(stream.Read(8), 0) }; break;
                case dataType.BigInteger: node = new valueNode { Value = readBigInteger(stream) }; break;
                case dataType.Double: node = new valueNode { Value = BitConverter.ToDouble(stream.Read(8), 0) }; break;
                case dataType.DateTime: node = new valueNode { Value = DateTime.FromBinary(BitConverter.ToInt64(stream.Read(8), 0)) }; break;
                case dataType.Decimal: node = new valueNode { Value = stream.ReadDecimalOptim() }; break;
                case dataType.String: node = new valueNode { Value = readBuffer(stream, true).FromUtf8() }; break;
                case dataType.RawData: node = new valueNode { Value = readBuffer(stream, false) }; break;
                case dataType.Null: node = new valueNode { Value = null }; break;
                case dataType.False: node = new valueNode { Value = false }; break;
                case dataType.True: node = new valueNode { Value = true }; break;
                case dataType.Ref: node = new valueNode { Value = stream.ReadInt32Optim() }; break;
                case dataType.KeyValuePair: node = new kvpNode { Key = ReadFromStream(stream), Value = ReadFromStream(stream) }; break;

                case dataType.List:
                    var list = new List<node>();
                    while ((node = ReadFromStream(stream)) != null)
                        list.Add(node);
                    node = new listNode { List = list };
                    break;

                case dataType.DictionaryOther:
                    keyType = (dataType) stream.ReadByte();
                    goto case dataType.DictionaryInt64;

                case dataType.DictionaryInt64:
                case dataType.DictionaryString:
                case dataType.DictionaryTwoStrings:
                    var dict = new Dictionary<object, node>();
                    // Dictionaries encode value first, then key.
                    while ((node = ReadFromStream(stream)) != null)
                    {
                        object key;
                        switch (dt)
                        {
                            case dataType.DictionaryInt64:
                                key = stream.ReadInt64Optim();
                                break;

                            case dataType.DictionaryString:
                                key = readBuffer(stream, true).FromUtf8();
                                break;

                            case dataType.DictionaryTwoStrings:
                                var fieldName = readBuffer(stream, true).FromUtf8();
                                var declaringType = readBuffer(stream, true).FromUtf8();
                                key = declaringType.Length > 0 ? new fieldNameWithType(fieldName, declaringType) : fieldName;
                                break;

                            case dataType.DictionaryOther:
                                key = keyType switch
                                {
                                    dataType.UInt64 => stream.ReadUInt64Optim(),
                                    dataType.Single => BitConverter.ToSingle(stream.Read(4), 0),
                                    dataType.Double => BitConverter.ToSingle(stream.Read(8), 0),
                                    dataType.DateTime => DateTime.FromBinary(BitConverter.ToInt64(stream.Read(8), 0)),
                                    dataType.Decimal => stream.ReadDecimalOptim(),
                                    dataType.RawData => readBuffer(stream, false).FromUtf16(),
                                    _ => throw new InvalidOperationException("Invalid dictionary key type."),
                                };
                                break;

                            default:
                                throw new InvalidOperationException("This case should never be reached.");
                        }
                        dict[key] = node;
                    }
                    node = new dictNode { Dictionary = dict, KeyType = keyType };
                    break;

                default:
                    throw new InvalidOperationException("Invalid object type in binary format.");
            }

            node.DataType = dt;
            if (ts == dataType.SimpleTypeSpec || (node.TypeSpecIsFull = (ts == dataType.FullTypeSpec)))
                node.TypeSpec = readBuffer(stream, true).FromUtf8();
            if (hasRefId)
                node.RefId = stream.ReadInt32Optim();
            return node;
        }

        private BigInteger readBigInteger(Stream stream)
        {
            var value = BigInteger.Zero;
            var mult = BigInteger.One;
            var read = BitConverter.ToUInt64(stream.Read(8), 0);
            while (read < ulong.MaxValue - 1)
            {
                value += read * mult;
                mult *= ulong.MaxValue - 1;
                read = BitConverter.ToUInt64(stream.Read(8), 0);
            }
            return (read == ulong.MaxValue - 1) ? ~value : value;
        }

        void IClassifyFormat<node>.WriteToStream(node element, Stream stream) => element.WriteToStream(stream);

        bool IClassifyFormat<node>.IsNull(node element) => element.DataType == dataType.Null;

        object IClassifyFormat<node>.GetSimpleValue(node element) => element.DataType switch
        {
            dataType.Null or dataType.False or dataType.True or dataType.Byte or dataType.SByte or dataType.Int16 or dataType.UInt16
                or dataType.Int32 or dataType.UInt32 or dataType.Single or dataType.Int64 or dataType.UInt64 or dataType.Double
                or dataType.DateTime or dataType.Decimal or dataType.String or dataType.BigInteger => ((valueNode) element).Value,

            dataType.RawData => ((byte[]) ((valueNode) element).Value).FromUtf16(),

            // These could arise if the class declaration changed and the serialized form still contains the old data type. Tolerate this instead of throwing.
            dataType.List or dataType.KeyValuePair or dataType.DictionaryInt64 or dataType.DictionaryString or dataType.DictionaryOther
                or dataType.DictionaryTwoStrings or dataType.Ref => null,

            _ => throw new InvalidOperationException("The binary format contains unexpected data."),
        };

        node IClassifyFormat<node>.GetSelfValue(node element) => element;   // This should never happen because “node” is a private type.

        IEnumerable<node> IClassifyFormat<node>.GetList(node element, int? tupleSize) =>
            element is listNode list ? list.List : element is kvpNode kvp ? new[] { kvp.Key, kvp.Value } : [];

        (node key, node value) IClassifyFormat<node>.GetKeyValuePair(node element) =>
            element is kvpNode kvp ? (kvp.Key, kvp.Value) : (null, null);

        IEnumerable<KeyValuePair<object, node>> IClassifyFormat<node>.GetDictionary(node element) => element is dictNode dict ? dict.Dictionary : Enumerable.Empty<KeyValuePair<object, node>>();

        byte[] IClassifyFormat<node>.GetRawData(node element)
        {
            if (element.DataType == dataType.List)
                // Support a list of integers as this was how Classify encoded byte arrays before GetRawData was introduced
                return ((listNode) element).List.Select(nd => ExactConvert.ToByte(((valueNode) nd).Value)).ToArray();

            return element.DataType == dataType.RawData ? (byte[]) ((valueNode) element).Value : null;
        }

        bool IClassifyFormat<node>.HasField(node element, string fieldName, string declaringType) => element is dictNode dict && (
                dict.Dictionary.ContainsKey(fieldName) ||
                dict.Dictionary.ContainsKey(new fieldNameWithType(fieldName, declaringType)));

        node IClassifyFormat<node>.GetField(node element, string fieldName, string declaringType)
        {
            var dict = ((dictNode) element).Dictionary;
            return
                dict.TryGetValue(new fieldNameWithType(fieldName, declaringType), out var node) ? node :
                dict.TryGetValue(fieldName, out node) ? node : null;
        }

        (string type, bool isFullType) IClassifyFormat<node>.GetType(node element) =>
            (element.TypeSpec, element.TypeSpecIsFull && element.TypeSpec != null);

        bool IClassifyFormat<node>.IsReference(node element) => element.DataType == dataType.Ref;

        bool IClassifyFormat<node>.IsReferable(node element) => element.RefId.HasValue;

        int IClassifyFormat<node>.GetReferenceID(node element) => element.RefId ?? (element.DataType == dataType.Ref ? (int) ((valueNode) element).Value :
                throw new InvalidOperationException("The binary Classify format encountered a contractual violation perpetrated by Classify. GetReferenceID() should not be called unless IsReference() or IsReferable() returned true."));

        node IClassifyFormat<node>.FormatNullValue() => new valueNode { Value = null, DataType = dataType.Null };

        private valueNode tryFormatSimpleValue<T>(object value, dataType dt) => value is T ? new valueNode { DataType = dt, Value = value } :
                !ExactConvert.Try(typeof(T), value, out var result) ? null :
                !ExactConvert.Try(value.GetType(), result, out var retour) || !retour.Equals(value) ? null :
                new valueNode { DataType = dt, Value = result };

        node IClassifyFormat<node>.FormatSimpleValue(object value)
        {
            if (value == null)
                return new valueNode { Value = null, DataType = dataType.Null };

            if (value is float)
                return new valueNode { DataType = dataType.Single, Value = value };
            if (value is double)
                return new valueNode { DataType = dataType.Double, Value = value };
            if (value is decimal)
                return new valueNode { DataType = dataType.Decimal, Value = value };
            if (value is DateTime)
                return new valueNode { DataType = dataType.DateTime, Value = value };

            // Use the smallest possible representation of the input.
            // Note that if the input is, say, the Int64 value 1, it will be stored compactly as the boolean “true”.
            // In fact, even the strings "True" and "False" will be stored that way and correctly converted back.
            // Strings that roundtrip-convert to DateTime are also stored compactly as DateTime.
            var node =
                tryFormatSimpleValue<bool>(value, dataType.False) ??
                tryFormatSimpleValue<byte>(value, dataType.Byte) ??
                tryFormatSimpleValue<sbyte>(value, dataType.SByte) ??
                tryFormatSimpleValue<short>(value, dataType.Int16) ??
                tryFormatSimpleValue<ushort>(value, dataType.UInt16) ??
                tryFormatSimpleValue<int>(value, dataType.Int32) ??
                tryFormatSimpleValue<uint>(value, dataType.UInt32) ??
                tryFormatSimpleValue<long>(value, dataType.Int64) ??
                tryFormatSimpleValue<ulong>(value, dataType.UInt64) ??
                tryFormatSimpleValue<DateTime>(value, dataType.DateTime) ??
                tryFormatSimpleValue<BigInteger>(value, dataType.BigInteger);

            if (node != null)
            {
                if (node.Value.Equals(true))
                    node.DataType = dataType.True;
                return node;
            }

            var str = ExactConvert.ToString(value);
            return str.Utf8Length() < str.Utf16Length()
                ? new valueNode { Value = str, DataType = dataType.String }
                : new valueNode { Value = str.ToUtf16(), DataType = dataType.RawData };
        }

        node IClassifyFormat<node>.FormatSelfValue(node value) => throw new InvalidOperationException("This should never happen.");

        node IClassifyFormat<node>.FormatList(bool isTuple, IEnumerable<node> values)
        {
            var list = values.ToList();
            return list.Count == 2
                ? new kvpNode { Key = list[0], Value = list[1], DataType = dataType.KeyValuePair }
                : new listNode { List = list, DataType = dataType.List };
        }

        node IClassifyFormat<node>.FormatKeyValuePair(node key, node value) => new kvpNode { Key = key, Value = value, DataType = dataType.KeyValuePair };

        node IClassifyFormat<node>.FormatDictionary(IEnumerable<KeyValuePair<object, node>> values)
        {
            var dic = values.ToDictionary();
            if (dic.Count == 0)
                return new dictNode { Dictionary = dic, DataType = dataType.DictionaryString };

            var firstKey = dic.Keys.First();
            var keyType = firstKey.GetType();
            Type underlyingType = null;
            if (firstKey is Enum)
                underlyingType = keyType.GetEnumUnderlyingType();

            dataType dt;
            dataType kt;

            if (keyType == typeof(ulong) || underlyingType == typeof(ulong))
            {
                dt = dataType.DictionaryOther;
                kt = dataType.UInt64;
            }
            else if (ExactConvert.IsTrueIntegerType(keyType) || ExactConvert.IsTrueIntegerType(underlyingType))
            {
                dt = dataType.DictionaryInt64;
                kt = 0;
            }
            else if (keyType == typeof(string))
            {
                var utf8len = dic.Keys.Take(32).Sum(k => ((string) k).Utf8Length());
                var utf16len = dic.Keys.Take(32).Sum(k => ((string) k).Utf16Length());
                if (utf8len > utf16len)
                {
                    dt = dataType.DictionaryOther;
                    kt = dataType.RawData;
                }
                else
                {
                    dt = dataType.DictionaryString;
                    kt = 0;
                }
            }
            else if (keyType == typeof(float))
            {
                dt = dataType.DictionaryOther;
                kt = dataType.Single;
            }
            else if (keyType == typeof(double))
            {
                dt = dataType.DictionaryOther;
                kt = dataType.Double;
            }
            else if (keyType == typeof(decimal))
            {
                dt = dataType.DictionaryOther;
                kt = dataType.Decimal;
            }
            else if (keyType == typeof(DateTime))
            {
                dt = dataType.DictionaryOther;
                kt = dataType.DateTime;
            }
            else
            {
                dt = dataType.DictionaryString;
                kt = 0;
            }

            return new dictNode { Dictionary = dic, DataType = dt, KeyType = kt };
        }

        node IClassifyFormat<node>.FormatObject(IEnumerable<ObjectFieldInfo<node>> fields)
        {
            var dic = fields.ToDictionary(f => f.DeclaringType == null ? f.FieldName : (object) new fieldNameWithType(f.FieldName, f.DeclaringType), f => f.Value);
            return new dictNode
            {
                DataType = dic.Keys.Any(k => k is fieldNameWithType) ? dataType.DictionaryTwoStrings : dataType.DictionaryString,
                Dictionary = dic
            };
        }

        node IClassifyFormat<node>.FormatRawData(byte[] value) => new valueNode { DataType = dataType.RawData, Value = value };

        node IClassifyFormat<node>.FormatReference(int refId) => new valueNode { Value = refId, DataType = dataType.Ref };

        node IClassifyFormat<node>.FormatReferable(node element, int refId)
        {
            element.RefId = refId;
            return element;
        }

        node IClassifyFormat<node>.FormatWithType(node element, string type, bool isFullType)
        {
            element.TypeSpec = type;
            element.TypeSpecIsFull = isFullType;
            return element;
        }

        void IClassifyFormat<node>.ThrowMissingReferable(int refID) =>
            throw new InvalidOperationException(@"An object reference was encountered, but no matching object was encountered during deserialization. If such an object is present somewhere in the binary data, the relevant object was not deserialized (most likely because a field corresponding to a parent object was removed from its class declaration).");
    }
}
