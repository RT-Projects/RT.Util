using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RT.Util.Web
{
    /// <summary>
    /// Allows PmWiki pages to be retrieved and edited.
    /// </summary>
    public class PmWiki
    {
        /// <summary>
        /// The instance of the Http class used to access PmWiki. Provided here to
        /// allow various features of Http to be used - such as debug logging.
        /// </summary>
        public Http TheSite;

        /// <summary>
        /// PmWiki edit password - this will be posted automatically if PmWiki asks
        /// for it.
        /// </summary>
        public string Password = "";

        private PmWiki() { }

        /// <summary>
        /// Constructs an instance of PmWiki located at the specified Url.
        /// </summary>
        public PmWiki(string Url)
        {
            TheSite = new Http(Url);
        }

        /// <summary>
        /// Retrieves the source of the specified page. If retrieval fails for
        /// any reason (including 404 status code) returns null.
        /// </summary>
        public string LoadPage(string page)
        {
            if (!TheSite.Get(page+"?action=source"))
                return null;

            return TheSite.LastHTML;
        }

        /// <summary>
        /// Saves the specified source at the specified page. Logs in if necessary.
        /// Adds an edit comment saying that the page was auto-edited. Returns false
        /// if fails, or true if it /thinks/ it succeeded - however that is not
        /// guaranteed, so to be certain the page must be loaded again.
        /// </summary>
        public bool SavePage(string page, string source)
        {
            // Invoke the Edit command
            if (!TheSite.Get(page+"?action=edit"))
                return false;

            // Check if password needed
            if (TheSite.LastHTML.Contains(@"<strong>Password required</strong>")
                && TheSite.LastHTML.Contains(@"<input type='password' name='authpw'"))
            {
                Dictionary<string, string> p = new Dictionary<string, string>();
                p["authpw"] = Password;
                if (!TheSite.Post(page+"?action=edit", p))
                    return false;
            }

            // Parse the edit form (just a little bit)
            Dictionary<string, string> P = new Dictionary<string, string>();
            if (!TheSite.LastHTML.Contains(@"type='hidden' name='action' value='edit'"))
                return false;
            P["action"] = "edit";

            Match m;
            m = Regex.Match(TheSite.LastHTML, @"type='hidden' name='n' value='([^']+)'");
            if (!m.Success)
                return false;
            P["n"] = m.Groups[1].Value;

            m = Regex.Match(TheSite.LastHTML, @"type='hidden' name='basetime' value='([^']+)' />");
            if (!m.Success)
                return false;
            P["basetime"] = m.Groups[1].Value;

            P["csum"] = "PmWiki auto editor, at " + DateTime.Now.ToString();
            P["text"] = source;
            P["post"] = " Save ";

            // Post it!
            if (!TheSite.Post(page+"?action=edit", P))
                return false;
            if (TheSite.LastHTML.Contains(@"<strong>Password required</strong>")
                && TheSite.LastHTML.Contains(@"<input type='password' name='authpw'"))
                return false;

            return true;
        }
    }
}
