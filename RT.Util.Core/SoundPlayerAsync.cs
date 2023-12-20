using System.Runtime.InteropServices;

namespace RT.Util;

/// <summary>The way SoundPlayer was meant to be, but someone screwed it up. This is the official MS workaround.</summary>
public sealed class SoundPlayerAsync : IDisposable
{
    private byte[] _bytesToPlay = null;

    /// <summary>Gets or sets the bytes representing the wave data to be played.</summary>
    public byte[] BytesToPlay
    {
        get { return _bytesToPlay; }
        set
        {
            FreeHandle();
            _bytesToPlay = value;
        }
    }

    private GCHandle? _gcHandle = null;

    /// <summary>Creates a new player using the specified stream as the source of wave data to play.</summary>
    public SoundPlayerAsync(System.IO.Stream stream)
    {
        LoadStream(stream);
    }

    /// <summary>Creates a new player.</summary>
    public SoundPlayerAsync()
    {
    }

    /// <summary>Loads the wave data to play from the specified stream. The stream can be closed immediately afterwards.</summary>
    public void LoadStream(System.IO.Stream stream)
    {
        byte[] bytesToPlay = new byte[stream.Length];
        stream.Read(bytesToPlay, 0, (int) stream.Length);
        BytesToPlay = bytesToPlay;
    }

    /// <summary>Plays the loaded file once, asynchronously.</summary>
    public void Play()
    {
        PlayASync(NativeMethods.SoundFlags.SND_ASYNC);
    }

    /// <summary>Plays the loaded file forever in a loop, asynchronously. This class provides no methods for stopping this.</summary>
    public void PlayLoop()
    {
        PlayASync(NativeMethods.SoundFlags.SND_ASYNC | NativeMethods.SoundFlags.SND_LOOP);
    }

    private void PlayASync(NativeMethods.SoundFlags flags)
    {
        FreeHandle();
        flags |= NativeMethods.SoundFlags.SND_ASYNC;
        if (BytesToPlay != null)
        {
            _gcHandle = GCHandle.Alloc(BytesToPlay, GCHandleType.Pinned);
            flags |= NativeMethods.SoundFlags.SND_MEMORY;
            NativeMethods.PlaySound(_gcHandle.Value.AddrOfPinnedObject(), (UIntPtr) 0, (uint) flags);
        }
        else
        {
            NativeMethods.PlaySound((byte[]) null, (UIntPtr) 0, (uint) flags);
        }
    }

    private void FreeHandle()
    {
        NativeMethods.PlaySound((byte[]) null, (UIntPtr) 0, (uint) 0);
        if (_gcHandle != null)
        {
            _gcHandle.Value.Free();
            _gcHandle = null;
        }
    }

    #region IDisposable Members

    /// <summary>Disposes of the class</summary>
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        BytesToPlay = null;
        if (disposing)
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>Destroys the class</summary>
    ~SoundPlayerAsync()
    {
        Dispose(false);
    }

    #endregion

    private static class NativeMethods
    {
        [DllImport("winmm.dll", SetLastError = true)]
        public static extern bool PlaySound(string pszSound,
         System.UIntPtr hmod, uint fdwSound);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern bool PlaySound(byte[] ptrToSound,
         System.UIntPtr hmod, uint fdwSound);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern bool PlaySound(IntPtr ptrToSound,
         System.UIntPtr hmod, uint fdwSound);

        [Flags]
        public enum SoundFlags : int
        {
            SND_SYNC = 0x0000,            // play synchronously (default)
            SND_ASYNC = 0x0001,           // play asynchronously
            SND_NODEFAULT = 0x0002,       // silence (!default) if sound not found
            SND_MEMORY = 0x0004,          // pszSound points to a memory file
            SND_LOOP = 0x0008,            // loop the sound until next sndPlaySound
            SND_NOSTOP = 0x0010,          // don't stop any currently playing sound
            SND_NOWAIT = 0x00002000,      // don't wait if the driver is busy
            SND_ALIAS = 0x00010000,       // name is a registry alias
            SND_ALIAS_ID = 0x00110000,    // alias is a predefined id
            SND_FILENAME = 0x00020000,    // name is file name
        }
    }

}
