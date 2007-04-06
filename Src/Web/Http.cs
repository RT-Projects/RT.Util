using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Web;
using System.Threading;

namespace RT.Util.Web
{
    public class Http
    {
        /// <summary>
        /// This event is invoked to report status messages - once at the start and once
        /// at the end of every request.
        /// </summary>
        public VoidFunc<string> OnReportStatus = null;
        /// <summary>
        /// This class is intended to use with a single website per instance. SiteUrl is the
        /// domain to be accessed, along with the protocol specifier, e.g. http://www.example.com/
        /// If the slash at the end is not present, it will be added on first request.
        /// </summary>
        public string SiteUrl = "http://www.example.com/";
        /// <summary>
        /// The user agent string to be used with every request. Defaults to Firefox 2.0.0.3.
        /// </summary>
        public string UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-GB; rv:1.8.1.3) Gecko/20070309 Firefox/2.0.0.3";
        /// <summary>
        /// The maximum number of redirects that can be followed automatically. Only redirects for
        /// the GET method are followed. Set this to 0 to disable redirects completely.
        /// </summary>
        public int MaxRedirectCount = 5;
        /// <summary>
        /// Provides a built-in way to ensure that a certain maximum number of requests per minute
        /// is never exceeded, e.g. due to a bug in the application using this class. Whenever the
        /// threshold is reached, Thread.Sleep is invoked to wait until a request can be executed
        /// without exceeding this threshold.
        /// </summary>
        public int MaxRequestsPerMinute = 60;

        /// <summary>
        /// A string containing a file path formatting string. Every request's result is dumped
        /// into a new file, whose name is constructed using this string. Argument 0 passed to
        /// String.Format will be incremented for every request; arg 1 - for every redirect within
        /// a request. Set this to null to disable response dumps.
        /// </summary>
        public string DebugDumpLocation = null;

        /// <summary>
        /// Contains the data received in response to the most recent request.
        /// </summary>
        public string LastHTML = null;
        /// <summary>
        /// Encapsulates the response to the most recent request.
        /// </summary>
        public HttpWebResponse LastResponse = null;
        /// <summary>
        /// If a request fails and both LastHTML and LastResponse are null, this will
        /// contain the WebException that occurred. A successful request clears this
        /// to null.
        /// </summary>
        public WebException LastException = null;
        /// <summary>
        /// Contains the return value of the last request.
        /// </summary>
        public bool LastResult = false;

        #region Constructors

        public Http()
        {
        }

        public Http(string SiteUrl)
        {
            this.SiteUrl = SiteUrl;
        }

        public Http(string SiteUrl, string UserAgent)
        {
            this.SiteUrl = SiteUrl;
            this.UserAgent = UserAgent;
        }

        #endregion

        #region Cookie stuff

        private CookieContainer Cookies = new CookieContainer();

