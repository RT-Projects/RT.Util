namespace RT.Util.Streams;

/// <summary>Calculates RSync checksums over bytes.</summary>
public sealed class RSyncChecksumCalculator
{
    private uint _windowSize;
    private byte[] _window;
    private bool _windowFull;
    private int _windowHead;
    private int _windowTail; // not used until window is full

    private uint _a, _b;

    /// <summary>
    ///     Initialises the checksum calculator. Window Size determines the number of bytes which are hashed (see rsync
    ///     algorithm details if this is unclear).</summary>
    public RSyncChecksumCalculator(int windowSize)
    {
        _windowSize = (uint) windowSize;

        _window = new byte[windowSize];
        _windowFull = false;
        _windowHead = -1;

        _a = _b = 0;
    }

    /// <summary>Passes all bytes in the array through the rsync hash algorithm.</summary>
    public void ProcessBytes(byte[] buffer)
    {
        ProcessBytes(buffer, 0, buffer.Length);
    }

    /// <summary>Passes the specified bytes in the array through the rsync hash algorithm.</summary>
    public void ProcessBytes(byte[] buffer, int offset, int count)
    {
        int endoffset = offset + count;
        if (endoffset > buffer.Length || offset < 0)
            throw new ArgumentOutOfRangeException("The arguments to ProcessBytes() point outside the array.");

        if (!_windowFull)
        {
            for (; offset < endoffset; offset++)
            {
                _windowHead++;
                _window[_windowHead] = buffer[offset];

                _a += buffer[offset];
                _b += buffer[offset] * (_windowSize - (uint) _windowHead);

                if (_windowHead == _windowSize - 1)
                {
                    offset++;
                    _windowTail = 0;
                    _windowFull = true;
                    break;
                }
            }
        }

        for (; offset < endoffset; offset++)
        {
            _a += (uint) (buffer[offset] - _window[_windowTail]);
            _b += _a - _window[_windowTail] * _windowSize;

            if (_windowHead < _windowSize - 1) _windowHead++; else _windowHead = 0;
            if (_windowTail < _windowSize - 1) _windowTail++; else _windowTail = 0;
            _window[_windowHead] = buffer[offset];
        }

    }

    /// <summary>Returns the rsync checksum calculated so far.</summary>
    public uint CurrentChecksum
    {
        get
        {
            return (_b << 16) | (_a & 0xFFFF);
        }
    }

    /// <summary>Returns the rsync checksum calculated so far.</summary>
    public byte[] CurrentChecksumBytes
    {
        get
        {
            uint cs = CurrentChecksum;
            return new byte[] { (byte) cs, (byte) (cs >> 8), (byte) (cs >> 16), (byte) (cs >> 24) };
        }
    }
}

/// <summary>
///     Timwi's version of the RSync checksum calculator. Based on the generic queue class. May or may not be noticeably
///     slower than the much longer version above.</summary>
public sealed class RSyncChecksumCalculatorTimwi
{
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
    private uint _windowSize;
    private Queue<byte> _window;

    private uint _a, _b;

    public RSyncChecksumCalculatorTimwi(int windowSize)
    {
        _windowSize = (uint) windowSize;

        _window = new Queue<byte>(windowSize + 1);

        _a = _b = 0;
    }

    public void ProcessBytes(byte[] buffer)
    {
        ProcessBytes(buffer, 0, buffer.Length);
    }

    public void ProcessBytes(byte[] buffer, int offset, int count)
    {
        int endoffset = offset + count;
        if (endoffset > buffer.Length || offset < 0)
            throw new ArgumentOutOfRangeException("The arguments to ProcessBytes() point outside the array.");

        for (uint i = (uint) offset; i < endoffset; i++)
        {
            _window.Enqueue(buffer[i]);
            _a += buffer[i];
            if (_window.Count > _windowSize)
            {
                byte by = _window.Dequeue();
                _a -= by;
                _b += _a - _windowSize * by;
            }
            else
                _b += (_windowSize - i) * buffer[i];
        }
    }

    public uint CurrentChecksum
    {
        get
        {
            return (_b << 16) | (_a & 0xFFFF);
        }
    }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
}

/// <summary>Calculates rsync checksum of all values that are read/written via this stream.</summary>
public sealed class RSyncChecksumStream : Stream
{
    private Stream _stream = null;
    private RSyncChecksumCalculator _calc;

    /// <summary>
    ///     This is the underlying stream. All reads/writes and most other operations on this class are performed on this
    ///     underlying stream.</summary>
    public Stream BaseStream { get { return _stream; } }

    private RSyncChecksumStream() { }

    /// <summary>
    ///     Initialises an rsync calculation stream using the specified stream as the underlying stream and the specified
    ///     rsync window size (number of bytes)</summary>
    public RSyncChecksumStream(Stream stream, int window)
    {
        _stream = stream;
        _calc = new RSyncChecksumCalculator(window);
    }

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member
    public override bool CanRead { get { return _stream.CanRead; } }
    public override bool CanSeek { get { return _stream.CanSeek; } }
    public override bool CanWrite { get { return _stream.CanWrite; } }
    public override void Flush() { _stream.Flush(); }
    public override long Length { get { return _stream.Length; } }

    public override long Position
    {
        get { return _stream.Position; }
        set { _stream.Position = value; }
    }

    /// <summary>Seeking is ignored (but propagated to the underlying stream). All the bytes seeked over will be ignored.</summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return _stream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _stream.SetLength(value);
    }
#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

    /// <summary>Reads data from the underlying stream. Updates the RSync with the bytes read.</summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        int numread = _stream.Read(buffer, offset, count);
        _calc.ProcessBytes(buffer, offset, numread);

        return numread;
    }

    /// <summary>Writes data to the underlying stream. Updates the RSync with the bytes written.</summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
        _calc.ProcessBytes(buffer, offset, count);
    }

    /// <summary>Returns the rsync checksum calculated so far for all the bytes read/written.</summary>
    public uint CurrentChecksum
    {
        get
        {
            return _calc.CurrentChecksum;
        }
    }

    /// <summary>Returns the rsync checksum calculated so far for all the bytes read/written.</summary>
    public byte[] CurrentChecksumBytes
    {
        get
        {
            return _calc.CurrentChecksumBytes;
        }
    }

}
