using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;
using RT.Util.Xml;

namespace RT.Util
{
    /// <summary>Encapsulates an error condition that occurred during an SMTP exchange.</summary>
    public sealed class RTSmtpException : RTException
    {
        /// <summary>Contains the SMTP conversation (protocol text sent back and forth) up to the point of the error.</summary>
        public List<string> Conversation { get; private set; }
        /// <summary>Constructor.</summary>
        /// <param name="message">Error message.</param>
        /// <param name="conversation">Contains the SMTP conversation (protocol text sent back and forth) up to the point of the error.</param>
        /// <param name="inner">Inner exception.</param>
        public RTSmtpException(string message, List<string> conversation, Exception inner = null)
            : base(message, inner)
        {
            Conversation = conversation;
        }
    }

    /// <summary>Represents an SMTP encryption mode.</summary>
    public enum SmtpEncryption
    {
        /// <summary>No encryption</summary>
        None,
        /// <summary>SSL/TLS encryption</summary>
        Ssl,
        /// <summary>SSL/TLS encryption; any certificate validation errors are ignored.</summary>
        SslIgnoreCert,
    }

    /// <summary>Holds all settings required to connect to an SMTP server.</summary>
    public abstract class RTSmtpSettings : IXmlClassifyProcess
    {
        /// <summary>Server host name or IP address.</summary>
        public string Host = "smtp.example.com";
        /// <summary>Server port. Standard ports: 25 for no encryption, 465 for SSL.</summary>
        public int Port = 25;
        /// <summary>Encryption to use.</summary>
        public SmtpEncryption Encryption = SmtpEncryption.None;
        /// <summary>SMTP username for login - for "me@example.com" this is typically "me" or "me@example.com", but can be anything.</summary>
        public string Username = "example_user";
        /// <summary>Unencrypted password to be automatically encrypted by XmlClassify whenever the settings are loaded or saved.</summary>
        public string Password = "password";
        /// <summary>The encrypted password.</summary>
        public string PasswordEncrypted;
        /// <summary>The decrypted password.</summary>
        public string PasswordDecrypted { get { return Password ?? DecryptPassword(PasswordEncrypted); } }

        /// <summary>When implemented in a derived class, decrypts the specified encrypted password.</summary>
        protected abstract string DecryptPassword(string encrypted);
        /// <summary>When implemented in a derived class, encrypts the specified clear-text password.</summary>
        protected abstract string EncryptPassword(string decrypted);

        private void encryptPassword()
        {
            if (Password == null)
                return;
            PasswordEncrypted = EncryptPassword(PasswordDecrypted);
            Password = null;
        }

        void IXmlClassifyProcess.BeforeXmlClassify()
        {
            encryptPassword();
        }

        void IXmlClassifyProcess.AfterXmlDeclassify()
        {
            encryptPassword();
        }
    }

    /// <summary>Provides methods to send e-mails via an SMTP server.</summary>
    public sealed class RTSmtpClient : IDisposable
    {
        private TcpClient _tcp;
        private Stream _tcpStream;
        private SslStream _sslStream;
        private TextWriter _writer;
        private TextReader _reader;
        private List<string> _conversation;

        /// <summary>The SMTP client logs various messages to this log at various verbosity levels.</summary>
        public LoggerBase Log = new NullLogger();