        /// <summary>
        /// Loads cookies from the specified stream. On success returns true. On failure
        /// returns false and leaves the cookies as they were (which is no cookies when
        /// an instance of this class is created).
        /// </summary>
        public bool LoadCookies(Stream stream)
        {
            try
            {
                BinaryFormatter BF = new BinaryFormatter();
                Cookies = (CookieContainer)BF.Deserialize(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Saves cookies to the specified stream using serialization.
        /// </summary>
        public void SaveCookies(Stream stream)
        {
            BinaryFormatter BF = new BinaryFormatter();
            BF.Serialize(stream, Cookies);
        }

        /// <summary>
        /// Clears all cookies from this instance of the class.
        /// </summary>
        public void ClearCookies()
        {
            Cookies = new CookieContainer();
        }

        #endregion

        private Queue<DateTime> LastRequestTime = new Queue<DateTime>();

        /// <summary>
        /// Performs an HTTP request to the specified Url on the Site associated with this instance. If
        /// everything is fine returns OK.
        /// <param name="Url">The Url to be requested, relative to the SiteUrl</param>
        /// <param name="DoGet">If true, method is GET. Otherwise method is POST.</param>
        /// <param name="Params">A set of parameters to be sent. These are URL-encoded automatically and posted appropriately for the specified method. Can be null.</param>
        /// <param name="Referer">An optional Referer header. Can be null.</param>
        /// </summary>
        private bool DoRequest(string Url, bool DoGet, Dictionary<string, string> Params, string Referer, int DepthLeft)
        {
            // Initialise
            LastHTML = null;
            LastResponse = null;
            LastException = null;
            if (SiteUrl[SiteUrl.Length-1] != '/')
                SiteUrl = SiteUrl + "/";
            Url = SiteUrl + Url;
            if (OnReportStatus!=null) OnReportStatus((DoGet ? "GET " : "POST ") + Url);

            // Maintain requests-per-minute
            DateTime curTime = DateTime.Now;
            do
            {
                while (LastRequestTime.Count > 0 && (curTime - LastRequestTime.Peek()) > TimeSpan.FromSeconds(60))
                    LastRequestTime.Dequeue();
                if (LastRequestTime.Count >= MaxRequestsPerMinute)
                    // Gotta wait, have already made that many requests in the last minute
                    Thread.Sleep(curTime - LastRequestTime.Peek());
                // In theory one iteration is all we need, but that relies on the fact that the statement
                // above has worked as intended. Much better if we go and update the queue once again
                // and check everything again. Note that multiple iterations only happen when we /are/
                // hitting the maximum-requests limit.
            } while (LastRequestTime.Count >= MaxRequestsPerMinute);

            // Add this request to the list of request times
            LastRequestTime.Enqueue(DateTime.Now);

            // Prepare parameters, if any
            StringBuilder ParamsSB = new StringBuilder();
            if (Params != null)
            {
                foreach (KeyValuePair<string, string> kvp in Params)
                {
                    ParamsSB.Append(HttpUtility.UrlEncode(kvp.Key));
                    ParamsSB.Append('=');
                    ParamsSB.Append(HttpUtility.UrlEncode(kvp.Value));
                    ParamsSB.Append('&');
                }
                if (Params.Count > 0)
                    ParamsSB.Remove(ParamsSB.Length-1, 1);
            }

            // Prepare the request
            HttpWebRequest request;
            if (DoGet && ParamsSB.Length > 0)
                request = (HttpWebRequest)WebRequest.Create(Url + "?" + ParamsSB.ToString());
            else
                request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = DoGet ? "GET" : "POST";
            request.UserAgent = UserAgent;
            request.CookieContainer = Cookies;
            if (Referer != null)
                request.Referer = Referer;

            if (!DoGet)
            {
                // Attach the data to the POST request
                byte[] data = (new ASCIIEncoding()).GetBytes(ParamsSB.ToString());
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                // This must be done after all other fields are set - apparently
                // GetRequestStream does something... uhm... complicated.
                Stream datastream = request.GetRequestStream();
                datastream.Write(data, 0, data.Length);
                datastream.Close();
            }

            // Send the request!
            try
            {
                LastResponse = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException E)
            {
                LastException = E;
                if (OnReportStatus!=null) OnReportStatus((DoGet ? "GET failed: " : "POST failed: ") + Url);
                return LastResult = false; // yes, _assignment_ here
            }

            // Get the response as a string
            LastHTML = StreamToString(LastResponse.GetResponseStream(), new ASCIIEncoding());
            // Dump it to a debug location on the disk if necessary
            if (DebugDumpLocation != null)
                DumpHtml(DepthLeft);

            // Do redirect if necessary
            int respcode = (int)LastResponse.StatusCode;
            if (respcode >= 300 && respcode <= 399 && DoGet                                    // redirection necessary
                && DepthLeft > 0 && LastResponse.Headers[HttpResponseHeader.Location] != null) // redirection possible
            {
                if (OnReportStatus!=null) OnReportStatus("Redirected to: " + Url);
                return DoRequest(LastResponse.Headers[HttpResponseHeader.Location], true, null, Url, DepthLeft-1);
                // The last of the redirects will set all the global fields as necessary, so we've
                // got nothing else to do here.
            }

            if (OnReportStatus!=null) OnReportStatus("Done.");
            LastResult = respcode >= 200 && respcode <= 299;
            return LastResult;
        }

        public bool Get(string Url)
        {
            RequestCounter++;
            return DoRequest(Url, true, null, null, MaxRedirectCount);
        }

        public bool Post(string Url, Dictionary<string, string> Params)
        {
            RequestCounter++;
            return DoRequest(Url, false, Params, null, MaxRedirectCount);
        }

        #region Private helper functions

        private int RequestCounter = -1;

        private void DumpHtml(int DepthLeft)
        {
            string FileName = string.Format(DebugDumpLocation, RequestCounter, MaxRedirectCount - DepthLeft);
            StreamWriter TW = new StreamWriter(FileName);
            TW.WriteLine("<!-- This information is inserted by the Http class.");
            TW.WriteLine("Status code: " + ((int)LastResponse.StatusCode) + " " + LastResponse.StatusCode);
            TW.WriteLine("Response URI: " + LastResponse.ResponseUri);
            TW.WriteLine("Status code: " + ((int)LastResponse.StatusCode) + " " + LastResponse.StatusCode);
            TW.WriteLine("Headers:");
            foreach (string hdr in LastResponse.Headers.AllKeys)
                if (LastResponse.Headers[hdr] != null)
                    TW.WriteLine("  {0}: {1}", hdr, LastResponse.Headers[hdr]);
            TW.WriteLine("END of information inserted by the Http class. -->");

            TW.Write(LastHTML);
            TW.Close();
        }

        private string StreamToString(Stream S, Encoding E)
        {
            StringBuilder sb  = new StringBuilder();
            byte[] buf = new byte[8192];
            int count;

            do
            {
                count = S.Read(buf, 0, buf.Length);

                if (count != 0)
                    sb.Append(E.GetString(buf, 0, count));
            }
            while (count > 0);

            return sb.ToString();
        }

        #endregion

    }

}
