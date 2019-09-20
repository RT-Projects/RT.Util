using RT.Serialization;

namespace RT.Util.Lingo
{
    /// <summary>Abstract base class to represent translations of a piece of software.</summary>
    public abstract class TranslationBase
    {
        /// <summary>Language of this translation.</summary>
        [LingoIgnore, ClassifyIgnore]
        public Language Language;

        /// <summary>Constructor.</summary>
        public TranslationBase(Language language) { Language = language; }
    }
}
