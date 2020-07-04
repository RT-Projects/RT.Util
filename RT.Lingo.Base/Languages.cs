using System;
using System.Reflection;
using RT.Util.ExtensionMethods;

namespace RT.Lingo
{
    /// <summary>Lists all the languages that have an ISO-639-1 two-letter code, and a selection of languages with an ISO-639-2 three-letter code.</summary>
    public enum Language
    {
        /// <summary>Represents the Afar language (aa).</summary>
        [LanguageInfo("aa", "Afar", "Afaraf", typeof(OneStringNumberSystem) /* unsure */)]
        Afar,
        /// <summary>Represents the Abkhazian language (ab).</summary>
        [LanguageInfo("ab", "Abkhazian", "Аҧсуа", typeof(OneStringNumberSystem) /* unsure */)]
        Abkhazian,
        /// <summary>Represents the Avestan language (ae).</summary>
        [LanguageInfo("ae", "Avestan", "Avesta", typeof(OneStringNumberSystem) /* unsure */)]
        Avestan,
        /// <summary>Represents the Afrikaans language (af).</summary>
        [LanguageInfo("af", "Afrikaans", "Afrikaans", typeof(Singular1PluralNumberSystem))]
        Afrikaans,
        /// <summary>Represents the Akan language (ak).</summary>
        [LanguageInfo("ak", "Akan", "Akan", typeof(OneStringNumberSystem) /* unsure */)]
        Akan,
        /// <summary>Represents the Amharic language (am).</summary>
        [LanguageInfo("am", "Amharic", "አማርኛ", typeof(OneStringNumberSystem) /* unsure */)]
        Amharic,
        /// <summary>Represents the Aragonese language (an).</summary>
        [LanguageInfo("an", "Aragonese", "Aragonés", typeof(OneStringNumberSystem) /* unsure */)]
        Aragonese,
        /// <summary>Represents the Arabic language (ar).</summary>
        [LanguageInfo("ar", "Arabic", "العربية", typeof(OneStringNumberSystem) /* unsure */)]
        Arabic,
        /// <summary>Represents the Assamese language (as).</summary>
        [LanguageInfo("as", "Assamese", "অসমীয়া", typeof(OneStringNumberSystem) /* unsure */)]
        Assamese,
        /// <summary>Represents the Avaric language (av).</summary>
        [LanguageInfo("av", "Avaric", "Авар мацӀ", typeof(OneStringNumberSystem) /* unsure */)]
        Avaric,
        /// <summary>Represents the Aymara language (ay).</summary>
        [LanguageInfo("ay", "Aymara", "Aymar aru", typeof(OneStringNumberSystem) /* unsure */)]
        Aymara,
        /// <summary>Represents the Azerbaijani language (az).</summary>
        [LanguageInfo("az", "Azerbaijani", "Azərbaycan dili", typeof(OneStringNumberSystem) /* unsure */)]
        Azerbaijani,
        /// <summary>Represents the Bashkir language (ba).</summary>
        [LanguageInfo("ba", "Bashkir", "Башҡорт теле", typeof(OneStringNumberSystem) /* unsure */)]
        Bashkir,
        /// <summary>Represents the Belarusian language (be).</summary>
        [LanguageInfo("be", "Belarusian", "Беларуская", typeof(OneStringNumberSystem) /* unsure */)]
        Belarusian,
        /// <summary>Represents the Bulgarian language (bg).</summary>
        [LanguageInfo("bg", "Bulgarian", "Български език", typeof(OneStringNumberSystem) /* unsure */)]
        Bulgarian,
        /// <summary>Represents the Bihari language (bh).</summary>
        [LanguageInfo("bh", "Bihari", "भोजपुरी", typeof(OneStringNumberSystem) /* unsure */)]
        Bihari,
        /// <summary>Represents the Bislama language (bi).</summary>
        [LanguageInfo("bi", "Bislama", "Bislama", typeof(OneStringNumberSystem) /* unsure */)]
        Bislama,
        /// <summary>Represents the Bambara language (bm).</summary>
        [LanguageInfo("bm", "Bambara", "Bamanankan", typeof(OneStringNumberSystem) /* unsure */)]
        Bambara,
        /// <summary>Represents the Bengali language (bn).</summary>
        [LanguageInfo("bn", "Bengali", "বাংলা", typeof(OneStringNumberSystem) /* unsure */)]
        Bengali,
        /// <summary>Represents the Tibetan language (bo).</summary>
        [LanguageInfo("bo", "Tibetan", "བོད་ཡིག", typeof(OneStringNumberSystem) /* unsure */)]
        Tibetan,
        /// <summary>Represents the Breton language (br).</summary>
        [LanguageInfo("br", "Breton", "Brezhoneg", typeof(OneStringNumberSystem) /* unsure */)]
        Breton,
        /// <summary>Represents the Bosnian language (bs).</summary>
        [LanguageInfo("bs", "Bosnian", "Bosanski jezik", typeof(OneStringNumberSystem) /* unsure */)]
        Bosnian,
        /// <summary>Represents the Catalan language (ca).</summary>
        [LanguageInfo("ca", "Catalan", "Català", typeof(OneStringNumberSystem) /* unsure */)]
        Catalan,
        /// <summary>Represents the Chechen language (ce).</summary>
        [LanguageInfo("ce", "Chechen", "Нохчийн мотт", typeof(OneStringNumberSystem) /* unsure */)]
        Chechen,
        /// <summary>Represents the Chamorro language (ch).</summary>
        [LanguageInfo("ch", "Chamorro", "Chamoru", typeof(OneStringNumberSystem) /* unsure */)]
        Chamorro,
        /// <summary>Represents the Corsican language (co).</summary>
        [LanguageInfo("co", "Corsican", "Corsu", typeof(OneStringNumberSystem) /* unsure */)]
        Corsican,
        /// <summary>Represents the Cree language (cr).</summary>
        [LanguageInfo("cr", "Cree", "ᓀᐦᐃᔭᐍᐏᐣ", typeof(OneStringNumberSystem) /* unsure */)]
        Cree,
        /// <summary>Represents the Czech language (cs).</summary>
        [LanguageInfo("cs", "Czech", "Česky", typeof(SlavicNumberSystem2))]
        Czech,
        /// <summary>Represents the Church Slavic language (cu).</summary>
        [LanguageInfo("cu", "Church Slavic", "Ѩзыкъ словѣньскъ", typeof(OneStringNumberSystem) /* unsure */)]
        ChurchSlavic,
        /// <summary>Represents the Chuvash language (cv).</summary>
        [LanguageInfo("cv", "Chuvash", "Чӑваш чӗлхи", typeof(OneStringNumberSystem) /* unsure */)]
        Chuvash,
        /// <summary>Represents the Welsh language (cy).</summary>
        [LanguageInfo("cy", "Welsh", "Cymraeg", typeof(OneStringNumberSystem) /* unsure */)]
        Welsh,
        /// <summary>Represents the Danish language (da).</summary>
        [LanguageInfo("da", "Danish", "Dansk", typeof(Singular1PluralNumberSystem))]
        Danish,
        /// <summary>Represents the German language (de).</summary>
        [LanguageInfo("de", "German", "Deutsch", typeof(Singular1PluralNumberSystem))]
        German,
        /// <summary>Represents the Divehi language (dv).</summary>
        [LanguageInfo("dv", "Divehi", "ދިވެހި", typeof(OneStringNumberSystem) /* unsure */)]
        Divehi,
        /// <summary>Represents the Dzongkha language (dz).</summary>
        [LanguageInfo("dz", "Dzongkha", "རྫོང་ཁ", typeof(OneStringNumberSystem) /* unsure */)]
        Dzongkha,
        /// <summary>Represents the Ewe language (ee).</summary>
        [LanguageInfo("ee", "Ewe", "Ɛʋɛgbɛ", typeof(OneStringNumberSystem) /* unsure */)]
        Ewe,
        /// <summary>Represents the Greek language (el).</summary>
        [LanguageInfo("el", "Greek", "Ελληνικά", typeof(Singular1PluralNumberSystem))]
        Greek,
        /// <summary>Represents the English language (en), as spoken in the UK.</summary>
        [LanguageInfo("en_gb", "English (UK)", "English (UK)", typeof(Singular1PluralNumberSystem))]
        EnglishUK,
        /// <summary>Represents the English language (en), as spoken in the USA.</summary>
        [LanguageInfo("en_us", "English (US)", "English (US)", typeof(Singular1PluralNumberSystem))]
        EnglishUS,
        /// <summary>Represents the Esperanto language (eo).</summary>
        [LanguageInfo("eo", "Esperanto", "Esperanto", typeof(Singular1PluralNumberSystem))]
        Esperanto,
        /// <summary>Represents the Spanish language (es).</summary>
        [LanguageInfo("es", "Spanish", "Español", typeof(Singular1PluralNumberSystem))]
        Spanish,
        /// <summary>Represents the Estonian language (et).</summary>
        [LanguageInfo("et", "Estonian", "Eesti", typeof(Singular1PluralNumberSystem))]
        Estonian,
        /// <summary>Represents the Basque language (eu).</summary>
        [LanguageInfo("eu", "Basque", "Euskara", typeof(OneStringNumberSystem) /* unsure */)]
        Basque,
        /// <summary>Represents the Persian language (fa).</summary>
        [LanguageInfo("fa", "Persian", "فارسی", typeof(OneStringNumberSystem) /* unsure */)]
        Persian,
        /// <summary>Represents the Fulah language (ff).</summary>
        [LanguageInfo("ff", "Fulah", "Fulfulde", typeof(OneStringNumberSystem) /* unsure */)]
        Fulah,
        /// <summary>Represents the Finnish language (fi).</summary>
        [LanguageInfo("fi", "Finnish", "Suomi", typeof(Singular1PluralNumberSystem))]
        Finnish,
        /// <summary>Represents the Fijian language (fj).</summary>
        [LanguageInfo("fj", "Fijian", "Vosa Vakaviti", typeof(OneStringNumberSystem) /* unsure */)]
        Fijian,
        /// <summary>Represents the Faroese language (fo).</summary>
        [LanguageInfo("fo", "Faroese", "Føroyskt", typeof(Singular1PluralNumberSystem))]
        Faroese,
        /// <summary>Represents the French language (fr).</summary>
        [LanguageInfo("fr", "French", "Français", typeof(Singular01PluralNumberSystem))]
        French,
        /// <summary>Represents the Western Frisian language (fy).</summary>
        [LanguageInfo("fy", "Western Frisian", "Frysk", typeof(OneStringNumberSystem) /* unsure */)]
        WesternFrisian,
        /// <summary>Represents the Irish language (ga).</summary>
        [LanguageInfo("ga", "Irish", "Gaeilge", typeof(IrishNumberSystem))]
        Irish,
        /// <summary>Represents the Scottish Gaelic language (gd).</summary>
        [LanguageInfo("gd", "Scottish Gaelic", "Gàidhlig", typeof(OneStringNumberSystem) /* unsure */)]
        ScottishGaelic,
        /// <summary>Represents the Galician language (gl).</summary>
        [LanguageInfo("gl", "Galician", "Galego", typeof(OneStringNumberSystem) /* unsure */)]
        Galician,
        /// <summary>Represents the Guaraní language (gn).</summary>
        [LanguageInfo("gn", "Guaraní", "Avañe'ẽ", typeof(OneStringNumberSystem) /* unsure */)]
        Guaraní,
        /// <summary>Represents the Gujarati language (gu).</summary>
        [LanguageInfo("gu", "Gujarati", "ગુજરાતી", typeof(OneStringNumberSystem) /* unsure */)]
        Gujarati,
        /// <summary>Represents the Manx language (gv).</summary>
        [LanguageInfo("gv", "Manx", "Gaelg; Gailck", typeof(OneStringNumberSystem) /* unsure */)]
        Manx,
        /// <summary>Represents the Hausa language (ha).</summary>
        [LanguageInfo("ha", "Hausa", "هَوُسَ", typeof(OneStringNumberSystem) /* unsure */)]
        Hausa,
        /// <summary>Represents the Hebrew language (he).</summary>
        [LanguageInfo("he", "Hebrew", "עברית", typeof(Singular1PluralNumberSystem))]
        Hebrew,
        /// <summary>Represents the Hindi language (hi).</summary>
        [LanguageInfo("hi", "Hindi", "हिन्दी; हिंदी", typeof(OneStringNumberSystem) /* unsure */)]
        Hindi,
        /// <summary>Represents the Hiri Motu language (ho).</summary>
        [LanguageInfo("ho", "Hiri Motu", "Hiri Motu", typeof(OneStringNumberSystem) /* unsure */)]
        HiriMotu,
        /// <summary>Represents the Croatian language (hr).</summary>
        [LanguageInfo("hr", "Croatian", "Hrvatski", typeof(SlavicNumberSystem1))]
        Croatian,
        /// <summary>Represents the Haitian language (ht).</summary>
        [LanguageInfo("ht", "Haitian", "Kreyòl Ayisyen", typeof(OneStringNumberSystem) /* unsure */)]
        Haitian,
        /// <summary>Represents the Hungarian language (hu).</summary>
        [LanguageInfo("hu", "Hungarian", "Magyar", typeof(OneStringNumberSystem) /* unsure */)]
        Hungarian,
        /// <summary>Represents the Armenian language (hy).</summary>
        [LanguageInfo("hy", "Armenian", "Հայերեն", typeof(OneStringNumberSystem) /* unsure */)]
        Armenian,
        /// <summary>Represents the Herero language (hz).</summary>
        [LanguageInfo("hz", "Herero", "Otjiherero", typeof(OneStringNumberSystem) /* unsure */)]
        Herero,
        /// <summary>Represents the Interlingua (International Auxiliary Language Association) language (ia).</summary>
        [LanguageInfo("ia", "Interlingua", "Interlingua", typeof(OneStringNumberSystem) /* unsure */)]
        Interlingua,
        /// <summary>Represents the Indonesian language (id).</summary>
        [LanguageInfo("id", "Indonesian", "Bahasa Indonesia", typeof(OneStringNumberSystem) /* unsure */)]
        Indonesian,
        /// <summary>Represents the Interlingue language (ie).</summary>
        [LanguageInfo("ie", "Interlingue", "Interlingue", typeof(OneStringNumberSystem) /* unsure */)]
        Interlingue,
        /// <summary>Represents the Igbo language (ig).</summary>
        [LanguageInfo("ig", "Igbo", "Igbo", typeof(OneStringNumberSystem) /* unsure */)]
        Igbo,
        /// <summary>Represents the Sichuan Yi language (ii).</summary>
        [LanguageInfo("ii", "Sichuan Yi", "ꆇꉙ", typeof(OneStringNumberSystem) /* unsure */)]
        SichuanYi,
        /// <summary>Represents the Inupiaq language (ik).</summary>
        [LanguageInfo("ik", "Inupiaq", "Iñupiaq", typeof(OneStringNumberSystem) /* unsure */)]
        Inupiaq,
        /// <summary>Represents the Ido language (io).</summary>
        [LanguageInfo("io", "Ido", "Ido", typeof(OneStringNumberSystem) /* unsure */)]
        Ido,
        /// <summary>Represents the Icelandic language (is).</summary>
        [LanguageInfo("is", "Icelandic", "Íslenska", typeof(OneStringNumberSystem) /* unsure */)]
        Icelandic,
        /// <summary>Represents the Italian language (it).</summary>
        [LanguageInfo("it", "Italian", "Italiano", typeof(Singular1PluralNumberSystem))]
        Italian,
        /// <summary>Represents the Inuktitut language (iu).</summary>
        [LanguageInfo("iu", "Inuktitut", "ᐃᓄᒃᑎᑐᑦ", typeof(OneStringNumberSystem) /* unsure */)]
        Inuktitut,
        /// <summary>Represents the Japanese language (ja).</summary>
        [LanguageInfo("ja", "Japanese", "日本語", typeof(OneStringNumberSystem))]
        Japanese,
        /// <summary>Represents the Javanese language (jv).</summary>
        [LanguageInfo("jv", "Javanese", "Basa Jawa", typeof(OneStringNumberSystem) /* unsure */)]
        Javanese,
        /// <summary>Represents the Georgian language (ka).</summary>
        [LanguageInfo("ka", "Georgian", "ქართული", typeof(OneStringNumberSystem) /* unsure */)]
        Georgian,
        /// <summary>Represents the Kongo language (kg).</summary>
        [LanguageInfo("kg", "Kongo", "KiKongo", typeof(OneStringNumberSystem) /* unsure */)]
        Kongo,
        /// <summary>Represents the Kikuyu language (ki).</summary>
        [LanguageInfo("ki", "Kikuyu", "Gĩkũyũ", typeof(OneStringNumberSystem) /* unsure */)]
        Kikuyu,
        /// <summary>Represents the Kwanyama language (kj).</summary>
        [LanguageInfo("kj", "Kwanyama", "Kuanyama", typeof(OneStringNumberSystem) /* unsure */)]
        Kwanyama,
        /// <summary>Represents the Kazakh language (kk).</summary>
        [LanguageInfo("kk", "Kazakh", "Қазақ Тілі", typeof(OneStringNumberSystem) /* unsure */)]
        Kazakh,
        /// <summary>Represents the Kalaallisut language (kl).</summary>
        [LanguageInfo("kl", "Kalaallisut", "Kalaallisut", typeof(OneStringNumberSystem) /* unsure */)]
        Kalaallisut,
        /// <summary>Represents the Khmer language (km).</summary>
        [LanguageInfo("km", "Khmer", "ភាសាខ្មែរ", typeof(OneStringNumberSystem) /* unsure */)]
        Khmer,
        /// <summary>Represents the Kannada language (kn).</summary>
        [LanguageInfo("kn", "Kannada", "ಕನ್ನಡ", typeof(OneStringNumberSystem) /* unsure */)]
        Kannada,
        /// <summary>Represents the Korean language (ko).</summary>
        [LanguageInfo("ko", "Korean", "한국어", typeof(OneStringNumberSystem))]
        Korean,
        /// <summary>Represents the Kanuri language (kr).</summary>
        [LanguageInfo("kr", "Kanuri", "Kanuri", typeof(OneStringNumberSystem) /* unsure */)]
        Kanuri,
        /// <summary>Represents the Kashmiri language (ks), written in Devanagari.</summary>
        [LanguageInfo("ks_d", "Kashmiri (Devanagari)", "कश्मीरी", typeof(OneStringNumberSystem) /* unsure */)]
        KashmiriDevanagari,
        /// <summary>Represents the Kashmiri language (ks), written in Arabic.</summary>
        [LanguageInfo("ks_a", "Kashmiri (Arabic)", "كشميري", typeof(OneStringNumberSystem) /* unsure */)]
        KashmiriArabic,
        /// <summary>Represents the Kurdish language (ku), written in Latin.</summary>
        [LanguageInfo("ku_l", "Kurdish (Latin)", "Kurdî", typeof(OneStringNumberSystem) /* unsure */)]
        KurdishLatin,
        /// <summary>Represents the Kurdish language (ku), written in Arabic.</summary>
        [LanguageInfo("ku_a", "Kurdish (Arabic)", "كوردی", typeof(OneStringNumberSystem) /* unsure */)]
        KurdishArabic,
        /// <summary>Represents the Komi language (kv).</summary>
        [LanguageInfo("kv", "Komi", "Коми Кыв", typeof(OneStringNumberSystem) /* unsure */)]
        Komi,
        /// <summary>Represents the Cornish language (kw).</summary>
        [LanguageInfo("kw", "Cornish", "Kernewek", typeof(OneStringNumberSystem) /* unsure */)]
        Cornish,
        /// <summary>Represents the Kirghiz language (ky).</summary>
        [LanguageInfo("ky", "Kirghiz", "Кыргыз Тили", typeof(OneStringNumberSystem) /* unsure */)]
        Kirghiz,
        /// <summary>Represents the Latin language (la).</summary>
        [LanguageInfo("la", "Latin", "Latine", typeof(OneStringNumberSystem) /* unsure */)]
        Latin,
        /// <summary>Represents the Luxembourgish language (lb).</summary>
        [LanguageInfo("lb", "Luxembourgish", "Lëtzebuergesch", typeof(OneStringNumberSystem) /* unsure */)]
        Luxembourgish,
        /// <summary>Represents the Ganda language (lg).</summary>
        [LanguageInfo("lg", "Ganda", "Luganda", typeof(OneStringNumberSystem) /* unsure */)]
        Ganda,
        /// <summary>Represents the Limburgish language (li).</summary>
        [LanguageInfo("li", "Limburgish", "Limburgs", typeof(OneStringNumberSystem) /* unsure */)]
        Limburgish,
        /// <summary>Represents the Lingala language (ln).</summary>
        [LanguageInfo("ln", "Lingala", "Lingála", typeof(OneStringNumberSystem) /* unsure */)]
        Lingala,
        /// <summary>Represents the Lao language (lo).</summary>
        [LanguageInfo("lo", "Lao", "ພາສາລາວ", typeof(OneStringNumberSystem) /* unsure */)]
        Lao,
        /// <summary>Represents the Lithuanian language (lt).</summary>
        [LanguageInfo("lt", "Lithuanian", "Lietuvių Kalba", typeof(LithuanianNumberSystem))]
        Lithuanian,
        /// <summary>Represents the Lithuanian language (lt).</summary>
        [LanguageInfo("lu", "Luba-Katanga", "Luba-Katanga", typeof(OneStringNumberSystem) /* unsure */)]
        LubaKatanga,
        /// <summary>Represents the Latvian language (lv).</summary>
        [LanguageInfo("lv", "Latvian", "Latviešu Valoda", typeof(LatvianNumberSystem))]
        Latvian,
        /// <summary>Represents the Malagasy language (mg).</summary>
        [LanguageInfo("mg", "Malagasy", "Malagasy Fiteny", typeof(OneStringNumberSystem) /* unsure */)]
        Malagasy,
        /// <summary>Represents the Marshallese language (mh).</summary>
        [LanguageInfo("mh", "Marshallese", "Kajin M̧ajeļ", typeof(OneStringNumberSystem) /* unsure */)]
        Marshallese,
        /// <summary>Represents the Māori language (mi).</summary>
        [LanguageInfo("mi", "Māori", "Te Reo Māori", typeof(OneStringNumberSystem) /* unsure */)]
        Māori,
        /// <summary>Represents the Macedonian language (mk).</summary>
        [LanguageInfo("mk", "Macedonian", "Македонски Јазик", typeof(OneStringNumberSystem) /* unsure */)]
        Macedonian,
        /// <summary>Represents the Malayalam language (ml).</summary>
        [LanguageInfo("ml", "Malayalam", "മലയാളം", typeof(OneStringNumberSystem) /* unsure */)]
        Malayalam,
        /// <summary>Represents the Mongolian language (mn).</summary>
        [LanguageInfo("mn", "Mongolian", "Монгол", typeof(OneStringNumberSystem) /* unsure */)]
        Mongolian,
        /// <summary>Represents the Marathi language (mr).</summary>
        [LanguageInfo("mr", "Marathi", "मराठी", typeof(OneStringNumberSystem) /* unsure */)]
        Marathi,
        /// <summary>Represents the Malay language (ms).</summary>
        [LanguageInfo("ms", "Malay", "Bahasa Melayu", typeof(OneStringNumberSystem))]
        Malay,
        /// <summary>Represents the Maltese language (mt).</summary>
        [LanguageInfo("mt", "Maltese", "Malti", typeof(OneStringNumberSystem) /* unsure */)]
        Maltese,
        /// <summary>Represents the Burmese language (my).</summary>
        [LanguageInfo("my", "Burmese", "ဗမာစာ", typeof(OneStringNumberSystem) /* unsure */)]
        Burmese,
        /// <summary>Represents the Nauru language (na).</summary>
        [LanguageInfo("na", "Nauru", "Ekakairũ Naoero", typeof(OneStringNumberSystem) /* unsure */)]
        Nauru,
        /// <summary>Represents the Norwegian Bokmål language (nb).</summary>
        [LanguageInfo("nb", "Norwegian (Bokmål)", "Norsk (Bokmål)", typeof(Singular1PluralNumberSystem))]
        NorwegianBokmål,
        /// <summary>Represents the North Ndebele language (nd).</summary>
        [LanguageInfo("nd", "North Ndebele", "IsiNdebele", typeof(OneStringNumberSystem) /* unsure */)]
        NorthNdebele,
        /// <summary>Represents the Nepali language (ne).</summary>
        [LanguageInfo("ne", "Nepali", "नेपाली", typeof(OneStringNumberSystem) /* unsure */)]
        Nepali,
        /// <summary>Represents the Ndonga language (ng).</summary>
        [LanguageInfo("ng", "Ndonga", "Owambo", typeof(OneStringNumberSystem) /* unsure */)]
        Ndonga,
        /// <summary>Represents the Dutch language (nl).</summary>
        [LanguageInfo("nl", "Dutch", "Nederlands", typeof(Singular1PluralNumberSystem))]
        Dutch,
        /// <summary>Represents the Norwegian Nynorsk language (nn).</summary>
        [LanguageInfo("nn", "Norwegian (Nynorsk)", "Norsk (Nynorsk)", typeof(Singular1PluralNumberSystem))]
        NorwegianNynorsk,
        /// <summary>Represents the South Ndebele language (nr).</summary>
        [LanguageInfo("nr", "South Ndebele", "IsiNdebele", typeof(OneStringNumberSystem) /* unsure */)]
        SouthNdebele,
        /// <summary>Represents the Navajo language (nv).</summary>
        [LanguageInfo("nv", "Navajo", "Diné Bizaad", typeof(OneStringNumberSystem) /* unsure */)]
        Navajo,
        /// <summary>Represents the Chichewa language (ny).</summary>
        [LanguageInfo("ny", "Chichewa", "ChiCheŵa", typeof(OneStringNumberSystem) /* unsure */)]
        Chichewa,
        /// <summary>Represents the Occitan language (oc).</summary>
        [LanguageInfo("oc", "Occitan", "Occitan", typeof(OneStringNumberSystem) /* unsure */)]
        Occitan,
        /// <summary>Represents the Ojibwa language (oj).</summary>
        [LanguageInfo("oj", "Ojibwa", "ᐊᓂᔑᓈᐯᒧᐎᓐ", typeof(OneStringNumberSystem) /* unsure */)]
        Ojibwa,
        /// <summary>Represents the Oromo language (om).</summary>
        [LanguageInfo("om", "Oromo", "Afaan Oromoo", typeof(OneStringNumberSystem) /* unsure */)]
        Oromo,
        /// <summary>Represents the Oriya language (or).</summary>
        [LanguageInfo("or", "Oriya", "ଓଡ଼ିଆ", typeof(OneStringNumberSystem) /* unsure */)]
        Oriya,
        /// <summary>Represents the Ossetian language (os).</summary>
        [LanguageInfo("os", "Ossetian", "Ирон Æвзаг", typeof(OneStringNumberSystem) /* unsure */)]
        Ossetian,
        /// <summary>Represents the Panjabi language (pa), written in Devanagari.</summary>
        [LanguageInfo("pa_d", "Panjabi (Devanagari)", "ਪੰਜਾਬੀ", typeof(OneStringNumberSystem) /* unsure */)]
        PanjabiDevanagari,
        /// <summary>Represents the Panjabi language (pa), written in Arabic.</summary>
        [LanguageInfo("pa_a", "Panjabi (Arabic)", "پنجابی", typeof(OneStringNumberSystem) /* unsure */)]
        PanjabiArabic,
        /// <summary>Represents the Pāli language (pi).</summary>
        [LanguageInfo("pi", "Pāli", "पाऴि", typeof(OneStringNumberSystem) /* unsure */)]
        Pāli,
        /// <summary>Represents the Polish language (pl).</summary>
        [LanguageInfo("pl", "Polish", "Polski", typeof(PolishNumberSystem))]
        Polish,
        /// <summary>Represents the Pashto language (ps).</summary>
        [LanguageInfo("ps", "Pashto", "پښتو", typeof(OneStringNumberSystem) /* unsure */)]
        Pashto,
        /// <summary>Represents the Portuguese variety of the Portuguese language (pt).</summary>
        [LanguageInfo("pt_pt", "Portuguese (Portugal)", "Português (Portugal)", typeof(Singular1PluralNumberSystem))]
        Portuguese,
        /// <summary>Represents the Brazilian variety of the Portuguese language (pt).</summary>
        [LanguageInfo("pt_br", "Portuguese (Brazil)", "Português (Braziliano)", typeof(Singular01PluralNumberSystem))]
        BrazilianPortuguese,
        /// <summary>Represents the Quechua language (qu).</summary>
        [LanguageInfo("qu", "Quechua", "Runa Simi", typeof(OneStringNumberSystem) /* unsure */)]
        Quechua,
        /// <summary>Represents the Raeto-Romance language (rm).</summary>
        [LanguageInfo("rm", "Raeto-Romance", "Rumantsch Grischun", typeof(OneStringNumberSystem) /* unsure */)]
        RaetoRomance,
        /// <summary>Represents the Kirundi language (rn).</summary>
        [LanguageInfo("rn", "Kirundi", "KiRundi", typeof(OneStringNumberSystem) /* unsure */)]
        Kirundi,
        /// <summary>Represents the Romanian language (ro).</summary>
        [LanguageInfo("ro", "Romanian", "Română", typeof(RomanianNumberSystem))]
        Romanian,
        /// <summary>Represents the Russian language (ru).</summary>
        [LanguageInfo("ru", "Russian", "Русский", typeof(SlavicNumberSystem1))]
        Russian,
        /// <summary>Represents the Kinyarwanda language (rw).</summary>
        [LanguageInfo("rw", "Kinyarwanda", "Ikinyarwanda", typeof(OneStringNumberSystem) /* unsure */)]
        Kinyarwanda,
        /// <summary>Represents the Sanskrit language (sa).</summary>
        [LanguageInfo("sa", "Sanskrit", "संस्कृतम्", typeof(OneStringNumberSystem) /* unsure */)]
        Sanskrit,
        /// <summary>Represents the Sardinian language (sc).</summary>
        [LanguageInfo("sc", "Sardinian", "Sardu", typeof(OneStringNumberSystem) /* unsure */)]
        Sardinian,
        /// <summary>Represents the Sindhi language (sd), written in Devanagari.</summary>
        [LanguageInfo("sd_d", "Sindhi (Devanagari)", "सिन्धी", typeof(OneStringNumberSystem) /* unsure */)]
        SindhiDevanagari,
        /// <summary>Represents the Sindhi language (sd), written in Arabic.</summary>
        [LanguageInfo("sd_a", "Sindhi (Arabic)", "سنڌي، سندھی", typeof(OneStringNumberSystem) /* unsure */)]
        SindhiArabic,
        /// <summary>Represents the Northern Sami language (se).</summary>
        [LanguageInfo("se", "Northern Sami", "Davvisámegiella", typeof(OneStringNumberSystem) /* unsure */)]
        NorthernSami,
        /// <summary>Represents the Sango language (sg).</summary>
        [LanguageInfo("sg", "Sango", "Yângâ Tî Sängö", typeof(OneStringNumberSystem) /* unsure */)]
        Sango,
        /// <summary>Represents the Sinhala language (si).</summary>
        [LanguageInfo("si", "Sinhala", "සිංහල", typeof(OneStringNumberSystem) /* unsure */)]
        Sinhala,
        /// <summary>Represents the Slovak language (sk).</summary>
        [LanguageInfo("sk", "Slovak", "Slovenčina", typeof(SlavicNumberSystem2))]
        Slovak,
        /// <summary>Represents the Slovenian language (sl).</summary>
        [LanguageInfo("sl", "Slovenian", "Slovenščina", typeof(SlovenianNumberSystem))]
        Slovenian,
        /// <summary>Represents the Samoan language (sm).</summary>
        [LanguageInfo("sm", "Samoan", "Gagana Fa'a Samoa", typeof(OneStringNumberSystem) /* unsure */)]
        Samoan,
        /// <summary>Represents the Shona language (sn).</summary>
        [LanguageInfo("sn", "Shona", "ChiShona", typeof(OneStringNumberSystem) /* unsure */)]
        Shona,
        /// <summary>Represents the Somali language (so).</summary>
        [LanguageInfo("so", "Somali", "Soomaaliga", typeof(OneStringNumberSystem) /* unsure */)]
        Somali,
        /// <summary>Represents the Albanian language (sq).</summary>
        [LanguageInfo("sq", "Albanian", "Shqip", typeof(OneStringNumberSystem) /* unsure */)]
        Albanian,
        /// <summary>Represents the Serbian language (sr).</summary>
        [LanguageInfo("sr", "Serbian", "Српски Језик", typeof(SlavicNumberSystem1))]
        Serbian,
        /// <summary>Represents the Swati language (ss).</summary>
        [LanguageInfo("ss", "Swati", "SiSwati", typeof(OneStringNumberSystem) /* unsure */)]
        Swati,
        /// <summary>Represents the Southern Sotho language (st).</summary>
        [LanguageInfo("st", "Southern Sotho", "Sesotho", typeof(OneStringNumberSystem) /* unsure */)]
        SouthernSotho,
        /// <summary>Represents the Sundanese language (su).</summary>
        [LanguageInfo("su", "Sundanese", "Basa Sunda", typeof(OneStringNumberSystem) /* unsure */)]
        Sundanese,
        /// <summary>Represents the Swedish language (sv).</summary>
        [LanguageInfo("sv", "Swedish", "Svenska", typeof(Singular1PluralNumberSystem))]
        Swedish,
        /// <summary>Represents the Swahili language (sw).</summary>
        [LanguageInfo("sw", "Swahili", "Kiswahili", typeof(OneStringNumberSystem) /* unsure */)]
        Swahili,
        /// <summary>Represents the Tamil language (ta).</summary>
        [LanguageInfo("ta", "Tamil", "தமிழ்", typeof(OneStringNumberSystem) /* unsure */)]
        Tamil,
        /// <summary>Represents the Telugu language (te).</summary>
        [LanguageInfo("te", "Telugu", "తెలుగు", typeof(OneStringNumberSystem) /* unsure */)]
        Telugu,
        /// <summary>Represents the Tajik language (tg), written in Cyrillic.</summary>
        [LanguageInfo("tg_c", "Tajik (Cyrillic)", "Тоҷикӣ", typeof(OneStringNumberSystem) /* unsure */)]
        TajikCyrillic,
        /// <summary>Represents the Tajik language (tg), written in Arabic.</summary>
        [LanguageInfo("tg_a", "Tajik (Arabic)", "تاجیکی", typeof(OneStringNumberSystem) /* unsure */)]
        TajikArabic,
        /// <summary>Represents the Thai language (th).</summary>
        [LanguageInfo("th", "Thai", "ไทย", typeof(OneStringNumberSystem) /* unsure */)]
        Thai,
        /// <summary>Represents the Tigrinya language (ti).</summary>
        [LanguageInfo("ti", "Tigrinya", "ትግርኛ", typeof(OneStringNumberSystem) /* unsure */)]
        Tigrinya,
        /// <summary>Represents the Turkmen language (tk), written in Latin.</summary>
        [LanguageInfo("tk_l", "Turkmen (Latin)", "Türkmen", typeof(OneStringNumberSystem) /* unsure */)]
        TurkmenLatin,
        /// <summary>Represents the Turkmen language (tk), written in Cyrillic.</summary>
        [LanguageInfo("tk_c", "Turkmen (Cyrillic)", "Түркмен", typeof(OneStringNumberSystem) /* unsure */)]
        TurkmenCyrillic,
        /// <summary>Represents the Tagalog language (tl).</summary>
        [LanguageInfo("tl", "Tagalog", "Tagalog", typeof(TagalogNumberSystem))]
        Tagalog,
        /// <summary>Represents the Tswana language (tn).</summary>
        [LanguageInfo("tn", "Tswana", "Setswana", typeof(OneStringNumberSystem) /* unsure */)]
        Tswana,
        /// <summary>Represents the Tonga language (to).</summary>
        [LanguageInfo("to", "Tonga", "Faka Tonga", typeof(OneStringNumberSystem) /* unsure */)]
        Tonga,
        /// <summary>Represents the Turkish language (tr).</summary>
        [LanguageInfo("tr", "Turkish", "Türkçe", typeof(OneStringNumberSystem))]
        Turkish,
        /// <summary>Represents the Tsonga language (ts).</summary>
        [LanguageInfo("ts", "Tsonga", "Xitsonga", typeof(OneStringNumberSystem) /* unsure */)]
        Tsonga,
        /// <summary>Represents the Tatar language (tt), written in Cyrillic.</summary>
        [LanguageInfo("tt_c", "Tatar (Cyrillic)", "Татарча", typeof(OneStringNumberSystem) /* unsure */)]
        TatarCyrillic,
        /// <summary>Represents the Tatar language (tt), written in Latin.</summary>
        [LanguageInfo("tt_l", "Tatar (Latin)", "Tatarça", typeof(OneStringNumberSystem) /* unsure */)]
        TatarLatin,
        /// <summary>Represents the Tatar language (tt), written in Arabic.</summary>
        [LanguageInfo("tt_a", "Tatar (Arabic)", "تاتارچا", typeof(OneStringNumberSystem) /* unsure */)]
        TatarArabic,
        /// <summary>Represents the Twi language (tw).</summary>
        [LanguageInfo("tw", "Twi", "Twi", typeof(OneStringNumberSystem) /* unsure */)]
        Twi,
        /// <summary>Represents the Tahitian language (ty).</summary>
        [LanguageInfo("ty", "Tahitian", "Reo Mā`ohi", typeof(OneStringNumberSystem) /* unsure */)]
        Tahitian,
        /// <summary>Represents the Uighur language (ug), written in Latin.</summary>
        [LanguageInfo("ug_l", "Uighur (Latin)", "Uyƣurqə", typeof(OneStringNumberSystem) /* unsure */)]
        UighurLatin,
        /// <summary>Represents the Uighur language (ug), written in Arabic.</summary>
        [LanguageInfo("ug_a", "Uighur", "ئۇيغۇرچە", typeof(OneStringNumberSystem) /* unsure */)]
        UighurArabic,
        /// <summary>Represents the Ukrainian language (uk).</summary>
        [LanguageInfo("uk", "Ukrainian", "Українська", typeof(SlavicNumberSystem1))]
        Ukrainian,
        /// <summary>Represents the Urdu language (ur).</summary>
        [LanguageInfo("ur", "Urdu", "اردو", typeof(OneStringNumberSystem) /* unsure */)]
        Urdu,
        /// <summary>Represents the Uzbek language (uz), written in Latin.</summary>
        [LanguageInfo("uz_l", "Uzbek (Latin)", "O'zbek", typeof(OneStringNumberSystem) /* unsure */)]
        UzbekLatin,
        /// <summary>Represents the Uzbek language (uz), written in Cyrillic.</summary>
        [LanguageInfo("uz_c", "Uzbek (Cyrillic)", "Ўзбек", typeof(OneStringNumberSystem) /* unsure */)]
        UzbekCyrillic,
        /// <summary>Represents the Uzbek language (uz), written in Arabic.</summary>
        [LanguageInfo("uz_a", "Uzbek (Arabic)", "أۇزبېك", typeof(OneStringNumberSystem) /* unsure */)]
        UzbekArabic,
        /// <summary>Represents the Venda language (ve).</summary>
        [LanguageInfo("ve", "Venda", "Tshivenḓa", typeof(OneStringNumberSystem) /* unsure */)]
        Venda,
        /// <summary>Represents the Vietnamese language (vi).</summary>
        [LanguageInfo("vi", "Vietnamese", "Tiếng Việt", typeof(OneStringNumberSystem))]
        Vietnamese,
        /// <summary>Represents the Volapük language (vo).</summary>
        [LanguageInfo("vo", "Volapük", "Volapük", typeof(OneStringNumberSystem) /* unsure */)]
        Volapük,
        /// <summary>Represents the Walloon language (wa).</summary>
        [LanguageInfo("wa", "Walloon", "Walon", typeof(OneStringNumberSystem) /* unsure */)]
        Walloon,
        /// <summary>Represents the Wolof language (wo).</summary>
        [LanguageInfo("wo", "Wolof", "Wollof", typeof(OneStringNumberSystem) /* unsure */)]
        Wolof,
        /// <summary>Represents the Xhosa language (xh).</summary>
        [LanguageInfo("xh", "Xhosa", "IsiXhosa", typeof(OneStringNumberSystem) /* unsure */)]
        Xhosa,
        /// <summary>Represents the Yiddish language (yi).</summary>
        [LanguageInfo("yi", "Yiddish", "ייִדיש", typeof(OneStringNumberSystem) /* unsure */)]
        Yiddish,
        /// <summary>Represents the Yoruba language (yo).</summary>
        [LanguageInfo("yo", "Yoruba", "Yorùbá", typeof(OneStringNumberSystem) /* unsure */)]
        Yoruba,
        /// <summary>Represents the Zhuang language (za).</summary>
        [LanguageInfo("za", "Zhuang", "Saɯ cueŋƅ", typeof(OneStringNumberSystem) /* unsure */)]
        Zhuang,
        /// <summary>Represents the Chinese language (zh), written with traditional characters.</summary>
        [LanguageInfo("zh_tw", "Chinese (Traditional)", "中文（繁體）", typeof(OneStringNumberSystem))]
        ChineseTraditional,
        /// <summary>Represents the Chinese language (zh), written with simplified characters.</summary>
        [LanguageInfo("zh_cn", "Chinese (Simplified)", "中文（简体）", typeof(OneStringNumberSystem))]
        ChineseSimplified,
        /// <summary>Represents the Zulu language (zu).</summary>
        [LanguageInfo("zu", "Zulu", "IsiZulu", typeof(OneStringNumberSystem) /* unsure */)]
        Zulu,
        /// <summary>Represents the Klingon language (tlh).</summary>
        [LanguageInfo("tlh", "Klingon", "tlhIngan Hol", typeof(Singular1PluralNumberSystem))]
        Klingon,
        /// <summary>Represents the Lojban language (jbo).</summary>
        [LanguageInfo("jbo", "Lojban", "lojban", typeof(OneStringNumberSystem) /* unsure */)]
        Lojban
    }

