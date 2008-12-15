using System;
using System.Runtime.InteropServices;

namespace RT.Util
{
    /// <summary>
    /// WinAPI function wrappers
    /// </summary>
    public static class WinAPI
    {
        static WinAPI()
        {
            QueryPerformanceFrequency(out PerformanceFreq);
        }

        /// <summary>
        /// This field is statically initialised by calling QueryPerformanceFrequency.
        /// It contains the frequency of the performance counter for the current system.
        /// </summary>
        public static readonly long PerformanceFreq;

#pragma warning disable 1591    // Missing XML comment for publicly visible type or member

        #region Enums / flags / consts

        /// <summary>Specifies a sound to be played back when displaying a message dialog.</summary>
        public enum MessageBeepType
        {
            /// <summary>Specifies the default sound.</summary>
            Default = -1,
            /// <summary>Specifies the OK sound.</summary>
            Ok = 0x00000000,
            /// <summary>Specifies the error sound.</summary>
            Error = 0x00000010,
            /// <summary>Specifies the question sound.</summary>
            Question = 0x00000020,
            /// <summary>Specifies the warning sound.</summary>
            Warning = 0x00000030,
            /// <summary>Specifies the information sound.</summary>
            Information = 0x00000040,
        }

        [Flags]
        public enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            Inherit = 0x80000000,
            All = 0x0000001F
        }

        // Low-Level Keyboard Constants
        public const int HC_ACTION = 0;
        public const int LLKHF_EXTENDED = 0x1;
        public const int LLKHF_INJECTED = 0x10;
        public const int LLKHF_ALTDOWN = 0x20;
        public const int LLKHF_UP = 0x80;

        // Virtual Keys
        public const int VK_TAB = 0x9;
        public const int VK_CONTROL = 0x11;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_DELETE = 0x2E;

        public const int WH_KEYBOARD_LL = 13;

        #endregion

        #region Structs

        public struct MemoryStatus
        {

            public uint Length; //Length of struct
            public uint MemoryLoad; //Value from 0-100 represents memory usage
            public uint TotalPhysical;
            public uint AvailablePhysical;
            public uint TotalPageFile;
            public uint AvailablePageFile;
            public uint TotalVirtual;
            public uint AvailableVirtual;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        public struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        #endregion

        #region Delegates

        public delegate int KeyboardHookDelegate(int Code, int wParam, WinAPI.KBDLLHOOKSTRUCT lParam);

        #endregion

        #region Function imports

        [DllImport("Kernel32.dll")]
        public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MessageBeep(MessageBeepType type);

        [DllImport("kernel32.dll")]
        public static extern void GlobalMemoryStatus(out MemoryStatus mem);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll")]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern int UnhookWindowsHookEx(int hHook);

        [DllImport("user32.dll", EntryPoint = "SetWindowsHookExA")]
        public static extern int SetWindowsHookEx(int idHook, KeyboardHookDelegate lpfn, int hmod, int dwThreadId);

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(int hHook, int nCode, int wParam, KBDLLHOOKSTRUCT lParam);

        [DllImport("user32.dll")]
        public static extern int GetLastError();

        #endregion

#pragma warning restore 1591    // Missing XML comment for publicly visible type or member

    }

}
