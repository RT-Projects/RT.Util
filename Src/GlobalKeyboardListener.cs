using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RT.Util
{
    /// <summary>
    /// Listens for a global keyboard shortcut and fires an event when the user presses it.
    /// 
    /// WARNING: This code is unfinished; review carefully before using in new stuff.
    /// </summary>
    public class GlobalKeyboardListener
    {
#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        private int KeyboardHandle;

        /*
        // Implement this function to block as many
        // key combinations as you'd like
        public bool IsHooked(WinAPI.KBDLLHOOKSTRUCT Hookstruct)
        {
            Console.WriteLine("Hookstruct.vkCode: " + Hookstruct.vkCode);
            Console.WriteLine(Hookstruct.vkCode = WinAPI.VK_ESCAPE);
            Console.WriteLine(Hookstruct.vkCode = WinAPI.VK_TAB);

            if ((Hookstruct.vkCode == WinAPI.VK_ESCAPE) && (WinAPI.GetAsyncKeyState(WinAPI.VK_CONTROL) & 0x8000) != 0)
            {
                HookedState("Ctrl + Esc blocked");
                return true;
            }

            if ((Hookstruct.vkCode == WinAPI.VK_TAB) && (Hookstruct.flags & WinAPI.LLKHF_ALTDOWN) != 0)
            {
                HookedState("Alt + Tab blockd");
                return true;
            }

            if ((Hookstruct.vkCode == WinAPI.VK_ESCAPE) && (Hookstruct.flags & WinAPI.LLKHF_ALTDOWN) != 0)
            {
                HookedState("Alt + Escape blocked");
                return true;
            }

            return false;
        }

        private void HookedState(string Text)
        {
            Console.WriteLine(Text);
        }
        */

        public int KeyboardCallback(int Code, int wParam, WinAPI.KBDLLHOOKSTRUCT lParam)
        {
            if (Code == WinAPI.HC_ACTION)
            {
                //Console.WriteLine("Calling IsHooked");
                /*
                if (IsHooked(lParam))
                {
                    return 1;
                }*/
            }
            return WinAPI.CallNextHookEx(KeyboardHandle, Code, wParam, lParam);
        }

        [MarshalAs(UnmanagedType.FunctionPtr)]
        private WinAPI.KeyboardHookDelegate callback;

        public void HookKeyboard()
        {
            callback = new WinAPI.KeyboardHookDelegate(KeyboardCallback);

            KeyboardHandle = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, callback,
                Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]).ToInt32(), 0);

            CheckHooked();
        }

        public void CheckHooked()
        {
            if (Hooked())
                Console.WriteLine("Keyboard hooked");
            else
                Console.WriteLine("Keyboard hook failed: " + WinAPI.GetLastError());
        }

        private bool Hooked()
        {
            return KeyboardHandle != 0;
        }

        public void UnhookKeyboard()
        {
            if (Hooked())
            {
                WinAPI.UnhookWindowsHookEx(KeyboardHandle);
            }
        }

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member
    }
}