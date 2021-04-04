using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using RT.Util;
using RT.Util.ExtensionMethods;

namespace RT.Serialization
{
    /// <summary>Offers a convenient way to use <see cref="Classify"/> to serialize objects using a compact binary format.</summary>
    public static class ClassifyBinary
    {
        private static readonly IClassifyFormat<node> DefaultFormat = ClassifyBinaryFormat.Default;

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
        public static T DeserializeFile<T>(string filename, ClassifyOptions options = null)
        {
            return Classify.DeserializeFile<node, T>(filename, DefaultFormat, options);
        }

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
        public static object DeserializeFile(Type type, string filename, ClassifyOptions options = null)
        {
            return Classify.DeserializeFile(type, filename, DefaultFormat, options);
        }

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
            using (var mem = new MemoryStream(binaryData))
                return Classify.Deserialize<node, T>(DefaultFormat.ReadFromStream(mem), DefaultFormat, options);
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
            using (var mem = new MemoryStream(binaryData))
                return Classify.Deserialize(type, DefaultFormat.ReadFromStream(mem), DefaultFormat, options);
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
            using (var mem = new MemoryStream(binaryData))
                Classify.DeserializeIntoObject(DefaultFormat.ReadFromStream(mem), intoObject, DefaultFormat, options);
        }

