using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util
{
    static class AttributeTargetSets
    {
        public const AttributeTargets TypeDefinitions = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Interface;
        public const AttributeTargets DefinitionsWithNames = AttributeTargetSets.TypeDefinitions | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.GenericParameter;
        public const AttributeTargets DefinitionsWithAccessModifiers = AttributeTargetSets.TypeDefinitions | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event;
    }

    /// <summary>Instructs Rummage to keep a specific type, method, constructor or field.</summary>
    [AttributeUsage(AttributeTargetSets.TypeDefinitions | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoRemoveAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to keep the original name of a specific element.</summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithNames, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoRenameAttribute : Attribute
    {
    }

    /// <summary>
    ///     Instructs Rummage to keep the original name of a specific type, all of its members, and all the members in all of its
    ///     nested types.</summary>
    [AttributeUsage(AttributeTargetSets.TypeDefinitions, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoRenameAnythingAttribute : Attribute
    {
    }

    /// <summary>Instructs rummage to avoid un-nesting the specified type.</summary>
    [AttributeUsage(AttributeTargetSets.TypeDefinitions, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoUnnestAttribute : Attribute
    {
    }

    /// <summary>Instructs rummage to keep the original access modifier of a specific element.</summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithAccessModifiers, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoMarkPublicAttribute : Attribute
    {
    }

    /// <summary>
    ///     Instructs Rummage not to inline a specific method or property that would otherwise be automatically inlined. This
    ///     attribute takes precedence over <see cref="RummageInlineAttribute"/> if both are specified on the same method or
    ///     property.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoInlineAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to inline a specific method or property that would otherwise not be automatically inlined.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RummageInlineAttribute : Attribute
    {
    }



    /// <summary>Instructs Rummage to refrain from making any changes to a specific type.</summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithAccessModifiers | AttributeTargetSets.DefinitionsWithNames | AttributeTargetSets.TypeDefinitions, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe]
    public sealed class RummageKeepReflectionSafeAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to keep all the types reflection-safe which are passed in for the given generic parameter.</summary>
    [AttributeUsage(AttributeTargets.GenericParameter, Inherited = false, AllowMultiple = false)]
    public sealed class RummageKeepArgumentsReflectionSafeAttribute : Attribute
    {
    }

    /// <summary>
    ///     Use only on custom-attribute class declarations. Instructs Rummage to keep everything reflection-safe that uses the
    ///     given custom attribute.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RummageKeepUsersReflectionSafeAttribute : Attribute
    {
    }

    /// <summary>
    ///     Use on a method or constructor parameter of type "Type". Instructs Rummage that this method uses the Type passed in in
    ///     a way that is fully compatible with all obfuscations, including removing members not directly referenced, renaming
    ///     members, unnesting types and so on.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class RummageTypeUseIsSafeAttribute : Attribute
    {
    }

    /// <summary>Contains methods used to augment the program with Rummage-related information.</summary>
    public static class Rummage
    {
        /// <summary>
        ///     Returns the type passed in. Use around a <c>typeof(SomeType)</c> to override Rummage's reflection safety analysis
        ///     and make Rummage believe that this particular use is entirely safe.</summary>
        public static Type Safe([RummageTypeUseIsSafe] Type type) { return type; }
    }
}
