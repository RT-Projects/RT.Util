using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using RT.Serialization;
using RT.Util.ExtensionMethods;

namespace RT.Util
{
    /// <summary>Encapsulates an error condition that occurred during an SMTP exchange.</summary>
    public sealed class RTSmtpException : Exception
    {
        /// <summary>Contains the SMTP conversation (protocol text sent back and forth) up to the point of the error.</summary>
        public List<string> Conversation { get; private set; }
        /// <summary>
        ///     Constructor.</summary>
        /// <param name="message">
        ///     Error message.</param>
        /// <param name="conversation">
        ///     Contains the SMTP conversation (protocol text sent back and forth) up to the point of the error.</param>
        /// <param name="inner">
        ///     Inner exception.</param>
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
    public abstract class RTSmtpSettings : IClassifyObjectProcessor
    {
        /// <summary>Server host name or IP address.</summary>
        public string Host = "smtp.example.com";
        /// <summary>Server port. Standard ports: 25 for no encryption, 465 for SSL.</summary>
        public int Port = 25;
        /// <summary>Encryption to use.</summary>
        public SmtpEncryption Encryption = SmtpEncryption.None;
        /// <summary>
        ///     SMTP username for login - for "me@example.com" this is typically "me" or "me@example.com", but can be
        ///     anything.</summary>
        public string Username = "example_user";
        /// <summary>
        ///     Unencrypted password to be automatically encrypted by Classify whenever the settings are loaded or saved.</summary>
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

        void IClassifyObjectProcessor.BeforeSerialize()
        {
            encryptPassword();
        }

        void IClassifyObjectProcessor.AfterDeserialize()
        {
            encryptPassword();
        }
    }

    /// <summary>Provides methods to send e-mails via an SMTP server.</summary>
    public sealed class RTSmtpClient : IDisposable
    {
        private readonly TcpClient _tcp;
        private readonly Stream _tcpStream;
        private readonly SslStream _sslStream;
        private readonly TextWriter _writer;
        private readonly TextReader _reader;
        private readonly List<string> _conversation;
        private readonly LoggerBase _log;

        /// <summary>
        ///     Creates a connection to the SMTP server and authenticates the specified user.</summary>
        /// <param name="host">
        ///     SMTP host name.</param>
        /// <param name="port">
        ///     SMTP host port.</param>
        /// <param name="username">
        ///     SMTP username.</param>
        /// <param name="password">
        ///     SMTP password.</param>
        /// <param name="encryption">
        ///     Encryption mode.</param>
        /// <param name="log">
        ///     The SMTP client logs various messages to this log at various verbosity levels.</param>
        /// <param name="timeout">
        ///     Network stream read/write timeout, in milliseconds.</param>
        /// <exception cref="RTSmtpException">
        ///     SMTP protocol error, or authentication failed.</exception>
        /// <exception cref="IOException">
        ///     Network error or timeout.</exception>
        public RTSmtpClient(string host, int port, string username = null, string password = null, SmtpEncryption encryption = SmtpEncryption.None, LoggerBase log = null, int timeout = 10000)
        {
            _log = log ?? new NullLogger();
            _log.Debug(2, "Connecting to {0}:{1}...".Fmt(host, port));
            _tcp = new TcpClient(host, port);
            _tcpStream = _tcp.GetStream();
            if (encryption == SmtpEncryption.Ssl || encryption == SmtpEncryption.SslIgnoreCert)
            {
                if (encryption == SmtpEncryption.Ssl)
                    _sslStream = new SslStream(_tcpStream);
                else
                    _sslStream = new SslStream(_tcpStream, false, (_, __, ___, ____) => true);
                _sslStream.AuthenticateAsClient(host);
                _log.Debug(3, "SSL: authenticated as client");
            }
            (_sslStream ?? _tcpStream).ReadTimeout = timeout;
            (_sslStream ?? _tcpStream).WriteTimeout = timeout;
            _writer = new StreamWriter(_sslStream ?? _tcpStream, new UTF8Encoding(false)) { NewLine = "\r\n", AutoFlush = true };
            _reader = new StreamReader(_sslStream ?? _tcpStream, Encoding.UTF8);
            _conversation = new List<string>();

            sendAndExpect(null, 220);
            sendAndExpect("EHLO localhost", 250);
            if (username == null || password == null)
            {
                _log.Debug(3, "Connected without authentication.");
                return;
            }

            var result = sendAndExpect("AUTH LOGIN", 334).Trim();
            var resultDec = Convert.FromBase64String(result).FromUtf8();
            if (resultDec != "Username:")
                throw new RTSmtpException("Expected 'Username:', got: '{0}'".Fmt(resultDec), _conversation);
            result = sendAndExpect(Convert.ToBase64String(username.ToUtf8()), 334).Trim();
            resultDec = Convert.FromBase64String(result).FromUtf8();
            if (resultDec != "Password:")
                throw new RTSmtpException("Expected 'Password:', got: '{0}'".Fmt(resultDec), _conversation);
            sendAndExpect(Convert.ToBase64String(password.ToUtf8()), 235);
            _log.Debug(3, "Connected.");
        }