        /// <summary>Creates a connection to the SMTP server and authenticates the specified user.</summary>
        /// <param name="host">SMTP host name.</param>
        /// <param name="port">SMTP host port.</param>
        /// <param name="username">SMTP username.</param>
        /// <param name="password">SMTP password.</param>
        /// <param name="encryption">Encryption mode.</param>
        /// <exception cref="RTSmtpException">SMTP protocol error, or authentication failed.</exception>
        public RTSmtpClient(string host, int port, string username, string password, SmtpEncryption encryption = SmtpEncryption.None)
        {
            Log.Debug(1, "Connecting to {0}:{1}...".Fmt(host, port));
            _tcp = new TcpClient(host, port);
            _tcpStream = _tcp.GetStream();
            if (encryption == SmtpEncryption.Ssl || encryption == SmtpEncryption.SslIgnoreCert)
            {
                if (encryption == SmtpEncryption.Ssl)
                    _sslStream = new SslStream(_tcpStream);
                else
                    _sslStream = new SslStream(_tcpStream, false, (_, __, ___, ____) => true);
                _sslStream.AuthenticateAsClient(host);
                Log.Debug(2, "SSL: authenticated as client");
            }
            _writer = new StreamWriter(_sslStream ?? _tcpStream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
            _reader = new StreamReader(_sslStream ?? _tcpStream, Encoding.UTF8);
            _conversation = new List<string>();

            sendAndExpect(null, 220);
            sendAndExpect("EHLO localhost", 250);
            var result = sendAndExpect("AUTH LOGIN", 334).Trim();
            var resultDec = Convert.FromBase64String(result).FromUtf8();
            if (resultDec != "Username:")
                throw new RTSmtpException("Expected 'Username:', got: '{0}'".Fmt(resultDec), _conversation);
            result = sendAndExpect(Convert.ToBase64String(username.ToUtf8()), 334).Trim();
            resultDec = Convert.FromBase64String(result).FromUtf8();
            if (resultDec != "Password:")
                throw new RTSmtpException("Expected 'Password:', got: '{0}'".Fmt(resultDec), _conversation);
            sendAndExpect(Convert.ToBase64String(password.ToUtf8()), 235);
            Log.Debug(2, "Connected.");
        }

        /// <summary>Creates a connection to the SMTP server and authenticates the specified user.</summary>
        /// <param name="settings">An object containing the relevant SMTP settings.</param>
        /// <exception cref="RTSmtpException">SMTP protocol error, or authentication failed.</exception>
        public RTSmtpClient(RTSmtpSettings settings)
            : this(settings.Host, settings.Port, settings.Username, settings.PasswordDecrypted, settings.Encryption)
        {
        }

        private string sendAndExpect(string toSend, int statusCode)
        {
            if (toSend != null)
            {
                _writer.Write(toSend + "\r\n");
                _conversation.Add("> " + toSend);
                Log.Debug(8, "> " + toSend);
            }
            string response = "";
            Match m;
            do
            {
                var line = _reader.ReadLine();
                _conversation.Add("< " + line);
                Log.Debug(8, "< " + line);
                m = Regex.Match(line, @"^(\d+)(-| |$)?(.*)?$");
                if (!m.Success)
                    throw new RTSmtpException("Expected status code '{0}', got unexpected line: {1}".Fmt(statusCode, line), _conversation);
                if (int.Parse(m.Groups[1].Value) != statusCode)
                    throw new RTSmtpException("Expected status code '{0}', got '{1}'.".Fmt(statusCode, m.Groups[1].Value), _conversation);
                response += m.Groups[3].Value.Trim() + Environment.NewLine;
            }
            while (m.Groups[2].Value == "-");
            return response;
        }

        /// <summary>Sends an e-mail.</summary>
        /// <param name="from">From address.</param>
        /// <param name="to">Recipient address(es).</param>
        /// <param name="subject">Subject line.</param>
        /// <param name="plainText">Plain-text version of the e-mail.</param>
        /// <param name="html">HTML version of the e-mail.</param>
        public void SendEmail(MailAddress from, IEnumerable<MailAddress> to, string subject, string plainText, string html)
        {
            if (from == null)
                throw new ArgumentNullException("from");
            if (to == null)
                throw new ArgumentNullException("to");
            if (plainText == null && html == null)
                throw new ArgumentException("You must have either a plain-text or an HTML to your e-mail (or both).", "html");
            if (subject == null)
                subject = "";
            if (plainText == null)
                plainText = "This e-mail is only available in HTML format.";

            var toHeader = to.Select(t => @"""{0}"" <{1}>".Fmt(t.DisplayName, t.Address)).JoinString(", ");
            Log.Info(1, "Sending email to " + toHeader);

            sendAndExpect(@"MAIL FROM: ""{0}"" <{1}>".Fmt(from.DisplayName, from.Address), 250);
            foreach (var toAddr in to)
                sendAndExpect(@"RCPT TO: ""{0}"" <{1}>".Fmt(toAddr.DisplayName, toAddr.Address), 250);
            sendAndExpect(@"DATA", 354);

            Action<string> sendLine = str =>
            {
                if (str.StartsWith("."))
                    str = "." + str;
                _writer.WriteLine(str);
                Log.Debug(9, ">> " + str);
            };
            Action<string> sendAsQuotedPrintable = str =>
            {
                foreach (var line in toQuotedPrintable(str).Split(new[] { "\r\n" }, StringSplitOptions.None))
                    sendLine(line);
            };

            sendLine(@"From: ""{0}"" <{1}>".Fmt(from.DisplayName, from.Address));
            sendLine(@"To: {0}".Fmt(toHeader));
            sendLine(@"Date: {0}".Fmt(DateTime.UtcNow.ToString("r"))); // "r" appends "GMT" even when the timestamp is known not to be GMT...
            sendLine(@"Subject: =?utf-8?B?{0}?=".Fmt(Convert.ToBase64String(subject.ToUtf8())));

            if (html != null)
            {
                string boundary1 = new string(Enumerable.Range(0, 64).Select(dummy => { var i = Rnd.Next(62); return (char) (i < 10 ? '0' + i : i < 36 ? 'a' + i - 10 : 'A' + i - 36); }).ToArray());
                string boundary2 = new string(Enumerable.Range(0, 64).Select(dummy => { var i = Rnd.Next(62); return (char) (i < 10 ? '0' + i : i < 36 ? 'a' + i - 10 : 'A' + i - 36); }).ToArray());
                string boundary3 = new string(Enumerable.Range(0, 64).Select(dummy => { var i = Rnd.Next(62); return (char) (i < 10 ? '0' + i : i < 36 ? 'a' + i - 10 : 'A' + i - 36); }).ToArray());
                sendLine(@"Content-Type: multipart/mixed; boundary={0}".Fmt(boundary1));
                sendLine("");
                sendLine(@"--{0}".Fmt(boundary1));
                sendLine(@"Content-Type: multipart/related; boundary=""{0}""".Fmt(boundary2));
                sendLine("");
                sendLine(@"--{0}".Fmt(boundary2));
                sendLine(@"Content-Type: multipart/alternative; boundary=""{0}""".Fmt(boundary3));
                sendLine("");
                sendLine(@"--{0}".Fmt(boundary3));
                sendLine(@"Content-Type: text/plain; charset=UTF-8");
                sendLine(@"Content-Transfer-Encoding: quoted-printable");
                sendLine("");
                sendAsQuotedPrintable(plainText);
                sendLine(@"--{0}".Fmt(boundary3));
                sendLine(@"Content-Type: text/html; charset=UTF-8");
                sendLine(@"Content-Transfer-Encoding: quoted-printable");
                sendLine("");
                sendAsQuotedPrintable(html);
                sendLine(@"--{0}--".Fmt(boundary3));
                sendLine("");
                sendLine(@"--{0}--".Fmt(boundary2));
                sendLine("");
                sendLine(@"--{0}--".Fmt(boundary1));
            }
            else
            {
                sendLine(@"Content-Type: text/plain; charset=UTF-8");
                sendLine(@"Content-Transfer-Encoding: quoted-printable");
                sendLine("");
                sendAsQuotedPrintable(plainText);
            }
            sendAndExpect(".", 250);
            Log.Debug(1, "Sent successfully.");
        }

