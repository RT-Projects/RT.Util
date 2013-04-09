using System;
using System.IO;
using System.Linq;
using System.Net;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace RT.Util
{
    public class HClient
    {
        public static string DefaultRootUrl = null;
        public static LoggerBase DefaultLog = new NullLogger();

        public CookieContainer Cookies = new CookieContainer();
        public LoggerBase Log = DefaultLog;

        public bool AllowAutoRedirect { get; set; }
        public string Accept { get; set; }
        public string AcceptLanguage { get; set; }
        public string AcceptCharset { get; set; }
        public string Referer { get; set; }
        public TimeSpan Timeout { get; set; }
        public string UserAgent { get; set; }

        public string RootUrl = DefaultRootUrl;

        public HClient()
        {
            AllowAutoRedirect = false;
            Referer = null;
            Timeout = TimeSpan.FromSeconds(10);
            UserAgent = "Mozilla/5.0 (Windows NT 6.1; rv:6.0.2) Gecko/20100101 Firefox/6.0.2";
            Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            AcceptLanguage = "en-gb,en;q=0.5";
            AcceptCharset = "ISO-8859-1,utf-8;q=0.7,*;q=0.7";
        }

        public HResponse Get(string url, params HArg[] args)
        {
            if (!args.All(a => a.ValidForUrlEncoded))
                throw new ArgumentException();
            var request = makeRequest(url + (args.Any() ? (url.Contains('?') ? "&" : "?") : "") + args.Select(a => a.Name.UrlEscape() + "=" + a.Value.UrlEscape()).JoinString("&"));
            request.Method = "GET";
            return performRequest(request);
        }

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

        public HResponse Post(string url, params HArg[] args)
        {
            return args.Any(a => a.ValidForFormData) ? PostFormdata(url, args) : PostUrlencoded(url, args);
        }

        public HResponse PostUrlencoded(string url, params HArg[] args)
        {
            if (!args.All(a => a.ValidForUrlEncoded))
                throw new ArgumentException();
            return Post(url, args.Select(a => a.Name.UrlEscape() + "=" + a.Value.UrlEscape()).JoinString("&").ToUtf8(),
                "application/x-www-form-urlencoded");
        }

        public HResponse PostFormdata(string url, params HArg[] args)
        {
            if (!args.All(a => a.Valid))
                throw new ArgumentException();

            string boundary = Rnd.NextBytes(20).ToHex();

            var ms = new MemoryStream(300 + args.Sum(a => 30 + a.Name.Length + (a.FileContent == null ? a.Value.Length : a.FileContent.Length)));
            var sw = new StreamWriter(ms);
            sw.AutoFlush = true;
            foreach (var arg in args)
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
                sw.WriteLine("--" + boundary + "--");
            }

            return Post(url, ms.ToArray(), "multipart/form-data; boundary=" + boundary);
        }

        private HttpWebRequest makeRequest(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(RootUrl == null || url.ToLower().StartsWith("http://") || url.ToLower().StartsWith("https://") ? url : (RootUrl + url));
            request.CookieContainer = Cookies;
            request.AllowAutoRedirect = AllowAutoRedirect;
            request.Accept = Accept;
            request.Headers[HttpRequestHeader.AcceptLanguage] = AcceptLanguage;
            request.Headers[HttpRequestHeader.AcceptCharset] = AcceptCharset;
            request.Referer = Referer;
            request.Timeout = (int) Timeout.TotalMilliseconds;
            request.UserAgent = UserAgent;
            return request;
        }

        private HResponse performRequest(HttpWebRequest request)
        {
            Log.Debug(1, "Requested ({0}) URL \"{1}\"".Fmt(request.Method, request.RequestUri.OriginalString));
            HResponse result;
            try
            {
                var resp = (HttpWebResponse) request.GetResponse();
                if (resp == null)
                    throw new WebException("Received a null response.");
                result = new HResponse(resp);
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    throw new WebException("Caught WebException containing no response.", e);
                else
                    result = new HResponse((HttpWebResponse) e.Response);
            }
            Log.Debug(2, "Response: " + result.StatusCode + " - " + result.DataString.SubstringSafe(0, 50));
            return result;
        }
    }

    public class HArg
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

        public string FileName { get; private set; }
        public string FileContentType { get; private set; }
        public byte[] FileContent { get; private set; }

        public HArg(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public HArg(string name, JsonValue value)
        {
            Name = name;
            Value = value.ToString();
        }

        public HArg(string name, string fileName, string fileContentType, byte[] fileContent)
        {
            Name = name;
            FileName = fileName;
            FileContentType = fileContentType;
            FileContent = fileContent;
        }

        public bool ValidForUrlEncoded { get { return Name != null && Value != null && FileName == null && FileContentType == null && FileContent == null; } }
        public bool ValidForFormData { get { return Name != null && Value == null && FileName != null && FileContentType != null && FileContent != null; } }
        public bool Valid { get { return ValidForUrlEncoded || ValidForFormData; } }
    }

    public class HResponse
    {
        private HttpWebResponse _response;
        private byte[] _data;

        public HResponse(HttpWebResponse response)
        {
            _response = response;
            _data = _response.GetResponseStream().ReadAllBytes();
            _response.Close();
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public string DataString
        {
            get { return _data.FromUtf8(); }
        }

        public JsonValue DataJson
        {
            get { return JsonValue.Parse(DataString); }
        }

        public HttpStatusCode StatusCode
        {
            get { return _response.StatusCode; }
        }

        public string Location
        {
            get
            {
                return _response.Headers.Keys.Cast<string>().Contains("Location")
                    ? _response.Headers["Location"]
                    : _response.ResponseUri.AbsoluteUri;
            }
        }

        public HResponse Expect(HttpStatusCode status)
        {
            if (_response.StatusCode != status)
                throw new Exception("Expected status {0}, got {1}.".Fmt(status, _response.StatusCode));
            return this;
        }
    }
}
