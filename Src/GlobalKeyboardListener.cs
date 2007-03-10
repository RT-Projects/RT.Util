using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System;

namespace RT.Util
{
    public class GlobalKeyboardListener
    {
        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(int hHook);

        [DllImport("user32.dll", EntryPoint="SetWindowsHookExA")]
        public static extern int SetWindowsHookEx(int idHook, KeyboardHookDelegate lpfn, int hmod, int dwThreadId);

        [DllImport("user32.dll")]
        private static extern int GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int hHook, int nCode, int wParam, KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        private static extern int GetLastError();

        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        // Low-Level Keyboard Constants
        private const int HC_ACTION       = 0;
        private const int LLKHF_EXTENDED  = 0x1;
        private const int LLKHF_INJECTED  = 0x10;
        private const int LLKHF_ALTDOWN   = 0x20;
        private const int LLKHF_UP        = 0x80;

        // Virtual Keys
        public const int VK_TAB     = 0x9;
        public const int VK_CONTROL = 0x11;
        public const int VK_ESCAPE  = 0x1B;
        public const int VK_DELETE  = 0x2E;

        private const int WH_KEYBOARD_LL = 13;

        public int KeyboardHandle;


        // Implement this function to block as many
        // key combinations as you'd like
        public bool IsHooked(KBDLLHOOKSTRUCT Hookstruct)
        {
            Console.WriteLine("Hookstruct.vkCode: " + Hookstruct.vkCode);
            Console.WriteLine(Hookstruct.vkCode = VK_ESCAPE);
            Console.WriteLine(Hookstruct.vkCode = VK_TAB);

            if ((Hookstruct.vkCode == VK_ESCAPE) && (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                HookedState("Ctrl + Esc blocked");
                return true;
            }

            if ((Hookstruct.vkCode == VK_TAB) && (Hookstruct.flags & LLKHF_ALTDOWN) != 0)
            {
                HookedState("Alt + Tab blockd");
                return true;
            }

            if ((Hookstruct.vkCode == VK_ESCAPE) && (Hookstruct.flags & LLKHF_ALTDOWN) != 0)
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

        public int KeyboardCallback(int Code, int wParam, KBDLLHOOKSTRUCT lParam)
        {
            if (Code == HC_ACTION)
            {
                //Console.WriteLine("Calling IsHooked");
                /*
                if (IsHooked(lParam))
                {
                    return 1;
                }*/
            }
            return CallNextHookEx(KeyboardHandle, Code, wParam, lParam);
        }

        public delegate int KeyboardHookDelegate(int Code, int wParam, KBDLLHOOKSTRUCT lParam);

        [MarshalAs(UnmanagedType.FunctionPtr)]
        private KeyboardHookDelegate callback;

        public void HookKeyboard()
        {
            callback = new KeyboardHookDelegate(KeyboardCallback);

            KeyboardHandle = SetWindowsHookEx(WH_KEYBOARD_LL, callback,
                Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]).ToInt32(), 0);

            CheckHooked();
        }

        public void CheckHooked()
        {
            if (Hooked())
                Console.WriteLine("Keyboard hooked");
            else
                Console.WriteLine("Keyboard hook failed: " + GetLastError());
        }

        private bool Hooked()
        {
            return KeyboardHandle != 0;
        }

        public void UnhookKeyboard()
        {
            if (Hooked())
            {
                UnhookWindowsHookEx(KeyboardHandle);
            }
        }
    }
}