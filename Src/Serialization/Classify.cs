using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using RT.Util.ExtensionMethods;

/*
 * Provide a proper way to distinguish exceptions due to the caller breaking some contract from exceptions due to data load failures. Always pass through the former.
 * Can the Follow attribute be implemented separately using ClassifyOptions?
 * Built-in versioning support (e.g. in XML, using attribute like ver="1"), an IClassifyVersioned { Version { get } }, and passing it to IClassify[Object/Type]Processor<TElement>
 */

namespace RT.Util.Serialization
{
    /// <summary>
    ///     Provides static methods to represent objects of (almost) arbitrary classes in various formats (such as XML or
    ///     JSON) and to restore such objects again. See the remarks section for features and limitations.</summary>
    /// <remarks>
    ///     <para>
    ///         By default, when serializing a custom class, Classify persists the value of all instance fields, including
    ///         private, inherited and compiler-generated ones. It does not persist static members or the result of property
    ///         getters. Each field is persisted under a name that is the field’s name minus any leading underscores.
    ///         Compiler-generated fields for automatically-implemented properties are instead named after the
    ///         automatically-implemented property’s name minus any leading underscores.</para>
    ///     <para>
    ///         Classify can also generate representations of basic types such as <c>string</c>, <c>int</c>, <c>bool</c>, etc.</para>
    ///     <para>
    ///         Features:</para>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Classify fully supports all the built-in types which are keywords in C# except <c>object</c> and
    ///             <c>dynamic</c>. It also supports <c>DateTime</c> and all enum types.</description></item>
    ///         <item><description>
    ///             Classify fully supports classes and structs that contain only fields of the above types as well as fields
    ///             whose type is itself such a class or struct.</description></item>
    ///         <item><description>
    ///             Classify has special handling for classes that implement <c>IDictionary&lt;TKey, TValue&gt;</c>, where
    ///             <c>TValue</c> must be a type also supported by Classify. <c>TKey</c> must be <c>string</c>, an integer
    ///             type or an enum type. If the field is of a concrete type, that type is maintained, but its extra fields
    ///             are not persisted. If the field is of the interface type <c>IDictionary&lt;TKey, TValue&gt;</c> itself,
    ///             the type <c>Dictionary&lt;TKey, TValue&gt;</c> is used to reconstruct the object.</description></item>
    ///         <item><description>
    ///             Classify has special handling for classes that implement <c>ICollection&lt;T&gt;</c>, where <c>T</c> must
    ///             be a type also supported by Classify. If the field is of a concrete type, that type is maintained, but its
    ///             extra fields are not persisted. If the field is of the interface type <c>ICollection&lt;T&gt;</c> or
    ///             <c>IList&lt;T&gt;</c> itself, the type <c>List&lt;T&gt;</c> is used to reconstruct the object. If the type
    ///             also implements <c>IDictionary&lt;TKey, TValue&gt;</c>, the special handling for that takes precedence.</description></item>
    ///         <item><description>
    ///             Classify also supports <see cref="KeyValuePair{TKey,TValue}"/> and all the different
    ///             <c>System.Tuple&lt;...&gt;</c> types.</description></item>
    ///         <item><description>
    ///             Classify handles the element type specially. For example, if you are classifying to XML, classifying an
    ///             <see cref="System.Xml.Linq.XElement"/> object generates the XML directly; if you are classifying to JSON,
    ///             the same goes for <see cref="RT.Util.Json.JsonValue"/> objects, etc.</description></item>
    ///         <item><description>
    ///             For classes that don’t implement any of the above-mentioned collection interfaces, Classify supports
    ///             polymorphism. The actual type of an instance is persisted if it is different from the declared type.</description></item>
    ///         <item><description>
    ///             Classify supports auto-implemented properties. It uses the name of the property rather than the hidden
    ///             auto-generated field, although the field’s value is persisted. All other properties are ignored.</description></item>
    ///         <item><description>
    ///             Classify ignores the order of input elements (except when handling collections and dictionaries). For
    ///             example, XML tags or JSON dictionary keys are mapped to fields by their names; their order is considered
    ///             immaterial.</description></item>
    ///         <item><description>
    ///             Classify silently discards unrecognized XML tags/JSON dictionary keys instead of throwing errors. This is
    ///             by design because it enables the programmer to remove a field from a class without invalidating objects
    ///             previously persisted.</description></item>
    ///         <item><description>
    ///             Classify silently ignores missing elements. A field whose element is missing retains the value assigned to
    ///             it by the parameterless constructor. This is by design because it enables the programmer to add a new
    ///             field to a class (and to specify a default initialization value for it) without invalidating objects
    ///             previously persisted.</description></item>
    ///         <item><description>
    ///             The following custom attributes can be used to alter Classify’s behavior. See the custom attribute class’s
    ///             documentation for more information: <see cref="ClassifyFollowIdAttribute"/>, <see
    ///             cref="ClassifyIdAttribute"/>, <see cref="ClassifyIgnoreAttribute"/>, <see
    ///             cref="ClassifyIgnoreIfAttribute"/>, <see cref="ClassifyIgnoreIfDefaultAttribute"/>, <see
    ///             cref="ClassifyIgnoreIfEmptyAttribute"/>, <see cref="ClassifyParentAttribute"/>. Any attribute that can be
    ///             used on a field, can equally well be used on an auto-implemented property, but not on any other
    ///             properties.</description></item>
    ///         <item><description>
    ///             Classify maintains object identity and correctly handles cycles in the object graph. Only <c>string</c>s
    ///             are exempt from this.</description></item>
    ///         <item><description>
    ///             Classify can make use of type substitutions. See <see cref="IClassifySubstitute{TTrue,TSubstitute}"/> for
    ///             more information.</description></item>
    ///         <item><description>
    ///             Classify allows you to pre-/post-process the serialized form and/or the serialized objects. See <see
    ///             cref="IClassifyObjectProcessor"/>, <see cref="IClassifyObjectProcessor{TElement}"/>, <see
    ///             cref="IClassifyTypeProcessor"/> and <see cref="IClassifyTypeProcessor{TElement}"/> for more information.</description></item></list>
    ///     <para>
    ///         Limitations:</para>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Classify requires that every type involved have a parameterless constructor, although it need not be
    ///             public. This parameterless constructor is executed with all its side-effects before each object is
    ///             reconstructed. An exception is made when a field in an object already has a non-null instance assigned to
    ///             it by the constructor; in such cases, the object is reused.</description></item>
    ///         <item><description>
    ///             If a field is of type <c>ICollection&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>, <c>IDictionary&lt;TKey,
    ///             TValue&gt;</c>, or any class that implements either of these, polymorphism is not supported, and nor is
    ///             any information stored in those classes. In particular, this means that the comparer used by a
    ///             <c>SortedDictionary&lt;TKey, TValue&gt;</c> is not persisted. However, if the containing class’s
    ///             constructor assigned a <c>SortedDictionary&lt;TKey, TValue&gt;</c> with a comparer, that instance, and
    ///             hence its comparer, is reused.</description></item></list></remarks>
    public static class Classify
    {
        /// <summary>
        ///     Options used when null is passed to methods that take options. Make sure not to modify this instance if any
        ///     thread in the application might be in the middle of using <see cref="Classify"/>; ideally the options shoud be
        ///     set once during startup and never changed after that.</summary>
        public static ClassifyOptions DefaultOptions = new ClassifyOptions();

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified file.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of object to read.</typeparam>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="parent">
        ///     If the class to be declassified has a field with the <see cref="ClassifyParentAttribute"/>, that field will
        ///     receive this object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T DeserializeFile<TElement, T>(string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null, object parent = null)
        {
            return (T) DeserializeFile<TElement>(typeof(T), filename, format, options, parent);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified file.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="type">
        ///     Type of object to read.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="parent">
        ///     If the class to be declassified has a field with the <see cref="ClassifyParentAttribute"/>, that field will
        ///     receive this object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object DeserializeFile<TElement>(Type type, string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null, object parent = null)
        {
            string defaultBaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            TElement elem;
            using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                elem = format.ReadFromStream(f);
            return new classifier<TElement>(format, options, defaultBaseDir).Deserialize(type, elem, parent);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of object to reconstruct.</typeparam>
        /// <param name="elem">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T Deserialize<TElement, T>(TElement elem, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return (T) new classifier<TElement>(format, options).Deserialize(typeof(T), elem, null);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="type">
        ///     Type of object to reconstruct.</param>
        /// <param name="elem">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object Deserialize<TElement>(Type type, TElement elem, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return new classifier<TElement>(format, options).Deserialize(type, elem, null);
        }

        /// <summary>
        ///     Reconstructs an object of the specified type from the specified serialized form by applying the values to an
        ///     existing instance of the type.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of object to reconstruct.</typeparam>
        /// <param name="element">
        ///     Serialized form to reconstruct object from.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void DeserializeIntoObject<TElement, T>(TElement element, T intoObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            new classifier<TElement>(format, options).DeserializeIntoObject(typeof(T), element, intoObject, null);
        }

