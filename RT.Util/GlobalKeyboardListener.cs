using System.ComponentModel;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace RT.Util;

/// <summary>Manages a global low-level keyboard hook.</summary>
public sealed class GlobalKeyboardListener : IDisposable
{
    /// <summary>The collections of keys to watch for. This is ignored if <see cref="HookAllKeys"/> is set to true.</summary>
    public List<Keys> HookedKeys { get { return _hookedKeys; } }
    private List<Keys> _hookedKeys = new List<Keys>();

    /// <summary>
    ///     Gets or sets a value indicating whether all keys are listened for. If this is set to true, <see
    ///     cref="HookedKeys"/> is ignored.</summary>
    public bool HookAllKeys { get; set; }

    /// <summary>Handle to the hook, need this to unhook and call the next hook</summary>
    private IntPtr _hHook = IntPtr.Zero;

    /// <summary>Current state of each modifier key.</summary>
    private bool _ctrl, _alt, _shift, _win;

    /// <summary>Occurs when one of the hooked keys is pressed.</summary>
    public event GlobalKeyEventHandler KeyDown;
    /// <summary>Occurs when one of the hooked keys is released.</summary>
    public event GlobalKeyEventHandler KeyUp;

    /// <summary>Keeps the managed delegate referenced so that the garbage collector doesn’t collect it.</summary>
    private WinAPI.KeyboardHookProc _hook;

    private DesktopLockNotifierForm _lockNotifier;
    private Timer _afterLockTimer;

