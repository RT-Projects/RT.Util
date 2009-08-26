using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RT.Util
{
    /// <summary>
    /// A class that manages a global low-level keyboard hook
    /// </summary>
    public class GlobalKeyboardListener
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
        private IntPtr hhook = IntPtr.Zero;

        #region Events
        /// <summary>
        /// Occurs when one of the hooked keys is pressed
        /// </summary>
        public event KeyEventHandler KeyDown;
        /// <summary>
        /// Occurs when one of the hooked keys is released
        /// </summary>
        public event KeyEventHandler KeyUp;
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

        private WinAPI.KeyboardHookProc hookDelegate;

        #region Public Methods
        /// <summary>
        /// Installs the global hook
        /// </summary>
        private void hook()
        {
            IntPtr hInstance = WinAPI.LoadLibrary("User32");
            hookDelegate = new WinAPI.KeyboardHookProc(hookProc);
            hhook = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, hookDelegate, hInstance, 0);
        }

        /// <summary>
        /// Uninstalls the global hook
        /// </summary>
        private void unhook()
        {
            WinAPI.UnhookWindowsHookEx(hhook);
        }

        /// <summary>
        /// The callback for the keyboard hook
        /// </summary>
        /// <param name="code">The hook code, if it isn't >= 0, the function shouldn't do anyting</param>
        /// <param name="wParam">The event type</param>
        /// <param name="lParam">The keyhook event information</param>
        /// <returns></returns>
        private int hookProc(int code, int wParam, ref WinAPI.KeyboardHookStruct lParam)
        {
            if (code >= 0)
            {
                Keys key = (Keys) lParam.vkCode;

                if (HookAllKeys || _hookedKeys.Contains(key))
                {
                    KeyEventArgs kea = new KeyEventArgs(key);
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
            return WinAPI.CallNextHookEx(hhook, code, wParam, ref lParam);
        }
        #endregion
    }
}