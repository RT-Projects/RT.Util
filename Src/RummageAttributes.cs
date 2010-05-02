using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util
{
    /// <summary>Instructs Rummage to keep a specific type, method, constructor or field.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoRemoveAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        public RummageNoRemoveAttribute() { }
    }

    /// <summary>Instructs Rummage to keep the original name of a specific element. </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.Delegate | AttributeTargets.GenericParameter, Inherited = false, AllowMultiple = false)]
    public sealed class RummageNoRenameAttribute : Attribute
    {
        /// <summary>Constructor.</summary>
        public RummageNoRenameAttribute() { }
    }
}
