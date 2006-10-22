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

    public class ConfigFile : SettingsStore
    {
        private Dictionary<string, Dictionary<string, string>> Data = new Dictionary<string, Dictionary<string, string>>();

        public void LoadFromFile(string name)
        {
            string[] lines = File.ReadAllLines(name, Encoding.UTF8);
            string cursec = "";
            Data.Clear();
            Data.Add("", new Dictionary<string, string>());
            Regex rgSection = new Regex(@"^\s*\[\s*(.*?)\s*\]\s*$");
            Regex rgValue = new Regex(@"^\s*([^=]*?)\s*=\s*(.*?)\s*$");

            foreach (string ln in lines)
            {
                if (ln.Trim().Length == 0)
                    continue;

                Match ms = rgSection.Match(ln);
                Match mv = rgValue.Match(ln);

                if (ms.Success)
                {
                    cursec = UnmakeSingleString(ms.Groups[1].Value);
                    if (!Data.ContainsKey(cursec))
                        Data.Add(cursec, new Dictionary<string, string>());
                }
                else if (mv.Success)
                    Data[cursec][UnmakeSingleString(mv.Groups[1].Value)] =
                        UnmakeSingleString(mv.Groups[2].Value);
                else
                    throw new Exception("Cannot parse line: " + ln);
            }
        }

        public void SaveToFile(string name)
        {
            List<string> lines = new List<string>();

            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in Data)
            {
                // Add section header
                if (kvp.Key != "")
                {
                    lines.Add("");
                    lines.Add("["+MakeSingleString(kvp.Key)+"]");
                }
                // Add items
                foreach (KeyValuePair<string, string> kp in kvp.Value)
                    lines.Add(MakeSingleString(kp.Key) + " = " + MakeSingleString(kp.Value));
            }

            // Save
            File.WriteAllLines(name, lines.ToArray(), Encoding.UTF8);
        }

        public static string MakeSingleString(string val)
        {
            val = val.Replace(@"\", @"\\");
            val = val.Replace("\r",@"\R");
            val = val.Replace("\n", @"\N");
            return val;
        }

        public static string UnmakeSingleString(string val)
        {
            val = Regex.Replace(val, @"(^|[^\\])(\\\\)*\\R", "$1\r");
            val = Regex.Replace(val, @"(^|[^\\])(\\\\)*\\N", "$1\n");
            val = val.Replace(@"\\", @"\");
            return val;
        }

        public override void SetString(string section, string name, string val)
        {
            if (!Data.ContainsKey(section))
                Data[section] = new Dictionary<string, string>();
            Data[section][name] = val;
        }

        public override string GetString(string section, string name)
        {
            return Data[section][name];
        }

        public override void SetDouble(string section, string name, double val)
        {
            SetString(section, name, val.ToString());
        }

        public override double GetDouble(string section, string name)
        {
            return double.Parse(GetString(section, name));
        }

        public override void SetDecimal(string section, string name, decimal val)
        {
            SetString(section, name, val.ToString());
        }

        public override decimal GetDecimal(string section, string name)
        {
            return decimal.Parse(GetString(section, name));
        }

        public override void SetInt(string section, string name, int val)
        {
            SetString(section, name, val.ToString());
        }

        public override int GetInt(string section, string name)
        {
            return int.Parse(GetString(section, name));
        }

        public override void SetBool(string section, string name, bool val)
        {
            SetString(section, name, val.ToString());
        }

        public override bool GetBool(string section, string name)
        {
            return bool.Parse(GetString(section, name));
        }

    }
}