    /// <summary>Initializes a new instance of the <see cref="GlobalKeyboardListener"/> class and installs the keyboard hook.</summary>
    public GlobalKeyboardListener()
    {
        IntPtr hInstance = WinAPI.LoadLibrary("User32");
        _hook = hookProc;   // don’t remove this or the garbage collector will collect it while the global hook still tries to access it
        _hHook = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, _hook, IntPtr.Zero, 0);
        _lockNotifier = new DesktopLockNotifierForm();
        _lockNotifier.SessionLocked += recheckPossibleSwallowedKeys;
        _lockNotifier.SessionUnlocked += recheckPossibleSwallowedKeys;
        _afterLockTimer = new Timer();
        _afterLockTimer.Interval = 1000;
        _afterLockTimer.Tick += recheckPossibleSwallowedKeysTimer;
    }

    private bool _disposed = false;

    /// <summary>
    ///     Releases unmanaged resources and performs other cleanup operations before the <see cref="GlobalKeyboardListener"/>
    ///     is reclaimed by garbage collection and uninstalls the keyboard hook.</summary>
    ~GlobalKeyboardListener()
    {
        Dispose();
    }

    /// <summary>Unregisters the hook and disposes the object.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _lockNotifier.Close();
            _lockNotifier.Dispose();
            _afterLockTimer.Dispose();
            WinAPI.UnhookWindowsHookEx(_hHook);
        }
    }

    /// <summary>
    ///     The callback for the keyboard hook.</summary>
    /// <param name="code">
    ///     The hook code. If this is &lt; 0, the callback shouldn’t do anyting.</param>
    /// <param name="wParam">
    ///     The event type. Only <c>WM_(SYS)?KEY(DOWN|UP)</c> events are handled.</param>
    /// <param name="lParam">
    ///     Information about the key pressed/released.</param>
    private int hookProc(int code, int wParam, ref WinAPI.KeyboardHookStruct lParam)
    {
        if (code >= 0)
        {
            Keys key = (Keys) lParam.vkCode;

            if (HookAllKeys || _hookedKeys.Contains(key))
            {
                if ((wParam == WinAPI.WM_KEYDOWN || wParam == WinAPI.WM_SYSKEYDOWN))
                {
                    switch (key)
                    {
                        case Keys.ControlKey:
                        case Keys.LControlKey:
                        case Keys.RControlKey: _ctrl = true; break;

                        case Keys.Menu:
                        case Keys.LMenu:
                        case Keys.RMenu: _alt = true; break;

                        case Keys.ShiftKey:
                        case Keys.LShiftKey:
                        case Keys.RShiftKey: _shift = true; break;

                        case Keys.LWin:
                        case Keys.RWin: _win = true; break;
                    }
                    if (KeyDown != null)
                    {
                        var kea = new GlobalKeyEventArgs(key, lParam.scanCode, new ModifierKeysState(_ctrl, _alt, _shift, _win));
                        KeyDown(this, kea);
                        if (kea.Handled)
                            return 1;
                    }
                }
                else if ((wParam == WinAPI.WM_KEYUP || wParam == WinAPI.WM_SYSKEYUP))
                {
                    switch (key)
                    {
                        case Keys.ControlKey:
                        case Keys.LControlKey:
                        case Keys.RControlKey: _ctrl = false; break;

                        case Keys.Menu:
                        case Keys.LMenu:
                        case Keys.RMenu: _alt = false; break;

                        case Keys.ShiftKey:
                        case Keys.LShiftKey:
                        case Keys.RShiftKey: _shift = false; break;

                        case Keys.LWin:
                        case Keys.RWin: _win = false; break;
                    }
                    if (KeyUp != null)
                    {
                        var kea = new GlobalKeyEventArgs(key, lParam.scanCode, new ModifierKeysState(_ctrl, _alt, _shift, _win));
                        KeyUp(this, kea);
                        if (kea.Handled)
                            return 1;
                    }
                }
            }
        }
        return WinAPI.CallNextHookEx(_hHook, code, wParam, ref lParam);
    }

    private void recheckPossibleSwallowedKeys(object sender, EventArgs e)
    {
        // When the desktop is locked with a quick press of Win+L, the up events for these keypresses are swallowed, even though all subsequent keypresses
        // get through to the handler even though we're on the lock screen already. In this scenario the "Session Locked" notification can arrive while the keys are
        // actually still down, even though the up events will still be swallowed shortly after, breaking this check. Hence the repeat on timeout.
        // A similar issue occurs with Ctrl/Alt when unlocking with Ctrl+Alt+Del, and it similarly requires a timer to re-check the keys.
        recheckPossibleSwallowedKeys();
        _afterLockTimer.Start();
        // All of this key swallowing on lock/unlock means that Up/Down events from this Global Keyboard Listener are not always paired correctly.
        // We do not attempt to emulate the swallowed events even for the modifier keys (which are easier because we track their state already).
    }

    private void recheckPossibleSwallowedKeysTimer(object sender, EventArgs e)
    {
        _afterLockTimer.Stop();
        recheckPossibleSwallowedKeys();
    }

    private void recheckPossibleSwallowedKeys()
    {
        if (_ctrl && WinAPI.GetKeyState((int) Keys.LControlKey) >= 0 && WinAPI.GetKeyState((int) Keys.RControlKey) >= 0)
            _ctrl = false;
        if (_alt && WinAPI.GetKeyState((int) Keys.LMenu) >= 0 && WinAPI.GetKeyState((int) Keys.RMenu) >= 0)
            _alt = false;
        if (_shift && WinAPI.GetKeyState((int) Keys.LShiftKey) >= 0 && WinAPI.GetKeyState((int) Keys.RShiftKey) >= 0)
            _shift = false;
        if (_win && WinAPI.GetKeyState((int) Keys.LWin) >= 0 && WinAPI.GetKeyState((int) Keys.RWin) >= 0)
            _win = false;
    }
}

/// <summary>Encapsulates the current state of modifier keys.</summary>
public struct ModifierKeysState
{
    private int _state;

    /// <summary>Constructor.</summary>
    public ModifierKeysState(bool ctrl = false, bool alt = false, bool shift = false, bool win = false)
    {
        _state = (ctrl ? 1 : 0) | (alt ? 2 : 0) | (shift ? 4 : 0) | (win ? 8 : 0);
    }