        /// <summary>
        ///     Creates a connection to the SMTP server and authenticates the specified user.</summary>
        /// <param name="settings">
        ///     An object containing the relevant SMTP settings.</param>
        /// <param name="log">
        ///     The SMTP client logs various messages to this log at various verbosity levels.</param>
        /// <exception cref="RTSmtpException">
        ///     SMTP protocol error, or authentication failed.</exception>
        public RTSmtpClient(RTSmtpSettings settings, LoggerBase log = null)
            : this(settings.Host, settings.Port, settings.Username, settings.PasswordDecrypted, settings.Encryption, log)
        {
        }

        private string sendAndExpect(string toSend, int statusCode)
        {
            if (toSend != null)
            {
                _writer.Write(toSend + "\r\n");
                _conversation.Add("> " + toSend);
                _log.Debug(8, "> " + toSend);
            }
            string response = "";
            Match m;
            do
            {
                var line = _reader.ReadLine();
                _conversation.Add("< " + line);
                _log.Debug(8, "< " + line);
                m = Regex.Match(line, @"^(\d+)(-| |$)?(.*)?$");
                if (!m.Success)
                    throw new RTSmtpException("Expected status code '{0}', got unexpected line: {1}".Fmt(statusCode, line), _conversation);
                if (int.Parse(m.Groups[1].Value) != statusCode)
                    throw new RTSmtpException("Expected status code '{0}', got: {1}.".Fmt(statusCode, line), _conversation);
                response += m.Groups[3].Value.Trim() + Environment.NewLine;
            }
            while (m.Groups[2].Value == "-");
            return response;
        }

        /// <summary>
        ///     Sends an e-mail.</summary>
        /// <param name="from">
        ///     From address.</param>
        /// <param name="to">
        ///     Recipient address(es).</param>
        /// <param name="subject">
        ///     Subject line.</param>
        /// <param name="bodyPlain">
        ///     Plain-text version of the e-mail.</param>
        /// <param name="bodyHtml">
        ///     HTML version of the e-mail.</param>
        public void SendEmail(MailAddress from, IEnumerable<MailAddress> to, string subject, string bodyPlain, string bodyHtml)
        {
            var headers = new List<string>();
            headers.Add(EncodeHeader("From", from));
            headers.Add(EncodeHeader("To", to.ToArray()));
            headers.Add(EncodeHeader("Date", DateTime.UtcNow.ToString("r"))); // "r" appends "GMT" even when the timestamp is known not to be GMT...
            headers.Add(EncodeHeader("Subject", subject));
            SendEmail(from, to, headers, bodyPlain, bodyHtml);
        }

