using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace RT.Util
{
    /// <summary>
    /// This class keeps track of the instances of those classes which intend only one
    /// instance to be created. It stores an instance once created, and this can be
    /// retrieved later using the type of the class. Note that once created, an instance
    /// will be retained forever since there is no reliable way to free instances.
    /// </summary>
    //public static class SingleInstance
    //{
    //    private static Dictionary<Type, object> Instances = new Dictionary<Type, object>();

    //    public static void CreatedInstance(object instance)
    //    {
    //        if (Instances.ContainsKey(instance.GetType()))
    //            throw new Exception("An instance of '" + instance.GetType().ToString() + "' already exists; a second instance cannot be created.");

    //        Instances.Add(instance.GetType(), instance);
    //    }

    //    public static void DisposedInstance(object instance)
    //    {
    //        Instances.Remove(instance.GetType());
    //    }

    //    public static T GetInstance<T>(bool AutoCreateIfNotExist) where T : new()
    //    {
    //        if (Instances.ContainsKey(typeof(T)))
    //            return (T)Instances[typeof(T)];
    //        else if (!AutoCreateIfNotExist)
    //            throw new Exception("An instance of '" + typeof(T).ToString() + "' has been requested but doesn't exist yet.");
    //        else
    //        {
    //            T inst = new T();
    //            Instances.Add(typeof(T), inst);
    //            return inst;
    //        }
    //    }
    //}

    //public static class Singleton<T>  where T : class
    //{
    //    static Singleton()
    //    {
    //    }

    //    static private T m_Instance;

    //    static public T Instance
    //    {
    //        get
    //        {
    //            if (m_Instance == null || (m_Instance is Control && (m_Instance as Control).IsDisposed))
    //                m_Instance =
    //                    typeof(T).InvokeMember(typeof(T).Name,
    //                    System.Reflection.BindingFlags.CreateInstance |
    //                    System.Reflection.BindingFlags.Instance |
    //                    System.Reflection.BindingFlags.NonPublic,
    //                    null, null, null) as T;

    //            return m_Instance;
    //        }
    //    }

    //    //public static readonly T Instance =
    //    //    typeof(T).InvokeMember(typeof(T).Name,
    //    //    BindingFlags.CreateInstance |
    //    //    BindingFlags.Instance |
    //    //    BindingFlags.NonPublic,
    //    //    null, null, null) as T;
    //}

}
