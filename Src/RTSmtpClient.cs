using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using RT.Util.ExtensionMethods;

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

    /// <summary>Provides methods to send e-mails via an SMTP server.</summary>
    public sealed class RTSmtpClient : IDisposable
    {
        private TcpClient _tcp;
        private Stream _tcpStream;
        private SslStream _sslStream;
        private TextWriter _writer;
        private TextReader _reader;
        private List<string> _conversation;

        /// <summary>Creates a connection to the SMTP server and authenticates the specified user.</summary>
        /// <param name="host">SMTP host name.</param>
        /// <param name="port">SMTP host port.</param>
        /// <param name="username">SMTP username.</param>
        /// <param name="password">SMTP password.</param>
        /// <param name="encryption">Encryption mode.</param>
        /// <exception cref="RTSmtpException">SMTP protocol error, or authentication failed.</exception>
        public RTSmtpClient(string host, int port, string username, string password, SmtpEncryption encryption = SmtpEncryption.None)
        {
            _tcp = new TcpClient(host, port);
            _tcpStream = _tcp.GetStream();
            if (encryption == SmtpEncryption.Ssl || encryption == SmtpEncryption.SslIgnoreCert)
            {
                if (encryption == SmtpEncryption.Ssl)
                    _sslStream = new SslStream(_tcpStream);
                else
                    _sslStream = new SslStream(_tcpStream, false, (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true);
                _sslStream.AuthenticateAsClient(host);
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
        }

        private string sendAndExpect(string toSend, int statusCode)
        {
            if (toSend != null)
            {
                _writer.Write(toSend + "\r\n");
                _conversation.Add("> " + toSend);
            }
            string response = "";
            Match m;
            do
            {
                var line = _reader.ReadLine();
                _conversation.Add("< " + line);
                m = Regex.Match(line, @"^(\d+)(-| |$)?(.*)?$".Fmt(statusCode));
                if (!m.Success || int.Parse(m.Groups[1].Value) != statusCode)
                    throw new RTSmtpException("Expected status code '{0}', got '{1}'.".Fmt(statusCode, m.Groups[1].Value), _conversation);
                response += m.Groups[3].Value.Trim() + Environment.NewLine;
            }
            while (m.Groups[2].Value == "-");
            return response;
        }

        /// <summary>Sends an e-mail.</summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="plainText"></param>
        /// <param name="html"></param>
        public void SendEmail(MailAddress from, IEnumerable<MailAddress> to, string subject, string plainText, string html)
        {
            if (plainText == null && html == null)
                throw new InvalidOperationException("You must have either a plain-text or an HTML to your e-mail (or both).");
            if (plainText == null)
                plainText = "This e-mail is only available in HTML format.";

            sendAndExpect(@"MAIL FROM: ""{0}"" <{1}>".Fmt(from.DisplayName, from.Address), 250);
            foreach (var toAddr in to)
                sendAndExpect(@"RCPT TO: ""{0}"" <{1}>".Fmt(toAddr.DisplayName, toAddr.Address), 250);
            sendAndExpect(@"DATA", 354);

            Action<string> sendLine = str =>
            {
                if (str.StartsWith("."))
                    str = "." + str;
                _writer.WriteLine(str);
            };
            Action<string> sendAsQuotedPrintable = str =>
            {
                foreach (var line in toQuotedPrintable(str).Split(new[] { "\r\n" }, StringSplitOptions.None))
                    sendLine(line);
            };

            sendLine(@"From: ""{0}"" <{1}>".Fmt(from.DisplayName, from.Address));
            sendLine(@"To: {0}".Fmt(to.Select(t => @"""{0}"" <{1}>".Fmt(t.DisplayName, t.Address)).JoinString(", ")));
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
        }

        private static string toQuotedPrintable(string input)
        {
            var lines = input.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var lineLength = 0;
                var numSpaces = 0;
                var lineUtf8 = line.ToUtf8();
                for (int i = 0; i < lineUtf8.Length; i++)
                {
                    var byt = lineUtf8[i];
                    if (lineLength > 72)
                    {
                        sb.Append("=\r\n");
                        lineLength = 0;
                    }
                    if (byt == 32)
                        numSpaces++;
                    else
                    {
                        if (numSpaces > 0)
                        {
                            if (lineLength + numSpaces > 72)
                            {
                                do
                                {
                                    if (lineLength > 72)
                                    {
                                        sb.Append("=\r\n");
                                        lineLength = 0;
                                    }
                                    sb.Append("=20");
                                    lineLength += 3;
                                    numSpaces--;
                                }
                                while (numSpaces > 0);
                            }
                            else
                            {
                                sb.Append(new string(' ', numSpaces));
                                lineLength += numSpaces;
                                numSpaces = 0;
                            }
                        }

                        if (byt <= 32 || byt > 126 || byt == '=')
                        {
                            sb.Append('=');
                            var n = byt >> 4;
                            sb.Append((char) (n < 10 ? '0' + n : 'A' + n - 10));
                            n = byt & 0xf;
                            sb.Append((char) (n < 10 ? '0' + n : 'A' + n - 10));
                            lineLength += 3;
                        }
                        else
                        {
                            sb.Append((char) byt);
                            lineLength++;
                        }
                    }
                }

                if (numSpaces > 0)
                {
                    do
                    {
                        if (lineLength > 72)
                        {
                            sb.Append("=\r\n");
                            lineLength = 0;
                        }
                        sb.Append("=20");
                        lineLength += 3;
                        numSpaces--;
                    }
                    while (numSpaces > 0);
                }

                sb.Append("\r\n");
            }
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
