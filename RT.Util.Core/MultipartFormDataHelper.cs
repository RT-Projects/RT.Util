using System.Net;
using RT.Util.ExtensionMethods;

namespace RT.Util;

/// <summary>
///     Simplifies submitting POST requests with data encoded in the multipart/form-data format. See Remarks.</summary>
/// <remarks>
///     The simplest use example is to create a web request using <c>(HttpWebRequest) WebRequest.Create(...)</c>, add the data
///     using <see cref="AddField"/> and <see cref="AddFile(string, string, string)"/> methods, and complete the request using
///     <see cref="GetResponse"/>.</remarks>
public sealed class MultipartFormDataHelper
{
    private HttpWebRequest _request;
    private Stream _stream;
    private static readonly byte[] _bytesNewline = new byte[] { 13, 10 };
    private byte[] _bytesBoundary, _bytesBoundaryLast;
    private MultipartFileStream _currentFileStream;

    /// <summary>
    ///     Constructor.</summary>
    /// <param name="request">
    ///     The request to be used for the form data submission. This class automatically sets several fields which should not
    ///     be modified by the caller afterwards. These are: Method, ContentType. You MUST NOT call <see
    ///     cref="HttpWebRequest.GetRequestStream()"/> or <see cref="HttpWebRequest.GetResponse()"/> on this request. You may
    ///     modify request headers until the first call to an Add* method.</param>
    public MultipartFormDataHelper(HttpWebRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        _request = request;
        var boundary = Rnd.NextBytes(15).ToHex();
        _bytesBoundary = ("--" + boundary + "\r\n").ToUtf8();
        _bytesBoundaryLast = ("--" + boundary + "--\r\n").ToUtf8();
        _request.ContentType = "multipart/form-data; boundary=" + boundary;
        _request.Method = "POST";
    }

    /// <summary>
    ///     Finalizes the request and sends it to the remote host (or, if the request is not buffered, ensures the entire
    ///     request has been sent). Waits for response and returns the response object.</summary>
    public HttpWebResponse GetResponse()
    {
        if (_request == null)
            throw new InvalidOperationException("The request has already been sent in full. This operation is no longer legal.");
        if (_currentFileStream != null)
        {
            _currentFileStream.Close();
            _currentFileStream = null;
        }
        stream.Write(_bytesBoundaryLast);
        var result = (HttpWebResponse) _request.GetResponse();
        _request = null;
        return result;
    }

    private Stream stream
    {
        get
        {
            if (_stream == null)
                _stream = _request.GetRequestStream();
            return _stream;
        }
    }

