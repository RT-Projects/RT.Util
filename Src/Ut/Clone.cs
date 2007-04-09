/// Clone.cs  -  clone an arbitrary object using reflection

using System;
using System.Reflection;

namespace RT.Util
{
    public static partial class Ut
    {
        /// <summary>
        /// Performs a deep copy of the specified objects. A few important notes:
        /// 
        /// * Only ICloneable members are cloned recursively. Others are just copied by reference.
        /// * Does not recognise references to objects already cloned, so this is currently
        ///   completely unsuitable for complex structures
        /// </summary>
        public static object Clone(object Obj)
        {
            Type T = Obj.GetType();
            object newObj = Activator.CreateInstance(T);
            FieldInfo[] fields = T.GetFields();

            foreach (FieldInfo fi in fields)
            {
                // Check if it supports ICloneable
                Type ICloneType = fi.FieldType.GetInterface("ICloneable");

                if (ICloneType != null)
                {
                    // Cloneable - so clone it!
                    ICloneable icl = (ICloneable)fi.GetValue(Obj);
                    fi.SetValue(newObj, icl == null ? null : icl.Clone());
                }
                else
                {
                    // Not cloneable - just copy it then
                    fi.SetValue(newObj, fi.GetValue(Obj));
                }
            }
            return newObj;
        }

    }
}
