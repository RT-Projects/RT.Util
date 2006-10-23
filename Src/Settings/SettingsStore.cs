using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace RT.Util
{
    public abstract class SettingsStore
    {
        #region String

        public abstract string GetString(string section, string name);
        public abstract void SetString(string section, string name, string val);

        public void Set(string section, string name, string val)
        {
            SetString(section, name, val);
        }

        public string GetString(string section, string name, string def)
        {
            try { return GetString(section, name); }
            catch { return def; }
        }

        #endregion

        #region Double

        public abstract double GetDouble(string section, string name);
        public abstract void SetDouble(string section, string name, double val);

        public void Set(string section, string name, double val)
        {
            SetDouble(section, name, val);
        }

        public double GetDouble(string section, string name, double def)
        {
            try { return GetDouble(section, name); }
            catch { return def; }
        }

        #endregion

        #region Decimal

        public abstract decimal GetDecimal(string section, string name);
        public abstract void SetDecimal(string section, string name, decimal val);

        public void Set(string section, string name, decimal val)
        {
            SetDecimal(section, name, val);
        }

        public decimal GetDecimal(string section, string name, decimal def)
        {
            try { return GetDecimal(section, name); }
            catch { return def; }
        }

        #endregion

        #region Int

        public abstract int GetInt(string section, string name);
        public abstract void SetInt(string section, string name, int val);

        public void Set(string section, string name, int val)
        {
            SetInt(section, name, val);
        }

        public int GetInt(string section, string name, int def)
        {
            try { return GetInt(section, name); }
            catch { return def; }
        }

        #endregion

        #region Bool

        public abstract bool GetBool(string section, string name);
        public abstract void SetBool(string section, string name, bool val);

        public void Set(string section, string name, bool val)
        {
            SetBool(section, name, val);
        }

        public bool GetBool(string section, string name, bool def)
        {
            try { return GetBool(section, name); }
            catch { return def; }
        }

        #endregion
    }
}
