using System;
using System.IO;
using System.Linq;
using System.Net;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace RT.Util
{
    /// <summary>Provides very simple and easy-to-use methods to run an HTTP request and receive the response.</summary>
    public class HClient
    {
        /// <summary>Specifies the default root URL. See <see cref="RootUrl"/> for more information.</summary>
        public static string DefaultRootUrl = null;
        /// <summary>Specifies the default logger. See <see cref="Log"/> for more information.</summary>
        public static LoggerBase DefaultLog = new NullLogger();

        /// <summary>Contains the cookies to be sent to the server and received from the server.</summary>
        public CookieContainer Cookies = new CookieContainer();
        /// <summary>Specifies a logger that logs all outgoing requests and responses.</summary>
        public LoggerBase Log = DefaultLog;

        /// <summary>
        ///     Exposes a collection of request headers. See Remarks.</summary>
        /// <remarks>
        ///     HttpWebRequest labels certain headers "restricted" and requires them to be modified by modifying request
        ///     properties, rather than setting values via the Headers collection. This class unrestricts some of those
        ///     headers, specifically: Accept, Host, Referer, TransferEncoding, UserAgent. Some of the restricted headers are
        ///     only supported partially, indirectly or via properties, specifically: Connection, ContentType, ContentLength,
        ///     Expect. The remaining restricted headers cannot currently be modified at all, specifically: Date,
        ///     IfModifiedSince, ProxyConnection, Range</remarks>
        public WebHeaderCollection ReqHeaders { get; set; }

        /// <summary>
        ///     A shorthand for getting and setting ReqHeaders[HttpRequestHeader.Accept]. Defaults to what a recent version of
        ///     US Firefox might send.</summary>
        public string ReqAccept { get { return ReqHeaders[HttpRequestHeader.Accept]; } set { ReqHeaders[HttpRequestHeader.Accept] = value; } }
        /// <summary>
        ///     A shorthand for getting and setting ReqHeaders[HttpRequestHeader.AcceptLanguage]. Defaults to what a recent
        ///     version of US Firefox might send.</summary>
        public string ReqAcceptLanguage { get { return ReqHeaders[HttpRequestHeader.AcceptLanguage]; } set { ReqHeaders[HttpRequestHeader.AcceptLanguage] = value; } }
        /// <summary>
        ///     A shorthand for getting and setting ReqHeaders[HttpRequestHeader.UserAgent] Defaults to what a recent version
        ///     of US Firefox might send..</summary>
        public string ReqUserAgent { get { return ReqHeaders[HttpRequestHeader.UserAgent]; } set { ReqHeaders[HttpRequestHeader.UserAgent] = value; } }
        /// <summary>A shorthand for getting and setting ReqHeaders[HttpRequestHeader.Referer]. Defaults to null</summary>
        public string ReqReferer { get { return ReqHeaders[HttpRequestHeader.Referer]; } set { ReqHeaders[HttpRequestHeader.Referer] = value; } }

        /// <summary>
        ///     Specifies how long to wait for the server to respond to the request before throwing an exception. Defaults to
        ///     10 seconds.</summary>
        public TimeSpan Timeout { get; set; }
        /// <summary>
        ///     Specifies whether the receipt of a redirect should automatically generate a new request for the new URL.
        ///     Defaults to false.</summary>
        public bool AllowAutoRedirect { get; set; }
        /// <summary>
        ///     Specifies which compression methods are supported. This affects the Accept-Encoding header and automatically
        ///     decompresses the response if necessary. Defaults to gzip+deflate, as used by modern browsers.</summary>
        public DecompressionMethods AutomaticDecompression { get; set; }
        /// <summary>Specifies whether to use keep-alive for follow-up requests. Defaults to true.</summary>
        public bool KeepAlive { get; set; }
        /// <summary>
        ///     Specifies whether to use Expect: 100 (continue) for post requests, thus querying the server for whether it
        ///     wants to accept the post request, before sending the request body. Defaults to false. If true, the entire
        ///     interaction is handled transparently.</summary>
        public bool Expect100Continue { get; set; }


        /// <summary>
        ///     Specifies the root URL. If the request URL begins with <c>http://</c> or <c>https://</c>, this is ignored.
        ///     Otherwise the URL is prepended with this value.</summary>
        public string RootUrl = DefaultRootUrl;

        /// <summary>Constructor. Initializes the request to look like it came from a recent version of Firefox.</summary>
        public HClient()
        {
            Timeout = TimeSpan.FromSeconds(10);
            AllowAutoRedirect = false;
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            KeepAlive = true;
            Expect100Continue = false;

            ReqHeaders = new WebHeaderCollection();
            ReqReferer = null;
            ReqUserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.0";
            ReqAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            ReqAcceptLanguage = "en-US,en;q=0.5";
        }

        /// <summary>
        ///     Performs a GET request to the specified URL and with the specified query parameters.</summary>
        /// <param name="url">
        ///     The URL of the request. If the URL does not begin with <c>http://</c> or <c>https://</c>, it is automatically
        ///     prepended with <see cref="RootUrl"/>.</param>
        /// <param name="args">
        ///     Query parameters to add to the end of the URL in the usual <c>?k1=v1&amp;k2=v2&amp;...</c> format. Null values
        ///     are allowed and ignored.</param>
        /// <returns>
        ///     The response received from the server.</returns>
        public HResponse Get(string url, params HArg[] args)
        {
            if (args == null)
                args = new HArg[0];
            if (!args.Where(a => a != null).All(a => a.ValidForUrlEncoded))
                throw new ArgumentException();
            var request = makeRequest(url + (args.Where(a => a != null).Any() ? (url.Contains('?') ? "&" : "?") : "") + args.Where(a => a != null).Select(a => a.Name.UrlEscape() + "=" + a.Value.UrlEscape()).JoinString("&"));
            request.Method = "GET";
            return performRequest(request);
        }

        /// <summary>
        ///     Performs a POST request with the body of the request specified as raw data.</summary>
        /// <param name="url">
        ///     The URL of the request. If the URL does not begin with <c>http://</c> or <c>https://</c>, it is automatically
        ///     prepended with <see cref="RootUrl"/>.</param>
        /// <param name="bytes">
        ///     The body of the request, as raw data.</param>
        /// <param name="contentType">
        ///     The value of the <c>Content-Type</c> header.</param>
        /// <returns>
        ///     The response received from the server.</returns>
        public HResponse Post(string url, byte[] bytes, string contentType)
        {
            var request = makeRequest(url);
            request.Method = "POST";
            request.ContentType = contentType;
            request.ContentLength = bytes.Length;

            var stream = request.GetRequestStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Close();
            return performRequest(request);
        }

        /// <summary>
        ///     Performs a POST request with the body consisting of a series of key-value arguments.</summary>
        /// <param name="url">
        ///     The URL of the request. If the URL does not begin with <c>http://</c> or <c>https://</c>, it is automatically
        ///     prepended with <see cref="RootUrl"/>.</param>
        /// <param name="args">
        ///     The arguments to pass with the POST request. Null values are allowed and ignored.</param>
        /// <returns>
        ///     The response received from the server.</returns>
        /// <remarks>
        ///     This method automatically calls either <see cref="PostFormdata"/> or <see cref="PostUrlencoded"/> depending on
        ///     whether the provided arguments contain any file uploads or not.</remarks>
        public HResponse Post(string url, params HArg[] args)
        {
            if (args == null)
                args = new HArg[0];
            return args.Where(a => a != null).All(a => a.ValidForUrlEncoded) ? PostUrlencoded(url, args) : PostFormdata(url, args);
        }

        /// <summary>
        ///     Performs a POST request with the body consisting of a series of key-value arguments, encoded in the
        ///     <c>application/x-www-form-urlencoded</c> format.</summary>
        /// <param name="url">
        ///     The URL of the request. If the URL does not begin with <c>http://</c> or <c>https://</c>, it is automatically
        ///     prepended with <see cref="RootUrl"/>.</param>
        /// <param name="args">
        ///     The arguments to pass with the POST request. Null values are allowed and ignored.</param>
        /// <returns>
        ///     The response received from the server.</returns>
        /// <remarks>
        ///     Choose this format for requests that need to be small and do not contain any file uploads.</remarks>
        /// <seealso cref="Post(string,HArg[])"/>
        public HResponse PostUrlencoded(string url, params HArg[] args)
        {
            if (args == null)
                args = new HArg[0];
            var invalid = args.Where(a => a != null).FirstOrDefault(a => !a.ValidForUrlEncoded);
            if (invalid != null)
                throw new ArgumentException("args", "The argument with name '{0}' is not valid for URL-encoded POST requests.".Fmt(invalid.Name));
            return Post(url, args.Where(a => a != null).Select(a => a.Name.UrlEscape() + "=" + a.Value.UrlEscape()).JoinString("&").ToUtf8(),
                "application/x-www-form-urlencoded");
        }

        /// <summary>
        ///     Performs a POST request with the body consisting of a series of key-value arguments, encoded in the
        ///     <c>multipart/form-data</c> format.</summary>
        /// <param name="url">
        ///     The URL of the request. If the URL does not begin with <c>http://</c> or <c>https://</c>, it is automatically
        ///     prepended with <see cref="RootUrl"/>.</param>
        /// <param name="args">
        ///     The arguments to pass with the POST request. Null values are allowed and ignored.</param>
        /// <returns>
        ///     The response received from the server.</returns>
        /// <remarks>
        ///     Choose this format for requests that need to contain file uploads.</remarks>
        /// <seealso cref="Post(string,HArg[])"/>
        public HResponse PostFormdata(string url, params HArg[] args)
        {
            if (args == null)
                args = new HArg[0];
            var invalid = args.Where(a => a != null).FirstOrDefault(a => !a.Valid);
            if (invalid != null)
                throw new ArgumentException("args", "The argument with name '{0}' is not valid.".Fmt(invalid.Name));

            string boundary = Rnd.NextBytes(20).ToHex();

            var ms = new MemoryStream(300 + args.Where(a => a != null).Sum(a => 30 + a.Name.Length + (a.FileContent == null ? a.Value.Length : a.FileContent.Length)));
            var sw = new StreamWriter(ms);
            sw.AutoFlush = true;
            foreach (var arg in args.Where(a => a != null))
            {
                if (arg.FileContent == null)
                {
                    sw.WriteLine("--" + boundary);
                    sw.WriteLine(@"Content-Disposition: form-data; name=""" + arg.Name + @"""");
                    sw.WriteLine();
                    sw.WriteLine(arg.Value);
                }
                else
                {
                    sw.WriteLine("--" + boundary);
                    sw.WriteLine(@"Content-Disposition: form-data; name=""" + arg.Name + @"""; filename=""" + arg.FileName + @"""");
                    sw.WriteLine(@"Content-Type: " + arg.FileContentType);
                    sw.WriteLine();
                    ms.Write(arg.FileContent);
                    sw.WriteLine();
                }
            }
            sw.WriteLine("--" + boundary + "--");

            return Post(url, ms.ToArray(), "multipart/form-data; boundary=" + boundary);
        }

        private HttpWebRequest makeRequest(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(RootUrl == null || url.ToLower().StartsWith("http://") || url.ToLower().StartsWith("https://") ? url : (RootUrl + url));
            request.ServicePoint.Expect100Continue = false;
            request.CookieContainer = Cookies;
            // See Remarks for ReqHeaders
            request.Accept = ReqHeaders[HttpRequestHeader.Accept];
            if (ReqHeaders[HttpRequestHeader.Host] != null)
                request.Host = ReqHeaders[HttpRequestHeader.Host];
            request.Referer = ReqHeaders[HttpRequestHeader.Referer];
            request.TransferEncoding = ReqHeaders[HttpRequestHeader.TransferEncoding];
            request.UserAgent = ReqHeaders[HttpRequestHeader.UserAgent];
            foreach (string header in ReqHeaders.Keys)
                if (header != "Accept" && header != "Host" && header != "Referer" && header != "Transfer-Encoding" && header != "User-Agent")
                    request.Headers[header] = ReqHeaders[header];
            // Some of these also set request headers
            request.AllowAutoRedirect = AllowAutoRedirect;
            request.AutomaticDecompression = AutomaticDecompression;
            request.KeepAlive = KeepAlive;
            request.Timeout = (int) Timeout.TotalMilliseconds;
            return request;
        }

        private HResponse performRequest(HttpWebRequest request)
        {
            Log.Debug(1, "Requested ({0}) URL \"{1}\"".Fmt(request.Method, request.RequestUri.OriginalString));
            for (int i = 0; i < request.Headers.Count; i++)
                Log.Debug(3, "  " + request.Headers.AllKeys[i] + ": " + request.Headers[i]);
            if (request.CookieContainer != null && request.CookieContainer.Count > 0)
                Log.Debug(3, "  Cookie: " + request.CookieContainer.GetCookieHeader(request.RequestUri));

            HResponse result;
            HttpWebResponse resp;
            try
            {
                resp = (HttpWebResponse) request.GetResponse();
                if (resp == null)
                    throw new WebException("Received a null response.");
                result = new HResponse(resp);
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    throw new WebException("Caught WebException containing no response.", e);
                else
                    result = new HResponse(resp = (HttpWebResponse) e.Response);
            }
            Log.Debug(2, "Response: " + result.StatusCode + " - " + result.DataString.SubstringSafe(0, 50));
            for (int i = 0; i < resp.Headers.Count; i++)
                Log.Debug(3, "  " + resp.Headers.AllKeys[i] + ": " + resp.Headers[i]);
            return result;
        }
    }

    /// <summary>Encapsulates an argument passed into an HTTP request sent by <see cref="HClient"/>.</summary>
    public class HArg
    {
        /// <summary>The name of the argument.</summary>
        public string Name { get; private set; }
        /// <summary>
        ///     The value of the argument, or <c>null</c> if this is a file upload.</summary>
        /// <remarks>
        ///     If this is non-<c>null</c>, all of <see cref="FileName"/>, <see cref="FileContentType"/> and <see
        ///     cref="FileContent"/> must be <c>null</c>.</remarks>
        public string Value { get; private set; }

        /// <summary>
        ///     The name of the file to upload.</summary>
        /// <remarks>
        ///     If this is non-<c>null</c>, <see cref="FileContentType"/> and <see cref="FileContent"/> must be
        ///     non-<c>null</c> too, and <see cref="Value"/> must be <c>null</c>.</remarks>
        public string FileName { get; private set; }
        /// <summary>
        ///     The name of the file to upload.</summary>
        /// <remarks>
        ///     If this is non-<c>null</c>, <see cref="FileName"/> and <see cref="FileContent"/> must be non-<c>null</c> too,
        ///     and <see cref="Value"/> must be <c>null</c>.</remarks>
        public string FileContentType { get; private set; }
        /// <summary>
        ///     The name of the file to upload.</summary>
        /// <remarks>
        ///     If this is non-<c>null</c>, <see cref="FileName"/> and <see cref="FileContentType"/> must be non-<c>null</c>
        ///     too, and <see cref="Value"/> must be <c>null</c>.</remarks>
        public byte[] FileContent { get; private set; }

        /// <summary>
        ///     Constructor for a key-value argument (not a file upload).</summary>
        /// <param name="name">
        ///     Name of the argument.</param>
        /// <param name="value">
        ///     Value of the argument.</param>
        /// <seealso cref="HArg(string,JsonValue)"/>
        /// <seealso cref="HArg(string,string,string,byte[])"/>
        public HArg(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        ///     Constructor for a key-value argument (not a file upload).</summary>
        /// <param name="name">
        ///     Name of the argument.</param>
        /// <param name="value">
        ///     Value of the argument. This value is encoded as JSON syntax.</param>
        /// <seealso cref="HArg(string,string)"/>
        /// <seealso cref="HArg(string,string,string,byte[])"/>
        public HArg(string name, JsonValue value)
        {
            Name = name;
            Value = value.ToString();
        }

        /// <summary>
        ///     Constructor for a file upload.</summary>
        /// <param name="name">
        ///     Name of the argument.</param>
        /// <param name="fileName">
        ///     Name of the file to upload.</param>
        /// <param name="fileContentType">
        ///     MIME content-type of the file to upload.</param>
        /// <param name="fileContent">
        ///     Raw file content.</param>
        /// <seealso cref="HArg(string,string)"/>
        /// <seealso cref="HArg(string,JsonValue)"/>
        public HArg(string name, string fileName, string fileContentType, byte[] fileContent)
        {
            Name = name;
            FileName = fileName;
            FileContentType = fileContentType;
            FileContent = fileContent;
        }

        /// <summary>Determines whether this argument can be used in a call to <see cref="HClient.PostUrlencoded"/>.</summary>
        public bool ValidForUrlEncoded { get { return Name != null && Value != null && FileName == null && FileContentType == null && FileContent == null; } }
        /// <summary>
        ///     Determines whether this argument is valid.</summary>
        /// <remarks>
        ///     <para>
        ///         To be valid, this argument must satisfy the following conditions:</para>
        ///     <list type="bullet">
        ///         <item><description>
        ///             <see cref="Name"/> cannot be <c>null</c>;</description></item>
        ///         <item><description>
        ///             <para>
        ///                 either:</para>
        ///             <list type="bullet">
        ///                 <item><description>
        ///                     <see cref="Value"/> is non-<c>null</c>, while <see cref="FileName"/>, <see
        ///                     cref="FileContentType"/> and <see cref="FileContent"/> are <c>null</c>, or</description></item>
        ///                 <item><description>
        ///                     <see cref="Value"/> is <c>null</c>, while <see cref="FileName"/>, <see
        ///                     cref="FileContentType"/> and <see cref="FileContent"/> are non-<c>null</c>.</description></item></list></description></item></list></remarks>
        public bool Valid { get { return ValidForUrlEncoded || (Name != null && Value == null && FileName != null && FileContentType != null && FileContent != null); } }
    }

    /// <summary>
    ///     Encapsulates the response received by an HTTP server after a call to <see cref="HClient.Get"/>, <see
    ///     cref="HClient.Post(string,HArg[])"/> or related methods.</summary>
    public class HResponse
    {
        private HttpWebResponse _response;
        private byte[] _data;

        /// <summary>
        ///     Constructs a new instance of <see cref="HResponse"/> by reading all data from the response stream of the
        ///     specified <see cref="HttpWebResponse"/> object and closing it.</summary>
        /// <param name="response">
        ///     Object to copy data from.</param>
        public HResponse(HttpWebResponse response)
        {
            _response = response;
            _data = _response.GetResponseStream().ReadAllBytes();
            _response.Close();
        }

        /// <summary>The raw content of the response.</summary>
        public byte[] Data
        {
            get { return _data; }
        }

        /// <summary>The content of the response, converted to a <c>string</c> from UTF-8.</summary>
        public string DataString
        {
            get { return _data.FromUtf8(); }
        }

        /// <summary>The content of the response, parsed as JSON.</summary>
        public JsonValue DataJson
        {
            get { return JsonValue.Parse(DataString); }
        }

        /// <summary>The status code of the response.</summary>
        public HttpStatusCode StatusCode
        {
            get { return _response.StatusCode; }
        }

        /// <summary>Specifies the location this response is redirecting to or was redirected to.</summary>
        public string Location
        {
            get
            {
                return _response.Headers.AllKeys.Contains("Location")
                    ? _response.Headers["Location"]
                    : _response.ResponseUri.AbsoluteUri;
            }
        }

        /// <summary>Gets a collection containing response headers.</summary>
        public WebHeaderCollection Headers
        {
            get { return _response.Headers; }
        }

        /// <summary>
        ///     Throws an exception if the status code of the response is not the specified expected status code.</summary>
        /// <param name="status">
        ///     The status code that is expected.</param>
        /// <returns>
        ///     Itself.</returns>
        public HResponse Expect(HttpStatusCode status)
        {
            if (_response.StatusCode != status)
                throw new Exception("Expected status {0}, got {1}.".Fmt(status, _response.StatusCode));
            return this;
        }
    }
}
