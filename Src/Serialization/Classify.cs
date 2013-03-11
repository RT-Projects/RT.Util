using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;

/*
 * Provide a proper way to distinguish exceptions due to the caller breaking some contract from exceptions due to data load failures. Always pass through the former.
 * Can the Follow attribute be implemented separately using ClassifyOptions?
 * Built-in versioning support (e.g. in XML, using attribute like ver="1"), an IClassifyVersioned { Version { get } }, and passing it to IClassify[Object/Type]Processor<TElement>
 */

namespace RT.Util.Serialization
{
    /// <summary>
    ///     Provides static methods to represent objects of (almost) arbitrary classes in various formats (such as XML or JSON)
    ///     and to restore such objects again. See the remarks section for features and limitations.</summary>
    /// <remarks>
    ///     <para>
    ///         By default, when serializing a custom class, Classify persists the value of all instance fields, including
    ///         private, inherited and compiler-generated ones. It does not persist static members or the result of property
    ///         getters. Each field is persisted under a name that is the field’s name minus any leading underscores.
    ///         Compiler-generated fields for automatically-implemented properties are instead named after the
    ///         automatically-implemented property’s name minus any leading underscores.</para>
    ///     <para>
    ///         Classify can also generate representations of basic types such as <c>string</c>, <c>int</c>, <c>bool</c>,
    ///         etc.</para>
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
    ///             <c>TValue</c> must be a type also supported by Classify. <c>TKey</c> must be <c>string</c>, an integer type or
    ///             an enum type. If the field is of a concrete type, that type is maintained, but its extra fields are not
    ///             persisted. If the field is of the interface type <c>IDictionary&lt;TKey, TValue&gt;</c> itself, the type
    ///             <c>Dictionary&lt;TKey, TValue&gt;</c> is used to reconstruct the object.</description></item>
    ///         <item><description>
    ///             Classify has special handling for classes that implement <c>ICollection&lt;T&gt;</c>, where <c>T</c> must be a
    ///             type also supported by Classify. If the field is of a concrete type, that type is maintained, but its extra
    ///             fields are not persisted. If the field is of the interface type <c>ICollection&lt;T&gt;</c> or
    ///             <c>IList&lt;T&gt;</c> itself, the type <c>List&lt;T&gt;</c> is used to reconstruct the object. If the type
    ///             also implements <c>IDictionary&lt;TKey, TValue&gt;</c>, the special handling for that takes
    ///             precedence.</description></item>
    ///         <item><description>
    ///             Classify also supports <see cref="KeyValuePair{TKey,TValue}"/> and all the different
    ///             <c>System.Tuple&lt;...&gt;</c> types.</description></item>
    ///         <item><description>
    ///             Classify handles the element type specially. For example, if you are classifying to XML, classifying an <see
    ///             cref="System.Xml.Linq.XElement"/> object generates the XML directly; if you are classifying to JSON, the same
    ///             goes for <see cref="RT.Util.Json.JsonValue"/> objects, etc.</description></item>
    ///         <item><description>
    ///             For classes that don’t implement any of the above-mentioned collection interfaces, Classify supports
    ///             polymorphism. The actual type of an instance is persisted if it is different from the declared
    ///             type.</description></item>
    ///         <item><description>
    ///             Classify supports auto-implemented properties. It uses the name of the property rather than the hidden
    ///             auto-generated field, although the field’s value is persisted. All other properties are
    ///             ignored.</description></item>
    ///         <item><description>
    ///             Classify ignores the order of input elements (except when handling collections and dictionaries). For example,
    ///             XML tags or JSON dictionary keys are mapped to fields by their names; their order is considered
    ///             immaterial.</description></item>
    ///         <item><description>
    ///             Classify silently discards unrecognized XML tags/JSON dictionary keys instead of throwing errors. This is by
    ///             design because it enables the programmer to remove a field from a class without invalidating objects
    ///             previously persisted.</description></item>
    ///         <item><description>
    ///             Classify silently ignores missing elements. A field whose element is missing retains the value assigned to it
    ///             by the parameterless constructor. This is by design because it enables the programmer to add a new field to a
    ///             class (and to specify a default initialization value for it) without invalidating objects previously
    ///             persisted.</description></item>
    ///         <item><description>
    ///             The following custom attributes can be used to alter Classify’s behavior. See the custom attribute class’s
    ///             documentation for more information: <see cref="ClassifyFollowIdAttribute"/>, <see
    ///             cref="ClassifyIdAttribute"/>, <see cref="ClassifyIgnoreAttribute"/>, <see cref="ClassifyIgnoreIfAttribute"/>,
    ///             <see cref="ClassifyIgnoreIfDefaultAttribute"/>, <see cref="ClassifyIgnoreIfEmptyAttribute"/>, <see
    ///             cref="ClassifyParentAttribute"/>. Any attribute that can be used on a field, can equally well be used on an
    ///             auto-implemented property, but not on any other properties.</description></item>
    ///         <item><description>
    ///             Classify maintains object identity and correctly handles cycles in the object graph. Only <c>string</c>s are
    ///             exempt from this.</description></item>
    ///         <item><description>
    ///             Classify can make use of type substitutions. See <see cref="IClassifySubstitute{TTrue,TSubstitute}"/> for more
    ///             information.</description></item>
    ///         <item><description>
    ///             Classify allows you to pre-/post-process the serialized form and/or the serialized objects. See <see
    ///             cref="IClassifyObjectProcessor{TElement}"/> and <see cref="IClassifyTypeProcessor{TElement}"/> for more
    ///             information.</description></item></list>
    ///     <para>
    ///         Limitations:</para>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Classify requires that every type involved have a parameterless constructor, although it need not be public.
    ///             This parameterless constructor is executed with all its side-effects before each object is reconstructed. An
    ///             exception is made when a field in an object already has a non-null instance assigned to it by the constructor;
    ///             in such cases, the object is reused.</description></item>
    ///         <item><description>
    ///             If a field is of type <c>ICollection&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>, <c>IDictionary&lt;TKey,
    ///             TValue&gt;</c>, or any class that implements either of these, polymorphism is not supported, and nor is any
    ///             information stored in those classes. In particular, this means that the comparer used by a
    ///             <c>SortedDictionary&lt;TKey, TValue&gt;</c> is not persisted. However, if the containing class’s constructor
    ///             assigned a <c>SortedDictionary&lt;TKey, TValue&gt;</c> with a comparer, that instance, and hence its comparer,
    ///             is reused.</description></item></list></remarks>
    public static class Classify
    {
        /// <summary>
        ///     Options used when null is passed to methods that take options. Make sure not to modify this instance if any thread
        ///     in the application might be in the middle of using <see cref="Classify"/>; ideally the options shoud be set once
        ///     during startup and never changed after that.</summary>
        public static ClassifyOptions DefaultOptions = new ClassifyOptions();

