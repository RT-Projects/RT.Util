using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using RT.Util.ExtensionMethods;

namespace RT.Util.Lingo
{
    /// <summary>Holds the translation of a member's description.</summary>
    [LingoStringClass]
    public sealed class MemberDescriptionTr
    {
        /// <summary>How this member should be named in the property grid.</summary>
        public TrString DisplayName;
        /// <summary>How this member should be described in the description section of the property grid.</summary>
        public TrString Description;

        /// <summary>Override, for debugging purposes only.</summary>
        public override string ToString()
        {
            return DisplayName + ": " + Description;
        }
    }

    /// <summary>Holds the translation of a member's category and description.</summary>
    public sealed class MemberTr
    {
        /// <summary>Translation of the name of the category that this member belongs to.</summary>
        public TrString Category;
        /// <summary>How this member should be named in the property grid.</summary>
        public TrString DisplayName;
        /// <summary>How this member should be described in the description section of the property grid.</summary>
        public TrString Description;

        /// <summary>Constructor.</summary>
        public MemberTr(TrString category, MemberDescriptionTr memberDescription)
        {
            Category = category;
            DisplayName = memberDescription.DisplayName;
            Description = memberDescription.Description;
        }

        /// <summary>Constructor.</summary>
        public MemberTr(TrString category, TrString displayName, TrString description)
        {
            Category = category ?? "";
            DisplayName = displayName;
            Description = description ?? "";
        }

        /// <summary>Constructor.</summary>
        public MemberTr(TrString displayName)
        {
            DisplayName = displayName;
            Description = Category = "";
        }

        /// <summary>Constructor.</summary>
        public MemberTr(TrString displayName, TrString description)
        {
            DisplayName = displayName;
            Description = description;
            Category = "";
        }

        /// <summary>Override, for debugging purposes only.</summary>
        public override string ToString()
        {
            return Category + "/" + DisplayName + ": " + Description;
        }
    }

    /// <summary>Provides a type descriptor which supports translation of various string values using the Lingo conventions (see remarks).</summary>
    /// <typeparam name="TTranslation">The type of the translation class.</typeparam>
    /// <remarks>
    /// Every property requiring translatable name/description/category should have a corresponding method in the same class. This method
    /// should be static, named the same as the property with a "Tr" suffix, take a translation instance and return a <see cref="MemberTr"/>.
    /// </remarks>
    public sealed class LingoTypeDescriptionProvider<TTranslation> : TypeDescriptionProvider where TTranslation : TranslationBase
    {
        private Func<TTranslation> _getTranslation;

        /// <summary>Constructs a type description provider using the specified translation getter.</summary>
        /// <param name="getTranslation">A function that returns the currently active translation.</param>
        public LingoTypeDescriptionProvider(Func<TTranslation> getTranslation)
        {
            _getTranslation = getTranslation;
        }

