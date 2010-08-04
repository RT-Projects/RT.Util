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

    /// <summary>Instructs Rummage to keep the original name of a specific element. </summary>
    [AttributeUsage(AttributeTargetSets.DefinitionsWithNames, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoRenameAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargetSets.TypeDefinitions, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoUnnestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargetSets.DefinitionsWithAccessModifiers, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoMarkPublicAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage not to inline a specific method that would otherwise be automatically inlined. This attribute takes precedence over <see cref="RummageInlineAttribute"/> if both are specified on the same method.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoInlineAttribute : Attribute
    {
    }

    /// <summary>Instructs Rummage to inline a specific method that would otherwise not be automatically inlined.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class RummageInlineAttribute : Attribute
    {
    }



    /// <summary>Instructs Rummage to keep all the methods, constructors and fields in a specific type.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class RummageKeepReflectionSafeAttribute : Attribute
    {
    }
}
