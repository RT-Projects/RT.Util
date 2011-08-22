using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RT.Util
{
    /// <summary>Manages a global low-level keyboard hook.</summary>
    public sealed class GlobalKeyboardListener
    {
        /// <summary>
        /// The collections of keys to watch for. This is ignored if <see cref="HookAllKeys"/> is set to true.
        /// </summary>
        public List<Keys> HookedKeys { get { return _hookedKeys; } }
        private List<Keys> _hookedKeys = new List<Keys>();

        /// <summary>
        /// Gets or sets a value indicating whether all keys are listened for. If this is set to true, <see cref="HookedKeys"/> is ignored.
        /// </summary>
        public bool HookAllKeys { get; set; }

        /// <summary>
        /// Handle to the hook, need this to unhook and call the next hook
        /// </summary>
        private IntPtr _hHook = IntPtr.Zero;

        #region Events
        /// <summary>
        /// Occurs when one of the hooked keys is pressed.
        /// </summary>
        public event GlobalKeyEventHandler KeyDown;
        /// <summary>
        /// Occurs when one of the hooked keys is released.
        /// </summary>
        public event GlobalKeyEventHandler KeyUp;
        #endregion

        #region Constructors and Destructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalKeyboardListener"/> class and installs the keyboard hook.
        /// </summary>
        public GlobalKeyboardListener()
        {
            hook();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="GlobalKeyboardListener"/> is reclaimed by garbage collection and uninstalls the keyboard hook.
        /// </summary>
        ~GlobalKeyboardListener()
        {
            unhook();
        }
        #endregion

        private WinAPI.KeyboardHookProc _hook;

        #region Public Methods
        /// <summary>
        /// Installs the global hook
        /// </summary>
        private void hook()
        {
            IntPtr hInstance = WinAPI.LoadLibrary("User32");
            _hook = new WinAPI.KeyboardHookProc(hookProc);
            _hHook = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, _hook, hInstance, 0);
        }

        /// <summary>
        /// Uninstalls the global hook
        /// </summary>
        private void unhook()
        {
            WinAPI.UnhookWindowsHookEx(_hHook);
        }

        /// <summary>The callback for the keyboard hook.</summary>
        /// <param name="code">The hook code. If this is &lt; 0, the callback shouldn’t do anyting.</param>
        /// <param name="wParam">The event type. Only <c>WM_(SYS)?KEY(DOWN|UP)</c> events are handled.</param>
        /// <param name="lParam">Information about the key pressed/released.</param>
        private int hookProc(int code, int wParam, ref WinAPI.KeyboardHookStruct lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys) lParam.vkCode;

                if (HookAllKeys || _hookedKeys.Contains(key))
                {
                    var kea = new GlobalKeyEventArgs(key, lParam.scanCode);
                    if ((wParam == WinAPI.WM_KEYDOWN || wParam == WinAPI.WM_SYSKEYDOWN) && (KeyDown != null))
                    {
                        KeyDown(this, kea);
                    }
                    else if ((wParam == WinAPI.WM_KEYUP || wParam == WinAPI.WM_SYSKEYUP) && (KeyUp != null))
                    {
                        KeyUp(this, kea);
                    }
                    if (kea.Handled)
                        return 1;
                }
            }
            return WinAPI.CallNextHookEx(_hHook, code, wParam, ref lParam);
        }
        #endregion
    }

    /// <summary>Contains arguments for the KeyUp/KeyDown event in a <see cref="GlobalKeyboardListener"/>.</summary>
    public sealed class GlobalKeyEventArgs : EventArgs
    {
        /// <summary>The virtual-key code of the key being pressed or released.</summary>
        public Keys VirtualKeyCode { get; private set; }
        /// <summary>The scancode of the key being pressed or released.</summary>
        public int ScanCode { get; private set; }
        /// <summary>Set this to ‘true’ to prevent further processing of the keystroke (i.e. to ‘swallow’ it).</summary>
        public bool Handled { get; set; }

        /// <summary>Constructor.</summary>
        public GlobalKeyEventArgs(Keys virtualKeyCode, int scanCode)
        {
            VirtualKeyCode = virtualKeyCode;
            ScanCode = scanCode;
            Handled = false;
        }
    }

    /// <summary>Used to trigger the KeyUp/KeyDown events in <see cref="GlobalKeyboardListener"/>.</summary>
    public delegate void GlobalKeyEventHandler(object sender, GlobalKeyEventArgs e);
}