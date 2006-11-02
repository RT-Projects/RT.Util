using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace RT.Util.Settings
{
    public abstract class SettingsStore
    {
        [Serializable]
        protected class Dir
        {
            public Dictionary<string, Dir> Dirs = new Dictionary<string, Dir>();
            public Dictionary<string, object> Vals = new Dictionary<string, object>();
        }

        /// <summary>
        /// This structure holds all the settings. The reason this object is protected is
        /// that derived classes may choose to store settings elsewhere (or nowhere at all
        /// other than the target non-volatile store, such as the registry), so Data may
        /// not even contain anything.
        /// </summary>
        protected Dir Data = new Dir();

        /// <summary>
        /// Derived classes implement this to load the settings from a permanent store.
        /// It is possible that some derived classes will override GetObject in
        /// such a way that LoadSettings is not necessary.
        /// </summary>
        public abstract void LoadSettings();

        /// <summary>
        /// Derived classes implement this to save the settings to a permanent store.
        /// It is possible that some derived classes will override SetObject in
        /// such a way that SaveSettings is not necessary.
        /// </summary>
        public abstract void SaveSettings();

        /// <summary>
        /// If true, all paths are treated as unique names storing everything
        /// in a flat structure. E.g. "some.setting" and "one.more.setting"
        /// will be both stored in the root dir.
        /// </summary>
        public bool UseFlatNames = false;

        /// <summary>
        /// Turns a string path into an array of string elements. For example,
        /// "Some.Path.Here" will be turned into ["Some","Path","Here"].
        /// </summary>
        public string[] MakePath(string path)
        {
            if (UseFlatNames)
            {
                return new string[] { path };
            }
            else
            {
                // This is somewhat simplified; a perfect function would allow the
                // dots to be escaped.
                return path.Split('.');
            }
        }

        // Here you go Timwi :)
        #region Object

        /// <summary>
        /// Obtains the object at the specified path. Throws an Exception with a
        /// descriptive message if the path or the object does not exist.
        /// 
        /// The implementation provided in this class will access settings in the
        /// Data dictionary, which are loaded / saved by the derived classes. A derived
        /// class can override this method to implement a different behaviour,
        /// such as direct read/write to some backing store.
        /// </summary>
        public virtual object GetObject(string[] path)
        {
            Dir cur = Data;
            // Navigate to the dir
            int i;
            for (i=0; i<path.Length-1; i++)
                if (cur.Dirs.ContainsKey(path[i]))
                    cur = cur.Dirs[path[i]];
                else
                    throw new Exception("The Settings Store does not contain path \"" + string.Join(".", path, 0, i+1) + "\"");
            // Return the value
            if (cur.Vals.ContainsKey(path[i]))
                return cur.Vals[path[i]];
            else
                throw new Exception("The Settings Store does not contain value \"" + path[i] + "\" under path \"" + string.Join(".", path, 0, i) + "\"");
        }

        /// <summary>
        /// Stores an object at the specified path. Automatically creates entries
        /// in the Data structure for non-existent dirs.
        /// 
        /// The implementation provided in this class will access settings in the
        /// Data dictionary, which are loaded / saved by the derived classes. A derived
        /// class can override this method to implement a different behaviour,
        /// such as direct write-through to some backing store.
        /// </summary>
        public virtual void SetObject(string[] path, object obj)
        {
            Dir cur = Data;
            // Navigate to the dir
            int i;
            for (i=0; i<path.Length-1; i++)
            {
                if (!cur.Dirs.ContainsKey(path[i]))
                    cur.Dirs.Add(path[i], new Dir());
                cur = cur.Dirs[path[i]];
            }
            // Store the value
            cur.Vals[path[i]] = obj;
        }

        /// <summary>
        /// The following methods should also be virtual, and these implementations
        /// should handle the UseFlatNames setting in such a way as to improve
        /// efficiency of storage/retrieval. See B-5.
        /// </summary>

        public object GetObject(string path)
        {
            return GetObject(MakePath(path));
        }

        public object GetObject(string path, object def)
        {
            try { return GetObject(MakePath(path)); }
            catch { return def; }
        }

        public object Get(string path, object def)
        {
            try { return GetObject(MakePath(path)); }
            catch { return def; }
        }

        public void SetObject(string path, object obj)
        {
            SetObject(MakePath(path), obj);
        }

        public void Set(string path, object obj)
        {
            SetObject(MakePath(path), obj);
        }

        #endregion

        #region String

        public string GetString(string path) { return (string)GetObject(path); }
        public string GetString(string path, string def) { return (string)GetObject(path, def); }
        public string Get(string path, string def) { return (string)GetObject(path, def); }
        public void SetString(string path, string val) { SetObject(path, val); }
        public void Set(string path, string val) { SetObject(path, val); }

        #endregion

        #region Bool

        public bool GetBool(string path) { return (bool)GetObject(path); }
        public bool GetBool(string path, bool def) { return (bool)GetObject(path, def); }
        public bool Get(string path, bool def) { return (bool)GetObject(path, def); }
        public void SetBool(string path, bool val) { SetObject(path, val); }
        public void Set(string path, bool val) { SetObject(path, val); }

        #endregion

        #region Int

        public int GetInt(string path) { return (int)GetObject(path); }
        public int GetInt(string path, int def) { return (int)GetObject(path, def); }
        public int Get(string path, int def) { return (int)GetObject(path, def); }
        public void SetInt(string path, int val) { SetObject(path, val); }
        public void Set(string path, int val) { SetObject(path, val); }

        #endregion

        #region Long

        public long GetLong(string path) { return (long)GetObject(path); }
        public long GetLong(string path, long def) { return (long)GetObject(path, def); }
        public long Get(string path, long def) { return (long)GetObject(path, def); }
        public void SetLong(string path, long val) { SetObject(path, val); }
        public void Set(string path, long val) { SetObject(path, val); }

        #endregion

        #region Double

        public double GetDouble(string path) { return (double)GetObject(path); }
        public double GetDouble(string path, double def) { return (double)GetObject(path, def); }
        public double Get(string path, double def) { return (double)GetObject(path, def); }
        public void SetDouble(string path, double val) { SetObject(path, val); }
        public void Set(string path, double val) { SetObject(path, val); }

        #endregion

        #region Decimal

        public decimal GetDecimal(string path) { return (decimal)GetObject(path); }
        public decimal GetDecimal(string path, decimal def) { return (decimal)GetObject(path, def); }
        public decimal Get(string path, decimal def) { return (decimal)GetObject(path, def); }
        public void SetDecimal(string path, decimal val) { SetObject(path, val); }
        public void Set(string path, decimal val) { SetObject(path, val); }

        #endregion
    }
}
