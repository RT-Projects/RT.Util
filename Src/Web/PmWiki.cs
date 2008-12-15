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
        public Http Site;

        /// <summary>
        /// PmWiki edit password - this will be posted automatically if PmWiki asks
        /// for it.
        /// </summary>
        public string Password = "";

        private PmWiki() { }

        /// <summary>
        /// Constructs an instance of PmWiki located at the specified Url.
        /// </summary>
        public PmWiki(string url)
        {
            Site = new Http(url);
        }

        /// <summary>
        /// Retrieves the source of the specified page. If retrieval fails for
        /// any reason (including 404 status code) returns null.
        /// </summary>
        public string LoadPage(string page)
        {
            if (!Site.Get(page + "?action=source"))
                return null;

            return Site.LastHtml;
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
            if (!Site.Get(page + "?action=edit"))
                return false;

            // Check if password needed
            if (Site.LastHtml.Contains(@"<strong>Password required</strong>")
                && Site.LastHtml.Contains(@"<input type='password' name='authpw'"))
            {
                Dictionary<string, string> p = new Dictionary<string, string>();
                p["authpw"] = Password;
                if (!Site.Post(page + "?action=edit", p))
                    return false;
            }

            // Parse the edit form (just a little bit)
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (!Site.LastHtml.Contains(@"type='hidden' name='action' value='edit'"))
                return false;
            parameters["action"] = "edit";

            Match m;
            m = Regex.Match(Site.LastHtml, @"type='hidden' name='n' value='([^']+)'");
            if (!m.Success)
                return false;
            parameters["n"] = m.Groups[1].Value;

            m = Regex.Match(Site.LastHtml, @"type='hidden' name='basetime' value='([^']+)' />");
            if (!m.Success)
                return false;
            parameters["basetime"] = m.Groups[1].Value;

            parameters["csum"] = "PmWiki auto editor, at " + DateTime.Now.ToString();
            parameters["text"] = source;
            parameters["post"] = " Save ";

            // Post it!
            if (!Site.Post(page + "?action=edit", parameters))
                return false;
            if (Site.LastHtml.Contains(@"<strong>Password required</strong>")
                && Site.LastHtml.Contains(@"<input type='password' name='authpw'"))
                return false;

            return true;
        }
    }
}