        /// <summary>
        ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired
        ///     type. The type of object is inferred from the object passed in.</summary>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object. Also determines the type of object
        ///     expected.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void DeserializeFileIntoObject(string filename, object intoObject, ClassifyOptions options = null)
        {
            Classify.DeserializeFileIntoObject(filename, intoObject, DefaultFormat, options);
        }

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
        public static void SerializeToFile<T>(T saveObject, string filename, ClassifyOptions options = null)
        {
            Classify.SerializeToFile(saveObject, filename, DefaultFormat, options);
        }

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
        public static void SerializeToFile(Type saveType, object saveObject, string filename, ClassifyOptions options = null)
        {
            Classify.SerializeToFile(saveType, saveObject, filename, DefaultFormat, options);
        }

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
            var node = Classify.Serialize(saveObject, DefaultFormat, options);
            using (var mem = new MemoryStream())
            {
                node.WriteToStream(mem);
                return mem.ToArray();
            }
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
            var node = Classify.Serialize(saveType, saveObject, DefaultFormat, options);
            using (var mem = new MemoryStream())
            {
                node.WriteToStream(mem);
                return mem.ToArray();
            }
        }

        [Flags]
        private enum DataType : byte
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
        private static readonly DataType[] _reffables = new[] {
            DataType.DictionaryInt64,
            DataType.DictionaryString,
            DataType.DictionaryOther,
            DataType.DictionaryTwoStrings,
            DataType.List,
            DataType.RawData
        };

        // These must correspond index-by-index with those in _reffables
        private static readonly DataType[] _withRefs = new[] {
            DataType.DictionaryInt64WithRefId,
            DataType.DictionaryStringWithRefId,
            DataType.DictionaryOtherWithRefId,
            DataType.DictionaryTwoStringsWithRefId,
            DataType.ListWithRefId,
            DataType.RawDataWithRefId
        };

        private abstract class node
        {
            public int? RefId;
            public DataType DataType;
            public string TypeSpec;
            public bool TypeSpecIsFull;

            public void WriteToStream(Stream stream)
            {
                var dt = DataType;
                var dtStr = DataType.ToString();
                var typeSpec =
                    TypeSpec == null ? DataType.NoTypeSpec :
                    TypeSpecIsFull ? DataType.FullTypeSpec : DataType.SimpleTypeSpec;

                if (RefId != null)
                {
                    var ix = _reffables.IndexOf(dt);
                    Ut.Assert(ix >= 0);
                    dt = _withRefs[ix];
                }

                stream.WriteByte((byte) (dt | typeSpec));

                writeToStreamImpl(stream);

                if (typeSpec == DataType.SimpleTypeSpec || typeSpec == DataType.FullTypeSpec)
                    WriteBuffer(stream, TypeSpec.ToUtf8(), true);

                if (RefId != null)
                    stream.WriteInt32Optim(RefId.Value);
            }
            protected abstract void writeToStreamImpl(Stream stream);

            public void WriteBuffer(Stream stream, byte[] buffer, bool isUtf8String)
            {
                for (int i = 0; i < buffer.Length; i++)
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
                    case DataType.Byte: stream.WriteByte((byte) Value); break;
                    case DataType.SByte: stream.WriteByte((byte) (sbyte) Value); break;
                    case DataType.Int16: stream.Write(BitConverter.GetBytes((short) Value)); break;
                    case DataType.UInt16: stream.Write(BitConverter.GetBytes((ushort) Value)); break;
                    case DataType.Int32: stream.Write(BitConverter.GetBytes((int) Value)); break;
                    case DataType.UInt32: stream.Write(BitConverter.GetBytes((uint) Value)); break;
                    case DataType.Single: stream.Write(BitConverter.GetBytes((float) Value)); break;
                    case DataType.Int64: stream.Write(BitConverter.GetBytes((long) Value)); break;
                    case DataType.UInt64: stream.Write(BitConverter.GetBytes((ulong) Value)); break;
                    case DataType.BigInteger: WriteBigInteger(stream, (BigInteger) Value); break;
                    case DataType.Double: stream.Write(BitConverter.GetBytes((double) Value)); break;
                    case DataType.DateTime: stream.Write(BitConverter.GetBytes(((DateTime) Value).ToBinary())); break;
                    case DataType.Decimal: stream.WriteDecimalOptim((decimal) Value); break;
                    case DataType.String: WriteBuffer(stream, ((string) Value).ToUtf8(), true); break;
                    case DataType.RawData: WriteBuffer(stream, (byte[]) Value, false); break;
                    case DataType.Null: break;
                    case DataType.False: break;
                    case DataType.True: break;
                    case DataType.Ref: stream.WriteInt32Optim((int) Value); break;
                }
            }

            private void WriteBigInteger(Stream stream, BigInteger value)
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
                stream.Write(new byte[] { (byte) DataType.End });
            }
        }

        sealed class FieldNameWithType : IEquatable<FieldNameWithType>
        {
            public string FieldName { get; private set; }
            public string DeclaringType { get; private set; }

            public FieldNameWithType(string fieldName, string declaringType)
            {
                FieldName = fieldName;
                DeclaringType = declaringType;
            }

            public bool Equals(FieldNameWithType other)
            {
                return other != null && other.FieldName == FieldName && other.DeclaringType == DeclaringType;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as FieldNameWithType);
            }

            public override int GetHashCode()
            {
                return FieldName.GetHashCode() * 37 + DeclaringType.GetHashCode();
            }
        }

        private sealed class dictNode : node
        {
            public Dictionary<object, node> Dictionary;
            public DataType KeyType;

            protected override void writeToStreamImpl(Stream stream)
            {
                if (DataType == DataType.DictionaryOther)
                    stream.WriteByte((byte) KeyType);

                foreach (var kvp in Dictionary)
                {
                    // Store value first
                    kvp.Value.WriteToStream(stream);

                    // Then store key
                    switch (DataType)
                    {
                        case DataType.DictionaryInt64:
                            stream.WriteInt32Optim(ExactConvert.ToInt(kvp.Key));
                            break;

                        case DataType.DictionaryString:
                            WriteBuffer(stream, ExactConvert.ToString(kvp.Key).ToUtf8(), true);
                            break;

                        case DataType.DictionaryTwoStrings:
                            if (kvp.Key is string key)
                            {
                                WriteBuffer(stream, key.ToUtf8(), true);
                                WriteBuffer(stream, new byte[0], true);
                            }
                            else
                            {
                                var fn = (FieldNameWithType) kvp.Key;
                                WriteBuffer(stream, fn.FieldName.ToUtf8(), true);
                                WriteBuffer(stream, fn.DeclaringType == null ? new byte[0] : fn.DeclaringType.ToUtf8(), true);
                            }
                            break;

                        case DataType.DictionaryOther:
                            switch (KeyType)
                            {
                                case DataType.UInt64:
                                    stream.WriteUInt64Optim(ExactConvert.ToULong(kvp.Key));
                                    break;

                                case DataType.Single:
                                    stream.Write(BitConverter.GetBytes(ExactConvert.ToFloat(kvp.Key)));
                                    break;

                                case DataType.Double:
                                    stream.Write(BitConverter.GetBytes(ExactConvert.ToDouble(kvp.Key)));
                                    break;

                                case DataType.DateTime:
                                    stream.Write(BitConverter.GetBytes(((DateTime) kvp.Key).ToBinary()));
                                    break;

                                case DataType.Decimal:
                                    stream.WriteDecimalOptim(ExactConvert.ToDecimal(kvp.Key));
                                    break;

                                case DataType.RawData:
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
                stream.WriteByte((byte) DataType.End);
            }
        }

        private sealed class ClassifyBinaryFormat : IClassifyFormat<node>
        {
            public static IClassifyFormat<node> Default { get { return _default ?? (_default = new ClassifyBinaryFormat()); } }
            private static ClassifyBinaryFormat _default;

            private ClassifyBinaryFormat() { }

            private byte[] readBuffer(Stream stream, bool isUtf8String)
            {
                using (var mem = new MemoryStream())
                {
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
            }

            public node ReadFromStream(Stream stream)
            {
                var dt = (DataType) stream.ReadByte();
                var ts = dt & DataType.TypeSpecMask;
                dt &= DataType.Mask;

                var hasRefId = false;
                var ix = Array.IndexOf(_withRefs, dt);
                if (ix >= 0)
                {
                    hasRefId = true;
                    dt = _reffables[ix];
                }

                node node;
                DataType keyType = 0;
                switch (dt)
                {
                    case DataType.End: return null;
                    case DataType.Byte: node = new valueNode { Value = (byte) stream.ReadByte() }; break;
                    case DataType.SByte: node = new valueNode { Value = (sbyte) (byte) stream.ReadByte() }; break;
                    case DataType.Int16: node = new valueNode { Value = BitConverter.ToInt16(stream.Read(2), 0) }; break;
                    case DataType.UInt16: node = new valueNode { Value = BitConverter.ToUInt16(stream.Read(2), 0) }; break;
                    case DataType.Int32: node = new valueNode { Value = BitConverter.ToInt32(stream.Read(4), 0) }; break;
                    case DataType.UInt32: node = new valueNode { Value = BitConverter.ToUInt32(stream.Read(4), 0) }; break;
                    case DataType.Single: node = new valueNode { Value = BitConverter.ToSingle(stream.Read(4), 0) }; break;
                    case DataType.Int64: node = new valueNode { Value = BitConverter.ToInt64(stream.Read(8), 0) }; break;
                    case DataType.UInt64: node = new valueNode { Value = BitConverter.ToUInt64(stream.Read(8), 0) }; break;
                    case DataType.BigInteger: node = new valueNode { Value = readBigInteger(stream) }; break;
                    case DataType.Double: node = new valueNode { Value = BitConverter.ToDouble(stream.Read(8), 0) }; break;
                    case DataType.DateTime: node = new valueNode { Value = DateTime.FromBinary(BitConverter.ToInt64(stream.Read(8), 0)) }; break;
                    case DataType.Decimal: node = new valueNode { Value = stream.ReadDecimalOptim() }; break;
                    case DataType.String: node = new valueNode { Value = readBuffer(stream, true).FromUtf8() }; break;
                    case DataType.RawData: node = new valueNode { Value = readBuffer(stream, false) }; break;
                    case DataType.Null: node = new valueNode { Value = null }; break;
                    case DataType.False: node = new valueNode { Value = false }; break;
                    case DataType.True: node = new valueNode { Value = true }; break;
                    case DataType.Ref: node = new valueNode { Value = stream.ReadInt32Optim() }; break;
                    case DataType.KeyValuePair: node = new kvpNode { Key = ReadFromStream(stream), Value = ReadFromStream(stream) }; break;

                    case DataType.List:
                        var list = new List<node>();
                        while ((node = ReadFromStream(stream)) != null)
                            list.Add(node);
                        node = new listNode { List = list };
                        break;

                    case DataType.DictionaryOther:
                        keyType = (DataType) stream.ReadByte();
                        goto case DataType.DictionaryInt64;

                    case DataType.DictionaryInt64:
                    case DataType.DictionaryString:
                    case DataType.DictionaryTwoStrings:
                        var dict = new Dictionary<object, node>();
                        // Dictionaries encode value first, then key.
                        while ((node = ReadFromStream(stream)) != null)
                        {
                            object key;
                            switch (dt)
                            {
                                case DataType.DictionaryInt64:
                                    key = stream.ReadInt64Optim();
                                    break;

                                case DataType.DictionaryString:
                                    key = readBuffer(stream, true).FromUtf8();
                                    break;

                                case DataType.DictionaryTwoStrings:
                                    var fieldName = readBuffer(stream, true).FromUtf8();
                                    var declaringType = readBuffer(stream, true).FromUtf8();
                                    key = declaringType.Length > 0 ? new FieldNameWithType(fieldName, declaringType) : (object) fieldName;
                                    break;

                                case DataType.DictionaryOther:
                                    switch (keyType)
                                    {
                                        case DataType.UInt64:
                                            key = stream.ReadUInt64Optim();
                                            break;

                                        case DataType.Single:
                                            key = BitConverter.ToSingle(stream.Read(4), 0);
                                            break;

                                        case DataType.Double:
                                            key = BitConverter.ToSingle(stream.Read(8), 0);
                                            break;

                                        case DataType.DateTime:
                                            key = DateTime.FromBinary(BitConverter.ToInt64(stream.Read(8), 0));
                                            break;

                                        case DataType.Decimal:
                                            key = stream.ReadDecimalOptim();
                                            break;

                                        case DataType.RawData:
                                            key = readBuffer(stream, false).FromUtf16();
                                            break;

                                        default:
                                            throw new InvalidOperationException("Invalid dictionary key type.");
                                    }
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
                if (ts == DataType.SimpleTypeSpec || (node.TypeSpecIsFull = (ts == DataType.FullTypeSpec)))
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

            void IClassifyFormat<node>.WriteToStream(node element, Stream stream)
            {
                element.WriteToStream(stream);
            }

            bool IClassifyFormat<node>.IsNull(node element)
            {
                return element.DataType == DataType.Null;
            }

            object IClassifyFormat<node>.GetSimpleValue(node element)
            {
                switch (element.DataType)
                {
                    case DataType.Null:
                    case DataType.False:
                    case DataType.True:
                    case DataType.Byte:
                    case DataType.SByte:
                    case DataType.Int16:
                    case DataType.UInt16:
                    case DataType.Int32:
                    case DataType.UInt32:
                    case DataType.Single:
                    case DataType.Int64:
                    case DataType.UInt64:
                    case DataType.Double:
                    case DataType.DateTime:
                    case DataType.Decimal:
                    case DataType.String:
                    case DataType.BigInteger:
                        return ((valueNode) element).Value;

                    case DataType.RawData:
                        return ((byte[]) ((valueNode) element).Value).FromUtf16();

                    case DataType.List:
                    case DataType.KeyValuePair:
                    case DataType.DictionaryInt64:
                    case DataType.DictionaryString:
                    case DataType.DictionaryOther:
                    case DataType.DictionaryTwoStrings:
                    case DataType.Ref:
                        // These could arise if the class declaration changed and the serialized form still contains the old data type. Tolerate this instead of throwing.
                        return null;

                    default:
                        throw new InvalidOperationException("The binary format contains unexpected data.");
                }
            }

            node IClassifyFormat<node>.GetSelfValue(node element)
            {
                // This should never happen because “node” is a private type.
                return element;
            }

            IEnumerable<node> IClassifyFormat<node>.GetList(node element, int? tupleSize)
            {
                var list = element as listNode;
                var kvp = element as kvpNode;
                return list == null ? kvp == null ? Enumerable.Empty<node>() : new[] { kvp.Key, kvp.Value } : list.List;
            }

            void IClassifyFormat<node>.GetKeyValuePair(node element, out node key, out node value)
            {
                var kvp = element as kvpNode;
                if (kvp == null)
                {
                    key = null;
                    value = null;
                    return;
                }

                key = kvp.Key;
                value = kvp.Value;
            }

            IEnumerable<KeyValuePair<object, node>> IClassifyFormat<node>.GetDictionary(node element)
            {
                var dict = element as dictNode;
                if (dict == null)
                    return Enumerable.Empty<KeyValuePair<object, node>>();
                return dict.Dictionary;
            }

            byte[] IClassifyFormat<node>.GetRawData(node element)
            {
                if (element.DataType == DataType.List)
                    // Support a list of integers as this was how Classify encoded byte arrays before GetRawData was introduced
                    return ((listNode) element).List.Select(nd => ExactConvert.ToByte(((valueNode) nd).Value)).ToArray();

                if (element.DataType == DataType.RawData)
                    return (byte[]) ((valueNode) element).Value;
                return null;
            }

            bool IClassifyFormat<node>.HasField(node element, string fieldName, string declaringType)
            {
                return element is dictNode && (
                    ((dictNode) element).Dictionary.ContainsKey(fieldName) ||
                    ((dictNode) element).Dictionary.ContainsKey(new FieldNameWithType(fieldName, declaringType)));
            }

            node IClassifyFormat<node>.GetField(node element, string fieldName, string declaringType)
            {
                var dict = ((dictNode) element).Dictionary;
                node node;

                if (dict.TryGetValue(new FieldNameWithType(fieldName, declaringType), out node))
                    return node;

                if (dict.TryGetValue(fieldName, out node))
                    return node;

                return null;
            }

            string IClassifyFormat<node>.GetType(node element, out bool isFullType)
            {
                isFullType = element.TypeSpecIsFull && element.TypeSpec != null;
                return element.TypeSpec;
            }

            bool IClassifyFormat<node>.IsReference(node element)
            {
                return element.DataType == DataType.Ref;
            }

            bool IClassifyFormat<node>.IsReferable(node element)
            {
                return element.RefId.HasValue;
            }

            int IClassifyFormat<node>.GetReferenceID(node element)
            {
                if (element.RefId.HasValue)
                    return element.RefId.Value;
                else if (element.DataType == DataType.Ref)
                    return (int) ((valueNode) element).Value;
                else
                    throw new InvalidOperationException("The binary Classify format encountered a contractual violation perpetrated by Classify. GetReferenceID() should not be called unless IsReference() or IsReferable() returned true.");
            }

            node IClassifyFormat<node>.FormatNullValue()
            {
                return new valueNode { Value = null, DataType = DataType.Null };
            }

            valueNode tryFormatSimpleValue<T>(object value, DataType dt)
            {
                if (value is T)
                    return new valueNode { DataType = dt, Value = value };
                if (!ExactConvert.Try(typeof(T), value, out var result))
                    return null;
                if (!ExactConvert.Try(value.GetType(), result, out var retour) || !retour.Equals(value))
                    return null;
                return new valueNode { DataType = dt, Value = result };
            }

            node IClassifyFormat<node>.FormatSimpleValue(object value)
            {
                if (value == null)
                    return new valueNode { Value = null, DataType = DataType.Null };

                if (value is float)
                    return new valueNode { DataType = DataType.Single, Value = value };
                if (value is double)
                    return new valueNode { DataType = DataType.Double, Value = value };
                if (value is decimal)
                    return new valueNode { DataType = DataType.Decimal, Value = value };
                if (value is DateTime)
                    return new valueNode { DataType = DataType.DateTime, Value = value };

                // Use the smallest possible representation of the input.
                // Note that if the input is, say, the Int64 value 1, it will be stored compactly as the boolean “true”.
                // In fact, even the strings "True" and "False" will be stored that way and correctly converted back.
                // Strings that roundtrip-convert to DateTime are also stored compactly as DateTime.
                var node =
                    tryFormatSimpleValue<bool>(value, DataType.False) ??
                    tryFormatSimpleValue<byte>(value, DataType.Byte) ??
                    tryFormatSimpleValue<sbyte>(value, DataType.SByte) ??
                    tryFormatSimpleValue<short>(value, DataType.Int16) ??
                    tryFormatSimpleValue<ushort>(value, DataType.UInt16) ??
                    tryFormatSimpleValue<int>(value, DataType.Int32) ??
                    tryFormatSimpleValue<uint>(value, DataType.UInt32) ??
                    tryFormatSimpleValue<long>(value, DataType.Int64) ??
                    tryFormatSimpleValue<ulong>(value, DataType.UInt64) ??
                    tryFormatSimpleValue<DateTime>(value, DataType.DateTime) ??
                    tryFormatSimpleValue<BigInteger>(value, DataType.BigInteger);

                if (node != null)
                {
                    if (node.Value.Equals(true))
                        node.DataType = DataType.True;
                    return node;
                }

                var str = ExactConvert.ToString(value);
                var strAsUtf16 = str.ToUtf16();
                return str.Utf8Length() < strAsUtf16.Length
                    ? new valueNode { Value = str, DataType = DataType.String }
                    : new valueNode { Value = strAsUtf16, DataType = DataType.RawData };
            }

            node IClassifyFormat<node>.FormatSelfValue(node value)
            {
                throw new InvalidOperationException("This should never happen.");
            }

            node IClassifyFormat<node>.FormatList(bool isTuple, IEnumerable<node> values)
            {
                var list = values.ToList();
                if (list.Count == 2)
                    return new kvpNode { Key = list[0], Value = list[1], DataType = DataType.KeyValuePair };
                return new listNode { List = values.ToList(), DataType = DataType.List };
            }

            node IClassifyFormat<node>.FormatKeyValuePair(node key, node value)
            {
                return new kvpNode { Key = key, Value = value, DataType = DataType.KeyValuePair };
            }

            node IClassifyFormat<node>.FormatDictionary(IEnumerable<KeyValuePair<object, node>> values)
            {
                var dic = values.ToDictionary();
                if (dic.Count == 0)
                    return new dictNode { Dictionary = dic, DataType = DataType.DictionaryString };

                var firstKey = dic.Keys.First();
                var keyType = firstKey.GetType();
                Type underlyingType = null;
                if (firstKey is Enum)
                    underlyingType = keyType.GetEnumUnderlyingType();

                DataType dt;
                DataType kt;

                if (keyType == typeof(ulong) || underlyingType == typeof(ulong))
                {
                    dt = DataType.DictionaryOther;
                    kt = DataType.UInt64;
                }
                else if (ExactConvert.IsTrueIntegerType(keyType) || ExactConvert.IsTrueIntegerType(underlyingType))
                {
                    dt = DataType.DictionaryInt64;
                    kt = 0;
                }
                else if (keyType == typeof(string))
                {
                    var utf8len = dic.Keys.Take(32).Sum(k => ((string) k).Utf8Length());
                    var utf16len = dic.Keys.Take(32).Sum(k => ((string) k).Utf16Length());
                    if (utf8len > utf16len)
                    {
                        dt = DataType.DictionaryOther;
                        kt = DataType.RawData;
                    }
                    else
                    {
                        dt = DataType.DictionaryString;
                        kt = 0;
                    }
                }
                else if (keyType == typeof(float))
                {
                    dt = DataType.DictionaryOther;
                    kt = DataType.Single;
                }
                else if (keyType == typeof(double))
                {
                    dt = DataType.DictionaryOther;
                    kt = DataType.Double;
                }
                else if (keyType == typeof(decimal))
                {
                    dt = DataType.DictionaryOther;
                    kt = DataType.Decimal;
                }
                else if (keyType == typeof(DateTime))
                {
                    dt = DataType.DictionaryOther;
                    kt = DataType.DateTime;
                }
                else
                {
                    dt = DataType.DictionaryString;
                    kt = 0;
                }

                return new dictNode { Dictionary = dic, DataType = dt, KeyType = kt };
            }

            node IClassifyFormat<node>.FormatObject(IEnumerable<ObjectFieldInfo<node>> fields)
            {
                var dic = fields.ToDictionary(f => f.DeclaringType == null ? f.FieldName : (object) new FieldNameWithType(f.FieldName, f.DeclaringType), f => f.Value);
                return new dictNode
                {
                    DataType = dic.Keys.Any(k => k is FieldNameWithType) ? DataType.DictionaryTwoStrings : DataType.DictionaryString,
                    Dictionary = dic
                };
            }

            node IClassifyFormat<node>.FormatRawData(byte[] value)
            {
                return new valueNode { DataType = DataType.RawData, Value = value };
            }

            node IClassifyFormat<node>.FormatReference(int refId)
            {
                return new valueNode { Value = refId, DataType = DataType.Ref };
            }

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

            void IClassifyFormat<node>.ThrowMissingReferable(int refID)
            {
                throw new InvalidOperationException(@"An object reference was encountered, but no matching object was encountered during deserialization. If such an object is present somewhere in the binary data, the relevant object was not deserialized (most likely because a field corresponding to a parent object was removed from its class declaration).");
            }
        }
    }
}