    /// <summary>Gets the state of the Control key (true if left OR right is down).</summary>
    public bool Ctrl { get { return (_state & 1) > 0; } }
    /// <summary>Gets the state of the Alt key (true if left OR right is down).</summary>
    public bool Alt { get { return (_state & 2) > 0; } }
    /// <summary>Gets the state of the Shift key (true if left OR right is down).</summary>
    public bool Shift { get { return (_state & 4) > 0; } }
    /// <summary>Gets the state of the Windows key (true if left OR right is down).</summary>
    public bool Win { get { return (_state & 8) > 0; } }

    /// <summary>Compares the modifiers and returns true iff the two are equal.</summary>
    public static bool operator ==(ModifierKeysState k1, ModifierKeysState k2) { return k1._state == k2._state; }
    /// <summary>Compares the modifiers and returns true iff the two are not equal.</summary>
    public static bool operator !=(ModifierKeysState k1, ModifierKeysState k2) { return k1._state != k2._state; }
    /// <summary>Override; see base.</summary>
    public override bool Equals(object obj) { return (obj is ModifierKeysState) && (this == (ModifierKeysState) obj); }
    /// <summary>Override; see base.</summary>
    public override int GetHashCode() { return _state; }
}

/// <summary>Contains arguments for the KeyUp/KeyDown event in a <see cref="GlobalKeyboardListener"/>.</summary>
public sealed class GlobalKeyEventArgs : EventArgs
{
    /// <summary>The virtual-key code of the key being pressed or released.</summary>
    public Keys VirtualKeyCode { get; private set; }
    /// <summary>The scancode of the key being pressed or released.</summary>
    public int ScanCode { get; private set; }
    /// <summary>Current state of the modifier keys</summary>
    public ModifierKeysState ModifierKeys { get; private set; }
    /// <summary>Set this to ‘true’ to prevent further processing of the keystroke (i.e. to ‘swallow’ it).</summary>
    public bool Handled { get; set; }

    /// <summary>Constructor.</summary>
    public GlobalKeyEventArgs(Keys virtualKeyCode, int scanCode, ModifierKeysState modifierKeys)
    {
        VirtualKeyCode = virtualKeyCode;
        ScanCode = scanCode;
        ModifierKeys = modifierKeys;
        Handled = false;
    }
}

/// <summary>Used to trigger the KeyUp/KeyDown events in <see cref="GlobalKeyboardListener"/>.</summary>
public delegate void GlobalKeyEventHandler(object sender, GlobalKeyEventArgs e);

/// <summary>
///     Subscribes to desktop (session) lock/unlock notifications and exposes events for these. It's untested whether
///     disposing correctly unsubscribes from the notifications, so you should call Close and then Dispose to shut down the
///     notifier.</summary>
public class DesktopLockNotifierForm : Form
{
    /// <summary>Constructor.</summary>
    public DesktopLockNotifierForm()
    {
        WTSRegisterSessionNotification(Handle, 0 /*NOTIFY_FOR_THIS_SESSION*/);
    }
    /// <summary>Close handler.</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        WTSUnRegisterSessionNotification(Handle);
        base.OnClosing(e);
    }
    /// <summary>Triggers every time the desktop (session) is locked.</summary>
    public event EventHandler SessionLocked;
    /// <summary>Triggers every time the desktop (session) is unlocked.</summary>
    public event EventHandler SessionUnlocked;

    /// <summary>Message handler.</summary>
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x2b1/*WM_WTSSESSION_CHANGE*/)
        {
            int value = m.WParam.ToInt32();
            if (value == 7 /*WTS_SESSION_LOCK*/)
                SessionLocked?.Invoke(this, EventArgs.Empty);
            if (value == 8 /*WTS_SESSION_UNLOCK*/)
                SessionUnlocked?.Invoke(this, EventArgs.Empty);
        }
        base.WndProc(ref m);
    }

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern bool WTSRegisterSessionNotification(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)] int dwFlags);
    [DllImport("WtsApi32.dll")]
    static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);
}