        /// <summary>
        ///     Sends an email with fully custom headers.</summary>
        /// <param name="from">
        ///     The "envelope From" address presented to the MTA.</param>
        /// <param name="to">
        ///     The "envelope To" address presented to the MTA.</param>
        /// <param name="headers">
        ///     Zero or more headers. Headers must be in the correct format, including escaping, and excluding the linebreak
        ///     that separates headers (but including any line breaks required within the header). This method does not
        ///     validate the headers in any way, and does not include From, To, Date or any other headers unless explicitly
        ///     passed in through this argument.</param>
        /// <param name="bodyPlain">
        ///     Plain-text version of the e-mail.</param>
        /// <param name="bodyHtml">
        ///     HTML version of the e-mail.</param>
        public void SendEmail(MailAddress from, IEnumerable<MailAddress> to, IEnumerable<string> headers, string bodyPlain, string bodyHtml)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));
            if (to == null)
                throw new ArgumentNullException(nameof(to));
            if (to.Count() == 0)
                return;
            if (bodyPlain == null && bodyHtml == null)
                throw new ArgumentException("You must have either a plain-text or an HTML to your e-mail (or both).", nameof(bodyHtml));
            if (bodyPlain == null)
                bodyPlain = "This e-mail is only available in HTML format.";
            if (headers == null)
                throw new ArgumentNullException(nameof(headers));

