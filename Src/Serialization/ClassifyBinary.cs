using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace RT.Util.Serialization
{
    /// <summary>Offers a convenient way to use <see cref="Classify"/> to serialize objects using a compact binary format.</summary>
    public static class ClassifyBinary
    {
        private static IClassifyFormat<node> DefaultFormat = ClassifyBinaryFormat.Default;

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified file.</summary>
        /// <typeparam name="T">
        ///     Type of object to read.</typeparam>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="parent">
        ///     If the class to be declassified has a field with the <see cref="ClassifyParentAttribute"/>, that field will
        ///     receive this object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T DeserializeFile<T>(string filename, ClassifyOptions options = null, object parent = null)
        {
            return Classify.DeserializeFile<node, T>(filename, DefaultFormat, options, parent);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified file.</summary>
        /// <param name="type">
        ///     Type of object to read.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="parent">
        ///     If the class to be declassified has a field with the <see cref="ClassifyParentAttribute"/>, that field will
        ///     receive this object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object DeserializeFile(Type type, string filename, ClassifyOptions options = null, object parent = null)
        {
            return Classify.DeserializeFile(type, filename, DefaultFormat, options, parent);
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
        ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired type.
        ///     The type of object is inferred from the object passed in.</summary>
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
            Classify.SerializeToFile<node>(saveType, saveObject, filename, DefaultFormat, options);
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
        private enum dataType : byte
        {
            End = 0x00,

            // Simple types
            Byte = 0x01,
            SByte = 0x02,
            Short = 0x03,
            UShort = 0x04,
            // 0x05: unused
            Int = 0x06,
            UInt = 0x07,
            Float = 0x08,
            Long = 0x09,
            ULong = 0x0a,
            Double = 0x0b,
            DateTime = 0x0c,
            Decimal = 0x0d,
            StringUtf8 = 0x0e,
            StringUtf16 = 0x0f,

            // Dictionaries (the types are the type of the key)
            DictionaryInt = 0x10,
            DictionaryLong = 0x11,
            DictionaryULong = 0x12,
            DictionaryDouble = 0x13,
            DictionaryDateTime = 0x14,
            DictionaryStringUtf8 = 0x15,
            DictionaryTwoStringsUtf8 = 0x16,
            // 0x17: unused
            // 0x18: unused

            // Other values
            Null = 0x19,
            False = 0x1a,
            True = 0x1b,
            // 0x1c: unused

            // Complex types
            List = 0x1d,
            Kvp = 0x1e,
            Ref = 0x1f,

            Mask = 0x1f,

            HasRefId = 0x20,
            HasTypeSpec = 0x40,
            HasFullTypeSpec = 0x80
        }

        private abstract class node
        {
            public int? RefId;
            public dataType DataType;
            public string TypeSpec;
            public bool TypeSpecIsFull;
            public void WriteToStream(Stream stream)
            {
                var dt = DataType;
                if (RefId != null)
                    dt = dt | dataType.HasRefId;
                if (TypeSpec != null)
                    dt = dt | (TypeSpecIsFull ? dataType.HasFullTypeSpec : dataType.HasTypeSpec);
                stream.WriteByte((byte) dt);
                writeToStreamImpl(stream);
                if (TypeSpec != null)
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
                    case dataType.Byte: stream.WriteByte((byte) Value); break;
                    case dataType.SByte: stream.WriteByte((byte) (sbyte) Value); break;
                    case dataType.Short: stream.Write(BitConverter.GetBytes((short) Value)); break;
                    case dataType.UShort: stream.Write(BitConverter.GetBytes((ushort) Value)); break;
                    case dataType.Int: stream.Write(BitConverter.GetBytes((int) Value)); break;
                    case dataType.UInt: stream.Write(BitConverter.GetBytes((uint) Value)); break;
                    case dataType.Float: stream.Write(BitConverter.GetBytes((float) Value)); break;
                    case dataType.Long: stream.Write(BitConverter.GetBytes((long) Value)); break;
                    case dataType.ULong: stream.Write(BitConverter.GetBytes((ulong) Value)); break;
                    case dataType.Double: stream.Write(BitConverter.GetBytes((double) Value)); break;
                    case dataType.DateTime: stream.Write(BitConverter.GetBytes(((DateTime) Value).ToBinary())); break;
                    case dataType.Decimal: stream.WriteDecimalOptim((decimal) Value); break;
                    case dataType.StringUtf8: WriteBuffer(stream, ((string) Value).ToUtf8(), true); break;
                    case dataType.StringUtf16: WriteBuffer(stream, ((string) Value).ToUtf16(), false); break;
                    case dataType.Null: break;
                    case dataType.False: break;
                    case dataType.True: break;
                    case dataType.Ref: stream.WriteInt32Optim((int) Value); break;
                }
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
                stream.Write(new byte[] { (byte) dataType.End });
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

            public override int GetHashCode()
            {
                return FieldName.GetHashCode() * 37 + DeclaringType.GetHashCode();
            }
        }

        private sealed class dictNode : node
        {
            public Dictionary<object, node> Dictionary;
            protected override void writeToStreamImpl(Stream stream)
            {
                foreach (var kvp in Dictionary)
                {
                    // Store value first
                    kvp.Value.WriteToStream(stream);

                    // Then store key
                    switch (DataType)
                    {
                        case dataType.DictionaryInt:
                            stream.WriteInt32Optim(ExactConvert.ToInt(kvp.Key));
                            break;
                        case dataType.DictionaryLong:
                            stream.WriteInt64Optim(ExactConvert.ToLong(kvp.Key));
                            break;
                        case dataType.DictionaryULong:
                            stream.WriteUInt64Optim(ExactConvert.ToULong(kvp.Key));
                            break;
                        case dataType.DictionaryDouble:
                            stream.Write(BitConverter.GetBytes(ExactConvert.ToDouble(kvp.Key)));
                            break;
                        case dataType.DictionaryDateTime:
                            stream.Write(BitConverter.GetBytes(((DateTime) kvp.Key).ToBinary()));
                            break;
                        case dataType.DictionaryStringUtf8:
                            WriteBuffer(stream, ExactConvert.ToString(kvp.Key).ToUtf8(), true);
                            break;
                        case dataType.DictionaryTwoStringsUtf8:
                            if (kvp.Key is string)
                            {
                                WriteBuffer(stream, ((string) kvp.Key).ToUtf8(), true);
                                WriteBuffer(stream, new byte[0], true);
                            }
                            else
                            {
                                var fn = (FieldNameWithType) kvp.Key;
                                WriteBuffer(stream, fn.FieldName.ToUtf8(), true);
                                WriteBuffer(stream, fn.DeclaringType == null ? new byte[0] : fn.DeclaringType.ToUtf8(), true);
                            }
                            break;
                    }
                }
                stream.WriteByte((byte) dataType.End);
            }
        }

        private sealed class endNode : node
        {
            protected override void writeToStreamImpl(Stream stream)
            {
                throw new InvalidOperationException("This should never be called.");
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
                var dt = (dataType) stream.ReadByte();
                if (dt.HasFlag(dataType.HasTypeSpec) && dt.HasFlag(dataType.HasFullTypeSpec))
                    // This happens at EOF too, because ReadByte() returns −1, which converts to 0xff.
                    throw new InvalidOperationException("The binary format is invalid.");

                node node;
                switch (dt & dataType.Mask)
                {
                    case dataType.End: return new endNode();
                    case dataType.Byte: node = new valueNode { Value = (byte) stream.ReadByte() }; break;
                    case dataType.SByte: node = new valueNode { Value = (sbyte) (byte) stream.ReadByte() }; break;
                    case dataType.Short: node = new valueNode { Value = BitConverter.ToInt16(stream.Read(2), 0) }; break;
                    case dataType.UShort: node = new valueNode { Value = BitConverter.ToUInt16(stream.Read(2), 0) }; break;
                    case dataType.Int: node = new valueNode { Value = BitConverter.ToInt32(stream.Read(4), 0) }; break;
                    case dataType.UInt: node = new valueNode { Value = BitConverter.ToUInt32(stream.Read(4), 0) }; break;
                    case dataType.Float: node = new valueNode { Value = BitConverter.ToSingle(stream.Read(4), 0) }; break;
                    case dataType.Long: node = new valueNode { Value = BitConverter.ToInt64(stream.Read(8), 0) }; break;
                    case dataType.ULong: node = new valueNode { Value = BitConverter.ToUInt64(stream.Read(8), 0) }; break;
                    case dataType.Double: node = new valueNode { Value = BitConverter.ToDouble(stream.Read(8), 0) }; break;
                    case dataType.DateTime: node = new valueNode { Value = DateTime.FromBinary(BitConverter.ToInt64(stream.Read(8), 0)) }; break;
                    case dataType.Decimal: node = new valueNode { Value = stream.ReadDecimalOptim() }; break;
                    case dataType.StringUtf8: node = new valueNode { Value = readBuffer(stream, true).FromUtf8() }; break;
                    case dataType.StringUtf16: node = new valueNode { Value = readBuffer(stream, false).FromUtf16() }; break;
                    case dataType.Null: node = new valueNode { Value = null }; break;
                    case dataType.False: node = new valueNode { Value = false }; break;
                    case dataType.True: node = new valueNode { Value = true }; break;
                    case dataType.Ref: node = new valueNode { Value = stream.ReadInt32Optim() }; break;
                    case dataType.Kvp: node = new kvpNode { Key = ReadFromStream(stream), Value = ReadFromStream(stream) }; break;

                    case dataType.List:
                        var list = new List<node>();
                        while (!((node = ReadFromStream(stream)) is endNode))
                            list.Add(node);
                        node = new listNode { List = list };
                        break;

                    case dataType.DictionaryInt:
                    case dataType.DictionaryLong:
                    case dataType.DictionaryULong:
                    case dataType.DictionaryDouble:
                    case dataType.DictionaryDateTime:
                    case dataType.DictionaryStringUtf8:
                    case dataType.DictionaryTwoStringsUtf8:
                        var dict = new Dictionary<object, node>();
                        // Dictionaries encode value first, then key.
                        while (!((node = ReadFromStream(stream)) is endNode))
                        {
                            object key;
                            switch (dt & dataType.Mask)
                            {
                                case dataType.DictionaryInt: key = stream.ReadInt32Optim(); break;
                                case dataType.DictionaryLong: key = stream.ReadInt64Optim(); break;
                                case dataType.DictionaryULong: key = stream.ReadUInt64Optim(); break;
                                case dataType.DictionaryDouble: key = BitConverter.ToDouble(stream.Read(8), 0); break;
                                case dataType.DictionaryDateTime: key = DateTime.FromBinary(BitConverter.ToInt64(stream.Read(8), 0)); break;
                                case dataType.DictionaryStringUtf8: key = readBuffer(stream, true).FromUtf8(); break;
                                case dataType.DictionaryTwoStringsUtf8:
                                    var fieldName = readBuffer(stream, true).FromUtf8();
                                    var declaringType = readBuffer(stream, true).FromUtf8();
                                    key = declaringType.Length > 0 ? new FieldNameWithType(fieldName, declaringType) : (object) fieldName;
                                    break;
                                default: throw new InvalidOperationException("This case should never be reached.");
                            }
                            dict[key] = node;
                        }
                        node = new dictNode { Dictionary = dict };
                        break;

                    default:
                        throw new InvalidOperationException("Invalid binary format.");
                }

                node.DataType = dt & dataType.Mask;
                if (dt.HasFlag(dataType.HasTypeSpec) || (node.TypeSpecIsFull = dt.HasFlag(dataType.HasFullTypeSpec)))
                    node.TypeSpec = readBuffer(stream, true).FromUtf8();
                if (dt.HasFlag(dataType.HasRefId))
                    node.RefId = stream.ReadInt32Optim();
                return node;
            }

            void IClassifyFormat<node>.WriteToStream(node element, Stream stream)
            {
                element.WriteToStream(stream);
            }

            bool IClassifyFormat<node>.IsNull(node element)
            {
                return element.DataType == dataType.Null;
            }

            object IClassifyFormat<node>.GetSimpleValue(node element)
            {
                switch (element.DataType)
                {
                    case dataType.Null:
                    case dataType.False:
                    case dataType.True:
                    case dataType.Byte:
                    case dataType.SByte:
                    case dataType.Short:
                    case dataType.UShort:
                    case dataType.Int:
                    case dataType.UInt:
                    case dataType.Float:
                    case dataType.Long:
                    case dataType.ULong:
                    case dataType.Double:
                    case dataType.DateTime:
                    case dataType.Decimal:
                    case dataType.StringUtf8:
                    case dataType.StringUtf16:
                        return ((valueNode) element).Value;

                    case dataType.List:
                    case dataType.Kvp:
                    case dataType.DictionaryInt:
                    case dataType.DictionaryLong:
                    case dataType.DictionaryULong:
                    case dataType.DictionaryDouble:
                    case dataType.DictionaryDateTime:
                    case dataType.DictionaryStringUtf8:
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
                return list == null ? Enumerable.Empty<node>() : list.List;
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
                return element.DataType == dataType.Ref;
            }

            bool IClassifyFormat<node>.IsReferable(node element)
            {
                return element.RefId.HasValue;
            }

            int IClassifyFormat<node>.GetReferenceID(node element)
            {
                if (element.RefId.HasValue)
                    return element.RefId.Value;
                else if (element.DataType == dataType.Ref)
                    return (int) ((valueNode) element).Value;
                else
                    throw new InvalidOperationException("The binary Classify format encountered a contractual violation perpetrated by Classify. GetReferenceID() should not be called unless IsReference() or IsReferable() returned true.");
            }

            node IClassifyFormat<node>.FormatNullValue()
            {
                return new valueNode { Value = null, DataType = dataType.Null };
            }

            valueNode tryFormatSimpleValue<T>(object value, dataType dt)
            {
                object result;
                if (!ExactConvert.Try(typeof(T), value, out result))
                    return null;
                if (value is string && ExactConvert.ToString(result) != (string) value)
                    return null;
                return new valueNode { DataType = dt, Value = result };
            }

            node IClassifyFormat<node>.FormatSimpleValue(object value)
            {
                if (value == null)
                    return new valueNode { Value = null, DataType = dataType.Null };

                if (value is float)
                    return new valueNode { DataType = dataType.Float, Value = value };
                if (value is double)
                    return new valueNode { DataType = dataType.Double, Value = value };
                if (value is decimal)
                    return new valueNode { DataType = dataType.Decimal, Value = value };
                if (value is DateTime)
                    return new valueNode { DataType = dataType.DateTime, Value = value };

                // Use the smallest possible representation of the input.
                // Note that if the input is, say, the Int64 value 1, it will be stored compactly as the boolean “true”.
                // In fact, even the string "true" will be stored that way and correctly converted back.
                // Strings that roundtrip-convert to DateTime are also stored compactly as DateTime.
                var node =
                    tryFormatSimpleValue<bool>(value, dataType.False) ??
                    tryFormatSimpleValue<byte>(value, dataType.Byte) ??
                    tryFormatSimpleValue<sbyte>(value, dataType.SByte) ??
                    tryFormatSimpleValue<short>(value, dataType.Short) ??
                    tryFormatSimpleValue<ushort>(value, dataType.UShort) ??
                    tryFormatSimpleValue<int>(value, dataType.Int) ??
                    tryFormatSimpleValue<uint>(value, dataType.UInt) ??
                    tryFormatSimpleValue<long>(value, dataType.Long) ??
                    tryFormatSimpleValue<ulong>(value, dataType.ULong) ??
                    tryFormatSimpleValue<DateTime>(value, dataType.DateTime);

                if (node != null)
                {
                    if (node.Value.Equals(true))
                        node.DataType = dataType.True;
                    return node;
                }

                var str = ExactConvert.ToString(value);
                return new valueNode { Value = str, DataType = str.Utf8Length() < str.Utf16Length() ? dataType.StringUtf8 : dataType.StringUtf16 };
            }

            node IClassifyFormat<node>.FormatSelfValue(node value)
            {
                throw new InvalidOperationException("This should never happen.");
            }

            node IClassifyFormat<node>.FormatList(bool isTuple, IEnumerable<node> values)
            {
                return new listNode { List = values.ToList(), DataType = dataType.List };
            }

            node IClassifyFormat<node>.FormatKeyValuePair(node key, node value)
            {
                return new kvpNode { Key = key, Value = value, DataType = dataType.Kvp };
            }

            node IClassifyFormat<node>.FormatDictionary(IEnumerable<KeyValuePair<object, node>> values)
            {
                var dic = values.ToDictionary();
                if (dic.Count == 0)
                    return new dictNode { Dictionary = dic, DataType = dataType.DictionaryStringUtf8 };

                var firstKey = dic.Keys.First();
                var keyType = firstKey.GetType();
                var dt = dataType.DictionaryStringUtf8;
                Type underlyingType = null;
                if (firstKey is Enum)
                    underlyingType = keyType.GetEnumUnderlyingType();

                if (keyType == typeof(long) || underlyingType == typeof(long))
                    dt = dataType.DictionaryLong;
                else if (keyType == typeof(ulong) || underlyingType == typeof(ulong))
                    dt = dataType.DictionaryULong;
                else if (ExactConvert.IsTrueIntegerType(keyType) || ExactConvert.IsTrueIntegerType(underlyingType))
                    dt = dataType.DictionaryInt;
                else if (keyType == typeof(string))
                    dt = dataType.DictionaryStringUtf8;
                else if (keyType == typeof(float) || keyType == typeof(double))
                    dt = dataType.DictionaryDouble;
                else if (keyType == typeof(DateTime))
                    dt = dataType.DictionaryDateTime;

                return new dictNode { Dictionary = dic, DataType = dt };
            }

            node IClassifyFormat<node>.FormatObject(IEnumerable<ObjectFieldInfo<node>> fields)
            {
                var dic = fields.ToDictionary(f => f.DeclaringType == null ? f.FieldName : (object) new FieldNameWithType(f.FieldName, f.DeclaringType), f => f.Value);
                return new dictNode
                {
                    DataType = dic.Keys.Any(k => k is FieldNameWithType) ? dataType.DictionaryTwoStringsUtf8 : dataType.DictionaryStringUtf8,
                    Dictionary = dic
                };
            }

            node IClassifyFormat<node>.FormatReference(int refId)
            {
                return new valueNode { Value = refId, DataType = dataType.Ref };
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