        private static string toQuotedPrintable(string input)
        {
            // Encode the input as UTF-8 and then encode all non-printable bytes as '='+hex.
            // Then change =0D=0A back into \r\n (but leave a lone =0D or =0A encoded)
            var encoded = input.ToUtf8().Select(ch => ch < 32 || ch == '=' || ch > 126 ? '=' + ch.ToString("X2") : new string((char) ch, 1)).JoinString().Replace("=0D=0A", "\r\n");

            // Encode spaces before newlines as required by the encoding
            encoded = Regex.Replace(encoded, @"( +)\r", m => "=20".Repeat(m.Groups[1].Length) + "\r");

            // Break lines that are longer than 76 characters
            var sb = new StringBuilder();
            Match match;
            while ((match = Regex.Match(encoded, "^[^\r]{77}", RegexOptions.Multiline)).Success)
            {
                var p = match.Value.IndexOf('=', 73);
                if (p == -1 || p == 76)
                    p = 75;
                sb.Append(encoded.Substring(0, match.Index + p)).Append("=\r\n");
                encoded = encoded.Substring(match.Index + p);
            }
            sb.Append(encoded);
            return sb.ToString();
        }

        private bool _disposed;

        /// <summary>Closes the SMTP connection and frees all associated resources.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            if (_tcp != null)
                ((IDisposable) _tcp).Dispose();
            if (_tcpStream != null)
                ((IDisposable) _tcpStream).Dispose();
            if (_sslStream != null)
                ((IDisposable) _sslStream).Dispose();
            if (_writer != null)
                ((IDisposable) _writer).Dispose();
            if (_reader != null)
                ((IDisposable) _reader).Dispose();
            _disposed = true;
        }
    }
}
