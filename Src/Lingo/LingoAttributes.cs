using System;

namespace RT.Util.Lingo
{
    /// <summary>
    /// Use this attribute on a field for a translatable string to specify notes to the translator, detailing the purpose, context, or format of a translatable string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class LingoNotesAttribute : Attribute
    {
        /// <summary>
        /// Constructor for a <see cref="LingoNotesAttribute"/> attribute.
        /// </summary>
        /// <param name="notes">Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.</param>
        public LingoNotesAttribute(string notes) { _notes = notes; }

        /// <summary>
        /// Specifies notes to the translator, detailing the purpose, context, or format of a translatable string.
        /// </summary>
        public string Notes { get { return _notes; } }
        private string _notes;
    }

#if DEBUG
    /// <summary>
    /// Use this attribute on a type that contains translations for a form. <see cref="Lingo.TranslateControl"/> will automatically add missing fields to the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LingoDebugAttribute : Attribute
    {
        /// <summary>
        /// Specifies the relative path from the compiled assembly to the source file of the translation type.
        /// </summary>
        public string RelativePath { get; set; }
    }
#endif
}
