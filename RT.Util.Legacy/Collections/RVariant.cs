using System;
using System.Collections.Generic;
using System.Xml;
using RT.Serialization;
using RT.Util;
using RT.Util.ExtensionMethods;

// TODO: possible areas of improvement
//
// - built-in conversion between Dictionary/List and RVariant
// - .OrNotFound property - to throw a more user friendly exception if the value is a stub
// - add a new Kind, Object, to store arbitrary objects. Serialize this to/from XML. Allow storing nulls.
// - enable "nice" automatic serialization by using special helper attributes?
// - allow xml loading method to be lenient and silently swallow all errors
// - possibly add .AsInt/.AsString/.AsLong and other such methods?
// - add various IList/IDictionary methods
// - track if element/subelement has been used; avoid saving unused ones (must be pretty smart about this and behave as if they're not there at all)

namespace RT.KitchenSink.Collections
{
    /// <summary>
    /// Determines what kind of an RVariant we're dealing with. See documentation
    /// for individual items.
    /// </summary>
    public enum RVariantKind
    {
        /// <summary>
        /// Is used for an RVariant whose actual kind has not been determined yet.
        /// E.g. when accessing a dict element which doesn't exist yet, the newly
        /// created variant will be a Stub. Such variants assume one of the other Kinds
        /// upon being accessed in a certain way, e.g. perfoming string indexing will
        /// turn into a Dict.
        /// </summary>
        Stub,

        /// <summary>
        /// This RVariant stores a char, a string, a DateTime or one of the integer types.
        /// </summary>
        Value,

        /// <summary>
        /// This RVariant is a List, holding other RVariants accessible via an
        /// integer index.
        /// </summary>
        List,

        /// <summary>
        /// This RVariant is a Dict, holding other RVariants accessible via a
        /// string key.
        /// </summary>
        Dict,
    }

    /// <summary>
    /// <para>Enables easy manipulation of simple hierarchical sets of values.
    /// Targets specifically the use of XML for storing application settings.</para>
    ///
    /// <para>Examples:</para>
    ///
    /// <code>
    ///   RVariant v;
    ///   v = 3;                   // now stores the integer "3"
    ///   v = "hi there";          // now stores the string
    ///   v = DateTime.Now();      // stores current time
    ///
    ///   string s = v;            // s holds datetime in ISO, e.g. "2008-03-13 19:20:14.1230000Z"
    ///
    ///   v = new RVariant();      // v can become anything at the moment, known as Stub
    ///   v["form1"]["top"] = 20;  // v becomes a Dict, holding another dict, holding 20.
    ///   v["columns"][4] = "hi";  // v["columns"] holds a list: 0..3 are all Stubs, while
    ///                            // item 4 holds "hi" and has path "columns[4]"
    ///
    ///   v["form2"] = v["form1"]; // deep copying
    ///
    ///   int i = v["form3"]["top"].OrDefaultTo(47);
    ///                            // since form3/top does not exist, returns 47.
    ///                            // but i = v["form3"]["top"]; throws an RVariantConvertException.
    ///
    ///   RVariant.ToXml(v, "settings").Save("settings.xml");
    ///                            // save to an XML file
    /// </code>
    ///
    /// <para>Properties that aid debugging:</para>
    ///
    /// <code>
    ///   v.FullPath  - the path to the given node in its hierarchy, e.g. "form2/top".
    ///   v.Kind      - one of the possible variant kinds: Stub, List, Dict, Value.
    ///   v.Value     - for Value variants, returns the value stored.
    /// </code>
    /// </summary>
    public sealed class RVariant : IEquatable<RVariant>, ICloneable, ICollection<RVariant>
    {
        /// <summary>
        /// Defines which kind of node this RVariant represents: a List, a
        /// Dict or a Value. Can also be Stub if the node kind has not been
        /// determined yet.
        /// </summary>
        public RVariantKind Kind { get { return _kind; } }

