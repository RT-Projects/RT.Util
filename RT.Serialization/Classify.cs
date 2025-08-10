using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RT.Internal;
using RT.PostBuild;

/*
 * Provide a proper way to distinguish exceptions due to the caller breaking some contract from exceptions due to data load failures. Always pass through the former.
 * Can the Follow attribute be implemented separately using ClassifyOptions?
 * Built-in versioning support (e.g. in XML, using attribute like ver="1"), an IClassifyVersioned { Version { get } }, and passing it to IClassify[Object/Type]Processor<TElement>
 */

namespace RT.Serialization;

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
///         Classify can also generate representations of basic types such as <c>string</c>, <c>int</c>, <c>bool</c>, etc.</para>
///     <para>
///         Features:</para>
///     <list type="bullet">
///         <item><description>
///             Classify fully supports all the built-in types which are keywords in C# except <c>object</c> and
///             <c>dynamic</c>. It also supports <c>DateTime</c>, all enum types, <c>Tuple&lt;...&gt;</c> and <see
///             cref="KeyValuePair{TKey, TValue}"/>.</description></item>
///         <item><description>
///             Classify fully supports classes and structs that contain only fields of the above types as well as fields
///             whose type is itself such a class or struct.</description></item>
///         <item><description>
///             Classify has special handling for classes that implement <see cref="IDictionary{TKey, TValue}"/>, where
///             <c>TKey</c> and <c>TValue</c> must be type also supported by Classify. If a field containing a dictionary is
///             of a concrete type, that type is maintained, but its extra fields are not persisted. If the field is of the
///             interface type <see cref="IDictionary{TKey, TValue}"/> itself, the type <see cref="Dictionary{TKey, TValue}"/>
///             is used to reconstruct the object.</description></item>
///         <item><description>
///             Classify has special handling for classes that implement <see cref="ICollection{T}"/>, where <c>T</c> must be
///             a type also supported by Classify. If the field is of a concrete type, that type is maintained, but its extra
///             fields are not persisted. If the field is of the interface type <see cref="ICollection{T}"/> or <see
///             cref="IList{T}"/>, the type <see cref="List{T}"/> is used to reconstruct the object. If the type also
///             implements <see cref="IDictionary{TKey, TValue}"/>, the special handling for that takes precedence.</description></item>
///         <item><description>
///             Classify also specially handles <see cref="Stack{T}"/> and <see cref="Queue{T}"/> because they do not
///             implement <see cref="ICollection{T}"/>. Types derived from these are not supported (but are serialized as if
///             they weren’t a derived type).</description></item>
///         <item><description>
///             Classify supports fields of declared type <c>object</c> just as long as the value stored in it is of a
///             supported type.</description></item>
///         <item><description>
///             Classify handles values of the type of the serialized form specially. For example, if you are serializing to
///             XML using <see cref="System.Xml.Linq.XElement"/>, serializing an actual <see cref="System.Xml.Linq.XElement"/>
///             object generates the XML directly; if you are classifying to JSON, the same goes for JSON value objects, etc.</description></item>
///         <item><description>
///             For classes that don’t implement any of the above-mentioned collection interfaces, Classify supports
///             polymorphism. The actual type of an instance is persisted if it is different from the declared type.</description></item>
///         <item><description>
///             Classify supports auto-implemented properties. It uses the name of the property rather than the hidden
///             auto-generated field, although the field’s value is persisted. All other properties are ignored.</description></item>
///         <item><description>
///             Classify ignores the order of fields in a class. For example, XML tags or JSON dictionary keys are mapped to
///             fields by their names; their order is considered immaterial.</description></item>
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
///             documentation for more information: <see cref="ClassifyIgnoreAttribute"/>, <see
///             cref="ClassifyIgnoreIfAttribute"/>, <see cref="ClassifyIgnoreIfDefaultAttribute"/>, <see
///             cref="ClassifyIgnoreIfEmptyAttribute"/>. Any attribute that can be used on a field, can equally well be used
///             on an auto-implemented property, but not on any other properties.</description></item>
///         <item><description>
///             Classify maintains object identity and correctly handles cycles in the object graph. Only <c>string</c>s are
///             exempt from this.</description></item>
///         <item><description>
///             Classify can make use of type substitutions. See <see cref="IClassifySubstitute{TTrue,TSubstitute}"/> for more
///             information.</description></item>
///         <item><description>
///             Classify allows you to pre-/post-process the serialized form and/or the serialized objects. See <see
///             cref="IClassifyObjectProcessor"/>, <see cref="IClassifyObjectProcessor{TElement}"/>, <see
///             cref="IClassifyTypeProcessor"/> and <see cref="IClassifyTypeProcessor{TElement}"/> for more information.</description></item></list>
///     <para>
///         Limitations:</para>
///     <list type="bullet">
///         <item><description>
///             Classify requires that every type involved have a parameterless constructor, although it can be private. This
///             parameterless constructor is executed with all its side-effects before each object is reconstructed. An
///             exception is made when a field in an object already has a non-null instance assigned to it by the constructor;
///             in such cases, the object is reused.</description></item>
///         <item><description>
///             If a field is of type <see cref="ICollection{T}"/>, <see cref="IList{T}"/>, <see cref="IDictionary{TKey,
///             TValue}"/>, or any class that implements either of these, polymorphism is not supported, and nor is any
///             information stored in those classes. In particular, this means that the comparer used by a <see
///             cref="SortedDictionary{TKey, TValue}"/> is not persisted. However, if the containing class’s constructor
///             assigned a <see cref="SortedDictionary{TKey, TValue}"/> with a comparer, that instance, and hence its
///             comparer, is reused.</description></item>
///         <item><description>
///             Classify is not at all optimized for speed or memory efficiency.</description></item></list></remarks>
public static class Classify
{
    /// <summary>
    ///     Options used when null is passed to methods that take options. Make sure not to modify this instance if any thread
    ///     in the application might be in the middle of using <see cref="Classify"/>; ideally the options should be set once
    ///     during startup and never changed after that.</summary>
    public static ClassifyOptions DefaultOptions = new();