    /// <summary>
    ///     Adds a named text value to the POST request.</summary>
    /// <param name="name">
    ///     The name of the value to add. For maximum compatibility with servers, use only printable ASCII characters in this
    ///     name. This field is encoded using UTF-8, which is supported by some modern servers, but not by others.</param>
    /// <param name="value">
    ///     The content to add as the value. Note that this is interpreted as Unicode text, and is a poor choice for binary
    ///     data. For binary data, see <see cref="AddFile(string,string,string)"/>.</param>
    public void AddField(string name, string value)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (_request == null)
            throw new InvalidOperationException("The request has already been sent in full. This operation is no longer legal.");
        if (_currentFileStream != null)
        {
            _currentFileStream.Close();
            _currentFileStream = null;
        }
        stream.Write(_bytesBoundary);
        stream.Write("Content-Disposition: form-data; name=\"".ToUtf8());
        stream.Write(name.ToUtf8()); // there is no compatible way of doing this, so might as well just go the utf8-everywhere route...
        stream.Write("\"\r\nContent-Type: text/plain; charset=utf-8\r\n\r\n".ToUtf8());
        stream.Write(value.ToUtf8());
        stream.Write(_bytesNewline);
    }

    /// <summary>
    ///     Adds a named file (or, generally, a binary data field) to the POST request.</summary>
    /// <param name="name">
    ///     The name of the value to add. For maximum compatibility with servers, use only printable ASCII characters in this
    ///     name. This field is encoded using UTF-8, which is supported by some modern servers, but not by others.</param>
    /// <param name="filename">
    ///     The filename to use. The server may interpret this as it pleases, but this value will often end up being exposed
    ///     as the name of the uploaded file. For maximum compatibility with servers, use only printable ASCII characters in
    ///     this name. This field is encoded using UTF-8, which is supported by some modern servers, but not by others.</param>
    /// <param name="contentType">
    ///     The content type to specify for this data/file. Some servers decide whether to accept or reject an upload based on
    ///     the content type. Specify <c>null</c> to prevent the inclusion of the Content-Type header.</param>
    /// <returns>
    ///     A stream into which the binary data is to be written. You may close this stream when done, but if you don't, it
    ///     will be closed automatically next time you add a field or a file, or if you call <see cref="GetResponse"/> (which
    ///     prevents further writing).</returns>
    public MultipartFileStream AddFile(string name, string filename, string contentType = "application/octet-stream")
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (filename == null)
            throw new ArgumentNullException(nameof(filename));
        if (_request == null)
            throw new InvalidOperationException("The request has already been sent in full. This operation is no longer legal.");
        if (_currentFileStream != null)
        {
            _currentFileStream.Close();
            _currentFileStream = null;
        }
        stream.Write(_bytesBoundary);
        stream.Write("Content-Disposition: form-data; name=\"".ToUtf8());
        stream.Write(name.ToUtf8()); // there is no compatible way of doing this, so might as well just go the utf8-everywhere route...
        stream.Write("\"; filename=\"".ToUtf8());
        stream.Write(filename.ToUtf8()); // same as above... See a related test here: http://greenbytes.de/tech/tc2231/#attwithutf8fnplain
        if (contentType == null)
            stream.Write("\"\r\n\r\n".ToUtf8());
        else
        {
            stream.Write("\"\r\nContent-Type: ".ToUtf8());
            stream.Write(contentType.ToUtf8());
            stream.Write("\r\n\r\n".ToUtf8());
        }
        _currentFileStream = new MultipartFileStream(stream);
        return _currentFileStream;
    }

    /// <summary>
    ///     Adds a named file (or, generally, a binary data field) to the POST request.</summary>
    /// <param name="name">
    ///     The name of the value to add. For maximum compatibility with servers, use only printable ASCII characters in this
    ///     name. This field is encoded using UTF-8, which is supported by some modern servers, but not by others.</param>
    /// <param name="filename">
    ///     The filename to use. The server may interpret this as it pleases, but this value will often end up being exposed
    ///     as the name of the uploaded file. For maximum compatibility with servers, use only printable ASCII characters in
    ///     this name. This field is encoded using UTF-8, which is supported by some modern servers, but not by others.</param>
    /// <param name="data">
    ///     The binary data to send as the content of this file.</param>
    /// <param name="contentType">
    ///     The content type to specify for this data/file. Some servers decide whether to accept or reject an upload based on
    ///     the content type. Specify <c>null</c> to prevent the inclusion of the Content-Type header.</param>
    public void AddFile(string name, string filename, byte[] data, string contentType = "application/octet-stream")
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        using (var filestream = AddFile(name, filename, contentType))
            filestream.Write(data);
    }

    /// <summary>
    ///     Encapsulates a stream used for writing a file body into a multipart/form-data stream.</summary>
    /// <remarks>
    ///     The main purpose is to prevent the caller accidentally closing the request stream (which results in an exception
    ///     whose cause is pretty tricky to establish).</remarks>
    public sealed class MultipartFileStream : Stream
    {
        private Stream _stream;

        internal MultipartFileStream(Stream underlyingStream)
        {
            _stream = underlyingStream;
        }

        /// <summary>
        ///     Closes the stream and prevents further writing to this stream. You may call this explicitly; you may call <see
        ///     cref="Stream.Dispose()"/> instead, or you could just leave it up to <see cref="MultipartFormDataHelper"/> to
        ///     close this stream automatically next time a new field or file is added, or when the helper itself is closed.</summary>
        public override void Close()
        {
            if (_stream == null)
                return;
            _stream.Write(_bytesNewline);
            _stream = null;
            base.Close();
        }

        /// <summary>Writes file data to the request stream.</summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_stream == null)
                throw new InvalidOperationException("The multipart/form-data file stream has already been closed and can no longer be written to.");
            _stream.Write(buffer, offset, count);
        }

        /// <summary>Always <c>true</c>.</summary>
        public override bool CanWrite { get { return true; } }
        /// <summary>Always <c>false</c>.</summary>
        public override bool CanRead { get { return false; } }
        /// <summary>Always <c>false</c>.</summary>
        public override bool CanSeek { get { return false; } }
        /// <summary>Flushes the request stream.</summary>
        public override void Flush() { _stream.Flush(); }

        /// <summary>Throws a <c>NotSupportedException</c>.</summary>
        public override long Length { get { throw new NotSupportedException(); } }
        /// <summary>Throws a <c>NotSupportedException</c>.</summary>
        public override void SetLength(long value) { throw new NotSupportedException(); }
        /// <summary>Throws a <c>NotSupportedException</c>.</summary>
        public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
        /// <summary>Throws a <c>NotSupportedException</c>.</summary>
        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
        /// <summary>Throws a <c>NotSupportedException</c>.</summary>
        public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }
}