        /// <summary>
        ///     Reads an object of the specified type from the specified file.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of object to read.</typeparam>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="parent">
        ///     If the class to be declassified has a field with the [XmlParent] attribute, that field will receive this
        ///     object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T LoadObjectFromFile<TElement, T>(IClassifyFormat<TElement> format, string filename, ClassifyOptions options = null, object parent = null)
        {
            return (T) LoadObjectFromFile<TElement>(typeof(T), format, filename, options, parent);
        }

        /// <summary>
        ///     Reads an object of the specified type from the specified file.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="type">
        ///     Type of object to read.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <param name="parent">
        ///     If the class to be declassified has a field with the [XmlParent] attribute, that field will receive this
        ///     object.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object LoadObjectFromFile<TElement>(Type type, IClassifyFormat<TElement> format, string filename, ClassifyOptions options = null, object parent = null)
        {
            string defaultBaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            TElement elem;
            using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                elem = format.ReadFromStream(f);
            return new classifier<TElement>(format, options, defaultBaseDir).Declassify(type, elem, parent);
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
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static T ObjectFromElement<TElement, T>(TElement elem, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return (T) new classifier<TElement>(format, options).Declassify(typeof(T), elem);
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
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     A new instance of the requested type.</returns>
        public static object ObjectFromElement<TElement>(Type type, TElement elem, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return new classifier<TElement>(format, options).Declassify(type, elem);
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
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void ClassifyIntoObject<TElement, T>(TElement element, IClassifyFormat<TElement> format, T intoObject, ClassifyOptions options = null)
        {
            new classifier<TElement>(format, options).IntoObject(element, intoObject, typeof(T), null);
        }

        /// <summary>
        ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired type.
        ///     The type of object is inferred from the object passed in.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="filename">
        ///     Path and filename of the file to read from.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="intoObject">
        ///     Object to assign values to in order to reconstruct the original object. Also determines the type of object
        ///     expected.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void ReadFileIntoObject<TElement>(string filename, IClassifyFormat<TElement> format, object intoObject, ClassifyOptions options = null)
        {
            TElement elem;
            using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                elem = format.ReadFromStream(f);
            new classifier<TElement>(format, options).IntoObject(elem, intoObject, intoObject.GetType(), null);
        }

        /// <summary>
        ///     Stores the specified object in a file with the given path and filename.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <typeparam name="T">
        ///     Type of the object to store.</typeparam>
        /// <param name="saveObject">
        ///     Object to store in a file.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="filename">
        ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void SaveObjectToFile<TElement, T>(T saveObject, IClassifyFormat<TElement> format, string filename, ClassifyOptions options = null)
        {
            SaveObjectToFile<TElement>(saveObject, format, typeof(T), filename, options);
        }

        /// <summary>
        ///     Stores the specified object in a file with the given path and filename.</summary>
        /// <typeparam name="TElement">
        ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
        /// <param name="saveObject">
        ///     Object to store in a file.</param>
        /// <param name="format">
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="saveType">
        ///     Type of the object to store.</param>
        /// <param name="filename">
        ///     Path and filename of the file to be created. If the file already exists, it is overwritten.</param>
        /// <param name="options">
        ///     Options.</param>
        public static void SaveObjectToFile<TElement>(object saveObject, IClassifyFormat<TElement> format, Type saveType, string filename, ClassifyOptions options = null)
        {
            string defaultBaseDir = filename.Contains(Path.DirectorySeparatorChar) ? filename.Remove(filename.LastIndexOf(Path.DirectorySeparatorChar)) : ".";
            var element = new classifier<TElement>(format, options, defaultBaseDir).Classify(saveObject, saveType)();
            PathUtil.CreatePathToFile(filename);
            using (var f = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None))
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
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     The serialized form generated from the object.</returns>
        public static TElement ObjectToElement<TElement, T>(T saveObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return new classifier<TElement>(format, options).Classify(saveObject, typeof(T))();
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
        ///     Implementation of a Classify format. See <see cref="ClassifyFormats"/> for some provided examples.</param>
        /// <param name="options">
        ///     Options.</param>
        /// <returns>
        ///     The serialized form generated from the object.</returns>
        public static TElement ObjectToElement<TElement>(Type saveType, object saveObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
        {
            return new classifier<TElement>(format, options).Classify(saveObject, saveType)();
        }

        private sealed class classifier<TElement>
        {
            private static Type[] SimpleTypes = { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(decimal), typeof(float), typeof(double), typeof(bool), typeof(char), typeof(string), typeof(DateTime) };

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

            private Dictionary<string, Func<object>> _rememberD
            {
                get
                {
                    if (_rememberCacheD == null)
                        _rememberCacheD = new Dictionary<string, Func<object>>();
                    return _rememberCacheD;
                }
            }
            private Dictionary<string, Func<object>> _rememberCacheD;

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

            public object Declassify(Type type, TElement elem, object parentNode = null)
            {
                _doAtTheEnd = new List<Action>();
                var result = declassify(type, elem, null, parentNode);
                foreach (var action in _doAtTheEnd)
                    action();
                return result();
            }

            // “already” = an object that was already stored in a field that we’re declassifying. We re-use this object in case it has no default constructor
            private Func<object> declassify(Type type, TElement elem, object already, object parentNode)
            {
                Func<object> result;

                var originalType = type;
                var genericDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

                ClassifyTypeOptions typeOptions = null;
                if (_options._typeOptions.TryGetValue(type, out typeOptions))
                {
                    if (typeOptions._substituteType != null)
                        type = typeOptions._substituteType;
                    if (typeOptions is IClassifyTypeProcessor<TElement>)
                        ((IClassifyTypeProcessor<TElement>) typeOptions).BeforeDeclassify(elem);
                }

                if (_format.IsNull(elem))
                    result = () => null;
                else if (_format.IsReference(elem))
                {
                    var refID = _format.GetReferenceID(elem);
                    result = () =>
                    {
                        if (!_rememberD.ContainsKey(refID))
                            throw new InvalidOperationException(@"An element with the attribute ref=""{0}"" was encountered, but there is no matching element with the corresponding refid=""{0}"".".Fmt(refID));
                        return _rememberD[refID]();
                    };
                }
                else if (type == typeof(TElement))
                    result = () => _format.GetSelfValue(elem);
                else if (SimpleTypes.Contains(type) || ExactConvert.IsSupportedType(type))
                    result = () => ExactConvert.To(type, _format.GetSimpleValue(elem));
                else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    // It’s a nullable type, just determine the inner type and start again
                    result = declassify(type.GetGenericArguments()[0], elem, already, parentNode);
                }
                else if (genericDefinition != null && _tupleTypes.Contains(type.GetGenericTypeDefinition()))
                {
                    // It’s a Tuple or KeyValuePair
                    var genericArguments = type.GetGenericArguments();
                    var tupleParams = new Func<object>[genericArguments.Length];
                    if (genericDefinition == typeof(KeyValuePair<,>))
                    {
                        TElement key, value;
                        _format.GetKeyValuePair(elem, out key, out value);
                        tupleParams[0] = declassify(genericArguments[0], key, null, parentNode);
                        tupleParams[1] = declassify(genericArguments[1], value, null, parentNode);
                    }
                    else
                    {
                        var values = _format.GetList(elem, genericArguments.Length).ToArray();
                        for (int i = 0; i < genericArguments.Length; i++)
                            if (i < values.Length && values[i] != null)
                                tupleParams[i] = declassify(genericArguments[i], values[i], null, parentNode);
                    }
                    var constructor = type.GetConstructor(genericArguments);
                    if (constructor == null)
                        throw new InvalidOperationException("Could not find expected Tuple constructor.");
                    result = () => constructor.Invoke(tupleParams.Select(act => act()).ToArray());
                }
                else
                {
                    // Check if it’s an array, collection or dictionary
                    Type[] typeParameters;
                    Type keyType = null, valueType = null;
                    if (type.IsArray)
                        valueType = type.GetElementType();
                    else if (type.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                    {
                        keyType = typeParameters[0];
                        valueType = typeParameters[1];
                    }
                    else if (type.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters))
                        valueType = typeParameters[0];

                    if (valueType != null)
                    {
                        // It’s a collection or dictionary.

                        if (keyType != null)
                        {
                            // It’s a dictionary.
                            if (keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                                throw new InvalidOperationException("Classify encountered a dictionary with the key type {0}. Only string, integer types and enums are supported.".Fmt(keyType));

                            object outputDict;
                            if (already != null)
                            {
                                outputDict = already;
                                typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType)).GetMethod("Clear").Invoke(outputDict, null);
                            }
                            else
                                outputDict = Activator.CreateInstance(genericDefinition == typeof(IDictionary<,>) ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type);

                            if (outputDict is IClassifyObjectProcessor<TElement>)
                                ((IClassifyObjectProcessor<TElement>) outputDict).BeforeDeclassify(elem);

                            var addMethod = typeof(IDictionary<,>).MakeGenericType(keyType, valueType).GetMethod("Add", new Type[] { keyType, valueType });
                            foreach (var kvp in _format.GetDictionary(elem))
                            {
                                var key = ExactConvert.To(keyType, kvp.Key);
                                var value = declassify(valueType, kvp.Value, null, parentNode);
                                _doAtTheEnd.Add(() => { addMethod.Invoke(outputDict, new object[] { key, value() }); });
                            }
                            result = () => outputDict;
                        }
                        else if (type.IsArray)
                        {
                            var input = _format.GetList(elem, null).ToArray();
                            var outputArray = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { input.Length });
                            var setMethod = type.GetMethod("Set", new Type[] { typeof(int), valueType });
                            for (int i = 0; i < input.Length; i++)
                            {
                                var index = i;
                                var value = declassify(valueType, input[i], null, parentNode);
                                _doAtTheEnd.Add(() => { setMethod.Invoke(outputArray, new object[] { index, value() }); });
                            }
                            result = () => outputArray;
                        }
                        else
                        {
                            // It’s a list, but not an array or a dictionary.
                            object outputList;
                            if (already != null)
                            {
                                outputList = already;
                                typeof(ICollection<>).MakeGenericType(valueType).GetMethod("Clear").Invoke(outputList, null);
                            }
                            else
                                outputList = Activator.CreateInstance(genericDefinition == typeof(ICollection<>) || genericDefinition == typeof(IList<>) ? typeof(List<>).MakeGenericType(valueType) : type);

                            if (outputList is IClassifyObjectProcessor<TElement>)
                                ((IClassifyObjectProcessor<TElement>) outputList).BeforeDeclassify(elem);

                            var addMethod = typeof(ICollection<>).MakeGenericType(valueType).GetMethod("Add", new Type[] { valueType });
                            foreach (var item in _format.GetList(elem, null))
                            {
                                var value = declassify(valueType, item, null, parentNode);
                                _doAtTheEnd.Add(() => { addMethod.Invoke(outputList, new object[] { value() }); });
                            }
                            result = () => outputList;
                        }
                    }
                    else
                    {
                        // It’s NOT a collection or dictionary

                        object ret;

                        Type realType = type;
                        var typeName = _format.GetType(elem);
                        if (typeName != null)
                        {
                            var candidate = Type.GetType(typeName) ??
                                type.Assembly.GetTypes().FirstOrDefault(t => !t.IsGenericType && !t.IsNested && ((t.Namespace == type.Namespace && t.Name == typeName) || t.FullName == typeName));
                            if (candidate != null)
                                realType = candidate;
                        }

                        try
                        {
                            ret = already == null || already.GetType() != realType ? Activator.CreateInstance(realType, true) : already;
                        }
                        catch (Exception e)
                        {
                            throw new Exception("An object of type {0} could not be created:\n{1}".Fmt(realType.FullName, e.Message), e);
                        }

                        intoObject(elem, ret, realType, parentNode);
                        result = () => ret;
                    }
                }

                // Apply de-substitution (if any)
                if (originalType != type)
                {
                    var unsubstitutedResult = result;
                    result = () => typeOptions._fromSubstitute(unsubstitutedResult());
                }

                // Make sure the declassified object is only generated once
                {
                    bool retrieved = false;
                    object retrievedObj = null;
                    Func<object> previousResult = result;
                    result = () =>
                    {
                        if (!retrieved)
                        {
                            retrieved = true;
                            retrievedObj = previousResult();
                            if (retrievedObj is IClassifyObjectProcessor<TElement>)
                                ((IClassifyObjectProcessor<TElement>) retrievedObj).AfterDeclassify(elem);
                            if (typeOptions is IClassifyTypeProcessor<TElement>)
                                ((IClassifyTypeProcessor<TElement>) typeOptions).AfterDeclassify(retrievedObj, elem);
                        }
                        return retrievedObj;
                    };
                }

                if (_format.IsReferable(elem))
                    _rememberD[_format.GetReferenceID(elem)] = result;

                return result;
            }

            public void IntoObject(TElement elem, object intoObj, Type type, object parentNode)
            {
                _doAtTheEnd = new List<Action>();
                if (_format.IsReferable(elem))
                    _rememberD[_format.GetReferenceID(elem)] = () => intoObj;
                intoObject(elem, intoObj, type, parentNode);
                foreach (var action in _doAtTheEnd)
                    action();
            }

            private void intoObject(TElement elem, object intoObj, Type type, object parentNode)
            {
                if (intoObj is IClassifyObjectProcessor<TElement>)
                    ((IClassifyObjectProcessor<TElement>) intoObj).BeforeDeclassify(elem);

                foreach (var field in type.GetAllFields())
                {
                    string rFieldName = field.Name.TrimStart('_');
                    MemberInfo getAttrsFrom = field;

                    // Special case: compiler-generated fields for auto-implemented properties have a name that can’t be used as a tag name. Use the property name instead, which is probably what the user expects anyway
                    var m = Regex.Match(rFieldName, @"^<(.*)>k__BackingField$");
                    if (m.Success)
                    {
                        var prop = type.GetAllProperties().FirstOrDefault(p => p.Name == m.Groups[1].Value);
                        if (prop != null)
                        {
                            rFieldName = m.Groups[1].Value;
                            getAttrsFrom = prop;
                        }
                    }

                    // [ClassifyIgnore]
                    if (getAttrsFrom.IsDefined<ClassifyIgnoreAttribute>())
                        continue;

                    // [ClassifyParent]
                    else if (getAttrsFrom.IsDefined<ClassifyParentAttribute>())
                        field.SetValue(intoObj, parentNode);

                    // [ClassifyFollowId]
                    else if (getAttrsFrom.IsDefined<ClassifyFollowIdAttribute>())
                    {
                        if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(ClassifyDeferredObject<>))
                            throw new Exception("The field {0}.{1} uses ClassifyFollowIdAttribute, but does not have the type ClassifyDeferredObject<T> for some T.".Fmt(type.FullName, field.Name));

                        Type innerType = field.FieldType.GetGenericArguments()[0];
                        if (_format.HasField(elem, rFieldName))
                        {
                            var subElem = _format.GetField(elem, rFieldName);
                            if (_format.IsFollowID(subElem))
                            {
                                var followId = _format.GetReferenceID(subElem);
                                if (_baseDir == null)
                                    throw new InvalidOperationException(@"An object that uses [ClassifyFollowId] can only be reconstructed if a base directory is specified (see “BaseDir” in the ClassifyOptions class).");
                                string newFile = Path.Combine(_baseDir, innerType.Name, followId + ".xml");
                                field.SetValue(intoObj,
                                    typeof(ClassifyDeferredObject<>).MakeGenericType(innerType)
                                        .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) /* id */, typeof(Func<object>) /* generator */ }, null)
                                        .Invoke(Ut.NewArray<object>(
                                            followId /* id */,
                                            new Func<object>(() => LoadObjectFromFile(innerType, _format, newFile, _options, intoObj)) /* generator */
                                        ))
                                );
                            }
                        }
                    }

                    // Fields with no special attributes
                    else if (_format.HasField(elem, rFieldName))
                    {
                        var subElem = _format.GetField(elem, rFieldName);
                        var declassified = declassify(field.FieldType, subElem, field.GetValue(intoObj), intoObj);
                        _doAtTheEnd.Add(() => { field.SetValue(intoObj, declassified()); });
                    }
                }
            }