    /// <summary>
    ///     Reconstructs an object of the specified type from the specified file.</summary>
    /// <typeparam name="TElement">
    ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
    /// <typeparam name="T">
    ///     Type of object to read.</typeparam>
    /// <param name="filename">
    ///     Path and filename of the file to read from.</param>
    /// <param name="format">
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static T DeserializeFile<TElement, T>(string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        return (T) DeserializeFile(typeof(T), filename, format, options);
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
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static object DeserializeFile<TElement>(Type type, string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        TElement elem;
        using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            elem = format.ReadFromStream(f);
        return new classifier<TElement>(format, options).Deserialize(type, elem);
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
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static T Deserialize<TElement, T>(TElement elem, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        return (T) new classifier<TElement>(format, options).Deserialize(typeof(T), elem);
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
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    /// <returns>
    ///     A new instance of the requested type.</returns>
    public static object Deserialize<TElement>(Type type, TElement elem, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        return new classifier<TElement>(format, options).Deserialize(type, elem);
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
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void DeserializeIntoObject<TElement, T>(TElement element, T intoObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        new classifier<TElement>(format, options).DeserializeIntoObject(typeof(T), element, intoObject, "this");
    }

    /// <summary>
    ///     Reconstructs an object from the specified file by applying the values to an existing instance of the desired type.
    ///     The type of object is inferred from the object passed in.</summary>
    /// <typeparam name="TElement">
    ///     Type of the serialized form (see <paramref name="format"/>).</typeparam>
    /// <param name="filename">
    ///     Path and filename of the file to read from.</param>
    /// <param name="intoObject">
    ///     Object to assign values to in order to reconstruct the original object. Also determines the type of object
    ///     expected.</param>
    /// <param name="format">
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void DeserializeFileIntoObject<TElement>(string filename, object intoObject, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        TElement elem;
        using (var f = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            elem = format.ReadFromStream(f);
        new classifier<TElement>(format, options).DeserializeIntoObject(intoObject.GetType(), elem, intoObject, "this");
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
    ///     Implementation of a Classify format.</param>
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
    ///     Implementation of a Classify format.</param>
    /// <param name="options">
    ///     Options.</param>
    public static void SerializeToFile<TElement>(Type saveType, object saveObject, string filename, IClassifyFormat<TElement> format, ClassifyOptions options = null)
    {
        var element = new classifier<TElement>(format, options).Serialize(saveObject, saveType)();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(".", filename)));
        using var f = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
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
    ///     Implementation of a Classify format.</param>
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
    ///     Implementation of a Classify format.</param>
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
    private static readonly Type[] _simpleTypes = {
        // Integers
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(BigInteger),
        // Floating-point numbers
        typeof(decimal), typeof(float), typeof(double),
        // Other
        typeof(bool), typeof(char), typeof(string), typeof(DateTime)
    };

    private sealed class classifier<TElement>
    {
        private readonly ClassifyOptions _options;
        private int _nextId = 0;
        private List<Action> _doAtTheEnd;
        private readonly IClassifyFormat<TElement> _format;
        private readonly Dictionary<MemberInfo, object[]> _attributesCache = new();
        private HashSet<string> _classifySimpleAttributes;

        private enum collectionCategory
        {
            /// <summary>Array types such as <c>T[]</c>, <c>T[,]</c> etc.</summary>
            Array,

            /// <summary><see cref="Stack{T}"/>.</summary>
            Stack,

            /// <summary><see cref="Queue{T}"/>.</summary>
            Queue,

            /// <summary>
            ///     <see cref="Dictionary{TKey, TValue}"/>, where <c>TKey</c> is something that <see cref="ExactConvert"/> can
            ///     convert to and from <c>string</c>. This is the only <see cref="collectionCategory"/> that is serialized by
            ///     <see cref="IClassifyFormat{TElement}.FormatDictionary"/> instead of <see
            ///     cref="IClassifyFormat{TElement}.FormatList"/>.</summary>
            SimpleKeyedDictionary,

            /// <summary>
            ///     All other implementations of <see cref="ICollection{T}"/>, including any <see cref="Dictionary{TKey,
            ///     TValue}"/> not already covered by <see cref="SimpleKeyedDictionary"/>.</summary>
            Other
        }

        private bool hasClassifyAttribute<T>(MemberInfo member) where T : Attribute
        {
            return getClassifyAttributeOrNull<T>(member) != null;
        }

        private T getClassifyAttributeOrNull<T>(MemberInfo member) where T : Attribute
        {
            if (!_attributesCache.TryGetValue(member, out var attrs))
            {
                attrs = member.GetCustomAttributes(true);
                _attributesCache[member] = attrs;
            }

            _classifySimpleAttributes ??= new HashSet<string>(Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(Attribute).IsAssignableFrom(t))
                .Where(t => t.Name.StartsWith("Classify") && t.Name.EndsWith("Attribute"))
                .Where(t => t.GetConstructors().Length == 1 && t.GetConstructor(Type.EmptyTypes) != null) // only look at simple attributes that have no associated data
                .Select(t => t.Name));

            foreach (var attr in attrs)
            {
                if (attr is T match)
                    return match;
                else if (typeof(T).Name == attr.GetType().Name && _classifySimpleAttributes.Contains(attr.GetType().Name))
                    return Activator.CreateInstance<T>(); // it's a simple attribute declared externally: return an instance of the "real" classify attribute
            }

            return null;
        }

        public classifier(IClassifyFormat<TElement> format, ClassifyOptions options)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _options = options ?? DefaultOptions ?? new ClassifyOptions(); // in case someone set default options to null
        }

        private sealed class declassifyRememberedObject
        {
            public Func<object> WithDesubstitution;
            public Func<object> WithoutDesubstitution;
        }

        private Dictionary<int, declassifyRememberedObject> _rememberCacheDeser;
        private Dictionary<int, declassifyRememberedObject> rememberDeser => _rememberCacheDeser ??= new Dictionary<int, declassifyRememberedObject>();

        private Dictionary<object, object> _rememberCacheSer;
        private Dictionary<object, object> rememberSer => _rememberCacheSer ??= new Dictionary<object, object>(_options.ActualEqualityComparer);

        private Dictionary<object, int> requireRefId => _requireRefIdCache ??= new Dictionary<object, int>(new CustomEqualityComparer<object>(ReferenceEquals, o => o.GetHashCode()));
        private Dictionary<object, int> _requireRefIdCache;

        private static readonly Type[] _tupleTypes = new[] {
            typeof(KeyValuePair<,>),
            typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>), typeof(Tuple<,,,,,,>), typeof(Tuple<,,,,,,,>),
            typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>)
        };

        public object Deserialize(Type type, TElement elem)
        {
            _doAtTheEnd = new List<Action>();
            var resultFunc = CustomCallStack.Run(deserialize(type, elem, null, _options.EnforceEnums, "this"));
            foreach (var action in _doAtTheEnd)
                action();
            var result = resultFunc();
            if (result is ClassifyError ce)
            {
                _options.Errors.Add(ce);
                return type.GetDefaultValue();
            }
            return type.IsEnum && _options.EnforceEnums && !allowEnumValue(type, result) ? type.GetDefaultValue() : result;
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
                    if (retrievedObj is IClassifyObjectProcessor objP)
                        objP.AfterDeserialize();
                    if (retrievedObj is IClassifyObjectProcessor<TElement> objT)
                        objT.AfterDeserialize(elem);
                    typeOptions?.AfterDeserialize(retrievedObj, elem);
                }
                return retrievedObj;
            };
        }

        public void DeserializeIntoObject(Type type, TElement elem, object intoObj, string objectPath)
        {
            if (_options._typeOptions.TryGetValue(type, out var typeOptions))
            {
                if (typeOptions.Substitutor != null)
                    throw new InvalidOperationException("Cannot use type substitution when populating a provided object.");
                typeOptions?.BeforeDeserialize(elem);
            }

            _doAtTheEnd = new List<Action>();

            if (_format.IsReferable(elem))
                rememberDeser[_format.GetReferenceID(elem)] = new declassifyRememberedObject { WithoutDesubstitution = () => intoObj };

            var cat = tryGetCollectionInfo(type, out var keyType, out var valueType);
            var result =
                cat == null ? CustomCallStack.Run(deserializeIntoObject(elem, intoObj, type, objectPath)) :
                cat == collectionCategory.Array ? CustomCallStack.Run(deserializeIntoArray(type, valueType, elem, (Array) intoObj, _options.EnforceEnums, objectPath)) :
                cat == collectionCategory.SimpleKeyedDictionary ? CustomCallStack.Run(deserializeIntoDictionary(keyType, valueType, elem, intoObj, _options.EnforceEnums, objectPath)) :
                CustomCallStack.Run(deserializeIntoCollection(valueType, cat.Value, elem, intoObj, _options.EnforceEnums, objectPath));

            foreach (var action in _doAtTheEnd)
                action();
            result();

            if (intoObj is IClassifyObjectProcessor objP)
                objP.AfterDeserialize();
            if (intoObj is IClassifyObjectProcessor<TElement> objT)
                objT.AfterDeserialize(elem);
            typeOptions?.AfterDeserialize(intoObj, elem);
        }