        /// <summary>
        ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired
        ///     type. The type of object is inferred from the object passed in.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object. Also determines the type of object
        ///     expected.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void DeserializeFileIntoObject<TElement>(string filename, object intoObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            TElement elem;
            using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                elem = format.ReadFromStream(f);
            new classifier<TElement>(format, options).DeserializeIntoObject(intoObject.GetType(), elem, intoObject, null);
        }

        /// <summary>
        ///     Stores the specified object in a file with the given path and filename.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of the object to store.</typeparam>
        /// <param name="saveObject">
        ///     Object to store in a file.</param>
        /// <param name="filename">
        ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void SerializeToFile<TElement, T>(T saveObject, string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            SerializeToFile(typeof(T), saveObject, filename, format, options);
        }

        /// <summary>
        ///     Stores the specified object in a file with the given path and filename.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="saveType">
        ///     Type of the object to store.</param>
        /// <param name="saveObject">
        ///     Object to store in a file.</param>
        /// <param name="filename">
        ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void SerializeToFile<TElement>(Type saveType, object saveObject, string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            string defaultBaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            var element = new classifier<TElement>(format, options, defaultBaseDir).Serialize(saveObject, saveType)();
            PathUtil.CreatePathToFile(filename);
            using (var f = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read))
                format.WriteToStream(element, f);
        }

        /// <summary>
        ///     Converts the specified object into a serialized form.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of object to convert.</typeparam>
        /// <param name="saveObject">
        ///     Object to be serialized.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     The serialized form generated from the object.</returns>
        public static TElement Serialize<TElement, T>(T saveObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return new classifier<TElement>(format, options).Serialize(saveObject, typeof(T))();
        }

        /// <summary>
        ///     Converts the specified object into a serialized form.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="saveType">
        ///     Type of object to convert.</param>
        /// <param name="saveObject">
        ///     Object to be serialized.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyXmlFormat"/> for an example.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     The serialized form generated from the object.</returns>
        public static TElement Serialize<TElement>(Type saveType, object saveObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return new classifier<TElement>(format, options).Serialize(saveObject, saveType)();
        }

        // NOTE: If you change this list, also change the XML comment on IClassifyFormat<TElement>.GetSimpleValue and IClassifyFormat<TElement>.FormatSimpleValue
        // This list determines both which types are (de-)classified using GetSimpleValue/FormatSimpleValue, and which types of dictionary keys use GetDictionary/FormatDictionary (all others use GetList/FormatList with GetKeyValuePair/FormatKeyValuePair)
        // All enum types are also treated as if they were listed here.
        private static Type[] _simpleTypes = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(decimal), typeof(float), typeof(double), typeof(bool), typeof(char), typeof(string), typeof(DateTime) };

        private sealed class classifier<TElement>
        {
            private ClassifyOptions _options;
            private int _nextId = 0;
            private List<Action> _doAtTheEnd;
            private string _baseDir;
            private IClassifyFormat<TElement> _format;

            public classifier(IClassifyFormat<TElement> format, ClassifyOptions options, string defaultBaseDir = null)
            {
                if (format == null)
                    throw new ArgumentNullException("format");

                _options = options ?? DefaultOptions ?? new ClassifyOptions(); // in case someone set default options to null
                _format = format;
                _baseDir = _options.BaseDir ?? defaultBaseDir;
            }

            private sealed class declassifyRememberedObject
            {
                public Func<object> WithDesubstitution;
                public Func<object> WithoutDesubstitution;
            }

            private Dictionary<int, declassifyRememberedObject> _rememberD
            {
                get
                {
                    if (_rememberCacheD == null)
                        _rememberCacheD = new Dictionary<int, declassifyRememberedObject>();
                    return _rememberCacheD;
                }
            }
            private Dictionary<int, declassifyRememberedObject> _rememberCacheD;

            private HashSet<object> _rememberC
            {
                get
                {
                    if (_rememberCacheC == null)
                        _rememberCacheC = new HashSet<object>(new CustomEqualityComparer<object>(object.ReferenceEquals, o => o.GetHashCode()));
                    return _rememberCacheC;
                }
            }
            private HashSet<object> _rememberCacheC;

            private Dictionary<object, int> _requireRefId
            {
                get
                {
                    if (_requireRefIdCache == null)
                        _requireRefIdCache = new Dictionary<object, int>(new CustomEqualityComparer<object>(object.ReferenceEquals, o => o.GetHashCode()));
                    return _requireRefIdCache;
                }
            }
            private Dictionary<object, int> _requireRefIdCache;

            private static Type[] _tupleTypes = new[] { typeof(KeyValuePair<,>), typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>) };

            private static bool isIntegerType(Type t)
            {
                return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte);
            }

            public object Deserialize(Type type, TElement elem, object parentNode)
            {
                _doAtTheEnd = new List<Action>();
                var resultFunc = CustomCallStack.Run(deserialize(type, elem, null, parentNode, _options.EnforceEnums));
                foreach (var action in _doAtTheEnd)
                    action();
                var result = resultFunc();
                if (type.IsEnum && _options.EnforceEnums && !allowEnumValue(type, result))
                    return type.GetDefaultValue();
                return result;
            }

            // Function to make sure a declassified object is only generated once
            private Func<object> cachify(Func<object> res, TElement elem, ClassifyTypeOptions typeOptions)
            {
                var retrieved = false;
                object retrievedObj = null;
                return () =>
                {
                    if (!retrieved)
                    {
                        retrieved = true;
                        retrievedObj = res();
                        retrievedObj.IfType((IClassifyObjectProcessor obj) => { obj.AfterDeserialize(); });
                        retrievedObj.IfType((IClassifyObjectProcessor<TElement> obj) => { obj.AfterDeserialize(elem); });
                        typeOptions.IfType((IClassifyTypeProcessor opt) => { opt.AfterDeserialize(retrievedObj); });
                        typeOptions.IfType((IClassifyTypeProcessor<TElement> opt) => { opt.AfterDeserialize(retrievedObj, elem); });
                    }
                    return retrievedObj;
                };
            }

            public void DeserializeIntoObject(Type type, TElement elem, object intoObj, object parentNode)
            {
                ClassifyTypeOptions typeOptions = null;
                if (_options._typeOptions.TryGetValue(type, out typeOptions))
                {
                    if (typeOptions._substituteType != null)
                        throw new InvalidOperationException("Cannot use type substitution when populating a provided object.");
                    typeOptions.IfType((IClassifyTypeProcessor<TElement> opt) => { opt.BeforeDeserialize(elem); });
                }

                _doAtTheEnd = new List<Action>();

                if (_format.IsReferable(elem))
                    _rememberD[_format.GetReferenceID(elem)] = new declassifyRememberedObject { WithoutDesubstitution = () => intoObj };

                var result = CustomCallStack.Run(deserializeIntoObject(elem, intoObj, type, parentNode));
                foreach (var action in _doAtTheEnd)
                    action();
                result();

                intoObj.IfType((IClassifyObjectProcessor obj) => { obj.AfterDeserialize(); });
                intoObj.IfType((IClassifyObjectProcessor<TElement> obj) => { obj.AfterDeserialize(elem); });
                typeOptions.IfType((IClassifyTypeProcessor opt) => { opt.AfterDeserialize(intoObj); });
                typeOptions.IfType((IClassifyTypeProcessor<TElement> opt) => { opt.AfterDeserialize(intoObj, elem); });
            }

            /// <summary>
            ///     Deserializes an object from its serialized form.</summary>
            /// <param name="declaredType">
            ///     The type to deserialize to.</param>
            /// <param name="elem">
            ///     The serialized form.</param>
            /// <param name="already">
            ///     An object that we may potentially re-use (e.g. the object already stored in the field when deserializing a
            ///     field inside an outer object).</param>
            /// <param name="parentNode">
            ///     The value for a [ClassifyParent] field.</param>
            /// <param name="enforceEnums">
            ///     <c>true</c> if [ClassifyEnforceEnums] semantics are in effect for this object.</param>
            private WorkNode<Func<object>> deserialize(Type declaredType, TElement elem, object already, object parentNode, bool enforceEnums)
            {
                if (declaredType.IsPointer || declaredType.IsByRef)
                    throw new NotSupportedException("Classify cannot deserialize pointers or by-reference variables.");

                var substType = declaredType;

                ClassifyTypeOptions typeOptions = null;
                if (_options._typeOptions.TryGetValue(declaredType, out typeOptions))
                {
                    if (typeOptions._substituteType != null)
                        substType = typeOptions._substituteType;
                    typeOptions.IfType((IClassifyTypeProcessor<TElement> opt) => { opt.BeforeDeserialize(elem); });
                }

                // Every object created by declassify goes through this function
                var cleanUp = Ut.Lambda((Func<object> res) =>
                {
                    var withoutDesubstitution = cachify(res, elem, typeOptions);
                    Func<object> withDesubstitution = null;

                    // Apply de-substitution (if any)
                    if (declaredType != substType)
                        withDesubstitution = cachify(() => typeOptions._fromSubstitute(withoutDesubstitution()), elem, typeOptions);

                    // Remember the result if something else refers to it
                    if (_format.IsReferable(elem))
                        _rememberD[_format.GetReferenceID(elem)] = new declassifyRememberedObject
                        {
                            WithDesubstitution = withDesubstitution,
                            WithoutDesubstitution = withoutDesubstitution
                        };

                    return declaredType != substType ? withDesubstitution : withoutDesubstitution;
                });

                if (_format.IsReference(elem))
                {
                    var refID = _format.GetReferenceID(elem);
                    return _ => new Func<object>(() =>
                    {
                        declassifyRememberedObject inf;
                        if (!_rememberD.TryGetValue(refID, out inf))
                            _format.ThrowMissingReferable(refID);
                        if (declaredType == substType)
                            return inf.WithoutDesubstitution();
                        if (inf.WithDesubstitution == null)
                            inf.WithDesubstitution = cachify(() => typeOptions._fromSubstitute(inf.WithoutDesubstitution()), elem, typeOptions);
                        return inf.WithDesubstitution();
                    });
                }

                var serializedType = substType;
                bool isFullType;
                var typeName = _format.GetType(elem, out isFullType);
                if (typeName != null)
                {
                    serializedType = isFullType
                        ? Type.GetType(typeName)
                        : Type.GetType(typeName) ?? Type.GetType((substType.Namespace == null ? null : substType.Namespace + ".") + typeName)
                            ?? declaredType.Assembly.GetType(typeName) ?? declaredType.Assembly.GetType((substType.Namespace == null ? null : substType.Namespace + ".") + typeName)
                            ?? substType.Assembly.GetType(typeName) ?? substType.Assembly.GetType((substType.Namespace == null ? null : substType.Namespace + ".") + typeName);
                    if (serializedType == null)
                        throw new Exception("The type {0} needed for deserialization cannot be found.".Fmt(typeName));
                }
                var genericDefinition = serializedType.IsGenericType ? serializedType.GetGenericTypeDefinition() : null;

                if (_format.IsNull(elem))
                    return _ => cleanUp(() => null);
                else if (typeof(TElement).IsAssignableFrom(serializedType))
                    return _ => cleanUp(() => _format.GetSelfValue(elem));
                else if (_simpleTypes.Contains(serializedType) || ExactConvert.IsSupportedType(serializedType))
                    return _ => cleanUp(() => ExactConvert.To(serializedType, _format.GetSimpleValue(elem)));
                else if (serializedType.IsGenericType && serializedType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // It’s a nullable type, just determine the inner type and start again
                    return deserialize(serializedType.GetGenericArguments()[0], elem, already, parentNode, enforceEnums);
                }
                else if (genericDefinition != null && _tupleTypes.Contains(serializedType.GetGenericTypeDefinition()))
                {
                    // It’s a Tuple or KeyValuePair
                    var genericArguments = serializedType.GetGenericArguments();
                    var tupleParams = new Func<object>[genericArguments.Length];
                    TElement[] values;
                    if (genericDefinition == typeof(KeyValuePair<,>))
                    {
                        TElement key, value;
                        _format.GetKeyValuePair(elem, out key, out value);
                        values = new[] { key, value };
                    }
                    else
                        values = _format.GetList(elem, genericArguments.Length).ToArray();

                    if (genericArguments.Length > values.Length)
                        throw new InvalidOperationException("While trying to deserialize a tuple with {0} elements, Classify encountered a serialized form with only {1} elements.".Fmt(genericArguments.Length, values.Length));

                    int i = -1;
                    return prevResult =>
                    {
                        if (i >= 0)
                            tupleParams[i] = prevResult;
                        i++;
                        if (i < genericArguments.Length && i < values.Length)
                            return deserialize(genericArguments[i], values[i], null, parentNode, enforceEnums);
                        var constructor = serializedType.GetConstructor(genericArguments);
                        if (constructor == null)
                            throw new InvalidOperationException("Could not find expected Tuple constructor.");
                        return cleanUp(() => constructor.Invoke(tupleParams.Select(act => act()).ToArray()));
                    };
                }
                else
                {
                    // Check if it’s an array, collection or dictionary
                    Type[] typeParameters;
                    Type keyType = null, valueType = null;
                    if (serializedType.IsArray)
                        valueType = serializedType.GetElementType();
                    else if (serializedType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters) && (typeParameters[0].IsEnum || _simpleTypes.Contains(typeParameters[0])))
                    {
                        // Dictionaries which are stored specially (key is a simple type).
                        // (More complex dictionaries are classified by treating them as an ICollection<KeyValuePair<K,V>>)
                        keyType = typeParameters[0];
                        valueType = typeParameters[1];
                    }
                    else if (serializedType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                        valueType = typeParameters[0];

                    if (valueType != null)
                    {
                        // It’s a collection or dictionary.

                        if (keyType != null)
                        {
                            // It’s a dictionary with simple-type keys.
                            object outputDict;
                            if (already != null)
                            {
                                outputDict = already;
                                typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType)).GetMethod("Clear").Invoke(outputDict, null);
                            }
                            else
                                outputDict = Activator.CreateInstance(genericDefinition == typeof(IDictionary<,>) ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : serializedType);

                            outputDict.IfType((IClassifyObjectProcessor<TElement> dict) => { dict.BeforeDeserialize(elem); });

                            var addMethod = typeof(IDictionary<,>).MakeGenericType(keyType, valueType).GetMethod("Add", new Type[] { keyType, valueType });
                            var e = _format.GetDictionary(elem).GetEnumerator();

                            var keysToAdd = new List<object>();
                            var valuesToAdd = new List<Func<object>>();
                            return prevResult =>
                            {
                                if (keysToAdd.Count > 0)
                                    valuesToAdd.Add(prevResult);

                                if (e.MoveNext())
                                {
                                    keysToAdd.Add(ExactConvert.To(keyType, e.Current.Key));
                                    return deserialize(valueType, e.Current.Value, null, parentNode, enforceEnums);
                                }

                                Ut.Assert(keysToAdd.Count == valuesToAdd.Count);
                                _doAtTheEnd.Add(() =>
                                {
                                    for (int i = 0; i < keysToAdd.Count; i++)
                                    {
                                        var keyToAdd = keysToAdd[i];
                                        var valueToAdd = valuesToAdd[i]();
                                        if (!enforceEnums || ((!keyType.IsEnum || allowEnumValue(keyType, keyToAdd)) && (!valueType.IsEnum || allowEnumValue(valueType, valueToAdd))))
                                            addMethod.Invoke(outputDict, new object[] { keyToAdd, valueToAdd });
                                    }
                                });
                                return cleanUp(() => outputDict);
                            };
                        }
                        else if (serializedType.IsArray)
                        {
                            var input = _format.GetList(elem, null).ToArray();
                            var outputArray = (already != null && ((Array) already).GetLength(0) == input.Length) ? already : serializedType.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { input.Length });
                            var setMethod = serializedType.GetMethod("Set", new Type[] { typeof(int), valueType });
                            var i = -1;
                            var setters = new Func<object>[input.Length];
                            return prevResult =>
                            {
                                if (i >= 0)
                                    setters[i] = prevResult;
                                i++;
                                if (i < input.Length)
                                    return deserialize(valueType, input[i], null, parentNode, enforceEnums);
                                _doAtTheEnd.Add(() =>
                                {
                                    for (int j = 0; j < setters.Length; j++)
                                    {
                                        var valueToSet = setters[j]();
                                        if (!enforceEnums || !valueType.IsEnum || allowEnumValue(valueType, valueToSet))
                                            setMethod.Invoke(outputArray, new object[] { j, valueToSet });
                                    }
                                });
                                return cleanUp(() => outputArray);
                            };
                        }
                        else
                        {
                            // It’s a list, but not an array or a dictionary.
                            object outputList;
                            if (already != null && already.GetType() == serializedType)
                            {
                                outputList = already;
                                typeof(ICollection<>).MakeGenericType(valueType).GetMethod("Clear").Invoke(outputList, null);
                            }
                            else
                                outputList = Activator.CreateInstance(genericDefinition == typeof(ICollection<>) || genericDefinition == typeof(IList<>) ? typeof(List<>).MakeGenericType(valueType) : serializedType);

                            outputList.IfType((IClassifyObjectProcessor<TElement> list) => { list.BeforeDeserialize(elem); });

                            var addMethod = typeof(ICollection<>).MakeGenericType(valueType).GetMethod("Add", new Type[] { valueType });
                            var e = _format.GetList(elem, null).GetEnumerator();
                            var first = true;
                            var adders = new List<Func<object>>();
                            return prevResult =>
                            {
                                if (!first)
                                    adders.Add(prevResult);
                                first = false;
                                if (e.MoveNext())
                                    return deserialize(valueType, e.Current, null, parentNode, enforceEnums);
                                _doAtTheEnd.Add(() =>
                                {
                                    foreach (var adder in adders)
                                    {
                                        var valueToAdd = adder();
                                        if (!enforceEnums || !valueType.IsEnum || allowEnumValue(valueType, valueToAdd))
                                            addMethod.Invoke(outputList, new object[] { valueToAdd });
                                    }
                                });
                                return cleanUp(() => outputList);
                            };
                        }
                    }
                    else
                    {
                        // It’s NOT a collection or dictionary

                        object ret;

                        try
                        {
                            if (already != null && already.GetType() == serializedType)
                                ret = already;
                            // Anonymous types
                            else if (serializedType.Name.StartsWith("<>f__AnonymousType") && serializedType.IsGenericType && serializedType.IsDefined<CompilerGeneratedAttribute>())
                            {
                                var constructor = serializedType.GetConstructors().First();
                                ret = constructor.Invoke(constructor.GetParameters().Select(p => p.ParameterType.GetDefaultValue()).ToArray());
                            }
                            else
                                ret = Activator.CreateInstance(serializedType, true);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("An object of type {0} could not be created:\n{1}".Fmt(serializedType.FullName, e.Message), e);
                        }

                        var first = true;
                        return prevResult =>
                        {
                            if (first)
                            {
                                first = false;
                                return deserializeIntoObject(elem, ret, serializedType, parentNode);
                            }
                            return cleanUp(prevResult);
                        };
                    }
                }
            }

            private struct deserializeFieldInfo
            {
                public FieldInfo FieldToAssignTo;
                public TElement ElementToAssign;
                public bool EnforceEnum;
                public Type DeserializeAsType;
                public Func<object, object> SubstituteConverter;
            }

            private WorkNode<Func<object>> deserializeIntoObject(TElement elem, object intoObj, Type type, object parentNode)
            {
                intoObj.IfType((IClassifyObjectProcessor<TElement> obj) => { obj.BeforeDeserialize(elem); });

                var infos = new List<deserializeFieldInfo>();

                foreach (var field in type.GetAllFields())
                {
                    string rFieldName = field.Name.TrimStart('_');
                    MemberInfo getAttrsFrom = field;

                    if (rFieldName.StartsWith("<") && rFieldName.EndsWith(">k__BackingField"))
                    {
                        // Compiler-generated fields for auto-implemented properties 
                        rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>k__BackingField".Length);
                        var prop = type.GetAllProperties().FirstOrDefault(p => p.Name == rFieldName);
                        if (prop != null)
                            getAttrsFrom = prop;
                    }
                    else if (rFieldName.StartsWith("<") && rFieldName.EndsWith(">i__Field"))
                    {
                        // Fields in anonymous types
                        rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>i__Field".Length);
                    }

                    // Skip events
                    if (type.GetEvent(rFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) != null)
                        continue;

                    var fieldDeclaringType = field.DeclaringType.AssemblyQualifiedName;

                    // [ClassifyIgnore]
                    if (getAttrsFrom.IsDefined<ClassifyIgnoreAttribute>())
                        continue;

                    // Fields with no special attributes (except perhaps [ClassifySubstitute])
                    else if (_format.HasField(elem, rFieldName, fieldDeclaringType))
                    {
                        var value = _format.GetField(elem, rFieldName, fieldDeclaringType);
                        if (!_format.IsNull(value) || !getAttrsFrom.IsDefined<ClassifyNotNullAttribute>())
                        {
                            var inf = new deserializeFieldInfo
                            {
                                FieldToAssignTo = field,
                                DeserializeAsType = field.FieldType,
                                ElementToAssign = value,
                                EnforceEnum = getAttrsFrom.IsDefined<ClassifyEnforceEnumAttribute>()
                            };

                            // [ClassifySubstitute]
                            var substituteAttr = getAttrsFrom.GetCustomAttributes<ClassifySubstituteAttribute>().FirstOrDefault();
                            if (substituteAttr != null)
                            {
                                var substInf = substituteAttr.GetInfo(field.FieldType);
                                inf.DeserializeAsType = substInf.SubstituteType;
                                inf.SubstituteConverter = substInf.FromSubstitute;
                            }

                            infos.Add(inf);
                        }
                    }
                }

                var valuesToAssign = new Func<object>[infos.Count];
                var i = -1;
                return prevResult =>
                {
                    if (i >= 0)
                        valuesToAssign[i] = prevResult;
                    i++;
                    if (i < infos.Count)
                        return deserialize(infos[i].DeserializeAsType, infos[i].ElementToAssign, infos[i].FieldToAssignTo.GetValue(intoObj), intoObj, infos[i].EnforceEnum);

                    _doAtTheEnd.Add(() =>
                    {
                        for (int j = 0; j < infos.Count; j++)
                        {
                            var valueToAssign = valuesToAssign[j]();
                            if (infos[j].SubstituteConverter != null)
                                valueToAssign = infos[j].SubstituteConverter(valueToAssign);
                            if (!infos[j].FieldToAssignTo.FieldType.IsEnum || !infos[j].EnforceEnum || allowEnumValue(infos[j].FieldToAssignTo.FieldType, valueToAssign))
                                infos[j].FieldToAssignTo.SetValue(intoObj, valueToAssign);
                        }
                    });
                    return new Func<object>(() => intoObj);
                };
            }

            private bool allowEnumValue(Type enumType, object enumValue)
            {
                if (!enumType.IsDefined<FlagsAttribute>())
                    return Array.IndexOf(Enum.GetValues(enumType), enumValue) != -1;

                if (Enum.GetUnderlyingType(enumType) == typeof(ulong))
                {
                    var ulongValue = (ulong) enumValue;
                    foreach (ulong allowedValue in Enum.GetValues(enumType))
                        ulongValue &= ~allowedValue;
                    return ulongValue == 0;
                }
                else    // everything else can be represented in a long
                {
                    var longValue = Convert.ToInt64(enumValue);
                    foreach (var allowedValue in Enum.GetValues(enumType))
                        longValue &= ~Convert.ToInt64(allowedValue);
                    return longValue == 0;
                }
            }

            public Func<TElement> Serialize(object saveObject, Type declaredType)
            {
                if (declaredType.IsPointer || declaredType.IsByRef)
                    throw new NotSupportedException("Classify cannot serialize pointers or by-reference variables.");

                Func<TElement> elem;

                // Add a “type” attribute if the instance type is different from the field’s declared type
                Type saveType = declaredType;
                string typeStr = null;
                bool typeStrIsFull = false;
                if (saveObject != null)
                {
                    saveType = saveObject.GetType();
                    if (saveType == typeof(IntPtr) || saveType == typeof(Pointer))
                        throw new NotSupportedException("Classify does not support serializing values of type \"{0}\". Consider marking the offending field with [ClassifyIgnore].".Fmt(saveType));
                    if (declaredType != saveType && !(saveType.IsValueType && declaredType == typeof(Nullable<>).MakeGenericType(saveType)))
                    {
                        // ... but only add this attribute if it is not a collection, because then Classify doesn’t care about the type when restoring the object anyway
                        Type[] typeParameters;
                        if (!declaredType.IsArray && !declaredType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters) && !declaredType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                        {
                            if (saveType.Assembly.Equals(declaredType.Assembly) && !saveType.IsGenericType && !saveType.IsNested)
                                typeStr = saveType.Namespace.Equals(declaredType.Namespace) && !saveType.IsArray ? saveType.Name : saveType.FullName;
                            else
                            {
                                typeStr = saveType.AssemblyQualifiedName;
                                typeStrIsFull = true;
                            }
                        }
                    }
                }

                // See if there’s a substitute type defined
                ClassifyTypeOptions typeOptions;
                var originalObject = saveObject;
                var originalType = saveType;

                if (_options._typeOptions.TryGetValue(saveType, out typeOptions) && typeOptions._substituteType != null)
                {
                    saveObject = typeOptions._toSubstitute(saveObject);
                    saveType = typeOptions._substituteType;
                }

                // Preserve reference identity of reference types except string
                if ((!(originalObject is ValueType) && !(originalObject is string) && _rememberC.Contains(originalObject)) ||
                    (saveType != originalType && !(saveObject is ValueType) && !(saveObject is string) && _rememberC.Contains(saveObject)))
                {
                    int refId;
                    if (!_requireRefId.TryGetValue(originalObject, out refId) && !_requireRefId.TryGetValue(saveObject, out refId))
                    {
                        refId = _nextId;
                        _nextId++;
                        _requireRefId[originalObject] = refId;
                        _requireRefId[saveObject] = refId;
                    }
                    return () => _format.FormatReference(refId);
                }

                // Remember this object so that we can detect cycles and maintain reference equality
                if (originalObject != null && !(originalObject is ValueType) && !(originalObject is string))
                    _rememberC.Add(originalObject);
                if (saveType != originalType && saveObject != null && !(saveObject is ValueType) && !(saveObject is string))
                    _rememberC.Add(saveObject);

                if (saveObject == null)
                    return () => _format.FormatNullValue();

                saveObject.IfType((IClassifyObjectProcessor obj) => { obj.BeforeSerialize(); });
                saveObject.IfType((IClassifyObjectProcessor<TElement> obj) => { obj.BeforeSerialize(); });
                typeOptions.IfType((IClassifyTypeProcessor opt) => { opt.BeforeSerialize(saveObject); });
                typeOptions.IfType((IClassifyTypeProcessor<TElement> opt) => { opt.BeforeSerialize(saveObject); });

                if (typeof(TElement).IsAssignableFrom(saveType))
                {
                    elem = () => _format.FormatSelfValue((TElement) saveObject);
                    typeStr = null;
                }
                else if (_simpleTypes.Contains(saveType) || saveType.IsEnum)
                    elem = () => _format.FormatSimpleValue(saveObject);
                else if (ExactConvert.IsSupportedType(saveType))
                    elem = () => _format.FormatSimpleValue(ExactConvert.ToString(saveObject));
                else
                {
                    Type[] typeParameters;

                    // Tuples and KeyValuePairs
                    var genericDefinition = saveType.IsGenericType ? saveType.GetGenericTypeDefinition() : null;
                    if (genericDefinition != null && _tupleTypes.Contains(genericDefinition))
                    {
                        var genericArguments = saveType.GetGenericArguments();
                        if (genericDefinition == typeof(KeyValuePair<,>))
                        {
                            var keyProperty = saveType.GetProperty("Key");
                            var valueProperty = saveType.GetProperty("Value");
                            if (keyProperty == null || valueProperty == null)
                                throw new InvalidOperationException("Cannot find Key or Value property in KeyValuePair type.");
                            var key = Serialize(keyProperty.GetValue(saveObject, null), genericArguments[0]);
                            var value = Serialize(valueProperty.GetValue(saveObject, null), genericArguments[1]);
                            elem = () => _format.FormatKeyValuePair(key(), value());
                        }
                        else
                        {
                            var items = Enumerable.Range(0, genericArguments.Length).Select(i =>
                            {
                                var property = saveType.GetProperty("Item" + (i + 1));
                                if (property == null)
                                    throw new InvalidOperationException("Cannot find expected item property in Tuple type.");
                                return Serialize(property.GetValue(saveObject, null), genericArguments[i]);
                            }).ToArray();
                            elem = () => _format.FormatList(true, items.Select(item => item()));
                        }
                    }
                    else if (saveType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters) && (typeParameters[0].IsEnum || _simpleTypes.Contains(typeParameters[0])))
                    {
                        // It’s a dictionary with a simple-type key.
                        // (More complex dictionaries are classified by treating them as an ICollection<KeyValuePair<K,V>>)
                        var keyType = typeParameters[0];
                        var valueType = typeParameters[1];

                        var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                        var keyProperty = kvpType.GetProperty("Key");
                        var valueProperty = kvpType.GetProperty("Value");
                        if (keyProperty == null || valueProperty == null)
                            throw new InvalidOperationException("Cannot find Key or Value property in KeyValuePair type.");

                        var kvps = ((IEnumerable) saveObject).Cast<object>().Select(kvp => new
                        {
                            Key = keyProperty.GetValue(kvp, null),
                            GetValue = Serialize(valueProperty.GetValue(kvp, null), valueType)
                        }).ToArray();
                        elem = () => _format.FormatDictionary(kvps.Select(kvp => new KeyValuePair<object, TElement>(kvp.Key, kvp.GetValue())));
                    }
                    else if (saveType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters) || saveType.IsArray)
                    {
                        // It’s an array or collection
                        var valueType = saveType.IsArray ? saveType.GetElementType() : typeParameters[0];
                        var items = ((IEnumerable) saveObject).Cast<object>().Select(val => Serialize(val, valueType)).ToArray();
                        elem = () => _format.FormatList(false, items.Select(item => item()));
                    }
                    else
                    {
                        var infs = serializeObject(saveObject, saveType).ToArray();
                        elem = () => _format.FormatObject(infs.Select(inf => new ObjectFieldInfo<TElement>(inf.FieldName, inf.DeclaringType, inf.Value())));
                    }
                }

                if (typeStr != null)
                {
                    var prevElem = elem;
                    elem = () => _format.FormatWithType(prevElem(), typeStr, typeStrIsFull);
                }

                // Make sure the classified element is only generated once,
                // and add the refid if it needs it
                {
                    bool retrieved = false;
                    TElement retrievedElem = default(TElement);
                    Func<TElement> previousElem = elem;
                    elem = () =>
                    {
                        if (!retrieved)
                        {
                            retrieved = true;
                            retrievedElem = previousElem();
                            int refId;
                            if (_requireRefId.TryGetValue(originalObject, out refId) || _requireRefId.TryGetValue(saveObject, out refId))
                                retrievedElem = _format.FormatReferable(retrievedElem, refId);
                            saveObject.IfType((IClassifyObjectProcessor<TElement> obj) => { obj.AfterSerialize(retrievedElem); });
                            typeOptions.IfType((IClassifyTypeProcessor<TElement> opt) => { opt.AfterSerialize(saveObject, retrievedElem); });
                        }
                        return retrievedElem;
                    };
                }

                return elem;
            }

            private IEnumerable<ObjectFieldInfo<Func<TElement>>> serializeObject(object saveObject, Type saveType)
            {
                bool ignoreIfDefaultOnType = saveType.IsDefined<ClassifyIgnoreIfDefaultAttribute>(true);
                bool ignoreIfEmptyOnType = saveType.IsDefined<ClassifyIgnoreIfEmptyAttribute>(true);

                var results = new List<Tuple<string, Type, Func<TElement>>>();
                var namesAlreadySeen = new HashSet<string>();
                var needsDeclaringType = new HashSet<string>();

                foreach (var field in saveType.GetAllFields())
                {
                    if (field.FieldType == saveType && saveType.IsValueType)
                        throw new InvalidOperationException(@"Cannot serialize an instance of the type {0} because it is a value type that contains itself.".Fmt(saveType.FullName));

                    // Ignore the backing field for events
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType) && saveType.GetEvent(field.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) != null)
                        continue;

                    string rFieldName = field.Name.TrimStart('_');
                    MemberInfo getAttrsFrom = field;

                    if (rFieldName.StartsWith("<") && rFieldName.EndsWith(">k__BackingField"))
                    {
                        // Compiler-generated fields for auto-implemented properties 
                        rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>k__BackingField".Length);
                        var prop = saveType.GetAllProperties().FirstOrDefault(p => p.Name == rFieldName);
                        if (prop != null)
                            getAttrsFrom = prop;
                    }
                    else if (rFieldName.StartsWith("<") && rFieldName.EndsWith(">i__Field"))
                    {
                        // Fields in anonymous types
                        rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>i__Field".Length);
                    }

                    // [ClassifyIgnore], [ClassifyParent]
                    if (getAttrsFrom.IsDefined<ClassifyIgnoreAttribute>())
                        continue;

                    object saveValue = field.GetValue(saveObject);
                    bool ignoreIfDefault = ignoreIfDefaultOnType || getAttrsFrom.IsDefined<ClassifyIgnoreIfDefaultAttribute>(true);

                    if (ignoreIfDefault)
                    {
                        if (saveValue == null)
                            continue;
                        if (field.FieldType.IsValueType &&
                            !(field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)) &&
                            saveValue.Equals(Activator.CreateInstance(field.FieldType)))
                            continue;
                    }

                    var ignoreIf = getAttrsFrom.GetCustomAttributes<ClassifyIgnoreIfAttribute>(true).FirstOrDefault();
                    if (ignoreIf != null && saveValue != null && saveValue.Equals(ignoreIf.Value))
                        continue;

                    // Arrays, lists and dictionaries all implement ICollection
                    bool ignoreIfEmpty = ignoreIfEmptyOnType || getAttrsFrom.IsDefined<ClassifyIgnoreIfEmptyAttribute>(true);
                    if (ignoreIfEmpty && saveValue is ICollection && ((ICollection) saveValue).Count == 0)
                        continue;

                    if (!namesAlreadySeen.Add(rFieldName))
                        needsDeclaringType.Add(rFieldName);

                    Func<TElement> elem;

                    // [ClassifySubstitute]
                    if (getAttrsFrom.IsDefined<ClassifySubstituteAttribute>())
                    {
                        var attr = getAttrsFrom.GetCustomAttributes<ClassifySubstituteAttribute>().First();
                        var attrInf = attr.GetInfo(field.FieldType);
                        elem = Serialize(attrInf.ToSubstitute(saveValue), attrInf.SubstituteType);
                    }
                    else
                    {
                        // None of the special attributes — just classify the value
                        elem = Serialize(saveValue, field.FieldType);
                    }

                    results.Add(new Tuple<string, Type, Func<TElement>>(rFieldName, field.DeclaringType, elem));
                }

                foreach (var result in results)
                    yield return new ObjectFieldInfo<Func<TElement>>(result.Item1, needsDeclaringType.Contains(result.Item1) ? result.Item2.AssemblyQualifiedName : null, result.Item3);
            }
        }

        /// <summary>
        ///     Performs safety checks to ensure that a specific type doesn't cause Classify exceptions. Run this method as a
        ///     post-build step to ensure reliability of execution. For an example of use, see <see
        ///     cref="Ut.RunPostBuildChecks"/>.</summary>
        /// <typeparam name="T">
        ///     The type that must be Classify-able.</typeparam>
        /// <param name="rep">
        ///     Object to report post-build errors to.</param>
        public static void PostBuildStep<T>(IPostBuildReporter rep)
        {
            PostBuildStep(typeof(T), rep);
        }

        /// <summary>
        ///     Performs safety checks to ensure that a specific type doesn't cause Classify exceptions. Run this method as a
        ///     post-build step to ensure reliability of execution. For an example of use, see <see
        ///     cref="Ut.RunPostBuildChecks"/>.</summary>
        /// <param name="type">
        ///     The type that must be Classify-able.</param>
        /// <param name="rep">
        ///     Object to report post-build errors to.</param>
        public static void PostBuildStep(Type type, IPostBuildReporter rep)
        {
            object instance;
            try
            {
                instance = Activator.CreateInstance(type, true);
            }
            catch (MissingMethodException)
            {
                rep.Error("The type {0} does not have a parameterless constructor.".Fmt(type.FullName), "class", type.Name);
                return;
            }
            postBuildStep(type, instance, null, rep, new HashSet<Type>(), Enumerable.Empty<string>());
        }

        private static void postBuildStep(Type type, object instance, MemberInfo member, IPostBuildReporter rep, HashSet<Type> alreadyChecked, IEnumerable<string> chain)
        {
            ClassifyTypeOptions opts;
            if (DefaultOptions._typeOptions.TryGetValue(type, out opts) && opts._substituteType != null)
            {
                postBuildStep(opts._substituteType, opts._toSubstitute(instance), member, rep, alreadyChecked, chain.Concat("Type substitution: " + opts._substituteType.FullName));
                return;
            }

            if (!alreadyChecked.Add(type))
                return;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                postBuildStep(type.GetGenericArguments()[0], instance, member, rep, alreadyChecked, chain);
                return;
            }

            Type[] genericTypeArguments;
            if (type.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out genericTypeArguments) || type.TryGetInterfaceGenericParameters(typeof(ICollection<>), out genericTypeArguments))
            {
                foreach (var typeArg in genericTypeArguments)
                    postBuildStep(typeArg, null, member, rep, alreadyChecked, chain.Concat("Dictionary type argument " + typeArg.FullName));
                return;
            }

            if (type == typeof(Pointer) || type == typeof(IntPtr) || type.IsPointer || type.IsByRef)
            {
                if (member == null)
                    rep.Error("Classify cannot serialize the type {0}. Use [ClassifyIgnore] to mark the field as not to be serialized".Fmt(type.FullName, chain.JoinString(", ")));
                else
                    rep.Error("Classify cannot serialize the type {0}, used by field {1}.{2}. Use [ClassifyIgnore] to mark the field as not to be serialized. Chain: {3}".Fmt(type.FullName, member.DeclaringType.FullName, member.Name, chain.JoinString(", ")), member.DeclaringType.Name, member.Name);
                return;
            }

            if (_simpleTypes.Contains(type))
                return; // these are safe

            if (!type.IsAbstract)
            {
                if (instance == null && !type.IsValueType && type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
                    rep.Error(
                        "The {3} {0}.{1} is set to null by default, and its type, {2}, does not have a parameterless constructor. Assign a non-null instance to the field in {0}'s constructor or declare a parameterless constructor in {2}. (Chain: {4})"
                            .Fmt(member.NullOr(m => m.DeclaringType.FullName), member.NullOr(m => m.Name), type.FullName, member is FieldInfo ? "field" : "property",
                                chain.JoinString(", ")),
                        member.NullOr(m => m.DeclaringType.Name), member.NullOr(m => m.Name));
                else
                {
                    var inst = instance ?? (type.ContainsGenericParameters ? null : Activator.CreateInstance(type, true));
                    foreach (var f in type.GetAllFields())
                    {
                        MemberInfo m = f;
                        if (f.Name.StartsWith("<") && f.Name.EndsWith(">k__BackingField"))
                        {
                            var pName = f.Name.Substring(1, f.Name.Length - "<>k__BackingField".Length);
                            m = type.GetAllProperties().FirstOrDefault(p => p.Name == pName) ?? (MemberInfo) f;
                        }
                        // Skip events
                        else if (type.GetEvent(f.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) != null)
                            continue;

                        if (m.IsDefined<ClassifyIgnoreAttribute>())
                            continue;
                        postBuildStep(f.FieldType, inst == null ? null : f.GetValue(inst), m, rep, alreadyChecked, chain.Concat(m.DeclaringType.FullName + "." + m.Name));
                    }
                }
            }
            if (type != typeof(object))
                foreach (var derivedType in type.Assembly.GetTypes())
                    if (type.IsAssignableFrom(derivedType))
                        postBuildStep(derivedType, null, member, rep, alreadyChecked, chain.Concat("Derived type: " + derivedType.FullName));
        }
    }

    /// <summary>
    ///     Contains methods to process an object and/or the associated serialized form before or after <see cref="Classify"/>
    ///     (de)serializes it. To have effect, this interface must be implemented by the object being serialized.</summary>
    /// <typeparam name="TElement">
    ///     Type of the serialized form.</typeparam>
    public interface IClassifyObjectProcessor<TElement>
    {
        /// <summary>
        ///     Pre-processes this object before <see cref="Classify"/> serializes it. This method is automatically invoked by
        ///     <see cref="Classify"/> and should not be called directly.</summary>
        void BeforeSerialize();

        /// <summary>
        ///     Post-processes the serialization produced by <see cref="Classify"/> for this object. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form produced for this object. All changes made to it are final and will appear in <see
        ///     cref="Classify"/>’s output.</param>
        void AfterSerialize(TElement element);

        /// <summary>
        ///     Pre-processes a serialized form before <see cref="Classify"/> restores the object from it. The object’s fields
        ///     have not yet been populated when this method is called. This method is automatically invoked by <see
        ///     cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form from which this object is about to be restored. All changes made to it will affect how the
        ///     object is restored.</param>
        void BeforeDeserialize(TElement element);

        /// <summary>
        ///     Post-processes this object after <see cref="Classify"/> has restored it from serialized form. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form from which this object was restored. Changes made to this will have no effect on the
        ///     deserialization.</param>
        void AfterDeserialize(TElement element);
    }

    /// <summary>
    ///     Contains methods to process an object before or after <see cref="Classify"/> (de)serializes it, irrespective of
    ///     the serialization format used. To have effect, this interface must be implemented by the object being serialized.</summary>
    public interface IClassifyObjectProcessor
    {
        /// <summary>
        ///     Pre-processes this object before <see cref="Classify"/> serializes it. This method is automatically invoked by
        ///     <see cref="Classify"/> and should not be called directly.</summary>
        void BeforeSerialize();

        /// <summary>
        ///     Post-processes this object after <see cref="Classify"/> has restored it from serialized form. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        void AfterDeserialize();
    }

    /// <summary>
    ///     Contains methods to process an object and/or the associated serialized form before or after <see cref="Classify"/>
    ///     (de)serializes it. To have effect, this interface must be implemented by a class derived from <see
    ///     cref="ClassifyTypeOptions"/> and associated with a type via <see cref="ClassifyOptions.AddTypeOptions"/>.</summary>
    /// <typeparam name="TElement">
    ///     Type of the serialized form.</typeparam>
    public interface IClassifyTypeProcessor<TElement>
    {
        /// <summary>
        ///     Pre-processes the object before <see cref="Classify"/> serializes it. This method is automatically invoked by
        ///     <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The object about to be serialized.</param>
        void BeforeSerialize(object obj);

        /// <summary>
        ///     Post-processes the serialization produced by <see cref="Classify"/> for this object. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The object that has just been serialized.</param>
        /// <param name="element">
        ///     The serialized form produced for this object. All changes made to it are final and will appear in <see
        ///     cref="Classify"/>’s output.</param>
        void AfterSerialize(object obj, TElement element);

        /// <summary>
        ///     Pre-processes a serialized form before <see cref="Classify"/> restores the object from it. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form from which this object is about to be restored. All changes made to it will affect how the
        ///     object is restored.</param>
        void BeforeDeserialize(TElement element);

        /// <summary>
        ///     Post-processes an object after <see cref="Classify"/> has restored it from serialized form. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The deserialized object.</param>
        /// <param name="element">
        ///     The serialized form from which this object was restored. Changes made to this will have no effect on the
        ///     deserialization.</param>
        void AfterDeserialize(object obj, TElement element);
    }

    /// <summary>
    ///     Contains methods to process an object before or after <see cref="Classify"/> (de)serializes it. To have effect,
    ///     this interface must be implemented by a class derived from <see cref="ClassifyTypeOptions"/> and associated with a
    ///     type via <see cref="ClassifyOptions.AddTypeOptions"/>.</summary>
    public interface IClassifyTypeProcessor
    {
        /// <summary>
        ///     Pre-processes the object before <see cref="Classify"/> serializes it. This method is automatically invoked by
        ///     <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The object about to be serialized.</param>
        void BeforeSerialize(object obj);

        /// <summary>
        ///     Post-processes an object after <see cref="Classify"/> has restored it from serialized form. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The deserialized object.</param>
        void AfterDeserialize(object obj);
    }

    /// <summary>
    ///     Implement this interface to specify how to substitute a type for another type during Classify. The type
    ///     implementing this interface can be used in a class derived from <see cref="ClassifyTypeOptions"/> or in <see
    ///     cref="ClassifySubstituteAttribute"/>.</summary>
    /// <typeparam name="TTrue">
    ///     The type that is actually used for instances in memory.</typeparam>
    /// <typeparam name="TSubstitute">
    ///     The substitute type to be used for purposes of classifying and declassifying.</typeparam>
    public interface IClassifySubstitute<TTrue, TSubstitute>
    {
        /// <summary>
        ///     Converts an instance of the “real” type to a substitute instance to be classified.</summary>
        /// <param name="instance">
        ///     An instance of the “real” type to be substituted.</param>
        /// <returns>
        ///     The converted object to use in classifying.</returns>
        TSubstitute ToSubstitute(TTrue instance);
        /// <summary>
        ///     Converts a substitute instance, generated by declassifying, back to the “real” type.</summary>
        /// <param name="instance">
        ///     An instance of the substituted type, provided by Classify.</param>
        /// <returns>
        ///     The converted object to put into the real type.</returns>
        TTrue FromSubstitute(TSubstitute instance);
    }

    /// <summary>Specifies some options for use by <see cref="Classify"/>.</summary>
    public sealed class ClassifyOptions
    {
        /// <summary>
        ///     The base directory from which to construct the paths for additional files whenever a field has an <see
        ///     cref="ClassifyFollowIdAttribute"/> attribute. Inferred automatically from filename if null.</summary>
        public string BaseDir = null;

        /// <summary>
        ///     This option is only relevant if the value you are deserializing is an enum value or a collection or dictionary
        ///     involving enum keys or values. If <c>true</c>, only enum values declared in the enum type are allowed (as if
        ///     it were a field with <see cref="ClassifyEnforceEnumAttribute"/>). Enum values inside of objects are not
        ///     affected by this option (but only by <see cref="ClassifyEnforceEnumAttribute"/>).</summary>
        /// <seealso cref="ClassifyEnforceEnumAttribute"/>
        public bool EnforceEnums = false;

        internal Dictionary<Type, ClassifyTypeOptions> _typeOptions = new Dictionary<Type, ClassifyTypeOptions>();

        /// <summary>
        ///     Adds options that are relevant to classifying/declassifying a specific type.</summary>
        /// <param name="type">
        ///     The type to which these options apply.</param>
        /// <param name="options">
        ///     Options that apply to the <paramref name="type"/>. To enable type substitution, pass an instance of a class
        ///     that implements <see cref="IClassifySubstitute{TTrue,TSubstitute}"/>. To use pre-/post-processing of the
        ///     object or its serialized form, pass an instance of a class that implements <see
        ///     cref="IClassifyTypeProcessor"/> or <see cref="IClassifyTypeProcessor{TElement}"/>.</param>
        /// <returns>
        ///     Itself.</returns>
        public ClassifyOptions AddTypeOptions(Type type, ClassifyTypeOptions options)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (options == null)
                throw new ArgumentNullException("options");
            if (_typeOptions.ContainsKey(type))
                throw new ArgumentException("Classify options for type {0} have already been defined.".Fmt(type), "type");
            if (_typeOptions.Values.Contains(options))
                throw new ArgumentException("Must use a different ClassifyTypeOptions instance for every type.", "options");
            bool implementsUsefulInterface = options is IClassifyTypeProcessor
                || options.GetType().GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(IClassifyTypeProcessor<>) || i.GetGenericTypeDefinition() == typeof(IClassifySubstitute<,>)));
            bool implementsUselessInterface = options is IClassifyObjectProcessor
                || options.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClassifyObjectProcessor<>));
            if (implementsUselessInterface && !implementsUsefulInterface)
                throw new InvalidOperationException("This ClassifyTypeOptions type implements at least one interface meant for the objects to be serialized, and none of the interfaces meant for the ClassifyTypeOptions descendants. These type options would have zero effect, so this is likely a programming error.");
            options.initializeFor(type);
            _typeOptions.Add(type, options);
            return this;
        }
    }

    /// <summary>
    ///     Provides an abstract base type to derive from to specify type-specific options for use in Classify. See remarks
    ///     for more information.</summary>
    /// <remarks>
    ///     <para>
    ///         Derive from this type and implement <see cref="IClassifySubstitute{TTrue,TSubstitute}"/> to enable type
    ///         substitution during Classify. (This type substitution can be overridden by the presence of a <see
    ///         cref="ClassifySubstituteAttribute"/> on a field or automatically-implemented property.)</para>
    ///     <para>
    ///         Derive from this type and implement <see cref="IClassifyTypeProcessor"/> or <see
    ///         cref="IClassifyTypeProcessor{TElement}"/> to pre-/post-process the object or its serialized form before/after
    ///         Classify. (You can also implement <see cref="IClassifyObjectProcessor"/> or <see
    ///         cref="IClassifyObjectProcessor{TElement}"/> on the serialized type itself.)</para>
    ///     <para>
    ///         Intended use is to declare a class derived from <see cref="ClassifyTypeOptions"/> and pass an instance of it
    ///         into <see cref="ClassifyOptions.AddTypeOptions"/>.</para></remarks>
    public abstract class ClassifyTypeOptions
    {
        internal Type _substituteType;
        internal Func<object, object> _toSubstitute;
        internal Func<object, object> _fromSubstitute;

        internal void initializeFor(Type type)
        {
            var substInterfaces = GetType().GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IClassifySubstitute<,>) && t.GetGenericArguments()[0] == type).ToArray();
            if (substInterfaces.Length > 1)
                throw new ArgumentException("The type {0} implements more than one IClassifySubstitute<{1}, *> interface. Expected at most one.".Fmt(GetType().FullName, type.FullName), "type");
            else if (substInterfaces.Length == 1)
            {
                _substituteType = substInterfaces[0].GetGenericArguments()[1];
                if (type == _substituteType)
                    throw new InvalidOperationException("The type {0} implements a substitution from type {1} to itself.".Fmt(GetType().FullName, type.FullName));
                var toSubstMethod = substInterfaces[0].GetMethod("ToSubstitute");
                var fromSubstMethod = substInterfaces[0].GetMethod("FromSubstitute");
                _toSubstitute = obj =>
                {
                    var result = toSubstMethod.Invoke(this, new[] { obj });
                    if (result != null && result.GetType() != _substituteType) // forbidden just in case because I see no use cases for returning a subtype
                        throw new InvalidOperationException("The method {0} is expected to return an instance of the substitute type, {1}. It returned a subtype, {2}.".Fmt(toSubstMethod, _substituteType.FullName, result.GetType().FullName));
                    return result;
                };
                _fromSubstitute = obj =>
                {
                    var result = fromSubstMethod.Invoke(this, new[] { obj });
                    if (result != null && result.GetType() != type) // forbidden just in case because I see no use cases for returning a subtype
                        throw new InvalidOperationException("The method {0} is expected to return an instance of the true type, {1}. It returned a subtype, {2}.".Fmt(fromSubstMethod, type.FullName, result.GetType().FullName));
                    return result;
                };
            }
        }
    }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, it is ignored by <see
    ///     cref="Classify"/>. Data stored in this field or automatically-implemented property is not persisted.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyIgnoreAttribute : Attribute { }

    /// <summary>
    ///     Indicates that the value stored in this field or automatically-implemented property should be converted to another
    ///     type when serializing and back when deserializing. This takes precedence over any type substitution configured in
    ///     a <see cref="ClassifyTypeOptions"/> derived class.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifySubstituteAttribute : Attribute
    {
        /// <summary>Gets the type used to perform the type substitution.</summary>
        public Type ConverterType { get; private set; }

        /// <summary>
        ///     Constructor.</summary>
        /// <param name="converterType">
        ///     Specifies a type that implements <see cref="IClassifySubstitute{TTrue,TSubstitute}"/>, where <c>TTrue</c> must
        ///     be the exact type of the field or automatically-implemented property bearing this attribute.</param>
        public ClassifySubstituteAttribute(Type converterType)
        {
            ConverterType = converterType;
        }

        internal Info GetInfo(Type fieldType)
        {
            var converter = Activator.CreateInstance(ConverterType);
            var inter = ConverterType.FindInterfaces((itf, _) => itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(IClassifySubstitute<,>) && itf.GetGenericArguments()[0] == fieldType, null);
            if (inter.Length != 1)
                throw new InvalidOperationException("Provided type in [ClassifySubstitute] attribute must implement interface IClassifySubstitute<,>, the first generic type argument must be the field type, and there must be only one such interface.");

            // Can’t use Delegate.CreateDelegate() because the method could return a value type or void, which is not directly compatible with Func<object, object>
            var fromSubstituteMethod = inter[0].GetMethod("FromSubstitute");
            var toSubstituteMethod = inter[0].GetMethod("ToSubstitute");
            return new Info(
                substituteType: inter[0].GetGenericArguments()[1],
                fromSubstitute: new Func<object, object>(obj => fromSubstituteMethod.Invoke(converter, new[] { obj })),
                toSubstitute: new Func<object, object>(obj => toSubstituteMethod.Invoke(converter, new[] { obj })));
        }

        internal sealed class Info
        {
            public Type SubstituteType { get; private set; }
            public Func<object, object> FromSubstitute { get; private set; }
            public Func<object, object> ToSubstitute { get; private set; }

            public Info(Type substituteType, Func<object, object> fromSubstitute, Func<object, object> toSubstitute)
            {
                SubstituteType = substituteType;
                FromSubstitute = fromSubstitute;
                ToSubstitute = toSubstitute;
            }
        }
    }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if the value is null, 0, false, etc. If it is used on a type, it applies to all fields and
    ///     automatically-implemented properties in the type. See also remarks.</summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Using this together with <see cref="ClassifyIgnoreIfEmptyAttribute"/> will cause the distinction between
    ///             null and an empty collection to be lost. However, a collection containing only null elements is persisted
    ///             correctly.</description></item>
    ///         <item><description>
    ///             Do not use this custom attribute on a field that has a non-default value set in the containing class’s
    ///             constructor. Doing so will cause a serialized null/0/false value to revert to that constructor value upon
    ///             deserialization.</description></item></list></remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class ClassifyIgnoreIfDefaultAttribute : Attribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if that serialization would be completely empty. If it is used on a type, it applies to all
    ///     collection-type fields in the type. See also remarks.</summary>
    /// <remarks>
    ///     Using this together with <see cref="ClassifyIgnoreIfDefaultAttribute"/> will cause the distinction between null
    ///     and an empty collection to be lost. However, a collection containing only null elements is persisted correctly.</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class ClassifyIgnoreIfEmptyAttribute : Attribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if the field’s or property’s value is equal to the specified value. See also remarks.</summary>
    /// <remarks>
    ///     Using this together with <see cref="ClassifyIgnoreIfDefaultAttribute"/> will cause the distinction between the
    ///     type’s default value and the specified value to be lost.</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyIgnoreIfAttribute : Attribute
    {
        private object _value;
        /// <summary>
        ///     Constructs an <see cref="ClassifyIgnoreIfAttribute"/> instance.</summary>
        /// <param name="value">
        ///     Specifies the value which causes a field or automatically-implemented property to be ignored.</param>
        public ClassifyIgnoreIfAttribute(object value) { _value = value; }
        /// <summary>Retrieves the value which causes a field or automatically-implemented property to be ignored.</summary>
        public object Value { get { return _value; } }
    }

    /// <summary>
    ///     Specifies that Classify shall not set this field or automatically-implemented property to <c>null</c>. If the
    ///     serialized form is <c>null</c>, the field or automatically-implemented property is instead left at the default
    ///     value assigned by the object’s default constructor.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyNotNullAttribute : Attribute { }

    /// <summary>
    ///     To be used on a field or automatically-implemented property of an enum type or a collection involving an enum
    ///     type. Specifies that Classify shall not allow integer values that are not explicitly declared in the relevant enum
    ///     type. If the serialized form is such an integer, fields or automatically-implemented properties of an enum type
    ///     are instead left at the default value assigned by the object’s default constructor, while in collections, the
    ///     relevant element is omitted (changing the size of the collection). If the enum type has the [Flags] attribute,
    ///     bitwise combinations of the declared values are allowed.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyEnforceEnumAttribute : Attribute { }
}