        /// <summary>
        /// Defines what kind of a node this object represents. The kind may never be
        /// changed with a single exception: a change from the Stub kind to any other
        /// kind is permitted. Hence the ONLY legal way to set this field internally
        /// is via the <see cref="assumeKind"/> method, and there is no explicit way to
        /// change the Kind from outside of the RVariant.
        /// </summary>
        private RVariantKind _kind = RVariantKind.Stub;

        /// <summary>
        /// Gets the path of this variant. The path is null unless this variant is
        /// a Dict, a List or is contained in a Dict or a List. NOTE: This property
        /// is for debugging purposes only, since it is lossy and cannot reliably
        /// be split back into parts (since arbitrary Dict keys are allowed and are
        /// not escaped before use in this property)
        /// </summary>
        public string FullPath { get { return _path; } }

        /// <summary>
        /// Same as the <see cref="FullPath"/> property except that it never returns a
        /// null. If the path is null returns the string "&lt;null-path&gt;" instead.
        /// Note that, just like the FullPath property, this is only meant for debugging.
        /// </summary>
        public string FullPathNoNull { get { return _path == null ? "<null-path>" : _path; } }

        /// <summary>
        /// Stores the full path to this node. This field is fully maintained by the
        /// class upon all operations which alter it, such as assignments. The path
        /// is null for any RVariant which is not part of a tree. List and Dict are
        /// always considered to be part of a tree.
        /// </summary>
        private string _path = null;

        /// <summary>
        /// Gets the Value stored in this RVariant as an object. The object will
        /// be either a string or a boxed integer type. If the value has never been
        /// assigned an empty string will be returned. Will throw an exception if
        /// executed on a non-Value kind RVariant.
        /// </summary>
        public object Value
        {
            get
            {
                if (_kind != RVariantKind.Value && _kind != RVariantKind.Stub)
                    throw new RVariantNotFoundException(this, RVariantKind.Value);
                assumeKind(RVariantKind.Value);
                return _value;
            }
        }

        /// <summary>
        /// Stores the value represented by those RVariants whose Kind is Value.
        /// RVariant always stores the value as-is, i.e. if assigned an sbyte this
        /// field will hold a boxed sbyte. This field is non-null if and only if the
        /// kind is Value. It defaults to "" whenever a Stub gets automatically
        /// promoted into a Value.
        /// </summary>
        private object _value;

        /// <summary>
        /// Stores a list of RVariants if the Kind is List. This field is non-null
        /// if and only if the Kind is List. The variants stored in this list are
        /// never null - they are at least Stubs.
        /// </summary>
        private List<RVariant> _list;

        /// <summary>
        /// Stores a dict of RVariants if the Kind is Dict. This field is non-null
        /// if and only if the Kind is Dict. The variants stored in this list are
        /// never null - they are at least Stubs.
        /// </summary>
        private Dictionary<string, RVariant> _dict;

        /// <summary>
        /// Creates a Stub RVariant which has not yet assumed any specific behaviour.
        /// Such a node can be turned into a Value, a List or a Dict by performing an
        /// operation on it. E.g. indexing [] with an integer will turn it into a List.
        /// </summary>
        public RVariant()
        {
        }

        /// <summary>
        /// Even though this constructor is private, the user can easily invoke
        /// it via one of the defined implicit casts. This ensures that only the
        /// supported types can be stored.
        /// </summary>
        private RVariant(object value)
        {
            assumeKind(RVariantKind.Value); // safe: new values are always stubs
            _value = value;
        }

        /// <summary>
        /// Creates a RVariant from the specified XmlDocument. Does not tolerate
        /// errors - will throw an exception if the XML is not a valid representation
        /// of a RVariant.
        /// </summary>
        public RVariant(XmlDocument document)
        {
            doFromXml(document.DocumentElement, "");
        }

        /// <summary>
        /// Creates a RVariant from the specified XmlDocument. Does not tolerate
        /// errors - will throw an exception if the XML is not a valid representation
        /// of a RVariant. The name of the root element will be stored in the
        /// "rootNodeName" parameter.
        /// </summary>
        public RVariant(XmlDocument document, out string rootNodeName)
        {
            rootNodeName = document.DocumentElement.Name;
            doFromXml(document.DocumentElement, "");
        }

