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

        protected Dir Data = new Dir();

        /// <summary>
        /// Turns a string path into an array of string elements. For example,
        /// "Some.Path.Here" will be turned into ["Some","Path","Here"].
        /// </summary>
        public string[] MakePath(string path)
        {
            // This is somewhat simplified; a perfect function would allow the
            // dots to be escaped.
            return path.Split('.');
        }

        /// Here you go Timwi :)
        #region Object

        public object GetObject(string[] path)
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

        public void SetObject(string[] path, object obj)
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