    /// <summary>
    /// Extension methods for the <see cref="Language"/> enum.
    /// </summary>
    public static class LanguageMethods
    {
        /// <summary>Gets the number system associated with the specified language.</summary>
        public static NumberSystem GetNumberSystem(this Language language)
        {
            var t = typeof(Language);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                if ((Language) f.GetValue(null) == language)
                    foreach (var a in f.GetCustomAttributes<LanguageInfoAttribute>())
                        return a.NumberSystem;
            return null;
        }

        /// <summary>Gets the native name of the specified language.</summary>
        public static string GetNativeName(this Language language)
        {
            var t = typeof(Language);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                if ((Language) f.GetValue(null) == language)
                    foreach (var a in f.GetCustomAttributes<LanguageInfoAttribute>())
                        return a.NativeName;
            return null;
        }

        /// <summary>Gets the English name of the specified language.</summary>
        public static string GetEnglishName(this Language language)
        {
            var t = typeof(Language);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                if ((Language) f.GetValue(null) == language)
                    foreach (var a in f.GetCustomAttributes<LanguageInfoAttribute>())
                        return a.EnglishName;
            return null;
        }

        /// <summary>Gets the ISO language code of the specified language.</summary>
        public static string GetIsoLanguageCode(this Language language)
        {
            var t = typeof(Language);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                if ((Language) f.GetValue(null) == language)
                    foreach (var a in f.GetCustomAttributes<LanguageInfoAttribute>())
                        return a.LanguageCode;
            return null;
        }
    }
}
