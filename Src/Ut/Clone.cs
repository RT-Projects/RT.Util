/// Clone.cs  -  clone an arbitrary object using reflection

using System;
using System.Reflection;

namespace RT.Util
{
    public static partial class Ut
    {
        public static object Clone(object Obj)
        {
            Type T = Obj.GetType();
            object newObj = Activator.CreateInstance(T);
            FieldInfo[] fields = T.GetFields();

            int i = 0;

            foreach (FieldInfo fi in fields)
            {
                // Check if it supports ICloneable
                Type ICloneType = fi.FieldType.GetInterface("ICloneable");

                if (ICloneType != null)
                    // Cloneable - so clone it!
                    fields[i].SetValue(newObj, ((ICloneable)fi.GetValue(Obj)).Clone());
                else
                    // Not cloneable - just copy it then
                    fields[i].SetValue(newObj, fi.GetValue(Obj));
            }
            return newObj;
        }

    }
}