        /// <summary>
        /// Recreates a RVariant from the specified XmlElement. Can be used to store
        /// several RVariants in a single XmlDocument.
        /// </summary>
        public RVariant(XmlElement element)
        {
            doFromXml(element, "");
        }

        /// <summary>
        /// Returns a string representation of this RVariant. If this RVariant is
        /// of the Value Kind, returns the result of <see cref="ExactConvert.ToString"/>.
        /// Otherwise returns a string showing the Path and the Kind of this value.
        /// </summary>
        public override string ToString()
        {
            if (_kind == RVariantKind.Value)
                return this;
            else
                return string.Format("<{0}: {1}>", _kind, _path == null ? "<null>" : _path);
        }

        /// <summary>
        /// Changes the Kind of this RVariant. Throws an Internal Error exception
        /// if the change is not valid - callers must check themselves and throw
        /// a friendlier exception. Upon changing the kind from a Stub to another
        /// Kind will initialise the fields relevant to that kind.
        /// </summary>
        private void assumeKind(RVariantKind kind)
        {
            if (_kind == kind)
                return;

            if (_kind != RVariantKind.Stub)
                throw new InternalErrorException("RVariant.assumeKind() encountered an internal error.");

            _kind = kind;

            if (_kind == RVariantKind.List)
                _list = new List<RVariant>();
            else if (_kind == RVariantKind.Dict)
                _dict = new Dictionary<string, RVariant>();
            else if (_kind == RVariantKind.Value)
                _value = "";

            if (_kind != RVariantKind.Value && _path == null)
                _path = "";
        }

        #region Copy (Clone)