            var toHeader = to.Select(t => @"""{0}"" <{1}>".Fmt(t.DisplayName, t.Address)).JoinString(", ");
            _log.Debug(2, "Sending email to " + toHeader);

            sendAndExpect(@"MAIL FROM: <{0}>".Fmt(from.Address), 250);
            foreach (var toAddr in to)
                sendAndExpect(@"RCPT TO: <{0}>".Fmt(toAddr.Address), 250);
            sendAndExpect(@"DATA", 354);

            // sends the input to the SMTP server, followed by a newline, while correctly escaping all lines starting with a "."
            Action<string> sendLines = str =>
            {
                if (str.StartsWith("."))
                    _writer.Write('.');
                _writer.Write(str.Replace("\r\n.", "\r\n.."));
                _writer.Write("\r\n");
                _log.Debug(9, ">> " + str);
            };

            foreach (var header in headers)
                sendLines(header);

            if (bodyHtml != null)
            {
                string boundary1 = new string(Enumerable.Range(0, 64).Select(dummy => { var i = Rnd.Next(62); return (char) (i < 10 ? '0' + i : i < 36 ? 'a' + i - 10 : 'A' + i - 36); }).ToArray());
                string boundary2 = new string(Enumerable.Range(0, 64).Select(dummy => { var i = Rnd.Next(62); return (char) (i < 10 ? '0' + i : i < 36 ? 'a' + i - 10 : 'A' + i - 36); }).ToArray());
                string boundary3 = new string(Enumerable.Range(0, 64).Select(dummy => { var i = Rnd.Next(62); return (char) (i < 10 ? '0' + i : i < 36 ? 'a' + i - 10 : 'A' + i - 36); }).ToArray());
                sendLines(@"Content-Type: multipart/mixed; boundary={0}".Fmt(boundary1));
                sendLines("");
                sendLines(@"--{0}".Fmt(boundary1));
                sendLines(@"Content-Type: multipart/related; boundary=""{0}""".Fmt(boundary2));
                sendLines("");
                sendLines(@"--{0}".Fmt(boundary2));
                sendLines(@"Content-Type: multipart/alternative; boundary=""{0}""".Fmt(boundary3));
                sendLines("");
                sendLines(@"--{0}".Fmt(boundary3));
                sendLines(@"Content-Type: text/plain; charset=UTF-8");
                sendLines(@"Content-Transfer-Encoding: quoted-printable");
                sendLines("");
                sendLines(toQuotedPrintable(bodyPlain));
                sendLines(@"--{0}".Fmt(boundary3));
                sendLines(@"Content-Type: text/html; charset=UTF-8");
                sendLines(@"Content-Transfer-Encoding: quoted-printable");
                sendLines("");
                sendLines(toQuotedPrintable(bodyHtml));
                sendLines(@"--{0}--".Fmt(boundary3));
                sendLines("");
                sendLines(@"--{0}--".Fmt(boundary2));
                sendLines("");
                sendLines(@"--{0}--".Fmt(boundary1));
            }
            else
            {
                sendLines(@"Content-Type: text/plain; charset=UTF-8");
                sendLines(@"Content-Transfer-Encoding: quoted-printable");
                sendLines("");
                sendLines(toQuotedPrintable(bodyPlain));
            }
            sendAndExpect(".", 250);
            _log.Debug(1, "Sent successfully.");
        }

        /// <summary>
        ///     Encodes an email header for use with <see cref="SendEmail(MailAddress, IEnumerable&lt;MailAddress&gt;,
        ///     IEnumerable&lt;string&gt;, string, string)"/>, escaping, quoting and line-wrapping as required by the relevant
        ///     RFCs.</summary>
        public static string EncodeHeader(string name, string value)
        {
            // RFC 5322 defines how the headers should be encoded. There are two line length limits, both of which are ignored by this
            // implementation. The 78 character limit is a "should" limit that appears to be widely broken already without ill effects. The
            // 998 limit is worked around by forbidding header values this long and saving the trouble of correctly breaking the lines.
            if (!validHeaderName(name))
                throw new ArgumentException("This email header name is not valid as per RFC 5322", nameof(name));

            var chunks = new List<chunk>();
            addChunk(chunks, name + ": ");
            addChunk(chunks, value);
            return chunksToString(chunks);
        }

        /// <summary>
        ///     Encodes an email header for use with <see cref="SendEmail(MailAddress, IEnumerable&lt;MailAddress&gt;,
        ///     IEnumerable&lt;string&gt;, string, string)"/>, escaping, quoting and line-wrapping as required by the relevant
        ///     RFCs.</summary>
        public static string EncodeHeader(string name, params MailAddress[] addresses)
        {
            // In addition to the comment in the EncodeHeader(string, string) method, this method employs a simple strategy regarding the
            // 998 character line length limit: every email address starts on a new line. This is simple to implement, always valid, and the only
            // thing this renders impossible is /very/ long address displaynames.

            if (!validHeaderName(name))
                throw new ArgumentException("This email header name is not valid as per RFC 5322", nameof(name));
            if (addresses.Length == 0)
                throw new ArgumentException("At least one address is required", nameof(addresses));

            var chunks = new List<chunk>();
            addChunk(chunks, name + ": ");
            foreach (var address in addresses)
            {
                if (address.DisplayName != "")
                {
                    addChunk(chunks, "\"");
                    addChunk(chunks, address.DisplayName);
                    addChunk(chunks, "\" <");
                }
                addChunk(chunks, address.Address);
                if (address.DisplayName != "")
                    addChunk(chunks, ">");
                addChunk(chunks, ", ");
            }
            chunks.RemoveAt(chunks.Count - 1); // remove the last ", " chunk
            return chunksToString(chunks);
        }

        private sealed class chunk
        {
            public string Text;
            public byte[] Base64;
        }

        private static void addChunk(List<chunk> chunks, string value)
        {
            if (requiresBase64(value))
                chunks.Add(new chunk { Base64 = value.ToUtf8() });
            else
                chunks.Add(new chunk { Text = value });
        }

        private static string chunksToString(List<chunk> chunks)
        {
            var result = new StringBuilder();
            int lineLength = 0;

            foreach (var chunk in chunks)
            {
                // RFC 2047: An 'encoded-word' may not be more than 75 characters long. Each line of a header field that contains one or more 'encoded-word's is limited to 76 characters.
                if (chunk.Text != null)
                {
                    result.Append(chunk.Text);
                    lineLength += chunk.Text.Length;
                    while (lineLength > 76)
                        wrap(result, ref lineLength, 76);
                }
                else // chunk.Base64 != null
                {
                    // base-64 chunks must be surrounded by "=?utf-8?B?" and followed by "?=". It can be split anywhere except across a UTF-8 sequence.
                    // The minimum space occupied by an encoded-word is 16 characters (because the base64 has to be padded, so min. 4 chars)
                    // This min. length (16 chars) is also guaranteed to be achievable because even the longest unicode sequences can be encoded in four base64 characters.
                    int pos = 0;
                    while (pos < chunk.Base64.Length)
                    {
                        // The first time round is special, because it's the only time when we might wrap _before_ adding a chunk
                        if (pos == 0 && lineLength > 76 - 16)
                            wrap(result, ref lineLength, 76);
                        // Figure out how many bytes we can add. Every 3 bytes take 4 characters.
                        int bytes = Math.Min(chunk.Base64.Length - pos, (76 - lineLength - 12) / 4 * 3);
                        Ut.Assert(bytes > 0);
                        // Now move backwards until we hit a utf8 boundary
                        while (pos + bytes < chunk.Base64.Length && chunk.Base64[pos + bytes] >= 0x80 && chunk.Base64[pos + bytes] < 0xC0)
                            bytes--;
                        Ut.Assert(bytes > 0);
                        // Add them!
                        lineLength -= result.Length; // coupled with the next change, this increments lineLength by how many chars we're about to append.
                        result.Append("=?UTF-8?B?");
                        result.Append(Convert.ToBase64String(chunk.Base64, pos, bytes));
                        result.Append("?=");
                        lineLength += result.Length;
                        pos += bytes;
                        // If we have any bytes left, we must wrap, because that's the only reason we would ever not append everything
                        if (pos < chunk.Base64.Length)
                        {
                            result.Append("\r\n ");
                            lineLength = 1;
                        }
                    }
                }
            }

            return result.ToString();
        }

        private static void wrap(StringBuilder text, ref int lineLength, int maxLineLength)
        {
            Ut.Assert(text.Length > 0);
            // This method MAY be called with a lineLength less than the max line length.
            // It MUST add a line wrap, ideally before the max line length, but if not possible then after the max length, possibly even at the very end of the text.

            // If the line already fits within max line length, just insert a newline at the very end and be done
            if (lineLength <= maxLineLength)
            {
                lineLength = 1;
                text.Append("\r\n ");
                return;
            }
            // First search back from the max line length position for a wrappable location
            int pos = text.Length - lineLength + maxLineLength; // "pos < text.Length" is guaranteed by the test above
            while (true)
            {
                if (pos < (text.Length - lineLength) || text[pos] == '\n') // exit if we've reached the start of the current line
                {
                    pos = -1;
                    break;
                }
                if (text[pos] == ' ')
                    break;
                pos--;
            }
            // If that worked, make sure it isn't all spaces until the start of the line
            if (pos >= 0)
            {
                int pos2 = pos;
                while (true)
                {
                    pos2--;
                    if (pos2 < (text.Length - lineLength) || text[pos2] == '\n')
                    {
                        pos = -1; // found nothing but spaces until the start of the line
                        break;
                    }
                    if (text[pos2] != ' ')
                        break; // found a non-space, so OK to wrap at "pos"
                }
            }
            // If that failed, seek forward
            if (pos < 0)
            {
                pos = text.Length - lineLength + maxLineLength + 1;
                while (true)
                {
                    if (pos >= text.Length)
                        break;
                    if (text[pos] == ' ')
                        break;
                    pos++;
                }
            }
            // Insert \r\n at "pos", which either points at the space that becomes the (mandatory) indent for the next line, or is at the very end of the line
            if (pos >= text.Length)
            {
                lineLength = 1;
                text.Append("\r\n ");
            }
            else
            {
                lineLength = text.Length - pos;
                text.Insert(pos, "\r\n");
            }
        }

        private static bool validHeaderName(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c <= 32 || c >= 127 || c == ':')
                    return false;
            }
            return true;
        }

        private static bool requiresBase64(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c <= 31 || c >= 127)
                    return true;
            }
            return false;
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

            var lastBreakIndex = 0;
            for (int i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '\r')
                {
                    sb.Append(encoded.Substring(lastBreakIndex, i - lastBreakIndex));
                    lastBreakIndex = i;
                }
                else
                {
                    if (i - lastBreakIndex >= 77)
                    {
                        var substr = encoded.Substring(lastBreakIndex, 77);
                        var p = substr.IndexOf('=', 73);
                        if (p == -1 || p == 76)
                            p = 75;
                        sb.Append(encoded.Substring(lastBreakIndex, p)).Append("=\r\n");
                        lastBreakIndex += p;
                    }
                }
            }
            sb.Append(encoded.Substring(lastBreakIndex));
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

    /// <summary>
    ///     Implements a simple interface for sending an email that shares a global repository of SMTP settings. This
    ///     repository is stored using <see cref="SettingsUtil"/> using the name "RT.Emailer". The repository contains a list
    ///     of SMTP accounts with a unique name. One of the accounts may be designated as the default one if the application
    ///     doesn't specify any. The "From" address is fixed per account, but the name may be overridden by the application.</summary>
    public static class Emailer
    {
#pragma warning disable 649 // field never assigned to
        [Settings("RT.Emailer", SettingsKind.Global)]
        private class settings : SettingsBase
        {
            public string DefaultAccount;
            public Dictionary<string, account> Accounts = new Dictionary<string, account>();
        }
#pragma warning restore 649

        private class account : RTSmtpSettings
        {
            public string FromAddress = "me@example.com";
            public string FromName = "Me Smith (can be null to use process name)";

            private static byte[] _key = "303d1c9608d4323edccaa207ceb15bfe66603b5e361e0450fd5e38a0fc629c7a".FromHex(); // exactly 32 bytes

            protected override string DecryptPassword(string encrypted)
            {
                return SettingsUtil.DecryptPassword(encrypted, _key);
            }

            protected override string EncryptPassword(string decrypted)
            {
                return SettingsUtil.EncryptPassword(decrypted, _key);
            }
        }

        /// <summary>If set, the SMTP client will log various messages to this log at various verbosity levels.</summary>
        public static LoggerBase Log = null;

        /// <summary>
        ///     Sends an email using one of the pre-configured RT.Emailer SMTP accounts. If none are configured on this
        ///     computer, an exception will be thrown, describing what the user needs to do - though this requires a pretty
        ///     technical user.</summary>
        /// <param name="to">
        ///     The recipients of the email.</param>
        /// <param name="subject">
        ///     Subject line.</param>
        /// <param name="bodyPlain">
        ///     Body of the message in plaintext format, or null to omit this MIME type.</param>
        /// <param name="bodyHtml">
        ///     Body of the message in HTML format, or null to omit this MIME type.</param>
        /// <param name="account">
        ///     The name of one of the RT.Emailer accounts to use (case-sensitive). If null or not defined, will fall back to
        ///     exe name, then the Default Account setting, and then any defined account, in this order.</param>
        /// <param name="fromName">
        ///     The text to use as the "from" name. If null, will use the executable name. This setting has no effect if the
        ///     specified RT.Emailer account specifies a FromName of its own.</param>
        public static void SendEmail(IEnumerable<MailAddress> to, string subject, string bodyPlain = null, string bodyHtml = null, string account = null, string fromName = null)
        {
            // Load the settings file
            var settingsFile = SettingsUtil.GetAttribute<settings>().GetFileName();
            settings settings;
            SettingsUtil.LoadSettings(out settings);

            // Add an empty exe name account
            string exeName = Process.GetCurrentProcess().ProcessName + ".exe";
            if (exeName != null && !settings.Accounts.ContainsKey(exeName))
                settings.Accounts.Add(exeName, null);

            // Save any changes we've made (to a separate file!) and report error if we have no accounts at all
            if (!settings.Accounts.Any(a => a.Value != null))
            {
                settings.Accounts.Add("example", new account());
                settings.SaveQuiet(PathUtil.AppendBeforeExtension(settingsFile, ".example"));
                throw new InvalidOperationException("There are no RT.Emailer accounts defined on this computer. Please configure them in the file \"{0}\".".Fmt(settingsFile));
            }
            else
                settings.SaveQuiet(PathUtil.AppendBeforeExtension(settingsFile, ".rewrite"));

            // Pick the actual account we'll use
            if (account == null || !settings.Accounts.ContainsKey(account) || settings.Accounts[account] == null)
                account = exeName;
            if (account == null || !settings.Accounts.ContainsKey(account) || settings.Accounts[account] == null)
                account = settings.DefaultAccount;
            if (account == null || !settings.Accounts.ContainsKey(account) || settings.Accounts[account] == null)
                account = settings.Accounts.First(a => a.Value != null).Key;
            var acc = settings.Accounts[account];

            // Send the email
            using (var smtp = new RTSmtpClient(acc, Log))
            {
                var from = new MailAddress(acc.FromAddress, acc.FromName ?? fromName ?? (exeName == null ? null : Path.GetFileNameWithoutExtension(exeName)));
                smtp.SendEmail(from, to, subject, bodyPlain, bodyHtml);
            }
        }
    }
}