        /// <summary>Override; see base.</summary>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return new LingoTypeDescriptor<TTranslation>(objectType, _getTranslation());
        }
    }

    sealed class LingoTypeDescriptor<TTranslation> : CustomTypeDescriptor where TTranslation : TranslationBase
    {
        private Type _objectType;
        private TTranslation _translation;

        public LingoTypeDescriptor(Type objectType, TTranslation translation)
            : base()
        {
            if (objectType == null)
                throw new ArgumentNullException(nameof(objectType));
            if (translation == null)
                throw new ArgumentNullException(nameof(translation));
            _objectType = objectType;
            _translation = translation;
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            var properties = new List<PropertyDescriptor>();
            foreach (var prop in _objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                properties.Add(new LingoPropertyDescriptor<TTranslation>(prop, _translation, _objectType));
            return new PropertyDescriptorCollection(properties.ToArray());
        }
    }

    sealed class LingoPropertyDescriptor<TTranslation> : PropertyDescriptor where TTranslation : TranslationBase
    {
        private PropertyInfo _pi;
        private TTranslation _translation;
        private MethodInfo _trMethod; // is null if there was no method with the expected name

        public LingoPropertyDescriptor(PropertyInfo pi, TTranslation translation, Type objectType)
            : base(pi.Name, new Attribute[0])
        {
            _pi = pi;
            _translation = translation;

            var methName = _pi.Name + "Tr";
            var candidates = objectType.SelectChain(t => t.BaseType == typeof(object) ? null : t.BaseType)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                .Where(m => m.Name == methName);
            if (!candidates.Any())
                return;

            _trMethod = candidates.Where(m => m.IsStatic &&
                    m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(TTranslation)) &&
                    typeof(MemberTr).IsAssignableFrom(m.ReturnType))
                .FirstOrDefault();
            if (_trMethod == null)
                throw new MissingMethodException("A method named “{0}” on the type “{1}” has the wrong signature. It must be static, have a parameter of type “{2}” (or a base type) and a return type of “{3}” (or a derived type).".Fmt(
                    methName, _pi.DeclaringType.FullName, typeof(TTranslation).FullName, typeof(MemberTr).FullName), methName);
        }

        public override string Category { get { return _trMethod == null ? "Misc" : ((MemberTr) _trMethod.Invoke(null, new object[] { _translation })).Category.Translation; } }
        public override string Description { get { return _trMethod == null ? _pi.Name : ((MemberTr) _trMethod.Invoke(null, new object[] { _translation })).Description.Translation; } }
        public override string DisplayName { get { return _trMethod == null ? _pi.Name : ((MemberTr) _trMethod.Invoke(null, new object[] { _translation })).DisplayName.Translation; } }
        public override string Name { get { return _pi.Name; } }

        public override Type PropertyType { get { return _pi.PropertyType; } }
        public override Type ComponentType { get { return _pi.DeclaringType; } }
        public override object GetValue(object component) { return _pi.GetValue(component, null); }
        public override bool IsBrowsable { get { var attr = _pi.GetCustomAttributes<BrowsableAttribute>(true).FirstOrDefault(); return attr == null || attr.Browsable; } }

        public override void SetValue(object component, object value)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Property is read-only.");
            _pi.SetValue(component, value, null);
        }

        public override void ResetValue(object component)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Property is read-only.");
            SetValue(component, _pi.PropertyType.IsValueType ? Activator.CreateInstance(_pi.PropertyType) : null);
        }

        public override bool IsReadOnly
        {
            get { return ((ReadOnlyAttribute) Attributes[typeof(ReadOnlyAttribute)]).IsReadOnly; }
        }

        public override bool CanResetValue(object component)
        {
            return IsReadOnly;
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }

    /// <summary>Provides a base implementation for enum converters intended to enable enum translation. See remarks.</summary>
    /// <typeparam name="TEnum">Type of the enum being translated.</typeparam>
    /// <typeparam name="TTranslation">Type of a translation class which has fields named the same as enum values.</typeparam>
    /// <remarks>Suggested use: define a separate Lingo string class for the enum. Inside this class, declare a nested class deriving
    /// from this base class. Specify a TypeConverter on the enum in question using the said nested class.</remarks>
    public abstract class LingoEnumConverter<TEnum, TTranslation> : EnumConverter
        where TEnum : struct
    {
        private Type _enumType, _trType;
        private Func<TTranslation> _getTranslation;

        /// <summary>Constructor.</summary>
        /// <param name="getTranslation">A method which returns the currently active translation for this enum.</param>
        public LingoEnumConverter(Func<TTranslation> getTranslation)
            : base(typeof(TEnum))
        {
            _enumType = typeof(TEnum);
            _trType = typeof(TTranslation);
            _getTranslation = getTranslation;

            if (!_enumType.IsEnum)
                throw new ArgumentException("The type \"{0}\" is not an enum type, and so cannot be used in \"{1}\".".Fmt(typeof(TEnum).FullName, GetType().FullName));
        }

        /// <summary>Override; see base.</summary>
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                var tr = _getTranslation();
                var val = (string) value;
                foreach (var field in _trType.GetFields())
                    if (field.FieldType == typeof(TrString) && ((TrString) field.GetValue(tr)).Translation == val)
                        return Enum.Parse(_enumType, field.Name);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>Override; see base.</summary>
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value != null)
            {
                var result = value.ToString();
                var field = _trType.GetField(result);
                if (field != null)
                    result = ((TrString) field.GetValue(_getTranslation())).Translation;
                return result;
            }
            else
                return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
