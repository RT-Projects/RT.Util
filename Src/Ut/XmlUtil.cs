using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace RT.Util
{
    public static class XmlUtil
    {
        /// <summary>
        /// Converts the XmlDocument into indented text, stored in the returned
        /// StringBuilder.
        ///
        /// Check out the implementation... it's full of rants!
        /// </summary>
        public static StringBuilder ToStringBuilder(XmlDocument doc)
        {
            StringBuilder sb = new StringBuilder();
            // This area of .NET's API was so totally written by idiot students
            // and never ever reviewed. All the defaults are screwed up and the
            // documentation lies about them. The API itself is braindead and
            // very hard to use. No wonder it's been rewritten from scratch
            // in .NET 3.5
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = Encoding.UTF8;
            settings.OmitXmlDeclaration = true;
            // (this declaration is fucked up anyway because it lies that the encoding
            //  is utf-16 when it's actually utf-8)

            settings.IndentChars = "    ";
            settings.Indent = true;
            // moronic documentation doesn't mention that this also controls whether
            // to ever use any newlines.

            XmlWriter whyTheFuckIsThisClassCached = XmlWriter.Create(sb, settings);
            doc.WriteTo(whyTheFuckIsThisClassCached);
            whyTheFuckIsThisClassCached.Close(); // FLUSH DAMN IT!

            // God knows why the XmlWriter is cached. It's the underlying
            // steam's business to do the caching - in cases where that's necessary.
            // And the underlying streams _do_ do caching. Hence all of XmlWriter's
            // own caching is adding nothing but overhead (and debugging time.)
            // ARGH!

            return sb;
        }
    }
}