        /// <summary>
        /// Wipes whatever was in this variant and overwrites with a copy of the supplied
        /// variant. Note that all references to this variant remain valid (which is the
        /// whole point of this method, vs. a Clone() method, which creates a _new_
        /// variant).
        ///
        /// Copying from "null" will throw a NullReferenceException.
        /// </summary>
        private void copyFrom(RVariant source)
        {
            _kind = source._kind;

            // Copy the value. Note that the value can never store anything other than
            // a null, a string or a boxed integer type.
            _value = source._value;

            // Copy the dict, if any
            if (source._dict == null)
                _dict = null;
            else
            {
                _dict = new Dictionary<string, RVariant>();
                foreach (KeyValuePair<string, RVariant> kvp in source._dict)
                {
                    _dict[kvp.Key] = new RVariant();
                    _dict[kvp.Key]._path = _path + "/" + kvp.Key;
                    _dict[kvp.Key].copyFrom(kvp.Value);
                }
            }

            // Copy the list, if any
            if (source._list == null)
                _list = null;
            else
            {
                _list = new List<RVariant>(source._list.Count);
                for (int i = 0; i < source._list.Count; i++)
                {
                    _list.Add(new RVariant());
                    _list[i]._path = _path + "[" + i + "]";
                    _list[i].copyFrom(source._list[i]);
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of this RVariant.
        /// </summary>
        public object Clone()
        {
            RVariant mv = new RVariant();
            mv.copyFrom(this);
            return mv;
        }

        #endregion

        #region Equality

        /// <summary>
        /// Returns a hash code for this RVariant. Creates a deep recursive hash
        /// for Lists but not for Dicts. May be fairly slow - it's probably a bad
        /// idea to use RVariant as a dictionary key...
        /// </summary>
        public override int GetHashCode()
        {
            int code = Kind.GetHashCode() * 0x01010101;
            switch (Kind)
            {
                case RVariantKind.Stub:
                    return code;
                case RVariantKind.Value:
                    return code ^ _value.GetHashCode();
                case RVariantKind.List:
                    code ^= _list.Count.GetHashCode();
                    foreach (RVariant v in _list)
                        code ^= v.GetHashCode();
                    return code;
                case RVariantKind.Dict:
                    code ^= _dict.Count.GetHashCode();
                    // can't really hash the objects in the dict - the enumeration order
                    // is undefined...
                    return code;
                default:
                    throw new Exception("Internal error");
            }
        }

        /// <summary>
        /// Compares this RVariant to another object. If the other object is
        /// a RVariant then performs a deep comparison of the two. Otherwise
        /// compares the Value stored in this RVariant with the specified
        /// object.
        /// </summary>
        public override bool Equals(object other)
        {
            if (other is RVariant)
                return Equals((RVariant) other);

            TypeCode code = ExactConvert.GetTypeCode(other);
            switch (Kind)
            {
                case RVariantKind.Stub:
                    return false;

                case RVariantKind.Value:
                    if (!ExactConvert.IsSupportedType(code))
                        return false;
                    else
                        return ExactConvert.ToString(_value) == ExactConvert.ToString(other);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Performs a deep comparison of this RVariant to another. The result is true
        /// only if the structure and every stored value is identical. Note that Paths are
        /// not taken into account so it is possible to compare two sub-values of different
        /// trees.
        /// </summary>
        public bool Equals(RVariant other)
        {
            if ((object) other == null)
                return false;

            if (this.Kind != other.Kind)
                return false;

            switch (Kind)
            {
                case RVariantKind.Stub:
                    return true;

                case RVariantKind.Value:
                    return ExactConvert.ToString(_value) == ExactConvert.ToString(other._value);

                case RVariantKind.List:
                    if (_list.Count != other._list.Count)
                        return false;
                    for (int i = 0; i < _list.Count; i++)
                        if (!_list[i].Equals(other._list[i]))
                            return false;
                    return true;

                case RVariantKind.Dict:
                    if (_dict.Count != other._dict.Count)
                        return false;
                    foreach (KeyValuePair<string, RVariant> kvp in _dict)
                    {
                        if (!other._dict.ContainsKey(kvp.Key))
                            return false;
                        if (!other._dict[kvp.Key].Equals(kvp.Value))
                            return false;
                    }
                    return true;

                default:
                    throw new Exception("Internal error");
            }
        }

        /// <summary>
        /// Compares RVariant to another object. If the RVariant and the object are not
        /// null, uses <see cref="Equals(object)"/> to do the comparison.
        /// </summary>
        public static bool operator ==(RVariant a, object b)
        {
            if ((object) a == null && (object) b == null)
                return true;
            else if ((object) a == null)
                return false;
            else if ((object) b == null)
                return false;
            else
                return a.Equals(b);
        }

        /// <summary>
        /// The opposite of <see cref="operator=="/>.
        /// </summary>
        public static bool operator !=(RVariant a, object b)
        {
            if ((object) a == null && (object) b == null)
                return false;
            else if ((object) a == null)
                return true;
            else if ((object) b == null)
                return true;
            else
                return !a.Equals(b);
        }

        #endregion

        #region Indexing

        /// <summary>
        /// Called before accessing a Dict element, to ensure that this node is
        /// a Dict kind and to add the element if it doesn't exist yet.
        /// </summary>
        private void accessDictElement(string name)
        {
            // This element is now supposed to be a dict
            if (_kind != RVariantKind.Dict && _kind != RVariantKind.Stub)
                throw new RVariantNotFoundException(this, RVariantKind.Dict);
            assumeKind(RVariantKind.Dict);

            // Create the element if it's not yet available
            if (!_dict.ContainsKey(name))
            {
                _dict[name] = new RVariant();
                _dict[name]._path = _path + "/" + name;
            }
        }

        /// <summary>
        /// Called before accessing a List element, to ensure that this node is
        /// a List kind and to add the element if it doesn't exist yet.
        /// </summary>
        private void accessListElement(int index)
        {
            // This element is now supposed to be a list
            if (_kind != RVariantKind.List && _kind != RVariantKind.Stub)
                throw new RVariantNotFoundException(this, RVariantKind.List);
            assumeKind(RVariantKind.List);

            // Create the element if it's not yet available
            while (_list.Count <= index)
            {
                _list.Add(new RVariant());
                _list[_list.Count - 1]._path = _path + "[" + (_list.Count - 1) + "]";
            }
        }

        /// <summary>
        /// Accesses a sub-element of this node, treating this node as a dictionary.
        /// Note that this will throw an exception if attempted on a node which is
        /// already a List or a Value.
        /// </summary>
        public RVariant this[string name]
        {
            get
            {
                accessDictElement(name);
                return _dict[name];
            }

            set
            {
                // RVariant of kind Value cannot store nulls, full stop.
                // This will be posisble in a future version, when the extra
                // kind Object (Class?) is implemented.
                if (value == null)
                    throw new RVariantException("Cannot store a null value in a Dict.");
                accessDictElement(name);
                _dict[name].copyFrom(value);
            }
        }

        /// <summary>
        /// Accesses a sub-element of this node, treating this node as a list.
        /// Note that this will throw an exception if attempted on a node which is
        /// already a Dict or a Value.
        /// </summary>
        public RVariant this[int index]
        {
            get
            {
                accessListElement(index);
                return _list[index];
            }

            set
            {
                // RVariant of kind Value cannot store nulls, full stop.
                // This will be posisble in a future version, when the extra
                // kind Object (Class?) is implemented.
                if (value == null)
                    throw new RVariantException("Cannot store a null value in a List.");
                accessListElement(index);
                _list[index].copyFrom(value);
            }
        }

        #endregion

        #region Implicit casts

        #region From RVariant

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a bool. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator bool(RVariant value)
        {
            bool r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Boolean);
            else
                throw new RVariantConvertException(value, TypeCode.Boolean);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a byte. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator byte(RVariant value)
        {
            byte r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Byte);
            else
                throw new RVariantConvertException(value, TypeCode.Byte);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to an sbyte. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator sbyte(RVariant value)
        {
            sbyte r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.SByte);
            else
                throw new RVariantConvertException(value, TypeCode.SByte);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a short. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator short(RVariant value)
        {
            short r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Int16);
            else
                throw new RVariantConvertException(value, TypeCode.Int16);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a ushort. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator ushort(RVariant value)
        {
            ushort r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.UInt16);
            else
                throw new RVariantConvertException(value, TypeCode.UInt16);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to an int. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator int(RVariant value)
        {
            int r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Int32);
            else
                throw new RVariantConvertException(value, TypeCode.Int32);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a uint. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator uint(RVariant value)
        {
            uint r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.UInt32);
            else
                throw new RVariantConvertException(value, TypeCode.UInt32);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a long. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator long(RVariant value)
        {
            long r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Int64);
            else
                throw new RVariantConvertException(value, TypeCode.Int64);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a ulong. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator ulong(RVariant value)
        {
            ulong r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.UInt64);
            else
                throw new RVariantConvertException(value, TypeCode.UInt64);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a float. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator float(RVariant value)
        {
            float r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Single);
            else
                throw new RVariantConvertException(value, TypeCode.Single);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a double. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator double(RVariant value)
        {
            double r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Double);
            else
                throw new RVariantConvertException(value, TypeCode.Double);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a decimal. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator decimal(RVariant value)
        {
            decimal r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Decimal);
            else
                throw new RVariantConvertException(value, TypeCode.Decimal);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a DateTime. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator DateTime(RVariant value)
        {
            DateTime r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.DateTime);
            else
                throw new RVariantConvertException(value, TypeCode.DateTime);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a char. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator char(RVariant value)
        {
            char r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.Char);
            else
                throw new RVariantConvertException(value, TypeCode.Char);
        }

        /// <summary>
        /// Provides an implicit conversion of the RVariant to a string. Throws an appropriate
        /// exception if the conversion is not possible.
        /// </summary>
        public static implicit operator string(RVariant value)
        {
            string r;
            if (value._kind == RVariantKind.Value && ExactConvert.Try(value._value, out r))
                return r;
            else if (value._kind != RVariantKind.Value)
                throw new RVariantNotFoundException(value, TypeCode.String);
            else
                throw new RVariantConvertException(value, TypeCode.String);
        }

        #endregion

        #region To RVariant

        /// <summary>Provides an implicit conversion of a bool to RVariant.</summary>
        public static implicit operator RVariant(bool value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a byte to RVariant.</summary>
        public static implicit operator RVariant(byte value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a sbyte to RVariant.</summary>
        public static implicit operator RVariant(sbyte value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a short to RVariant.</summary>
        public static implicit operator RVariant(short value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a ushort to RVariant.</summary>
        public static implicit operator RVariant(ushort value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of an int to RVariant.</summary>
        public static implicit operator RVariant(int value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a uint to RVariant.</summary>
        public static implicit operator RVariant(uint value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a long to RVariant.</summary>
        public static implicit operator RVariant(long value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a ulong to RVariant.</summary>
        public static implicit operator RVariant(ulong value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a float to RVariant.</summary>
        public static implicit operator RVariant(float value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a double to RVariant.</summary>
        public static implicit operator RVariant(double value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a decimal to RVariant.</summary>
        public static implicit operator RVariant(decimal value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a DateTime to RVariant.</summary>
        public static implicit operator RVariant(DateTime value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a char to RVariant.</summary>
        public static implicit operator RVariant(char value)
        {
            return new RVariant((object) value);
        }

        /// <summary>Provides an implicit conversion of a string to RVariant.</summary>
        public static implicit operator RVariant(string value)
        {
            return new RVariant((object) value);
        }

        #endregion

        #endregion

        #region OrNotFound / OrDefaultTo

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a bool
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public bool OrDefaultTo(bool defaultValue)
        {
            bool result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a byte
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public byte OrDefaultTo(byte defaultValue)
        {
            byte result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to an sbyte
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public sbyte OrDefaultTo(sbyte defaultValue)
        {
            sbyte result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a short
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public short OrDefaultTo(short defaultValue)
        {
            short result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a ushort
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public ushort OrDefaultTo(ushort defaultValue)
        {
            ushort result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to an int
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public int OrDefaultTo(int defaultValue)
        {
            int result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a uint
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public uint OrDefaultTo(uint defaultValue)
        {
            uint result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a long
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public long OrDefaultTo(long defaultValue)
        {
            long result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a ulong
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public ulong OrDefaultTo(ulong defaultValue)
        {
            ulong result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a float
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public float OrDefaultTo(float defaultValue)
        {
            float result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a double
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public double OrDefaultTo(double defaultValue)
        {
            double result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a decimal
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public decimal OrDefaultTo(decimal defaultValue)
        {
            decimal result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a DateTime
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public DateTime OrDefaultTo(DateTime defaultValue)
        {
            DateTime result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a char
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public char OrDefaultTo(char defaultValue)
        {
            char result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        /// <summary>
        /// Treating this node as a Value, returns the value converted to a string
        /// or the default value specified by defaultValue if the conversion is not possible. Also
        /// returns the default value if this node is not of a Value type.
        /// </summary>
        public string OrDefaultTo(string defaultValue)
        {
            string result;
            if (_kind == RVariantKind.Value && ExactConvert.Try(_value, out result))
                return result;
            else
                return defaultValue;
        }

        #endregion

        /// <summary>
        /// Returns true if and only if this value is not a stub. This enables code
        /// like "settings["stuff"]["value"].Exists", which will return true only if
        /// the variant /stuff/value is defined.
        /// </summary>
        public bool Exists
        {
            get
            {
                return _kind != RVariantKind.Stub;
            }
        }

        #region XML

        /// <summary>
        /// Converts the specified RVariant to an XmlDocument. The name of the
        /// root element in the XmlDocument must be specified.
        /// </summary>
        public XmlDocument ToXml(string rootElementName)
        {
            XmlDocument document = new XmlDocument();
            XmlElement root = document.CreateElement(rootElementName);
            document.AppendChild(root);
            doToXml(root, this);
            return document;
        }

        /// <summary>
        /// Converts the specified RVariant to XML, storing it in the specified
        /// XmlElement. The name of the destination XmlElement will not be altered.
        /// </summary>
        public void ToXml(XmlElement rootElement)
        {
            doToXml(rootElement, this);
        }

        private static void doToXml(XmlElement element, RVariant value)
        {
            switch (value.Kind)
            {
                case RVariantKind.Value:
                    element.SetAttribute("value", ExactConvert.ToString(value._value));
                    break;

                case RVariantKind.Dict:
                    element.SetAttribute("kind", "dict");
                    foreach (KeyValuePair<string, RVariant> kvp in value._dict)
                    {
                        XmlElement subEl = element.OwnerDocument.CreateElement(kvp.Key);
                        element.AppendChild(subEl);
                        doToXml(subEl, kvp.Value);
                    }
                    break;

                case RVariantKind.List:
                    element.SetAttribute("kind", "list");
                    foreach (RVariant subVal in value._list)
                    {
                        XmlElement subEl = element.OwnerDocument.CreateElement("item");
                        element.AppendChild(subEl);
                        doToXml(subEl, subVal);
                    }
                    break;

                case RVariantKind.Stub:
                    // don't skip these - otherwise roundtrip doesn't really work
                    element.SetAttribute("kind", "stub");
                    break;

                default:
                    throw new Exception("Internal error");
            }
        }

        private void doFromXml(XmlElement element, string path)
        {
            Clear();
            _path = path;

            if (!element.HasAttribute("kind"))
            {
                // Value element
                _kind = RVariantKind.Value;
                if (element.HasAttribute("value"))
                    _value = element.GetAttribute("value");
                else
                    _value = null;
            }
            else
            {
                string kind = element.GetAttribute("kind");
                if (kind == "stub")
                {
                    _kind = RVariantKind.Stub;
                }
                else if (kind == "dict")
                {
                    // Dict element
                    _kind = RVariantKind.Dict;
                    _dict = new Dictionary<string, RVariant>(element.ChildNodes.Count);
                    foreach (XmlNode subNode in element.ChildNodes)
                    {
                        if (subNode is XmlElement)
                        {
                            if (_dict.ContainsKey(subNode.Name))
                                throw new RVariantXmlException("XML node \"{0}{1}\", which is of kind \"dict\", has more than one element with name \"{2}\"".Fmt(
                                    element.OwnerDocument.DocumentElement.Name, path, subNode.Name));
                            else
                            {
                                RVariant newEl = new RVariant();
                                newEl.doFromXml((XmlElement) subNode, path + "/" + subNode.Name);
                                _dict.Add(subNode.Name, newEl);
                            }
                        }
                    }
                }
                else if (kind == "list")
                {
                    // List element
                    _kind = RVariantKind.List;
                    XmlNodeList children = element.ChildNodes;
                    _list = new List<RVariant>(children.Count);
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i] is XmlElement)
                        {
                            if (children[i].Name == "item")
                            {
                                RVariant newEl = new RVariant();
                                newEl.doFromXml((XmlElement) children[i], path + "[" + i + "]");
                                _list.Add(newEl);
                            }
                            else
                                throw new RVariantXmlException("XML node \"{0}{1}\", which is of kind \"list\", has an element with an unexpected name: \"{1}\"".Fmt(
                                    element.OwnerDocument.DocumentElement.Name, path, children[i].Name));
                        }
                    }
                }
                else
                {
                    throw new RVariantXmlException("XML node \"{0}{1}\" has an unrecognized \"kind\" attribute: \"{2}\"".Fmt(element.OwnerDocument.DocumentElement.Name, path, kind));
                }
            }
        }

        /// <summary>
        /// Loads an XML file into this RVariant, deleting all existing values and adding new ones as appropriate.
        /// </summary>
        public string LoadFromXmlFile(string fileName)
        {
            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
            xml.Load(fileName);
            doFromXml(xml.DocumentElement, "");
            return xml.DocumentElement.Name;
        }

        /// <summary>
        /// Saves this RVariant to an XML file.
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="rootElementName">The name of the root element</param>
        public void SaveToXmlFile(string fileName, string rootElementName)
        {
            ToXml(rootElementName).Save(fileName);
        }

        #endregion

        #region IList members (very incomplete)

        /// <summary>
        /// Gets the number of items stored in this variant.
        /// Returns 0 for Stubs and 1 for Values.
        /// </summary>
        public int Count
        {
            get
            {
                switch (_kind)
                {
                    case RVariantKind.Stub:
                        return 0;
                    case RVariantKind.Value:
                        return 1;
                    case RVariantKind.List:
                        return _list.Count;
                    case RVariantKind.Dict:
                        return _dict.Count;
                    default:
                        throw new Exception("Internal error");
                }
            }
        }

        #endregion

        #region ICollection<RVariant> members

        /// <summary>
        /// Verifies that the value is a List; throws an exception if not. To be used
        /// by ICollection implementation only.
        /// </summary>
        private void ICollection_AssumeListKind()
        {
            if (_kind != RVariantKind.Stub && _kind != RVariantKind.List)
                throw new RVariantException("Location \"{0}\": cannot access as a list because item is already a {1}".Fmt(FullPathNoNull, _kind));
            assumeKind(RVariantKind.List);
        }

        /// <summary>
        /// Makes this RVariant a stub, with no values associated. This is the only legal
        /// way to revert the <see cref="Kind"/> from non-Stub to Stub.
        /// </summary>
        public void Clear()
        {
            _kind = RVariantKind.Stub;
            _value = null;
            _list = null;
            _dict = null;
        }

        /// <summary>
        /// Adds an RVariant to this variant, assuming/making it a List.
        /// </summary>
        public void Add(RVariant item)
        {
            ICollection_AssumeListKind();
            _list.Add(item);
        }

        /// <summary>
        /// Not currently implemented.
        /// </summary>
        public bool Remove(RVariant item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not currently implemented.
        /// </summary>
        public bool Contains(RVariant item)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Iterates over the items in this variant, assuming/making it a List.
        /// </summary>
        public IEnumerator<RVariant> GetEnumerator()
        {
            ICollection_AssumeListKind();
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Iterates over the items in this variant, assuming/making it a List.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            ICollection_AssumeListKind();
            return _list.GetEnumerator();
        }

        /// <summary>
        /// Always returns false since RVariants are never read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Not currently implemented.
        /// </summary>
        public void CopyTo(RVariant[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// All RVariant exceptions derive from this one.
    /// </summary>
    public class RVariantException : Exception
    {
        /// <summary>
        /// Should only be used by constructors which initialise the error message in
        /// the constructor body.
        /// </summary>
        protected RVariantException()
        {
        }

        /// <summary>Creates an exception instance with the specified message.</summary>
        public RVariantException(string message) :
            base(message)
        {
        }
    }

    /// <summary>
    /// Indicates that there was an error while attempting to convert a Value
    /// kind variant from one type to another. E.g. converting "hi" to int.
    /// </summary>
    public sealed class RVariantConvertException : RVariantException
    {
        /// <summary>Creates a conversion error exception with the appropriate message.</summary>
        public RVariantConvertException(RVariant variant, TypeCode desiredType)
            : base(string.Format("Location \"{0}\": value \"{1}\" cannot be converted to type \"{2}\"",
                variant.FullPathNoNull, variant.Value, desiredType))
        {
        }
    }

    /// <summary>
    /// Indicates that a variant was not found where expected. This is only thrown
    /// from the .OrNotFound series of methods.
    /// </summary>
    public sealed class RVariantNotFoundException : RVariantException
    {
        /// <summary>Creates a value-not-found exception with the appropriate message.</summary>
        public RVariantNotFoundException(RVariant variant, TypeCode desiredType)
            : base(string.Format("Location \"{0}\": expected a Value convertible to \"{1}\"",
                    variant.FullPathNoNull, desiredType))
        {
        }

        /// <summary>Creates a value-not-found exception with the appropriate message.</summary>
        public RVariantNotFoundException(RVariant variant, RVariantKind desiredKind)
            : base(string.Format("Location \"{0}\": expected a variant of kind {1}",
                variant.FullPathNoNull, desiredKind))
        {
        }
    }

    /// <summary>
    /// Indicates that RVariant encountered an error while converting XML to variant.
    /// </summary>
    public sealed class RVariantXmlException : RVariantException
    {
        /// <summary>Creates an exception instance with the specified message.</summary>
        public RVariantXmlException(string message) :
            base(message)
        {
        }
    }
}