            public Func<TElement> Classify(object saveObject, Type declaredType)
            {
                Func<TElement> elem;

                // Add a “type” attribute if the instance type is different from the field’s declared type
                Type saveType = declaredType;
                string typeStr = null;
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
                            typeStr = saveType.Assembly.Equals(declaredType.Assembly) && !saveType.IsGenericType && !saveType.IsNested
                                ? (saveType.Namespace.Equals(declaredType.Namespace) ? saveType.Name : saveType.FullName)
                                : saveType.AssemblyQualifiedName;
                        }
                    }
                }

                // See if there’s a substitute type defined
                ClassifyTypeOptions typeOptions;
                var originalObject = saveObject;
                if (_options._typeOptions.TryGetValue(saveType, out typeOptions) && typeOptions._substituteType != null)
                {
                    saveObject = typeOptions._toSubstitute(saveObject);
                    saveType = typeOptions._substituteType;
                }

                if (saveObject == null)
                    return () => _format.FormatNullValue();

                // Preserve reference identity of reference types except string
                if (!(originalObject is ValueType) && !(originalObject is string) && _rememberC.Contains(originalObject))
                {
                    int refId;
                    if (!_requireRefId.TryGetValue(originalObject, out refId))
                    {
                        refId = _nextId;
                        _nextId++;
                        _requireRefId[originalObject] = refId;
                    }
                    return () => _format.FormatReference(refId.ToString());
                }

                // Remember this object so that we can detect cycles and maintain reference equality
                if (saveObject != null && !(saveObject is ValueType) && !(saveObject is string))
                    _rememberC.Add(saveObject);

                if (saveObject is IClassifyObjectProcessor<TElement>)
                    ((IClassifyObjectProcessor<TElement>) saveObject).BeforeClassify();
                if (typeOptions is IClassifyTypeProcessor<TElement>)
                    ((IClassifyTypeProcessor<TElement>) typeOptions).BeforeClassify(saveObject);

                if (saveType == typeof(TElement))
                    elem = () => _format.FormatSelfValue((TElement) saveObject);
                else if (SimpleTypes.Contains(saveType))
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
                            var key = Classify(keyProperty.GetValue(saveObject, null), genericArguments[0]);
                            var value = Classify(valueProperty.GetValue(saveObject, null), genericArguments[1]);
                            elem = () => _format.FormatKeyValuePair(key(), value());
                        }
                        else
                        {
                            var items = Enumerable.Range(0, genericArguments.Length).Select(i =>
                            {
                                var property = saveType.GetProperty("Item" + (i + 1));
                                if (property == null)
                                    throw new InvalidOperationException("Cannot find expected item property in Tuple type.");
                                return Classify(property.GetValue(saveObject, null), genericArguments[i]);
                            }).ToArray();
                            elem = () => _format.FormatList(true, items.Select(item => item()));
                        }
                    }
                    else if (declaredType.TryGetInterfaceGenericParameters(typeof(IDictionary<,>), out typeParameters))
                    {
                        // It’s a dictionary
                        var keyType = typeParameters[0];
                        var valueType = typeParameters[1];

                        if (keyType != typeof(string) && !isIntegerType(keyType) && !keyType.IsEnum)
                            throw new InvalidOperationException("Classify encountered a dictionary with the key type {0}. Only string, integer types and enums are supported.".Fmt(keyType.FullName));

                        var kvpType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
                        var keyProperty = kvpType.GetProperty("Key");
                        var valueProperty = kvpType.GetProperty("Value");
                        if (keyProperty == null || valueProperty == null)
                            throw new InvalidOperationException("Cannot find Key or Value property in KeyValuePair type.");

                        var kvps = ((IEnumerable) saveObject).Cast<object>().Select(kvp => new
                        {
                            Key = keyProperty.GetValue(kvp, null),
                            GetValue = Classify(valueProperty.GetValue(kvp, null), valueType)
                        }).ToArray();
                        elem = () => _format.FormatDictionary(kvps.Select(kvp => new KeyValuePair<object, TElement>(kvp.Key, kvp.GetValue())));
                    }
                    else if (declaredType.TryGetInterfaceGenericParameters(typeof(ICollection<>), out typeParameters) || declaredType.IsArray)
                    {
                        // It’s an array or collection
                        var valueType = declaredType.IsArray ? declaredType.GetElementType() : typeParameters[0];
                        var items = ((IEnumerable) saveObject).Cast<object>().Select(val => Classify(val, valueType)).ToArray();
                        elem = () => _format.FormatList(false, items.Select(item => item()));
                    }
                    else
                    {
                        var kvps = classifyObject(saveObject, saveType).ToArray();
                        elem = () => _format.FormatObject(kvps.Select(kvp => new KeyValuePair<string, TElement>(kvp.Key, kvp.Value())));
                    }
                }

                if (typeStr != null)
                {
                    var prevElem = elem;
                    elem = () => _format.FormatWithType(prevElem(), typeStr);
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
                            if (_requireRefId.TryGetValue(originalObject, out refId))
                                retrievedElem = _format.FormatReferable(retrievedElem, refId.ToString());
                            if (saveObject is IClassifyObjectProcessor<TElement>)
                                ((IClassifyObjectProcessor<TElement>) saveObject).AfterClassify(retrievedElem);
                            if (typeOptions is IClassifyTypeProcessor<TElement>)
                                ((IClassifyTypeProcessor<TElement>) typeOptions).AfterClassify(saveObject, retrievedElem);
                        }
                        return retrievedElem;
                    };
                }

                return elem;
            }

            private IEnumerable<KeyValuePair<string, Func<TElement>>> classifyObject(object saveObject, Type saveType)
            {
                bool ignoreIfDefaultOnType = saveType.IsDefined<ClassifyIgnoreIfDefaultAttribute>(true);
                bool ignoreIfEmptyOnType = saveType.IsDefined<ClassifyIgnoreIfEmptyAttribute>(true);

                foreach (var field in saveType.GetAllFields())
                {
                    if (field.FieldType == saveType && saveType.IsValueType)
                        throw new InvalidOperationException(@"Cannot serialize an instance of the type {0} because it is a value type that contains itself.".Fmt(saveType.FullName));

                    // Ignore the backing field for events
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType) && saveType.GetEvent(field.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) != null)
                        continue;

                    string rFieldName = field.Name.TrimStart('_');
                    MemberInfo getAttrsFrom = field;

                    // Special case: compiler-generated fields for auto-implemented properties have a name that can’t be used as a tag name. Use the property name instead, which is probably what the user expects anyway
                    var m = Regex.Match(field.Name, @"^<(.*)>k__BackingField$");
                    if (m.Success)
                    {
                        var prop = saveType.GetAllProperties().FirstOrDefault(p => p.Name == m.Groups[1].Value);
                        if (prop != null)
                        {
                            rFieldName = m.Groups[1].Value;
                            getAttrsFrom = prop;
                        }
                    }

                    // [ClassifyIgnore], [ClassifyParent]
                    if (getAttrsFrom.IsDefined<ClassifyIgnoreAttribute>() || getAttrsFrom.IsDefined<ClassifyParentAttribute>())
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

                    // [ClassifyFollowId]
                    if (getAttrsFrom.IsDefined<ClassifyFollowIdAttribute>())
                    {
                        if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(ClassifyDeferredObject<>))
                            throw new InvalidOperationException("A field that uses the [ClassifyFollowId] attribute must have the type ClassifyDeferredObject<T> for some T.");

                        var deferredSaveValue = (IClassifyDeferredObject) saveValue;
                        var innerType = field.FieldType.GetGenericArguments()[0];

                        if (deferredSaveValue.Evaluated)
                        {
                            if (_baseDir == null)
                                throw new InvalidOperationException(@"An object that uses [ClassifyFollowId] can only be stored if a base directory is specified (see “BaseDir” in the ClassifyOptions class).");
                            var prop = field.FieldType.GetProperty("Value");
                            SaveObjectToFile(deferredSaveValue.Value, _format, innerType, Path.Combine(_baseDir, innerType.Name, deferredSaveValue.Id + ".xml"), _options);
                        }

                        yield return new KeyValuePair<string, Func<TElement>>(rFieldName, () => _format.FormatFollowID(deferredSaveValue.Id));
                    }
                    else
                    {
                        // None of the special attributes — just classify the value
                        yield return new KeyValuePair<string, Func<TElement>>(rFieldName, Classify(saveValue, field.FieldType));
                    }
                }
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
            object obj;
            try
            {
                obj = Activator.CreateInstance(type, nonPublic: true);
            }
            catch (Exception e)
            {
                rep.Error("Unable to instantiate type {0}, required by Classify. Check that it has a parameterless constructor and the constructor doesn't throw. Details: {1}".Fmt(type, e.Message), "class", type.Name);
                return;
            }
            System.Xml.Linq.XElement testElement;
            try
            {
                testElement = ObjectToElement(type, obj, ClassifyFormats.Xml);
            }
            catch (Exception e)
            {
                rep.Error("Unable to Classify type {0}. {1}".Fmt(type, e.Message), "class", type.Name);
                return;
            }
            try
            {
                ObjectFromElement(type, testElement, ClassifyFormats.Xml);
            }
            catch (Exception e)
            {
                rep.Error("Unable to de-Classify type {0}. {1}".Fmt(type, e.Message), "class", type.Name);
                return;
            }
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
        void BeforeClassify();

        /// <summary>
        ///     Post-processes the serialization produced by <see cref="Classify"/> for this object. This method is automatically
        ///     invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form produced for this object. All changes made to it are final and will appear in <see
        ///     cref="Classify"/>’s output.</param>
        void AfterClassify(TElement element);

        /// <summary>
        ///     Pre-processes a serialized form before <see cref="Classify"/> restores the object from it. The object’s fields
        ///     have not yet been populated when this method is called. This method is automatically invoked by <see
        ///     cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form from which this object is about to be restored. All changes made to it will affect how the
        ///     object is restored.</param>
        void BeforeDeclassify(TElement element);

        /// <summary>
        ///     Post-processes this object after <see cref="Classify"/> has restored it from serialized form. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form from which this object was restored. Changes made to this will have no effect on the
        ///     deserialization.</param>
        void AfterDeclassify(TElement element);
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
        ///     Pre-processes the object before <see cref="Classify"/> serializes it. This method is automatically invoked by <see
        ///     cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The object about to be serialized.</param>
        void BeforeClassify(object obj);

        /// <summary>
        ///     Post-processes the serialization produced by <see cref="Classify"/> for this object. This method is automatically
        ///     invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The object that has just been serialized.</param>
        /// <param name="element">
        ///     The serialized form produced for this object. All changes made to it are final and will appear in <see
        ///     cref="Classify"/>’s output.</param>
        void AfterClassify(object obj, TElement element);

        /// <summary>
        ///     Pre-processes a serialized form before <see cref="Classify"/> restores the object from it. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="element">
        ///     The serialized form from which this object is about to be restored. All changes made to it will affect how the
        ///     object is restored.</param>
        void BeforeDeclassify(TElement element);

        /// <summary>
        ///     Post-processes an object after <see cref="Classify"/> has restored it from serialized form. This method is
        ///     automatically invoked by <see cref="Classify"/> and should not be called directly.</summary>
        /// <param name="obj">
        ///     The deserialized object.</param>
        /// <param name="element">
        ///     The serialized form from which this object was restored. Changes made to this will have no effect on the
        ///     deserialization.</param>
        void AfterDeclassify(object obj, TElement element);
    }

    /// <summary>
    ///     Implement this interface in a subclass of <see cref="ClassifyTypeOptions"/> to specify how to substitute a type for
    ///     another type during Classify.</summary>
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

        internal Dictionary<Type, ClassifyTypeOptions> _typeOptions = new Dictionary<Type, ClassifyTypeOptions>();

        /// <summary>
        ///     Adds options that are relevant to classifying/declassifying a specific type.</summary>
        /// <param name="type">
        ///     The type to which these options apply.</param>
        /// <param name="options">
        ///     Options that apply to the <paramref name="type"/>. To enable type substitution, pass an instance of a class that
        ///     implements <see cref="IClassifySubstitute{TTrue,TSubstitute}"/>. To use pre-/post-processing of the object or its
        ///     serialized form, pass an instance of a class that implements <see
        ///     cref="IClassifyTypeProcessor{TElement}"/>.</param>
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
            options.initializeFor(type);
            _typeOptions.Add(type, options);
            return this;
        }
    }

    /// <summary>
    ///     Provides an abstract base type to derive from to specify type-specific options for use in Classify. See remarks for
    ///     more information.</summary>
    /// <remarks>
    ///     <para>
    ///         Derive from this type and implement <see cref="IClassifySubstitute{TTrue,TSubstitute}"/> to enable type
    ///         substitution during Classify.</para>
    ///     <para>
    ///         Derive from this type and implement <see cref="IClassifyTypeProcessor{TElement}"/> to pre-/post-process the object
    ///         or its serialized form before/after Classify. (You can also implement <see
    ///         cref="IClassifyObjectProcessor{TElement}"/> on the serialized type itself.)</para>
    ///     <para>
    ///         Intended use is to declare a class derived from <see cref="ClassifyTypeOptions"/> and pass an instance of it into
    ///         <see cref="ClassifyOptions.AddTypeOptions"/>.</para></remarks>
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
                throw new ArgumentException("The type {0} implements more than one IClassifySubstitute<{1}, *> interface. Expected at most one.".Fmt(GetType().FullName, type.FullName));
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
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> stores an ID that
    ///     points to another, separate file which in turn contains the actual object. This is only allowed on fields or
    ///     automatically-implemented properties of type <see cref="ClassifyDeferredObject&lt;T&gt;"/> for some type T. Use <see
    ///     cref="ClassifyDeferredObject&lt;T&gt;.Value"/> to retrieve the object. This retrieval is deferred until first use. Use
    ///     <see cref="ClassifyDeferredObject&lt;T&gt;.Id"/> to retrieve the ID used to reference the object. You can also capture
    ///     the ID into the type T by using the <see cref="ClassifyIdAttribute"/> attribute within that type.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyFollowIdAttribute : Attribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, it is ignored by <see cref="Classify"/>.
    ///     Data stored in this field or automatically-implemented property is not persisted.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyIgnoreAttribute : Attribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if the value is null, 0, false, etc. If it is used on a type, it applies to all fields and
    ///     automatically-implemented properties in the type. See also remarks.</summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>
    ///             Using this together with <see cref="ClassifyIgnoreIfEmptyAttribute"/> will cause the distinction between null
    ///             and an empty collection to be lost. However, a collection containing only null elements is persisted
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
    ///     Using this together with <see cref="ClassifyIgnoreIfDefaultAttribute"/> will cause the distinction between null and an
    ///     empty collection to be lost. However, a collection containing only null elements is persisted correctly.</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public sealed class ClassifyIgnoreIfEmptyAttribute : Attribute { }

    /// <summary>
    ///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
    ///     serialization if the field’s or property’s value is equal to the specified value. See also remarks.</summary>
    /// <remarks>
    ///     Using this together with <see cref="ClassifyIgnoreIfDefaultAttribute"/> will cause the distinction between the type’s
    ///     default value and the specified value to be lost.</remarks>
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
    ///     When reconstructing an interconnected graph of objects using <see cref="Classify"/>, a field or
    ///     automatically-implemented property with this attribute receives a reference to an object which refers to this object.
    ///     This can be used when serializing tree structures to receive a node’s parent. See also remarks.</summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>
    ///             If the field or automatically-implemented property is of an incompatible type, a run-time exception
    ///             occurs.</description></item>
    ///         <item><description>
    ///             If there was no parent node, the field or automatically-implemented property is set to
    ///             null.</description></item>
    ///         <item><description>
    ///             When persisting objects, fields and automatically-implemented properties with this attribute are
    ///             skipped.</description></item></list></remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyParentAttribute : Attribute { }

    /// <summary>
    ///     When reconstructing persisted objects using <see cref="Classify"/>, a field or automatically-implemented property with
    ///     this attribute receives the ID that was used to refer to the file that stores this object. See <see
    ///     cref="ClassifyFollowIdAttribute"/> for more information. The field or automatically-implemented property must be of
    ///     type string.</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ClassifyIdAttribute : Attribute { }

    /// <summary>Provides access to <see cref="ClassifyDeferredObject{T}"/> properties that do not depend on its type parameter.</summary>
    internal interface IClassifyDeferredObject
    {
        /// <summary>Determines whether the object has been computed.</summary>
        bool Evaluated { get; }

        /// <summary>Returns the ID used to refer to the object.</summary>
        string Id { get; }

        /// <summary>
        ///     Gets the object stored in this <see cref="ClassifyDeferredObject&lt;T&gt;"/>. Causes the object to be evaluated
        ///     when called.</summary>
        object Value { get; }
    }

    /// <summary>
    ///     Provides mechanisms to hold an object that has an ID and gets evaluated at first use.</summary>
    /// <typeparam name="T">
    ///     The type of the contained object.</typeparam>
    public sealed class ClassifyDeferredObject<T> : IClassifyDeferredObject
    {
        /// <summary>
        ///     Initialises a deferred object using a delegate or lambda expression.</summary>
        /// <param name="id">
        ///     Id that refers to the object to be generated.</param>
        /// <param name="generator">
        ///     Function to generate the object.</param>
        public ClassifyDeferredObject(string id, Func<T> generator) { _id = id; _generator = generator; }

        internal ClassifyDeferredObject(string id, Func<object> generator)
            : this(id, () => (T) generator()) { }

        /// <summary>
        ///     Initialises a deferred object using an actual object. Evaluation is not deferred.</summary>
        /// <param name="id">
        ///     Id that refers to the object.</param>
        /// <param name="value">
        ///     The object to store.</param>
        public ClassifyDeferredObject(string id, T value) { _id = id; _cached = value; _haveCache = true; }

        /// <summary>
        ///     Initialises a deferred object using a method reference and an array of parameters.</summary>
        /// <param name="id">
        ///     ID that refers to the object to be generated.</param>
        /// <param name="generatorMethod">
        ///     Reference to a method that generates the object.</param>
        /// <param name="generatorObject">
        ///     Object on which the method should be invoked. Use null for static methods.</param>
        /// <param name="generatorParams">
        ///     Set of parameters for the method invocation.</param>
        public ClassifyDeferredObject(string id, MethodInfo generatorMethod, object generatorObject, object[] generatorParams)
        {
            _id = id;
            _generator = () => (T) generatorMethod.Invoke(generatorObject, generatorParams);
        }

        private Func<T> _generator;
        private T _cached;
        private bool _haveCache = false;
        private string _id;

        /// <summary>
        ///     Gets or sets the object stored in this <see cref="ClassifyDeferredObject&lt;T&gt;"/>. The property getter causes
        ///     the object to be evaluated when called. The setter overrides the object with a pre-computed object whose
        ///     evaluation is not deferred.</summary>
        public T Value
        {
            get
            {
                if (!_haveCache)
                {
                    _cached = _generator();
                    // Update any field in the class that has a [ClassifyId] attribute and is of type string.
                    foreach (var field in _cached.GetType().GetAllFields().Where(fld => fld.FieldType == typeof(string) && fld.IsDefined<ClassifyIdAttribute>()))
                        field.SetValue(_cached, _id);
                    _haveCache = true;
                }
                return _cached;
            }
            set
            {
                _cached = value;
                _haveCache = true;
            }
        }

        object IClassifyDeferredObject.Value { get { return Value; } }

        /// <summary>Determines whether the object has been computed.</summary>
        public bool Evaluated { get { return _haveCache; } }

        /// <summary>Returns the ID used to refer to the object.</summary>
        public string Id { get { return _id; } }
    }
}
