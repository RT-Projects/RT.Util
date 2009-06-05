using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace RT.Util.Lingo
{
    /// <summary>Abstract base class to represent translations of a piece of software.</summary>
    public abstract class TranslationBase
    {
        /// <summary>Language of this translation.</summary>
        public Language Language;

        /// <summary>Gets the number system associated with this translation's language.</summary>
        public NumberSystem NumberSystem
        {
            get
            {
                var t = typeof(Language);
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                    if ((Language) f.GetValue(null) == Language)
                        foreach (var a in f.GetCustomAttributes(typeof(LanguageInfoAttribute), false))
                            return ((LanguageInfoAttribute) a).NumberSystem;
                return null;
            }
        }

        /// <summary>Gets the native name of this translation's language.</summary>
        public string LanguageNativeName
        {
            get
            {
                var t = typeof(Language);
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                    if ((Language) f.GetValue(null) == Language)
                        foreach (var a in f.GetCustomAttributes(typeof(LanguageInfoAttribute), false))
                            return ((LanguageInfoAttribute) a).NativeName;
                return null;
            }
        }

        /// <summary>Gets the English name of this translation's language.</summary>
        public string LanguageEnglishName
        {
            get
            {
                var t = typeof(Language);
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                    if ((Language) f.GetValue(null) == Language)
                        foreach (var a in f.GetCustomAttributes(typeof(LanguageInfoAttribute), false))
                            return ((LanguageInfoAttribute) a).EnglishName;
                return null;
            }
        }

        /// <summary>Constructor.</summary>
        public TranslationBase(Language language) { Language = language; }
    }
}