        private Func<object> reportError(Exception e, string objectPath)
        {
            if (_options == null || _options.Errors == null)
                throw e;
            return () => new ClassifyError(e, objectPath);
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
        /// <param name="enforceEnums">
        ///     <c>true</c> if <c>[ClassifyEnforceEnums]</c> semantics are in effect for this object.</param>
        /// <param name="objectPath">
        ///     Describes the chain of objects leading up to here.</param>
        private WorkNode<Func<object>> deserialize(Type declaredType, TElement elem, object already, bool enforceEnums, string objectPath)
        {
            if (declaredType.IsPointer || declaredType.IsByRef)
                return _ => reportError(new NotSupportedException("Classify cannot deserialize pointers or by-reference variables."), objectPath);

            var substType = declaredType;
            var hasTypeSubstitution = false;

            if (_options._typeOptions.TryGetValue(declaredType, out var typeOptions))
            {
                if (typeOptions.Substitutor != null)
                {
                    substType = typeOptions.Substitutor.SubstituteType;
                    hasTypeSubstitution = true;
                }
                typeOptions?.BeforeDeserialize(elem);
            }

            // Every object created by declassify goes through this function
            var cleanUp = Ut.Lambda((Func<object> res) =>
            {
                var withoutDesubstitution = cachify(res, elem, typeOptions);
                Func<object> withDesubstitution = null;

                // Apply de-substitution (if any)
                if (hasTypeSubstitution)
                    withDesubstitution = cachify(() =>
                    {
                        try { return typeOptions.Substitutor.FromSubstitute(withoutDesubstitution()); }
                        catch (ClassifyDesubstitutionFailedException) { return already; }
                    }, elem, typeOptions);

                // Remember the result if something else refers to it
                if (_format.IsReferable(elem))
                    rememberDeser[_format.GetReferenceID(elem)] = new declassifyRememberedObject
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
                    if (!rememberDeser.TryGetValue(refID, out var inf))
                        _format.ThrowMissingReferable(refID);
                    if (declaredType == substType)
                        return inf.WithoutDesubstitution();
                    inf.WithDesubstitution ??= cachify(() =>
                    {
                        try { return typeOptions.Substitutor.FromSubstitute(inf.WithoutDesubstitution()); }
                        catch (ClassifyDesubstitutionFailedException) { return already; }
                    }, elem, typeOptions);
                    return inf.WithDesubstitution();
                });
            }

            var serializedType = substType;
            var (typeName, isFullType) = _format.GetType(elem);
            if (typeName != null)
            {
                if (isFullType)
                {
                    try { serializedType = Type.GetType(typeName); }
                    catch { serializedType = null; }
                    serializedType ??= Type.GetType(typeName, asmName => AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(asm => asm.GetName().Name == asmName.Name && asm.GetName().Version >= asmName.Version), null);
                }
                else
                    serializedType = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(asm => asm.GetType(typeName) ?? asm.GetType((substType.Namespace == null ? null : substType.Namespace + ".") + typeName))
                        .Where(t => t != null)
                        .FirstOrDefault();
                if (serializedType == null)
                    return _ => reportError(new Exception($"The type {typeName} needed for deserialization cannot be found."), objectPath);
            }
            var genericDefinition = serializedType.IsGenericType ? serializedType.GetGenericTypeDefinition() : null;

            if (_format.IsNull(elem))
                return _ => cleanUp(() => null);
            else if (typeof(TElement).IsAssignableFrom(serializedType))
                return _ => cleanUp(() => _format.GetSelfValue(elem));
            else if (_simpleTypes.Contains(serializedType) || ExactConvert.IsSupportedType(serializedType))
                return _ => cleanUp(() => ExactConvert.To(serializedType, _format.GetSimpleValue(elem)));
            else if (serializedType == typeof(byte[]))
                return _ => cleanUp(() => _format.GetRawData(elem));
            else if (genericDefinition == typeof(Nullable<>))
            {
                // It’s a nullable type, just determine the inner type and start again
                return deserialize(serializedType.GetGenericArguments()[0], elem, already, enforceEnums, objectPath);
            }
            else if (genericDefinition != null && _tupleTypes.Contains(genericDefinition))
            {
                // It’s a Tuple or KeyValuePair
                var genericArguments = serializedType.GetGenericArguments();
                var tupleParams = new Func<object>[genericArguments.Length];
                TElement[] values;
                if (genericDefinition == typeof(KeyValuePair<,>))
                {
                    var (key, value) = _format.GetKeyValuePair(elem);
                    values = new[] { key, value };
                }
                else
                    values = _format.GetList(elem, genericArguments.Length).ToArray();

                if (genericArguments.Length > values.Length)
                    return _ => reportError(new InvalidOperationException($"While trying to deserialize a tuple with {genericArguments.Length} elements, Classify encountered a serialized form with only {values.Length} elements."), objectPath);

                var constructor = serializedType.GetConstructor(genericArguments);
                if (constructor == null)
                    return _ => reportError(new InvalidOperationException("Could not find expected Tuple constructor."), objectPath);
                var constructorParameters = constructor.GetParameters();

                int i = -1;
                return prevResult =>
                {
                    if (i >= 0)
                        tupleParams[i] = prevResult;
                    i++;
                    if (i < genericArguments.Length && i < values.Length)
                        return deserialize(genericArguments[i], values[i], null, enforceEnums, $"{objectPath}.{constructorParameters[i].Name}");
                    return cleanUp(() =>
                    {
                        var args = new object[tupleParams.Length];
                        for (var j = 0; j < args.Length; j++)
                        {
                            var value = tupleParams[j]();
                            if (value is ClassifyError ce)
                            {
                                _options.Errors.Add(ce);
                                args[j] = genericArguments[j].GetDefaultValue();
                            }
                            else
                                args[j] = value;
                        }
                        return constructor.Invoke(args);
                    });
                };
            }
            else
            {
                // Check if it’s an array, collection or dictionary
                var cat = tryGetCollectionInfo(serializedType, out var keyType, out var valueType);
                WorkNode<Func<object>> workNode;
                switch (cat)
                {
                    case collectionCategory.SimpleKeyedDictionary:
                        workNode = deserializeIntoDictionary(keyType, valueType, elem, already ?? Activator.CreateInstance(genericDefinition == typeof(IDictionary<,>) ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : serializedType), enforceEnums, objectPath);
                        break;

                    case collectionCategory.Array:
                        workNode = deserializeIntoArray(serializedType, valueType, elem, null, enforceEnums, objectPath);
                        break;

                    case collectionCategory.Stack:
                        workNode = deserializeIntoCollection(valueType, collectionCategory.Stack, elem, already ?? Activator.CreateInstance(typeof(Stack<>).MakeGenericType(valueType)), enforceEnums, objectPath);
                        break;

                    case collectionCategory.Queue:
                        workNode = deserializeIntoCollection(valueType, collectionCategory.Queue, elem, already ?? Activator.CreateInstance(typeof(Queue<>).MakeGenericType(valueType)), enforceEnums, objectPath);
                        break;

                    case collectionCategory.Other:
                        workNode = deserializeIntoCollection(valueType, collectionCategory.Other, elem, already ?? Activator.CreateInstance(genericDefinition == typeof(ICollection<>) || genericDefinition == typeof(IList<>) ? typeof(List<>).MakeGenericType(valueType) : serializedType), enforceEnums, objectPath);
                        break;

                    case null:
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
                            return _ => reportError(new Exception($"An object of type {serializedType.FullName} could not be created:\n{e.Message}", e), objectPath);
                        }

                        workNode = deserializeIntoObject(elem, ret, serializedType, objectPath);
                        break;

                    default:
                        return _ => reportError(new Exception(@"An internal bug in Classify was encountered. (56049)"), objectPath);
                }

                var first = true;
                return prevResult =>
                {
                    if (first)
                    {
                        first = false;
                        return workNode;
                    }
                    return cleanUp(prevResult);
                };
            }
        }

        /// <summary>
        ///     Deserializes a simple-keyed dictionary from its serialized form.</summary>
        /// <param name="keyType">
        ///     The type of the keys in the dictionary.</param>
        /// <param name="valueType">
        ///     The type of the values in the dictionary.</param>
        /// <param name="elem">
        ///     The serialized form.</param>
        /// <param name="already">
        ///     A dictionary instance to populate. This must not be <c>null</c>.</param>
        /// <param name="enforceEnums">
        ///     <c>true</c> if <c>[ClassifyEnforceEnums]</c> semantics are in effect for this object.</param>
        /// <param name="objectPath">
        ///     Describes the chain of objects leading up to here.</param>
        private WorkNode<Func<object>> deserializeIntoDictionary(Type keyType, Type valueType, TElement elem, object already, bool enforceEnums, string objectPath)
        {
            if (already is IClassifyObjectProcessor<TElement> dict)
                dict.BeforeDeserialize(elem);

            typeof(ICollection<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType)).GetMethod("Clear", Type.EmptyTypes).Invoke(already, null);
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
                    return deserialize(valueType, e.Current.Value, null, enforceEnums, $"{objectPath}[{(e.Current.Key is int result ? result.ToString() : $@"""{ExactConvert.ToString(e.Current.Key).CLiteralEscape()}""")}]");
                }

                Ut.Assert(keysToAdd.Count == valuesToAdd.Count);
                _doAtTheEnd.Add(() =>
                {
                    for (int i = 0; i < keysToAdd.Count; i++)
                    {
                        var keyToAdd = keysToAdd[i];
                        var valueToAdd = valuesToAdd[i]();
                        if (valueToAdd is ClassifyError ce)
                            _options.Errors.Add(ce);
                        else if (!enforceEnums || ((!keyType.IsEnum || allowEnumValue(keyType, keyToAdd)) && (!valueType.IsEnum || allowEnumValue(valueType, valueToAdd))))
                            addMethod.Invoke(already, new object[] { keyToAdd, valueToAdd });
                    }
                });
                return new Func<object>(() => already);
            };
        }

        /// <summary>
        ///     Deserializes a (single- or multi-dimensional) array from its serialized form.</summary>
        /// <param name="type">
        ///     The array type.</param>
        /// <param name="valueType">
        ///     The type of the values in the array.</param>
        /// <param name="elem">
        ///     The serialized form.</param>
        /// <param name="already">
        ///     An array to populate. This may be <c>null</c>, in which case a new array is instantiated.</param>
        /// <param name="enforceEnums">
        ///     <c>true</c> if <c>[ClassifyEnforceEnums]</c> semantics are in effect for this object.</param>
        /// <param name="objectPath">
        ///     Describes the chain of objects leading up to here.</param>
        private WorkNode<Func<object>> deserializeIntoArray(Type type, Type valueType, TElement elem, Array already, bool enforceEnums, string objectPath)
        {
            // The array may be a multi-dimensional one.
            var rank = type.GetArrayRank();
            var lengths = already == null ? new int[rank] : Ut.NewArray(rank, already.GetLength);

            // STEP 1 (done here): Generate an object[] of object[]s of object[]s containing the still-serialized elements (TElement).
            // At the same time, if ‘lengths’ isn’t already populated from ‘already’, populate it from the serialized form.
            object recurse(bool first, int rnk, TElement el)
            {
                var thisList = _format.GetList(el, null).ToArray();
                if (first && already == null)
                    lengths[rnk] = thisList.Length;
                else if (lengths[rnk] != thisList.Length)
                    throw new InvalidOperationException(already == null
                        ? @"The serialized form contains a multi-dimensional array in which the sizes of each dimension are inconsistent."
                        : @"The array size of the serialized form does not match the size of the provided array object.");
                var newList = new object[thisList.Length];
                rnk++;
                for (int i = 0; i < thisList.Length; i++)
                    newList[i] = rnk == rank ? thisList[i] : recurse(i == 0, rnk, thisList[i]);
                return newList;
            }

            // If ‘lengths’ isn’t already populated from ‘already’, this call populates it from the serialized form
            object[] arrays;
            try { arrays = (object[]) recurse(true, 0, elem); }
            catch (Exception e) { return _ => reportError(e, objectPath); }

            // This requires ‘lengths’ to be populated
            already ??= Array.CreateInstance(type.GetElementType(), lengths);

            // If any of the lengths are 0, the array contains no elements that need deserialization.
            if (lengths.Contains(0))
                return prevResult => new Func<object>(() => already);

            // STEP 2 (done using CustomCallStack): Deserialize the innermost TElement objects using deserialize(),
            // which gives a Func<object>, and store those Func<object>s in the same place, overwriting the TElements
            int[] ixs = null;
            return prevResult =>
            {
                if (ixs != null)
                {
                    // Put the deserialization result in the right location in the right array
                    object el1 = arrays;
                    for (int ix = 0; ix < rank - 1; ix++)
                        el1 = ((object[]) el1)[ixs[ix]];
                    ((object[]) el1)[ixs[rank - 1]] = prevResult;

                    // Move on to next coordinates
                    var r = rank - 1;
                    while (r >= 0 && ixs[r] == lengths[r] - 1)
                    {
                        ixs[r] = 0;
                        r--;
                    }

                    if (r < 0)
                    {
                        // We’ve called deserialize on everything.
                        // STEP 3 (done at the end): put all the deserialized elements into the array.
                        _doAtTheEnd.Add(() =>
                        {
                            void recurse3(int[] indices, int rnk, object obj)
                            {
                                if (rnk == rank)
                                {
                                    var valueToAdd = ((Func<object>) obj)();
                                    if (valueToAdd is ClassifyError ce)
                                        _options.Errors.Add(ce);
                                    else if (!enforceEnums || !valueType.IsEnum || allowEnumValue(valueType, valueToAdd))
                                        already.SetValue(valueToAdd, indices);
                                }
                                else
                                {
                                    for (int i = 0; i < lengths[rnk]; i++)
                                    {
                                        indices[rnk] = i;
                                        recurse3(indices, rnk + 1, ((object[]) obj)[i]);
                                    }
                                }
                            }

                            recurse3(ixs, 0, arrays);
                        });
                        return new Func<object>(() => already);
                    }
                    ixs[r]++;
                }
                else
                    ixs = new int[rank];

                // Deserialize the next element
                object el2 = arrays;
                for (int ix = 0; ix < rank; ix++)
                    el2 = ((object[]) el2)[ixs[ix]];
                return deserialize(valueType, (TElement) el2, null, enforceEnums, $"{objectPath}[{ixs.JoinString(", ")}]");
            };
        }

        /// <summary>
        ///     Deserializes a collection from its serialized form. This includes dictionaries not already covered by <see
        ///     cref="deserializeIntoDictionary"/>, but not arrays, which are covered by <see cref="deserializeIntoArray"/>.</summary>
        /// <param name="valueType">
        ///     The type of the values in the array.</param>
        /// <param name="cat">
        ///     The category of collection.</param>
        /// <param name="elem">
        ///     The serialized form.</param>
        /// <param name="already">
        ///     A collection instance to populate. This must not be <c>null</c>.</param>
        /// <param name="enforceEnums">
        ///     <c>true</c> if <c>[ClassifyEnforceEnums]</c> semantics are in effect for this object.</param>
        /// <param name="objectPath">
        ///     Describes the chain of objects leading up to here.</param>
        private WorkNode<Func<object>> deserializeIntoCollection(Type valueType, collectionCategory cat, TElement elem, object already, bool enforceEnums, string objectPath)
        {
            if (already is IClassifyObjectProcessor<TElement> list)
                list.BeforeDeserialize(elem);

            var baseType = cat == collectionCategory.Stack ? typeof(Stack<>) : cat == collectionCategory.Queue ? typeof(Queue<>) : typeof(ICollection<>);
            baseType.MakeGenericType(valueType).GetMethod("Clear", Type.EmptyTypes).Invoke(already, null);
            var addMethod = baseType.MakeGenericType(valueType).GetMethod(cat == collectionCategory.Stack ? "Push" : cat == collectionCategory.Queue ? "Enqueue" : "Add", new Type[] { valueType });
            var e = _format.GetList(elem, null).GetEnumerator();
            var adders = new List<Func<object>>();
            var ix = -1;
            return prevResult =>
            {
                if (ix >= 0)
                    adders.Add(prevResult);
                ix++;
                if (e.MoveNext())
                    return deserialize(valueType, e.Current, null, enforceEnums, $"{objectPath}[{ix}]");
                _doAtTheEnd.Add(() =>
                {
                    foreach (var adder in adders)
                    {
                        var valueToAdd = adder();
                        if (valueToAdd is ClassifyError ce)
                            _options.Errors.Add(ce);
                        else if (!enforceEnums || !valueType.IsEnum || allowEnumValue(valueType, valueToAdd))
                            addMethod.Invoke(already, new object[] { valueToAdd });
                    }
                });
                return new Func<object>(() => already);
            };
        }

        /// <summary>
        ///     Determines whether <paramref name="type"/> is a supported collection type, and if so, which category of
        ///     collections it belongs to.</summary>
        /// <param name="type">
        ///     The type to examine.</param>
        /// <param name="keyType">
        ///     Receives the type of the keys if <paramref name="type"/> turns out to be a dictionary.</param>
        /// <param name="valueType">
        ///     Receives the type of the values if <paramref name="type"/> turns out to be a collection.</param>
        /// <returns>
        ///     <c>null</c> if the type is not a supported collection type, otherwise the category of collection.</returns>
        private static collectionCategory? tryGetCollectionInfo(Type type, out Type keyType, out Type valueType)
        {
            keyType = null;
            valueType = null;

            if (type.IsArray)
            {
                valueType = type.GetElementType();
                return collectionCategory.Array;
            }
            else if (type.TryGetGenericParameters(typeof(IDictionary<,>), out var typeParams) && (typeParams[0].IsEnum || _simpleTypes.Contains(typeParams[0])))
            {
                // Dictionaries which are stored specially (key is a simple type).
                // (More complex dictionaries are classified by treating them as an ICollection<KeyValuePair<K,V>>)
                keyType = typeParams[0];
                valueType = typeParams[1];
                return collectionCategory.SimpleKeyedDictionary;
            }
            else if (type.TryGetGenericParameters(typeof(ICollection<>), out typeParams))
            {
                valueType = typeParams[0];
                return collectionCategory.Other;
            }
            else if (type.TryGetGenericParameters(typeof(Queue<>), out typeParams))
            {
                valueType = typeParams[0];
                return collectionCategory.Queue;
            }
            else if (type.TryGetGenericParameters(typeof(Stack<>), out typeParams))
            {
                valueType = typeParams[0];
                return collectionCategory.Stack;
            }

            return null;
        }

        private struct deserializeFieldInfo
        {
            public FieldInfo FieldToAssignTo;
            public TElement ElementToAssign;
            public bool EnforceEnum;
            public Type DeserializeAsType;
            public Func<object, object> SubstituteConverter;
        }

        private WorkNode<Func<object>> deserializeIntoObject(TElement elem, object intoObj, Type type, string objectPath)
        {
            if (intoObj is IClassifyObjectProcessor<TElement> objT)
                objT.BeforeDeserialize(elem);

            var infos = new List<deserializeFieldInfo>();
            var globalClassifyName = type.GetCustomAttributes<ClassifyNameAttribute>().FirstOrDefault();
            if (globalClassifyName != null && globalClassifyName.SerializedName != null)
                throw new InvalidOperationException("A [ClassifyName] attribute on a type can only specify a ClassifyNameConvention, not an alternative name.");
            var usedFieldNames = new HashSet<(string fieldName, Type delaringType)>();

            foreach (var field in type.GetAllFields())
            {
                // Skip readonly members if requested via options
                if (_options.IgnoreReadonlyMembers && field.IsInitOnly)
                    continue;

                string rFieldName = field.Name.TrimStart('_');
                MemberInfo getAttrsFrom = field;

                if (rFieldName[0] == '<' && rFieldName.EndsWith(">k__BackingField"))
                {
                    // Compiler-generated fields for auto-implemented properties 
                    rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>k__BackingField".Length);
                    var prop = type.GetAllProperties().FirstOrDefault(p => p.Name == rFieldName);
                    if (prop != null)
                        getAttrsFrom = prop;
                }
                else if (rFieldName[0] == '<' && rFieldName.EndsWith(">i__Field"))
                {
                    // Fields in anonymous types
                    rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>i__Field".Length);
                }

                // Skip events
                if (type.GetEvent(rFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) != null)
                    continue;

                // [ClassifyIgnore]
                if (hasClassifyAttribute<ClassifyIgnoreAttribute>(getAttrsFrom))
                    continue;

                // [ClassifyName]
                var classifyName = getClassifyAttributeOrNull<ClassifyNameAttribute>(getAttrsFrom);
                if (classifyName != null || globalClassifyName != null)
                    rFieldName = (classifyName ?? globalClassifyName).TransformName(rFieldName);

                if (!usedFieldNames.Add((rFieldName, field.DeclaringType)))
                    throw new InvalidOperationException("The use of [ClassifyName] attributes has caused a duplicate field name. Make sure that no [ClassifyName] attribute conflicts with another [ClassifyName] attribute or another unmodified field name.");

                var fieldDeclaringType = field.DeclaringType.AssemblyQualifiedName;

                // Fields with no special attributes (except perhaps [ClassifySubstitute])
                if (_format.HasField(elem, rFieldName, fieldDeclaringType))
                {
                    var value = _format.GetField(elem, rFieldName, fieldDeclaringType);
                    if (!_format.IsNull(value) || !hasClassifyAttribute<ClassifyNotNullAttribute>(getAttrsFrom))
                    {
                        var inf = new deserializeFieldInfo
                        {
                            FieldToAssignTo = field,
                            DeserializeAsType = field.FieldType,
                            ElementToAssign = value,
                            EnforceEnum = hasClassifyAttribute<ClassifyEnforceEnumAttribute>(getAttrsFrom),
                        };

                        // [ClassifySubstitute]
                        var substituteAttr = getClassifyAttributeOrNull<ClassifySubstituteAttribute>(getAttrsFrom);
                        if (substituteAttr != null)
                        {
                            var substitutor = substituteAttr.GetSubstitutor(field.FieldType);
                            inf.DeserializeAsType = substitutor.SubstituteType;
                            inf.SubstituteConverter = substitutor.FromSubstitute;
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
                    return deserialize(infos[i].DeserializeAsType, infos[i].ElementToAssign, infos[i].SubstituteConverter == null ? infos[i].FieldToAssignTo.GetValue(intoObj) : null, infos[i].EnforceEnum, $"{objectPath}.{infos[i].FieldToAssignTo.Name}");

                _doAtTheEnd.Add(() =>
                {
                    for (int j = 0; j < infos.Count; j++)
                    {
                        var valueToAssign = valuesToAssign[j]();
                        if (valueToAssign is ClassifyError ce)
                            _options.Errors.Add(ce);
                        else
                        {
                            if (infos[j].SubstituteConverter != null)
                                valueToAssign = infos[j].SubstituteConverter(valueToAssign);
                            if (!infos[j].FieldToAssignTo.FieldType.IsEnum || !infos[j].EnforceEnum || allowEnumValue(infos[j].FieldToAssignTo.FieldType, valueToAssign))
                                infos[j].FieldToAssignTo.SetValue(intoObj, valueToAssign);
                        }
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
            var saveType = declaredType;
            string typeStr = null;
            bool typeStrIsFull = false;
            if (saveObject != null)
            {
                saveType = saveObject.GetType();
                if (saveType == typeof(IntPtr) || saveType == typeof(Pointer))
                    throw new NotSupportedException($"Classify does not support serializing values of type \"{saveType}\". Consider marking the offending field with [ClassifyIgnore].");
                if (declaredType != saveType && !(saveType.IsValueType && declaredType == typeof(Nullable<>).MakeGenericType(saveType)))
                {
                    // ... but only add this attribute if it is not a collection, because then Classify doesn’t care about the type when restoring the object anyway
                    if (!declaredType.IsArray && !declaredType.TryGetGenericParameters(typeof(IDictionary<,>), out var typeParameters) && !declaredType.TryGetGenericParameters(typeof(ICollection<>), out typeParameters))
                    {
                        if (saveType.Assembly.Equals(declaredType.Assembly) && !saveType.IsGenericType && !saveType.IsNested)
                            typeStr = Equals(saveType.Namespace, declaredType.Namespace) && !saveType.IsArray ? saveType.Name : saveType.FullName;
                        else
                        {
                            typeStr = saveType.AssemblyQualifiedName;
                            typeStrIsFull = true;
                        }
                    }
                }
            }

            // Preserve reference equality of objects before type substitution
            if (saveObject != null && rememberSer.TryGetValue(saveObject, out var previousOriginalObject))
            {
                if (_options.SerializationEqualityComparer.Equals(previousOriginalObject, saveObject))
                {
                    if (!requireRefId.TryGetValue(previousOriginalObject, out var refId))
                    {
                        refId = _nextId;
                        _nextId++;
                        requireRefId[previousOriginalObject] = refId;
                    }
                    return () => _format.FormatReference(refId);
                }
                else
                {
                    // Detected a cycle in an object that the user indicated should not be deduplicated
                    throw new InvalidOperationException($@"The object {previousOriginalObject} (of type {previousOriginalObject.GetType().FullName}) is part of a cycle, but Classify is configured (via ClassifyOptions.SerializationEqualityComparer) to not use reference identity on this object, so the cycle cannot be serialized.");
                }
            }

            // See if there’s a substitute type defined
            var originalObject = saveObject;
            var originalType = saveType;

            if (_options._typeOptions.TryGetValue(saveType, out var typeOptions) && typeOptions.Substitutor != null)
            {
                saveObject = typeOptions.Substitutor.ToSubstitute(saveObject);
                saveType = typeOptions.Substitutor.SubstituteType;

                // Preserve reference identity of the substitute objects
                if (saveObject != null && rememberSer.TryGetValue(saveObject, out previousOriginalObject))
                {
                    if (_options.SerializationEqualityComparer.Equals(previousOriginalObject, saveObject))
                    {
                        if (!requireRefId.TryGetValue(previousOriginalObject, out var refId))
                        {
                            refId = _nextId;
                            _nextId++;
                            requireRefId[previousOriginalObject] = refId;
                        }
                        return () => _format.FormatReference(refId);
                    }
                    else
                    {
                        // Detected a cycle in an object that the user indicated should not be deduplicated
                        throw new InvalidOperationException($@"The object {previousOriginalObject} (of type {previousOriginalObject.GetType().FullName}), which is a substitute for {originalObject} (of type {originalObject.GetType().FullName}), is part of a cycle, but Classify is configured (via ClassifyOptions.SerializationEqualityComparer) to not use reference identity on this object, so the cycle cannot be serialized.");
                    }
                }

                // Remember the substituted object so that we can detect cycles and maintain reference equality
                if (saveObject != null)
                    rememberSer[saveObject] = saveObject;
            }

            // Remember this object so that we can detect cycles and maintain reference equality
            if (originalObject != null)
                rememberSer[originalObject] = originalObject;

            if (saveObject == null)
                return () => _format.FormatNullValue();

            if (saveObject is IClassifyObjectProcessor objP)
                objP.BeforeSerialize();
            if (saveObject is IClassifyObjectProcessor<TElement> objT)
                objT.BeforeSerialize();
            typeOptions?.BeforeSerialize<TElement>(saveObject);

            if (typeof(TElement).IsAssignableFrom(saveType))
            {
                elem = () => _format.FormatSelfValue((TElement) saveObject);
                typeStr = null;
            }
            else if (_simpleTypes.Contains(saveType) || saveType.IsEnum)
                elem = () => _format.FormatSimpleValue(saveObject);
            else if (ExactConvert.IsSupportedType(saveType))
                elem = () => _format.FormatSimpleValue(ExactConvert.ToString(saveObject));
            else if (saveObject is byte[] byteArray)
                elem = () => _format.FormatRawData(byteArray);
            else
            {
                bool isStack = false;

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
                            // System.Tuple<>
                            var property = saveType.GetProperty("Item" + (i + 1), BindingFlags.Instance | BindingFlags.Public);
                            if (property != null)
                                return Serialize(property.GetValue(saveObject, null), genericArguments[i]);
                            // System.ValueType<>
                            var field = saveType.GetField("Item" + (i + 1), BindingFlags.Instance | BindingFlags.Public);
                            if (field != null)
                                return Serialize(field.GetValue(saveObject), genericArguments[i]);
                            throw new InvalidOperationException("Cannot find expected item property in Tuple type.");
                        }).ToArray();
                        elem = () => _format.FormatList(true, items.Select(item => item()));
                    }
                }
                else if (saveType.TryGetGenericParameters(typeof(IDictionary<,>), out var typeParameters) && (typeParameters[0].IsEnum || _simpleTypes.Contains(typeParameters[0])))
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
                else if (saveObject is Array saveArray && (saveArray.Rank > 1 || saveArray.GetLowerBound(0) != 0))
                {
                    var rank = saveArray.Rank;
                    var valueType = saveType.GetElementType();

                    // It’s a multi-dimensional array
                    for (int i = 0; i < saveArray.Rank; i++)
                        if (saveArray.GetLowerBound(i) != 0)
                            throw new NotSupportedException(@"Arrays with lower bounds other than zero are not supported by Classify.");

                    var lengths = new int[saveArray.Rank];
                    for (int rnk = 0; rnk < saveArray.Rank; rnk++)
                        lengths[rnk] = saveArray.GetLength(rnk);

                    // STEP 1: Serialize all the inner objects and organize them as jagged arrays.
                    // We have to do this before the next step so that object references work correctly.
                    Func<int[], int, object> recurse = null;
                    var ixs = new int[saveArray.Rank];
                    recurse = (indices, rnk) =>
                    {
                        if (rnk == rank)
                            return Serialize(saveArray.GetValue(indices), valueType);
                        var arr = new object[lengths[rnk]];
                        for (int i = 0; i < lengths[rnk]; i++)
                        {
                            indices[rnk] = i;
                            arr[i] = recurse(indices, rnk + 1);
                        }
                        return arr;
                    };
                    var arrays = recurse(ixs, 0);

                    // STEP 2: Call _format.FormatList() on everything.
                    TElement recurse2(object obj) => obj is object[] arr ? _format.FormatList(false, arr.Select(recurse2)) : ((Func<TElement>) obj)();
                    elem = () => recurse2(arrays);
                }
                else if (
                    (saveType.TryGetGenericParameters(typeof(ICollection<>), out typeParameters)) ||
                    (saveType.TryGetGenericParameters(typeof(Queue<>), out typeParameters)) ||
                    (saveType.TryGetGenericParameters(typeof(Stack<>), out typeParameters) && (isStack = true)) ||
                    saveType.IsArray)
                {
                    // It’s an array or collection. (Stack<T> and Queue<T> do not implement ICollection<T>.)
                    // Stack<T> reverses its own order, so we need to un-reverse it
                    var valueType = saveType.IsArray ? saveType.GetElementType() : typeParameters[0];
                    var items = ((IEnumerable) saveObject).Cast<object>().Select(val => Serialize(val, valueType)).ToArray();
                    elem = () => _format.FormatList(false, (isStack ? items.Reverse().Select(item => item()) : items.Select(item => item())));
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
                TElement retrievedElem = default;
                var previousElem = elem;
                elem = () =>
                {
                    if (!retrieved)
                    {
                        retrieved = true;
                        retrievedElem = previousElem();
                        if (requireRefId.TryGetValue(originalObject, out var refId) || requireRefId.TryGetValue(saveObject, out refId))
                            retrievedElem = _format.FormatReferable(retrievedElem, refId);
                        if (saveObject is IClassifyObjectProcessor<TElement> objE)
                            objE.AfterSerialize(retrievedElem);
                        typeOptions?.AfterSerialize(saveObject, retrievedElem);
                    }
                    return retrievedElem;
                };
            }

            // If the object is not considered reference-equal to itself, remove it from _rememberSer (we only put it in there to detect cycles).
            if (originalObject != null && !_options.SerializationEqualityComparer.Equals(originalObject, originalObject))
                rememberSer.Remove(originalObject);
            if (saveObject != null && !_options.SerializationEqualityComparer.Equals(saveObject, saveObject))
                rememberSer.Remove(saveObject);

            return elem;
        }

        private IEnumerable<ObjectFieldInfo<Func<TElement>>> serializeObject(object saveObject, Type saveType)
        {
            bool typeHasIgnoreIfDefault = saveType.IsDefinedOrIsNamedLike<ClassifyIgnoreIfDefaultAttribute>(true);
            bool typeHasIgnoreIfEmpty = saveType.IsDefinedOrIsNamedLike<ClassifyIgnoreIfEmptyAttribute>(true);

            var results = new List<Tuple<string, Type, Func<TElement>>>();
            var namesAlreadySeen = new HashSet<string>();
            var needsDeclaringType = new HashSet<string>();

            var globalClassifyName = saveType.GetCustomAttributes<ClassifyNameAttribute>().FirstOrDefault();
            if (globalClassifyName != null && globalClassifyName.SerializedName != null)
                throw new InvalidOperationException("A [ClassifyName] attribute on a type can only specify a ClassifyNameConvention, not an alternative name.");
            var usedFieldNames = new HashSet<(string fieldName, Type delaringType)>();

            foreach (var field in saveType.GetAllFields())
            {
                if (field.FieldType == saveType && saveType.IsValueType)
                    throw new InvalidOperationException($@"Cannot serialize an instance of the type {saveType.FullName} because it is a value type that contains itself.");

                // Ignore the backing field for events
                if (typeof(Delegate).IsAssignableFrom(field.FieldType) && saveType.GetEvent(field.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance) != null)
                    continue;

                // Skip readonly members if requested via options
                if (_options.IgnoreReadonlyMembers && field.IsInitOnly)
                    continue;

                string rFieldName = field.Name.TrimStart('_');
                MemberInfo getAttrsFrom = field;

                if (rFieldName[0] == '<' && rFieldName.EndsWith(">k__BackingField"))
                {
                    // Compiler-generated fields for auto-implemented properties 
                    rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>k__BackingField".Length);
                    var prop = saveType.GetAllProperties().FirstOrDefault(p => p.Name == rFieldName);
                    if (prop != null)
                        getAttrsFrom = prop;
                }
                else if (rFieldName[0] == '<' && rFieldName.EndsWith(">i__Field"))
                {
                    // Fields in anonymous types
                    rFieldName = rFieldName.Substring(1, rFieldName.Length - "<>i__Field".Length);
                }

                // [ClassifyIgnore]
                if (hasClassifyAttribute<ClassifyIgnoreAttribute>(getAttrsFrom))
                    continue;

                // [ClassifyName]
                var classifyName = getClassifyAttributeOrNull<ClassifyNameAttribute>(getAttrsFrom);
                if (classifyName != null || globalClassifyName != null)
                    rFieldName = (classifyName ?? globalClassifyName).TransformName(rFieldName);

                if (!usedFieldNames.Add((rFieldName, field.DeclaringType)))
                    throw new InvalidOperationException("The use of [ClassifyName] attributes has caused a duplicate field name. Make sure that no [ClassifyName] attribute conflicts with another [ClassifyName] attribute or another unmodified field name.");

                object saveValue = field.GetValue(saveObject);

                if (typeHasIgnoreIfDefault || hasClassifyAttribute<ClassifyIgnoreIfDefaultAttribute>(getAttrsFrom))
                {
                    if (saveValue == null)
                        continue;
                    if (field.FieldType.IsValueType &&
                        !(field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)) &&
                        saveValue.Equals(Activator.CreateInstance(field.FieldType)))
                        continue;
                }

                var ignoreIf = getClassifyAttributeOrNull<ClassifyIgnoreIfAttribute>(getAttrsFrom);
                if (ignoreIf != null && saveValue != null && saveValue.Equals(ignoreIf.Value))
                    continue;

                // Arrays, lists and dictionaries all implement ICollection
                if ((typeHasIgnoreIfEmpty || hasClassifyAttribute<ClassifyIgnoreIfEmptyAttribute>(getAttrsFrom)) && saveValue is ICollection collection && collection.Count == 0)
                    continue;

                if (!namesAlreadySeen.Add(rFieldName))
                    needsDeclaringType.Add(rFieldName);

                Func<TElement> elem;

                // [ClassifySubstitute]
                var substAttr = getClassifyAttributeOrNull<ClassifySubstituteAttribute>(getAttrsFrom);
                if (substAttr != null)
                {
                    var substitutor = substAttr.GetSubstitutor(field.FieldType);
                    elem = Serialize(substitutor.ToSubstitute(saveValue), substitutor.SubstituteType);
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
    ///     cref="PostBuildChecker.RunPostBuildChecks"/>.</summary>
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
    ///     cref="PostBuildChecker.RunPostBuildChecks"/>.</summary>
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
            rep.Error($"The type {type.FullName} does not have a parameterless constructor.", "class", type.Name);
            return;
        }
        postBuildStep(type, instance, null, rep, new HashSet<Type>(), Enumerable.Empty<string>());
    }

    private static void postBuildStep(Type type, object instance, MemberInfo member, IPostBuildReporter rep, HashSet<Type> alreadyChecked, IEnumerable<string> chain)
    {
        if (DefaultOptions._typeOptions.TryGetValue(type, out var opts) && opts.Substitutor != null)
        {
            postBuildStep(opts.Substitutor.SubstituteType, opts.Substitutor.ToSubstitute(instance), member, rep, alreadyChecked, chain.Concat("Type substitution: " + opts.Substitutor.SubstituteType.FullName));
            return;
        }

        if (!alreadyChecked.Add(type))
            return;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            postBuildStep(type.GetGenericArguments()[0], instance, member, rep, alreadyChecked, chain);
            return;
        }

        if (type.TryGetGenericParameters(typeof(IDictionary<,>), out var genericTypeArguments) || type.TryGetGenericParameters(typeof(ICollection<>), out genericTypeArguments))
        {
            foreach (var typeArg in genericTypeArguments)
                postBuildStep(typeArg, null, member, rep, alreadyChecked, chain.Concat("Dictionary type argument " + typeArg.FullName));
            return;
        }

        if (type == typeof(Pointer) || type == typeof(IntPtr) || type.IsPointer || type.IsByRef)
        {
            if (member == null)
                rep.Error($"Classify cannot serialize the type {type.FullName}. Use [ClassifyIgnore] to mark the field as not to be serialized. Chain: {string.Join(", ", chain)}");
            else
                rep.Error($"Classify cannot serialize the type {type.FullName}, used by field {member.DeclaringType.FullName}.{member.Name}. Use [ClassifyIgnore] to mark the field as not to be serialized. Chain: {string.Join(", ", chain)}", member.DeclaringType.Name, member.Name);
            return;
        }

        if (_simpleTypes.Contains(type))
            return; // these are safe

        if (!type.IsAbstract)
        {
            if (instance == null && !type.IsValueType && type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) == null)
                rep.Error(
                    string.Format("The {3} {0}.{1} is set to null by default, and its type, {2}, does not have a parameterless constructor. Assign a non-null instance to the field in {0}'s constructor or declare a parameterless constructor in {2}. (Chain: {4})",
                        member?.DeclaringType.FullName, member?.Name, type.FullName, member is FieldInfo ? "field" : "property", string.Join(", ", chain)),
                    member?.DeclaringType.Name, member?.Name);
            else
            {
                var inst = instance ?? (type.ContainsGenericParameters ? null : Activator.CreateInstance(type, true));
                foreach (var f in type.GetAllFields())
                {
                    MemberInfo m = f;
                    if (f.Name[0] == '<' && f.Name.EndsWith(">k__BackingField"))
                    {
                        var pName = f.Name.Substring(1, f.Name.Length - "<>k__BackingField".Length);
                        m = type.GetAllProperties().FirstOrDefault(p => p.Name == pName) ?? (MemberInfo) f;
                    }
                    // Skip events
                    else if (type.GetEvent(f.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) != null)
                        continue;

                    if (m.IsDefinedOrIsNamedLike<ClassifyIgnoreAttribute>())
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
/// <remarks>
///     This interface requires that the object type being serialized or deserialized implements it. If this is not possible,
///     use <see cref="IClassifyTypeProcessor{TElement}"/> instead.</remarks>
/// <seealso cref="IClassifyObjectProcessor"/>
public interface IClassifyObjectProcessor<TElement>
{
    /// <summary>
    ///     Pre-processes this object before <see cref="Classify"/> serializes it. This method is automatically invoked by
    ///     <see cref="Classify"/> and should not be called directly.</summary>
    void BeforeSerialize();

    /// <summary>
    ///     Post-processes the serialization produced by <see cref="Classify"/> for this object. This method is automatically
    ///     invoked by <see cref="Classify"/> and should not be called directly.</summary>
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
///     Contains methods to process an object before or after <see cref="Classify"/> (de)serializes it, irrespective of the
///     serialization format used. To have effect, this interface must be implemented by the object being serialized.</summary>
/// <remarks>
///     This interface requires that the object type being serialized or deserialized implements it. If this is not possible,
///     use <see cref="IClassifyTypeProcessor"/> instead.</remarks>
/// <seealso cref="IClassifyObjectProcessor{TElement}"/>
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
///     Contains methods to process all objects of a specific type and/or their serialized forms before or after <see
///     cref="Classify"/> serializes/deserializes them to/from a specific Classify format. To use this, create a type that
///     implements this interface and then pass an instance of that type to <see
///     cref="ClassifyOptions.AddTypeProcessor{TElement}"/>.</summary>
/// <typeparam name="TElement">
///     Type of the serialized form. For example, for an XML-based ClassifyFormat, this might be <see
///     cref="System.Xml.Linq.XElement"/>.</typeparam>
/// <remarks>
///     This interface has no effect when implemented by the object being serialized or deserialized. For that, use <see
///     cref="IClassifyObjectProcessor{TElement}"/>.</remarks>
/// <seealso cref="IClassifyTypeProcessor"/>
public interface IClassifyTypeProcessor<TElement>
{
    /// <summary>
    ///     Pre-processes the object before <see cref="Classify"/> serializes it. This method is automatically invoked by <see
    ///     cref="Classify"/> and should not be called directly.</summary>
    /// <param name="obj">
    ///     The object about to be serialized.</param>
    void BeforeSerialize(object obj);

    /// <summary>
    ///     Post-processes the serialization produced by <see cref="Classify"/> for this object. This method is automatically
    ///     invoked by <see cref="Classify"/> and should not be called directly.</summary>
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
///     Contains methods to process all objects of a specific type and/or their serialized forms before or after <see
///     cref="Classify"/> serializes/deserializes them, regardless of the Classify format. To use this, create a type that
///     implements this interface and then pass an instance of that type to <see cref="ClassifyOptions.AddTypeProcessor"/>.</summary>
/// <remarks>
///     This interface has no effect when implemented by the object being serialized or deserialized. For that, use <see
///     cref="IClassifyObjectProcessor"/>.</remarks>
/// <seealso cref="IClassifyTypeProcessor{TElement}"/>
public interface IClassifyTypeProcessor
{
    /// <summary>
    ///     Pre-processes the object before <see cref="Classify"/> serializes it. This method is automatically invoked by <see
    ///     cref="Classify"/> and should not be called directly.</summary>
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
///     Defines how to substitute a type for another type during Classify serialization/deserialization. Pass an instance of a
///     type implementing this interface to <see cref="ClassifyOptions.AddTypeSubstitution{TTrue, TSubstitute}"/> to use the
///     substitution throughout a serialization or deserialization, or use it in a <see cref="ClassifySubstituteAttribute"/>
///     to limit it to a specific field or automatically-implemented property.</summary>
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
    /// <exception cref="ClassifyDesubstitutionFailedException">
    ///     Implementors may throw this exception to communicate to Classify that the value is invalid and should be
    ///     disregarded, rather than causing the entire deserialization to fail.</exception>
    TTrue FromSubstitute(TSubstitute instance);
}

/// <summary>
///     When thrown by an implementation of <see cref="IClassifySubstitute{TTrue, TSubstitute}.FromSubstitute(TSubstitute)"/>,
///     communicates to Classify that the value is invalid and should be disregarded. Any other exception would be passed on
///     by Classify and would thus cause the entire deserialization to fail.</summary>
[Serializable]
public class ClassifyDesubstitutionFailedException : Exception
{
    /// <summary>Constructor.</summary>
    public ClassifyDesubstitutionFailedException() { }
}

/// <summary>
///     Provides an equality comparer that implements the default behavior for <see
///     cref="ClassifyOptions.SerializationEqualityComparer"/>.</summary>
public sealed class ClassifyEqualityComparer : IEqualityComparer<object>
{
    private ClassifyEqualityComparer() { }
    /// <summary>Provides the singleton instance of this class.</summary>
    public static IEqualityComparer<object> Instance = new ClassifyEqualityComparer();
    /// <summary>
    ///     Compares two arbitrary objects.</summary>
    /// <param name="x">
    ///     First object to compare.</param>
    /// <param name="y">
    ///     Second object to compare.</param>
    /// <returns>
    ///     <c>false</c> for strings and object reference equality for everything else.</returns>
    public new bool Equals(object x, object y) => x is not string && ReferenceEquals(x, y);
    /// <summary>
    ///     Returns a hash code for the specified object.</summary>
    /// <param name="obj">
    ///     The object to generate a hash code for.</param>
    /// <returns>
    ///     The object’s own hash code.</returns>
    public int GetHashCode(object obj) => obj.GetHashCode();
}

/// <summary>Provides the ability to specify some options for use by <see cref="Classify"/>.</summary>
public sealed class ClassifyOptions
{
    /// <summary>
    ///     This option is only relevant if the value you are deserializing is an enum value or a collection or dictionary
    ///     involving enum keys or values. If <c>true</c>, only enum values declared in the enum type are allowed (as if it
    ///     were a field with <see cref="ClassifyEnforceEnumAttribute"/>). Enum values inside of objects are not affected by
    ///     this option (but only by <see cref="ClassifyEnforceEnumAttribute"/>).</summary>
    /// <seealso cref="ClassifyEnforceEnumAttribute"/>
    public bool EnforceEnums = false;

    /// <summary>
    ///     If <c>null</c>, Classify will throw exceptions when encountering an error during deserialization. Otherwise,
    ///     errors are added to this list and deserialization continues. The field that caused the error remains at its
    ///     default value.</summary>
    public List<ClassifyError> Errors = null;

    /// <summary>
    ///     This option will cause Classify to ignore any auto-generated properties which have no setter, or any fields marked
    ///     with the C# “readonly” keyword.</summary>
    public bool IgnoreReadonlyMembers = false;

    /// <summary>
    ///     Provides a means to customize Classify’s definition of object equality, i.e. to control which objects are
    ///     serialized as references to each other and which ones are duplicated in the serialized form.</summary>
    /// <remarks>
    ///     <para>
    ///         The default comparer treats all strings as different, thus causing them to be re-serialized each time even if
    ///         they are in fact equal. All other objects are tested for reference equality. This way, object reference
    ///         equality is preserved across serializing and deserializing.</para>
    ///     <para>
    ///         An alternative may be, for example, to assign <c>EqualityComparer&lt;object&gt;.Default</c> here. This would
    ///         treat objects as equal if they implement <c>IEquatable&lt;T&gt;</c> and deem each other as equal. Such a
    ///         strategy would improve deduplication of information in the serialized form, but would cause the deserialized
    ///         form to re-use object instances more than the original did.</para></remarks>
    public IEqualityComparer<object> SerializationEqualityComparer
    {
        get { return _serializationEqualityComparer; }
        set
        {
            _serializationEqualityComparer = value;
            ActualEqualityComparer = new actualComparer(value);
        }
    }
    private IEqualityComparer<object> _serializationEqualityComparer;

    internal IEqualityComparer<object> ActualEqualityComparer { get; private set; }

    class actualComparer : IEqualityComparer<object>
    {
        private IEqualityComparer<object> _parent;
        public actualComparer(IEqualityComparer<object> parent) { _parent = parent; }
        public new bool Equals(object x, object y) => ReferenceEquals(x, y) || _parent.Equals(x, y);
        public int GetHashCode(object obj) => obj.GetHashCode();
    }

    /// <summary>Constructor.</summary>
    public ClassifyOptions()
    {
        SerializationEqualityComparer = ClassifyEqualityComparer.Instance;
    }

    internal Dictionary<Type, ClassifyTypeOptions> _typeOptions = new();

    /// <summary>
    ///     Adds a type substitution, instructing <see cref="Classify"/> to use a different type when serializing or
    ///     deserializing a specific type.</summary>
    /// <typeparam name="TTrue">
    ///     The true type to be replaced by a substitute type.</typeparam>
    /// <typeparam name="TSubstitute">
    ///     The substitute type to use during serialization/deserialization instead of <typeparamref name="TTrue"/>.</typeparam>
    /// <param name="substitutor">
    ///     An implementation of <see cref="IClassifySubstitute{TTrue, TSubstitute}"/> that defines the substitution.</param>
    /// <returns>
    ///     The same options object, allowing chaining.</returns>
    public ClassifyOptions AddTypeSubstitution<TTrue, TSubstitute>(IClassifySubstitute<TTrue, TSubstitute> substitutor)
    {
        if (substitutor == null)
            throw new ArgumentNullException(nameof(substitutor));
        if (_typeOptions.TryGetValue(typeof(TTrue), out var opt) && opt.Substitutor != null)
            throw new ArgumentException($"A substitution for type {typeof(TTrue).FullName} has already been defined.", nameof(substitutor));
        (opt ?? (_typeOptions[typeof(TTrue)] = new ClassifyTypeOptions())).Substitutor = new ClassifySubstitutor(typeof(TSubstitute), obj => substitutor.FromSubstitute((TSubstitute) obj), obj => substitutor.ToSubstitute((TTrue) obj));
        return this;
    }

    /// <summary>
    ///     Adds an instruction to <see cref="Classify"/> to run every object of type <paramref name="type"/> through the
    ///     specified <see cref="IClassifyTypeProcessor{TElement}"/> implementation before and after serializing or
    ///     deserializing to/from a Classify format that uses <typeparamref name="TElement"/> as its serialized form.</summary>
    /// <typeparam name="TElement">
    ///     The type of the serialized form. For example, for an XML-based ClassifyFormat, this might be <see
    ///     cref="System.Xml.Linq.XElement"/>.</typeparam>
    /// <param name="type">
    ///     The type of objects to run through the type processor.</param>
    /// <param name="processor">
    ///     An implementation of <see cref="IClassifyTypeProcessor{TElement}"/> that defines the operations to perform
    ///     before/after serialization/deserialization.</param>
    /// <returns>
    ///     The same options object, allowing chaining.</returns>
    public ClassifyOptions AddTypeProcessor<TElement>(Type type, IClassifyTypeProcessor<TElement> processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));
        (_typeOptions.Get(type, null) ?? (_typeOptions[type] = new ClassifyTypeOptions())).AddElementTypeProcessor(processor);
        return this;
    }

    /// <summary>
    ///     Adds an instruction to <see cref="Classify"/> to run every object of type <paramref name="type"/> through the
    ///     specified <see cref="IClassifyTypeProcessor"/> implementation before and after serializing or deserializing.</summary>
    /// <param name="type">
    ///     The type of objects to run through the type processor.</param>
    /// <param name="processor">
    ///     An implementation of <see cref="IClassifyTypeProcessor"/> that defines the operations to perform before/after
    ///     serialization/deserialization.</param>
    /// <returns>
    ///     The same options object, allowing chaining.</returns>
    public ClassifyOptions AddTypeProcessor(Type type, IClassifyTypeProcessor processor)
    {
        if (processor == null)
            throw new ArgumentNullException(nameof(processor));
        (_typeOptions.Get(type, null) ?? (_typeOptions[type] = new ClassifyTypeOptions())).AddTypeProcessor(processor);
        return this;
    }
}

internal sealed class ClassifySubstitutor
{
    public Type SubstituteType { get; private set; }
    public Func<object, object> FromSubstitute { get; private set; }
    public Func<object, object> ToSubstitute { get; private set; }
    public ClassifySubstitutor(Type substituteType, Func<object, object> fromSubstitute, Func<object, object> toSubstitute)
    {
        SubstituteType = substituteType;
        FromSubstitute = fromSubstitute;
        ToSubstitute = toSubstitute;
    }
}

internal sealed class ClassifyTypeOptions
{
    public ClassifySubstitutor Substitutor;
    public List<IClassifyTypeProcessor> TypeProcessors;
    public List<object> TypeElementProcessors;

    public void AddTypeProcessor(IClassifyTypeProcessor processor) => (TypeProcessors ??= new List<IClassifyTypeProcessor>()).Add(processor);
    public void AddElementTypeProcessor(object processor) => (TypeElementProcessors ??= new List<object>()).Add(processor);

    public void BeforeSerialize<TElement>(object obj)
    {
        if (TypeProcessors != null)
            foreach (var proc in TypeProcessors)
                proc.BeforeSerialize(obj);
        if (TypeElementProcessors != null)
            foreach (var proc in TypeElementProcessors.OfType<IClassifyTypeProcessor<TElement>>())
                proc.BeforeSerialize(obj);
    }

    public void AfterSerialize<TElement>(object obj, TElement elem)
    {
        if (TypeElementProcessors != null)
            foreach (var proc in TypeElementProcessors.OfType<IClassifyTypeProcessor<TElement>>())
                proc.AfterSerialize(obj, elem);
    }

    public void BeforeDeserialize<TElement>(TElement elem)
    {
        if (TypeElementProcessors != null)
            foreach (var proc in TypeElementProcessors.OfType<IClassifyTypeProcessor<TElement>>())
                proc.BeforeDeserialize(elem);
    }

    public void AfterDeserialize<TElement>(object obj, TElement elem)
    {
        if (TypeProcessors != null)
            foreach (var proc in TypeProcessors)
                proc.AfterDeserialize(obj);
        if (TypeElementProcessors != null)
            foreach (var proc in TypeElementProcessors.OfType<IClassifyTypeProcessor<TElement>>())
                proc.AfterDeserialize(obj, elem);
    }
}

/// <summary>
///     Indicates that the value stored in this field or automatically-implemented property should be converted to another
///     type when serializing and back when deserializing. This takes precedence over any type substitution configured in a
///     <see cref="ClassifyOptions"/> object.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ClassifySubstituteAttribute : Attribute
{
    /// <summary>Gets the type used to perform the type substitution.</summary>
    public Type ConverterType { get; private set; }

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="converterType">
    ///     Specifies a type that implements <see cref="IClassifySubstitute{TTrue,TSubstitute}"/>, where <c>TTrue</c> must be
    ///     the exact type of the field or automatically-implemented property bearing this attribute.</param>
    public ClassifySubstituteAttribute(Type converterType)
    {
        ConverterType = converterType;
    }

    internal ClassifySubstitutor GetSubstitutor(Type fieldType)
    {
        var converter = Activator.CreateInstance(ConverterType);
        var inter = ConverterType.FindInterfaces((itf, _) => itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(IClassifySubstitute<,>) && itf.GetGenericArguments()[0] == fieldType, null);
        if (inter.Length != 1)
            throw new InvalidOperationException($"Provided type in [ClassifySubstitute] attribute must implement interface IClassifySubstitute<,>, the first generic type argument must be {fieldType.FullName}, and there must be only one such interface.");

        // Can’t use Delegate.CreateDelegate() because the method could return a value type or void, which is not directly compatible with Func<object, object>
        var fromSubstituteMethod = inter[0].GetMethod("FromSubstitute");
        var toSubstituteMethod = inter[0].GetMethod("ToSubstitute");
        return new ClassifySubstitutor(
            substituteType: inter[0].GetGenericArguments()[1],
            fromSubstitute: new Func<object, object>(obj => fromSubstituteMethod.Invoke(converter, new[] { obj })),
            toSubstitute: new Func<object, object>(obj => toSubstituteMethod.Invoke(converter, new[] { obj })));
    }
}

/// <summary>
///     If this attribute is used on a field or automatically-implemented property, <see cref="Classify"/> omits its
///     serialization if the field’s or property’s value is equal to the specified value. See also remarks.</summary>
/// <remarks>
///     Using this together with <see cref="ClassifyIgnoreIfDefaultAttribute"/> will cause the distinction between the type’s
///     default value and the specified value to be lost.</remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ClassifyIgnoreIfAttribute : Attribute
{
    /// <summary>
    ///     Constructs an <see cref="ClassifyIgnoreIfAttribute"/> instance.</summary>
    /// <param name="value">
    ///     Specifies the value which causes a field or automatically-implemented property to be ignored.</param>
    public ClassifyIgnoreIfAttribute(object value) { Value = value; }
    /// <summary>Retrieves the value which causes a field or automatically-implemented property to be ignored.</summary>
    public object Value { get; private set; }
}

/// <summary>Specifies a naming convention for Classify to follow.</summary>
public enum ClassifyNameConvention
{
    /// <summary>Capitalize each word and upper-case the first letter.</summary>
    UpperCamelcase,
    /// <summary>Capitalize each word, but lower-case the first letter.</summary>
    LowerCamelcase,
    /// <summary>Use all lower-case.</summary>
    Lowercase,
    /// <summary>Use all upper-case.</summary>
    Uppercase,
    /// <summary>Use the underscore character (<c>_</c>) to delimit words.</summary>
    DelimiterSeparated,
}

/// <summary>
///     Use on a field or automatically-implemented property to override the default naming behavior of Classify. When used on
///     a type, affects the names of fields and automatically-implemented properties declared in that type, not the type name.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ClassifyNameAttribute : Attribute
{
    /// <summary>
    ///     An alternative name to use for this field in serialization, or <c>null</c> to use <see cref="Convention"/>
    ///     instead.</summary>
    public string SerializedName { get; set; }
    /// <summary>
    ///     The naming convention to apply to the field names, or <c>null</c> to use <see cref="SerializedName"/> instead.</summary>
    public ClassifyNameConvention? Convention { get; set; }

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="convention">
    ///     The naming convention to apply to the field names.</param>
    public ClassifyNameAttribute(ClassifyNameConvention convention)
    {
        Convention = convention;
    }

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="serializedName">
    ///     An alternative name to use for this field in serialization.</param>
    public ClassifyNameAttribute(string serializedName)
    {
        SerializedName = serializedName;
    }

    internal string TransformName(string original)
    {
        if (original == null)
            return null;

        if (SerializedName != null)
            return SerializedName;

        if (Convention == null)
            return original;

        return Convention.Value switch
        {
            ClassifyNameConvention.Uppercase => original.ToUpper(),
            ClassifyNameConvention.Lowercase => original.ToLower(),
            ClassifyNameConvention.UpperCamelcase => char.ToUpperInvariant(original[0]) + original.Substring(1),
            ClassifyNameConvention.LowerCamelcase => char.ToLowerInvariant(original[0]) + original.Substring(1),
            ClassifyNameConvention.DelimiterSeparated => Regex.Replace(original, @"(?<=[^\p{Lu}_])(?=\p{Lu})", "_"),
            _ => throw new ArgumentOutOfRangeException(nameof(Convention), Convention.Value.ToString()),
        };
    }
}

internal static class AttributeExtensions
{
    public static bool IsDefinedOrIsNamedLike<T>(this MemberInfo member, bool inherit = false)
    {
        return member.IsDefined(typeof(T), inherit) || member.GetCustomAttributes(inherit).Any(a => a.GetType().Name == typeof(T).Name);
    }
}

/// <summary>
///     Encapsulates an error encountered during deserialization with <see cref="Classify"/>. Note these errors are only
///     collected if <see cref="ClassifyOptions.Errors"/> is not <c>null</c>; otherwise they are thrown as exceptions.</summary>
public sealed class ClassifyError
{
    /// <summary>The exception encountered during deserialization.</summary>
    public Exception Exception { get; private set; }
    /// <summary>A string representing the chain of objects leading up to the object that encountered the error.</summary>
    public string ObjectPath { get; private set; }

    /// <summary>Constructor.</summary>
    public ClassifyError(Exception exception, string objectPath)
    {
        Exception = exception;
        ObjectPath = objectPath;
    }
}
