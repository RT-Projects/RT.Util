using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Provides a means for an application to throw exceptions containing translatable messages without the need to pass a <see cref="TranslationBase"/>-derived object to all methods that could throw exceptions.
    /// </summary>
    /// <typeparam name="TTranslation">The type containing the translatable strings.</typeparam>
    [Serializable]
    public class TranslatableException<TTranslation> : RTException where TTranslation : TranslationBase, new()
    {
        private static TTranslation _defaultTranslation;
        private Func<TTranslation, string> _getMessage;

        /// <summary>Constructor.</summary>
        /// <param name="getMessage">A function which returns the exception message given a <typeparamref name="TTranslation"/> object. 
        /// This would usually be of the form "tr => tr.FieldName" or "tr => tr.FieldName.Fmt(parameters)".</param>
        public TranslatableException(Func<TTranslation, string> getMessage)
            : this(getMessage, null)
        {
        }

        /// <summary>Constructor.</summary>
        /// <param name="getMessage">A function which returns the exception message given a <typeparamref name="TTranslation"/> object. 
        /// This would usually be of the form "tr => tr.FieldName" or "tr => tr.FieldName.Fmt(parameters)".</param>
        /// <param name="inner">Inner exception.</param>
        public TranslatableException(Func<TTranslation, string> getMessage, Exception inner)
            : base(getMessage(_defaultTranslation == null ? (_defaultTranslation = new TTranslation()) : _defaultTranslation), inner)
        {
            _getMessage = getMessage;
        }

        /// <summary>Returns the translated exception message.</summary>
        /// <param name="tr">Translation object containing the translations for all messages.</param>
        public string GetMessage(TTranslation tr)
        {
            return _getMessage(tr);
        }
    }
}
