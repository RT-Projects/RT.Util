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
    [AttributeUsage(AttributeTargetSets.TypeDefinitions | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageNoRemoveAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to keep the original name of a specific element. </summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithNames, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageNoRenameAttribute : Attribute
    {
    }

    /// <summary>Instructs rummage to avoid un-nesting the specified type.</summary>
    [AttributeUsage(AttributeTargetSets.TypeDefinitions, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageNoUnnestAttribute : Attribute
    {
    }

    /// <summary>Instructs rummage to keep the original access modifier of a specific element.</summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithAccessModifiers, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageNoMarkPublicAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage not to inline a specific method or property that would otherwise be automatically inlined. This attribute takes precedence over <see cref="RummageInlineAttribute"/> if both are specified on the same method or property.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageNoInlineAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to inline a specific method or property that would otherwise not be automatically inlined.</summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageInlineAttribute : Attribute
    {
    }



    /// <summary>Instructs Rummage to refrain from making any changes to a specific type.</summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithAccessModifiers | AttributeTargetSets.DefinitionsWithNames | AttributeTargetSets.TypeDefinitions, Inherited = false, AllowMultiple = false), RummageKeepUsersReflectionSafe, RummageKeepReflectionSafe]
    public sealed class RummageKeepReflectionSafeAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to keep all the types reflection-safe which are passed in for the given generic parameter.</summary>
    [AttributeUsage(AttributeTargets.GenericParameter, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageKeepArgumentsReflectionSafeAttribute : Attribute
    {
    }

    /// <summary>Use only on custom-attribute class declarations. Instructs Rummage to keep everything reflection-safe that uses the given custom attribute.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), RummageKeepReflectionSafe]
    public sealed class RummageKeepUsersReflectionSafeAttribute : Attribute
    {
    }
}
