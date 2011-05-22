using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Markup;

namespace RT.KitchenSink.Fonts
{
    /// <summary>Provides some assorted functionality relating to fonts.</summary>
    public static class FontUtil
    {
        /// <summary>Returns a list of all monospace fonts installed in the system.</summary>
        public static IEnumerable<string> GetMonospaceFonts()
        {
            if (_monospaceFontNames == null)
            {
                _monospaceFontNames = new HashSet<string>();
                LOGFONT lf = createLogFont("");
                IntPtr plogFont = Marshal.AllocHGlobal(Marshal.SizeOf(lf));
                Marshal.StructureToPtr(lf, plogFont, true);

                int ret = 0;
                try
                {
                    using (var bmp = new Bitmap(1024, 768, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    using (var graphics = Graphics.FromImage(bmp))
                    {
                        IntPtr hdc = graphics.GetHdc();
                        ret = EnumFontFamiliesEx(hdc, plogFont, enumFontsCallback, IntPtr.Zero, 0);
                        graphics.ReleaseHdc(hdc);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message + " (" + e.GetType().FullName + ")");
                }
                finally
                {
                    Marshal.DestroyStructure(plogFont, typeof(LOGFONT));
                }
            }
            return _monospaceFontNames;
        }

        private static HashSet<string> _monospaceFontNames;

        /// <summary>Returns a list of all installed font families that contain a glyph for all of the specified Unicode characters.</summary>
        public static IEnumerable<string> GetFontFamiliesContaining(params int[] charactersToCheck)
        {
            ICollection<System.Windows.Media.FontFamily> fontFamilies = System.Windows.Media.Fonts.GetFontFamilies(@"C:\Windows\Fonts\");
            foreach (var family in fontFamilies)
            {
                var typefaces = family.GetTypefaces();
                foreach (var typeface in typefaces)
                {
                    System.Windows.Media.GlyphTypeface glyph;
                    typeface.TryGetGlyphTypeface(out glyph);
                    ushort glyphIndex;
                    if (glyph != null && charactersToCheck.All(cc => glyph.CharacterToGlyphMap.TryGetValue(cc, out glyphIndex)))
                    {
                        string familyName;
                        if (family.FamilyNames.TryGetValue(XmlLanguage.GetLanguage("en-us"), out familyName))
                        {
                            yield return familyName;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>Returns a list of Unicode codepoints for which the specified font family has a glyph.</summary>
        /// <param name="fontFamily">The font family to check.</param>
        /// <param name="characters">A list of Unicode codepoints of the characters to check, or null to check all characters.</param>
        public static IEnumerable<int> GetSupportedCharacters(string fontFamily, IEnumerable<int> characters = null)
        {
            var family = new System.Windows.Media.FontFamily(fontFamily);
            var typeface = family.GetTypefaces().First();
            ushort glyphIndex;
            System.Windows.Media.GlyphTypeface glyph;
            if (!typeface.TryGetGlyphTypeface(out glyph))
                yield break;
            foreach (var ch in characters ?? Enumerable.Range(0, 0x110000))
                if (glyph.CharacterToGlyphMap.TryGetValue(ch, out glyphIndex))
                    yield return ch;
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern int EnumFontFamiliesEx(IntPtr hdc, [In] IntPtr pLogfont, EnumFontExDelegate lpEnumFontFamExProc, IntPtr lParam, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public FontWeight lfWeight;
            [MarshalAs(UnmanagedType.U1)]
            public bool lfItalic;
            [MarshalAs(UnmanagedType.U1)]
            public bool lfUnderline;
            [MarshalAs(UnmanagedType.U1)]
            public bool lfStrikeOut;
            public FontCharSet lfCharSet;
            public FontPrecision lfOutPrecision;
            public FontClipPrecision lfClipPrecision;
            public FontQuality lfQuality;
            public FontPitchAndFamily lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;
        }

        private enum FontWeight : int
        {
            FW_DONTCARE = 0,
            FW_THIN = 100,
            FW_EXTRALIGHT = 200,
            FW_LIGHT = 300,
            FW_NORMAL = 400,
            FW_MEDIUM = 500,
            FW_SEMIBOLD = 600,
            FW_BOLD = 700,
            FW_EXTRABOLD = 800,
            FW_HEAVY = 900,
        }
        private enum FontCharSet : byte
        {
            ANSI_CHARSET = 0,
            DEFAULT_CHARSET = 1,
            SYMBOL_CHARSET = 2,
            SHIFTJIS_CHARSET = 128,
            HANGEUL_CHARSET = 129,
            HANGUL_CHARSET = 129,
            GB2312_CHARSET = 134,
            CHINESEBIG5_CHARSET = 136,
            OEM_CHARSET = 255,
            JOHAB_CHARSET = 130,
            HEBREW_CHARSET = 177,
            ARABIC_CHARSET = 178,
            GREEK_CHARSET = 161,
            TURKISH_CHARSET = 162,
            VIETNAMESE_CHARSET = 163,
            THAI_CHARSET = 222,
            EASTEUROPE_CHARSET = 238,
            RUSSIAN_CHARSET = 204,
            MAC_CHARSET = 77,
            BALTIC_CHARSET = 186,
        }
        private enum FontPrecision : byte
        {
            OUT_DEFAULT_PRECIS = 0,
            OUT_STRING_PRECIS = 1,
            OUT_CHARACTER_PRECIS = 2,
            OUT_STROKE_PRECIS = 3,
            OUT_TT_PRECIS = 4,
            OUT_DEVICE_PRECIS = 5,
            OUT_RASTER_PRECIS = 6,
            OUT_TT_ONLY_PRECIS = 7,
            OUT_OUTLINE_PRECIS = 8,
            OUT_SCREEN_OUTLINE_PRECIS = 9,
            OUT_PS_ONLY_PRECIS = 10,
        }
        private enum FontClipPrecision : byte
        {
            CLIP_DEFAULT_PRECIS = 0,
            CLIP_CHARACTER_PRECIS = 1,
            CLIP_STROKE_PRECIS = 2,
            CLIP_MASK = 0xf,
            CLIP_LH_ANGLES = (1 << 4),
            CLIP_TT_ALWAYS = (2 << 4),
            CLIP_DFA_DISABLE = (4 << 4),
            CLIP_EMBEDDED = (8 << 4),
        }
        private enum FontQuality : byte
        {
            DEFAULT_QUALITY = 0,
            DRAFT_QUALITY = 1,
            PROOF_QUALITY = 2,
            NONANTIALIASED_QUALITY = 3,
            ANTIALIASED_QUALITY = 4,
            CLEARTYPE_QUALITY = 5,
            CLEARTYPE_NATURAL_QUALITY = 6,
        }
        [Flags]
        private enum FontPitchAndFamily : byte
        {
            DEFAULT_PITCH = 0,
            FIXED_PITCH = 1,
            VARIABLE_PITCH = 2,
            FF_DONTCARE = (0 << 4),
            FF_ROMAN = (1 << 4),
            FF_SWISS = (2 << 4),
            FF_MODERN = (3 << 4),
            FF_SCRIPT = (4 << 4),
            FF_DECORATIVE = (5 << 4),
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NEWTEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
            public int ntmFlags;
            public int ntmSizeEM;
            public int ntmCellHeight;
            public int ntmAvgWidth;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct FONTSIGNATURE
        {
            [MarshalAs(UnmanagedType.ByValArray)]
            public int[] fsUsb;
            [MarshalAs(UnmanagedType.ByValArray)]
            public int[] fsCsb;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct NEWTEXTMETRICEX
        {
            public NEWTEXTMETRIC ntmTm;
            public FONTSIGNATURE ntmFontSig;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct ENUMLOGFONTEX
        {
            public LOGFONT elfLogFont;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string elfFullName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string elfStyle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string elfScript;
        }

        private const byte DEFAULT_CHARSET = 1;
        private const byte SHIFTJIS_CHARSET = 128;
        private const byte JOHAB_CHARSET = 130;
        private const byte EASTEUROPE_CHARSET = 238;

        private const byte DEFAULT_PITCH = 0;
        private const byte FIXED_PITCH = 1;
        private const byte VARIABLE_PITCH = 2;
        private const byte FF_DONTCARE = (0 << 4);
        private const byte FF_ROMAN = (1 << 4);
        private const byte FF_SWISS = (2 << 4);
        private const byte FF_MODERN = (3 << 4);
        private const byte FF_SCRIPT = (4 << 4);
        private const byte FF_DECORATIVE = (5 << 4);

        private delegate int EnumFontExDelegate(ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, int FontType, int lParam);

        private static int enumFontsCallback(ref ENUMLOGFONTEX lpelfe, ref NEWTEXTMETRICEX lpntme, int FontType, int lParam)
        {
            if (lpelfe.elfLogFont.lfPitchAndFamily.HasFlag(FontPitchAndFamily.FIXED_PITCH))
                _monospaceFontNames.Add(lpelfe.elfFullName);
            return 1;
        }

        private static LOGFONT createLogFont(string fontname)
        {
            LOGFONT lf = new LOGFONT();
            lf.lfHeight = 0;
            lf.lfWidth = 0;
            lf.lfEscapement = 0;
            lf.lfOrientation = 0;
            lf.lfWeight = 0;
            lf.lfItalic = false;
            lf.lfUnderline = false;
            lf.lfStrikeOut = false;
            lf.lfCharSet = FontCharSet.DEFAULT_CHARSET;
            lf.lfOutPrecision = 0;
            lf.lfClipPrecision = 0;
            lf.lfQuality = 0;
            lf.lfPitchAndFamily = FontPitchAndFamily.FF_DONTCARE;
            lf.lfFaceName = "";
            return lf;
        }
    }
}
