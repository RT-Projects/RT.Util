using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RT.Util
{
    /// <summary>Manages a global low-level keyboard hook.</summary>
    public sealed class GlobalKeyboardListener : IDisposable
    {
        /// <summary>The collections of keys to watch for. This is ignored if <see cref="HookAllKeys" /> is set to true.</summary>
        public List<Keys> HookedKeys { get { return _hookedKeys; } }
        private List<Keys> _hookedKeys = new List<Keys>();

        /// <summary>
        ///     Gets or sets a value indicating whether all keys are listened for. If this is set to true, <see
        ///     cref="HookedKeys" /> is ignored.</summary>
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

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalKeyboardListener" /> class and installs the keyboard hook.</summary>
        public GlobalKeyboardListener()
        {
            IntPtr hInstance = WinAPI.LoadLibrary("User32");
            _hook = hookProc;   // don’t remove this or the garbage collector will collect it while the global hook still tries to access it
            _hHook = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, _hook, IntPtr.Zero, 0);
        }

        private bool _disposed = false;

        /// <summary>
        ///     Releases unmanaged resources and performs other cleanup operations before the <see
        ///     cref="GlobalKeyboardListener" /> is reclaimed by garbage collection and uninstalls the keyboard hook.</summary>
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

    /// <summary>Contains arguments for the KeyUp/KeyDown event in a <see cref="GlobalKeyboardListener" />.</summary>
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

    /// <summary>Used to trigger the KeyUp/KeyDown events in <see cref="GlobalKeyboardListener" />.</summary>
    public delegate void GlobalKeyEventHandler(object sender, GlobalKeyEventArgs e);
}
