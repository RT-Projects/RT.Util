using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Web;

namespace R.Util.Web
{
    /// <summary>
    /// Helper class for interacting with a web site via HTTP.
    /// </summary>
    public class Http
    {
        /// <summary>
        /// This event is invoked to report status messages - once at the start and once
        /// at the end of every request.
        /// </summary>
        public Action<string> OnReportStatus = null;
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
        public string LastHtml = null;
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
        /// Contains the status code of the last request.
        /// </summary>
        public int LastStatusCode = -1;
        /// <summary>
        /// True if the last request was successful.
        /// </summary>
        public bool LastSuccess = false;

        #region Constructors

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        public Http()
        {
        }

        public Http(string siteUrl)
        {
            SiteUrl = siteUrl;
        }

        public Http(string siteUrl, string userAgent)
        {
            SiteUrl = siteUrl;
            UserAgent = userAgent;
        }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

        #endregion

        #region Cookie stuff

        private CookieContainer _cookies = new CookieContainer();

        /// <summary>
        /// Loads cookies from the specified stream. On success returns true. On failure
        /// returns false and leaves the cookies as they were (which is no cookies when
        /// an instance of this class is created).
        /// </summary>
        public bool LoadCookies(Stream stream)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                _cookies = (CookieContainer) bf.Deserialize(stream);
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
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, _cookies);
        }

        /// <summary>
        /// Clears all cookies from this instance of the class.
        /// </summary>
        public void ClearCookies()
        {
            _cookies = new CookieContainer();
        }

        #endregion

        private Queue<DateTime> _lastRequestTime = new Queue<DateTime>();

        /// <summary>
        /// Performs an HTTP request to the specified Url on the Site associated with this instance. If
        /// everything is fine returns OK.
        /// <param name="url">The Url to be requested, relative to the SiteUrl</param>
        /// <param name="doGet">If true, method is GET. Otherwise method is POST.</param>
        /// <param name="parameters">A set of parameters to be sent. These are URL-encoded automatically and posted appropriately for the specified method. Can be null.</param>
        /// <param name="referer">An optional Referer header. Can be null.</param>
        /// <param name="maxRedirectDepth">Maximum number of HTTP redirects to follow.</param>
        /// </summary>
        private bool doRequest(string url, bool doGet, Dictionary<string, string> parameters, string referer, int maxRedirectDepth)
        {
            // Initialise
            LastHtml = null;
            LastResponse = null;
            LastException = null;
            if (SiteUrl[SiteUrl.Length - 1] != '/')
                SiteUrl = SiteUrl + "/";
            url = SiteUrl + url;
            if (OnReportStatus != null) OnReportStatus((doGet ? "GET " : "POST ") + url);

            // Maintain requests-per-minute
            DateTime curTime = DateTime.Now;
            do
            {
                while (_lastRequestTime.Count > 0 && (curTime - _lastRequestTime.Peek()) > TimeSpan.FromSeconds(60))
                    _lastRequestTime.Dequeue();
                if (_lastRequestTime.Count >= MaxRequestsPerMinute)
                    // Gotta wait, have already made that many requests in the last minute
                    Thread.Sleep(curTime - _lastRequestTime.Peek());
                // In theory one iteration is all we need, but that relies on the fact that the statement
                // above has worked as intended. Much better if we go and update the queue once again
                // and check everything again. Note that multiple iterations only happen when we /are/
                // hitting the maximum-requests limit.
            } while (_lastRequestTime.Count >= MaxRequestsPerMinute);

            // Add this request to the list of request times
            _lastRequestTime.Enqueue(DateTime.Now);

            // Prepare parameters, if any
            StringBuilder paramsSB = new StringBuilder();
            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> kvp in parameters)
                {
                    paramsSB.Append(HttpUtility.UrlEncode(kvp.Key));
                    paramsSB.Append('=');
                    paramsSB.Append(HttpUtility.UrlEncode(kvp.Value));
                    paramsSB.Append('&');
                }
                if (parameters.Count > 0)
                    paramsSB.Remove(paramsSB.Length - 1, 1);
            }

            // Prepare the request
            HttpWebRequest request;
            if (doGet && paramsSB.Length > 0)
                request = (HttpWebRequest) WebRequest.Create(url + "?" + paramsSB.ToString());
            else
                request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = doGet ? "GET" : "POST";
            request.UserAgent = UserAgent;
            request.CookieContainer = _cookies;
            if (referer != null)
                request.Referer = referer;

            if (!doGet)
            {
                // Attach the data to the POST request
                byte[] data = (new ASCIIEncoding()).GetBytes(paramsSB.ToString());
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
                LastResponse = (HttpWebResponse) request.GetResponse();
            }
            catch (WebException e)
            {
                LastResponse = (HttpWebResponse) e.Response;
            }

            // Get the response as a string
            LastHtml = streamToString(LastResponse.GetResponseStream(), new ASCIIEncoding());
            // Dump it to a debug location on the disk if necessary
            if (DebugDumpLocation != null)
                dumpHtml(maxRedirectDepth);

            // Do redirect if necessary
            int respcode = (int) LastResponse.StatusCode;
            if (respcode >= 300 && respcode <= 399 && doGet                                    // redirection necessary
                && maxRedirectDepth > 0 && LastResponse.Headers[HttpResponseHeader.Location] != null) // redirection possible
            {
                if (OnReportStatus != null) OnReportStatus("Redirected to: " + url);
                return doRequest(LastResponse.Headers[HttpResponseHeader.Location], true, null, url, maxRedirectDepth - 1);
                // The last of the redirects will set all the global fields as necessary, so we've
                // got nothing else to do here.
            }

            if (OnReportStatus != null) OnReportStatus("Done.");
            LastStatusCode = respcode;
            LastSuccess = respcode >= 200 && respcode <= 299;
            return LastSuccess;
        }

        /// <summary>
        /// Performs a GET request for the specified Url.
        /// Returns true on success.
        /// </summary>
        public bool Get(string url)
        {
            _requestCounter++;
            return doRequest(url, true, null, null, MaxRedirectCount);
        }

        /// <summary>
        /// Performs a POST request for the specified Url, passing the specified arguments.
        /// Returns true on success.
        /// </summary>
        public bool Post(string url, Dictionary<string, string> parameters)
        {
            _requestCounter++;
            return doRequest(url, false, parameters, null, MaxRedirectCount);
        }

        #region Private helper functions

        private int _requestCounter = -1;

        private void dumpHtml(int depthLeft)
        {
            string filename = string.Format(DebugDumpLocation, _requestCounter, MaxRedirectCount - depthLeft);
            StreamWriter tw = new StreamWriter(filename);
            tw.WriteLine("<!-- This information is inserted by the Http class.");
            tw.WriteLine("Status code: " + ((int) LastResponse.StatusCode) + " " + LastResponse.StatusCode);
            tw.WriteLine("Response URI: " + LastResponse.ResponseUri);
            tw.WriteLine("Status code: " + ((int) LastResponse.StatusCode) + " " + LastResponse.StatusCode);
            tw.WriteLine("Headers:");
            foreach (string hdr in LastResponse.Headers.AllKeys)
                if (LastResponse.Headers[hdr] != null)
                    tw.WriteLine("  {0}: {1}", hdr, LastResponse.Headers[hdr]);
            tw.WriteLine("END of information inserted by the Http class. -->");

            tw.Write(LastHtml);
            tw.Close();
        }

        private string streamToString(Stream stream, Encoding encoding)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            int count;

            do
            {
                count = stream.Read(buf, 0, buf.Length);

                if (count != 0)
                    sb.Append(encoding.GetString(buf, 0, count));
            }
            while (count > 0);

            return sb.ToString();
        }

        #endregion

    }

}
